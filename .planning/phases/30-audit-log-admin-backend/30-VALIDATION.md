---
phase: 30
slug: audit-log-admin-backend
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-15
---

# Phase 30 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.x + Shouldly 4.x + Testcontainers 4.x |
| **Config file** | `tests/Onboarding.Tests/Onboarding.Tests.csproj` |
| **Quick run command** | `dotnet test tests/Onboarding.Tests --filter "Category=Unit" --no-build` |
| **Full suite command** | `dotnet test tests/Onboarding.Tests` |
| **Estimated runtime** | ~45 seconds (unit) / ~120 seconds (integration com Testcontainers) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/Onboarding.Tests --filter "Category=Unit" --no-build`
- **After every plan wave:** Run `dotnet test tests/Onboarding.Tests`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 120 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 30-01-01 | 01 | 0 | AUD-01 | — | N/A | grep | `grep -r "IAuditService" src/Onboarding.Application/` | ✅ W0 | ⬜ pending |
| 30-01-02 | 01 | 1 | AUD-01 | — | Audit gravado com RecordAsync | unit | `dotnet test --filter "AuditServiceTests"` | ❌ W0 | ⬜ pending |
| 30-01-03 | 01 | 1 | AUD-01 | T-30-01 | Handlers usam IAuditService, não IAuditLogRepository | unit | `dotnet test --filter "BlockUserCommandHandlerTests"` | ❌ W0 | ⬜ pending |
| 30-01-04 | 01 | 2 | AUD-01 | — | Entidade AuditLog removida; build compila | build | `dotnet build src/Onboarding.API` | ✅ | ⬜ pending |
| 30-02-01 | 02 | 1 | ADM-03 | T-30-02 | GET /administrators exige role admin | integration | `dotnet test --filter "GetAdministratorsTests"` | ❌ W0 | ⬜ pending |
| 30-02-02 | 02 | 1 | ADM-01 | T-30-02 | POST /administrators cria admin no Keycloak | integration | `dotnet test --filter "CreateAdminTests"` | ❌ W0 | ⬜ pending |
| 30-02-03 | 02 | 1 | ADM-04 | T-30-02 | Non-admin recebe 403 em ambos endpoints | integration | `dotnet test --filter "AdminAuthorizationTests"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/Onboarding.Tests/Admin/AuditServiceTests.cs` — unit tests para IAuditService/AuditService
- [ ] `tests/Onboarding.Tests/Admin/GetAdministratorsTests.cs` — integration tests para GET /api/admin/administrators
- [ ] Atualizar `tests/Onboarding.Tests/Admin/AdminFullFlowTests.cs` — migrar asserções de `AuditLogRepositoryMock` para `AuditServiceMock`

*Infraestrutura xUnit + Testcontainers já instalada — apenas criar arquivos de teste.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| HasTemporaryPassword=true imediatamente após criar admin | ADM-02 | Requer inspeção do estado Keycloak em tempo real | 1. POST /api/admin/administrators; 2. GET /api/admin/administrators; 3. Verificar HasTemporaryPassword=true na resposta |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
