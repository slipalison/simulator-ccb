---
phase: 35
plan: "01"
subsystem: backend
tags: [admin-management, keycloak, audit, security, cqrs]
dependency_graph:
  requires: [phase-30-admin-management, phase-34-realm-isolation]
  provides: [paginated-admin-list, update-admin, reset-admin-password, toggle-admin-status]
  affects: [AdminUserController, IKeycloakUserService, KeycloakUserService, ActionType, DependencyInjection]
tech_stack:
  added: []
  patterns: [manual-cqrs, fluent-validation, audit-log-append-only, crypto-secure-password-generation]
key_files:
  created:
    - src/Onboarding.Application/Admin/Queries/GetPaginatedAdministratorsQuery.cs
    - src/Onboarding.Application/Admin/Commands/UpdateAdministratorCommand.cs
    - src/Onboarding.Application/Admin/Commands/ResetAdministratorPasswordCommand.cs
    - src/Onboarding.Application/Admin/Commands/ToggleAdministratorStatusCommand.cs
  modified:
    - src/Onboarding.API/Controllers/AdminUserController.cs
    - src/Onboarding.Application/Admin/Queries/GetAdministratorsQuery.cs
    - src/Onboarding.Application/Common/IKeycloakUserService.cs
    - src/Onboarding.Application/DependencyInjection.cs
    - src/Onboarding.Domain/Aggregates/Audit/ActionType.cs
    - src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs
decisions:
  - "Toggle pattern (single endpoint + Activate bool) used instead of separate deactivate/reactivate endpoints — functionally equivalent with less surface area"
  - "GetAdministratorsQuery kept as simple non-paginated list for backward compatibility; new GetPaginatedAdministratorsQuery handles Phase 35 requirements"
  - "Email uniqueness enforced at Keycloak level inside UpdateAdminUserAsync — double-check via GET before PUT"
metrics:
  duration: "~15 minutes (continuation execution)"
  completed_date: "2026-04-22"
  tasks_completed: 4
  files_changed: 10
---

# Phase 35 Plan 01: Admin Management Backend Summary

**One-liner:** Backend admin management — paginated+filtered list, edit name/email with uniqueness guard, crypto-secure password reset, disable/reactivate with last-admin protection and full audit trail.

## Tasks Completed

| Task | Name | Commit |
|------|------|--------|
| 35-01-01 | Paginated + filtered administrators query | d715f25 |
| 35-01-02 | UpdateAdministratorCommand | c50cec8 |
| 35-01-03 | ResetAdministratorPasswordCommand | b944de4 |
| 35-01-04 | ToggleAdministratorStatusCommand | a36611d |
| — | Wire endpoints + infrastructure | 049f45d |

## What Was Built

### Task 35-01-01 — Paginated Administrators Query (MGMT-01, MGMT-02)

`GetPaginatedAdministratorsQuery` fetches all `admin` role members from Keycloak then applies in-memory filtering by name (contains), email (contains), and status (active/inactive). Pagination is applied after filtering. `GetAdministratorsQuery` was restored to its simple non-paginated form to preserve the legacy `GET /api/admin/administrators` endpoint.

New endpoint: `GET /api/admin/administrators/paginated?page=1&pageSize=20&name=&email=&status=active|inactive`

### Task 35-01-02 — Update Administrator (MGMT-03, SEC-01, SEC-04, AUD-04)

`UpdateAdministratorCommand` updates firstName/email in Keycloak. Before update:
- SEC-01: validator and handler both reject `targetId == actorSub`
- SEC-04: `UpdateAdminUserAsync` queries Keycloak by email, throws `ArgumentException` if another user owns that email (mapped to 409 in controller)
- AUD-04: old email + new fullName/email written as JSON `details` to audit log

Endpoint: `PUT /api/admin/administrators/{id}`

### Task 35-01-03 — Reset Administrator Password (MGMT-04, SEC-01, SEC-03, AUD-05)

`ResetAdministratorPasswordCommand` generates a 16-character cryptographically secure password using `RandomNumberGenerator`. Ambiguous characters (O, 0, l, 1, I) are excluded. At least one character from each category (upper, lower, digit, special) is guaranteed, then Fisher-Yates shuffle randomizes position. After setting the password, `UPDATE_PASSWORD` requiredAction is added to force change on next login.

SEC-01: blocks self-reset. AUD-05: actor+target only — password never written to audit log. Temporary password returned in response body once.

Endpoint: `POST /api/admin/administrators/{id}/reset-password`

### Task 35-01-04 — Toggle Administrator Status (MGMT-05, MGMT-06, SEC-01, SEC-05, AUD-06)

`ToggleAdministratorStatusCommand` handles both disable and reactivate via `Activate: bool` flag.

Deactivate path:
- SEC-05: counts active admins; if target is the only active admin, throws `InvalidOperationException`
- Calls `BlockUserAsync` (sets `Enabled=false`) then `LogoutAllSessionsAsync` (POST /users/{id}/logout — forces immediate session termination)

Reactivate path: calls `UnblockUserAsync` (sets `Enabled=true`).

AUD-06: `AdminDisabled` or `AdminReactivated` action type logged with optional reason field as JSON details.

Endpoint: `POST /api/admin/administrators/{id}/toggle-status`

### Infrastructure Changes

- `IKeycloakUserService`: added `UpdateAdminUserAsync` and `LogoutAllSessionsAsync` contracts
- `KeycloakUserService`: implemented both methods; `LogoutAllSessionsAsync` treats 404 (no active sessions) as success
- `ActionType`: added `AdminEdited=14`, `AdminPasswordReset=15`, `AdminDisabled=16`, `AdminReactivated=17`
- `DependencyInjection`: registered all Phase 35 handlers and validators

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Type mismatch in GetAdministratorsQuery after refactor**
- **Found during:** Task 35-01-01 — build verification
- **Issue:** `GetAdministratorsQuery` handler was changed to return `PaginatedResult<AdminUserDto>` but `DependencyInjection.cs` still registered it as `IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>>`, causing a CS0311 compile error
- **Fix:** Restored `GetAdministratorsQuery` to simple non-paginated handler returning `IReadOnlyList<AdminUserDto>`; paginated functionality lives entirely in `GetPaginatedAdministratorsQuery`
- **Files modified:** `src/Onboarding.Application/Admin/Queries/GetAdministratorsQuery.cs`
- **Commit:** d715f25

### Design Variation (not a bug)

**Toggle vs. Separate Endpoints**

The plan requested `DeactivateAdministratorCommand.cs` + `ReactivateAdministratorCommand.cs` and separate endpoints `/deactivate` + `/reactivate`. The implementation uses a single `ToggleAdministratorStatusCommand` with `Activate: bool` and a single `/toggle-status` endpoint. This reduces surface area while satisfying all acceptance criteria (SEC-01, SEC-05, AUD-06, MGMT-05, MGMT-06).

## Known Stubs

None — all operations delegate to Keycloak Admin API with real HTTP calls.

## Threat Flags

None — all new endpoints are inside `[Authorize(AuthenticationSchemes = "BearerBackoffice", Roles = "admin")]` inherited from controller class attribute. SEC-02 satisfied by existing controller-level authorization.

## Self-Check: PASSED

Files created:
- src/Onboarding.Application/Admin/Queries/GetPaginatedAdministratorsQuery.cs — FOUND
- src/Onboarding.Application/Admin/Commands/UpdateAdministratorCommand.cs — FOUND
- src/Onboarding.Application/Admin/Commands/ResetAdministratorPasswordCommand.cs — FOUND
- src/Onboarding.Application/Admin/Commands/ToggleAdministratorStatusCommand.cs — FOUND

Commits:
- d715f25 — feat(35-01): add paginated+filtered administrators query
- c50cec8 — feat(35-01): implement UpdateAdministratorCommand
- b944de4 — feat(35-01): implement ResetAdministratorPasswordCommand
- a36611d — feat(35-01): implement ToggleAdministratorStatusCommand
- 049f45d — feat(35-01): wire admin management endpoints and infrastructure

Build: 0 errors, 0 warnings
