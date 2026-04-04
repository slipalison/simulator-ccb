---
phase: 3
slug: backend-domain-layer
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-04-02
---

# Phase 3 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.x + Shouldly 4.x |
| **Config file** | tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj (Wave 0 creates) |
| **Quick run command** | `dotnet test tests/Onboarding.Domain.Tests/ --filter "Category=Unit" --no-build` |
| **Full suite command** | `dotnet test tests/Onboarding.Domain.Tests/ --no-build` |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/Onboarding.Domain.Tests/ --filter "Category=Unit" --no-build`
- **After every plan wave:** Run `dotnet test tests/Onboarding.Domain.Tests/ --no-build`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 10 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 03-01-01 | 01 | 0 | BACK-01 | infra | `dotnet build tests/Onboarding.Domain.Tests/` | ❌ W0 | ⬜ pending |
| 03-01-02 | 01 | 1 | BACK-01 | unit | `dotnet test tests/Onboarding.Domain.Tests/ --filter "CpfTests"` | ❌ W0 | ⬜ pending |
| 03-01-03 | 01 | 1 | BACK-01 | unit | `dotnet test tests/Onboarding.Domain.Tests/ --filter "CnpjTests"` | ❌ W0 | ⬜ pending |
| 03-01-04 | 01 | 1 | BACK-02 | unit | `dotnet test tests/Onboarding.Domain.Tests/ --filter "ClientTests"` | ❌ W0 | ⬜ pending |
| 03-01-05 | 01 | 2 | BACK-03 | unit | `dotnet test tests/Onboarding.Domain.Tests/ --filter "RegisterCommandTests"` | ❌ W0 | ⬜ pending |
| 03-01-06 | 01 | 2 | BACK-04 | unit | `dotnet test tests/Onboarding.Domain.Tests/ --filter "HandlerTests"` | ❌ W0 | ⬜ pending |
| 03-01-07 | 01 | 3 | BACK-06 | build | `dotnet build src/Onboarding.Domain/` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj` — xUnit test project referencing Domain + Application
- [ ] `tests/Onboarding.Domain.Tests/ValueObjects/CpfTests.cs` — stubs for CPF validation (BACK-01)
- [ ] `tests/Onboarding.Domain.Tests/ValueObjects/CnpjTests.cs` — stubs for CNPJ validation (BACK-01)
- [ ] `tests/Onboarding.Domain.Tests/Aggregates/ClientTests.cs` — stubs for Client aggregate (BACK-02)
- [ ] `tests/Onboarding.Domain.Tests/Application/Commands/RegisterClientCommandHandlerTests.cs` — stubs for CQRS handler (BACK-03, BACK-04)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| DDD layer boundaries enforced (Domain has no infra refs) | BACK-01 | Dependency inspection | Run `grep "ProjectReference" src/Onboarding.Application/Onboarding.Application.csproj` and confirm only Onboarding.Domain is referenced — no Infrastructure |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 10s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
