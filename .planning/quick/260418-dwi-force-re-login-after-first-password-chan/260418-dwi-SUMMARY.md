---
phase: quick/260418-dwi
plan: 01
status: completed
started: 2026-04-18T19:19:00-03:00
completed: 2026-04-18T19:44:00-03:00
commit: fa4ef9b
---

# Summary — Force re-login after first password change

## What was built

Full-stack implementation that forces admin users to re-login after completing their first password change (UPDATE_PASSWORD flow). When a new admin completes the Keycloak password update during Auth Code Flow, the session started with the temporary credential is invalidated — the admin must re-enter with the definitive password.

## Changes

### Backend (.NET)

| File | Change |
|------|--------|
| `IKeycloakUserService.cs` | Added `ClearFirstLoginFlagAsync(userId, ct)` signature |
| `KeycloakUserService.cs` | Implemented `ClearFirstLoginFlagAsync` (GET → mutate attributes → PUT, idempotent) |
| `AdminUserController.cs` | Added `POST /api/admin/me/complete-first-login` endpoint (204, `[Authorize(Roles = "admin")]`) |

**Note:** `CreateAdminUserAsync` already sets `Attributes["isFirstLogin"] = ["true"]` — no change needed there.

### Keycloak

| File | Change |
|------|--------|
| `onboarding-realm.json` | Added `protocolMappers` to `onboarding-backoffice` client — `isFirstLogin` attribute mapper (access token only) |

### Frontend

| File | Change |
|------|--------|
| `auth-server.ts` | `/callback` handler: decode access token → detect `isFirstLogin === "true"` → call backend (best-effort) → clear cookies → redirect `/admin/login` |

### Tests

| File | Tests | Type |
|------|-------|------|
| `KeycloakUserServiceFirstLoginTests.cs` | 3 tests (attribute=true → update, absent → no-op, already=false → no-op) | Unit |
| `AdminFirstLoginEndpointTests.cs` | 3 tests (401 unauthenticated, 403 non-admin, 204 admin + mock verify) | Integration |

## key-files

### created

- `tests/Onboarding.Domain.Tests/Application/Commands/KeycloakUserServiceFirstLoginTests.cs`
- `tests/Onboarding.API.Tests/Admin/AdminFirstLoginEndpointTests.cs`

### modified

- `src/Onboarding.Application/Common/IKeycloakUserService.cs`
- `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs`
- `src/Onboarding.API/Controllers/AdminUserController.cs`
- `keycloak/onboarding-realm.json`
- `frontend/backoffice/auth-server.ts`

## Verification

- ✅ `dotnet build src/Onboarding.API/Onboarding.API.csproj` — compiles without errors
- ✅ Unit tests: 3/3 passed (`KeycloakUserServiceFirstLoginTests`)
- ✅ Integration tests: 3/3 passed (`AdminFirstLoginEndpointTests`)
- ✅ Realm JSON valid (parseable)
- ✅ Protocol mapper correctly configured (access.token.claim=true, id.token.claim=false)
- ✅ auth-server.ts contains isFirstLogin detection + complete-first-login call + /admin/login redirect

## Self-Check: PASSED

## Deviations

None — all plan specifications honored as-is.

## Design Decisions

- **Backend URL in callback:** Used `http://api:8080` (Docker internal hostname from `server.ts` proxy pattern) instead of `BACKEND_URL` env var, consistent with existing proxy handler.
- **Defensive boolean check:** `payload.isFirstLogin === "true" || payload.isFirstLogin === true` — handles both string and boolean mapper output.
- **Idempotency:** `ClearFirstLoginFlagAsync` checks attribute value before calling UpdateUserAsync, avoiding unnecessary Keycloak API calls.
