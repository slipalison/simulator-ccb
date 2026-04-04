---
phase: 04
slug: observability
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-03
---

# Phase 04 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 |
| **Config file** | `tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj` (❌ Wave 0) |
| **Quick run command** | `dotnet test tests/Onboarding.Domain.Tests/ -x` |
| **Full suite command** | `dotnet test --no-restore` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/Onboarding.Domain.Tests/ -x`
- **After every plan wave:** Run `dotnet test --no-restore`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 04-01-01 | 01 | 0 | OBS-01, SEC-09, OBS-04 | unit | `dotnet test tests/ -x --filter "Category=Observability|Category=Security"` | ❌ W0 | ⬜ pending |
| 04-01-02 | 01 | 0 | OBS-05 | integration | `dotnet test tests/ -x --filter "Category=HealthCheck"` | ❌ W0 | ⬜ pending |
| 04-01-03 | 01 | 1 | OBS-01 | unit | `dotnet test tests/ -x --filter "Category=Observability"` | ❌ W0 | ⬜ pending |
| 04-01-04 | 01 | 1 | SEC-09 | unit | `dotnet test tests/ -x --filter "Category=Security"` | ❌ W0 | ⬜ pending |
| 04-01-05 | 01 | 1 | OBS-04 | unit | `dotnet test tests/ -x --filter "Category=Observability"` | ❌ W0 | ⬜ pending |
| 04-01-06 | 01 | 1 | OBS-05 | integration | `dotnet test tests/ -x --filter "Category=HealthCheck"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj` — novo projeto de testes targeting `net10.0`, referenciando `Onboarding.API`
- [ ] `tests/Onboarding.API.Tests/Observability/SensitiveDataDestructuringPolicyTests.cs` — cobre SEC-09 e OBS-01 (masking de campos sensíveis + enriquecimento TraceId)
- [ ] `tests/Onboarding.API.Tests/HealthChecks/HealthCheckEndpointTests.cs` — cobre OBS-05 (split live/ready com checks mockados via NSubstitute)
- [ ] `tests/Onboarding.API.Tests/Observability/TracePropagationTests.cs` — cobre OBS-04 (W3C traceparent em chamadas outbound)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| OTel SDK traces cobrem ASP.NET Core, HttpClient, EF Core | OBS-02 | Requer Grafana Tempo rodando via Docker Compose — não testável via xUnit | Fazer request à API com `compose up`, abrir Grafana (localhost:3000), verificar trace com spans de ASP.NET Core + EF Core + HttpClient |
| Métricas runtime e ASP.NET Core aparecem no Mimir | OBS-03 | Requer Grafana Mimir rodando — não testável via xUnit | Abrir Grafana (localhost:3000), datasource Mimir, verificar métricas `process_runtime_*` e `http_server_*` |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
