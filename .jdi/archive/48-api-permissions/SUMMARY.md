# Phase 48 — API + Permissions for Fundos Module — Execution Summary

## Status
Executed via /jdi-loop iter 1 — all 7 tasks completed.

## Tasks completed

| Task | Specialist | Commit | Notes |
|---|---|---|---|
| T-48.1 | security | `e8a680c` | PermissionPolicies.FundRead/Write/Delete/Manage constants + 7 unit tests |
| T-48.2 | backend | `b0aee52` | 4 policies registered in Program.cs + GlobalExceptionHandler maps DuplicateEntity→409, InvalidStateTransition→400 |
| T-48.3 | backend | `630f418` | AccessGroup CreateDefaultGroups extended (admin-empresa via Perm.All, viewer adds FundsRead) |
| T-48.4 | backend | `9163a52` | FundosController created — 12 endpoints (ConsultoriaFundo + Custodiante + TipoAtivo CRUD) + 49 unit tests |
| T-48.5 | backend | `335881a` | FundosController extended — 5 Fundo + 5 Cedente endpoints + state-machine endpoint (D-9 minimal body) + 61 unit tests |
| T-48.6 | backend | `24ad7c8` | AdminFundosController + 4 admin queries + handlers (IgnoreQueryFilters + Company join) + 14 unit tests |
| T-48.7 | backend | `6b37fa9` | Integration smoke (Testcontainers PostgreSQL) — 10 scenarios incl multi-tenant isolation, 401/403 gates, state machine 400, admin cross-company |

## Files modified
- `src/Onboarding.API/Security/PermissionPolicyConstants.cs` (T-48.1)
- `src/Onboarding.API/Program.cs` (T-48.2)
- `src/Onboarding.API/Middleware/GlobalExceptionHandler.cs` (T-48.2)
- `src/Onboarding.Domain/Aggregates/EmployeeAggregate/AccessGroup.cs` (T-48.3)
- `src/Onboarding.API/Controllers/FundosController.cs` (T-48.4 create + T-48.5 extend → 22 endpoints total)
- `src/Onboarding.API/Controllers/AdminFundosController.cs` (T-48.6)
- `src/Onboarding.Application/Fundos/Queries/Admin/` (T-48.6 — 4 queries + 4 DTOs)
- `src/Onboarding.Infrastructure/Repositories/FundosAdminQueryHandlers.cs` (T-48.6)
- `src/Onboarding.Application/DependencyInjection.cs` (T-48.6 add → T-48.7 reverted, DDD layering fix)
- `src/Onboarding.Infrastructure/DependencyInjection.cs` (T-48.7 — 4 admin handler registrations relocated to correct layer)
- `tests/Onboarding.API.Tests/Security/PermissionPolicyConstantsTests.cs` (T-48.1)
- `tests/Onboarding.API.Tests/Middleware/GlobalExceptionHandlerTests.cs` (T-48.2 extend)
- `tests/Onboarding.Domain.Tests/Aggregates/AccessGroupTests.cs` (T-48.3)
- `tests/Onboarding.API.Tests/Controllers/FundosControllerTests.cs` (T-48.4 + T-48.5 — 244 tests)
- `tests/Onboarding.API.Tests/Controllers/AdminFundosControllerTests.cs` (T-48.6)
- `tests/Onboarding.Integration.Tests/Fundos/FundosControllerIntegrationTests.cs` (T-48.7)

## Test results
- Onboarding.API.Tests: 244 passed, 0 failed, 4 skipped (pre-existing OTel/integration skips)
- Onboarding.Domain.Tests: 378 passed, 0 failed
- Onboarding.Application.Tests: 85 passed, 0 failed
- Onboarding.Integration.Tests: 12 passed, 0 failed (Testcontainers)

## Notable design decisions
- D-7: started from clean state after `0e73aee` discard
- D-8: AdminFundosController = read-only, list-only (no detail-by-id, no mutation)
- D-9: status endpoint body = `{ NewStatus }` only
- D-10: Cedente uniqueness already at infrastructure level — no DB work
- DDD fix discovered in T-48.7: admin handler registrations belong in Infrastructure.DependencyInjection (Application must not reference Infrastructure types)

## iter=2 — Security fix: cross-tenant leak on GET-by-id endpoints (2026-05-16)

Security review iter 1 identified a BLOCKER: all 4 GET-by-id controller actions in `FundosController` called repository methods using `IgnoreQueryFilters()`, which bypassed EF Core's HasQueryFilter tenant isolation. A company-B user could supply a company-A entity GUID and receive a 200 response with the entity's data.

Fix strategy: controller-side tenant check after fetch, chosen because admin paths do not share these repository methods (verified by grep — `FundosAdminQueryHandlers` uses its own paginated cross-company queries). Adding `if (entity is null || entity.ClienteId != _currentCompanyService.CompanyId) return NotFound()` to the 4 public GET-by-id actions restores isolation without altering the repository contract or breaking admin paths. `NotFound` is returned rather than `Forbid` to avoid leaking entity existence across tenant boundaries.

Four new integration test scenarios (9–12) were added to `FundosControllerIntegrationTests.cs` to assert each endpoint returns 404 when company-B authenticates and requests a company-A entity GUID. All 16 integration tests pass (12 existing + 4 new). All 244 API unit tests remain green.

Files changed:
- `src/Onboarding.API/Controllers/FundosController.cs` — tenant check on GetConsultoriaById, GetCustodianteById, GetFundoById, GetCedenteById
- `tests/Onboarding.Integration.Tests/Fundos/FundosControllerIntegrationTests.cs` — 4 new cross-tenant GET-by-id scenarios

## Next
/jdi-verify 48 (ralph loop step B — reviewer dispatch)
