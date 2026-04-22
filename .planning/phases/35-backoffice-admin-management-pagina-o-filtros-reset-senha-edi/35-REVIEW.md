---
phase: 35-backoffice-admin-management-pagina-o-filtros-reset-senha-edi
reviewed: 2026-04-22T00:00:00Z
depth: standard
files_reviewed: 10
files_reviewed_list:
  - src/Onboarding.Application/Admin/Queries/GetPaginatedAdministratorsQuery.cs
  - src/Onboarding.Application/Admin/Commands/UpdateAdministratorCommand.cs
  - src/Onboarding.Application/Admin/Commands/ResetAdministratorPasswordCommand.cs
  - src/Onboarding.Application/Admin/Commands/ToggleAdministratorStatusCommand.cs
  - src/Onboarding.API/Controllers/AdminUserController.cs
  - src/Onboarding.Application/Admin/Queries/GetAdministratorsQuery.cs
  - src/Onboarding.Application/Common/IKeycloakUserService.cs
  - src/Onboarding.Application/DependencyInjection.cs
  - src/Onboarding.Domain/Aggregates/Audit/ActionType.cs
  - src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs
findings:
  critical: 2
  warning: 4
  info: 3
  total: 9
status: issues_found
---

# Phase 35: Code Review Report

**Reviewed:** 2026-04-22T00:00:00Z
**Depth:** standard
**Files Reviewed:** 10
**Status:** issues_found

## Summary

These files implement the Phase 35 admin management backend: paginated/filtered administrator listing (MGMT-01, MGMT-02), edit name/email (MGMT-03), reset password (MGMT-04), and toggle active status (MGMT-05/06). The security-critical features — SEC-01 self-modification guard, SEC-03 password generation, SEC-04 email uniqueness, SEC-05 last-admin guard — are all present and correctly implemented at the business logic layer.

Two critical issues were found. The most severe is a race condition in the last-admin guard (SEC-05): the check-then-act pattern in `ToggleAdministratorStatusCommandHandler` is not atomic, leaving a window where two concurrent requests can both pass the guard and disable all admins. The second critical issue is that `UpdateUserPasswordAsync` (called in `ResetAdministratorPasswordCommand`) sets `temporary = false` at the infrastructure level, contradicting the `SetTemporaryPasswordFlagAsync` call that follows and meaning the reset password is set as permanent before the required-action is added — creating a brief window where it is not temporary.

Four warnings cover: unvalidated `pageSize` ceiling (allows unlimited data export), missing 404 route constraint on admin edit/reset/toggle endpoints, missing audit of the old full name in the edit diff, and a null-safety gap on `a.Email` in the paginated filter.

---

## Critical Issues

### CR-01: Race condition in last-admin guard (SEC-05) — two concurrent disables can lock out all admins

**File:** `src/Onboarding.Application/Admin/Commands/ToggleAdministratorStatusCommand.cs:60-69`

**Issue:** The SEC-05 guard reads all active admins from Keycloak (`GetUsersByRoleAsync`), counts them, and only then calls `BlockUserAsync`. Because Keycloak is not transactional across these two calls, two simultaneous requests targeting different admins can both read a count of 2, both pass the guard, and both disable their targets — leaving zero active administrators.

The risk is real in multi-tab or concurrent API usage. An attacker with access to two admin sessions could intentionally exploit this to achieve a full lockout.

**Fix:** Add a Keycloak-side re-verification immediately before `BlockUserAsync`, or serialize disable operations with a distributed lock. The simplest safe approach is to re-fetch the count _after_ the disable and roll back (re-enable) if the post-check shows zero active admins:

```csharp
await _keycloakUserService.BlockUserAsync("backoffice", command.TargetKeycloakId, ct);

// Post-check: if we just disabled the last admin, immediately roll back
var afterDisable = await _keycloakUserService.GetUsersByRoleAsync("backoffice", "admin", ct);
if (!afterDisable.Any(a => a.IsEnabled))
{
    await _keycloakUserService.UnblockUserAsync("backoffice", command.TargetKeycloakId, ct);
    throw new InvalidOperationException(
        "Cannot disable the last active administrator. At least one active administrator must remain.");
}
```

This compensating approach is safe without requiring a distributed lock and is idiomatic for Keycloak-backed services.

---

### CR-02: Password reset sets `temporary = false` in infrastructure, then adds UPDATE_PASSWORD action separately — two-call gap

**File:** `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs:157-168` and `src/Onboarding.Application/Admin/Commands/ResetAdministratorPasswordCommand.cs:76-77`

**Issue:** `UpdateUserPasswordAsync` hard-codes `temporary = false` in the `reset-password` payload (line 158 of `KeycloakUserService.cs`). `ResetAdministratorPasswordCommand` then calls `SetTemporaryPasswordFlagAsync` as a separate request to add `UPDATE_PASSWORD` to the user's required actions (line 77 of the command). Between the two calls there is a window where the admin can log in with the new password and _not_ be forced to change it — the `UPDATE_PASSWORD` action has not been added yet.

Additionally, `SetTemporaryPasswordFlagAsync` fails silently if Keycloak returns an error on the `PUT /users/{id}` call — the response is never checked (line 221 of `KeycloakUserService.cs`).

**Fix — preferred:** Add a dedicated `ResetPasswordAsTemporaryAsync` method that issues a single `PUT reset-password` call with `temporary: true`, which atomically sets the password _and_ adds `UPDATE_PASSWORD` in one Keycloak transaction. Keep `UpdateUserPasswordAsync` as-is for permanent password changes.

```csharp
// In IKeycloakUserService
Task ResetPasswordAsTemporaryAsync(string targetRealm, string userId, string newPassword, CancellationToken ct = default);

// In KeycloakUserService
public async Task ResetPasswordAsTemporaryAsync(string targetRealm, string userId, string newPassword, CancellationToken ct = default)
{
    var payload = new { type = "password", value = newPassword, temporary = true };
    var response = await GetClient(targetRealm).PutAsJsonAsync(
        $"admin/realms/{targetRealm}/users/{userId}/reset-password", payload, ct);
    response.EnsureSuccessStatusCode();
}
```

Then replace the two-call sequence in `ResetAdministratorPasswordCommandHandler` with a single call to `ResetPasswordAsTemporaryAsync`.

**Fix — minimal (check the PUT response in `SetTemporaryPasswordFlagAsync`):**

```csharp
// KeycloakUserService.cs line ~221 — check result of PutAsJsonAsync
var putResp = await client.PutAsJsonAsync($"admin/realms/{targetRealm}/users/{userId}", user, ct);
if (!putResp.IsSuccessStatusCode)
{
    var body = await putResp.Content.ReadAsStringAsync(ct);
    throw new InvalidOperationException($"Failed to set UPDATE_PASSWORD required action for user '{userId}': {body}");
}
```

---

## Warnings

### WR-01: No upper bound on `pageSize` — allows unbounded data export

**File:** `src/Onboarding.Application/Admin/Queries/GetPaginatedAdministratorsQuery.cs:50`

**Issue:** The handler clamps `pageSize` to 20 when it is `<= 0`, but imposes no maximum. A caller passing `pageSize=10000` will receive all administrators in a single response, bypassing pagination. While the in-memory filtering over Keycloak role members is noted as acceptable for small admin sets, an uncapped pageSize is a latent denial-of-service vector if the admin list ever grows, and is also an information-exposure surface.

**Fix:**

```csharp
var pageSize = query.PageSize > 0 ? Math.Min(query.PageSize, 100) : 20;
```

---

### WR-02: Admin endpoints accept `{id}` as unconstrained string — no GUID validation at route level

**File:** `src/Onboarding.API/Controllers/AdminUserController.cs:423, 462, 496`

**Issue:** `PUT /api/admin/administrators/{id}`, `POST /api/admin/administrators/{id}/reset-password`, and `POST /api/admin/administrators/{id}/toggle-status` bind `id` as `string` with no route constraint (`{id:guid}` is absent). Any string reaches the command handler, which then attempts a Keycloak lookup with whatever value was passed. Malformed IDs cause confusing 404 responses rather than 400s, and long/crafted strings are forwarded in URL-encoded form to the Keycloak Admin API.

**Fix:** Either add `:guid` constraint to the route and change the parameter to `Guid` (matching the pattern used by existing user endpoints), or add explicit GUID format validation in the validator:

```csharp
// Option A — route constraint (preferred)
[HttpPut("administrators/{id:guid}")]
public async Task<IActionResult> UpdateAdministrator([FromRoute] Guid id, ...)

// Option B — validator rule
RuleFor(x => x.TargetKeycloakId)
    .Must(id => Guid.TryParse(id, out _))
    .WithMessage("TargetKeycloakId must be a valid GUID.");
```

---

### WR-03: Audit diff for `UpdateAdministrator` omits old `FullName` — incomplete change record (AUD-04)

**File:** `src/Onboarding.Application/Admin/Commands/UpdateAdministratorCommand.cs:69-73`

**Issue:** The audit payload records `old = { email }` but not `old.fullName`. The comment states "AUD-04: old/new snapshot" yet only the email is captured from the fetched `current` state, so a name-only edit produces a diff that shows no change in the `old` object.

Note: `KeycloakUserDetails` does not expose `FullName` as a field today — it would need to be added. However the omission means the audit trail cannot answer "what was the name before it was changed?", which is a compliance gap.

**Fix:** Extend `KeycloakUserDetails` with a `FullName` property (populated from `BuildFullName` in the service), then include it in the diff:

```csharp
var details = JsonSerializer.Serialize(new
{
    old = new { fullName = current.FullName, email = current.Email },
    @new = new { fullName = command.FullName, email = command.Email }
});
```

---

### WR-04: `a.Email` is not null-guarded in paginated filter — NullReferenceException possible

**File:** `src/Onboarding.Application/Admin/Queries/GetPaginatedAdministratorsQuery.cs:39-41`

**Issue:** The email filter calls `a.Email.Contains(...)` directly. `AdminUserDto.Email` is constructed in `GetUsersByRoleAsync` as `u.Email ?? u.Username ?? string.Empty`, so it should ordinarily be non-null, but if a future change relaxes that fallback or the DTO is constructed differently, this line will throw `NullReferenceException` at runtime. The name filter (line 36) has the same potential concern for `a.FullName`, but `BuildFullName` always returns a non-null string so that is safe.

**Fix:**

```csharp
filtered = filtered.Where(a =>
    (a.Email ?? string.Empty).Contains(query.Email, StringComparison.OrdinalIgnoreCase));
```

---

## Info

### IN-01: `GetAuditContextSafe` falls back to `email` as the `sub` claim — may silently produce incorrect audit actor

**File:** `src/Onboarding.API/Controllers/AdminUserController.cs:617-626`

**Issue:** When the `sub` JWT claim is absent, `GetAuditContextSafe` falls back first to `preferred_username`, then to `email`. The `sub` claim is always present in a valid Keycloak JWT, so the fallback is defensive, but if it ever fires, audit records for that request will have an email address stored as `actorSub`, which is semantically incorrect (sub is a UUID). This could corrupt audit queries that join on actor identity.

**Fix:** Log a warning if the fallback fires so the gap is visible, and consider throwing rather than silently degrading for new Phase-35 endpoints:

```csharp
private (string Sub, string Email) GetAuditContextSafe()
{
    var sub = User.FindFirst("sub")?.Value;
    var email = HttpContext.Items["AdminEmail"] as string
        ?? User.FindFirst("email")?.Value
        ?? "unknown";

    if (sub is null)
        _logger.LogWarning("JWT 'sub' claim missing for request {Path}; using email as fallback actor sub.", HttpContext.Request.Path);

    return (sub ?? User.FindFirst("preferred_username")?.Value ?? email, email);
}
```

---

### IN-02: `passwordPayload` variable declared but never used in `ResetAdministratorPasswordCommandHandler`

**File:** `src/Onboarding.Application/Admin/Commands/ResetAdministratorPasswordCommand.cs:72`

**Issue:** Line 72 declares `var passwordPayload = new { type = "password", value = temporaryPassword, temporary = true };` but this object is never read — the actual Keycloak call is made via `UpdateUserPasswordAsync` which constructs its own payload internally. This is dead code and may mislead readers into thinking the `temporary = true` flag is being used.

**Fix:** Remove the unused variable declaration entirely.

---

### IN-03: `BlockUserAsync` and `UnblockUserAsync` silently swallow the HTTP response from `PutAsJsonAsync`

**File:** `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs:179, 191`

**Issue:** Both methods call `client.PutAsJsonAsync(...)` but do not check `IsSuccessStatusCode` or call `EnsureSuccessStatusCode()`. If Keycloak rejects the update (e.g., 4xx or 5xx), the caller receives no error and the account status has not actually changed — yet the code continues as if it succeeded. For `BlockUserAsync` this is particularly important because `ToggleAdministratorStatusCommandHandler` proceeds to call `LogoutAllSessionsAsync` assuming the block succeeded.

**Fix:**

```csharp
// In BlockUserAsync
var putResp = await client.PutAsJsonAsync($"admin/realms/{targetRealm}/users/{keycloakUserId}", user, ct);
putResp.EnsureSuccessStatusCode();

// In UnblockUserAsync
var putResp = await client.PutAsJsonAsync($"admin/realms/{targetRealm}/users/{keycloakUserId}", user, ct);
putResp.EnsureSuccessStatusCode();
```

---

_Reviewed: 2026-04-22T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
