# Phase 49 --- auth-flow-fix --- SUMMARY

---

## Iter 3 entries (2026-05-16)

### T-10 (2026-05-16T20:35:00Z)

**Status:** DONE — all 3 scenarios pass in both SPAs

**Commit sha:** 149247c

**Iter-2 blocker resolved:** B-FE-1 / B-BE-1

**Blocker text (verbatim from REVIEW.md):**
> B-BE-1 / B-FE-1: api-proxy.spec.ts Scenario 3 in both SPAs — GET /api/healthz/live returns 404, not 200. Vinxi proxy prepends /api to every path: /api/healthz/live → http://api:8080/api/healthz/live (404). Backend healthz is at /healthz/live without /api prefix (Program.cs:298). Spec JSDoc comment "Proxies to http://api:8080/healthz/live" is factually wrong.

**Fix mechanism:**
Replaced Scenario 3 `GET /api/healthz/live → 200` with `GET /api/companies/registration → 405 Method Not Allowed`:
- Proxy forwards `GET /api/companies/registration` → `http://api:8080/api/companies/registration` (correct path, proxy adds /api prefix correctly).
- Backend only allows POST on this endpoint → returns 405 + `Allow: POST` header.
- Asserting `status===405` (not 503) + `Allow` header contains "POST" proves round-trip through proxy without requiring auth.
- Deterministic and unauthenticated — same as Scenario 2 (POST).

**Files modified:**
- `frontend/client/playwright/specs/api-proxy.spec.ts` — Scenario 3 replaced
- `frontend/backoffice/playwright/specs/api-proxy.spec.ts` — Scenario 3 replaced

**Playwright run:** client 3/3 pass, backoffice 3/3 pass (pw-no-setup.config.ts)

---

### T-11 (2026-05-16T20:50:00Z)

**Status:** DONE — D-17 narrowed, api-proxy 3/3 pass (both SPAs); auth-flow Scenarios 1/5/6 now pass (3/5 active); Scenarios 2/8 fail for a NEW reason (see new findings below)

**Commit sha:** baf5417

**Iter-2 blocker resolved:** B-FE-2 / B-BE-2

**Blocker text (verbatim from REVIEW.md):**
> B-BE-2 / B-FE-2: keycloak/client-realm.json and keycloak/backoffice-realm.json redirectUris lists only localhost variants. T-9 (D-17) changed Playwright baseURL to 127.0.0.1:PORT but did NOT add 127.0.0.1 variants to the Keycloak allowlists. Keycloak redirects to the registered localhost redirect_uri; PKCE state cookie is on the 127.0.0.1 origin and is not sent on the localhost callback; all auth-flow scenarios fail with Invalid state.

**Fix mechanism (option a — narrow D-17, no realm JSON changes):**
- `auth-flow.spec.ts` BASE_URL: `http://127.0.0.1:5173` → `http://localhost:5173`
- `admin-auth-flow.spec.ts` BASE_URL: `http://127.0.0.1:5174` → `http://localhost:5174`
- `playwright.config.ts` (client): auth-flow project gets `baseURL: 'http://localhost:5173'` override; api-proxy project inherits `http://127.0.0.1:5173` from global `use`
- `pw-no-setup.config.ts` (client): same per-project split
- `playwright.config.ts` (backoffice): backoffice-auth project gets `baseURL: 'http://localhost:5174'` override; api-proxy inherits `http://127.0.0.1:5174`
- `.jdi/DECISIONS.md` D-17: appended "Refined 2026-05-16 (iter 3)" note preserving original D-17 text immutable

**Files modified:**
- `frontend/client/playwright.config.ts`
- `frontend/client/pw-no-setup.config.ts`
- `frontend/client/playwright/specs/auth-flow.spec.ts`
- `frontend/backoffice/playwright.config.ts`
- `frontend/backoffice/playwright/specs/admin-auth-flow.spec.ts`
- `.jdi/DECISIONS.md`

**Playwright run results:**
- client api-proxy: 3/3 pass (127.0.0.1 unchanged) ✓
- backoffice api-proxy: 3/3 pass (127.0.0.1 unchanged) ✓
- client auth-flow: Scenarios 1/5/6 PASS, Scenario 7 SKIP (intentional), Scenarios 2/8 FAIL (new finding — see below)

**New findings (out of T-11 scope — logging per workflow rule):**

NF-1 (Scenario 2 — client logout): The spec waits for `page.waitForURL('http://localhost:5173/auth/login')` after logout. But `/auth/login` is a server-side route (h3 handler) that immediately 302-redirects to Keycloak's authorize endpoint. Playwright follows redirects automatically — the browser never "lands" on `localhost:5173/auth/login`; it goes straight to `localhost:8180/realms/client/...`. The spec should `waitForURL(/localhost:8180.*openid-connect\/auth/)` or assert the Keycloak login form is visible. This is a spec defect introduced in T-7 that was never observable before (auth-flow was environment-blocked in iter-1 and broken by D-17 in iter-2). Fix requires updating the spec assertion — not a product bug.

NF-2 (Scenario 8 — cookie-blocked): After clearing cookies and navigating to `/profile`, the SPA redirects to `/auth/login` which goes to Keycloak. If the previous Keycloak SSO session is still alive (because Scenario 1's login was not followed by logout), Keycloak silently re-authenticates and redirects to `/auth/callback`. The callback handler finds no `pkce_state` cookie (cleared) → `Invalid state` error. Playwright sees `net::ERR_ABORTED` because the page load is aborted mid-redirect chain. Fix: the test should either logout before clearing cookies, or suppress the SSO session by navigating in an isolated browser context that never authenticated. Not a product bug.

---

### T-12 (2026-05-16T20:45:00Z)

**Status:** DONE — 3 new Vitest assertions pass; logout URL verified correct in container

**Commit sha:** 419c40a

**Iter-2 blocker/warning resolved:** W-FE-3 / W1 (elevated to exploitable by iter-2 frontend reviewer)

**Blocker text (verbatim from REVIEW.md):**
> W-FE-3 (confirmed active, elevated): frontend/client/auth-server.ts:171 logout URL missing client_id param. Confirmed broken: KC26 returns "Missing parameters: id_token_hint". KC SSO session NOT terminated. User can re-authenticate without credentials via SSO during KC session lifetime.

**Fix mechanism:**
Added `&client_id=${encodeURIComponent(CLIENT_ID)}` to `fullUrl` in the logout handler at `auth-server.ts:171`. Mirrors the backoffice pattern at line 270 verbatim.

Before: `?post_logout_redirect_uri=...`
After: `?post_logout_redirect_uri=...&client_id=onboarding-client-acf`

**Files modified:**
- `frontend/client/auth-server.ts` — logout URL + inline comment citing W-FE-3/W1
- `frontend/client/auth-server.test.ts` — 3 new static source assertions (T-12 block)

**Vitest:** 17/17 pass in auth-server.test.ts (10 pre-existing + 3 cookie attribute + 3 new T-12 + 1... wait, re-counting: 7 realm fail-fast + 7 cookie attribute + 3 logout = 17). All pass.

**Live verification:** curl `http://localhost:5173/auth/logout` after container restart returns Location header containing `&client_id=onboarding-client-acf`.

---

### T-13 (2026-05-16T20:38:00Z)

**Status:** DONE — path depth corrected, compose.yaml guard added, backoffice api-proxy suite now runs

**Commit sha:** 2b24c20

**Iter-2 warning resolved:** W-BE-5 (also W-FE-4 from frontend reviewer)

**Blocker text (verbatim from REVIEW.md):**
> W-BE-5 / W-FE-4: frontend/backoffice/playwright/global-setup.ts:87: path.resolve(__dirname, '../../../..') (4 levels up from playwright/) resolves to D:\REPO\ (parent of repo root, not repo root). docker compose ps fails. Main playwright.config.ts blocked.

**Fix mechanism:**
- Changed `path.resolve(__dirname, '../../../..')` → `path.resolve(__dirname, '../../..')` (3 levels up → repo root)
- Added `existsSync(compose.yaml)` guard: throws with actionable message if resolved path does not contain compose.yaml, making future depth regressions immediately visible
- Added ESM shims (`fileURLToPath` + `__filename`/`__dirname`) that were missing from the original file (causing `ReferenceError: __dirname is not defined` in ESM context)
- Added `import { existsSync } from 'fs'` (ESM context cannot use `require`)
- Reused `resolvedRoot` for the Step 2 seed script `cwd` (eliminating the second copy of the wrong depth)
- Created `frontend/backoffice/pw-no-setup.config.ts` (mirrors client pattern) for running api-proxy probes without the jq-blocked seed step (W-FE-5/W-BE-6 pre-existing)

**Files modified/created:**
- `frontend/backoffice/playwright/global-setup.ts` — path depth + ESM shims + compose.yaml guard + fs import
- `frontend/backoffice/pw-no-setup.config.ts` (new)

**Note:** Client `global-setup.ts` path depth is correct at 3 levels and was not changed.

---

### T-9 (2026-05-16T17:30:00Z)

**Status:** DONE — compile verified via `pnpm exec playwright test --list`; actual run env-blocked (no docker compose in agent sandbox — see ship-time validation)

**Commit sha:** a5edf06

**Files modified / created:**
- `frontend/client/playwright.config.ts` — baseURL `http://127.0.0.1:5173` (D-17); added `api-proxy` project (testMatch `/api-proxy\.spec\.ts/`)
- `frontend/client/pw-no-setup.config.ts` (previously untracked, now committed) — baseURL `http://127.0.0.1:5173`; added `api-proxy` project
- `frontend/client/playwright/specs/auth-flow.spec.ts` — `BASE_URL` constant + inline URL comment swapped to `http://127.0.0.1:5173`
- `frontend/client/playwright/specs/api-proxy.spec.ts` (new) — 3 scenarios, client SPA port 5173
- `frontend/backoffice/playwright.config.ts` — baseURL `http://127.0.0.1:5174` (D-17); updated JSDoc comment; added `api-proxy` project
- `frontend/backoffice/playwright/specs/admin-auth-flow.spec.ts` — `BASE_URL` constant + inline URL comment swapped to `http://127.0.0.1:5174`
- `frontend/backoffice/playwright/specs/api-proxy.spec.ts` (new) — 3 scenarios, backoffice SPA port 5174

**Scenarios implemented (6 total, 3 per SPA):**

Client SPA (`api-proxy.spec.ts` — project `api-proxy`, port 5173):
1. Single listener guard: `Get-NetTCPConnection -LocalPort 5173` (win32) / `ss -tlpn` (Linux) / `netstat` fallback. Fails with PIDs + cleanup command if >1 listener detected. `test.skip` gracefully when probe unavailable.
2. `POST /api/companies/registration` empty body → asserts 422, `content-type: application/(problem+)?json`, body parses as JSON with `title` field. Hard-fails with Bug 3 diagnostic if 503 HTML or `TypeError: fetch failed` in body.
3. `GET /api/healthz/live` → asserts 200, body `"Healthy"`. Same hard-fail guard.

Backoffice SPA (`api-proxy.spec.ts` — project `api-proxy`, port 5174):
Same 3 scenarios mirrored for port 5174. POST scenario uses `POST /api/companies/registration` (same unauthenticated backend endpoint, both SPAs proxy to the same `http://api:8080`).

**`pnpm exec playwright test --list` output:**
- Client: 3 tests in 1 file (project `api-proxy`, `pw-no-setup.config.ts`)
- Backoffice: 3 tests in 1 file (project `api-proxy`, `playwright.config.ts`)
- Total: 6 new scenarios

**Listener probe platform-handling decision:**
- Windows: PowerShell `Get-NetTCPConnection -LocalPort <port> -State Listen | Select-Object -ExpandProperty OwningProcess`. Returns `null` (skip) if PowerShell unavailable.
- Linux: `ss -tlpn "sport = :PORT"` extracting `pid=N` from output, deduplicated. Falls back to `netstat -tlpn | awk` if `ss` unavailable. Returns `null` (skip) if both fail.
- 0 listeners → skip (compose not up); 1 listener → pass; >1 → hard-fail with PID list + `Stop-Process` / `pkill` instructions.

**Actual E2E run:** env-blocked — no `docker compose up -d` in agent sandbox. Ship-time validation: reviewer runs `pnpm --filter ./frontend/client exec playwright test api-proxy --config=pw-no-setup.config.ts` and `pnpm --filter ./frontend/backoffice exec playwright test api-proxy` against a fresh `docker compose up -d` with no stale vinxi-host. Expected: 6/6 pass.

**Zero `http://localhost:5173|5174` in Playwright surface confirmed:** grep of all three config files + all four spec files returns zero hits. Remaining `http://localhost:8180` in `global-setup.ts` files are Keycloak URL defaults — outside D-17 scope.

**No production code touched:** `server.ts`, `auth-server.ts`, `app.config.ts`, `src/**`, `package.json` — all unmodified.

**Vinext migration debt:** None. All changes are test configuration and spec files. No Vinxi-internal APIs.

---

### T-8 (2026-05-16T17:00:00Z)

**Status:** DONE

**Commit sha:** bd8f742

**Files modified / created:**
- `scripts/check-dev-env.mjs` (new) — pure-Node guard; exported `run(serviceNames, execSyncImpl)` for testability; `isMain` detection via `argv[1]` suffix; bypass via `ALLOW_HOST_DEV=1|true|yes`; soft-exit when docker is unavailable; actionable error message cites D-16 and lists PowerShell + Linux cleanup commands
- `scripts/check-dev-env.test.mjs` (new) — 9 Vitest (node env) cases: clean exit 0, conflict exit 1, multi-service conflict, multi-service clean, bypass `=1`, bypass `=true`, bypass `=yes`, docker-unavailable soft-exit, empty output
- `vitest.config.mjs` (new) — root-level Vitest config (environment: node) targeting `scripts/**/*.test.{mjs,ts,js}`; runs independently of SPA jsdom configs
- `frontend/client/package.json` — added `"predev": "node ../../scripts/check-dev-env.mjs frontend-client"` before `"dev"`
- `frontend/backoffice/package.json` — added `"predev": "node ../../scripts/check-dev-env.mjs frontend-backoffice"` before `"dev"`
- `docs/dev-setup.md` (new, 86 lines) — official compose-only workflow, detect/clean stale host Vinxi via `Get-NetTCPConnection` (Windows) and `ss -tlpn` (Linux), `ALLOW_HOST_DEV` escape hatch, D-16 + D-17 citations
- `README.md` — added "Local Development" section (2 sentences) linking `docs/dev-setup.md`; updated Quick Start to remove `npm run dev` host invocations
- `CONTRIBUTING.md` — replaced dangerous `npm run dev` setup instructions with compose-first workflow + IMPORTANT warning + predev guard explanation + `docs/dev-setup.md` link

**Behavior summary:**
- `node scripts/check-dev-env.mjs frontend-client` while compose up → exits 1, prints actionable error with D-16 explanation, cleanup commands, and `ALLOW_HOST_DEV=1` escape hatch
- `ALLOW_HOST_DEV=1 node scripts/check-dev-env.mjs frontend-client` → exits 0, prints bypass notice
- `node scripts/check-dev-env.mjs nonexistent-service` → exits 0 (service not in compose)
- `docker compose` unavailable (throws) → exits 0 with soft warning (does not block greenfield envs)
- `pnpm dev` in `frontend/client/` while compose up → blocked by predev hook, guard exits 1

**Test counts:** 9/9 pass (`node_modules/.bin/vitest run --config vitest.config.mjs`)

**Test command:** `node_modules/.bin/vitest run --config vitest.config.mjs` (from repo root)

**Vinext migration debt:** None. `scripts/check-dev-env.mjs` has no Vinxi dependency. `predev` hooks are standard npm lifecycle scripts compatible with any bundler.

---

### T-7 (2026-05-16T15:00:00Z)

**Status:** DONE — deferred actual run to reviewer (docker compose unavailable in agent sandbox)

**Commit sha:** f7a2b46

**Files modified / created:**
- `.gitignore` — added explicit backoffice and client playwright output dirs
- `frontend/backoffice/package.json` — added `@playwright/test ^1.59.1`, scripts `test:e2e`, `test:e2e:ui`, `test:e2e:report`
- `frontend/backoffice/playwright.config.ts` (new) — baseURL :5174, testDir `./playwright/specs`, `globalSetup`, sequential workers, `playwright-report` output
- `frontend/backoffice/playwright/global-setup.ts` (new) — verifies `docker compose ps` health for `keycloak`, `api`, `frontend-backoffice`; invokes `scripts/seed-test-users.sh` idempotently
- `frontend/backoffice/playwright/specs/admin-auth-flow.spec.ts` (new) — 4 backoffice scenarios (3, 4, 5-backoffice, 6-backoffice)
- `frontend/backoffice/pnpm-lock.yaml` (new) — lockfile after adding `@playwright/test`
- `frontend/client/playwright.config.ts` — added `globalSetup` + new `auth-flow` project (testDir `./playwright/specs`)
- `frontend/client/playwright/global-setup.ts` (new) — verifies `docker compose ps` health for `keycloak`, `api`, `frontend-client`; invokes seed script
- `frontend/client/playwright/specs/auth-flow.spec.ts` (new) — 6 client scenarios (1, 2, 5, 6, 7-skip, 8)
- `frontend/client/pnpm-lock.yaml` (new)

**Scenarios implemented (8 total across both SPAs):**

Client SPA (`auth-flow.spec.ts` — project `auth-flow`, port 5173):
1. Client login happy path → `/profile`, `code_challenge_method=S256` intercepted, `localStorage.length===0 && sessionStorage.length===0` (D-12)
2. Client logout → clears session, `/auth/me` returns 401
5. Client post-login race → no transient `/auth/login` URL between `/auth/callback` and `/profile`
6. Client refresh resilience → reload stays on `/profile`
7. `test.skip` — expired-token refresh: httpOnly cookies not inspectable from browser; `/auth/refresh` path covered by auth-context unit tests; documented rationale inline
8. Cookie-blocked graceful error → `context.clearCookies()` + visit `/profile` → redirect to `/auth/login`, no loop, login button visible

Backoffice SPA (`admin-auth-flow.spec.ts` — project `backoffice-auth`, port 5174):
3. Backoffice login happy path → `/admin/companies`, PKCE S256, no storage (D-12)
4. Backoffice logout → `/auth/login`, `/auth/me` returns 401
5. (backoffice) Post-login race → no transient `/admin/login` URL between callback and `/admin/companies`
6. (backoffice) Refresh resilience → reload stays on `/admin/companies`, loading shell not visible after restore

**Test command + result:**
- `cd frontend/client && npx playwright test --list --project=auth-flow` → 6 tests in 1 file (compile verified)
- `cd frontend/backoffice && pnpm test:e2e --list` → 4 tests in 1 file (compile verified)
- `pnpm typecheck && pnpm lint` → 0 errors in both SPAs
- `pnpm vitest run` → client 11 failed / 20 passed (pre-existing, same count as before T-7); backoffice 1 failed / 19 passed (pre-existing, same count as before T-7). T-7 introduces zero new vitest failures.
- **Actual E2E run: deferred to reviewer.** Agent sandbox does not have `docker compose up -d`. Reviewer executes `/jdi-verify` with compose stack healthy and `KC_ADMIN_CLIENT_SECRET=dev-admin-secret` (or from `.env`). Expected: 10 tests run (5 client active + 4 backoffice + 1 skip), 9/9 pass.

**Note on `npx playwright` vs `pnpm exec playwright` for backoffice:**
The root `node_modules/playwright` (older version) shadows `npx playwright` resolution when run from `frontend/backoffice/`. The `package.json` scripts use `pnpm exec playwright` to correctly resolve the locally-installed `@playwright/test 1.60.0`. This is documented in `playwright.config.ts` comments.

**Vinext migration debt:** None. All changes are test-only files with no Vinxi-internal APIs.

---

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
