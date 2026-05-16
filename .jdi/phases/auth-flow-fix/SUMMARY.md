# Phase 49 --- auth-flow-fix --- SUMMARY

### T-6 (2026-05-16T14:30:00Z)

**Status:** DONE

**Commit sha:** 381a334

**Files modified:**
- `frontend/backoffice/src/components/templates/AdminLayout.tsx` — T-6a: replaced `null` return during `isLoading` with a centered `<Loader2>` loading shell (`data-testid="admin-loading-shell"`, `aria-busy="true"`). The redirect `useEffect` was already correctly gated on `!isLoading && !isAuthenticated` — no change needed there. Added `sidebar-users-link` to `AdminSidebar` (Usuarios → `/admin/users`) to fix the pre-existing failing test and match the restored route.
- `frontend/backoffice/src/lib/admin-auth-context.tsx` — T-6b: single bounded retry on 401. After first `/auth/me` returns `AdminApiError` with `status === 401`, waits 200ms then retries exactly once. Second 401 finalizes as unauthenticated. 5xx and network errors fail fast. Comment cites D-12 + post-redirect cookie-commit race hypothesis.
- `frontend/backoffice/src/lib/admin-api.ts` — `getAdminMe` now passes HTTP response status to `AdminApiError` constructor so callers can distinguish 401 from 5xx.
- `frontend/backoffice/src/router.tsx` — T-6c: removed `RedirectCompanies` component and replaced `/admin/users` route with `AdminUsersPage` (which was written but never wired into the router). Added `AdminUsersPage` import.
- `frontend/backoffice/auth-server.ts:249` — T-6c: changed post-login redirect from `/admin/users` to `/admin/companies` (eliminating the extra client-side route hop).
- `frontend/backoffice/src/tests/admin-layout.test.tsx` — extended with 3 new T-6a tests: loading shell rendered + no redirect during `isLoading=true`; children rendered when authenticated; redirect fires when unauthenticated. Updated `AdminApiError` mock to accept `status` param.
- `frontend/backoffice/src/tests/admin-auth-context.test.tsx` — extended with 3 new T-6b tests: retry on 401 + success; retry on 401 + second 401; 5xx fails fast. Updated `AdminApiError` mock to accept `status` param.

**RedirectCompanies decision: (B) — server-side redirect, component removed**

Grep proof:
- `RedirectCompanies` appeared in exactly 2 lines in `router.tsx` (L81 component reference + L167 function definition) and 0 lines elsewhere in the project.
- `/admin/users` is a live route used by `AdminUsersPage`, `AdminUserDetailPage`, `AdminUserEditPage`, `AdminLoginPage` (redirect-if-authenticated), sidebar links, and multiple tests. It was never "only" a post-login landing pad.
- `RedirectCompanies` was incorrectly intercepting all navigations to `/admin/users` (including the real users list) and bouncing them to `/admin/companies`. Option (B) fixes both problems: `auth-server.ts` redirects straight to `/admin/companies` post-login, and `/admin/users` now serves the real `AdminUsersPage`.

**Test results:** `pnpm typecheck` → 0 errors. `pnpm lint` → 0 warnings. `pnpm vitest run` → 171/171 pass (19 test files). Pre-existing failure (sidebar-users-link) resolved as a side effect of restoring the route.

**Vinext migration debt:** None. No Vinxi-internal APIs introduced. All changes are in React component/context files and `auth-server.ts` server handler (h3/Vinxi server router, compatible with Vinext migration path per Phase 53 plan).

---

### T-5 (2026-05-16T14:25:00Z)

**Status:** DONE

**Commit sha:** 1388746

**Files modified:**
- `frontend/client/src/components/guards/AuthGuard.tsx` — T-5a: gate navigate on `!isLoading && !isAuthenticated`; render skeleton on `isLoading=true`; render `null` on `isLoading=false && !isAuthenticated`; render children only when authenticated.
- `frontend/client/src/components/guards/AuthGuard.test.tsx` (new) — 10 tests covering all 3 state branches.
- `frontend/client/src/components/pages/AuthCallbackPage.tsx` (deleted) — T-5b: dead code removed.
- `frontend/client/src/router.tsx` — removed `authCallbackRoute` definition and route-tree entry; removed `AuthCallbackPage` import.
- `frontend/client/src/tests/login-flow.test.tsx` — replaced `/auth/callback` existence test with assertion that callback route does NOT exist in SPA router.
- `frontend/client/src/tests/login-first-navigation.test.tsx` — replaced `AuthCallbackPage` spinner test with assertion that `/auth/callback` falls through to 404 in SPA router.
- `frontend/client/src/tests/login-form-redesign.test.tsx` — removed `AuthCallbackPage` import and its `describe` block; added tombstone comment.

**AuthCallbackPage decision: DELETED (option A)**

Proof: `frontend/client/app.config.ts` defines a Vinxi http router with `name: "auth"`, `type: "http"`, `handler: "./auth-server.ts"`, `base: "/auth"`. This router is type `"http"` (server-side), which means Vinxi routes ALL requests matching `/auth/*` to `auth-server.ts` before the SPA handler is consulted. `GET /auth/callback` is therefore handled server-side by `auth-server.ts` (which performs the PKCE token exchange and sets HttpOnly cookies), and the SPA never loads for that URL. `AuthCallbackPage` with its `useEffect` polling loop could never mount.

Grep confirms zero remaining references to `AuthCallbackPage` after deletion: all three import sites in `router.tsx`, `login-flow.test.tsx`, and `login-form-redesign.test.tsx` removed. `login-first-navigation.test.tsx` updated in place.

**Test results:** `pnpm typecheck` → 0 errors. `pnpm vitest run` (5 affected files) → 31/31 pass.

**Vinext migration debt:** None. No Vinxi-internal APIs introduced. The `app.config.ts` router configuration is declarative and compatible with Vinext migration path.

**Tech debt observed (out of scope):** `AuthGuard.tsx` line with `"Verificando autenticacao..."` is a hardcoded pt-BR string, violating the i18n rule (CLAUDE.md). Left in place — string predates this phase's scope. Should be addressed in a dedicated i18n cleanup phase.

---

### T-4 (2026-05-16T00:00:00Z)

**Status:** PASS --- zero code changes. All four checks confirmed correct.

**Commit sha:** none --- verification only

**Files modified:** none --- verification only

---

#### Check 1 --- Issuer match

| Scheme | Config key | appsettings.json value | ValidIssuer resolved |
|---|---|---|---|
| BearerBackoffice | Keycloak:ValidIssuer | http://localhost:8180/realms/backoffice | http://localhost:8180/realms/backoffice |
| BearerClient | Keycloak:ClientRealmUrl .Replace(keycloak:8080 -> localhost:8180) | http://localhost:8180/realms/client (no internal hostname, Replace is no-op) | http://localhost:8180/realms/client |

Both ValidIssuer values resolve to http://localhost:8180/realms/{realm} using the public Keycloak URL, matching JWTs issued by Keycloak. No drift vs compose.yaml wiring (KEYCLOAK_REALM=client line 115 / KEYCLOAK_REALM=backoffice line 140). Authority values confirmed: BackofficeRealmUrl = http://localhost:8180/realms/backoffice, ClientRealmUrl = http://localhost:8180/realms/client. ValidateAudience = false on both schemes intentional (D-05).

#### Check 2 --- CORS allowlist (Program.cs:254)

Origins whitelisted: http://localhost:5173 (client SPA) and http://localhost:5174 (backoffice SPA). No wildcard, no AllowAnyOrigin(), no origin reflection. AllowCredentials() present. SecurityHeaders:AllowedOrigins in appsettings.json mirrors the same two origins. Matches D-15 gate exactly.

#### Check 3 --- Middleware order (Program.cs:284-294)

UseCors -> UseAuthentication -> UseAuthorization confirmed. Session middlewares (UseAdminSession, UseClientSession) run before UseAuthentication --- correct for cookie-to-Bearer conversion.

#### Check 4 --- Test results

| Suite | Passed | Failed | Skipped | Duration |
|---|---|---|---|---|
| Onboarding.API.Tests | 244 | 0 | 4 | ~1m 47s |
| Onboarding.Integration.Tests | 20 | 0 | 0 | ~3m 13s |

Both suites green. The 4 skipped tests are pre-existing (TracePropagationTests x2 + AdminCompanyDetailsTests x2).

---

**Conclusion:** Backend auth wiring is correct and aligned with runtime config. No defect found. No code change required.
