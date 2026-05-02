---
phase: 39-keycloak-groups-permissions
plan: 03
subsystem: auth
tags: [keycloak-groups, permission-policies, authorization, handler-sync, eventual-consistency]

# Dependency graph
requires:
  - phase: 38-employee-registration-management-api
    provides: AccessGroup domain entity, handler constructors, IEmployeeRepository
  - phase: 39-01
    provides: IKeycloakUserService group methods (CreateGroupAsync, AddUserToGroupAsync, RemoveUserFromGroupAsync, GetGroupByNameAsync)
  - phase: 39-02
    provides: PermissionPolicies constants, PermissionAuthorizationHandler, ICurrentCompanyPermissionsService
provides:
  - RegisterCompanyCommandHandler creates 3 Keycloak groups on company registration
  - RegisterEmployeeCommandHandler adds employee to Keycloak group after user creation
  - ChangeEmployeeAccessGroupCommandHandler syncs group changes to Keycloak (add + remove)
  - CompaniesController endpoints enforce permission-based authorization policies
  - AdminUserController uses CrossCompanyAccess policy
affects: [40-client-frontend-pj, 41-backoffice-employee-audit]

# Tech tracking
tech-stack:
  added: []
  patterns: [best-effort Keycloak sync with eventual consistency, permission-based [Authorize(Policy)] on controller endpoints]

key-files:
  created: []
  modified:
    - src/Onboarding.Application/Companies/Commands/RegisterCompanyCommandHandler.cs
    - src/Onboarding.Application/Companies/Commands/RegisterEmployeeCommandHandler.cs
    - src/Onboarding.Application/Companies/Commands/ChangeEmployeeAccessGroupCommandHandler.cs
    - src/Onboarding.API/Controllers/CompaniesController.cs
    - src/Onboarding.API/Controllers/AdminUserController.cs
    - tests/Onboarding.Domain.Tests/Application/Companies/RegisterCompanyCommandHandlerTests.cs
    - tests/Onboarding.Domain.Tests/Application/Companies/EmployeeManagement/RegisterEmployeeCommandHandlerTests.cs
    - tests/Onboarding.Domain.Tests/Application/Companies/EmployeeManagement/EmployeeManagementHandlerTests.cs

key-decisions:
  - "Keycloak group sync failures are caught and logged but not rethrown (eventual consistency pattern per D-13, D-15, D-16)"
  - "PJ owner gets all permissions from ClientClaimsMiddleware — no special handler logic needed"
  - "GET /companies/me retains plain BearerClient auth — any authenticated client user can view own profile"
  - "POST /companies/registration remains public — no auth required for registration"
  - "CrossCompanyAccess policy replaces Roles=admin on AdminUserController — semantically equivalent but cleaner"

patterns-established:
  - "Best-effort Keycloak sync: DB is source of truth, Keycloak operations in try/catch with logging"
  - "Permission-based controller authorization: [Authorize(AuthenticationSchemes, Policy = PermissionPolicies.X)]"

requirements-completed: [PERM-01, PERM-02, PERM-03, PERM-04, PERM-05]

# Metrics
duration: 25min
completed: 2026-04-26
---

# Phase 39: Keycloak Groups & Permissions — Plan 03 Summary

**Handler-to-Keycloak group sync + permission-based authorization policies on all company endpoints**

## Performance

- **Duration:** 25 min
- **Started:** 2026-04-26T12:27:01Z
- **Completed:** 2026-04-26T12:51:34Z
- **Tasks:** 2
- **Files modified:** 8

## Accomplishments
- RegisterCompanyCommandHandler provisions 3 Keycloak groups (admin-empresa, viewer, dashboard) on company registration
- RegisterEmployeeCommandHandler adds employee to Keycloak group after user creation (viewer group by default)
- ChangeEmployeeAccessGroupCommandHandler syncs group changes to Keycloak: add to new group + remove from old group
- All Keycloak sync operations follow eventual consistency pattern — failures logged but not rethrown
- CompaniesController endpoints enforce permission-based policies: EmployeeRead, EmployeeWrite, EmployeeDelete, AccessGroupsManage
- AdminUserController uses CrossCompanyAccess policy (equivalent to admin role, cleaner semantics)
- GET /companies/me retains plain BearerClient auth, POST /registration remains public
- 8 new unit tests covering Keycloak group sync scenarios

## Task Commits

Each task was committed atomically:

1. **Task 1: Extend handlers with Keycloak group sync** - `eb4c97d` (feat)
2. **Task 2: Update CompaniesController with permission-based authorization policies** - `874a9f6` (feat)

## Files Created/Modified
- `src/Onboarding.Application/Companies/Commands/RegisterCompanyCommandHandler.cs` — Added CreateGroupAsync calls for 3 default groups after seeding AccessGroups
- `src/Onboarding.Application/Companies/Commands/RegisterEmployeeCommandHandler.cs` — Added AddUserToGroupAsync call after Keycloak user creation
- `src/Onboarding.Application/Companies/Commands/ChangeEmployeeAccessGroupCommandHandler.cs` — Added IKeycloakUserService injection + AddUserToGroupAsync/RemoveUserFromGroupAsync sync with best-effort try/catch
- `src/Onboarding.API/Controllers/CompaniesController.cs` — Added permission-based [Authorize(Policy)] on employee management endpoints, using Onboarding.API.Security namespace
- `src/Onboarding.API/Controllers/AdminUserController.cs` — Replaced Roles="admin" with Policy=PermissionPolicies.CrossCompanyAccess, added Onboarding.API.Security using
- `tests/Onboarding.Domain.Tests/Application/Companies/RegisterCompanyCommandHandlerTests.cs` — 2 new tests: CreateGroupAsync called 3 times, group creation failure doesn't block registration
- `tests/Onboarding.Domain.Tests/Application/Companies/EmployeeManagement/RegisterEmployeeCommandHandlerTests.cs` — 2 new tests: AddUserToGroupAsync called after creation, group not found logs warning but completes
- `tests/Onboarding.Domain.Tests/Application/Companies/EmployeeManagement/EmployeeManagementHandlerTests.cs` — 3 new tests for ChangeEmployeeAccessGroup: sync add+remove, Keycloak failure still completes DB update, existing tests updated with IKeycloakUserService mock

## Decisions Made
- Keycloak group sync failures are caught and logged but NOT rethrown — eventual consistency pattern (D-13, D-15, D-16)
- GET /companies/me uses plain `[Authorize(AuthenticationSchemes = "BearerClient")]` — any authenticated client user can view their own profile, no specific permission needed
- POST /companies/registration has NO auth attribute — registration is public by design
- CrossCompanyAccess policy replaces `[Authorize(Roles = "admin")]` on AdminUserController — semantically equivalent (Policy uses RequireRole("admin")) but more consistent with the permission-based model

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

- Test mock setup for RegisterEmployeeCommandHandlerTests required using `AccessGroup.Create().Id` instead of a random GUID, because the handler resolves the access group by ID and the returned group's Id is the internally-generated one. Fixed test to use `viewerGroup.Id` consistently.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness
- All handler-to-Keycloak sync operations are in place (create groups on registration, add to group on employee creation, sync on group change)
- All controller endpoints enforce permission-based authorization policies
- Phase 39 complete — authorization pipeline from JWT → permissions → controller policy enforcement is fully wired
- Ready for Phase 40 (Client Frontend — PJ Registration & Employee Management)

## Self-Check: PASSED

- All 8 modified files exist on disk
- Both commits (eb4c97d, 874a9f6) found in git log
- dotnet build: 0 errors
- dotnet test: 204 unit tests pass (Domain.Tests), 85 pass (API.Tests), 4 skipped (pre-existing)
- RegisterCompanyCommandHandler calls CreateGroupAsync for admin-empresa, viewer, dashboard
- RegisterEmployeeCommandHandler calls AddUserToGroupAsync with employee's group
- ChangeEmployeeAccessGroupCommandHandler calls AddUserToGroupAsync + RemoveUserFromGroupAsync
- Group sync failures caught and logged, not rethrown
- CompaniesController endpoints use correct permission policies
- AdminUserController uses CrossCompanyAccess policy

---
*Phase: 39-keycloak-groups-permissions*
*Completed: 2026-04-26*