---
phase: 10-profile-ui
reviewed: 2026-04-08T00:00:00Z
depth: standard
files_reviewed: 11
files_reviewed_list:
  - frontend/src/components/atoms/ProfileBadge.tsx
  - frontend/src/components/atoms/ProfileField.tsx
  - frontend/src/components/molecules/ProfileCard.tsx
  - frontend/src/components/pages/ProfilePage.tsx
  - frontend/src/lib/api.ts
  - frontend/src/lib/auth-context.tsx
  - frontend/src/lib/types.ts
  - frontend/src/tests/login-flow.test.tsx
  - frontend/src/tests/profile-components.test.tsx
  - frontend/src/tests/profile-e2e.test.tsx
  - frontend/src/tests/profile-page.test.tsx
findings:
  critical: 0
  warning: 6
  info: 4
  total: 10
status: issues_found
---

# Phase 10: Code Review Report

**Reviewed:** 2026-04-08
**Depth:** standard
**Files Reviewed:** 11
**Status:** issues_found

## Summary

This phase introduces the profile display feature: `ProfileBadge`, `ProfileField`, `ProfileCard`, `ProfilePage`, the `getProfileClient` API function, token handling in `auth-context.tsx`, and a shared `ClientProfileDto` type. The test suite is thorough, covering unit, integration, and E2E flows.

No critical security vulnerabilities were found. The token storage is correctly kept in module-level memory (never localStorage), and the Bearer token is forwarded appropriately.

Six warnings were identified — the most impactful are: a silent fallback in `ProfileCard` for PJ `razaoSocial`, an unvalidated API response cast, and HTTP status handling gaps in the API client. Four info items cover type precision, suppressed TypeScript errors, and a misleading Content-Type header on a GET request.

---

## Warnings

### WR-01: `loginClient` and `refreshTokenClient` only accept HTTP 200 — all other 2xx are errors

**File:** `frontend/src/lib/api.ts:61-73` and `79-93`
**Issue:** Both functions check `response.status === 200` for success. Any other 2xx response code (e.g., `201`, `204`) falls through to `throw new ApiError(...)`. While the current backend is known to return `200`, using strict equality on status codes makes the client brittle against backend changes. The `refreshTokenClient` has the same pattern.
**Fix:**
```typescript
// Replace status === 200 check with response.ok (covers all 2xx)
if (response.ok) {
  return (await response.json()) as LoginResponse;
}
// Then check specific error codes below
if (response.status === 422) { ... }
if (response.status === 401) { ... }
throw new ApiError("An unexpected error occurred.");
```

---

### WR-02: `getProfileClient` casts the API response without runtime validation

**File:** `frontend/src/lib/api.ts:244`
**Issue:** `return response.json() as Promise<ClientProfileDto>` is a TypeScript type assertion only — it provides no runtime guarantee. If the backend returns a shape that differs from `ClientProfileDto` (missing fields, renamed keys, or null where a string is expected), the `ProfileCard` component will silently render empty or `undefined` values with no error surfaced to the user.
**Fix:** Either add a lightweight runtime check, or at minimum verify the critical discriminating field:
```typescript
const data = await response.json();
if (!data || typeof data.type !== "string") {
  throw new ProfileError("Invalid profile data received from server");
}
return data as ClientProfileDto;
```

---

### WR-03: `ProfileCard` silently falls back to `profile.name` when PJ `razaoSocial` is null

**File:** `frontend/src/components/molecules/ProfileCard.tsx:42`
**Issue:** `value={profile.razaoSocial ?? profile.name}` — for a PJ profile where `razaoSocial` is `null`, the "Razão Social" field displays the generic `name` value without any indication to the user that the expected field was missing. This masks a potential data integrity problem and can mislead users about what is actually stored for their company.
**Fix:** Either render an explicit placeholder, or remove the fallback and treat a missing `razaoSocial` on a PJ profile as an error state:
```tsx
<ProfileField
  label="Razão Social"
  value={profile.razaoSocial ?? "—"}
/>
```
If a non-null `razaoSocial` is always expected for PJ clients, enforce it in the type (see IN-04) and remove the fallback entirely.

---

### WR-04: `login()` in `AuthProvider` swallows errors — callers must catch without a guaranteed contract

**File:** `frontend/src/lib/auth-context.tsx:49-65`
**Issue:** The `login` function does not re-throw on error. It updates `isLoading` to `false` in `finally` but provides no error signal through context state. The only way callers know a login failed is if they `catch` the re-thrown error — but `login` does not `throw`. Any error from `loginClient` is currently silently swallowed.

Looking at the code path: `loginClient` can throw `LoginError` or `ApiError`. Because `login()` has no `catch` block, those exceptions propagate naturally out of the `async function` — the `finally` runs and then the exception continues up the call stack. The function does re-throw implicitly (no catch swallows it), but this is non-obvious and fragile. Adding an explicit `throw` after `setIsLoading(false)` makes the contract clear.
**Fix:**
```typescript
async function login(email: string, password: string): Promise<void> {
  setIsLoading(true);
  try {
    const response = await loginClient(email, password);
    tokens = { ... };
    setIsAuthenticated(true);
  } catch (err) {
    // Ensure isLoading resets, then re-throw for callers to handle
    setIsLoading(false);
    throw err;
  }
  setIsLoading(false);
}
```
Or keep the `finally` pattern but add a `catch` that re-throws explicitly, making the intent clear to future maintainers.

---

### WR-05: `refreshIfNeeded` never checks if the refresh token itself has expired

**File:** `frontend/src/lib/auth-context.tsx:79-98`
**Issue:** The function checks whether the access token is within 60 seconds of expiry but never validates `tokens.refreshExpiresAt`. If the refresh token has itself expired (e.g., user was inactive for longer than `refreshExpiresIn`), the function will still attempt a `refreshTokenClient` call, which will fail server-side. The failure is caught and `logout()` is called, so there is no crash — but the UX is a silent session termination rather than a predictable redirect.
**Fix:**
```typescript
async function refreshIfNeeded(): Promise<void> {
  if (!tokens.refreshToken || !tokens.expiresAt) return;

  // Also bail out early if refresh token has expired
  if (tokens.refreshExpiresAt && tokens.refreshExpiresAt <= Date.now()) {
    logout();
    return;
  }

  const timeUntilExpiry = tokens.expiresAt - Date.now();
  if (timeUntilExpiry > 60_000) return;
  // ... rest of logic
}
```

---

### WR-06: `ProfilePage` has two independent `useEffect`s with an implicit ordering dependency

**File:** `frontend/src/components/pages/ProfilePage.tsx:23-52`
**Issue:** The auth guard effect (lines 23-28) and the fetch effect (lines 31-52) both depend on `auth.isAuthenticated`. Both fire on the same render cycle. The fetch effect guards itself with `if (!auth.isAuthenticated) return`, so no API call happens when unauthenticated — this is correct. However, the navigation side-effect (to `/login`) and the fetch side-effect both run simultaneously when `isAuthenticated` is `false`, meaning there is a brief moment where the component is both navigating away and attempting to skip a fetch. In React Strict Mode (double-invocation), this can produce state-update-after-unmount warnings. The guard and the fetch are tightly coupled and should be a single effect.
**Fix:**
```typescript
useEffect(() => {
  if (!auth.isAuthenticated) {
    navigate({ to: "/login" as any, replace: true });
    return;
  }

  async function fetchProfile() {
    try {
      setIsLoading(true);
      setError(null);
      const data = await getProfileClient();
      setProfile(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao carregar dados do perfil");
    } finally {
      setIsLoading(false);
    }
  }

  fetchProfile();
}, [auth.isAuthenticated, navigate]);
```

---

## Info

### IN-01: `Content-Type: application/json` header sent on GET request

**File:** `frontend/src/lib/api.ts:232`
**Issue:** The `getProfileClient` function sends `"Content-Type": "application/json"` as a request header on a GET call that has no body. This header describes the format of a request body; on a body-less GET it is meaningless and may confuse strict proxies or middleware.
**Fix:** Remove the `Content-Type` header from the GET request:
```typescript
headers: {
  Authorization: `Bearer ${token}`,
},
```

---

### IN-02: `// eslint-disable-next-line @typescript-eslint/no-explicit-any` on router navigation

**File:** `frontend/src/components/pages/ProfilePage.tsx:26` and `57`
**Issue:** The route `"/login"` is cast to `any` to satisfy TanStack Router's typed navigation. This suppresses a legitimate type error that indicates the router's route tree type is not wired up correctly. The fix should be in the router configuration (ensuring all routes are registered and exported for type inference), not in the call site.
**Fix:** Ensure the router is set up with `createRouter` and the route tree is registered so TanStack Router can infer the valid `to` paths. Once the type is correct, remove the `as any` casts. See TanStack Router docs on `createRootRoute` and `FileRoutesByPath`.

---

### IN-03: `api.ts` has mid-file `import` declarations — file is structured as three concatenated modules

**File:** `frontend/src/lib/api.ts:100` and `203`
**Issue:** ES `import` statements appear at lines 100 and 203, in the middle of the file. TypeScript allows this syntactically, but it is non-standard and signals the file was grown by appending three logical modules. `ApiError` is defined at line 141 but thrown at lines 73 and 93 (before the definition textually), relying on JS class-declaration hoisting. This makes the file hard to read and maintain.
**Fix:** Move all `import` statements to the top of the file. Consider splitting `api.ts` into `auth-api.ts`, `registration-api.ts`, and `profile-api.ts` with shared error classes in a separate `api-errors.ts` module.

---

### IN-04: `ClientProfileDto` fields typed as `string | null | undefined` when `string | null` suffices

**File:** `frontend/src/lib/types.ts:17-19`
**Issue:** The optional modifier `?` on `cpf`, `cnpj`, and `razaoSocial` creates a three-state type (`string | null | undefined`). The backend DTO likely only ever sends `null` or a string value — not omitting the field entirely. The extra `undefined` state necessitates defensive `?? fallback` coding in consumers (e.g., `ProfileCard` line 42) and widens the type unnecessarily.
**Fix:**
```typescript
export interface ClientProfileDto {
  id: string;
  name: string;
  email: string;
  phone: string;
  type: "PessoaFisica" | "PessoaJuridica";
  cpf: string | null;
  cnpj: string | null;
  razaoSocial: string | null;
}
```
This also allows `ProfileCard` to drop the `?? profile.name` fallback and instead explicitly handle `null`.

---

_Reviewed: 2026-04-08_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
