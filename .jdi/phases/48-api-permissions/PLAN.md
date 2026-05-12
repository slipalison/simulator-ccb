# Phase 48: API + Permissions for Fundos module — Plan

## Goal
Expose REST endpoints for the Fundos module (ConsultoriaFundo, Custodiante, TipoAtivo, Fundo, Cedente PF/PJ) via `FundosController` + create `AdminFundosController` for cross-company read-only listing + register `funds:read/write/delete/manage` policies + extend default access groups (`admin-empresa` gets `funds:manage`, `viewer` gets `funds:read`) — with multi-tenant isolation applied across all company-scoped mutations and queries.

## Locked decisions (from CONTEXT.md)
- D-1: Code design = DDD
- D-2: Coverage 80% enforced ONLY on files created after `968eefb` boundary
- D-3: OSS-only — no MediatR, no FluentAssertions (use manual CQRS via DI + Shouldly)
- D-5: Multi-tenant isolation is first-class — HasQueryFilter + ClientId guards
- D-7: Plan 48-01 pre-work discarded (commit `0e73aee`), re-implement from clean state
- D-8: AdminFundosController scope = List cross-company only (4 entities)
- D-9: `POST /api/fundos/{id}/status` body = `{ NewStatus }` only
- D-10: Cedente uniqueness company-scoped — composite `(ClientId, Cpf)` + `(ClientId, Cnpj)` (verified already in CedenteConfiguration.cs lines 104/110)

## Tasks

### Wave 1 (parallel-eligible, no deps)

#### T-48.1: Add fund permission policy constants
- **Specialist:** jdi-doer-onboarding-keycloak-security (file glob `**/Permission*` + `**/Security/**`)
- **Files modified:** `src/Onboarding.API/Security/PermissionPolicyConstants.cs`
- **Acceptance:**
  - `PermissionPolicies` static class contains constants `FundRead`, `FundWrite`, `FundDelete`, `FundManage` (string values matching constant names)
  - Build passes: `dotnet build src/Onboarding.API`
- **Dependencies:** none
- **Test:** `tests/Onboarding.API.Tests/Security/PermissionPolicyConstantsTests.cs` (assert 4 new constants exist + values match)
- **Status:** completed (commit `e8a680c`, 2026-05-12)

#### T-48.2: Register 4 fund policies + map domain exceptions
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp (file glob `**/*.cs`)
- **Files modified:**
  - `src/Onboarding.API/Program.cs` (add 4 `AddPolicy` calls in `AddAuthorization` block)
  - `src/Onboarding.API/Middleware/GlobalExceptionHandler.cs` (add `DuplicateEntityException` → 409, `InvalidStateTransitionException` → 400 BEFORE catch-all 500)
- **Acceptance:**
  - Each fund policy registered with `PermissionRequirement(Permissions.FundsX)`
  - `DuplicateEntityException` and `InvalidStateTransitionException` mapped in switch expression with proper detail message passthrough
  - `dotnet test tests/Onboarding.API.Tests/Middleware/GlobalExceptionHandlerTests.cs` passes
- **Dependencies:** none (independent of T-48.1 — policies reference constants but registration only needs string keys which can be added independently and validated together at runtime)
- **Test:** extend `GlobalExceptionHandlerTests.cs` with 2 new tests (DuplicateEntity→409, InvalidStateTransition→400)
- **Status:** completed (T-48.2, 2026-05-12)

#### T-48.3: Extend default access groups with fund permissions
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp (`**/*.cs`)
- **Files modified:** `src/Onboarding.Domain/Aggregates/EmployeeAggregate/AccessGroup.cs` (extend `CreateDefaultGroups()` factory method)
- **Acceptance:**
  - `admin-empresa` default group permissions list includes `Permissions.FundsManage` (which implies all funds:* operations by convention; document this in code comment)
  - `viewer` default group permissions list includes `Permissions.FundsRead`
  - Existing `AccessGroupTests.cs` updated to assert new permission counts (admin-empresa now has 11 permissions, viewer has 2)
  - `dotnet test tests/Onboarding.Domain.Tests/Aggregates/AccessGroupTests.cs` passes
- **Dependencies:** none
- **Test:** update `tests/Onboarding.Domain.Tests/Aggregates/AccessGroupTests.cs` (already references this — see commit cf2af10 / c859267 historical)
- **Status:** completed

### Wave 2 (depends on Wave 1)

#### T-48.4: FundosController — ConsultoriaFundo + Custodiante + TipoAtivo CRUD (12 endpoints)
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:** `src/Onboarding.API/Controllers/FundosController.cs` (CREATE NEW)
- **Acceptance:**
  - Class-level `[Authorize(AuthenticationSchemes = "BearerClient")]`
  - 12 endpoints (4 per entity × 3 entities): POST/GET list/GET by id/PUT for ConsultoriaFundo (route `consultorias`), Custodiante (route `custodiantes`), TipoAtivo (route `tipos-ativo`)
  - Each endpoint decorated with `[Authorize(..., Policy = PermissionPolicies.FundRead|FundWrite)]` matching GET vs POST/PUT
  - Mutations catch `DuplicateEntityException` → 409 (explicit) and `KeyNotFoundException` → 404 (explicit)
  - FluentValidation failures → 422 UnprocessableEntity via `ToValidationProblem` helper
  - Actor info captured from `ICurrentCompanyService` (company-scoped) or JWT claims `sub`/`email` (TipoAtivo, global)
  - `[ProducesResponseType]` attributes per endpoint for OpenAPI/Swagger
- **Dependencies:** T-48.1 (policies constants), T-48.2 (policies registered + exception handler)
- **Test:** `tests/Onboarding.API.Tests/Controllers/FundosControllerTests.cs` (covers 12 endpoints — happy path + 4xx error paths per endpoint; verify policy attribute via reflection or via integration test 403 path)
- **Status:** completed (commit `TBD`, 2026-05-12)

### Wave 3 (depends on T-48.4, runs parallel internally)

#### T-48.5: FundosController extend — Fundo (5 endpoints incl status transition) + Cedente PF/PJ (5 endpoints)
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:** `src/Onboarding.API/Controllers/FundosController.cs` (EDIT — append endpoints)
- **Acceptance:**
  - Fundo: `POST /api/fundos`, `GET /api/fundos`, `GET /api/fundos/{id:guid}`, `PUT /api/fundos/{id:guid}`, `POST /api/fundos/{id:guid}/status` (body `{ NewStatus }` per D-9)
  - Cedente: `POST /api/fundos/cedentes/pf` (body with CPF), `POST /api/fundos/cedentes/pj` (body with CNPJ), `GET /api/fundos/cedentes`, `GET /api/fundos/cedentes/{id:guid}`, `PUT /api/fundos/cedentes/{id:guid}`
  - Status transition catches `InvalidStateTransitionException` → 400 BadRequest with from/to info in detail
  - All endpoints company-scoped via `ICurrentCompanyService` + actor in commands
- **Dependencies:** T-48.4 (same file — sequential)
- **Test:** extend `FundosControllerTests.cs` (Fundo status machine transitions: RASCUNHO→ATIVO valid, ENCERRADO→ATIVO invalid → 400; Cedente PF/PJ register paths)
- **Status:** pending

#### T-48.6: AdminFundosController + admin query handlers (4 endpoints cross-company List only)
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `src/Onboarding.API/Controllers/AdminFundosController.cs` (CREATE NEW)
  - `src/Onboarding.Application/Fundos/Queries/Admin/` (CREATE — `ListAdminFundoQuery`, `ListAdminConsultoriaQuery`, `ListAdminCustodianteQuery`, `ListAdminCedenteQuery` + DTOs with `ClienteId` + `EmpresaNome` join field)
  - `src/Onboarding.Infrastructure/Repositories/FundosAdminQueryHandlers.cs` (CREATE — 4 handlers using `IgnoreQueryFilters()` + join with Company)
  - `src/Onboarding.Application/DependencyInjection.cs` (EDIT — register 4 admin handlers)
- **Acceptance:**
  - Class-level `[Authorize(AuthenticationSchemes = "BearerBackoffice", Policy = PermissionPolicies.CrossCompanyAccess)]`
  - 4 endpoints: `GET /api/admin/fundos`, `GET /api/admin/fundos/consultorias`, `GET /api/admin/fundos/custodiantes`, `GET /api/admin/fundos/cedentes` (route prefix consistent with admin convention — check AdminUserController)
  - Each returns `PaginatedResult<TAdminDto>` with `ClienteId` + `EmpresaNome` populated via SQL join
  - Repositories use `IgnoreQueryFilters()` — verified via SQL log: query has NO `WHERE clientId = @currentCompanyId` filter
  - No detail-by-id endpoint, no admin override, no mutation (per D-8)
- **Dependencies:** T-48.1, T-48.2 (policy `CrossCompanyAccess` already exists per Program.cs)
- **Test:** `tests/Onboarding.API.Tests/Controllers/AdminFundosControllerTests.cs` (verify cross-company returns rows from 2 different ClientIds in same response)
- **Status:** pending

### Wave 4 (depends on T-48.5 + T-48.6)

#### T-48.7: Integration test smoke — Testcontainers verify multi-tenant isolation + permission gating
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:** `tests/Onboarding.Integration.Tests/Fundos/FundosControllerIntegrationTests.cs` (CREATE NEW — Testcontainers PostgreSQL fixture)
- **Acceptance:**
  - PJ-A with `funds:write` can POST `/api/fundos/consultorias` → 201
  - PJ-A then GET `/api/fundos/consultorias` returns the row created
  - PJ-B (different ClientId) GET `/api/fundos/consultorias` does NOT see PJ-A's row (multi-tenant isolation)
  - Request without `funds:read` claim → 403 Forbidden
  - Request without auth → 401 Unauthorized
  - Admin (`BearerBackoffice` + `CrossCompanyAccess`) GET `/api/admin/fundos/consultorias` sees rows from BOTH PJ-A and PJ-B
  - Fundo state machine: RASCUNHO → ATIVO via `POST /{id}/status` = 200; ENCERRADO → ATIVO via same endpoint = 400 (InvalidStateTransitionException)
- **Dependencies:** T-48.5, T-48.6
- **Test:** the file itself (xUnit + Testcontainers + Shouldly + NSubstitute)
- **Status:** pending

## Execution
- Total tasks: 7
- Waves: 4 (W1 parallel 3-way, W2 single, W3 parallel 2-way, W4 single)
- Estimated parallel speedup: ~1.75× over strictly sequential

## Files modified (all tasks)
- `src/Onboarding.API/Security/PermissionPolicyConstants.cs` (T-48.1)
- `src/Onboarding.API/Program.cs` (T-48.2)
- `src/Onboarding.API/Middleware/GlobalExceptionHandler.cs` (T-48.2)
- `src/Onboarding.Domain/Aggregates/EmployeeAggregate/AccessGroup.cs` (T-48.3)
- `src/Onboarding.API/Controllers/FundosController.cs` (T-48.4 create, T-48.5 extend)
- `src/Onboarding.API/Controllers/AdminFundosController.cs` (T-48.6 create)
- `src/Onboarding.Application/Fundos/Queries/Admin/` (T-48.6 create — 4 query files + DTOs)
- `src/Onboarding.Infrastructure/Repositories/FundosAdminQueryHandlers.cs` (T-48.6 create)
- `src/Onboarding.Application/DependencyInjection.cs` (T-48.6 edit)
- `tests/Onboarding.API.Tests/Security/PermissionPolicyConstantsTests.cs` (T-48.1 create)
- `tests/Onboarding.API.Tests/Middleware/GlobalExceptionHandlerTests.cs` (T-48.2 extend)
- `tests/Onboarding.Domain.Tests/Aggregates/AccessGroupTests.cs` (T-48.3 update)
- `tests/Onboarding.API.Tests/Controllers/FundosControllerTests.cs` (T-48.4 + T-48.5)
- `tests/Onboarding.API.Tests/Controllers/AdminFundosControllerTests.cs` (T-48.6 create)
- `tests/Onboarding.Integration.Tests/Fundos/FundosControllerIntegrationTests.cs` (T-48.7 create)

## Test requirements
- Unit: `dotnet test tests/Onboarding.API.Tests` + `dotnet test tests/Onboarding.Domain.Tests`
- Integration: `dotnet test tests/Onboarding.Integration.Tests` (Testcontainers PostgreSQL)
- Minimum coverage: 80% on NEW files (D-2 boundary `968eefb`)
- Regression mandate: reviewer Playwright G7 against running stack (docker compose up + UAT suite)

## Specialist routing summary
| Task | Specialist | Reason |
|---|---|---|
| T-48.1 | security | `PermissionPolicyConstants.cs` matches security glob `**/Security/**` + `**/Permission*` |
| T-48.2 — T-48.7 | backend-csharp | `**/*.cs` files outside security glob |

## Notes
- T-48.4 includes `CrossCompanyAccess` policy already registered (from Phase 17 v3.0). No need to add — confirm during execution.
- Cedente composite indexes verified (`src/Onboarding.Infrastructure/Persistence/Configurations/CedenteConfiguration.cs` lines 104, 110). D-10 already implemented at infrastructure level — task body skips index work.
- Plan 48-02 GSD plan mentions `tests/Onboarding.Domain.Tests/Aggregates/AccessGroupTests.cs` already updated for 10 permissions (commit c859267). T-48.3 updates again to 11 (adding funds:manage to admin-empresa).
- Reviewer Playwright G7 = MANDATORY (project rule). Will fail verify if dev stack does not boot or endpoints regress.
