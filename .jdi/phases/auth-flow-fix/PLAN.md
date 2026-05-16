# Phase 49: auth-flow-fix — Plan  (slug: auth-flow-fix)

## Goal
Diagnose and fix 2 ACF+PKCE bugs affecting both SPAs (client :5173, backoffice :5174):
1. Login/logout falls back into Keycloak ACF auth page (wrong realm fallback `"onboarding"` + missing `validPostLogoutRedirectUris`).
2. Post-login error/login flash that disappears on reload (race between `tryRestore()` and route guards).

Deliver end-to-end fix + Playwright regression suite running against the `docker compose` stack covering login, logout, refresh, and "post-login error flash" for both SPAs. Security gates from D-15 are non-negotiable.

## Locked decisions (Phase 49)
- **D-11:** ACF+PKCE locked. S256 + confidential client secret preserved.
- **D-12:** Access/refresh tokens exclusively in HttpOnly cookies. No browser storage.
- **D-13:** Compose-driven repro authorized. Keycloak changes persisted in `keycloak/*-realm.json` before commit.
- **D-14:** Test users via fixture — `scripts/seed-test-users.sh` (idempotent) or `users` block in realm JSONs.
- **D-15:** Non-negotiable security gates — PKCE S256, HttpOnly+Secure cookies, exact CORS allowlist, CSRF (state + SameSite), `bruteForceProtected`, full `end_session_endpoint` logout.

## Tasks

### Wave 1 (parallel-eligible)

#### T-1: Keycloak realm hardening — postLogoutRedirectUris + webOrigins audit
- **Specialist:** jdi-doer-onboarding-keycloak-security
- **Files modified:** `keycloak/client-realm.json`, `keycloak/backoffice-realm.json`
- **Acceptance:**
  - Both clients (`onboarding-client`, `onboarding-backoffice`) carry `attributes."post.logout.redirect.uris"` (Keycloak 26 syntax) covering each SPA's login URL.
  - `webOrigins` whitelist matches CORS allowlist exactly (no `*`, no `+`).
  - `redirectUris` includes success URL + `?retry=1` variant per existing convention.
  - `bruteForceProtected:true` preserved on both realms.
- **Dependencies:** none
- **Test:** `tests/keycloak-hardening/` automated checks pass; manual `curl` validates `end_session_endpoint` 302 lands on configured SPA URL.
- **Status:** pending

#### T-2: auth-server.ts realm config + cookie sameSite decision (both SPAs)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:** `frontend/client/auth-server.ts`, `frontend/backoffice/auth-server.ts`, `.env.example`
- **Acceptance:**
  - `KEYCLOAK_REALM` resolution fail-fast: throws on undefined env. Per-SPA default (`"client"`/`"backoffice"`) only if explicitly justified; no `"onboarding"` fallback anywhere.
  - `.env.example` documents `KEYCLOAK_REALM` per SPA with commented examples.
  - `sameSite` decision applied uniformly across both SPAs (lax with justification OR strict + retry strategy in T-6). Rationale captured inline in SUMMARY.
  - All cookies keep `httpOnly:true`, `secure:NODE_ENV==='production'`, `path:'/'`.
- **Dependencies:** none
- **Test:** Vitest covers env resolution branches + cookie attribute snapshot. Playwright (T-7) integration gate.
- **Status:** pending

#### T-3: Test users fixture — `scripts/seed-test-users.sh`
- **Specialist:** jdi-doer-onboarding-keycloak-security
- **Files modified:** `scripts/seed-test-users.sh`
- **Acceptance:**
  - Idempotent bash script using `onboarding-api-admin` service account `client_credentials` to PUT `e2e-client@example.com` (client realm, group `pj-admin`) and `e2e-admin@example.com` (backoffice realm, realm role `admin`) with fixed dev passwords.
  - Empties `requiredActions` so first login skips UPDATE_PASSWORD/VERIFY_EMAIL.
  - Re-run after `docker compose down -v && docker compose up -d` reaches identical state (no duplicates, no failures).
  - Top-of-script comment documents preconditions + invocation.
- **Dependencies:** none
- **Test:** Manual smoke (run twice, both green). Playwright (T-7) consumes these users.
- **Status:** pending

#### T-4: Backend auth sanity — Program.cs issuer + CORS verification
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:** `src/Onboarding.API/Program.cs`, `src/Onboarding.API/appsettings.json` (only if defect surfaces; otherwise zero-change)
- **Acceptance:**
  - `BearerBackoffice` + `BearerClient` `ValidIssuer` matches `http://localhost:8180/realms/{realm}` for each (no drift vs `compose.yaml`).
  - CORS allowlist (`Program.cs:254`) covers `http://localhost:5173` + `http://localhost:5174` with `AllowCredentials()`, no wildcard.
  - `dotnet test tests/Onboarding.API.Tests tests/Onboarding.Integration.Tests` runs green against current branch.
  - If a defect is found, task converts to fix; SUMMARY notes the deviation.
- **Dependencies:** none
- **Test:** `dotnet test tests/Onboarding.API.Tests tests/Onboarding.Integration.Tests`. Coverage gate applies only if new files are added.
- **Status:** pending

### Wave 2 (parallel-eligible)

#### T-5: Frontend client race fix — AuthGuard isLoading + AuthCallbackPage cleanup
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:** `frontend/client/src/components/guards/AuthGuard.tsx`, `frontend/client/src/components/pages/AuthCallbackPage.tsx`, `frontend/client/src/router.tsx` (only if AuthCallbackPage route is removed)
- **Acceptance:**
  - `AuthGuard` waits for `auth.isLoading === false` before navigating to `/login`; renders skeleton/spinner during load.
  - `AuthCallbackPage` either deleted (dead code per CONTEXT hypothesis 3) or wired through a distinct route; one-line rationale in SUMMARY.
  - Logged-out users hitting protected routes still redirect to `/auth/login` (no regression).
- **Dependencies:** T-2
- **Test:** Vitest component tests cover (isLoading=true) + (isLoading=false, !authenticated) + (authenticated). Playwright (T-7) covers integration.
- **Status:** pending

#### T-6: Frontend backoffice race fix — AdminLayout redirect guard
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:** `frontend/backoffice/src/components/templates/AdminLayout.tsx`, `frontend/backoffice/src/lib/admin-auth-context.tsx`
- **Acceptance:**
  - `AdminLayout` redirect effect only fires when `isLoading === false`; renders loading shell during initial restore (no `null` mid-render flash).
  - `admin-auth-context.tsx` `tryRestore` retries `/auth/me` once with 200ms backoff on transient 401 (cookie-commit race); single bounded retry, no infinite loop.
  - `RedirectCompanies` (`/admin/users` → `/admin/companies`) preserved unless decision moves redirect server-side via `auth-server.ts:228` — option captured in SUMMARY.
- **Dependencies:** T-2
- **Test:** Vitest component tests for AdminLayout. Playwright (T-7) covers integration.
- **Status:** pending

### Wave 3

#### T-7: Playwright regression suite — both SPAs, 8 scenarios
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:** `frontend/client/playwright/specs/auth-flow.spec.ts`, `frontend/backoffice/playwright.config.ts` (new), `frontend/backoffice/playwright/specs/admin-auth-flow.spec.ts` (new), `frontend/backoffice/package.json`
- **Acceptance:**
  - 8 scenarios pass against fresh `docker compose up -d`:
    1. Client login happy path (`/auth/login` → KC → `/profile`).
    2. Client logout (`/profile` → KC `end_session` → `/auth/login`; `/auth/me` returns 401 immediately).
    3. Backoffice login happy (`/admin/login` → KC → `/admin/companies`).
    4. Backoffice logout (mirror of #2).
    5. Post-login race — assert no transient `/login` URL between callback and target route.
    6. Refresh resilience — reload authenticated page, still authenticated.
    7. Expired-token refresh — manipulate cookie expiry, validate silent refresh.
    8. Cookie-blocked graceful error — strip cookies, login attempt shows actionable message, no infinite loop.
  - Each test intercepts authorize URL and asserts `code_challenge_method=S256`.
  - `page.evaluate(() => ({ls: localStorage.length, ss: sessionStorage.length}))` returns `{0,0}` post-login (D-12 gate).
  - Suite registered in CI workflow (or deferred note in SUMMARY).
- **Dependencies:** T-1, T-2, T-3, T-4, T-5, T-6
- **Test:** This IS the test. Pass = 8/8 green. Reviewer re-runs at `/jdi-verify`.
- **Status:** pending

## Execution
- Total tasks: 7
- Waves: 3 (Wave 1: T-1, T-2, T-3, T-4 — 4 parallel; Wave 2: T-5, T-6 — 2 parallel; Wave 3: T-7)
- Estimated parallel speedup: ~7/3 ≈ 2.3x

## Files modified (all tasks)
- `keycloak/client-realm.json`
- `keycloak/backoffice-realm.json`
- `frontend/client/auth-server.ts`
- `frontend/backoffice/auth-server.ts`
- `.env.example`
- `scripts/seed-test-users.sh`
- `frontend/client/src/components/guards/AuthGuard.tsx`
- `frontend/client/src/components/pages/AuthCallbackPage.tsx`
- `frontend/client/src/router.tsx` (conditional)
- `frontend/backoffice/src/components/templates/AdminLayout.tsx`
- `frontend/backoffice/src/lib/admin-auth-context.tsx`
- `frontend/client/playwright/specs/auth-flow.spec.ts`
- `frontend/backoffice/playwright.config.ts` (new)
- `frontend/backoffice/playwright/specs/admin-auth-flow.spec.ts` (new)
- `frontend/backoffice/package.json`
- `src/Onboarding.API/Program.cs` (conditional)
- `src/Onboarding.API/appsettings.json` (conditional)

## Test requirements
- Frontend unit: `cd frontend/client && pnpm vitest run`, `cd frontend/backoffice && pnpm vitest run`
- Frontend e2e: `cd frontend/client && pnpm playwright test`, `cd frontend/backoffice && pnpm playwright test`
- Backend: `dotnet test tests/Onboarding.API.Tests tests/Onboarding.Integration.Tests`
- Keycloak hardening: `dotnet test tests/keycloak-hardening` (or invocation captured by `tests/keycloak-hardening/run.*`)
- Minimum coverage: 80% on new files only (D-2 boundary 968eefb)
- Security: full 13-tool pipeline via `/jdi-verify` cross-cutting reviewer trigger
