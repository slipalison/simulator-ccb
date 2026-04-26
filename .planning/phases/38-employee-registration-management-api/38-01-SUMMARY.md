---
phase: 38-employee-registration-management-api
plan: 01
subsystem: api, application
tags: [company, registration, cnpj, keycloak, access-group, cqrs, fluentvalidation, admin, pagination]

# Dependency graph
requires:
  - phase: 37-domain-model-redesign
    plan: 01
    provides: Company aggregate, Employee aggregate, AccessGroup, Permissions, TermsAcceptance, ActionType, repositories
  - phase: 37-domain-model-redesign
    plan: 04
    provides: Stub handlers (NotImplementedException), CompaniesController skeleton, DependencyInjection registration

provides:
  - POST /api/companies/registration — full company registration with Keycloak user + AccessGroup seeding
  - RegisterCompanyCommand + Handler with duplicate check, compensation, audit
  - RegisterCompanyCommandValidator with CNPJ, email, password policy, terms acceptance
  - RegisterCompanyRequest/RegisterCompanyResult DTOs
  - GetPaginatedCompaniesHandler with search/status filters and pagination
  - GetCompanyDetailsHandler returning CompanySummaryDto or KeyNotFoundException
  - UpdateCompanyCommandHandler with DB update + Keycloak email sync + audit
  - CompanyUpdated=26 ActionType for audit logging
  - ICompanyRepository.GetPagedAsync for admin paginated listing
  - 28 unit tests (6 handler + 14 validator + 4 paginated + 2 details + 2 update)

affects: [39-keycloak-groups, 40-client-frontend, 41-backoffice-employee]

# Tech tracking
tech-stack:
  added: []
  patterns: [company-registration-flow, compensation-on-failure, admin-paginated-query, keycloak-email-sync-on-update]

key-files:
  created:
    - src/Onboarding.Application/Companies/Commands/RegisterCompanyCommand.cs
    - src/Onboarding.Application/Companies/Commands/RegisterCompanyCommandHandler.cs
    - src/Onboarding.Application/Companies/Commands/RegisterCompanyCommandValidator.cs
    - src/Onboarding.Application/Companies/DTOs/RegisterCompanyResult.cs
    - src/Onboarding.Application/Companies/DTOs/RegisterCompanyRequest.cs
    - tests/Onboarding.Domain.Tests/Application/Companies/RegisterCompanyCommandHandlerTests.cs
    - tests/Onboarding.Domain.Tests/Application/Companies/RegisterCompanyCommandValidatorTests.cs
    - tests/Onboarding.Domain.Tests/Application/Admin/GetPaginatedCompaniesHandlerTests.cs
    - tests/Onboarding.Domain.Tests/Application/Admin/GetCompanyDetailsHandlerTests.cs
    - tests/Onboarding.Domain.Tests/Application/Admin/UpdateCompanyCommandHandlerTests.cs
  modified:
    - src/Onboarding.API/Controllers/CompaniesController.cs
    - src/Onboarding.Application/DependencyInjection.cs
    - src/Onboarding.Application/Admin/Commands/UpdateCompanyCommand.cs
    - src/Onboarding.Application/Admin/Queries/GetPaginatedCompaniesQuery.cs
    - src/Onboarding.Application/Admin/Queries/GetCompanyDetailsQuery.cs
    - src/Onboarding.Domain/Aggregates/Audit/ActionType.cs
    - src/Onboarding.Domain/Repositories/ICompanyRepository.cs
    - src/Onboarding.Infrastructure/Repositories/CompanyRepository.cs

key-decisions:
  - "RegisterCompanyCommandHandler compensates by deleting Company row if Keycloak user creation fails (T-38-05)"
  - "CompaniesController extracts IP from RemoteIpAddress + X-Forwarded-For first IP"
  - "DuplicateCompanyException maps to 409 Conflict in controller — generic message avoids info leakage (SEC-08, T-38-04)"
  - "DuplicateKeycloakUserException also maps to 409 Conflict in controller"
  - "UpdateCompanyCommandHandler syncs email change to Keycloak best-effort (logs error, does not rethrow)"
  - "CompanyUpdated=26 added to ActionType enum for update audit records"
  - "GetPagedAsync uses search by razaoSocial/CNPJ/email and status filter by DeletedAt"

patterns-established:
  - "Registration flow: validate → check duplicates → persist → create Keycloak → set KeycloakUserId → seed AccessGroups → audit"
  - "Compensation pattern: if Keycloak CreateUserAsync throws, delete Company row from DB before rethrowing"
  - "Admin update sync: DB update first, then best-effort Keycloak sync with error logging"

requirements-completed: [REG-01, REG-02]

# Metrics
duration: 18min
completed: 2026-04-26
---

# Phase 38 Plan 01: Company Registration & Admin Handlers Summary

**Company registration endpoint with Keycloak user creation, AccessGroup seeding, and admin company query/update handlers — replacing Phase 37 stubs**

## Performance

- **Duration:** 18 min
- **Started:** 2026-04-26T00:49:13Z
- **Completed:** 2026-04-26T01:07:20Z
- **Tasks:** 2
- **Files modified:** 18

## Accomplishments
- POST /api/companies/registration endpoint — full PJ company registration with duplicate CNPJ/email check, Keycloak user creation, AccessGroup seeding, and compensation on failure
- FluentValidation matching Keycloak password policy (8+ chars, upper, lower, digit, special) plus CNPJ validation via Cnpj.Create()
- TermsAcceptance required on registration (TermsAccepted must be true)
- GetPaginatedCompanies query with search/status filters and pagination via ICompanyRepository.GetPagedAsync
- GetCompanyDetails query returning CompanySummaryDto or KeyNotFoundException
- UpdateCompany command handler with DB update, best-effort Keycloak email sync, and audit logging (CompanyUpdated=26)
- CompaniesController.RegisterCompany replaces skeleton 501 with full implementation — 201 Created, 409 Conflict, 422 Unprocessable Entity
- 28 unit tests all passing (6 registration handler + 14 registration validator + 2 paginated + 2 details + 4 update)

## Task Commits

1. **Task 1: Company Registration — Command, Handler, Validator with TDD** - `87a1d4d` → `e7deab5` (test → feat)
2. **Task 2: Admin Company Handlers (GetPaginatedCompanies, GetCompanyDetails, UpdateCompany)** - `1c38beb` (feat)

**Plan metadata:** (no separate metadata commit yet)

## Files Created/Modified
- `src/Onboarding.Application/Companies/Commands/RegisterCompanyCommand.cs` - Command record with full field list
- `src/Onboarding.Application/Companies/Commands/RegisterCompanyCommandHandler.cs` - Full handler with duplicate check, Keycloak creation, compensation, AccessGroup seeding, audit
- `src/Onboarding.Application/Companies/Commands/RegisterCompanyCommandValidator.cs` - FluentValidation with CNPJ, email, password policy, terms acceptance
- `src/Onboarding.Application/Companies/DTOs/RegisterCompanyResult.cs` - Result record with CompanyId and KeycloakUserId
- `src/Onboarding.Application/Companies/DTOs/RegisterCompanyRequest.cs` - Request DTO for controller
- `src/Onboarding.API/Controllers/CompaniesController.cs` - POST /registration endpoint with 201/409/422 responses
- `src/Onboarding.Application/DependencyInjection.cs` - Handler and validator DI registration
- `src/Onboarding.Application/Admin/Commands/UpdateCompanyCommand.cs` - Replaced stub with full handler + Keycloak email sync + audit
- `src/Onboarding.Application/Admin/Queries/GetPaginatedCompaniesQuery.cs` - Replaced stub with full handler and pagination
- `src/Onboarding.Application/Admin/Queries/GetCompanyDetailsQuery.cs` - Replaced stub with full handler returning CompanySummaryDto
- `src/Onboarding.Domain/Aggregates/Audit/ActionType.cs` - Added CompanyUpdated=26
- `src/Onboarding.Domain/Repositories/ICompanyRepository.cs` - Added GetPagedAsync signature
- `src/Onboarding.Infrastructure/Repositories/CompanyRepository.cs` - Added GetPagedAsync implementation
- `tests/Onboarding.Domain.Tests/Application/Companies/RegisterCompanyCommandHandlerTests.cs` - 6 handler tests
- `tests/Onboarding.Domain.Tests/Application/Companies/RegisterCompanyCommandValidatorTests.cs` - 14 validator tests
- `tests/Onboarding.Domain.Tests/Application/Admin/GetPaginatedCompaniesHandlerTests.cs` - 2 paginated tests
- `tests/Onboarding.Domain.Tests/Application/Admin/GetCompanyDetailsHandlerTests.cs` - 2 details tests
- `tests/Onboarding.Domain.Tests/Application/Admin/UpdateCompanyCommandHandlerTests.cs` - 4 update tests

## Decisions Made
- Compensation pattern: Keycloak failure → delete Company row from DB before rethrowing (per plan threat model T-38-05)
- UpdateCompanyCommandHandler syncs email change to Keycloak best-effort: catches exception, logs error, does NOT rethrow — DB update is the source of truth
- CompanyUpdated=26 added to ActionType enum (next available after AccessGroupChanged=25)
- GetPagedAsync search: case-insensitive contains on razaoSocial, CNPJ, email fields
- GetPagedAsync status filter: "active" = not deleted, "deleted" = deleted, any other value = no filter

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Duplicate ICompanyRepository namespace and using references**
- **Found during:** Task 2 (implementing GetPagedAsync in ICompanyRepository)
- **Issue:** The original stub files contained both the record definition AND the handler stub, but I overwrote them with handler-only files, losing the record definitions
- **Fix:** Added the record definitions (GetPaginatedCompaniesQuery, GetCompanyDetailsQuery, UpdateCompanyCommand) back into the same files as their handlers
- **Files modified:** GetPaginatedCompaniesQuery.cs, GetCompanyDetailsQuery.cs, UpdateCompanyCommand.cs
- **Verification:** Build clean 0 errors 0 warnings, all 28 tests passing
- **Committed in:** 1c38beb (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** Essential for compilation. No scope creep.

## Issues Encountered
- None — build clean on first attempt after fixes

## Known Stubs
- GetPaginatedEmployeesHandler, GetEmployeeDetailsHandler, DeleteEmployeeCommandHandler, BlockEmployeeCommandHandler, UnblockEmployeeCommandHandler — still throw NotImplementedException (Phase 41 scope)

## Next Phase Readiness
- Company registration endpoint fully functional — POST /api/companies/registration creates Company + Keycloak user + seeds 3 AccessGroups
- Admin company handlers implemented — paginated listing, details, update with Keycloak sync
- Phase 39: Keycloak groups sync for AccessGroups
- Phase 40: Client frontend for PJ registration and employee management
- Phase 41: BackOffice employee management (block, unblock, delete) + remaining admin handlers

## Self-Check: PASSED

- All 18 created/modified files verified on disk
- All 3 commits (87a1d4d, e7deab5, 1c38beb) verified in git log
- dotnet build: 0 errors, 0 warnings
- 28 unit tests passing (6 handler + 14 validator + 2 paginated + 2 details + 4 update)
- No accidental file deletions

---
*Phase: 38-employee-registration-management-api*
*Completed: 2026-04-26*