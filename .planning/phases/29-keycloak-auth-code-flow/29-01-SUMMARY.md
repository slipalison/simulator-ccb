---
phase: 29-keycloak-auth-code-flow
plan: "01"
subsystem: backoffice-auth
status: completed
tags: [auth, keycloak, pkce, acf, frontend, backend]
dependency_graph:
  requires: [keycloak/onboarding-realm.json, frontend/backoffice, src/Onboarding.API/Middleware]
  provides: [ACF+PKCE login flow for backoffice, /auth/* Vinxi server routes, backoffice_refresh_token cookie]
  affects: [frontend/backoffice/src/lib, frontend/backoffice/src/components/pages, frontend/backoffice/src/router.tsx]
tech_stack:
  added: [auth-code-flow.ts (PKCE helpers), Vinxi h3 auth-server.ts]
  patterns: [Authorization Code Flow + PKCE, httpOnly cookie token storage, Vinxi server-side auth routes]
key_files:
  created:
    - frontend/backoffice/src/lib/auth-code-flow.ts
    - frontend/backoffice/auth-server.ts
    - frontend/backoffice/src/components/pages/AuthLoginPage.tsx
    - frontend/backoffice/src/components/pages/AuthCallbackPage.tsx
    - frontend/backoffice/src/components/pages/AuthErrorPage.tsx
  modified:
    - keycloak/onboarding-realm.json
    - frontend/backoffice/app.config.ts
    - compose.yaml
    - frontend/backoffice/src/lib/admin-auth-context.tsx
    - frontend/backoffice/src/lib/admin-api.ts
    - frontend/backoffice/src/lib/admin-error-handler.ts
    - frontend/backoffice/src/lib/admin-http-interceptor.ts
    - frontend/backoffice/src/components/pages/AdminLoginPage.tsx
    - frontend/backoffice/src/components/templates/AdminLayout.tsx
    - frontend/backoffice/src/router.tsx
    - src/Onboarding.API/Middleware/AdminSessionMiddleware.cs
    - src/tests/admin-api.test.ts
    - src/tests/admin-auth-context.test.tsx
    - src/tests/admin-error-handler.test.ts
    - src/tests/admin-layout.test.tsx
    - src/tests/admin-login-flow.test.tsx
decisions:
  - Vinxi h3 auth-server handles all PKCE/token logic server-side — client JS never sees tokens
  - backoffice_refresh_token cookie path changed from /api/admin to / to be readable by both Vinxi and .NET
  - AdminSessionMiddleware cookie name updated from adminRefreshToken to backoffice_refresh_token
  - AdminLayout logout() delegates to context (window.location.href = /auth/logout) — no direct API call
  - AdminLoginPage simplified to redirect-only — ROPC form removed entirely
metrics:
  duration: ~15 minutes
  completed_date: "2026-04-15"
  tasks_completed: 4
  files_modified: 21
---

# Phase 29 Plan 01: Migrate Backoffice to Authorization Code Flow + PKCE — Summary

**One-liner:** Backoffice auth migrated from ROPC to Authorization Code Flow + PKCE using Vinxi h3 server routes handling all token exchange and httpOnly cookie management.

## What Was Done

Replaced the backoffice ROPC (Resource Owner Password Credentials) authentication with Authorization Code Flow + PKCE across 4 tasks:

**Task 1 — Keycloak realm:** Added `onboarding-backoffice` confidential client to `onboarding-realm.json` with ACF enabled, PKCE redirect URI `http://localhost:5174/auth/callback`, realm-roles protocol mapper, and `offline_access` optional scope.

**Task 2 — Auth infrastructure:** Created `src/lib/auth-code-flow.ts` with pure PKCE helpers (`generateCodeVerifier`, `generateCodeChallenge`, `buildAuthorizationUrl`, `exchangeCodeForTokens`, `refreshAccessToken`, `decodeJwtPayload`). Created `auth-server.ts` as a Vinxi h3 event handler covering `/login`, `/callback`, `/logout`, `/me`, and `/refresh` routes — all token handling is server-side. Added `auth` router to `app.config.ts` with base `/auth`. Added all required env vars and a volume mount for `auth-server.ts` to `compose.yaml`.

**Task 3 — Frontend:** Rewrote `admin-auth-context.tsx` — `login()` and `logout()` are now synchronous redirects to `/auth/login` and `/auth/logout`. Updated `admin-api.ts` to remove `loginAdmin`, `logoutAdmin`, `AdminLoginError` and point `getAdminMe` to `/auth/me`. Updated `admin-http-interceptor.ts` to redirect 401s directly to `/auth/login`. Updated `admin-error-handler.ts` redirect URL. Created `AuthLoginPage`, `AuthCallbackPage`, `AuthErrorPage` components. Rewrote `router.tsx` with auth routes and `ProtectedRoute` guard wrapping all `/admin/*` routes. Updated all affected tests.

**Task 4 — Backend:** Updated `AdminSessionMiddleware.cs` cookie name from `adminRefreshToken` to `backoffice_refresh_token` and cookie path from `/api/admin` to `/`.

## Deviations from Plan

**1. [Rule 1 - Bug] AdminLayout.tsx still redirected to `/admin/login` after logout**
- Found during: Task 3 verification (vitest run)
- Issue: `handleLogout` in AdminLayout called `logout()` then explicitly redirected to `/admin/login`, bypassing the context's ACF logout redirect
- Fix: Simplified `handleLogout` to call `logout()` only — the context now owns the redirect to `/auth/logout`
- Files modified: `frontend/backoffice/src/components/templates/AdminLayout.tsx`
- Commit: f5eb5d2

**2. [Rule 1 - Bug] `admin-error-handler.test.ts` expected old redirect URL**
- Found during: Task 3 verification (vitest run)
- Issue: Test asserted `window.location.href === "/admin/login?expired=true"` but source was updated to `/auth/login`
- Fix: Updated test expectation to match new URL
- Files modified: `frontend/backoffice/src/tests/admin-error-handler.test.ts`
- Commit: f5eb5d2

**3. [Rule 2 - Missing] `AdminLoginPage.tsx` still used ROPC `login(email, password)` signature**
- Found during: TypeScript check after Task 3
- Issue: `login` context function signature changed from `(email, password) => Promise<void>` to `() => void`, but `AdminLoginPage` still called it with args and imported `AdminLoginError`
- Fix: Rewrote `AdminLoginPage` as a redirect-only component (consistent with new ACF flow)
- Files modified: `frontend/backoffice/src/components/pages/AdminLoginPage.tsx`
- Commit: f5eb5d2

**4. [Rule 2 - Missing] `admin-api.test.ts` tested removed ROPC functions**
- Found during: TypeScript check after Task 3
- Issue: Test imported `loginAdmin`, `logoutAdmin`, `AdminLoginError` — all removed
- Fix: Rewrote test to cover only `getAdminMe` pointing to `/auth/me`
- Files modified: `frontend/backoffice/src/tests/admin-api.test.ts`
- Commit: f5eb5d2

**5. [Rule 2 - Missing] `admin-layout.test.tsx` mocked removed `logoutAdmin` function**
- Found during: TypeScript check after Task 3
- Issue: Test mock included `logoutAdmin` and `AdminLoginError` which no longer exist; test assertion expected `/admin/login`
- Fix: Updated mock to remove deleted exports; updated assertion to expect `/auth/logout`
- Files modified: `frontend/backoffice/src/tests/admin-layout.test.tsx`
- Commit: f5eb5d2

## Commits

| Hash | Message |
|------|---------|
| 2752dec | feat(phase-29): add onboarding-backoffice Keycloak client to realm JSON |
| 1894472 | feat(phase-29): add auth-code-flow lib, auth-server, update app.config and compose |
| f5eb5d2 | feat(phase-29): replace ROPC with Auth Code Flow in backoffice frontend + backend |

## Verification Results

- `npx tsc --noEmit`: PASSED (0 errors)
- `npx vitest run`: PASSED (149/149 tests, 18 test files)
- `dotnet build src/Onboarding.API/`: PASSED (0 errors, 1 pre-existing warning)

## Known Stubs

None — all auth routes are fully implemented server-side.

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| threat_flag: server-side-secret | frontend/backoffice/auth-server.ts | CLIENT_SECRET read from env var — must never be exposed to client bundle. Correct: runs in Vinxi server context only. |
| threat_flag: cookie-secure-false | frontend/backoffice/auth-server.ts | `secure: false` is intentional for local dev (HTTP). Must be `true` in production. |

## Self-Check: PASSED

- `frontend/backoffice/src/lib/auth-code-flow.ts`: FOUND
- `frontend/backoffice/auth-server.ts`: FOUND
- `frontend/backoffice/src/components/pages/AuthLoginPage.tsx`: FOUND
- `frontend/backoffice/src/components/pages/AuthCallbackPage.tsx`: FOUND
- `frontend/backoffice/src/components/pages/AuthErrorPage.tsx`: FOUND
- Commits 2752dec, 1894472, f5eb5d2: FOUND
