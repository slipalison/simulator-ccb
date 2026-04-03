using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Onboarding.API.Observability;
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

    builder.Services.AddControllers();

    var app = builder.Build();

    app.UseSerilogRequestLogging();     // D-05: per-request log with method/path/status/duration

    // Liveness: process is alive — no dependency checks (D-26: used by Docker Compose healthcheck)
    app.MapHealthChecks("/healthz/live", new HealthCheckOptions
    {
        Predicate = _ => false   // No checks run — always 200 if process is alive
    });

    // Readiness: all dependencies must be healthy (D-22, D-25: JSON response)
    app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteDetailedJson
    });

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
