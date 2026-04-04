---
phase: 04-observability
verified: 2026-04-03T23:00:00Z
status: gaps_found
score: 6/7 must-haves verified
gaps:
  - truth: "GET /healthz/live returns HTTP 200 immediately without calling any external service"
    status: failed
    reason: "AddHealthChecks() and MapHealthChecks() are absent from Program.cs on master. Health check NuGet packages (AspNetCore.HealthChecks.NpgSql, AspNetCore.HealthChecks.Uris) are not in Onboarding.API.csproj. The production code was committed in worktree branch worktree-agent-acd23163 (commit 4dd8ef8) but never merged to master."
    artifacts:
      - path: "src/Onboarding.API/Program.cs"
        issue: "Missing AddHealthChecks(), AddNpgSql(), AddUrlGroup(), MapHealthChecks('/healthz/live'), MapHealthChecks('/healthz/ready'), and WriteDetailedJson. File on master is the Plan 04-01 version (Serilog+OTel only)."
      - path: "src/Onboarding.API/Onboarding.API.csproj"
        issue: "Missing PackageReference for AspNetCore.HealthChecks.NpgSql 9.0.0 and AspNetCore.HealthChecks.Uris 9.0.0."
      - path: "tests/Onboarding.API.Tests/HealthChecks/HealthCheckEndpointTests.cs"
        issue: "Still contains original RED stub tests (all 5 assert true.ShouldBeFalse(...)). The real integration tests from Plan 04-02 (HealthyApiFactory/UnhealthyApiFactory) exist only in worktree branch worktree-agent-acd23163."
    missing:
      - "Merge branch worktree-agent-acd23163 into master, OR cherry-pick commit 4dd8ef8 onto master"
      - "Verify dotnet test --filter Category=HealthCheck exits 0 after merge"
  - truth: "GET /healthz/ready returns HTTP 200 when PostgreSQL, Keycloak, disk, and memory checks pass"
    status: failed
    reason: "Same root cause: /healthz/ready endpoint not wired in Program.cs on master."
    artifacts:
      - path: "src/Onboarding.API/Program.cs"
        issue: "MapHealthChecks('/healthz/ready') absent."
    missing:
      - "Same fix as above: merge worktree-agent-acd23163 into master."
  - truth: "GET /healthz/ready returns HTTP 503 when a dependency check fails"
    status: failed
    reason: "Same root cause: endpoint absent on master."
    artifacts:
      - path: "src/Onboarding.API/Program.cs"
        issue: "No /healthz/ready endpoint registered."
    missing:
      - "Same fix as above."
  - truth: "GET /healthz/ready response body is JSON with per-check name, status, and duration"
    status: failed
    reason: "WriteDetailedJson response writer absent from Program.cs on master."
    artifacts:
      - path: "src/Onboarding.API/Program.cs"
        issue: "WriteDetailedJson static function absent."
    missing:
      - "Same fix as above."
  - truth: "Docker Compose healthcheck for the api service uses /healthz/live (fast, no I/O)"
    status: partial
    reason: "compose.yaml correctly references /healthz/live AND the api depends_on alloy:service_started. However the /healthz/live endpoint itself does not exist in the running binary because Program.cs is missing the health check registration. At runtime the healthcheck would return a non-200 response causing the api container to be marked unhealthy."
    artifacts:
      - path: "compose.yaml"
        issue: "Configuration is correct (healthz/live present, OTEL env vars set), but the endpoint it calls does not exist in the deployed binary."
    missing:
      - "Merge worktree-agent-acd23163 so the endpoint is available at runtime."
human_verification:
  - test: "End-to-end trace and log correlation in Grafana"
    expected: "After docker compose up, query {service_name='onboarding-api'} in Grafana Loki Explore and confirm JSON log entries contain TraceId and SpanId fields. Cross-link a trace in Tempo to the corresponding log entry."
    why_human: "Requires running Docker stack and browser interaction. Cannot verify OTLP data flow programmatically in the local repo."
  - test: "Grafana datasource connectivity"
    expected: "In Grafana (http://localhost:3000) Connections > Data Sources: Loki, Tempo, and Mimir should all show green status (no error)."
    why_human: "Requires running containers. The 04-03 human checkpoint confirmed this at time of execution, but cannot be re-verified statically."
---

# Phase 4: Observability Verification Report

**Phase Goal:** Deliver Serilog structured logging + OpenTelemetry traces/metrics + health checks + local Grafana visualization stack. Every HTTP request is traced end-to-end, sensitive data is masked in logs, and developers can see telemetry at http://localhost:3000 immediately after docker compose up.
**Verified:** 2026-04-03T23:00:00Z
**Status:** gaps_found
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Every HTTP request log entry contains TraceId and SpanId fields (OBS-01) | VERIFIED | Program.cs has `.Enrich.WithSpan()` in both bootstrap logger and UseSerilog(). Serilog.Enrichers.Span 3.1.0 installed. 9 Observability tests GREEN (2 E2E skipped with documented reason). |
| 2 | OpenTelemetry SDK instruments ASP.NET Core, HttpClient, and EF Core (OBS-02) | VERIFIED | Program.cs: `AddAspNetCoreInstrumentation()`, `AddHttpClientInstrumentation()`, `AddEntityFrameworkCoreInstrumentation()`, `AddOtlpExporter()` all present. Tempo backend live in compose.yaml. Human checkpoint passed. |
| 3 | Runtime and ASP.NET Core metrics exported via OTLP (OBS-03) | VERIFIED | Program.cs: `AddRuntimeInstrumentation()`, `AddAspNetCoreInstrumentation()` (metrics), `AddOtlpExporter()` all present. Mimir backend in compose.yaml with Prometheus datasource. Human checkpoint passed. |
| 4 | HttpClient calls automatically carry W3C traceparent header (OBS-04) | VERIFIED (structural) | `AddHttpClientInstrumentation()` registered in Program.cs — this is sufficient for automatic W3C propagation per OpenTelemetry .NET SDK behavior. E2E trace correlation tests are `[Fact(Skip=...)]` with documented VALIDATION.md reference. |
| 5 | Logging a DTO with 'password' field produces '[REDACTED]' in the log output (SEC-09) | VERIFIED | `SensitiveDataDestructuringPolicy` implemented and registered via `.Destructure.With<SensitiveDataDestructuringPolicy>()` in Program.cs. 9 unit tests GREEN including password, token, secret, client_secret, authorization, CPF, email masking. |
| 6 | GET /healthz/live returns HTTP 200 without calling any external service (OBS-05) | FAILED | `AddHealthChecks()` and `MapHealthChecks()` absent from `src/Onboarding.API/Program.cs` on master. Health check NuGet packages absent from csproj. 5 HealthCheck tests are RED stubs. The implementation was committed to worktree branch `worktree-agent-acd23163` (commit `4dd8ef8`) but was never merged into master. |
| 7 | Grafana UI accessible at http://localhost:3000 with Loki, Tempo, Mimir datasources pre-configured | VERIFIED | All 5 observability services in compose.yaml (alloy, loki, tempo, mimir, grafana). `infra/grafana/provisioning/datasources/datasources.yaml` contains all three datasources. Human checkpoint in 04-03-SUMMARY confirms Grafana shows all datasources correctly. |

**Score:** 6/7 truths verified (OBS-05 health checks blocked)

---

### Required Artifacts

| Artifact | Expected | Exists | Substantive | Wired | Status |
|----------|----------|--------|-------------|-------|--------|
| `src/Onboarding.API/Observability/SensitiveDataDestructuringPolicy.cs` | IDestructuringPolicy masking sensitive fields | Yes | Yes (104 lines, full implementation) | Yes (Program.cs `.Destructure.With<SensitiveDataDestructuringPolicy>()`) | VERIFIED |
| `src/Onboarding.API/Program.cs` | Serilog + OTel wiring, UseSerilog, AddOpenTelemetry | Yes | Yes (Serilog+OTel portion) | Yes | PARTIAL — missing health check sections |
| `src/Onboarding.API/appsettings.json` | Serilog MinimumLevel config section | Yes | Yes (`"Serilog": { "MinimumLevel": {...} }`) | Yes | VERIFIED |
| `src/Onboarding.API/appsettings.Development.json` | Debug level override | Yes | Yes (`"Default": "Debug"`) | Yes | VERIFIED |
| `src/Onboarding.API/Onboarding.API.csproj` | All observability NuGet packages | Yes | PARTIAL — missing AspNetCore.HealthChecks.NpgSql and AspNetCore.HealthChecks.Uris | n/a | STUB (missing packages) |
| `infra/alloy/config.alloy` | OTLP receiver routing to Loki/Tempo/Mimir | Yes | Yes (57 lines, full pipeline) | Yes (compose.yaml volume mount) | VERIFIED |
| `infra/grafana/provisioning/datasources/datasources.yaml` | Loki, Tempo, Mimir datasources | Yes | Yes (28 lines, all 3 datasources) | Yes (compose.yaml volume mount) | VERIFIED |
| `infra/tempo/tempo.yaml` | Tempo single-binary config | Yes | Yes | Yes | VERIFIED |
| `infra/loki/loki-config.yaml` | Loki single-process config | Yes | Yes | Yes | VERIFIED |
| `infra/mimir/mimir.yaml` | Mimir monolithic config | Yes | Yes (target: all, filesystem backend) | Yes | VERIFIED |
| `compose.yaml` | All 5 observability stack services | Yes | Yes (alloy, loki, tempo, mimir, grafana) | Yes (OTEL_EXPORTER_OTLP_ENDPOINT: http://alloy:4317 on api) | VERIFIED |
| `tests/Onboarding.API.Tests/HealthChecks/HealthCheckEndpointTests.cs` | 5 GREEN integration tests for health endpoints | Yes | STUB — still contains original RED stubs (all 5 assert `true.ShouldBeFalse(...)`) | n/a | STUB |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `Program.cs` | `SensitiveDataDestructuringPolicy` | `.Destructure.With<SensitiveDataDestructuringPolicy>()` | WIRED | Pattern present on line 17 (bootstrap) and line 31 (UseSerilog) |
| `Program.cs` | OTLP exporter | `AddOtlpExporter()` | WIRED | Present in both `.WithTracing()` and `.WithMetrics()` blocks |
| `Program.cs` | `Enrich.WithSpan()` | Serilog.Enrichers.Span | WIRED | Present on lines 16 and 30 |
| `Program.cs` | `AddHealthChecks()` | Health check registration | NOT WIRED | Absent from Program.cs on master |
| `Program.cs` | `/healthz/live` endpoint | `MapHealthChecks("/healthz/live")` | NOT WIRED | Absent from Program.cs on master |
| `Program.cs` | `/healthz/ready` endpoint | `MapHealthChecks("/healthz/ready")` | NOT WIRED | Absent from Program.cs on master |
| `api service (compose.yaml)` | `alloy service` | `OTEL_EXPORTER_OTLP_ENDPOINT: http://alloy:4317` | WIRED | Present in compose.yaml api environment block |
| `infra/alloy/config.alloy` | `loki service` | `loki.write endpoint http://loki:3100/loki/api/v1/push` | WIRED | Pattern `loki:3100` present |
| `infra/alloy/config.alloy` | `tempo service` | `otelcol.exporter.otlp tempo http://tempo:4317` | WIRED | Pattern `tempo:4317` present |
| `infra/alloy/config.alloy` | `mimir service` | `prometheus.remote_write http://mimir:9009/api/v1/push` | WIRED | Pattern `mimir:9009` present |

---

### Data-Flow Trace (Level 4)

This phase produces infrastructure configuration, not data-rendering components. Level 4 data-flow analysis is not applicable to log exporters and OTLP pipelines. The data flow is verified at Level 3 (wiring) through OTLP endpoint configuration and human checkpoint confirmation.

---

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Security tests GREEN | `dotnet test tests/Onboarding.API.Tests/ --filter "Category=Security"` | 9 passed, 0 failed | PASS |
| Observability tests GREEN (non-E2E) | `dotnet test tests/Onboarding.API.Tests/ --filter "Category=Observability"` | 9 passed, 0 failed, 2 skipped | PASS |
| HealthCheck tests RED (unmerged code) | `dotnet test tests/Onboarding.API.Tests/ --filter "Category=HealthCheck"` | 0 passed, 5 failed | FAIL |
| Grafana stack services in compose.yaml | `grep -c "grafana/alloy\|grafana/loki\|grafana/tempo\|grafana/mimir\|grafana/grafana" compose.yaml` | 5 | PASS |
| Alloy routes to all backends | grep loki:3100, tempo:4317, mimir:9009 in config.alloy | All 3 patterns found | PASS |
| Program.cs has Serilog+OTel wiring | grep UseSerilog/AddOpenTelemetry/WithSpan/CloseAndFlush | All 4 found | PASS |
| Program.cs missing health check wiring | grep AddHealthChecks/MapHealthChecks in Program.cs | 0 matches | FAIL |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| OBS-01 | 04-01 | Serilog structured logging (JSON) with TraceId/SpanId automáticos | SATISFIED | `Enrich.WithSpan()` in Program.cs; 9 tests GREEN; JSON console via CompactJsonFormatter |
| OBS-02 | 04-01, 04-03 | OpenTelemetry traces instrumentando ASP.NET Core, HttpClient, EF Core | SATISFIED | AddAspNetCoreInstrumentation, AddHttpClientInstrumentation, AddEntityFrameworkCoreInstrumentation in Program.cs; Tempo backend in compose.yaml; human checkpoint confirmed |
| OBS-03 | 04-01, 04-03 | OpenTelemetry metrics (runtime + ASP.NET Core) | SATISFIED | AddRuntimeInstrumentation + AddAspNetCoreInstrumentation (metrics) in Program.cs; Mimir backend in compose.yaml; human checkpoint confirmed |
| OBS-04 | 04-01 | Correlation ID propagado em chamadas ao Keycloak Admin API | SATISFIED (structural) | AddHttpClientInstrumentation() enables automatic W3C traceparent propagation; TracePropagation tests are Skip-annotated with documented VALIDATION.md rationale |
| OBS-05 | 04-02 | Health check endpoints (/healthz) para API e Keycloak | BLOCKED | /healthz/live and /healthz/ready endpoints absent from Program.cs on master. Implementation commit 4dd8ef8 stranded in worktree branch worktree-agent-acd23163, never merged. compose.yaml references the endpoint correctly but the endpoint does not exist at runtime. |
| SEC-09 | 04-00, 04-01 | Log masking para dados sensíveis | SATISFIED | SensitiveDataDestructuringPolicy implements all masking rules; registered globally via `.Destructure.With<>()` in Program.cs; 9 unit tests GREEN |

**Note on OBS-01 vs compose.yaml:** The REQUIREMENTS.md also maps INFRA-04 (healthchecks on all services) to Phase 1 as complete. Phase 4 introduces /healthz/live and /healthz/ready for the api service specifically, which compose.yaml references — but the endpoint code must exist in the binary. The api service healthcheck in compose.yaml calling a non-existent endpoint means the api container would be marked unhealthy in practice.

---

### Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|----------|--------|
| `tests/Onboarding.API.Tests/HealthChecks/HealthCheckEndpointTests.cs` | All 5 test methods assert `true.ShouldBeFalse("Production code not yet implemented...")` — original RED stubs | Blocker | The real integration test code (HealthyApiFactory, UnhealthyApiFactory) exists only in worktree branch `worktree-agent-acd23163`. OBS-05 goal is not demonstrably achieved. |
| `src/Onboarding.API/appsettings.json` | `"AppDb": ""` — empty connection string | Warning | Acceptable for dev since Program.cs has a fallback placeholder. Not a blocker. |

---

### Human Verification Required

#### 1. End-to-End Trace and Log Correlation in Grafana

**Test:** After `docker compose up`, make a request to any API endpoint, then in Grafana at http://localhost:3000 open Explore > Loki, query `{service_name="onboarding-api"}`, and verify log entries contain `TraceId` and `SpanId` fields. Then in Explore > Tempo, search for the corresponding trace and verify the trace links back to the Loki log.
**Expected:** JSON log entries with `TraceId` and `SpanId` fields visible in Loki. Trace spans visible in Tempo. Tempo-to-Logs link works.
**Why human:** Requires running Docker stack and live OTLP telemetry flow. Cannot verify data transmission programmatically from the repo.

#### 2. Grafana Datasource Connectivity

**Test:** Open http://localhost:3000 > Connections > Data Sources. Click Test on each of Loki, Tempo, and Mimir.
**Expected:** All three datasources show green/OK status with no connection errors.
**Why human:** Requires running containers. The 04-03 human checkpoint confirmed this at time of execution, but cannot be re-verified statically.

---

### Gaps Summary

**One root cause, five failing truths:** The Plan 04-02 work (health check endpoints) was executed correctly in a git worktree (branch `worktree-agent-acd23163`, commit `4dd8ef8`), but that commit was never merged into the `master` branch.

As a result, on `master`:
- `src/Onboarding.API/Program.cs` does not contain `AddHealthChecks()`, `MapHealthChecks()`, or `WriteDetailedJson`
- `src/Onboarding.API/Onboarding.API.csproj` does not contain `AspNetCore.HealthChecks.NpgSql` or `AspNetCore.HealthChecks.Uris`
- `tests/Onboarding.API.Tests/HealthChecks/HealthCheckEndpointTests.cs` still has RED stub tests (5/5 failing)
- The api service healthcheck in `compose.yaml` calls `http://localhost:8080/healthz/live` but that endpoint does not exist in the compiled binary

**Resolution path:** Merge branch `worktree-agent-acd23163` into `master`, or cherry-pick commit `4dd8ef8` onto master. Then run `dotnet test tests/Onboarding.API.Tests/ --filter "Category=HealthCheck"` to confirm 5/5 GREEN.

All other phase 4 goals (OBS-01, OBS-02, OBS-03, OBS-04, SEC-09, Grafana stack) are fully achieved and verified.

---

_Verified: 2026-04-03T23:00:00Z_
_Verifier: Claude (gsd-verifier)_
