---
phase: 33-pkce-custom-keycloak-themes
reviewed: 2026-04-16T00:00:00Z
depth: standard
files_reviewed: 22
files_reviewed_list:
  - frontend/client/auth-server.ts
  - frontend/client/src/lib/auth-code-flow.ts
  - frontend/client/src/lib/auth-context.tsx
  - frontend/client/src/components/pages/AuthLoginPage.tsx
  - frontend/client/src/components/pages/AuthCallbackPage.tsx
  - frontend/client/src/components/pages/AuthErrorPage.tsx
  - frontend/client/app.config.ts
  - frontend/client/src/lib/api.ts
  - frontend/client/src/router.tsx
  - frontend/client/src/components/molecules/RegistrationForm.tsx
  - frontend/client/src/components/organisms/Header.tsx
  - frontend/client/src/components/pages/ForgotPasswordPage.tsx
  - frontend/client/src/components/pages/ProfilePage.tsx
  - frontend/client/src/components/pages/ResetPasswordPage.tsx
  - keycloak/onboarding-realm.json
  - compose.yaml
  - keycloak/themes/onboarding-client/login/login.ftl
  - keycloak/themes/onboarding-client/login/theme.properties
  - keycloak/themes/onboarding-client/login/resources/css/styles.css
  - keycloak/themes/onboarding-backoffice/login/login.ftl
  - keycloak/themes/onboarding-backoffice/login/theme.properties
  - keycloak/themes/onboarding-backoffice/login/resources/css/styles.css
findings:
  critical: 4
  warning: 5
  info: 4
  total: 13
status: issues_found
---

# Phase 33: Code Review Report

**Reviewed:** 2026-04-16
**Depth:** standard
**Files Reviewed:** 22
**Status:** issues_found

## Summary

This phase introduces the ACF+PKCE migration for the client app and two custom Keycloak FreeMarker themes. The PKCE implementation itself is structurally sound — code verifier and challenge generation use the Web Crypto API correctly, the S256 method is used, state is validated on callback, and tokens are stored in httpOnly cookies. The FreeMarker templates are safe and produce no XSS vectors. Cookie naming (`client_access_token` / `client_refresh_token`) correctly avoids collision with the backoffice namespace.

Four critical issues require attention before this can be considered production-ready: the `/auth/me` endpoint trusts the JWT payload without signature verification; the OIDC logout is broken in Keycloak 26; two sets of client secrets and an admin password are hardcoded in a committed realm import file; and Grafana's anonymous access is enabled in the compose stack.

---

## Critical Issues

### CR-01: JWT payload trusted without signature verification

**File:** `frontend/client/auth-server.ts:166-189`

**Issue:** The `/auth/me` handler decodes the JWT by splitting on `.` and base64-decoding the payload. It never verifies the signature. Any request that carries a hand-crafted or tampered `client_access_token` cookie (e.g., by altering the payload to elevate the `sub` claim or forge an email) will be accepted as authenticated. This completely nullifies the security guarantee of using JWTs.

**Fix:** Either call Keycloak's `/protocol/openid-connect/userinfo` endpoint with the access token as a Bearer (the token is validated server-side by Keycloak), or verify the signature locally using the realm's JWKS endpoint. The userinfo approach is simpler and avoids key management:

```typescript
// Replace the decode block in the /me handler with:
const userinfoUrl = `${KEYCLOAK_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/userinfo`;
const resp = await fetch(userinfoUrl, {
  headers: { Authorization: `Bearer ${accessToken}` },
});
if (!resp.ok) {
  event.node.res.statusCode = 401;
  return { isAuthenticated: false };
}
const payload = await resp.json() as Record<string, unknown>;
// sub, email, preferred_username are available here, verified by Keycloak
```

---

### CR-02: OIDC logout broken in Keycloak 26 — missing `client_id` and `id_token_hint`

**File:** `frontend/client/auth-server.ts:147-149`

**Issue:** The `/auth/logout` handler redirects to Keycloak's logout endpoint with only `post_logout_redirect_uri`. Keycloak 26 (per the OpenID Connect RP-Initiated Logout spec) requires either `id_token_hint` or `client_id` to associate the logout request with a specific client. Without these, Keycloak 26 will redirect back immediately without terminating the SSO session, meaning the user stays logged in at the Keycloak level and can re-authenticate silently without entering credentials again.

**Fix:** At minimum, append `client_id`. Including the current `id_token_hint` if available (from the ID token cookie) provides the strongest guarantee. The server already stores the access token; store the ID token too, or at minimum pass `client_id`:

```typescript
const fullUrl =
  `${logoutUrl}` +
  `?client_id=${encodeURIComponent(CLIENT_ID)}` +
  `&post_logout_redirect_uri=${encodeURIComponent(postLogoutRedirectUri)}`;
```

If you persist the ID token in a cookie (recommended), add `&id_token_hint=${encodeURIComponent(idToken)}` for full compliance.

---

### CR-03: Hardcoded client secrets and admin password committed to repository

**File:** `keycloak/onboarding-realm.json:73, 115, 148`

**Issue:** Three credentials are hardcoded in the committed realm import file:
- Line 73: `"secret": "backoffice-secret-dev-change-in-prod-2026"` (onboarding-backoffice client)
- Line 115: `"secret": "client-acf-secret-dev-change-in-prod-2026"` (onboarding-client-acf client)
- Line 148: `"value": "Admin@123!"` (admin user bootstrap password)

Even though these are labelled as dev defaults, committing them creates a persistent record in git history that is difficult to purge and poses risk if the repo is ever exposed. The `"temporary": true` flag on the admin password mitigates that specific one slightly, but the client secrets persist indefinitely.

**Fix:** Replace the hardcoded values with placeholder strings and inject the real secrets at startup via environment variables using Keycloak's `${env.VAR_NAME}` interpolation syntax in the import file, or use a separate secret management step (e.g., a post-init script that calls the Keycloak Admin API to rotate the secrets to values from environment variables):

```json
"secret": "${env.KEYCLOAK_CLIENT_ACF_CLIENT_SECRET}"
```

Keycloak's realm import supports this substitution when the `--import-realm` flag is used with `KC_*` env vars.

---

### CR-04: Grafana anonymous access exposes all observability data

**File:** `compose.yaml:223-224`

**Issue:** `GF_AUTH_ANONYMOUS_ENABLED: "true"` with `GF_AUTH_ANONYMOUS_ORG_ROLE: Viewer` grants unauthenticated access to Grafana, which contains application traces, logs, and metrics. In a local dev environment, this exposes potentially sensitive data (user emails in traces, JWT structures, API error details) to anyone on the host network. This setting should not be the default; developers can enable it locally themselves if needed.

**Fix:** Remove or default-to-false for anonymous access:

```yaml
GF_AUTH_ANONYMOUS_ENABLED: "${GF_ANONYMOUS_ENABLED:-false}"
```

At minimum, change the anonymous org role from `Viewer` to something that cannot read datasources if anonymous access must remain for convenience.

---

## Warnings

### WR-01: Operator precedence bug produces wrong `nome` mapping in ProfilePage

**File:** `frontend/client/src/components/pages/ProfilePage.tsx:48`

**Issue:** The expression `data.name || data.cpf ? data.name : undefined` is parsed by JavaScript as `(data.name) || (data.cpf ? data.name : undefined)` due to `||` having lower precedence than `?:`. The intended behavior (set `nome` only when `data.name` exists) is not what executes:

- When `data.name` is truthy: evaluates to `data.name` — correct.
- When `data.name` is falsy but `data.cpf` is truthy: evaluates to `undefined` — correct.
- When `data.name` is falsy and `data.cpf` is also falsy: evaluates to `undefined` — correct.

So in this specific case the result happens to be the same as intended, but the logic is misleading and fragile. The next developer who reads this will likely misunderstand it.

**Fix:** Use explicit parentheses to state intent clearly:

```typescript
nome: (data.name || data.cpf) ? data.name : undefined,
```

Or, if only the name field should appear for PessoaFisica:

```typescript
nome: data.type === "PessoaFisica" ? data.name || undefined : undefined,
```

---

### WR-02: `handleLogout` in ProfilePage calls `navigate` after synchronous page navigation

**File:** `frontend/client/src/components/pages/ProfilePage.tsx:67-69`

**Issue:** `handleLogout` is declared `async` and calls `await logout()`, but `logout()` (defined in `auth-context.tsx:61`) synchronously sets `window.location.href = "/auth/logout"`, which triggers an immediate navigation. The `navigate({ to: "/login" as any })` on line 69 executes after `logout()` returns but the page will already be navigating away. The `navigate` call is dead code that may also produce a React state update on an unmounted component warning.

**Fix:** Remove the `navigate` call after `logout()`. The server-side `/auth/logout` handler already redirects to `/auth/login` via Keycloak's `post_logout_redirect_uri`.

```typescript
function handleLogout() {
  logout(); // triggers window.location.href redirect — no need to navigate
}
```

---

### WR-03: PKCE state parameter has insufficient entropy

**File:** `frontend/client/auth-server.ts:35`

**Issue:** `generateCodeVerifier().slice(0, 20)` generates a full 32-byte random verifier (base64url = 43 chars) and then truncates it to 20 characters. This provides approximately 119 bits of entropy, which while technically sufficient, contradicts the purpose of calling `generateCodeVerifier()`. If the slice length were ever reduced further (e.g., during debugging), this could become a vulnerability. More importantly, it wastes a full `getRandomValues` call when a dedicated state generator would be cleaner and signal intent.

**Fix:** Generate the state with its own dedicated call sized for state, or simply do not truncate:

```typescript
const state = generateCodeVerifier(); // full 43-char base64url state, no truncation
```

---

### WR-04: `exchangeCodeForTokens` does not validate presence of returned tokens

**File:** `frontend/client/src/lib/auth-code-flow.ts:75-80`

**Issue:** The token exchange response is cast with `as string` / `as number` without any existence checks. If the Keycloak response is missing `refresh_token` (e.g., because `offline_access` scope was not granted, which is optional for `onboarding-client-acf`), `tokens.refreshToken` will be `undefined`, which is then stored in the cookie as the string `"undefined"`. Similarly `expires_in` missing would store `undefined` as the `maxAge`.

**Fix:** Add guard checks after deserializing:

```typescript
const accessToken = data.access_token as string | undefined;
const refreshToken = data.refresh_token as string | undefined;
const expiresIn = data.expires_in as number | undefined;
if (!accessToken || !refreshToken) {
  throw new Error("Token exchange response missing required fields");
}
return { accessToken, refreshToken, expiresIn: expiresIn ?? 300 };
```

---

### WR-05: Realm has `resetPasswordAllowed: false` but the frontend exposes reset-password UI

**File:** `keycloak/onboarding-realm.json:7` and `frontend/client/src/components/pages/ResetPasswordPage.tsx`

**Issue:** The realm configuration sets `"resetPasswordAllowed": false`, which disables Keycloak's built-in password reset flow. However, the frontend ships `ForgotPasswordPage` and `ResetPasswordPage` components and has API calls in `api.ts` to `/api/auth/forgot-password` and `/api/auth/reset-password`. If these backend routes delegate to Keycloak's built-in reset flow they will silently fail; if they are custom backend-implemented flows they bypass Keycloak's rate limiting and security controls. This is a configuration/code mismatch that needs to be made explicit.

**Fix:** If the reset password flow is custom (fully backend-managed), document this clearly and ensure the realm setting is intentionally `false`. If you intend to use Keycloak's native flow, set `"resetPasswordAllowed": true` in the realm JSON and remove the custom API routes.

---

## Info

### IN-01: `console.error` left in ProfilePage

**File:** `frontend/client/src/components/pages/ProfilePage.tsx:57`

**Issue:** `console.error("Failed to fetch profile:", err)` is a debug artifact in production code. It may leak sensitive error details (token contents, server URLs) to the browser console in production.

**Fix:** Replace with a proper error state that renders a user-facing error message, or remove entirely since the page already handles the `!profile` case on line 94.

---

### IN-02: Multiple `as any` casts bypass TanStack Router's type safety

**File:** `frontend/client/src/router.tsx:66, 75, 116`

**Issue:** Three route definitions and one navigation call use `as any` to work around type errors. The `profileRoute` definition uses `} as any` to bypass type checking on the route options. This removes the compile-time guarantee that route params and search params are correct.

**Fix:** These typically occur when the `routeTree` is generated manually rather than via the TanStack Router file-based code generator. Ensure the route tree is typed correctly. The `to: "/auth/login" as any` casts in `ForgotPasswordPage.tsx:73` and `ResetPasswordPage.tsx:70` can be removed once the router's `Register` interface is properly set up.

---

### IN-03: `base64UrlEncode` uses spread operator on Uint8Array

**File:** `frontend/client/src/lib/auth-code-flow.ts:118`

**Issue:** `btoa(String.fromCharCode(...input))` spreads the Uint8Array as function arguments. For the 32-byte verifier and 32-byte SHA-256 hash used here this is harmless, but the pattern is fragile — it will throw `RangeError: Maximum call stack exceeded` on arrays larger than ~65k elements. If this utility is ever reused with larger inputs it will break silently in some engines.

**Fix:** Use a safe loop-based conversion:

```typescript
function base64UrlEncode(input: Uint8Array): string {
  let binary = "";
  for (let i = 0; i < input.length; i++) {
    binary += String.fromCharCode(input[i]);
  }
  return btoa(binary)
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/, "");
}
```

---

### IN-04: `CLIENT_SECRET` defaults to empty string — silent failure for confidential client

**File:** `frontend/client/auth-server.ts:24`

**Issue:** `process.env.KEYCLOAK_CLIENT_ACF_CLIENT_SECRET || ""` defaults to an empty string when the env var is not set. The `onboarding-client-acf` Keycloak client is a confidential client (`"publicClient": false`) and requires a non-empty secret. Sending an empty string secret will cause the token exchange to fail with a `401 Unauthorized` from Keycloak. The failure is cryptic and the root cause (missing env var) is not surfaced to the developer.

**Fix:** Fail fast at startup when the secret is missing:

```typescript
const CLIENT_SECRET = process.env.KEYCLOAK_CLIENT_ACF_CLIENT_SECRET;
if (!CLIENT_SECRET) {
  throw new Error(
    "KEYCLOAK_CLIENT_ACF_CLIENT_SECRET env var is required for confidential client"
  );
}
```

---

_Reviewed: 2026-04-16_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
