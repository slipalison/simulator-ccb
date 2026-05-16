---
phase: 48
iter: 2
total_resets: 0
status: running
max_iter_per_round: 5
max_resets: 3
created_at: 2026-05-12T00:00:00Z
---

## History

### iter=1 — T-48.1 (2026-05-12)
- Status: completed
- Commit: e8a680c
- Tests: 132 passed, 4 skipped, 0 failed (Onboarding.API.Tests)
- Notes: Added FundRead/FundWrite/FundDelete/FundManage to PermissionPolicies. Created PermissionPolicyConstantsTests.cs (6 tests). Build 0 errors, 0 warnings.

### iter=1 — T-48.3 (2026-05-12)
- Status: completed
- Commit: 630f418
- Tests: 378 passed (AccessGroupTests 10/10)
- Notes: viewer got FundsRead (2→3 perms); admin-empresa already had FundsManage via Perm.All; added explicit test for funds:manage inclusion.

### iter=1 — T-48.4 (2026-05-12)
- Status: completed
- Tests: 183 passed, 0 failed, 4 skipped (Onboarding.API.Tests full suite; 49 new tests in FundosControllerTests)
- Notes: FundosController created with 12 endpoints (ConsultoriaFundo + Custodiante + TipoAtivo). Policy attributes, actor capture, null body 400, validation 422, DuplicateEntityException 409, KeyNotFoundException 404 all covered. TipoAtivo global scope confirmed via NSubstitute DidNotReceive assertion.

### iter=1 — T-48.5 (2026-05-12)
- Status: completed
- Commit: 335881a
- Tests: 244 passed, 0 failed, 4 skipped (Onboarding.API.Tests; +61 new tests)
- Notes: Extended FundosController with 10 endpoints (Fundo 5 + Cedente PF/PJ 5). State machine coverage: RASCUNHO→ATIVO valid 200, ENCERRADO→ATIVO invalid 400 with from/to detail. Cedente PF/PJ register paths covered (409 + 422 + happy path + actor capture). Policy reflection theory covers all 22 endpoints. Build 0 errors, 0 warnings.

### iter=1 — T-48.6 (2026-05-12)
- Status: completed
- Commit: 24ad7c8
- Tests: 244 passed, 0 failed, 4 skipped (Onboarding.API.Tests full suite; 14 new tests in AdminFundosControllerTests)
- Notes: AdminFundosController created with 4 read-only cross-company endpoints (GET /api/admin/fundos, /consultorias, /custodiantes, /cedentes). Class-level BearerBackoffice+CrossCompanyAccess enforced. 4 Infrastructure query handlers using IgnoreQueryFilters()+Join(Companies). Cedente shadow property projection (D-09/CR-03). 4 DI registrations added. Cross-company test asserts rows from CompanyA+CompanyB in same response.

### iter=1 — T-48.7 (2026-05-12)
- Status: completed
- Commit: 6b37fa9
- Tests: 12 passed, 0 failed (Onboarding.Integration.Tests full suite: 10 new Fundos + 2 existing Registration)
- Notes: Created FundosControllerIntegrationTests.cs with Testcontainers PostgreSQL + fake HMAC JWT (no Keycloak container). 10 scenarios: POST 201, GET own row, multi-tenant isolation PJ-B≠PJ-A, 403 no-perm, 401 no-auth, admin cross-company BearerBackoffice sees both companies, RASCUNHO→ATIVO 200, ENCERRADO→ATIVO 400. Also fixed DDD layering bug from T-48.6: moved 4 admin query handler DI registrations from Application to Infrastructure (Application must not reference Infrastructure types).

### iter=2 — fix cross-tenant blockers (2026-05-16)
- Status: completed
- Commit: eb5bc24
- Tests: API.Tests 244 passed, 0 failed; Integration.Tests 16 passed, 0 failed (+4 new cross-tenant GET-by-id scenarios)
- Notes: Fixed 5 BLOCKER security findings from reviewer iter 1. Root cause: GetByIdAsync uses IgnoreQueryFilters() in all 4 Fundos repositories; company-A entities were readable by company-B users via the 4 GET-by-id controller actions. Strategy chosen: controller-side tenant check (lowest blast radius — admin paths do not call these repository methods). Added `if (entity is null || entity.ClienteId != _currentCompanyService.CompanyId) return NotFound()` to GetConsultoriaById, GetCustodianteById, GetFundoById, GetCedenteById. Return NotFound (not Forbid) to not leak entity existence across tenants. Added 4 integration test scenarios (scenarios 9–12) confirming cross-tenant GET-by-id returns 404 for ConsultoriaFundo, Custodiante, Fundo, and Cedente.

### iter=2 — review aggregate (2026-05-16)
- backend-csharp: BLOCKED (G8 whitespace lint on tests/Onboarding.Integration.Tests/Fundos/FundosControllerIntegrationTests.cs:494-495 anonymous object — introduced by iter 2)
- frontend-vinext: APPROVED_WITH_WARNINGS (no frontend touched; carried PERMISSION_LABELS contract drift)
- security: APPROVED_WITH_WARNINGS (all 5 iter-1 blockers RESOLVED; warnings pre-existing)
- Aggregate verdict: BLOCKED (worst-case wins)
- Hash: lint-G8-1file (different from iter1 hash → no oscillation)
- Next: iter 3 fix whitespace lint

