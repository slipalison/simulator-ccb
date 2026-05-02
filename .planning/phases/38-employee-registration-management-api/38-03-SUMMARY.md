---
phase: 38-employee-registration-management-api
plan: 03
subsystem: api, application
tags: [admin, employee, block, unblock, delete, lgpd, keycloak, audit, paginated-query, bypass-queryfilter, tdd]

# Dependency graph
requires:
  - phase: 37-domain-model-redesign
    plan: 01
    provides: Company aggregate, Employee aggregate, IEmployeeRepository, ActionType, IKeycloakUserService, IAuditService
  - phase: 37-domain-model-redesign
    plan: 03
    provides: EmployeeRepository with HasQueryFilter, ICurrentCompanyService, EF Core configs
  - phase: 38-employee-registration-management-api
    plan: 01
    provides: CompaniesController, DependencyInjection, company handlers
  - phase: 38-employee-registration-management-api
    plan: 02
    provides: Companies-scoped employee handlers (toggle-status, delete, register), AdminUserController stub endpoints

provides:
  - GET /api/admin/employees — paginated listing across ALL companies with optional CompanyId filter (bypasses HasQueryFilter)
  - GET /api/admin/employees/{id} — employee details regardless of company (bypasses HasQueryFilter)
  - POST /api/admin/employees/{id}/block — blocks employee in Keycloak + revokes sessions + audit
  - POST /api/admin/employees/{id}/unblock — unblocks employee in Keycloak + audit
  - DELETE /api/admin/employees/{id} — LGPD deletion (anonymize + Keycloak delete + audit, idempotent)
  - IEmployeeRepository.GetPagedAllAsync — admin cross-company paginated query with IgnoreQueryFilters
  - IEmployeeRepository.GetByIdIgnoreFilterAsync — single employee lookup bypassing HasQueryFilter
  - 13 unit tests for admin employee handlers

affects: [41-backoffice-employee]

# Tech tracking
tech-stack:
  added: []
  patterns: [admin-bypass-queryfilter, ignore-query-filters-cross-company, idempotent-lgpd-delete-admin, block-plus-session-revocation]

key-files:
  created:
    - tests/Onboarding.Domain.Tests/Application/Admin/GetPaginatedEmployeesHandlerTests.cs
    - tests/Onboarding.Domain.Tests/Application/Admin/GetEmployeeDetailsHandlerTests.cs
    - tests/Onboarding.Domain.Tests/Application/Admin/BlockEmployeeHandlerTests.cs
    - tests/Onboarding.Domain.Tests/Application/Admin/DeleteEmployeeHandlerTests.cs
  modified:
    - src/Onboarding.Application/Admin/Queries/GetPaginatedEmployeesQuery.cs
    - src/Onboarding.Application/Admin/Queries/GetEmployeeDetailsQuery.cs
    - src/Onboarding.Application/Admin/Commands/BlockEmployeeCommand.cs
    - src/Onboarding.Application/Admin/Commands/UnblockEmployeeCommand.cs
    - src/Onboarding.Application/Admin/Commands/DeleteEmployeeCommand.cs
    - src/Onboarding.Domain/Repositories/IEmployeeRepository.cs
    - src/Onboarding.Infrastructure/Repositories/EmployeeRepository.cs
    - src/Onboarding.Application/DependencyInjection.cs

key-decisions:
  - "Admin query handlers use IgnoreQueryFilters() to bypass HasQueryFilter by CompanyId — admin sees ALL companies' employees (T-38-13)"
  - "GetPagedAllAsync uses IgnoreQueryFilters for cross-company listing; GetPagedByCompanyAsync uses explicit CompanyId for filtered listing"
  - "GetByIdIgnoreFilterAsync bypasses HasQueryFilter for admin single-employee lookup"
  - "BlockEmployee + UnblockEmployee use admin context (actorSub only, no actorEmail) — audit records empty string for actorEmail"
  - "DeleteEmployeeCommand admin is idempotent on re-delete — same pattern as Companies-scoped delete"
  - "CompanyRazaoSocial resolved via batch lookup to ICompanyRepository — no join query needed"

patterns-established:
  - "Admin bypass pattern: Use GetByIdIgnoreFilterAsync / GetPagedAllAsync (IgnoreQueryFilters) for admin endpoints that must see across companies"
  - "Idempotent LGPD delete: Anonymize() is a no-op on already-deleted employees — audit the attempt, skip SaveAsync, try Keycloak deletion best-effort"

requirements-completed: [REG-03, MGMT-01, MGMT-02, MGMT-03, MGMT-04, MGMT-05]

# Metrics
duration: 10min
completed: 2026-04-26
---

# Phase 38 Plan 03: Admin Employee Handlers Summary

**Admin employee query/command handlers replacing stubs — paginated cross-company listing, employee details, block/unblock with session revocation, LGPD deletion with idempotent Anonymize**

## Performance

- **Duration:** 10 min
- **Started:** 2026-04-26T04:40:15Z
- **Completed:** 2026-04-26T04:51:04Z
- **Tasks:** 2
- **Files modified:** 12

## Accomplishments
- GetPaginatedEmployeesHandler: admin cross-company listing with optional CompanyId filter, bypasses HasQueryFilter
- GetEmployeeDetailsHandler: admin single-employee details bypassing company isolation, with CompanyRazaoSocial resolution
- BlockEmployeeCommandHandler: blocks in Keycloak, revokes sessions, records audit (EmployeeBlocked)
- UnblockEmployeeCommandHandler: unblocks in Keycloak, records audit (EmployeeUnblocked)
- DeleteEmployeeCommandHandler: LGPD Anonymize + Keycloak deletion + audit, idempotent on re-delete
- IEmployeeRepository: added GetPagedAllAsync (IgnoreQueryFilters) and GetByIdIgnoreFilterAsync for admin bypass
- All 5 admin employee endpoints now fully functional (previously threw NotImplementedException)
- 13 new unit tests (4 paginated + 2 details + 2 block + 2 unblock + 3 delete)

## Task Commits

Each task was committed atomically:

1. **Task 1: Admin Employee Query Handlers (GetPaginatedEmployees, GetEmployeeDetails)** - `81ca3cc` (feat)
2. **Task 2: Admin Employee Command Handlers (Block, Unblock, Delete LGPD)** - `3bae8a7` (feat)

## Files Created/Modified
- `src/Onboarding.Application/Admin/Queries/GetPaginatedEmployeesQuery.cs` - Full handler with GetPagedAllAsync/GetPagedByCompanyAsync routing + CompanyRazaoSocial resolution
- `src/Onboarding.Application/Admin/Queries/GetEmployeeDetailsQuery.cs` - Full handler with GetByIdIgnoreFilterAsync + CompanyRazaoSocial lookup
- `src/Onboarding.Application/Admin/Commands/BlockEmployeeCommand.cs` - BlockEmployeeCommandHandler with Keycloak block + session revocation + audit
- `src/Onboarding.Application/Admin/Commands/UnblockEmployeeCommand.cs` - UnblockEmployeeCommandHandler with Keycloak unblock + audit
- `src/Onboarding.Application/Admin/Commands/DeleteEmployeeCommand.cs` - DeleteEmployeeCommandHandler with LGPD Anonymize + Keycloak deletion + idempotent re-delete
- `src/Onboarding.Domain/Repositories/IEmployeeRepository.cs` - Added GetPagedAllAsync and GetByIdIgnoreFilterAsync
- `src/Onboarding.Infrastructure/Repositories/EmployeeRepository.cs` - Implemented GetPagedAllAsync (IgnoreQueryFilters) and GetByIdIgnoreFilterAsync
- `src/Onboarding.Application/DependencyInjection.cs` - Updated DI comment reflecting implemented handlers
- `tests/Onboarding.Domain.Tests/Application/Admin/GetPaginatedEmployeesHandlerTests.cs` - 4 tests (all companies, filtered by company, search/status, company name)
- `tests/Onboarding.Domain.Tests/Application/Admin/GetEmployeeDetailsHandlerTests.cs` - 2 tests (found, not found)
- `tests/Onboarding.Domain.Tests/Application/Admin/BlockEmployeeHandlerTests.cs` - 2 tests (block+revoke+audit, not found)
- `tests/Onboarding.Domain.Tests/Application/Admin/DeleteEmployeeHandlerTests.cs` - 3 tests (anonymize+delete+audit, idempotent, not found) + UnblockEmployeeHandlerTests (2 tests)

## Decisions Made
- Admin query handlers use IgnoreQueryFilters() to bypass HasQueryFilter by CompanyId — admin sees ALL companies' employees (T-38-13: accepted, admin endpoints ARE elevated by design)
- CompanyRazaoSocial resolved via ICompanyRepository batch lookup rather than join query — simpler implementation, adequate for admin listing
- BlockEmployeeCommand records empty string for actorEmail (same pattern as plan-specified `command.ActorSub, ""`)
- Idempotent LGPD delete: same pattern as Companies-scoped version — audit re-delete attempts, skip second Anonymize(), best-effort Keycloak deletion

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Invalid CPF in test data caused Employee.Create to throw ArgumentException**
- **Found during:** Task 1 (writing test for GetPaginatedEmployeesHandler)
- **Issue:** Used `52998256001` as CPF which is invalid per Cpf.Create validation
- **Fix:** Changed test helper to use valid CPF `52998224725` matching existing test conventions
- **Files modified:** GetPaginatedEmployeesHandlerTests.cs, GetEmployeeDetailsHandlerTests.cs
- **Verification:** All 13 admin tests pass
- **Committed in:** 81ca3cc (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** Minimal — test data only, no production code affected.

## Issues Encountered
- None — build clean, all tests passing on first implementation attempt

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All admin employee endpoints fully functional (list, details, block, unblock, delete)
- Phase 39: Keycloak groups/permissions sync for AccessGroups
- Phase 40: Client frontend for PJ registration and employee management
- Phase 41: BackOffice employee management UI + audit trail

## Self-Check: PASSED

- All 12 created/modified files verified on disk
- All 2 commits (81ca3cc, 3bae8a7) verified in git log
- dotnet build: 0 errors, pre-existing CS0649/CS8618 warnings only (not from this plan)
- 190 unit tests passing (13 new admin employee + 177 existing)
- No accidental file deletions

---
*Phase: 38-employee-registration-management-api*
*Completed: 2026-04-26*