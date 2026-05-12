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

