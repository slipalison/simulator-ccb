using System.Reflection;
using Keycloak.AuthServices.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authorization;
using Onboarding.API.Configuration;
using Onboarding.API.Middleware;
using Onboarding.API.Observability;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Infrastructure.Persistence;
using Onboarding.Application;
using Onboarding.Infrastructure;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;

// Bootstrap logger: captures startup errors before DI is configured (Pitfall 1 from RESEARCH.md)
// Uses CreateLogger() (not CreateBootstrapLogger()) to avoid the ReloadableLogger race condition:
// when multiple WebApplicationFactory instances run Program.cs concurrently in tests, they share
// the static Log.Logger. CreateBootstrapLogger() returns a ReloadableLogger; UseSerilog's AddSerilog
// then captures it and calls Freeze() at Build() time. If two factories race, the second Freeze()
// throws "logger is already frozen". With CreateLogger(), Log.Logger is a plain Logger — AddSerilog
// sees no ReloadableLogger and creates a DI-only logger without any Freeze() call.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithSpan()                                                  // OBS-01, D-03: TraceId/SpanId
    .Destructure.With<SensitiveDataDestructuringPolicy>()               // SEC-09, D-21: global masking
    .WriteTo.Console(new CompactJsonFormatter())                         // D-02: JSON console
    .CreateLogger();

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
    // Keycloak 22+ exposes /health/ready on the management port (9000), not the HTTP port (8080).
    var keycloakManagementUrl = builder.Configuration["Keycloak:ManagementUrl"] ?? "http://keycloak:9000";
    var keycloakHealthUrl = keycloakManagementUrl.TrimEnd('/') + "/health/ready";

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
    builder.Services.AddAuthentication()
        .AddJwtBearer("BearerBackoffice", options =>
        {
            // D-04: Auto-discovery via OIDC metadata endpoint
            options.Authority = builder.Configuration["Keycloak:BackofficeRealmUrl"]
                ?? throw new InvalidOperationException("Keycloak:BackofficeRealmUrl not configured.");

            // Allow HTTP authority in development (Keycloak runs on http://localhost:8180 locally)
            options.RequireHttpsMetadata = false;

            // D-05: ROPC tokens have aud: ["account"] — not our API audience. Disable to avoid 401 false positive.
            options.TokenValidationParameters.ValidateAudience = false;

            // IDX10204 fix: OIDC discovery via internal URL (keycloak:8080) does not auto-populate
            // ValidIssuer when KC_HOSTNAME differs. Set it explicitly from config.
            options.TokenValidationParameters.ValidIssuer =
                builder.Configuration["Keycloak:ValidIssuer"] ?? "http://localhost:8180/realms/backoffice";

            // D-04: Preserve Keycloak claim names as-is (e.g. "email" stays "email", not XML namespace URI)
            options.MapInboundClaims = false;

            // Extract realm_access.roles from JWT and add as flat role claims
            options.Events.OnTokenValidated = async context =>
            {
                try
                {
                    var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                    if (identity == null) return;

                    var jwt = context.SecurityToken as Microsoft.IdentityModel.JsonWebTokens.JsonWebToken;
                    if (jwt == null || string.IsNullOrEmpty(jwt.EncodedPayload))
                        return;

                    var encoded = jwt.EncodedPayload;
                    var padded = encoded.Length % 4 == 0 ? encoded : encoded.PadRight(encoded.Length + (4 - encoded.Length % 4), '=');
                    var decoded = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
                    using var doc = System.Text.Json.JsonDocument.Parse(decoded);

                    if (doc.RootElement.TryGetProperty("realm_access", out var realmAccess) &&
                        realmAccess.TryGetProperty("roles", out var rolesArray))
                    {
                        foreach (var role in rolesArray.EnumerateArray())
                        {
                            var roleName = role.GetString();
                            if (!string.IsNullOrEmpty(roleName))
                            {
                                identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleName));
                            }
                        }
                    }
                }
                catch
                {
                    // NEVER throw — this must not break client authentication
                }
            };

            // FIX Test 18: OIDC discovery returns jwks_uri with KC_HOSTNAME (localhost:8180) which is
            // unreachable from inside the API container. The Backchannel rewrites these URLs to use the
            // internal Docker network hostname (keycloak:8080) so the JWKS can be fetched.
            options.Backchannel = new HttpClient(new HostnameRewriteHandler(
                internalHost: "keycloak:8080",
                externalHost: "localhost:8180"))
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
        })
        .AddJwtBearer("BearerClient", options =>
        {
            options.Authority = builder.Configuration["Keycloak:ClientRealmUrl"]
                ?? throw new InvalidOperationException("Keycloak:ClientRealmUrl not configured.");
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters.ValidateAudience = false;
            options.TokenValidationParameters.ValidIssuer = builder.Configuration["Keycloak:ClientRealmUrl"]?.Replace("keycloak:8080", "localhost:8180") ?? "http://localhost:8180/realms/client";
            options.MapInboundClaims = false;

            options.Backchannel = new HttpClient(new HostnameRewriteHandler(
                internalHost: "keycloak:8080",
                externalHost: "localhost:8180"))
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        // 6 permission-based policies (D-06)
        options.AddPolicy(PermissionPolicies.EmployeeRead,
            policy => policy.Requirements.Add(new PermissionRequirement(Permissions.EmployeesRead)));
        options.AddPolicy(PermissionPolicies.EmployeeWrite,
            policy => policy.Requirements.Add(new PermissionRequirement(Permissions.EmployeesWrite)));
        options.AddPolicy(PermissionPolicies.EmployeeDelete,
            policy => policy.Requirements.Add(new PermissionRequirement(Permissions.EmployeesDelete)));
        options.AddPolicy(PermissionPolicies.AuditRead,
            policy => policy.Requirements.Add(new PermissionRequirement(Permissions.AuditRead)));
        options.AddPolicy(PermissionPolicies.DashboardAccess,
            policy => policy.Requirements.Add(new PermissionRequirement(Permissions.DashboardAccess)));
        options.AddPolicy(PermissionPolicies.AccessGroupsManage,
            policy => policy.Requirements.Add(new PermissionRequirement(Permissions.AccessGroupsManage)));

        // Fund permission policies — symmetric with other policy registrations above.
        options.AddPolicy(PermissionPolicies.FundRead,
            policy => policy.Requirements.Add(new PermissionRequirement(Permissions.FundsRead)));
        options.AddPolicy(PermissionPolicies.FundWrite,
            policy => policy.Requirements.Add(new PermissionRequirement(Permissions.FundsWrite)));
        options.AddPolicy(PermissionPolicies.FundDelete,
            policy => policy.Requirements.Add(new PermissionRequirement(Permissions.FundsDelete)));
        options.AddPolicy(PermissionPolicies.FundManage,
            policy => policy.Requirements.Add(new PermissionRequirement(Permissions.FundsManage)));

        // Cross-company policy (D-07) — admin role from backoffice realm
        options.AddPolicy(PermissionPolicies.CrossCompanyAccess,
            policy => policy.RequireRole("admin"));

        // BearerClient + policy combination can be done via
        // [Authorize(AuthenticationSchemes = "BearerClient", Policy = "...")]
    });

    // Keycloak role-based authorization — reads realm_access.roles from JWT
    // and adds them as flat "role" claims so [Authorize(Roles = "admin")] works.
    // Note: Keycloak.AuthServices RolesClaimTransformationSource only supports
    // ResourceAccess, but our tokens have roles in realm_access (realm roles).
    // We handle this manually via custom claims transformation.
    builder.Services.AddKeycloakAuthorization(options =>
    {
        options.EnableRolesMapping = Keycloak.AuthServices.Authorization.RolesClaimTransformationSource.ResourceAccess;
        options.RolesResource = "onboarding-api-admin";
    });

    // Permission-based authorization handler (D-08)
    builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

    // Claims transformation for client realm groups (D-03)
    builder.Services.AddSingleton<IClaimsTransformation, GroupsClaimsTransformation>();

    // Permissions service — scoped per-request, set by ClientClaimsMiddleware (D-10)
    builder.Services.AddScoped<ICurrentCompanyPermissionsService, CurrentCompanyPermissionsService>();

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

    // Infrastructure layer — DbContext, repositories, KeycloakUserService, KC Admin HTTP client
    builder.Services.AddInfrastructure(builder.Configuration);

    // Cookie settings — environment-configured Secure flag
    builder.Services.Configure<CookieSettings>(builder.Configuration.GetSection(nameof(CookieSettings)));

    builder.Services.AddControllers();

    var app = builder.Build();

    // Apply EF Core migrations on startup — creates/updates all tables
    // Skip in "Testing" environment: test factories mock all repositories and share a single
    // test DB, so concurrent migration attempts race and fail with "relation already exists".
    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    // Global exception handler — prevents stack trace exposure, returns standardized ProblemDetails
    app.UseGlobalExceptionHandler();

    app.UseSecurityHeaders();           // Sec-Fetch validation + Cache-Control + security headers (ZAP 90005, 10049)
    app.UseSerilogRequestLogging();     // D-05: per-request log with method/path/status/duration
    app.UseCors("AllowFrontendWithCredentials"); // Must come before UseAuthentication
    app.UseAdminSession();       // Convert backoffice_access_token cookie → Bearer header for /api/admin/*
    app.UseClientSession();      // Convert client_access_token cookie → Bearer header for /api/* (non-admin)
    app.UseAuthentication();   // D-04: populate HttpContext.User — MUST come after session middleware
    app.UseClientClaims();     // Phase 39 D-09: resolve JWT sub → Company/Employee → permissions
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
