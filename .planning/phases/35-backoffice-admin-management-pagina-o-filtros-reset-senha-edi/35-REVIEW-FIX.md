---
phase: 35-backoffice-admin-management-pagina-o-filtros-reset-senha-edi
fixed_at: 2026-04-22T00:00:00Z
review_path: .planning/phases/35-backoffice-admin-management-pagina-o-filtros-reset-senha-edi/35-REVIEW.md
iteration: 1
findings_in_scope: 6
fixed: 6
skipped: 0
status: all_fixed
---

# Phase 35: Code Review Fix Report

**Fixed at:** 2026-04-22T00:00:00Z
**Source review:** .planning/phases/35-backoffice-admin-management-pagina-o-filtros-reset-senha-edi/35-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 6 (2 Critical, 4 Warning)
- Fixed: 6
- Skipped: 0

## Fixed Issues

### CR-01: Race condition in last-admin guard (SEC-05)

**Files modified:** `src/Onboarding.Application/Admin/Commands/ToggleAdministratorStatusCommand.cs`
**Commit:** de66f51
**Applied fix:** After calling `BlockUserAsync`, re-fetches the active admin list from Keycloak. If the post-disable list has no enabled admins, immediately calls `UnblockUserAsync` to roll back the disable and throws `InvalidOperationException`. `LogoutAllSessionsAsync` is only called after the post-check passes, ensuring sessions are only revoked when the disable is confirmed safe.

---

### CR-02: Password reset sets `temporary = false` then adds UPDATE_PASSWORD separately — two-call gap

**Files modified:** `src/Onboarding.Application/Common/IKeycloakUserService.cs`, `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs`, `src/Onboarding.Application/Admin/Commands/ResetAdministratorPasswordCommand.cs`
**Commit:** 147cdb9
**Applied fix:** Added `ResetPasswordAsTemporaryAsync` to `IKeycloakUserService` and implemented it in `KeycloakUserService` as a single `PUT reset-password` call with `temporary: true`. Updated `ResetAdministratorPasswordCommandHandler` to call this new method instead of the previous two-call sequence (`UpdateUserPasswordAsync` + `SetTemporaryPasswordFlagAsync`), eliminating the window where the password was briefly set as permanent. The dead `passwordPayload` variable (IN-02) was also removed as part of this replacement.

---

### WR-01: No upper bound on `pageSize` — allows unbounded data export

**Files modified:** `src/Onboarding.Application/Admin/Queries/GetPaginatedAdministratorsQuery.cs`
**Commit:** 5e47810
**Applied fix:** Changed `query.PageSize > 0 ? query.PageSize : 20` to `query.PageSize > 0 ? Math.Min(query.PageSize, 100) : 20`, capping pageSize at 100.

---

### WR-02: Admin endpoints accept `{id}` as unconstrained string

**Files modified:** `src/Onboarding.API/Controllers/AdminUserController.cs`
**Commit:** 40416c9
**Applied fix:** Added `:guid` route constraint to all three admin management routes: `administrators/{id:guid}` (PUT), `administrators/{id:guid}/reset-password` (POST), and `administrators/{id:guid}/toggle-status` (POST). Non-GUID values are now rejected at routing with a 404 before the action body is reached. Parameter type kept as `string` to avoid cascading changes to command constructors.

---

### WR-03: Audit diff for `UpdateAdministrator` omits old `FullName`

**Files modified:** `src/Onboarding.Application/Common/IKeycloakUserService.cs`, `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs`, `src/Onboarding.Application/Admin/Commands/UpdateAdministratorCommand.cs`
**Commit:** da21d46
**Applied fix:** Extended `KeycloakUserDetails` record with a `FullName` property (default `""`). Updated `GetUserByIdAsync` in `KeycloakUserService` to populate `FullName` via the existing `BuildFullName` helper. Updated the audit diff in `UpdateAdministratorCommandHandler` to include `old.fullName = current.FullName` alongside `old.email`.

---

### WR-04: `a.Email` not null-guarded in paginated filter

**Files modified:** `src/Onboarding.Application/Admin/Queries/GetPaginatedAdministratorsQuery.cs`
**Commit:** c1d276c
**Applied fix:** Changed `a.Email.Contains(...)` to `(a.Email ?? string.Empty).Contains(...)` to guard against a potential `NullReferenceException` if `Email` is ever null.

---

_Fixed: 2026-04-22T00:00:00Z_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
