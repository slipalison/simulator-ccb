---
phase: 39-keycloak-groups-permissions
plan: 01
subsystem: auth
tags: [keycloak, groups, jwt, claims, authorization]

# Dependency graph
requires:
  - phase: 38-employee-registration-management-api
    provides: AccessGroup domain entity, IKeycloakUserService with user management, KeycloakUserService implementation
provides:
  - Keycloak client realm with 3 groups (admin-empresa, viewer, dashboard)
  - Group Membership mapper in roles client scope (JWT groups claim)
  - 4 group management methods on IKeycloakUserService
  - 8 unit tests for group operations
affects: [40-client-frontend-pj, 41-backoffice-employee-audit]

# Tech tracking
tech-stack:
  added: [oidc-group-membership-mapper]
  patterns: [idempotent Keycloak group operations, Keycloak Admin REST API for groups]

key-files:
  created:
    - tests/Onboarding.Domain.Tests/Infrastructure/KeycloakUserServiceGroupTests.cs
  modified:
    - keycloak/client-realm.json
    - src/Onboarding.Application/Common/IKeycloakUserService.cs
    - src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs
    - tests/Onboarding.Domain.Tests/Infrastructure/KeycloakUserServiceTests.cs

key-decisions:
  - "KeycloakGroupRepresentation made public (was internal) for test accessibility without InternalsVisibleTo"
  - "CreateGroupAsync extracts group ID from Location header first, falls back to GetGroupByNameAsync"
  - "Added RespondWithHeaders to MockHttpMessageHandler for testing Location header responses"

patterns-established:
  - "Idempotent group operations: CreateGroupAsync checks existence first, AddUserToGroupAsync ignores 409, RemoveUserFromGroupAsync ignores 404"
  - "Keycloak group API pattern: /admin/realms/{realm}/groups for CRUD, /admin/realms/{realm}/users/{id}/groups/{id} for membership"

requirements-completed: [PERM-01, PERM-02, PERM-03, PERM-04, PERM-05]

# Metrics
duration: 13min
completed: 2026-04-26
---

# Phase 39: Keycloak Groups & Permissions — Plan 01 Summary

**Keycloak groups provisioned in client realm with Group Membership JWT mapper; 4 group management API methods implemented and tested**

## Performance

- **Duration:** 13 min
- **Started:** 2026-04-26T14:26:10Z
- ** **Completed:** 2026-04-26T14:39:28Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments
- Provisioned 3 Keycloak groups (admin-empresa, viewer, dashboard) in client-realm.json
- Added Group Membership mapper to roles client scope — JWTs from client realm now include `groups` claim
- Extended IKeycloakUserService with 4 idempotent group management methods
- Fully implemented KeycloakUserService with Keycloak Admin REST API calls for group CRUD and membership
- 8 unit tests covering all group method scenarios including idempotency cases

## Task Commits

Each task was committed atomically:

1. **Task 1: Keycloak client-realm.json groups + Group Membership mapper** - `107f847` (feat)
2. **Task 2: Extend IKeycloakUserService with group management methods + implement in KeycloakUserService** - `0587af6` (feat)

**Plan metadata:** (pending)

## Files Created/Modified
- `keycloak/client-realm.json` — Added 3 groups + Group Membership mapper in roles scope
- `src/Onboarding.Application/Common/IKeycloakUserService.cs` — Added 4 group method signatures + KeycloakGroupRepresentation record
- `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs` — Implemented CreateGroupAsync, AddUserToGroupAsync, RemoveUserFromGroupAsync, GetGroupByNameAsync + KeycloakGroupRepresentation record
- `tests/Onboarding.Domain.Tests/Infrastructure/KeycloakUserServiceGroupTests.cs` — 8 unit tests for group operations
- `tests/Onboarding.Domain.Tests/Infrastructure/KeycloakUserServiceTests.cs` — Added RespondWithHeaders to MockHttpMessageHandler

## Decisions Made
- KeycloakGroupRepresentation made public instead of internal (avoids InternalsVisibleTo complexity, still a simple DTO)
- CreateGroupAsync tries Location header extraction first, falls back to GetGroupByNameAsync lookup (avoids unnecessary round trips)
- MockHttpMessageHandler extended with RespondWithHeaders for testing HTTP headers like Location

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- Client realm has 3 groups provisioned; JWTs will include `groups` claim
- IKeycloakUserService has group management methods ready for downstream handlers
- ChangeEmployeeAccessGroupCommandHandler and RegisterEmployeeCommandHandler can now use AddUserToGroupAsync/RemoveUserFromGroupAsync
- Ready for Plan 02: JWT claims transformation and permission-based authorization policies

## Self-Check: PASSED

- All 5 created/modified files exist on disk
- Both commits (107f847, 0587af6) found in git log
- dotnet build: 0 errors
- dotnet test: 198 tests pass (including 8 new group tests)
- client-realm.json has 3 groups + Group Membership mapper
- backoffice-realm.json unchanged (hash matches)

---
*Phase: 39-keycloak-groups-permissions*
*Completed: 2026-04-26*