---
phase: 5
slug: registration-api
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-04
---

# Phase 5 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 + Shouldly 4.3.0 + NSubstitute 5.3.0 |
| **Existing test projects** | `tests/Onboarding.API.Tests/`, `tests/Onboarding.Domain.Tests/` |
| **New test project** | `tests/Onboarding.Integration.Tests/` (Testcontainers) |
| **Quick run command** | `dotnet test tests/Onboarding.Domain.Tests/ tests/Onboarding.API.Tests/` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30 seconds (quick) / ~90 seconds (full, with Testcontainers) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/Onboarding.Domain.Tests/ tests/Onboarding.API.Tests/`
- **After every plan wave:** Run `dotnet test` (all projects including Onboarding.Integration.Tests)
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds (quick), 90 seconds (full)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 5-01-01 | 01 | 0 | REG-03,REG-04,REG-05,REG-06,REG-08,BACK-05,SEC-08 | unit/integration stubs | `dotnet test tests/Onboarding.API.Tests/ --filter "Registration"` | ❌ W0 | ⬜ pending |
| 5-02-01 | 02 | 1 | REG-03, REG-04 | Integration (WebApplicationFactory) | `dotnet test tests/Onboarding.API.Tests/ --filter "Registration"` | ❌ W0 | ⬜ pending |
| 5-02-02 | 02 | 1 | REG-05 | Integration (WebApplicationFactory) | same | ❌ W0 | ⬜ pending |
| 5-02-03 | 02 | 1 | SEC-08 | Integration (WebApplicationFactory) | same | ❌ W0 | ⬜ pending |
| 5-03-01 | 03 | 2 | REG-06 | Integration (Testcontainers) | `dotnet test tests/Onboarding.Integration.Tests/` | ❌ W0 | ⬜ pending |
| 5-03-02 | 03 | 2 | REG-06 (compensation) | Unit (NSubstitute) | `dotnet test tests/Onboarding.Domain.Tests/ --filter "CommandHandler"` | ❌ W0 | ⬜ pending |
| 5-04-01 | 04 | 3 | REG-08 | Integration (WebApplicationFactory) | `dotnet test tests/Onboarding.API.Tests/ --filter "Idempotency"` | ❌ W0 | ⬜ pending |
| 5-04-02 | 04 | 3 | BACK-05 | Integration (WebApplicationFactory) | `dotnet test tests/Onboarding.API.Tests/ --filter "Registration"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Onboarding.API.Tests/Registration/RegistrationControllerTests.cs` — stubs for REG-03, REG-04, REG-05, REG-08, BACK-05, SEC-08 (WebApplicationFactory)
- [ ] `tests/Onboarding.API.Tests/Registration/IdempotencyFilterTests.cs` — stubs for idempotency filter unit tests
- [ ] `tests/Onboarding.Domain.Tests/Application/Commands/RegisterClientCommandHandlerTests.cs` — extend with compensation path + duplicate detection stubs
- [ ] `tests/Onboarding.Integration.Tests/Onboarding.Integration.Tests.csproj` — new xUnit project with Testcontainers.Keycloak + Testcontainers.PostgreSql
- [ ] `tests/Onboarding.Integration.Tests/Registration/RegistrationIntegrationTests.cs` — stubs for REG-06 end-to-end

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Brute force lockout visible end-to-end | SEC-08 | Requires live Keycloak container with clock, not practical in unit test | 1. POST valid registration; 2. Attempt login 5x with wrong password; 3. Verify 429/locked response |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 90s (full suite)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
