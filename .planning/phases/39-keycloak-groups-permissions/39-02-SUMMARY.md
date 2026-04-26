---
phase: 39-keycloak-groups-permissions
plan: 02
subsystem: auth
tags: [permissions, claims-transformation, middleware, authorization-policies, jwt-groups]

# Dependency graph
requires:
  - phase: 38-employee-registration-management-api
    provides: AccessGroup domain entity, IEmployeeRepository, ICompanyRepository, IAccessGroupRepository
  - phase: 39-01
    provides: IKeycloakUserService group methods, client-realm.json groups, KeycloakGroupRepresentation
provides:
  - ICurrentCompanyPermissionsService interface with CompanyId, Permissions, IsCompanyOwner
  - CurrentCompanyPermissionsService scoped implementation
  - GroupsClaimsTransformation extracting JWT groups → ClaimTypes.Role
  - PermissionAuthorizationHandler checking ICurrentCompanyPermissionsService.Permissions
  - PermissionPolicies constants (7 policies: 6 resources + CrossCompanyAccess)
  - ClientClaimsMiddleware resolving JWT sub → Company/Employee → permissions per request
  - Authorization policies registered in Program.cs
  - Unit tests for PermissionAuthorizationHandler and ClientClaimsMiddleware
affects: [40-client-frontend-pj, 41-backoffice-employee-audit]

# Tech tracking
tech-stack:
  added: [Microsoft.AspNetCore.Authorization (PermissionRequirement), IClaimsTransformation (GroupsClaimsTransformation)]
  patterns: [permission-based authorization via custom AuthorizationHandler, scoped per-request permissions service set by middleware, claims transformation from JWT groups to ClaimTypes.Role]

key-files:
  created:
    - src/Onboarding.Application/Common/ICurrentCompanyPermissionsService.cs
    - src/Onboarding.Infrastructure/Persistence/CurrentCompanyPermissionsService.cs
    - src/Onboarding.API/Security/GroupsClaimsTransformation.cs
    - src/Onboarding.API/Security/PermissionAuthorizationHandler.cs
    - src/Onboarding.API/Security/PermissionPolicyConstants.cs
    - src/Onboarding.API/Middleware/ClientClaimsMiddleware.cs
    - tests/Onboarding.API.Tests/Security/PermissionAuthorizationHandlerTests.cs
    - tests/Onboarding.API.Tests/Middleware/ClientClaimsMiddlewareTests.cs
  modified:
    - src/Onboarding.API/Program.cs
    - src/Onboarding.Infrastructure/DependencyInjection.cs

key-decisions:
  - "Concrete CurrentCompanyService/CurrentCompanyPermissionsService cast in middleware — API project references Infrastructure directly for DI wiring"
  - "PermissionAuthorizationHandler does NOT call Fail() — requirement simply remains pending if permission not found (standard ASP.NET Core behavior)"
  - "GroupsClaimsTransformation handles both array and string values for groups claim (Keycloak may return either)"
  - "CrossCompanyAccess policy uses RequireRole('admin') instead of PermissionRequirement — backoffice realm uses realm_access.roles not groups"

patterns-established:
  - "Permission-based authorization: custom PermissionRequirement + PermissionAuthorizationHandler checking ICurrentCompanyPermissionsService"
  - "Per-request scoped service populated by middleware: CurrentCompanyPermissionsService set by ClientClaimsMiddleware in request pipeline"
  - "JWT sub → DB lookup → permissions resolution: middleware queries Company/Employee by KeycloakUserId, then AccessGroup for permissions"

requirements-completed: [PERM-01, PERM-02, PERM-03, PERM-04, PERM-05]

# Metrics
duration: 37min
completed: 2026-04-26
---

# Phase 39: Keycloak Groups & Permissions — Plan 02 Summary

**JWT claims transformation, permission middleware, and authorization pipeline connecting JWT groups → DB permissions → policy enforcement**

## Performance

- **Duration:** 37 min
- **Started:** 2026-04-26T14:42:49Z
- **Completed:** 2026-04-26T15:20:02Z
- **Tasks:** 2
- **Files modified:** 10

## Accomplishments
- ICurrentCompanyPermissionsService interface exposing CompanyId, Permissions list, and IsCompanyOwner flag
- CurrentCompanyPermissionsService scoped service populated per-request by middleware
- GroupsClaimsTransformation extracting JWT `groups` claim → ClaimTypes.Role for BearerClient authorization
- PermissionAuthorizationHandler checking permissions from ICurrentCompanyPermissionsService against required permission string
- 7 authorization policies (EmployeeRead, EmployeeWrite, EmployeeDelete, AuditRead, DashboardAccess, AccessGroupsManage + CrossCompanyAccess for admin role)
- ClientClaimsMiddleware resolving JWT sub → Company/Employee → AccessGroup → permissions per request
- Company isolation via HasQueryFilter now functional (CompanyId set from JWT claims)
- PJ owner (Company.KeycloakUserId == sub) gets all 6 permissions + IsCompanyOwner=true
- 12 unit tests covering all permission scenarios

## Task Commits

Each task was committed atomically:

1. **Task 1: ICurrentCompanyPermissionsService + GroupsClaimsTransformation + PermissionAuthorizationHandler** - `39e661a` (feat)
2. **Task 2: ClientClaimsMiddleware + Program.cs wiring + integration** - `f6f3e73` (feat)

## Files Created/Modified
- `src/Onboarding.Application/Common/ICurrentCompanyPermissionsService.cs` — Interface with CompanyId, Permissions, IsCompanyOwner
- `src/Onboarding.Infrastructure/Persistence/CurrentCompanyPermissionsService.cs` — Scoped implementation with settable properties
- `src/Onboarding.API/Security/GroupsClaimsTransformation.cs` — IClaimsTransformation extracting groups → Role claims
- `src/Onboarding.API/Security/PermissionAuthorizationHandler.cs` — AuthorizationHandler checking Permissions.Contains()
- `src/Onboarding.API/Security/PermissionPolicyConstants.cs` — 7 policy name constants
- `src/Onboarding.API/Middleware/ClientClaimsMiddleware.cs` — Middleware resolving JWT sub → Company/Employee → permissions
- `src/Onboarding.API/Program.cs` — Added UseClientClaims(), authorization policies, DI registrations
- `src/Onboarding.Infrastructure/DependencyInjection.cs` — Added ICurrentCompanyPermissionsService scoped registration
- `tests/Onboarding.API.Tests/Security/PermissionAuthorizationHandlerTests.cs` — 5 unit tests for handler
- `tests/Onboarding.API.Tests/Middleware/ClientClaimsMiddlewareTests.cs` — 7 unit tests for middleware

## Decisions Made
- Concrete CurrentCompanyService/CurrentCompanyPermissionsService cast in middleware — API project references Infrastructure for DI wiring, consistent with existing pattern of scoped service population
- PermissionAuthorizationHandler does NOT call Fail() — requirement remains pending if permission not found, consistent with ASP.NET Core authorization behavior
- GroupsClaimsTransformation handles both array and string values for groups claim
- CrossCompanyAccess policy uses RequireRole("admin") instead of PermissionRequirement — backoffice realm uses realm_access.roles not groups

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered
- Test file initially placed in Domain.Tests (wrong project — needs ASP.NET Core Authorization references). Moved to API.Tests which references the API project. This is a standard .NET layering decision.

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- ClientClaimsMiddleware fully resolves JWT sub → Company/Employee → permissions per request
- 7 authorization policies registered and functional
- Company isolation now functional (HasQueryFilter gets real CompanyId from middleware)
- Ready for Plan 03: handler extensions (ChangeEmployeeAccessGroupCommandHandler sync to Keycloak, RegisterEmployeeCommandHandler group assignment)

## Self-Check: PASSED

- All 8 created files exist on disk
- All 2 modified files exist on disk
- Both commits (39e661a, f6f3e73) found in git log
- dotnet build: 0 errors
- dotnet test: 283 unit tests pass (198 Domain + 85 API), 4 skipped (pre-existing)
- ClientClaimsMiddleware.cs between UseAuthentication and UseAuthorization in Program.cs
- 7 authorization policies registered in Program.cs
- GroupsClaimsTransformation registered as IClaimsTransformation
- ICurrentCompanyPermissionsService registered as scoped in DI

---
*Phase: 39-keycloak-groups-permissions*
*Completed: 2026-04-26*