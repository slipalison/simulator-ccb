# Phase 4: Observability - Research

**Researched:** 2026-04-02
**Domain:** .NET Observability (Serilog, OpenTelemetry SDK, Grafana LGTM stack, Health Checks)
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Logging — Serilog**
- D-01: Usar Serilog configurado via `UseSerilog()` no `Program.cs`, substituindo o logging padrão do .NET.
- D-02: Sink obrigatório: Console com formatador JSON (`Serilog.Formatting.Compact.CompactJsonFormatter` ou `RenderedCompactJsonFormatter`).
- D-03: TraceId e SpanId são enriquecidos automaticamente via integração Serilog ↔ OpenTelemetry (`Serilog.Enrichers.Span` ou equivalente). Nenhum campo manual.
- D-04: Nível mínimo: `Information` em produção, `Debug` em desenvolvimento — configurável via `appsettings.{env}.json`.
- D-05: Request logging via middleware `app.UseSerilogRequestLogging()` — log por request com propriedades: método, path, status code, duração.

**Export Target — Grafana Stack no Docker Compose**
- D-06: Adicionar ao `docker-compose.yml` (compose.yaml no projeto) os serviços: Grafana Alloy (collector), Loki (logs), Tempo (traces), Mimir (métricas), Grafana (UI).
- D-07: A API exporta OTLP via variável de ambiente `OTEL_EXPORTER_OTLP_ENDPOINT`. Em `compose.yaml`, essa variável aponta para o Alloy (`http://alloy:4317`). Em produção, basta trocar a variável.
- D-08: Grafana Alloy configurado para receber OTLP (gRPC/HTTP), rotear logs → Loki, traces → Tempo, métricas → Mimir.
- D-09: Grafana exposto na porta 3000, pré-configurado com datasources Loki, Tempo e Mimir via provisioning em arquivo.

**OpenTelemetry — Traces e Métricas**
- D-10: SDK configurado no `Program.cs` via `AddOpenTelemetry()` com instrumentações: `AddAspNetCoreInstrumentation()`, `AddHttpClientInstrumentation()`, `AddEntityFrameworkCoreInstrumentation()`.
- D-11: Métricas: `AddRuntimeInstrumentation()` + `AddAspNetCoreInstrumentation()` exportadas via OTLP.
- D-12: Exporters: `AddOtlpExporter()` para traces e métricas. Endpoint lido de `OTEL_EXPORTER_OTLP_ENDPOINT`.
- D-13: Service name configurável via `OTEL_SERVICE_NAME` (default: `onboarding-api`).

**Correlation ID — W3C traceparent (OBS-04)**
- D-14: Não criar header customizado. O HttpClient instrumentado pelo OpenTelemetry propaga automaticamente o header W3C `traceparent`.
- D-15: O SpanId do trace ativo já é o correlation ID. Aparece em todos os log entries via enriquecimento automático (D-03).
- D-16: Nenhum middleware adicional necessário — propagação é transparente via OTEL context.

**Log Masking — Destructuring Policy (SEC-09)**
- D-17: Implementar `IDestructuringPolicy` customizado no Serilog.
- D-18: Campos a mascarar (case-insensitive): `password`, `token`, `secret`, `client_secret`, todos os valores do header `Authorization`.
- D-19: CPF mascarado como `***.***.***-**`. CNPJ mascarado como `**.***.***/****.***-**`.
- D-20: Email mascarado parcialmente: `a***@domain.com`.
- D-21: A policy é registrada globalmente em `Log.Logger`.

**Health Checks — Split live/ready (OBS-05)**
- D-22: Dois endpoints distintos: `GET /healthz/live` (liveness) e `GET /healthz/ready` (readiness).
- D-23: `/healthz/ready` inclui checks: PostgreSQL (EF Core ping), Keycloak (HTTP GET `/health/ready`), Disco, Memória.
- D-24: Usar `Microsoft.Extensions.Diagnostics.HealthChecks` + `AspNetCore.HealthChecks.Npgsql` + `AspNetCore.HealthChecks.Uris`.
- D-25: Resposta JSON detalhada com nome de cada check, status e duração.
- D-26: Docker Compose healthcheck para a API usa `/healthz/live`.

### Claude's Discretion
- Configuração exata de retenção e limites de memória no Loki/Mimir/Tempo (defaults das imagens são aceitáveis em dev)
- Exata estrutura do arquivo de configuração do Grafana Alloy (`.alloy` ou `config.river`)
- Escolha entre OTLP gRPC (porta 4317) ou HTTP (porta 4318) para o Alloy — gRPC é padrão
- Dashboards Grafana pré-provisionados (se possível, adicionar um básico; se complexo, deixar para fase dedicada)

### Deferred Ideas (OUT OF SCOPE)
- Dashboards Grafana customizados (boards específicos para onboarding)
- Alertas (Grafana alerting ou Alertmanager)
- APM externo (Datadog, New Relic)
- Log sampling em produção
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| OBS-01 | Serilog structured logging (JSON) com TraceId/SpanId automáticos | Serilog 4.3.1 + Serilog.AspNetCore 10.0.0 + Serilog.Enrichers.Span 3.1.0 + Serilog.Sinks.OpenTelemetry 4.2.0 |
| OBS-02 | OpenTelemetry traces instrumentando ASP.NET Core, HttpClient, EF Core | OTel.Extensions.Hosting 1.15.1 + Instrumentation packages; EF Core beta but functional for PostgreSQL |
| OBS-03 | OpenTelemetry metrics (runtime + ASP.NET Core) | OTel.Instrumentation.Runtime 1.15.0 + AddAspNetCoreInstrumentation on MeterProviderBuilder |
| OBS-04 | Correlation ID propagado em chamadas ao Keycloak Admin API | W3C traceparent via OTel HttpClient instrumentation — zero code needed |
| OBS-05 | Health check endpoints (/healthz) para API e Keycloak | Microsoft.Extensions.Diagnostics.HealthChecks 10.0.5 + AspNetCore.HealthChecks.Npgsql 9.0.0 + AspNetCore.HealthChecks.Uris 9.0.0 |
| SEC-09 | Log masking para dados sensíveis (senhas, tokens, secrets não aparecem nos logs) | Custom IDestructuringPolicy — per-object regex/property masking pattern; Serilog.Enrichers.Sensitive as alternative |
</phase_requirements>

---

## Summary

Phase 4 instruments the ASP.NET Core API with structured logging via Serilog, distributed tracing and metrics via the OpenTelemetry SDK, and extends `compose.yaml` with a full Grafana LGTM stack (Alloy + Loki + Tempo + Mimir + Grafana). All keys decisions are already locked in CONTEXT.md, leaving research to verify exact package versions, API shapes, Alloy config syntax, and health check patterns.

The integration chain is: `Serilog (JSON console + OTLP sink) → Alloy (OTLP gRPC 4317) → Loki/Tempo/Mimir → Grafana`. TraceId/SpanId correlation between logs and traces is automatic through `Serilog.Enrichers.Span` reading `System.Diagnostics.Activity`. No custom correlation middleware is needed. W3C `traceparent` propagation from HttpClient to Keycloak calls is transparent via `AddHttpClientInstrumentation()`.

The main technical risk is that `OpenTelemetry.Instrumentation.EntityFrameworkCore` remains in beta (1.15.0-beta.1). It is functional for PostgreSQL via Npgsql and carries no known blockers for this project, but its API can change. All health check packages are Apache 2.0 licensed and OSS-compliant.

**Primary recommendation:** Configure Serilog via `UseSerilog()` in `Program.cs` writing to both Console (JSON) and OTLP sink; configure OTel SDK in the same file; add Grafana stack services to `compose.yaml`; implement `IDestructuringPolicy` for masking; wire split health checks with tagged predicates.

---

## Standard Stack

### Core NuGet Packages (verified against NuGet registry 2026-04-02)

| Library | Verified Version | Purpose | License |
|---------|-----------------|---------|---------|
| Serilog | 4.3.1 | Core logging | Apache 2.0 |
| Serilog.AspNetCore | 10.0.0 | `UseSerilog()` + request logging middleware | Apache 2.0 |
| Serilog.Sinks.OpenTelemetry | 4.2.0 | OTLP export of logs | Apache 2.0 |
| Serilog.Enrichers.Span | 3.1.0 | Enrich logs with TraceId/SpanId from OTel Activity | Apache 2.0 |
| OpenTelemetry.Extensions.Hosting | 1.15.1 | `AddOpenTelemetry()` host integration | Apache 2.0 |
| OpenTelemetry.Instrumentation.AspNetCore | 1.15.1 | Trace HTTP inbound requests | Apache 2.0 |
| OpenTelemetry.Instrumentation.Http | 1.15.0 | Trace outbound HttpClient calls (Keycloak Admin) | Apache 2.0 |
| OpenTelemetry.Instrumentation.EntityFrameworkCore | 1.15.0-beta.1 | Trace EF Core queries to PostgreSQL | Apache 2.0 |
| OpenTelemetry.Instrumentation.Runtime | 1.15.0 | Runtime metrics (GC, threads, memory) | Apache 2.0 |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.15.1 | OTLP exporter for traces + metrics | Apache 2.0 |
| Microsoft.Extensions.Diagnostics.HealthChecks | 10.0.5 | Built-in health check infrastructure | MIT |
| AspNetCore.HealthChecks.NpgSql | 9.0.0 | PostgreSQL readiness check | Apache 2.0 |
| AspNetCore.HealthChecks.Uris | 9.0.0 | HTTP URI readiness check (Keycloak `/health/ready`) | Apache 2.0 |

> Note: `Serilog.AspNetCore` 10.0.0 transitively brings `Serilog` — install `Serilog.AspNetCore` and `Serilog` explicitly to pin both.

### Docker Images (Grafana Stack)

| Image | Tag to Use | Purpose | Notes |
|-------|-----------|---------|-------|
| grafana/alloy | latest (or pin to v1.x) | OTLP collector — routes to backends | Use `latest` for dev; pin for CI |
| grafana/loki | 3 | Log storage | Single-process mode for dev |
| grafana/tempo | latest | Trace storage | Single-binary mode for dev |
| grafana/mimir | latest | Metrics storage (Prometheus-compatible) | Single-process mode for dev |
| grafana/grafana | latest | Visualization UI | Port 3000 |

**Installation (NuGet):**
```bash
dotnet add src/Onboarding.API/Onboarding.API.csproj package Serilog --version 4.3.1
dotnet add src/Onboarding.API/Onboarding.API.csproj package Serilog.AspNetCore --version 10.0.0
dotnet add src/Onboarding.API/Onboarding.API.csproj package Serilog.Sinks.OpenTelemetry --version 4.2.0
dotnet add src/Onboarding.API/Onboarding.API.csproj package Serilog.Enrichers.Span --version 3.1.0
dotnet add src/Onboarding.API/Onboarding.API.csproj package OpenTelemetry.Extensions.Hosting --version 1.15.1
dotnet add src/Onboarding.API/Onboarding.API.csproj package OpenTelemetry.Instrumentation.AspNetCore --version 1.15.1
dotnet add src/Onboarding.API/Onboarding.API.csproj package OpenTelemetry.Instrumentation.Http --version 1.15.0
dotnet add src/Onboarding.API/Onboarding.API.csproj package OpenTelemetry.Instrumentation.EntityFrameworkCore --version 1.15.0-beta.1
dotnet add src/Onboarding.API/Onboarding.API.csproj package OpenTelemetry.Instrumentation.Runtime --version 1.15.0
dotnet add src/Onboarding.API/Onboarding.API.csproj package OpenTelemetry.Exporter.OpenTelemetryProtocol --version 1.15.1
dotnet add src/Onboarding.API/Onboarding.API.csproj package Microsoft.Extensions.Diagnostics.HealthChecks --version 10.0.5
dotnet add src/Onboarding.API/Onboarding.API.csproj package AspNetCore.HealthChecks.NpgSql --version 9.0.0
dotnet add src/Onboarding.API/Onboarding.API.csproj package AspNetCore.HealthChecks.Uris --version 9.0.0
```

> `OpenTelemetry.Instrumentation.EntityFrameworkCore` is pre-release — add `--prerelease` flag or set `<AllowedPreRelease>` in the project.

---

## Architecture Patterns

### Recommended File Structure Changes

```
src/Onboarding.API/
├── Program.cs                    # All observability wired here
├── Observability/
│   └── SensitiveDataDestructuringPolicy.cs   # IDestructuringPolicy impl
├── appsettings.json              # Serilog MinimumLevel: Information
├── appsettings.Development.json  # Serilog MinimumLevel: Debug
infra/
├── alloy/
│   └── config.alloy              # Alloy pipeline: OTLP → Loki/Tempo/Mimir
├── grafana/
│   └── provisioning/
│       └── datasources/
│           └── datasources.yaml  # Auto-provision Loki, Tempo, Mimir
├── loki/
│   └── loki-config.yaml          # Single-process local config (optional)
├── tempo/
│   └── tempo.yaml                # Single-binary local config
compose.yaml                      # Extended with 5 new services
```

### Pattern 1: Serilog Bootstrap in Program.cs

Configure Serilog as early as possible (before `builder.Build()`) to capture startup errors:

```csharp
// Source: Serilog.AspNetCore README + official docs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithSpan()                         // OBS-01: TraceId + SpanId
    .Destructure.With<SensitiveDataDestructuringPolicy>()  // SEC-09
    .WriteTo.Console(new CompactJsonFormatter())           // D-02
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? "http://localhost:4317";
        options.Protocol = OtlpProtocol.Grpc;
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
                ?? "onboarding-api"
        };
    })
    .CreateLogger();

builder.Host.UseSerilog();
```

Then in the middleware pipeline:
```csharp
app.UseSerilogRequestLogging();   // D-05: per-request log with method/path/status/duration
```

### Pattern 2: OpenTelemetry SDK in Program.cs

```csharp
// Source: OpenTelemetry .NET official docs
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(
            serviceName: builder.Configuration["OTEL_SERVICE_NAME"] ?? "onboarding-api",
            serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(opts =>
        {
            // Exclude health check endpoints from traces (noise reduction)
            opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/healthz");
        })
        .AddHttpClientInstrumentation()        // D-14: W3C traceparent on Keycloak calls
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());
```

> The OTLP exporter reads `OTEL_EXPORTER_OTLP_ENDPOINT` from environment automatically when using `AddOtlpExporter()` without explicit endpoint. Set it in `compose.yaml` as `OTEL_EXPORTER_OTLP_ENDPOINT: http://alloy:4317`.

### Pattern 3: IDestructuringPolicy for SEC-09

```csharp
// Source: Serilog docs + IDestructuringPolicy interface
public class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> _sensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "token", "secret", "client_secret", "authorization"
    };

    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        // Only process anonymous/DTO objects — let primitives pass through
        if (value is null || value.GetType().IsPrimitive || value is string)
        {
            result = null!;
            return false;
        }

        var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (!properties.Any(p => _sensitiveKeys.Contains(p.Name)))
        {
            result = null!;
            return false;
        }

        var logProperties = properties.Select(prop =>
        {
            var propValue = prop.GetValue(value);
            if (_sensitiveKeys.Contains(prop.Name))
                return new LogEventProperty(prop.Name, new ScalarValue("[REDACTED]"));
            return new LogEventProperty(prop.Name,
                propertyValueFactory.CreatePropertyValue(propValue, true));
        });

        result = new StructureValue(logProperties);
        return true;
    }
}
```

For CPF/CNPJ/email masking at the string level, implement a separate `ILogEventEnricher` or handle in the destructuring policy when the property name matches `cpf`, `cnpj`, `email`.

### Pattern 4: Split Health Checks

```csharp
// Source: Microsoft Learn ASP.NET Core health checks docs
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("AppDb")!,
        name: "postgresql",
        tags: new[] { "ready" })
    .AddUrlGroup(
        uri: new Uri(builder.Configuration["Keycloak:RealmUrl"] + "/../health/ready"),
        name: "keycloak",
        tags: new[] { "ready" })
    .AddDiskStorageHealthCheck(
        setup => setup.AddDrive("C:\\", 512),   // 512 MB free minimum
        name: "disk",
        tags: new[] { "ready" })
    .AddProcessAllocatedMemoryHealthCheck(
        maximumMegabytesAllocated: 512,
        name: "memory",
        tags: new[] { "ready" });

// Endpoints
app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = _ => false   // Always healthy — process is alive
});

app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteDetailedJson
});
```

> Note: `AddDiskStorageHealthCheck` and `AddProcessAllocatedMemoryHealthCheck` are in `AspNetCore.HealthChecks.System` (package from Xabaril). Verify it is needed or use `Microsoft.Extensions.Diagnostics.HealthChecks`'s built-in `MemoryHealthCheck`. Built-in approach does not require the extra package.

### Pattern 5: Grafana Alloy config.alloy

```alloy
// Source: Grafana Alloy official docs + intro-to-mltp example
// File: infra/alloy/config.alloy

otelcol.receiver.otlp "default" {
  grpc {
    endpoint = "0.0.0.0:4317"
  }
  http {
    endpoint = "0.0.0.0:4318"
  }
  output {
    traces  = [otelcol.processor.batch.default.input]
    metrics = [otelcol.processor.batch.default.input]
    logs    = [otelcol.processor.batch.default.input]
  }
}

otelcol.processor.batch "default" {
  output {
    traces  = [otelcol.exporter.otlp.tempo.input]
    metrics = [otelcol.exporter.prometheus.mimir.input]
    logs    = [otelcol.exporter.loki.default.input]
  }
}

// Traces → Tempo (OTLP gRPC)
otelcol.exporter.otlp "tempo" {
  client {
    endpoint = "http://tempo:4317"
    tls {
      insecure = true
    }
  }
}

// Metrics → Mimir (via prometheus remote_write)
otelcol.exporter.prometheus "mimir" {
  forward_to = [prometheus.remote_write.mimir.receiver]
}

prometheus.remote_write "mimir" {
  endpoint {
    url = "http://mimir:9009/api/v1/push"
  }
}

// Logs → Loki
otelcol.exporter.loki "default" {
  forward_to = [loki.write.local.receiver]
}

loki.write "local" {
  endpoint {
    url = "http://loki:3100/loki/api/v1/push"
  }
}
```

### Pattern 6: Grafana Datasource Provisioning

```yaml
# File: infra/grafana/provisioning/datasources/datasources.yaml
apiVersion: 1

datasources:
  - name: Loki
    type: loki
    access: proxy
    url: http://loki:3100
    isDefault: false

  - name: Tempo
    type: tempo
    access: proxy
    url: http://tempo:3200
    isDefault: false
    jsonData:
      tracesToLogs:
        datasourceUid: loki
        filterByTraceID: true
        filterBySpanID: false

  - name: Mimir
    type: prometheus
    access: proxy
    url: http://mimir:9009/prometheus
    isDefault: true
```

### Anti-Patterns to Avoid

- **Calling `Log.CloseAndFlush()` without try/finally:** Always wrap the entire application in try/finally to flush buffered log entries, especially for OTLP async sinks.
- **Using `AddConsole()` alongside Serilog:** When `UseSerilog()` is set, the built-in ILogger pipeline is replaced. Do not also add `.AddConsole()` — you get duplicate output.
- **Configuring OTLP endpoint in code only:** The endpoint must also be exposed as an env var (`OTEL_EXPORTER_OTLP_ENDPOINT`) so it can change between environments without rebuild.
- **Enabling `SetDbStatementForText` on EF Core instrumentation:** This logs SQL query text which may contain user data. Keep disabled in production.
- **Health check route on `/health` without split:** A single endpoint cannot serve both liveness and readiness semantics. Docker Compose uses `/healthz/live` (fast, no I/O); Kubernetes uses both.
- **Registering `SensitiveDataDestructuringPolicy` only on specific sinks:** The policy must be on `Log.Logger` (global), not per-sink, to guarantee all sinks are covered.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| TraceId/SpanId in logs | Manual `Activity.Current?.TraceId` injection | `Serilog.Enrichers.Span` + `.Enrich.WithSpan()` | Handles null Activity, formats ids correctly, syncs with OTel context propagation |
| W3C traceparent on HttpClient | Manual header injection in DelegatingHandler | `AddHttpClientInstrumentation()` | Automatic context propagation; handles trace sampling decisions |
| Metrics collection | Custom counters/timers | `AddRuntimeInstrumentation()` + `AddAspNetCoreInstrumentation()` on MeterProviderBuilder | Semantic conventions, auto-labeled dimensions |
| PostgreSQL connectivity check | Raw `NpgsqlConnection.OpenAsync()` | `AddNpgSql()` from `AspNetCore.HealthChecks.NpgSql` | Handles connection pooling, timeout, error reporting correctly |
| Keycloak HTTP check | Raw `HttpClient.GetAsync()` | `AddUrlGroup()` from `AspNetCore.HealthChecks.Uris` | Retry policy, timeout, status code validation built-in |
| OTLP routing to Grafana backends | Custom OpenTelemetry Collector config | Grafana Alloy with `.alloy` config | Alloy is Grafana-native, fewer moving parts, first-class Loki/Tempo/Mimir integration |

**Key insight:** The OTel SDK's `AddOtlpExporter()` automatically reads `OTEL_EXPORTER_OTLP_ENDPOINT` from environment — no code change needed between dev and production.

---

## Common Pitfalls

### Pitfall 1: Serilog bootstrap before `builder.Build()` vs. after

**What goes wrong:** Configuring Serilog inside the `app.Use...` pipeline or after `builder.Build()` means startup exceptions (database connection failure, config load error) are never logged.

**Why it happens:** Developers follow ASP.NET Core patterns that configure services after the builder.

**How to avoid:** Create `Log.Logger` as the very first statement in `Program.cs`, before any `builder` calls. Use `builder.Host.UseSerilog()` to replace the hosted service logger.

**Warning signs:** Startup exceptions visible only on stderr with no structured context; `Unhandled exception` with no correlation to a trace.

---

### Pitfall 2: EF Core instrumentation beta breakage

**What goes wrong:** `OpenTelemetry.Instrumentation.EntityFrameworkCore` is `1.15.0-beta.1` — semantic conventions are experimental and the API can change between NuGet updates.

**Why it happens:** This package tracks upstream OTel spec for database spans, which is still evolving.

**How to avoid:** Pin the exact version (`1.15.0-beta.1`) in the `.csproj`. Do not use wildcard version ranges. Add a comment explaining why it is pinned.

**Warning signs:** Build-time errors after `dotnet restore` if version is updated without testing; missing span attributes after upgrade.

---

### Pitfall 3: OTLP sink in Serilog vs. OTel SDK exporter — duplicate log export

**What goes wrong:** Both `Serilog.Sinks.OpenTelemetry` (sends logs via OTLP) and an OTel Logs provider (if added) export logs. This results in duplicate log entries in Loki.

**Why it happens:** Developers add both `AddOpenTelemetry().WithLogging(...)` and `WriteTo.OpenTelemetry(...)`.

**How to avoid:** For this project, use **only** `Serilog.Sinks.OpenTelemetry` for log export. Do not call `WithLogging()` on the OTel SDK builder. The OTel SDK is used only for traces and metrics.

**Warning signs:** Every log entry appears twice in Loki with different resource attributes.

---

### Pitfall 4: Health check endpoint blocked by authorization middleware

**What goes wrong:** If JWT bearer auth is applied globally (e.g., `app.UseAuthorization()` before `app.MapHealthChecks()`), health check endpoints return 401.

**Why it happens:** Default middleware ordering or attribute-based authorization on all routes.

**How to avoid:** Allow anonymous access explicitly: `app.MapHealthChecks("/healthz/live").AllowAnonymous()` and `app.MapHealthChecks("/healthz/ready").AllowAnonymous()`.

**Warning signs:** `compose.yaml` reports API container unhealthy; Docker Compose health check loop cycles even after API starts.

---

### Pitfall 5: Alloy endpoint binding inside Docker network

**What goes wrong:** Alloy config uses `127.0.0.1:4317` — the API container cannot reach it because Alloy is a separate container. Only the host machine reaches `127.0.0.1`.

**Why it happens:** Copying Grafana documentation examples that use localhost (designed for same-machine deployments).

**How to avoid:** Always bind Alloy's OTLP receiver to `0.0.0.0:4317` in `config.alloy`. In `compose.yaml`, the API uses `OTEL_EXPORTER_OTLP_ENDPOINT: http://alloy:4317` (container DNS).

**Warning signs:** OTLP export errors in API logs (`Connection refused` to `127.0.0.1:4317`); no data appears in Grafana.

---

### Pitfall 6: Serilog `WithSpan()` requires an active OTel TracerProvider

**What goes wrong:** `WithSpan()` reads from `Activity.Current` which is populated by the OTel SDK. If Serilog is initialized before `AddOpenTelemetry()` registers the TracerProvider, `TraceId`/`SpanId` appear as `00000000...` in logs.

**Why it happens:** Bootstrap `Log.Logger` runs before `builder.Services.AddOpenTelemetry()`.

**How to avoid:** This is expected behavior for startup logs — there is no active trace yet. For request-scoped logs, the OTel SDK creates Activities during ASP.NET Core request handling (after middleware pipeline starts). Startup logs will correctly show zero trace IDs; this is acceptable.

**Warning signs:** All log entries show `TraceId: 00000000000000000000000000000000` — only a problem if it persists for HTTP request logs (not startup logs).

---

## Code Examples

### Full Program.cs observability wiring

```csharp
// Source: Serilog.AspNetCore + OTel .NET official docs
using System.Reflection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

// 1. Bootstrap logger (before builder) — captures startup errors
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithSpan()
    .Destructure.With<SensitiveDataDestructuringPolicy>()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 2. Replace with full logger (reads appsettings)
    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithSpan()
        .Destructure.With<SensitiveDataDestructuringPolicy>()
        .WriteTo.Console(new CompactJsonFormatter())
        .WriteTo.OpenTelemetry(opts =>
        {
            opts.Endpoint = ctx.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                ?? "http://localhost:4317";
            opts.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
            opts.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = ctx.Configuration["OTEL_SERVICE_NAME"] ?? "onboarding-api"
            };
        }));

    // 3. OpenTelemetry SDK — traces and metrics only (logs via Serilog OTLP sink)
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService(
            serviceName: builder.Configuration["OTEL_SERVICE_NAME"] ?? "onboarding-api"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(opts =>
                opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/healthz"))
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter());

    // 4. Health checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("AppDb")!,
            name: "postgresql", tags: new[] { "ready" })
        .AddUrlGroup(new Uri(builder.Configuration["Keycloak:RealmUrl"] + "/../health/ready"),
            name: "keycloak", tags: new[] { "ready" });

    builder.Services.AddControllers();

    var app = builder.Build();

    // 5. Request logging middleware — BEFORE routing
    app.UseSerilogRequestLogging();

    app.MapControllers();

    app.MapHealthChecks("/healthz/live", new HealthCheckOptions { Predicate = _ => false })
       .AllowAnonymous();

    app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
    {
        Predicate = c => c.Tags.Contains("ready"),
        ResponseWriter = WriteHealthCheckResponse
    }).AllowAnonymous();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

### compose.yaml services to add

```yaml
# Source: Grafana intro-to-mltp + official Alloy/Loki/Tempo/Mimir docs
# Add to existing compose.yaml

  alloy:
    image: grafana/alloy:latest
    volumes:
      - ./infra/alloy/config.alloy:/etc/alloy/config.alloy:ro
    ports:
      - "127.0.0.1:4317:4317"   # OTLP gRPC (API → Alloy)
      - "127.0.0.1:4318:4318"   # OTLP HTTP
      - "127.0.0.1:12345:12345" # Alloy UI
    command: run /etc/alloy/config.alloy
    depends_on:
      - loki
      - tempo
      - mimir

  loki:
    image: grafana/loki:3
    command: -config.file=/etc/loki/local-config.yaml
    ports:
      - "127.0.0.1:3100:3100"

  tempo:
    image: grafana/tempo:latest
    command: ["-config.file=/etc/tempo.yaml"]
    volumes:
      - ./infra/tempo/tempo.yaml:/etc/tempo.yaml:ro
    ports:
      - "127.0.0.1:3200:3200"   # Tempo HTTP
      # port 4317 internal only (Alloy → Tempo gRPC, no host binding needed)

  mimir:
    image: grafana/mimir:latest
    command: ["--config.file=/etc/mimir/mimir.yaml"]
    volumes:
      - ./infra/mimir/mimir.yaml:/etc/mimir/mimir.yaml:ro
    ports:
      - "127.0.0.1:9009:9009"

  grafana:
    image: grafana/grafana:latest
    environment:
      GF_AUTH_ANONYMOUS_ENABLED: "true"
      GF_AUTH_ANONYMOUS_ORG_ROLE: Admin
      GF_AUTH_DISABLE_LOGIN_FORM: "true"
    volumes:
      - ./infra/grafana/provisioning:/etc/grafana/provisioning:ro
    ports:
      - "127.0.0.1:3000:3000"
    depends_on:
      - loki
      - tempo
      - mimir
```

Also add `OTEL_EXPORTER_OTLP_ENDPOINT: http://alloy:4317` and `OTEL_SERVICE_NAME: onboarding-api` to the `api` service environment in `compose.yaml`.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Serilog.Enrichers.Span required for TraceId | Still the canonical package; note README says "deprecated" because native Serilog can now do this but `WithSpan()` is still the recommended one-liner | Serilog 4.x | Use `WithSpan()` — simpler than manual Activity access |
| OpenTelemetry Collector (contrib) | Grafana Alloy as OTLP collector | Alloy v1.0 (2024) | Alloy has native Loki/Tempo/Mimir exporters; simpler config; fewer containers |
| Grafana Agent (deprecated) | Grafana Alloy | 2024 (Agent sunset) | Do not use `grafana/agent` — use `grafana/alloy` |
| `config.river` file extension | `.alloy` file extension | Alloy v1.0 | River syntax renamed to "Alloy configuration language"; use `.alloy` extension |
| `otelcol.exporter.otlphttp` for Tempo | `otelcol.exporter.otlp` (gRPC) or `otlphttp` (HTTP) | Current | gRPC preferred for Alloy → Tempo; use `otelcol.exporter.otlp` with `insecure = true` locally |

**Deprecated/outdated:**
- `grafana/agent`: Sunset in favor of Alloy. Do not use in new setups.
- Serilog `config.river` extension: Any doc referencing `.river` files is from pre-Alloy-v1.0 era.
- `KEYCLOAK_ADMIN` / `KEYCLOAK_ADMIN_PASSWORD` env vars: Already confirmed deprecated in Keycloak 26.x (Phase 1 decision).

---

## Open Questions

1. **Tempo minimal config for local dev**
   - What we know: Tempo requires a config file (`tempo.yaml`) with storage backend configured; it cannot start with pure defaults if no config is mounted.
   - What's unclear: Minimal `tempo.yaml` for single-binary dev mode — whether `local` backend works out-of-the-box.
   - Recommendation: Use Grafana's own `tempo/tempo.yaml` from the `intro-to-mltp` repo as the base; it uses `local` backend which is sufficient for dev.

2. **Mimir minimal config for local dev**
   - What we know: Mimir in single-process mode requires a `mimir.yaml` with at least `target: all` and a storage backend.
   - What's unclear: Exact minimum config to prevent startup errors when running `grafana/mimir:latest` in a dev-only single-process mode.
   - Recommendation: Use `--config.file=/etc/mimir/mimir.yaml` with a minimal YAML that sets `target: all`, `memberlist: cluster_label: "dev"`, and `blocks_storage: backend: filesystem`.

3. **`AddNpgSql` before DbContext is registered (Phase 4 runs before Phase 5)**
   - What we know: `AspNetCore.HealthChecks.NpgSql` uses a raw connection string, not EF Core DbContext — it opens a direct connection to check availability.
   - What's unclear: Nothing blocking — raw connection string is available in `appsettings.json` already.
   - Recommendation: Use the raw connection string version; no dependency on EF Core DbContext registration.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Docker | Grafana stack services | ✓ | 29.2.1 | — |
| .NET SDK 10 | API compilation | ✓ | 10.0.201 | — |
| Internet (NuGet) | Package restore | ✓ | — | Local NuGet cache |
| Internet (Docker Hub) | Pull Grafana images | ✓ (assumed) | — | Pre-pull images manually |

No missing blocking dependencies.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 |
| Config file | Configured via `Onboarding.Domain.Tests.csproj` (no `xunit.runner.json` needed) |
| Quick run command | `dotnet test tests/Onboarding.Domain.Tests/ -x` |
| Full suite command | `dotnet test --no-restore` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| OBS-01 | Serilog JSON output contains TraceId and SpanId fields | unit | `dotnet test tests/ -x --filter "Category=Observability"` | ❌ Wave 0 |
| OBS-02 | OTel SDK traces cover ASP.NET Core, HttpClient, EF Core | smoke/manual | Visual verification in Grafana Tempo | manual only |
| OBS-03 | Runtime and ASP.NET Core metrics appear in Mimir | smoke/manual | Visual verification in Grafana via Mimir datasource | manual only |
| OBS-04 | `traceparent` header present on HttpClient calls to Keycloak | unit | `dotnet test tests/ -x --filter "Category=Observability"` | ❌ Wave 0 |
| OBS-05 | `/healthz/live` returns 200; `/healthz/ready` returns 200 or 503 | integration | `dotnet test tests/ -x --filter "Category=HealthCheck"` | ❌ Wave 0 |
| SEC-09 | Password/token fields appear as `[REDACTED]` in log output | unit | `dotnet test tests/ -x --filter "Category=Security"` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test tests/Onboarding.Domain.Tests/ -x`
- **Per wave merge:** `dotnet test --no-restore`
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `tests/Onboarding.API.Tests/Observability/SensitiveDataDestructuringPolicyTests.cs` — covers SEC-09, OBS-01 (log field masking + TraceId enrichment)
- [ ] `tests/Onboarding.API.Tests/HealthChecks/HealthCheckEndpointTests.cs` — covers OBS-05 (live/ready split with mocked checks)
- [ ] `tests/Onboarding.API.Tests/Observability/TracePropagationTests.cs` — covers OBS-04 (W3C traceparent on outbound calls)
- [ ] `tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj` — new test project targeting `net10.0`, referencing `Onboarding.API`

A new test project `Onboarding.API.Tests` is needed because existing tests (`Onboarding.Domain.Tests`) only reference Domain and Application layers — not the API.

---

## Project Constraints (from CLAUDE.md)

Directives the planner must verify compliance against:

| Directive | Impact on This Phase |
|-----------|---------------------|
| OSS-only packages (MIT/Apache 2.0) | All packages verified: Serilog (Apache 2.0), OTel (Apache 2.0), HealthChecks (Apache 2.0). No violations. |
| No MediatR | Not relevant to this phase. |
| No Moq — use NSubstitute | Tests must use `NSubstitute` for mocking `IHealthCheck`. |
| No FluentAssertions — use Shouldly | Tests must use `Shouldly` for assertions. |
| Controllers ASP.NET Core (no Minimal API) | Health check endpoints use `app.MapHealthChecks()` (not a controller). This is standard health check routing — no Minimal API concern. |
| Serilog + OpenTelemetry mandatory | Phase directly implements both. |
| All services in Docker Compose | New Grafana stack services are added to `compose.yaml`. |
| 127.0.0.1 loopback prefix on all host ports | New ports (3000, 4317, 4318, 9009, 3100, 3200, 12345) must use `127.0.0.1:` prefix. |

---

## Sources

### Primary (HIGH confidence)
- NuGet registry v3 flatcontainer API — all package versions verified directly (2026-04-02)
- [Serilog.AspNetCore GitHub README](https://github.com/serilog/serilog-aspnetcore) — `UseSerilog()`, `UseSerilogRequestLogging()` patterns
- [Serilog.Sinks.OpenTelemetry GitHub](https://github.com/serilog/serilog-sinks-opentelemetry) — `WriteTo.OpenTelemetry()` options, protocol config
- [Microsoft Learn: Health checks in ASP.NET Core 10.0](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0) — tagged liveness/readiness pattern, `ResponseWriter`
- [Grafana Alloy: Collect OTel data and forward to LGTM](https://grafana.com/docs/alloy/latest/collect/opentelemetry-to-lgtm-stack/) — `.alloy` config pipeline syntax
- [OTel EF Core README](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore/README.md) — beta status, caveats

### Secondary (MEDIUM confidence)
- [Grafana intro-to-mltp GitHub](https://github.com/grafana/intro-to-mltp) — Docker Compose structure, Alloy config patterns
- [xor22h.dev: Complete self-hosted observability stack](https://xor22h.dev/monitoring-applications-with-opentelemetry-grafana-alloy-loki-tempo-mimir-a-complete-self-hosted-observability-stack/) — config.alloy Loki/Tempo/Mimir routing, image tags
- [Serilog.Enrichers.Span GitHub](https://github.com/RehanSaeed/Serilog.Enrichers.Span) — `WithSpan()` usage, deprecation note

### Tertiary (LOW confidence)
- WebSearch snippets for Alloy image version tags — `grafana/alloy:latest` confirmed as recommended tag for local dev; specific version not pinned in research

---

## Metadata

**Confidence breakdown:**
- Standard stack (NuGet versions): HIGH — verified directly against NuGet registry
- Architecture patterns: HIGH — based on official Serilog and OTel .NET docs
- Alloy config syntax: MEDIUM — based on official Grafana Alloy docs and working examples
- Grafana Docker image versions: MEDIUM — `latest` tags confirmed; specific semver tags not pinned
- EF Core OTel instrumentation: MEDIUM — beta package, functional for PostgreSQL but API can change
- Health check patterns: HIGH — official Microsoft docs

**Research date:** 2026-04-02
**Valid until:** 2026-05-02 (stable ecosystem; Alloy/Grafana image tags should be re-verified if > 30 days)
