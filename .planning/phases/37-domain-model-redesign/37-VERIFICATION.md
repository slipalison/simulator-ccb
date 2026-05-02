---
phase: 37-domain-model-redesign
verified: 2026-04-26T02:45:00Z
status: gaps_found
score: 5/7 must-haves verified
overrides_applied: 0
re_verification: false
gaps:
  - truth: "AuthorizationMiddlewareTests and GlobalExceptionHandlerTests still reference /api/clients/me (deleted route)"
    status: failed
    reason: "Two test files were not updated when /api/clients/me was renamed to /api/companies/me — they call a non-existent route and fail"
    artifacts:
      - path: "tests/Onboarding.API.Tests/Authentication/AuthorizationMiddlewareTests.cs"
        issue: "Line 38: calls /api/clients/me — route no longer exists, test fails"
      - path: "tests/Onboarding.API.Tests/Middleware/GlobalExceptionHandlerTests.cs"
        issue: "Lines 60/76/87: call /api/clients/me — route no longer exists, tests fail"
    missing:
      - "Update test URLs from /api/clients/me to /api/companies/me"
  - truth: "AdminCompanyDetailsTests call stub handlers returning NotImplementedException — 2 integration tests fail"
    status: failed
    reason: "AdminCompanyDetailsTests.GetCompanyDetails tests call real handler stubs (NotImplementedException) instead of mocking the handler — tests fail with 500"
    artifacts:
      - path: "tests/Onboarding.API.Tests/Admin/AdminUserDetailsTests.cs"
        issue: "Lines 45-61, 65-74: call GetCompanyDetails handler which throws NotImplementedException"
    missing:
      - "Mock the GetCompanyDetailsHandler in AdminTestFactory, or defer these tests to Phase 38 when handler is implemented"
  - truth: "Integration test RegistrationIntegrationTests still calls /api/registration (deleted RegistrationController)"
    status: failed
    reason: "PostPj_ValidPayload_CreatesCompanyInKeycloak calls /api/registration which no longer exists — test fails"
    artifacts:
      - path: "tests/Onboarding.Integration.Tests/Registration/RegistrationIntegrationTests.cs"
        issue: "Lines 85, 102: POST /api/registration — controller deleted in Phase 37"
    missing:
      - "Update route to /api/companies/registration or remove integration test until Phase 38 implements registration"
deferred:
  - truth: "Admin CRUD handlers fully implemented (not stubs)"
    addressed_in: "Phase 38"
    evidence: "Phase 38 goal: Backend endpoints para registro PJ, cadastro de funcionários e CRUD completo"
  - truth: "CompaniesController /registration endpoint fully functional (not 501)"
    addressed_in: "Phase 38"
    evidence: "Phase 38 success criteria #1: POST /api/companies/registration registra PJ"
  - truth: "ICurrentCompanyService.CompanyId set from JWT claims per-request"
    addressed_in: "Phase 39"
    evidence: "Phase 39 goal: Backend lê groups do JWT e aplica permissões"
---

# Phase 37: Domain Model Redesign Verification Report

**Phase Goal:** Novos aggregates Company (PJ) e Employee (PF) substituem Client. AccessGroup como entidade configurável com permissões resource:action. TermsAcceptance value object. Remoção completa do fluxo PF (zero vestígios). Migration limpa: drop clients, cria companies, employees, access_groups. Admin endpoints que operam sobre Client são migrados nesta fase. Base zerada via docker compose down -v.
**Verified:** 2026-04-26T02:45:00Z
**Status:** gaps_found
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

ROADMAP success criteria merged with PLAN must-haves. 7 truths verified:

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Company aggregate exists with Cnpj, RazaoSocial, Email, PhoneNumber, KeycloakUserId, TermsAcceptance — CNPJ is value object with check-digit validation | ✓ VERIFIED | Company.cs: 96 lines, all properties present, Cnpj VO used |
| 2 | Employee aggregate exists with Cpf, Nome, Email, PhoneNumber, CompanyId (FK Guid), KeycloakUserId, AccessGroupId — CPF is value object with validation | ✓ VERIFIED | Employee.cs: 109 lines, all properties present, CompanyId Guid FK, Cpf VO used |
| 3 | TermsAcceptance value object exists with AcceptedAt, TermsVersion, IpAddress — mandatory on Company registration (D-12) | ✓ VERIFIED | TermsAcceptance.cs: 38 lines, sealed record with Create() factory, throws on empty input; Company.Register() throws on null TermsAcceptance |
| 4 | PF flow completely removed: zero traces of Client, ClientType.PF, RegisterPessoaFisica, /registration?tipo=pf in source code | ✓ VERIFIED | ClientAggregate dir deleted, IClientRepository deleted, 18 obsolete files confirmed deleted; grep across src/ shows only comment references |
| 5 | EF Core migration drops `clients`, creates `companies`, `employees`, `access_groups` — unique index on companies.cnpj (REG-02) | ✓ VERIFIED | Migration file exists: DropTable("clients"), CreateTable("companies"/"employees"/"access_groups"), IX_companies_cnpj unique with filter "cnpj IS NOT NULL" |
| 6 | HasQueryFilter on Employee and AccessGroup filtering by CompanyId (D-17) — company isolation | ✓ VERIFIED | EmployeeConfiguration.cs line 90: `HasQueryFilter(e => e.CompanyId == _currentCompanyService.CompanyId)`; AccessGroupConfiguration.cs line 55: same pattern |
| 7 | All domain unit tests passing (Company, Employee, AccessGroup, TermsAcceptance, Permissions) | ✗ FAILED | 124/124 domain tests pass, but 5 API/integration tests fail: 3 reference deleted /api/clients/me route, 2 call stub handlers, 1 calls deleted /api/registration |

**Score:** 5/7 truths verified (2 blocked by test failures in API layer)

### Deferred Items

Items not yet met but explicitly addressed in later milestone phases.

| # | Item | Addressed In | Evidence |
|---|------|-------------|----------|
| 1 | Admin CRUD handlers fully implemented (not stubs) | Phase 38 | Phase 38 goal: Backend endpoints para registro PJ e CRUD completo |
| 2 | CompaniesController /registration endpoint fully functional | Phase 38 | Phase 38 SC #1: POST /api/companies/registration registra PJ |
| 3 | ICurrentCompanyService.CompanyId set from JWT claims per-request | Phase 39 | Phase 39 goal: Backend lê groups do JWT e aplica permissões |

### Required Artifacts

| Artifact | Expected | Status | Details |
| -------- | -------- | ------ | ------- |
| `src/Onboarding.Domain/Aggregates/CompanyAggregate/Company.cs` | Company aggregate root with Register() and TermsAcceptance | ✓ VERIFIED | 96 lines, Register factory with TermsAcceptance required, Anonymize, Update, SetKeycloakUserId |
| `src/Onboarding.Domain/Aggregates/CompanyAggregate/TermsAcceptance.cs` | TermsAcceptance value object | ✓ VERIFIED | 38 lines, sealed record, Create() throws on empty, CurrentVersion="1.0" |
| `src/Onboarding.Domain/Aggregates/EmployeeAggregate/Employee.cs` | Employee aggregate root with CompanyId FK | ✓ VERIFIED | 109 lines, CompanyId Guid FK, AccessGroupId, Anonymize, Update, SetAccessGroup |
| `src/Onboarding.Domain/Aggregates/EmployeeAggregate/AccessGroup.cs` | AccessGroup entity with permissions | ✓ VERIFIED | 67 lines, CreateDefaultGroups() returns 3 groups, UpdatePermissions validates |
| `src/Onboarding.Domain/Aggregates/EmployeeAggregate/Permissions.cs` | 6 predefined permission constants | ✓ VERIFIED | 21 lines, All[] with 6 resource:action strings |
| `src/Onboarding.Domain/Repositories/ICompanyRepository.cs` | Company repository contract | ✓ VERIFIED | 28 lines, ExistsByCnpjAsync (REG-02), GetByKeycloakSubAsync |
| `src/Onboarding.Domain/Repositories/IEmployeeRepository.cs` | Employee repository contract | ✓ VERIFIED | 24 lines, GetPagedByCompanyAsync |
| `src/Onboarding.Domain/Repositories/IAccessGroupRepository.cs` | AccessGroup repository contract | ✓ VERIFIED | 17 lines, AddRangeAsync, GetByCompanyIdAsync |
| `src/Onboarding.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs` | EF Core config for Company | ✓ VERIFIED | 84 lines, OwnsOne TermsAcceptance, unique index Cnpj (REG-02) |
| `src/Onboarding.Infrastructure/Persistence/Configurations/EmployeeConfiguration.cs` | EF Core config for Employee with HasQueryFilter | ✓ VERIFIED | 108 lines, HasQueryFilter by CompanyId (D-17), FKs, unique indexes |
| `src/Onboarding.Infrastructure/Persistence/Configurations/AccessGroupConfiguration.cs` | EF Core config for AccessGroup with HasQueryFilter | ✓ VERIFIED | 65 lines, HasQueryFilter, Permissions CSV conversion, composite unique index |
| `src/Onboarding.Infrastructure/Persistence/AppDbContext.cs` | Updated DbContext with new DbSets | ✓ VERIFIED | 36 lines, DbSet<Company/Employees/AccessGroups>, ICurrentCompanyService constructor |
| `src/Onboarding.Application/Common/ICurrentCompanyService.cs` | Interface for query filter injection | ✓ VERIFIED | 10 lines, Guid CompanyId property |
| `src/Onboarding.Infrastructure/Persistence/CurrentCompanyService.cs` | Implementation for HasQueryFilter | ✓ VERIFIED | 14 lines, scoped, CompanyId settable |
| `src/Onboarding.Infrastructure/Repositories/CompanyRepository.cs` | CompanyRepository implementation | ✓ VERIFIED | 71 lines, full CRUD with ExistsByCnpjAsync |
| `src/Onboarding.Infrastructure/Repositories/EmployeeRepository.cs` | EmployeeRepository implementation | ✓ VERIFIED | 113 lines, GetPagedByCompanyAsync with search/status filters |
| `src/Onboarding.Infrastructure/Repositories/AccessGroupRepository.cs` | AccessGroupRepository implementation | ✓ VERIFIED | 60 lines, AddRangeAsync, GetByCompanyIdAsync |
| Migration file (ReplaceClientWithCompanyEmployee) | Drops clients, creates companies/employees/access_groups | ✓ VERIFIED | 190 lines, DropTable("clients"), CreateTable all 3, indexes, FKs |
| `src/Onboarding.API/Controllers/CompaniesController.cs` | Replaces ClientsController | ✓ VERIFIED | 80 lines, GET /me (working), POST /registration (501 stub for Phase 38) |
| `src/Onboarding.API/Controllers/AdminUserController.cs` | Migrated to Company/Employee endpoints | ✓ VERIFIED | 544 lines, Company/Employee endpoints (companies, employees), no /users endpoints |
| `src/Onboarding.Domain/Exceptions/DuplicateCompanyException.cs` | Renamed from DuplicateClientException | ✓ VERIFIED | 12 lines, exists |
| `src/Onboarding.Domain/Aggregates/Audit/ActionType.cs` | Extended with values 18-25 | ✓ VERIFIED | 35 lines, CompanyRegistered=18 through AccessGroupChanged=25 |
| `src/Onboarding.Application/Admin/DTOs/CompanySummaryDto.cs` | Company summary DTO | ✓ VERIFIED | 13 lines, all required fields |
| `src/Onboarding.Application/Admin/DTOs/EmployeeSummaryDto.cs` | Employee summary DTO | ✓ VERIFIED | 17 lines, with CompanyId, AccessGroupId |

### Key Link Verification

| From | To | Via | Status | Details |
| ---- | -- | --- | ------ | ------- |
| Employee.cs → Company.cs | CompanyId Guid FK | CompanyId property | ✓ WIRED | `public Guid CompanyId` in Employee, FK configured in EmployeeConfiguration |
| Employee.cs → AccessGroup.cs | AccessGroupId Guid FK | AccessGroupId property | ✓ WIRED | `public Guid AccessGroupId` in Employee, FK configured in EmployeeConfiguration |
| Company.cs → TermsAcceptance.cs | Owned value object | TermsAcceptance property | ✓ WIRED | `public TermsAcceptance TermsAcceptance`, OwnsOne in CompanyConfiguration |
| AppDbContext → Company/Employee/AccessGroup | DbSet registrations | DbSet<T> properties | ✓ WIRED | DbSet<Company>, DbSet<Employee>, DbSet<AccessGroup> + ApplyConfiguration |
| EmployeeConfiguration → ICurrentCompanyService | HasQueryFilter injection | Constructor injection | ✓ WIRED | `_currentCompanyService.CompanyId` used in HasQueryFilter lambda |
| AccessGroupConfiguration → ICurrentCompanyService | HasQueryFilter injection | Constructor injection | ✓ WIRED | Same pattern as Employee |
| DependencyInjection.cs → ICurrentCompanyService | Scoped registration | AddScoped | ✓ WIRED | `services.AddScoped<ICurrentCompanyService, CurrentCompanyService>()` |
| DependencyInjection.cs → Repositories | DI registrations | AddScoped | ✓ WIRED | ICompanyRepository, IEmployeeRepository, IAccessGroupRepository all registered |
| AdminUserController → Company/Employee handlers | DI-injected handlers | Constructor injection | ✓ WIRED | 8 Company/Employee handler interfaces injected, no User handlers |
| CompaniesController → ICompanyRepository | Repository usage | Constructor injection | ✓ WIRED | `ICompanyRepository` injected, used in GetMe endpoint |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
| -------- | ------------- | ------ | ------------------ | ------ |
| CompaniesController.GetMe | company (from GetByKeycloakSubAsync) | ICompanyRepository → AppDbContext.Companies | ✓ DB-backed query | ✓ FLOWING |
| AdminUserController (Company/Employee endpoints) | handlers (DI-injected) | Stub handlers (NotImplementedException) | ✗ Stubs only | ⚠️ STATIC — Phase 38 |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| -------- | ------- | ------ | ------ |
| Solution builds with 0 errors | `dotnet build src/Onboarding.API/Onboarding.API.csproj` | 0 warnings, 0 errors | ✓ PASS |
| Domain tests pass (Company/Employee/AccessGroup/TermsAcceptance/Permissions) | `dotnet test --filter "Company|Employee|AccessGroup|TermsAcceptance|Permissions"` | 46/46 passed | ✓ PASS |
| Full domain test suite passes | `dotnet test tests/Onboarding.Domain.Tests/` | 124/124 passed | ✓ PASS |
| API test suite passes | `dotnet test tests/Onboarding.API.Tests/` | 70/77 passed (5 failures) | ✗ FAIL |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| ----------- | ---------- | ----------- | ------ | -------- |
| REG-02 | 37-01, 37-03 | CNPJ must be unique — conflict returns 409 | ✓ SATISFIED | Unique index on companies.cnpj with HasFilter("cnpj IS NOT NULL") in migration + CompanyConfiguration; ICompanyRepository.ExistsByCnpjAsync |
| REG-04 | 37-01 | Terms acceptance mandatory on registration — stores timestamp and version | ✓ SATISFIED | TermsAcceptance.cs with AcceptedAt, TermsVersion, IpAddress; Company.Register() throws if null; OwnsOne in CompanyConfiguration |
| REG-05 | 37-01, 37-04 | Completely remove PF flow from frontend and API | ✓ SATISFIED | ClientAggregate deleted, RegistrationController deleted, ClientsController deleted, all PF CQRS/handlers/DTOs deleted, no PF traces in src/ |

All 3 requirement IDs (REG-02, REG-04, REG-05) are covered. No orphaned requirements found.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
| ---- | ---- | ------- | -------- | ------ |
| `src/Onboarding.Application/Admin/Commands/UpdateCompanyCommand.cs` | 19 | NotImplementedException handler stub | ℹ️ Info | Intentional — Phase 38 implements |
| `src/Onboarding.Application/Admin/Commands/DeleteEmployeeCommand.cs` | 17 | NotImplementedException handler stub | ℹ️ Info | Intentional — Phase 38 implements |
| `src/Onboarding.Application/Admin/Commands/BlockEmployeeCommand.cs` | 17 | NotImplementedException handler stub | ℹ️ Info | Intentional — Phase 38 implements |
| `src/Onboarding.Application/Admin/Commands/UnblockEmployeeCommand.cs` | 17 | NotImplementedException handler stub | ℹ️ Info | Intentional — Phase 38 implements |
| `src/Onboarding.Application/Admin/Queries/GetPaginatedCompaniesQuery.cs` | 22 | NotImplementedException handler stub | ℹ️ Info | Intentional — Phase 38 implements |
| `src/Onboarding.Application/Admin/Queries/GetPaginatedEmployeesQuery.cs` | 23 | NotImplementedException handler stub | ℹ️ Info | Intentional — Phase 38 implements |
| `src/Onboarding.Application/Admin/Queries/GetCompanyDetailsQuery.cs` | 18 | NotImplementedException handler stub | ℹ️ Info | Intentional — Phase 38 implements |
| `src/Onboarding.Application/Admin/Queries/GetEmployeeDetailsQuery.cs` | 18 | NotImplementedException handler stub | ℹ️ Info | Intentional — Phase 38 implements |
| `tests/Onboarding.API.Tests/Authentication/AuthorizationMiddlewareTests.cs` | 38 | References deleted route /api/clients/me | ⚠️ Warning | Test fails — route was renamed to /api/companies/me |
| `tests/Onboarding.API.Tests/Middleware/GlobalExceptionHandlerTests.cs` | 60,76,87 | References deleted route /api/clients/me | ⚠️ Warning | 3 tests fail — route was renamed to /api/companies/me |
| `tests/Onboarding.Integration.Tests/Registration/RegistrationIntegrationTests.cs` | 85,102 | References deleted route /api/registration | ⚠️ Warning | Test fails — RegistrationController was deleted |
| `tests/Onboarding.API.Tests/Admin/AdminUserDetailsTests.cs` | 45-74 | Calls real stub handler instead of mock | ⚠️ Warning | 2 tests fail — GetCompanyDetailsHandler throws NotImplementedException |

### Human Verification Required

### 1. Verify CompaniesController.GetMe Returns Company Profile

**Test:** Authenticate as a PJ user (with BearerClient token) and call GET /api/companies/me
**Expected:** Returns CompanyProfileDto with Id, RazaoSocial, Email, Phone, Cnpj — matching the authenticated user's company
**Why human:** Requires running Docker Compose with Keycloak + PostgreSQL, creating a company, and authenticating — cannot test programmatically without full environment

### 2. Verify Admin Endpoints Route Correctly

**Test:** Call GET /api/admin/companies, GET /api/admin/employees as admin
**Expected:** Routes exist and return appropriate responses (or 500 from stub handlers, not 404)
**Why human:** Requires running server with Keycloak; stub handlers throw NotImplementedException (500) which is expected until Phase 38

### Gaps Summary

**3 test files reference obsolete routes that were renamed/deleted during Phase 37:**

1. **AuthorizationMiddlewareTests.cs** — still calls `/api/clients/me` (renamed to `/api/companies/me`). The test fails because the route no longer exists. This is a straightforward URL update.

2. **GlobalExceptionHandlerTests.cs** — still calls `/api/clients/me` in 3 test methods. Same root cause: route was renamed but tests not updated.

3. **RegistrationIntegrationTests.cs** — still calls `/api/registration` (RegistrationController was deleted). This integration test should either be removed or updated to `/api/companies/registration`, though the latter will also fail until Phase 38 implements the registration flow.

**1 test file calls stub handlers instead of mocking them:**

4. **AdminUserDetailsTests.cs** — The `AdminCompanyDetailsTests` class calls the real `GetCompanyDetailsHandler` which is a stub (throws `NotImplementedException`). The AdminTestFactory doesn't mock this handler, causing 2 test failures (500 instead of 200/404). These tests need to either mock the handler or be deferred to Phase 38.

**Net effect:** 5 API/integration tests fail out of 77 API tests + 2 integration tests. All failures are caused by Phase 37's route renaming and controller deletion not being fully propagated to test files. The domain code itself (Company, Employee, AccessGroup, TermsAcceptance, Permissions) is fully implemented and passing 124/124 domain tests.

---

_Verified: 2026-04-26T02:45:00Z_
_Verifier: the agent (gsd-verifier)_