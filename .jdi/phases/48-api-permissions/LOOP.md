---
phase: 48
iter: 1
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

