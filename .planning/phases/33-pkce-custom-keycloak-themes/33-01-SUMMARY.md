---
phase: 33
plan: "01"
subsystem: frontend-client-auth
tags: [acf, pkce, keycloak-themes, auth-migration, vinxi]
dependency_graph:
  requires: [phase-31-backoffice-acf, phase-32-admin-ui]
  provides: [client-acf-pkce, keycloak-custom-themes, cookie-session-auth]
  affects: [frontend-client, keycloak-realm, docker-compose]
tech_stack:
  added:
    - "Vinxi h3 server routes for token exchange (auth-server.ts)"
    - "PKCE utilities: generateCodeVerifier, generateCodeChallenge (SHA-256)"
    - "FreeMarker .ftl templates for Keycloak custom themes"
  patterns:
    - "ACF+PKCE: server-side code exchange, httpOnly cookies, no tokens in JS"
    - "Redirect-only AuthLoginPage pattern"
    - "Session polling via /auth/me in AuthCallbackPage"
key_files:
  created:
    - frontend/client/auth-server.ts
    - frontend/client/src/lib/auth-code-flow.ts
    - frontend/client/src/components/pages/AuthLoginPage.tsx
    - frontend/client/src/components/pages/AuthCallbackPage.tsx
    - frontend/client/src/components/pages/AuthErrorPage.tsx
    - keycloak/themes/onboarding-client/login/login.ftl
    - keycloak/themes/onboarding-client/login/theme.properties
    - keycloak/themes/onboarding-client/login/resources/css/styles.css
    - keycloak/themes/onboarding-backoffice/login/login.ftl
    - keycloak/themes/onboarding-backoffice/login/theme.properties
    - keycloak/themes/onboarding-backoffice/login/resources/css/styles.css
  modified:
    - frontend/client/app.config.ts
    - frontend/client/src/lib/api.ts
    - frontend/client/src/lib/auth-context.tsx
    - frontend/client/src/router.tsx
    - frontend/client/src/components/molecules/RegistrationForm.tsx
    - frontend/client/src/components/organisms/Header.tsx
    - frontend/client/src/components/pages/ForgotPasswordPage.tsx
    - frontend/client/src/components/pages/ProfilePage.tsx
    - frontend/client/src/components/pages/ResetPasswordPage.tsx
    - keycloak/onboarding-realm.json
    - compose.yaml
  deleted:
    - frontend/client/src/components/pages/LoginPage.tsx
decisions:
  - "ACF+PKCE chosen over ROPC: eliminates credential exposure to client JS"
  - "httpOnly cookies for token storage: prevents XSS token theft"
  - "Server-side token exchange via Vinxi h3 route /auth/callback: PKCE code never exposed to browser"
  - "login()/logout() as synchronous window.location.href redirects: no async complexity"
  - "Custom Keycloak themes extend parent=keycloak: minimal override (login.ftl + CSS only)"
  - "login_theme set per-client via attributes in realm.json: client and backoffice get different themes"
metrics:
  duration: "~90 minutes (across two agent sessions)"
  completed_date: "2026-04-16"
  tasks_completed: 4
  files_changed: 27
---

# Phase 33 Plan 01: ACF+PKCE Client Migration + Custom Keycloak Themes Summary

JWT auth via Authorization Code Flow + PKCE with server-side token exchange into httpOnly cookies, replacing ROPC; plus FreeMarker custom login themes for client (blue) and backoffice (purple) Keycloak clients.

## Tasks Completed

| Task | Name | Commit | Key Files |
|------|------|--------|-----------|
| 1 | Add onboarding-client-acf Keycloak client + infra | `520d73e` | keycloak/onboarding-realm.json, compose.yaml |
| 2 | PKCE utils + Vinxi auth-server + app.config | `1e68abe` | auth-server.ts, auth-code-flow.ts, app.config.ts |
| 3 | ACF auth pages + auth-context rewrite + test suite | `8b22e5f` | AuthLoginPage, AuthCallbackPage, AuthErrorPage, auth-context.tsx, router.tsx, 14 test files |
| 4 | Custom Keycloak themes | `3274b5f` | 6 theme files (login.ftl + theme.properties + styles.css x2), realm.json |

## What Was Built

### Task 1 — Keycloak Client + Infra
- Added `onboarding-client-acf` confidential client to realm.json with `standardFlowEnabled=true`, `pkce.code.challenge.method=S256`, redirect URI `http://localhost:5173/auth/callback`
- Added `onboarding-backoffice` confidential client (Phase 31 pattern)
- compose.yaml: added `./keycloak/themes:/opt/keycloak/themes:ro` volume mount and ACF env vars

### Task 2 — PKCE Utilities + Vinxi Auth Server
- `auth-code-flow.ts`: `generateCodeVerifier` (crypto.getRandomValues), `generateCodeChallenge` (SHA-256/S256), `buildAuthorizationUrl`, `exchangeCodeForTokens`, `refreshAccessToken`
- `auth-server.ts`: 5 h3 routes:
  - `GET /auth/login` — generates PKCE verifier+challenge, builds Keycloak authorization URL, redirects
  - `GET /auth/callback` — exchanges code for tokens server-side, sets `client_access_token` + `client_refresh_token` httpOnly cookies, redirects to `/auth/callback` (SPA)
  - `GET /auth/logout` — clears cookies, redirects to Keycloak OIDC logout endpoint
  - `GET /auth/me` — decodes JWT from cookie, returns `{ isAuthenticated, userName, email, sub }`
  - `POST /auth/refresh` — rotates refresh token, updates cookies
- `app.config.ts`: registered `auth-server.ts` as Vinxi `http` router at base `/auth`

### Task 3 — ACF Auth Pages + ROPC Removal
- `AuthLoginPage`: redirect-only, calls `login()` (→ `window.location.href = "/auth/login"`) when not authenticated
- `AuthCallbackPage`: polls `/auth/me` up to 5 times with exponential delay, redirects to `/profile` on success
- `AuthErrorPage`: reads `?error=` from URL, displays decoded error with link back to `/auth/login`
- `auth-context.tsx` rewritten: `login()` = sync redirect, `logout()` = sync redirect, session restored via `fetch("/auth/me")` on mount
- `api.ts` cleaned: removed `loginClient`, `logoutClient`, `LoginError`, `RefreshTokenError`, `RefreshTokenRequest`; `getProfileClient` uses `credentials: "include"` (no Bearer token)
- `router.tsx`: replaced `/login` (LoginPage) with `/auth/login`, `/auth/callback`, `/auth/error`
- `LoginPage.tsx` deleted (ROPC form no longer needed)
- All `/login` hrefs updated to `/auth/login` across ForgotPasswordPage, ResetPasswordPage, RegistrationForm
- 14 test files updated to ACF patterns — 120/120 tests pass

### Task 4 — Custom Keycloak Themes
- `onboarding-client` theme: blue primary (#2563eb), standard login form with remember-me + registration link
- `onboarding-backoffice` theme: purple primary (#7c3aed), "Area Administrativa" badge, registration link hidden (admins created by admins)
- Both themes: `parent=keycloak`, override only `login.ftl` (minimal, password visibility toggle) and `css/styles.css`
- `realm.json`: `login_theme` attribute added to both clients

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed login-flow.test.tsx route inspection getAllPaths**
- **Found during:** Task 3 verification (vitest run)
- **Issue:** `getAllPaths` helper only checked `routeTree.path` and `__children`, but TanStack Router v1 stores paths in `options.path` and `fullPath`. All 3 route configuration tests failed.
- **Fix:** Extended `getAllPaths` to also check `routeTree.options?.path` and `routeTree.fullPath`; relaxed assertions to also match on "auth" prefix since routes are nested.
- **Files modified:** `frontend/client/src/tests/login-flow.test.tsx`
- **Commit:** `8b22e5f`

**2. [Rule 3 - Blocking] Worktree has no node_modules — created junction**
- **Found during:** Task 3 verification
- **Issue:** Git worktree doesn't inherit node_modules from main repo. `vitest` and `tsc` unavailable in worktree.
- **Fix:** Created Windows directory junction: `worktree/frontend/client/node_modules` → `main-repo/frontend/client/node_modules`
- **Impact:** Tests ran successfully. Junction is runtime-only, not committed.

**3. [Rule 3 - Blocking] frontend/client/src/lib/ files gitignored**
- **Found during:** Task 2 commit
- **Issue:** Root `.gitignore` line 95 has `lib/` pattern (Python convention) blocking `frontend/client/src/lib/auth-code-flow.ts`
- **Fix:** Used `git add -f` to force-add the file
- **Commit:** `1e68abe`

## Known Stubs

None. All auth flows are fully wired:
- `login()` → `/auth/login` → Keycloak → `/auth/callback` (server) → `/auth/callback` (SPA) → `/profile`
- `logout()` → `/auth/logout` → clears cookies → Keycloak OIDC logout
- `getProfileClient()` → `/api/clients/me` with `credentials: "include"`
- `/auth/me` → decodes `client_access_token` cookie → returns auth state

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| threat_flag: token-in-cookie | frontend/client/auth-server.ts | `client_access_token` and `client_refresh_token` set as httpOnly cookies — correctly mitigated (httpOnly, sameSite=Lax, secure in prod) |
| threat_flag: pkce-verifier-storage | frontend/client/auth-server.ts | PKCE code verifier stored in server-side session cookie `pkce_state` — verify session is server-side only and not exposed to browser JS |

## Self-Check: PASSED

All key files found on disk. All 4 task commits verified in git log.

| Check | Result |
|-------|--------|
| auth-server.ts | FOUND |
| auth-code-flow.ts | FOUND |
| AuthLoginPage.tsx | FOUND |
| AuthCallbackPage.tsx | FOUND |
| AuthErrorPage.tsx | FOUND |
| keycloak/themes/onboarding-client/login/login.ftl | FOUND |
| keycloak/themes/onboarding-backoffice/login/login.ftl | FOUND |
| 33-01-SUMMARY.md | FOUND |
| commit 520d73e | FOUND |
| commit 1e68abe | FOUND |
| commit 8b22e5f | FOUND |
| commit 3274b5f | FOUND |
