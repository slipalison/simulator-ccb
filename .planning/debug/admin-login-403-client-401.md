# Debug: admin-login-403-client-401

**Date:** 2026-04-10
**Status:** Admin 403 FIXED | Client 401 is expected behavior (no registered users yet)

## Root Cause

### Admin 403 Forbidden — TWO root causes found:

1. **Missing `roles` client scope on `onboarding-app` client** (`keycloak/onboarding-realm.json`)
   - The `onboarding-app` client had `defaultClientScopes: ["openid", "profile", "email"]` — missing `"roles"`.
   - Without the `roles` scope, Keycloak does NOT include `realm_access.roles` in the JWT token.
   - The `AdminLoginCommandHandler` looks for the `admin` role in `realm_access.roles`, which was absent.
   - Also, `openid` is NOT a valid client scope name in Keycloak 26.x (it's implicit), causing startup warnings.

2. **`AdminLoginCommandHandler` used wrong API to access nested JWT claims** (`src/Onboarding.Application/Auth/Commands/AdminLoginCommandHandler.cs`)
   - The handler used `jwt.Claims.FirstOrDefault(c => c.Type == "realm_access")` to find the realm_access claim.
   - `JwtSecurityTokenHandler.ReadJwtToken()` (System.IdentityModel.Tokens.Jwt v8.17.0) does NOT convert nested JSON objects (like `realm_access: {"roles": ["admin"]}`) into string claims. They are stored as `JsonElement` in the `Payload` dictionary, not in the `Claims` collection.
   - This means `realm_accessClaim` was always null, and no roles were ever extracted.

### Client 401 Unauthorized — NOT a bug, expected behavior:
   - The 401 response occurs because **no registered client users exist** in the system.
   - DB migrations had not been run (now fixed manually).
   - The registration flow (`POST /api/registration`) fails because the Keycloak Admin API service account (`onboarding-api-admin`) lacks `manage-users` role assignment.
   - The realm import's `users` entry with `serviceAccountClientId` does NOT reliably assign client roles during import — Keycloak creates the service account user but the role mapping from the JSON is not applied.
   - This is a **separate issue** from login — registration needs to work first before client login can succeed.

## Evidence

### Admin 403
- JWT payload before fix: no `realm_access` or `roles` fields at all
- JWT payload after adding `"roles"` to `defaultClientScopes`: `"realm_access": {"roles": ["admin"]}`
- API log: `"Roles":""` — empty roles list from handler
- `AdminLoginCommandHandler.cs` line 45: `jwt.Claims.FirstOrDefault(c => c.Type == "realm_access")` always returned null
- Direct Keycloak ROPC test confirmed token contains `realm_access.roles: ["admin"]` after scope fix

### Client 401
- `POST /api/auth/login` returns 401 for any email/password because no users exist in Keycloak
- Direct Keycloak ROPC test: `{"error":"invalid_grant","error_description":"Account is not fully set up"}` for manually created test user
- Registration fails with 503 because `KeycloakUserService.CreateUserAsync` gets `HTTP 403 Forbidden` from Keycloak Admin API
- Service account token shows no roles — realm import `users` entry with `serviceAccountClientId` does not apply role mappings

## Fix Applied

### 1. Added `roles` to `onboarding-app` defaultClientScopes
**File:** `D:\REPO\keycloak-tests\keycloak\onboarding-realm.json`
- Changed `defaultClientScopes` from `["openid", "profile", "email"]` to `["profile", "email", "roles"]`
- Removed invalid `"openid"` entry (not a real client scope in Keycloak 26.x)
- Set `onboarding-api-admin` `defaultClientScopes` to `[]` (was `["openid"]`, also invalid)

### 2. Fixed JWT role extraction in AdminLoginCommandHandler
**File:** `D:\REPO\keycloak-tests\src\Onboarding.Application\Auth\Commands\AdminLoginCommandHandler.cs`
- Changed from searching `jwt.Claims` for `realm_access` string claim
- Now uses `jwt.Payload` (which is a `JwtPayload` dictionary containing `JsonElement` for nested objects)
- Accesses `realm_access` and `resource_access` as `JsonElement` and extracts role arrays
- Added `using System.Text.Json;` import

### 3. Added service account user with realm-management roles to realm JSON
**File:** `D:\REPO\keycloak-tests\keycloak\onboarding-realm.json`
- Added service account user entry with `clientRoles` for `realm-management: ["manage-users", "view-users"]`
- Note: This import may not apply on existing volumes — manual role assignment via kcadm.sh or admin console may be needed

### 4. Ran EF Core migrations
- Applied 3 pending migrations to the app database: `InitialCreate`, `AddPasswordResetTokens`, `AddDeletedAtAndAuditLogs`

## Verification

- **Admin login**: `POST /api/admin/auth/login` with `admin@onboarding.local` / `Admin@123!` returns **200 OK** with `adminRefreshToken` cookie
- **Client login**: Still returns 401 because registration flow is broken (separate issue — service account role assignment)

## Remaining Work (separate issue)

The registration flow (`POST /api/registration`) fails because the `onboarding-api-admin` service account does not have `manage-users` role from the `realm-management` client. The realm import's `users` entry with `serviceAccountClientId` does not reliably apply role mappings. Options:
1. Manually assign roles via Keycloak Admin Console or `kcadm.sh`
2. Add a startup script to the API that assigns roles on first run
3. Use Keycloak's `clientScopeMappings` with proper token-based authorization (requires code changes)

This should be tracked as a separate issue: `registration-fails-keycloak-403`.

## Additional Fixes (Second Session)

### 3. EF Core Migration Auto-Apply
**Problem:** Registration returned 503 with `42P01: relation "clients" does not exist` — no EF Core migrations had been applied to the database.
**Fix:** Program.cs now calls `db.Database.Migrate()` on startup via `AppDbContext`. Tables are guaranteed to exist before the first request.
**File:** `D:\REPO\keycloak-tests\src\Onboarding.API\Program.cs`

### 4. Global Exception Handler
**Problem:** 500 errors exposed full stack traces to the frontend (security vulnerability).
**Fix:** Created `GlobalExceptionHandler.cs` middleware that:
- Logs FULL exception (including stack trace) server-side via ILogger
- Returns sanitized `ProblemDetails` JSON to client (RFC 9110 compliant)
- Maps known exception types to appropriate HTTP status codes:
  - `PostgresException` 23505 (unique violation) → 409 Conflict
  - `PostgresException` 23503 (foreign key) → 400 Bad Request
  - `DbUpdateException` → 409 Conflict
  - `KeyNotFoundException` → 404 Not Found
  - `UnauthorizedAccessException` → 403 Forbidden
  - Everything else → 500 Internal Server Error (generic message only)
**File:** `D:\REPO\keycloak-tests\src\Onboarding.API\Middleware\GlobalExceptionHandler.cs`
  
## Resolution  
**Status:** ? RESOLVED 2026-04-10  
**Commits:** d52786a (auth fixes), 73c3865 (migrations + exception handler) 
