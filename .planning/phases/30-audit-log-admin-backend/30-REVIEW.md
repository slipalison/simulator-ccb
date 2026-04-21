---
phase: 30-audit-log-admin-backend
reviewed: 2026-04-16T12:00:00Z
depth: standard
files_reviewed: 21
files_reviewed_list:
  - src/Onboarding.Application/Common/IAuditService.cs
  - src/Onboarding.Infrastructure/Services/AuditService.cs
  - src/Onboarding.Application/Admin/Commands/BlockUserCommand.cs
  - src/Onboarding.Application/Admin/Commands/UnblockUserCommand.cs
  - src/Onboarding.Application/Admin/Commands/UpdateUserCommand.cs
  - src/Onboarding.Application/Admin/Commands/DeleteUserCommand.cs
  - src/Onboarding.Application/Admin/Commands/CreateAdminCommand.cs
  - src/Onboarding.Infrastructure/Persistence/AppDbContext.cs
  - src/Onboarding.Application/Common/IKeycloakUserService.cs
  - src/Onboarding.Application/Admin/Queries/GetAdministratorsQuery.cs
  - src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs
  - src/Onboarding.API/Controllers/AdminUserController.cs
  - frontend/backoffice/src/lib/admin-api.ts
  - frontend/backoffice/src/components/pages/AdminAdministratorsPage.tsx
  - frontend/backoffice/src/router.tsx
  - frontend/backoffice/src/components/templates/AdminLayout.tsx
  - frontend/backoffice/src/tests/admin-api.test.ts
  - tests/Onboarding.API.Tests/Admin/AdminTestFactory.cs
  - tests/Onboarding.API.Tests/Admin/AdminFullFlowTests.cs
  - tests/Onboarding.Domain.Tests/Application/Commands/GetAdministratorsQueryHandlerTests.cs
  - tests/Onboarding.API.Tests/Admin/AdminAuthorizationTests.cs
findings:
  critical: 3
  warning: 8
  info: 5
  total: 16
status: issues_found
---

# Phase 30: Code Review Report

**Reviewed:** 2026-04-16
**Depth:** standard
**Files Reviewed:** 21
**Status:** issues_found

## Summary

This review covers the full Phase 30 scope: the unified `IAuditService` abstraction, all five admin command handlers (Block, Unblock, Update, Delete, CreateAdmin), the new `GetAdministratorsQuery`, `KeycloakUserService` additions, `AdminUserController`, and the complete frontend/test suite.

The architecture is sound. The IAuditService unification is clean, commands are properly separated, and the controller applies `[Authorize(Roles = "admin")]` consistently. The most significant issues are: (1) `RandomNumberGenerator` instances created inside a loop without disposal in `CreateAdminCommand`; (2) a broken contract between the frontend `deleteUser()` function and the backend DELETE endpoint, which requires a body the client never sends; (3) the `CreatedAtAction` call in `CreateAdmin` pointing to the wrong route action; and (4) double-serialized JSON in audit log `details` for Update and Delete operations. Several warnings and info-level items follow.

---

## Critical Issues

### CR-01: `RandomNumberGenerator` instances leak in loop — resource exhaustion risk

**File:** `src/Onboarding.Application/Admin/Commands/CreateAdminCommand.cs:113-117`

**Issue:** `RandomNumberGenerator.Create()` is called inside the Fisher-Yates shuffle loop (14 iterations) and also inside every `GetRandomChar` call (called 14 more times). Each call allocates a new `RandomNumberGenerator` object that implements `IDisposable`. None of these instances are disposed. On high-throughput paths this leaks OS-level entropy handles. Additionally, a single byte `randomBytes[0] % (i + 1)` introduces modulo bias: the bias is small for 14 characters but is a real statistical non-uniformity that weakens the password distribution.

**Fix:** Use the static `RandomNumberGenerator.GetBytes()` method (available since .NET 6) which avoids allocation entirely, and apply a bias-free algorithm:

```csharp
private static string GenerateTemporaryPassword()
{
    const string upperChars  = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const string lowerChars  = "abcdefghijklmnopqrstuvwxyz";
    const string digits      = "0123456789";
    const string specialChars = "!@#$%^&*";
    const string allChars    = upperChars + lowerChars + digits + specialChars;

    var chars = new char[14];
    chars[0] = GetRandomChar(upperChars);
    chars[1] = GetRandomChar(upperChars);
    chars[2] = GetRandomChar(lowerChars);
    chars[3] = GetRandomChar(lowerChars);
    chars[4] = GetRandomChar(digits);
    chars[5] = GetRandomChar(digits);
    chars[6] = GetRandomChar(specialChars);
    chars[7] = GetRandomChar(specialChars);
    for (int i = 8; i < chars.Length; i++)
        chars[i] = GetRandomChar(allChars);

    // Bias-free Fisher-Yates using RandomNumberGenerator.GetInt32
    for (int i = chars.Length - 1; i > 0; i--)
    {
        int j = RandomNumberGenerator.GetInt32(i + 1); // no modulo bias, no allocation
        (chars[i], chars[j]) = (chars[j], chars[i]);
    }

    return new string(chars);
}

private static char GetRandomChar(string charSet)
    => charSet[RandomNumberGenerator.GetInt32(charSet.Length)];
```

---

### CR-02: Frontend `deleteUser()` sends no body — backend always rejects with validation error

**File:** `frontend/backoffice/src/lib/admin-api.ts:263-283`

**Issue:** The `deleteUser(userId)` function sends a `DELETE` request with no body. The backend `DeleteUser` controller action requires a `DeleteUserRequest` body containing `ConfirmEmail` for LGPD compliance. The `DeleteUserCommandValidator` will reject a missing/empty `ConfirmEmail` with a 400 response every time. The function has no `confirmEmail` parameter and there is no way for callers to provide the required confirmation.

**Fix:** Add the required parameter and include the body:

```typescript
export async function deleteUser(
  userId: string,
  confirmEmail: string
): Promise<void> {
  const response = await fetch(`/api/admin/users/${userId}`, {
    method: "DELETE",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ confirmEmail }),
    credentials: "include",
  });

  if (response.status === 404) {
    throw new AdminApiError("Usuario nao encontrado.", 404);
  }
  if (response.status === 409) {
    throw new AdminApiError("Usuario ja foi deletado.", 409);
  }
  if (response.status === 400) {
    const body = await response.json().catch(() => ({}));
    throw new AdminApiError(body.detail || "Email de confirmacao invalido.", 400);
  }
  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new AdminApiError(body.detail || "Falha ao deletar usuario.");
  }
}
```

---

### CR-03: `CreatedAtAction(nameof(CreateAdmin), ...)` points to POST action — invalid `Location` header

**File:** `src/Onboarding.API/Controllers/AdminUserController.cs:334`

**Issue:** The 201 response from `POST /api/admin/administrators` uses `CreatedAtAction(nameof(CreateAdmin), ...)`. `CreateAdmin` is the POST action itself. The `Location` header in a 201 Created response must point to the newly created resource's GET URI, not to the action that just created it. As written, the `Location` header will contain a URL that resolves to `POST /api/admin/administrators` again, which is incorrect per RFC 9110. Any client that follows the Location header will get a 405 Method Not Allowed.

**Fix:** Since there is no individual `GetAdministratorById` endpoint, the most appropriate options are: (a) return `Ok(result)` with a 200 status if no singular GET exists, or (b) add a GET by ID endpoint and reference it. The minimal fix without adding a new endpoint:

```csharp
// Option A — use 200 OK instead of 201 if no resource URL is available
return Ok(result);

// Option B — point to the list endpoint (acceptable for collections)
return CreatedAtAction(nameof(GetAdministrators), result);
```

---

## Warnings

### WR-01: Double-serialized JSON in audit log `details` field

**File:** `src/Onboarding.Application/Admin/Commands/UpdateUserCommand.cs:85`
**File:** `src/Onboarding.Application/Admin/Commands/DeleteUserCommand.cs:102`

**Issue:** `before` and `after` are already JSON strings (produced by `JsonSerializer.Serialize(...)`). They are then serialized a second time as values inside `new { Before = before, After = after }`. The resulting `details` column stores escaped JSON-within-JSON:
```json
{"Before":"{\"Name\":\"...\",\"Email\":\"...\"}","After":"{...}"}
```
instead of the intended:
```json
{"Before":{"Name":"...","Email":"..."},"After":{...}}
```
This makes the audit log difficult to query and defeats the purpose of structured details.

**Fix:** Use `JsonDocument.Parse` to embed the already-parsed JSON as a raw element, or change the approach to capture typed objects and serialize once:

```csharp
// Capture as anonymous objects — serialize once at the end
var beforeObj = new { client.Name, Email = client.Email.Value, Phone = client.Phone.Value, client.RazaoSocial };
// ... apply update ...
var afterObj  = new { client.Name, Email = client.Email.Value, Phone = client.Phone.Value, client.RazaoSocial };

details: JsonSerializer.Serialize(new { Before = beforeObj, After = afterObj })
```

---

### WR-02: `Guid.Empty` stored in audit log when `actorSub` is not a GUID

**File:** `src/Onboarding.Infrastructure/Services/AuditService.cs:27`

**Issue:** `var adminId = Guid.TryParse(actorSub, out var parsed) ? parsed : Guid.Empty;` — when the JWT `sub` claim is not a UUID (e.g., it contains the admin's username or email as fallback), the audit record is created with `AdminUserId = Guid.Empty`. All such entries become indistinguishable in the database, losing actor traceability. The comment says email fallback is supported, but `Guid.Empty` is a poor sentinel — it collides with the all-zeros UUID in any query.

**Fix:** Either enforce that `actorSub` must always be a Keycloak UUID (fail fast if not), or store `actorSub` as a string separately, or use a nullable Guid so the DB value is `NULL` rather than zeros:

```csharp
var adminId = Guid.TryParse(actorSub, out var parsed) ? parsed : (Guid?)null;
// Update AdminAuditLog.Create to accept Guid? adminId
```

---

### WR-03: `AssignAdminRoleAsync` — inverted 409 guard logic

**File:** `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs:293-301`

**Issue:** The condition `if (!assignResponse.IsSuccessStatusCode && assignResponse.StatusCode != HttpStatusCode.Conflict)` is intended to skip the error path when the response is 409 (role already assigned). However, the subsequent `body.Contains("already exists")` string check only executes in the non-success AND non-409 branch, meaning it can never trigger for a legitimate 409. When Keycloak returns 409 the outer condition is false (because `StatusCode == Conflict` makes the second sub-condition true, making the entire `&&` false), so the method returns without error — which is the intended behavior. BUT if Keycloak returns a different non-success status with a body containing "already exists", the method also silently returns. The logic is coincidentally correct for the happy path but the string-contains fallback is unreachable and misleading.

**Fix:** Remove the unreachable string fallback and handle 409 explicitly:

```csharp
if (assignResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
    return; // Role already assigned — idempotent

if (!assignResponse.IsSuccessStatusCode)
{
    var body = await assignResponse.Content.ReadAsStringAsync(ct);
    throw new InvalidOperationException(
        $"Failed to assign admin role to user '{userId}': {body}");
}
```

---

### WR-04: `GetUsersByRoleAsync` silently truncates at Keycloak default page size

**File:** `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs:307-308`

**Issue:** `GET /admin/realms/{realm}/roles/{role}/users` uses Keycloak's default pagination, which caps results at 100 users per request. No `max` parameter is sent, so installations with more than 100 admins will silently return only the first 100. The comment in the query handler says "lista pequena" — but there is no enforcement of this assumption at the infrastructure layer. If the admin list ever exceeds 100, the endpoint returns partial data without any indication.

**Fix:** Add explicit `max` and `first` parameters to the URL, or pass a large max:

```csharp
var response = await _adminHttpClient.GetAsync(
    $"admin/realms/{_realm}/roles/{Uri.EscapeDataString(roleName)}/users?max=500",
    ct);
```

Alternatively, implement multi-page fetching if hardening against scale is required.

---

### WR-05: `blockUser` sends a body that the backend ignores — client/API contract mismatch

**File:** `frontend/backoffice/src/lib/admin-api.ts:238-257`

**Issue:** `blockUser(userId, reason)` sends `{ reason }` as the request body and accepts a `reason` string parameter. The backend `BlockUser` controller action takes no body — it reads only the route `{id}` parameter. The `reason` parameter and body are silently ignored by the backend today. This is a misleading API contract that creates a false expectation that `reason` is recorded.

**Fix:** Either remove the `reason` parameter from the frontend function (matching the backend), or add reason to the backend `BlockUserCommand` and audit log:

```typescript
// Minimal fix — remove unused parameter
export async function blockUser(userId: string): Promise<void> {
  const response = await fetch(`/api/admin/users/${userId}/block`, {
    method: "POST",
    credentials: "include",
  });
  // ...
}
```

---

### WR-06: 401 session expiry not handled in `AdminAdministratorsPage`

**File:** `frontend/backoffice/src/components/pages/AdminAdministratorsPage.tsx:21-28`

**Issue:** The `catch` block discards the error type entirely. A 401 response (session expired) should redirect to `/admin/login`. As written, an expired session leaves the user stuck seeing a retry button with no path to re-authenticate.

**Fix:**
```tsx
} catch (err) {
  if (err instanceof AdminApiError && err.status === 401) {
    window.location.href = "/admin/login";
    return;
  }
  setIsError(true);
  toast.error("Falha ao carregar administradores", {
    description: "Tente novamente.",
  });
}
```

---

### WR-07: Raw `<a href>` anchors in `AdminSidebar` cause full-page reloads

**File:** `frontend/backoffice/src/components/templates/AdminLayout.tsx:49-69`

**Issue:** `AdminSidebar` uses bare `<a href="...">` elements for in-app navigation. In a TanStack Router SPA, these trigger a full browser navigation, destroying all React state and re-running the auth context bootstrap on every sidebar click.

**Fix:** Replace `<a href>` with TanStack Router's `<Link>`:
```tsx
import { Link } from "@tanstack/react-router";

<Link
  to="/admin/users"
  className="block py-2 px-3 text-sm rounded-md hover:bg-accent transition-colors"
  data-testid="sidebar-users-link"
>
  Usuarios
</Link>
```

---

### WR-08: `admin` object accessed without null guard in `AdminLayout`

**File:** `frontend/backoffice/src/components/templates/AdminLayout.tsx:91`

**Issue:** `admin.adminName || "Admin"` will throw `TypeError: Cannot read properties of null` if `useAdminAuth` returns `null` for `admin` during context initialization or after session expiry.

**Fix:**
```tsx
export function AdminLayout({ children }: { children: ReactNode }) {
  const { admin, logout } = useAdminAuth();

  if (!admin) {
    return null; // or redirect to /admin/login
  }
  // ...
```

---

## Info

### IN-01: `UpdateUserRequest` has `address` field not present in domain command

**File:** `frontend/backoffice/src/lib/admin-api.ts:200-205`

**Issue:** The `UpdateUserDto` interface includes an `address` field. The backend `UpdateUserCommand` has no `Address` property and the handler does not apply it. The field is silently dropped. This is misleading to API consumers.

**Fix:** Remove `address` from `UpdateUserDto` to match the actual backend contract, or add address support to the backend command.

---

### IN-02: Dead code — `_adminFetchOptions` is defined but never called

**File:** `frontend/backoffice/src/lib/admin-api.ts:93-106`

**Issue:** The `_adminFetchOptions` helper is defined but is not called anywhere in the file. All API functions construct their own `RequestInit` inline. This is dead code that adds noise and maintenance burden.

**Fix:** Remove the function or replace inline `RequestInit` objects with calls to it.

---

### IN-03: Double-serialization test gap — no test validates `details` field structure

**File:** `tests/Onboarding.API.Tests/Admin/AdminFullFlowTests.cs:128-143`

**Issue:** `AdminFullFlowTests` verifies that `RecordAsync` is called with the correct `ActionType` values, but uses `Arg.Any<string?>()` for `details`. The double-serialization bug in `UpdateUserCommand` and `DeleteUserCommand` (WR-01) would not be caught by any test in scope.

**Fix:** Add a unit test for `UpdateUserCommandHandler` that verifies the `details` argument passed to `IAuditService.RecordAsync` is a flat JSON object (not escaped), e.g.:
```csharp
var detailsArg = (string?)auditCapture.ReceivedCalls()
    .First().GetArguments()[5];
var parsed = JsonDocument.Parse(detailsArg!);
parsed.RootElement.GetProperty("Before").ValueKind.ShouldBe(JsonValueKind.Object);
```

---

### IN-04: Redundant `vi.clearAllMocks()` + `mockFetch.mockReset()` in beforeEach

**File:** `frontend/backoffice/src/tests/admin-api.test.ts:21-23`

**Issue:** `mockFetch.mockReset()` already clears all implementations and call history. The preceding `vi.clearAllMocks()` is redundant for a single mock.

**Fix:**
```ts
beforeEach(() => {
  mockFetch.mockReset();
});
```

---

### IN-05: `AdminUserDto` DTO co-located in `IAuditService.cs`

**File:** `src/Onboarding.Application/Common/IAuditService.cs:26-31`

**Issue:** `AdminUserDto` is a query result DTO related to Keycloak user listing, but it is defined in `IAuditService.cs`. This violates single-responsibility at the file level and makes the DTO harder to discover. The interface's XML doc comment also does not mention `AdminUserDto`, signalling it was added as an afterthought.

**Fix:** Move `AdminUserDto` to its own file (e.g., `src/Onboarding.Application/Common/AdminUserDto.cs`) or to `src/Onboarding.Application/Admin/DTOs/AdminUserDto.cs` where the other admin DTOs reside.

---

_Reviewed: 2026-04-16_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
