---
phase: 3
slug: backend-domain-layer
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-02
---

# Phase 3 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.x + Shouldly 4.x |
| **Config file** | src/Onboarding.Tests/Onboarding.Tests.csproj (Wave 0 creates) |
| **Quick run command** | `dotnet test src/Onboarding.Tests/ --filter "Category=Unit" --no-build` |
| **Full suite command** | `dotnet test src/Onboarding.Tests/ --no-build` |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test src/Onboarding.Tests/ --filter "Category=Unit" --no-build`
- **After every plan wave:** Run `dotnet test src/Onboarding.Tests/ --no-build`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 10 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 03-01-01 | 01 | 0 | BACK-01 | infra | `dotnet build src/Onboarding.Tests/` | ❌ W0 | ⬜ pending |
| 03-01-02 | 01 | 1 | BACK-01 | unit | `dotnet test --filter "CpfTests"` | ❌ W0 | ⬜ pending |
| 03-01-03 | 01 | 1 | BACK-01 | unit | `dotnet test --filter "CnpjTests"` | ❌ W0 | ⬜ pending |
| 03-01-04 | 01 | 1 | BACK-02 | unit | `dotnet test --filter "ClientTests"` | ❌ W0 | ⬜ pending |
| 03-01-05 | 01 | 2 | BACK-03 | unit | `dotnet test --filter "RegisterCommandTests"` | ❌ W0 | ⬜ pending |
| 03-01-06 | 01 | 2 | BACK-04 | unit | `dotnet test --filter "HandlerTests"` | ❌ W0 | ⬜ pending |
| 03-01-07 | 01 | 3 | BACK-06 | build | `dotnet build src/Onboarding.Domain/` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `src/Onboarding.Tests/Onboarding.Tests.csproj` — xUnit test project referencing Domain + Application
- [ ] `src/Onboarding.Tests/Domain/ValueObjects/CpfTests.cs` — stubs for CPF validation (BACK-01)
- [ ] `src/Onboarding.Tests/Domain/ValueObjects/CnpjTests.cs` — stubs for CNPJ validation (BACK-01)
- [ ] `src/Onboarding.Tests/Domain/Aggregates/ClientTests.cs` — stubs for Client aggregate (BACK-02)
- [ ] `src/Onboarding.Tests/Application/Commands/RegisterClientCommandHandlerTests.cs` — stubs for CQRS handler (BACK-03, BACK-04)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| DDD layer boundaries enforced (Domain has no infra refs) | BACK-06 | Dependency inspection | Run `dotnet build` and confirm Onboarding.Domain.csproj has no refs to Application/Infrastructure/Persistence packages |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
