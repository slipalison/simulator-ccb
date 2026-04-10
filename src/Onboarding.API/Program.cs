using System.Reflection;
using Keycloak.AuthServices.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Onboarding.API.Configuration;
using Onboarding.API.Middleware;
using Onboarding.API.Observability;
using Onboarding.Application;
using Onboarding.Infrastructure;
using Onboarding.Infrastructure.Persistence;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;

// Bootstrap logger: captures startup errors before DI is configured (Pitfall 1 from RESEARCH.md)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithSpan()                                                  // OBS-01, D-03: TraceId/SpanId
    .Destructure.With<SensitiveDataDestructuringPolicy>()               // SEC-09, D-21: global masking
    .WriteTo.Console(new CompactJsonFormatter())                         // D-02: JSON console
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Replace .NET logging with Serilog — reads Serilog config from appsettings.{env}.json (D-01, D-04)
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithSpan()                                              // OBS-01, D-03
        .Destructure.With<SensitiveDataDestructuringPolicy>()           // SEC-09, D-21
        .WriteTo.Console(new CompactJsonFormatter())                    // D-02
        .WriteTo.OpenTelemetry(options =>                               // OTLP log export
        {
            options.Endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                ?? "http://localhost:4317";
            options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
                    ?? "onboarding-api"
            };
        }));

    // OpenTelemetry SDK — traces and metrics (D-10, D-11, D-12, D-13)
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r
            .AddService(
                serviceName: builder.Configuration["OTEL_SERVICE_NAME"] ?? "onboarding-api",
                serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(opts =>
            {
                // Exclude health check endpoints from traces — reduces noise (D-12)
                opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/healthz");
            })
            .AddHttpClientInstrumentation()                             // D-14: W3C traceparent on Keycloak calls
            .AddEntityFrameworkCoreInstrumentation()                    // D-10: EF Core query spans
            .AddOtlpExporter())                                         // D-12: reads OTEL_EXPORTER_OTLP_ENDPOINT
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()                             // D-11: HTTP server metrics
            .AddRuntimeInstrumentation()                                // D-11: GC, threads, memory
            .AddOtlpExporter());                                        // D-12: reads OTEL_EXPORTER_OTLP_ENDPOINT

    // Health checks — split live/ready (OBS-05, D-22 through D-26)
    var keycloakRealmUrl = builder.Configuration["Keycloak:RealmUrl"] ?? "http://keycloak:8080/realms/onboarding";
    var keycloakHealthUrl = keycloakRealmUrl.TrimEnd('/').Replace("/realms/onboarding", "") + "/health/ready";

    builder.Services.AddHealthChecks()
        .AddNpgSql(
            connectionString: builder.Configuration.GetConnectionString("AppDb")
                ?? "Host=localhost;Port=5432;Database=onboarding;Username=appuser;Password=placeholder",
            name: "postgresql",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"])
        .AddUrlGroup(
            uri: new Uri(keycloakHealthUrl),
            name: "keycloak",
            failureStatus: HealthStatus.Degraded,
            tags: ["ready"])
        .AddCheck("memory", () => HealthCheckResult.Healthy("memory ok"), ["ready"]);

    // IDistributedCache — used by IdempotencyFilter (Plan 04) and Duende CC token cache
    builder.Services.AddDistributedMemoryCache();

    // Authentication — JWT Bearer with Keycloak OIDC auto-discovery (D-04)
    // Authority reads /.well-known/openid-configuration lazily on first request
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // D-04: Auto-discovery via OIDC metadata endpoint
            options.Authority = builder.Configuration["Keycloak:RealmUrl"]
                ?? throw new InvalidOperationException("Keycloak:RealmUrl not configured.");

            // Allow HTTP authority in development (Keycloak runs on http://localhost:8180 locally)
            // In production, Keycloak:RealmUrl must use HTTPS and this remains false by default
            options.RequireHttpsMetadata = false;

            // D-05: ROPC tokens have aud: ["account"] — not our API audience. Disable to avoid 401 false positive.
            options.TokenValidationParameters.ValidateAudience = false;

            // IDX10204 fix: OIDC discovery via internal URL (keycloak:8080) does not auto-populate
            // ValidIssuer when KC_HOSTNAME differs. Set it explicitly from config.
            options.TokenValidationParameters.ValidIssuer =
                builder.Configuration["Keycloak:ValidIssuer"] ?? "http://localhost:8180/realms/onboarding";

            // D-04: Preserve Keycloak claim names as-is (e.g. "email" stays "email", not XML namespace URI)
            // Without this, User.FindFirst("email") returns null — Pitfall 2 from RESEARCH.md
            options.MapInboundClaims = false;

            // FIX Test 18: OIDC discovery returns jwks_uri with KC_HOSTNAME (localhost:8180) which is
            // unreachable from inside the API container. The Backchannel rewrites these URLs to use the
            // internal Docker network hostname (keycloak:8080) so the JWKS can be fetched.
            options.Backchannel = new HttpClient(new HostnameRewriteHandler(
                internalHost: "keycloak:8080",
                externalHost: "localhost:8180"))
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
        });

    builder.Services.AddAuthorization();

    // Keycloak role-based authorization — transforms nested resource_access/realm_access roles
    // to flat role claims so [Authorize(Roles = "admin")] works.
    builder.Services.AddKeycloakAuthorization(options =>
    {
        options.EnableRolesMapping = Keycloak.AuthServices.Authorization.RolesClaimTransformationSource.ResourceAccess;
        options.RolesResource = "onboarding-api-admin";
    });

    // CORS — allow frontend origin with credentials (cookies)
    const string corsPolicy = "AllowFrontendWithCredentials";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(corsPolicy, policy =>
        {
            policy.WithOrigins("http://localhost:5173","http://localhost:5174") // Vinxi dev server
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Required for httpOnly cookies
        });
    });

    // Application layer — handlers, validators
    builder.Services.AddApplication();

    // Infrastructure layer — DbContext, ClientRepository, KeycloakUserService, KC Admin HTTP client
    builder.Services.AddInfrastructure(builder.Configuration);

    // Cookie settings — environment-configured Secure flag
    builder.Services.Configure<CookieSettings>(builder.Configuration.GetSection(nameof(CookieSettings)));

    builder.Services.AddControllers();

    var app = builder.Build();

    // Apply EF Core migrations on startup — creates/updates all tables
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Global exception handler — prevents stack trace exposure, returns standardized ProblemDetails
    app.UseGlobalExceptionHandler();

    app.UseSerilogRequestLogging();     // D-05: per-request log with method/path/status/duration
    app.UseCors("AllowFrontendWithCredentials"); // Must come before UseAuthentication
    app.UseAuthentication();   // D-04: populate HttpContext.User — MUST come before UseAuthorization
    app.UseAuthorization();    // D-06: enforce [Authorize] attributes — MUST come after UseAuthentication

    // Liveness: process is alive — no dependency checks (D-26: used by Docker Compose healthcheck)
    // AllowAnonymous: health endpoints must bypass UseAuthorization middleware — they are infrastructure
    app.MapHealthChecks("/healthz/live", new HealthCheckOptions
    {
        Predicate = _ => false   // No checks run — always 200 if process is alive
    }).AllowAnonymous();

    // Readiness: all dependencies must be healthy (D-22, D-25: JSON response)
    app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteDetailedJson
    }).AllowAnonymous();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();                // Flush buffered OTLP async sink (anti-pattern from RESEARCH.md)
}

// D-25: Detailed JSON response writer for /healthz/ready
static Task WriteDetailedJson(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json; charset=utf-8";
    var result = System.Text.Json.JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        duration_ms = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            duration_ms = e.Value.Duration.TotalMilliseconds,
            description = e.Value.Description
        })
    });
    return context.Response.WriteAsync(result);
}
