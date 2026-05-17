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

#### T-7: Playwright regression suite — both SPAs, 8 scenarios (DONE — iter 1)
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

### Wave 4 (added iter 2, 2026-05-16 — api-proxy 503 bug surfaced post-convergence)

Root cause documented in `INVESTIGATION-api-proxy.md`: vinxi-host stale processes (Node 24, started by `pnpm dev` on Windows host) intercept `localhost:5173`/`:5174` via IPv6, cannot reach Docker bridge → 503 `TypeError: fetch failed`. Fix is workflow-level guards + IPv4-pinning of Playwright configs. NO production code changes.

#### T-8: Dev-workflow guard — `scripts/check-dev-env.mjs` + `predev` hook + docs
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:** `scripts/check-dev-env.mjs` (new), `frontend/client/package.json`, `frontend/backoffice/package.json`, `docs/dev-setup.md` (new), `README.md`, `CONTRIBUTING.md`, `scripts/check-dev-env.test.mjs` (new)
- **Acceptance:**
  - `scripts/check-dev-env.mjs` runs `docker compose ps --status running --services`, exits 1 with actionable message if `frontend-client` or `frontend-backoffice` are running, exits 0 otherwise. Bypass via `ALLOW_HOST_DEV=1`. Cross-platform (Node-only, no shell-specific syntax).
  - `frontend/client/package.json` and `frontend/backoffice/package.json` have `"predev": "node ../../scripts/check-dev-env.mjs <service>"` ahead of the existing `"dev"` script.
  - `docs/dev-setup.md` documents official workflow (`docker compose up`), how to detect vinxi-host stale via `Get-NetTCPConnection -LocalPort 5173,5174 -State Listen` / `ss -tlpn`, how to kill (`Stop-Process` on Windows, `pkill node` on *nix), and the `ALLOW_HOST_DEV=1` escape hatch.
  - `README.md` gains a "Local development" section linking to `docs/dev-setup.md` and a one-line warning. `CONTRIBUTING.md` mirrors the warning at first-time setup.
  - Vitest in `scripts/check-dev-env.test.mjs` covers (a) clean exit 0 when no containers, (b) exit 1 when target container running, (c) bypass via `ALLOW_HOST_DEV=1`.
- **Dependencies:** none
- **Test:** `pnpm vitest run scripts/check-dev-env` (or wherever the runner picks it up); manual sanity: run `node scripts/check-dev-env.mjs frontend-client` with and without compose up.
- **Status:** pending

#### T-9: Playwright IPv4 hardening + api-proxy regression specs
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:** `frontend/client/playwright.config.ts`, `frontend/client/pw-no-setup.config.ts`, `frontend/backoffice/playwright.config.ts`, `frontend/client/playwright/specs/api-proxy.spec.ts` (new), `frontend/backoffice/playwright/specs/api-proxy.spec.ts` (new)
- **Acceptance:**
  - All three Playwright config files use `baseURL: 'http://127.0.0.1:PORT'` (no `localhost`). PORT stays 5173 / 5174.
  - `frontend/{client,backoffice}/playwright/specs/api-proxy.spec.ts` covers three scenarios each:
    1. Single listener guard: `globalSetup` (or first test) shells out to `netstat`/`Get-NetTCPConnection` and asserts exactly ONE process listens on the SPA port. Skip the scenario gracefully on platforms where the command is unavailable (CI Linux runners use `ss -tlpn`).
    2. Proxy reaches backend on POST: `POST /api/companies/registration` with empty body returns **422 JSON** (`application/problem+json`), never 503 HTML. Asserts on `content-type` header and body shape.
    3. Proxy reaches healthz on GET: `GET /api/healthz/live` returns 200 with body `Healthy`.
  - No production code (`server.ts`, `auth-server.ts`, `app.config.ts`, backend) is touched.
- **Dependencies:** T-8 (compose must be up + no host vinxi for the Playwright run to succeed; T-8's guard makes this enforceable; can be developed in parallel but verified together).
- **Test:** `pnpm --filter ./frontend/client exec playwright test api-proxy` and `pnpm --filter ./frontend/backoffice exec playwright test api-proxy` against a fresh `docker compose up -d`.
- **Status:** pending

### Wave 5 (added iter 4, 2026-05-16 — clean residual warnings before ship)

User chose to NOT ship at iter-3 convergence and instead address remaining test-defect + config + script warnings. All four added below. Phase 49 stays open until iter 4 converges APPROVED or APPROVED_WITH_WARNINGS once more (re-judging the residual set).

#### T-14: Fix NF-1 — logout spec `waitForURL` defect (client + backoffice)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:** `frontend/client/playwright/specs/auth-flow.spec.ts`, `frontend/backoffice/playwright/specs/admin-auth-flow.spec.ts`
- **Acceptance:**
  - Scenario 2 (client logout) and the backoffice equivalent assert on the FINAL post-logout URL the browser actually dwells on (NOT the server-side 302 hop `/auth/login` or `/admin/login` that immediately redirects to Keycloak). Use `page.waitForURL` matching either (a) the Keycloak `/realms/.../logout` page, OR (b) the SPA URL the user lands on after the full logout chain completes. Read `auth-server.ts` logout handler to identify the canonical resting URL.
  - Both scenarios also assert `/auth/me` returns 401 immediately after the logout request finishes (keep this — it's the strong invariant).
  - No production code change.
- **Dependencies:** none
- **Test:** `pnpm exec playwright test auth-flow` in client; `pnpm exec playwright test admin-auth-flow` in backoffice. Both Scenario 2-equivalent passes.
- **Status:** pending

#### T-15: Fix NF-2 — cookie-blocked spec needs isolated context
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:** `frontend/client/playwright/specs/auth-flow.spec.ts` (Scenario 8 + any analogous backoffice scenario)
- **Acceptance:**
  - Scenario 8 (cookie-blocked graceful error) uses a fresh `browser.newContext()` (never authenticated) instead of relying on `clearCookies()` on an already-authenticated context. This prevents the residual Keycloak SSO session from silently re-authenticating mid-test.
  - The scenario still asserts: visiting a protected route without cookies redirects to `/auth/login` (or `/admin/login`), shows an actionable error/login UI, no infinite redirect loop.
  - Same pattern applied to any backoffice equivalent that suffers the same race.
  - No production code change.
- **Dependencies:** none (different scenario from T-14 in the same files but easy disjoint edits)
- **Test:** `pnpm exec playwright test auth-flow -g "cookie"` (or whatever the scenario is named). Pass.
- **Status:** pending

#### T-16: Fix W-FE-1 — `vitest.config.ts` exclude `playwright/specs/`
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:** `frontend/client/vitest.config.ts` (and `frontend/backoffice/vitest.config.ts` if it has the same issue)
- **Acceptance:**
  - `vitest` `test.exclude` (or `test.include`) is configured so files under `playwright/specs/**` are NEVER picked up. Run `pnpm vitest run` from each SPA to confirm the suite no longer attempts to load the Playwright specs.
  - Net effect: client vitest passing count moves from 113/128 to 113/X where X reflects only real vitest files (the structural failure noise from the playwright spec disappears).
- **Dependencies:** none
- **Test:** `pnpm vitest run` in each SPA. Zero references to `playwright/specs/` in the runner output.
- **Status:** pending

#### T-17: Fix W-FE-5 / W-BE-6 — `scripts/seed-test-users.sh` jq dependency
- **Specialist:** jdi-doer-onboarding-keycloak-security
- **Files modified:** `scripts/seed-test-users.sh`, possibly `scripts/seed-test-users.lib.sh` (extracted helpers; only if length crosses ~150 lines)
- **Acceptance:**
  - Either (a) replace `jq` usage with `python3 -c '...'` parsing (Python ships with Linux runners and is on most dev hosts), OR (b) use POSIX shell parameter expansion / `grep -oE` for the specific JSON paths read by the script (small surface — only `access_token`, user-id lookup, etc).
  - Re-running the script after `docker compose down -v && docker compose up -d` still produces idempotent seed (no duplicates, no failures).
  - Top-of-file comment updated to reflect the new dependency set (or its absence).
  - If you keep `jq` as the primary path with a Python fallback, document both invariants.
- **Dependencies:** none
- **Test:** Manual on a host without `jq` (or simulate by `PATH=$(echo $PATH | tr ':' '\n' | grep -v jq | paste -sd:)` if available; otherwise just verify the Python fallback path runs).
- **Status:** pending

### Wave 6 (added iter 5, 2026-05-17 — final hardening before ship)

Iter 4 converged APPROVED_WITH_WARNINGS. User chose iter 5 to address the structural `id_token_hint` finding (W-BE-11/W-SEC-IT4-1), the backoffice S5 spec re-design (W-BE-10), and two pre-existing security/UX polish items (W3, W4).

#### T-18: Capture `id_token` and forward as `id_token_hint` in logout (both SPAs)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext (with mandatory security re-review trigger; auth surface touched)
- **Files modified:** `frontend/client/src/lib/auth-code-flow.ts`, `frontend/backoffice/src/lib/auth-code-flow.ts`, `frontend/client/auth-server.ts`, `frontend/backoffice/auth-server.ts`, plus existing tests `auth-server.test.ts` in both SPAs
- **Acceptance:**
  - `exchangeCodeForTokens` (`auth-code-flow.ts`) includes `id_token` in the returned object (currently discarded).
  - `auth-server.ts` `/auth/callback` handler stores `id_token` in a short-lived HttpOnly cookie (`*_id_token`, same security attributes as access token: `httpOnly:true`, `secure:IS_PROD`, `sameSite:lax`, `path:"/"`, `maxAge` matches token TTL).
  - `auth-server.ts` `/auth/logout` handler reads `*_id_token` cookie and appends `id_token_hint=${encodeURIComponent(idToken)}` to the Keycloak `end_session_endpoint` URL. Cookie deleted before redirect (same pattern as access/refresh tokens).
  - When `id_token` cookie absent (e.g. older session), fall back to existing `client_id` path — do NOT hard-fail.
  - Live Playwright verification: backoffice logout no longer shows Keycloak "Do you want to log out?" confirmation page; goes straight back to `/admin/login`. Client also auto-completes.
  - D-12 preserved: id_token never written to localStorage/sessionStorage. D-15 strengthened: `end_session_endpoint` now spec-conformant with `id_token_hint` primary + `client_id` fallback.
- **Dependencies:** none
- **Test:** Vitest unit test asserts `id_token_hint=` present in logout URL when cookie exists; absent gracefully when cookie missing. Playwright auth-flow + admin-auth-flow logout scenarios pass without test-side regex workaround for confirmation page.
- **Status:** pending

#### T-19: Re-order Playwright `framenavigated` listener + add `callbackIndex` guard
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:** `frontend/backoffice/playwright/specs/admin-auth-flow.spec.ts` (S5), plus client equivalent if same pattern exists
- **Acceptance:**
  - The `page.on('framenavigated')` listener is registered BEFORE `page.goto('/admin/login')` (or whichever initial navigation kicks off the test) so `/auth/callback` navigation is captured.
  - The Scenario 5 assertion guards `if (callbackIndex === -1) test.skip(true, '...')` — skip with reason instead of asserting against `slice(0)` which captures pre-callback navigations.
  - Backoffice S5 passes against live `docker compose`.
- **Dependencies:** none (different file from T-18)
- **Test:** `pnpm exec playwright test admin-auth-flow -g "race"` (or whatever S5 is named) passes.
- **Status:** pending

#### T-20: `keycloak/client-realm.json` clientProfiles parity (W3)
- **Specialist:** jdi-doer-onboarding-keycloak-security
- **Files modified:** `keycloak/client-realm.json`
- **Acceptance:**
  - `clientProfiles` block added matching `backoffice-realm.json` structure: `enforce-no-wildcard-redirects` profile with `secure-redirect-uris-enforcer` executor.
  - `clientPolicies` block added enabling the profile on the realm.
  - `bruteForceProtected:true` preserved.
  - All other realm settings unchanged.
  - `tests/keycloak-hardening/` static checks pass (or replicate-as-grep validation).
- **Dependencies:** none
- **Test:** Manual: confirm `docker compose down -v && docker compose up -d` boots Keycloak cleanly with new realm config (no parse errors). Existing Playwright auth-flow still passes.
- **Status:** pending

#### T-21: Mask seed passwords in stdout (W4)
- **Specialist:** jdi-doer-onboarding-keycloak-security
- **Files modified:** `scripts/seed-test-users.sh`
- **Acceptance:**
  - Final echo summarizing seeded users no longer prints the literal `E2EClient@123!` / `E2EAdmin@123!` passwords. Replace with `********` placeholder, or print only "(password set per .env / D-14)".
  - Idempotency preserved.
  - Re-running script does not regress prior behavior.
- **Dependencies:** none
- **Test:** Run script, grep stdout for `E2EClient@123!` / `E2EAdmin@123!` — must be zero matches.
- **Status:** pending

## Execution
- Total tasks: 17 (T-1..T-7 iter 1; T-8/T-9 iter 2; T-10..T-13 iter 3; T-14..T-17 iter 4; T-18..T-21 iter 5)
- Waves: 6 (Waves 1-5 historical; Wave 6 = T-18 + T-19 frontend + T-20 + T-21 security, all 4 parallel-eligible — disjoint files)
- Estimated parallel speedup at full run: ~17/6 ≈ 2.8x

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
