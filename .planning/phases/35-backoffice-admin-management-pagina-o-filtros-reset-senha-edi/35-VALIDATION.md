---
phase: 35
slug: backoffice-admin-management-pagina-o-filtros-reset-senha-edi
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-21
---

# Phase 35 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit / .NET test |
| **Config file** | none — using established .NET solution |
| **Quick run command** | `dotnet test src/Onboarding.Domain.Tests` / `dotnet test src/Onboarding.Application.Tests` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test src/Onboarding.Application.Tests`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 35-01-01 | 01 | 1 | MGMT-01, MGMT-02 | SEC-02 | 403 for non-admin callers | unit/integration | `dotnet test src/Onboarding.Application.Tests --filter "GetPaginatedUsersQuery"` | ❌ W0 | ⬜ pending |
| 35-01-02 | 01 | 1 | MGMT-03 | SEC-01, SEC-04 | Validate uniqueness, block own edit | unit | `dotnet test src/Onboarding.Application.Tests --filter "UpdateAdministratorCommand"` | ❌ W0 | ⬜ pending |
| 35-01-03 | 01 | 2 | MGMT-04 | SEC-01, SEC-03 | Crypto pass, block own reset | unit | `dotnet test src/Onboarding.Application.Tests --filter "ResetAdministratorPasswordCommand"` | ❌ W0 | ⬜ pending |
| 35-01-04 | 01 | 2 | MGMT-05, MGMT-06 | SEC-01, SEC-05 | Block own block, force logout | unit | `dotnet test src/Onboarding.Application.Tests --filter "(Deactivate|Reactivate)AdministratorCommand"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Onboarding.Application.Tests/Admin/GetPaginatedAdministratorsQueryTests.cs`
- [ ] `tests/Onboarding.Application.Tests/Admin/UpdateAdministratorCommandTests.cs`
- [ ] `tests/Onboarding.Application.Tests/Admin/ResetAdministratorPasswordCommandTests.cs`
- [ ] `tests/Onboarding.Application.Tests/Admin/DeactivateAdministratorCommandTests.cs`
- [ ] `tests/Onboarding.Application.Tests/Admin/ReactivateAdministratorCommandTests.cs`

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Keycloak logoutAll | SEC-01 | Keycloak behavior | Ensure user session is actually destroyed in Keycloak admin console |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
