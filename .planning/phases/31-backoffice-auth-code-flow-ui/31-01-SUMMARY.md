---
phase: 31-backoffice-auth-code-flow-ui
plan: "01"
subsystem: backoffice-auth
status: completed
tags: [auth, keycloak, pkce, acf, frontend, vinxi, ropc-removal]
dependency_graph:
  requires: [keycloak/onboarding-realm.json, frontend/backoffice]
  provides: [ACF+PKCE login flow for backoffice, /auth/* Vinxi server routes, httpOnly cookie token storage]
  affects: [frontend/backoffice/src/lib, frontend/backoffice/src/components/pages, frontend/backoffice/src/router.tsx, frontend/backoffice/src/tests]
tech_stack:
  added: [auth-code-flow.ts (PKCE helpers), auth-server.ts (Vinxi h3 server routes)]
  patterns: [Authorization Code Flow + PKCE, httpOnly cookie token storage, Vinxi server-side auth routes, redirect-only login]
key_files:
  created:
    - frontend/backoffice/auth-server.ts
    - frontend/backoffice/src/lib/auth-code-flow.ts
    - frontend/backoffice/src/components/pages/AuthLoginPage.tsx
    - frontend/backoffice/src/components/pages/AuthCallbackPage.tsx
    - frontend/backoffice/src/components/pages/AuthErrorPage.tsx
  modified:
    - keycloak/onboarding-realm.json
    - frontend/backoffice/app.config.ts
    - compose.yaml
    - .env
    - frontend/backoffice/src/lib/admin-auth-context.tsx
    - frontend/backoffice/src/lib/admin-api.ts
    - frontend/backoffice/src/lib/admin-http-interceptor.ts
    - frontend/backoffice/src/lib/admin-error-handler.ts
    - frontend/backoffice/src/components/templates/AdminLayout.tsx
    - frontend/backoffice/src/router.tsx
    - frontend/backoffice/src/tests/admin-api.test.ts
    - frontend/backoffice/src/tests/admin-auth-context.test.tsx
    - frontend/backoffice/src/tests/admin-delete-flow.test.tsx
    - frontend/backoffice/src/tests/admin-error-handler.test.ts
    - frontend/backoffice/src/tests/admin-layout.test.tsx
  deleted:
    - frontend/backoffice/src/components/pages/AdminLoginPage.tsx (ROPC)
    - frontend/backoffice/src/tests/admin-login-flow.test.tsx (ROPC)
decisions:
  - "Vinxi h3 auth-server handles all PKCE/token logic server-side — client JS never sees tokens"
  - "login() and logout() are synchronous redirects (window.location.href) — no async API calls"
  - "getAdminMe points to /auth/me (Vinxi server) instead of /api/admin/auth/me"
  - "401 handler redirects to /auth/login (not /admin/login)"
  - "AdminLayout logout delegates to context — no explicit redirect after logout call"
metrics:
  duration: "~20 min"
  completed_date: "2026-04-16"
  tasks_completed: 3
  files_changed: 22
requirements:
  - ACF-01
  - ACF-02
  - ACF-03
  - ACF-04
---

# Phase 31 Plan 01: Migrate Backoffice to Authorization Code Flow + PKCE — Summary

**One-liner:** Backoffice migrated from ROPC to Auth Code Flow + PKCE using Vinxi h3 server routes handling all token exchange and httpOnly cookie management.

## What Was Done

**Task 1 — Keycloak realm:** Added `onboarding-backoffice` confidential client to `onboarding-realm.json` with ACF enabled, PKCE S256, redirect URI `http://localhost:5174/auth/callback`, offline_access scope. Added env vars to .env and compose.yaml.

**Task 2 — Auth infrastructure:** Created `src/lib/auth-code-flow.ts` with pure PKCE helpers (`generateCodeVerifier`, `generateCodeChallenge`, `buildAuthorizationUrl`, `exchangeCodeForTokens`, `refreshAccessToken`). Created `auth-server.ts` as a Vinxi h3 event handler covering `/login`, `/callback`, `/logout`, `/me`, and `/refresh` routes — all token handling is server-side. Added `auth` router to `app.config.ts` with base `/auth`.

**Task 3 — Frontend:** 
- Created `AuthLoginPage` (redirect-only), `AuthCallbackPage` (polls /auth/me), `AuthErrorPage`
- Rewrote `admin-auth-context.tsx` — `login()` and `logout()` are now synchronous redirects to `/auth/login` and `/auth/logout`
- Updated `admin-api.ts` to remove `loginAdmin`, `logoutAdmin`, `AdminLoginError` and point `getAdminMe` to `/auth/me`
- Updated `admin-http-interceptor.ts` to redirect 401s directly to `/auth/login`
- Updated `admin-error-handler.ts` redirect URL to `/auth/login`
- Updated `AdminLayout.tsx` — logout delegates to context redirect
- Rewrote `router.tsx` with `/auth/*` routes, removed `/admin/login`
- Updated all affected tests (149 passing)
- Deleted obsolete `AdminLoginPage.tsx` and `admin-login-flow.test.tsx`

## Deviations from Plan

None. All 3 tasks executed exactly as planned.

## Commits

| Hash | Message |
|------|---------|
| 14485a6 | feat(phase-31): migrate backoffice from ROPC to Auth Code Flow + PKCE |

## Verification Results

- `npx tsc --noEmit`: PASSED (0 errors)
- `npx vitest run`: PASSED (149/149 tests, 17 test files)

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
- `grep "loginAdmin" frontend/backoffice/src/lib/admin-api.ts`: zero results
- `grep "logoutAdmin" frontend/backoffice/src/lib/admin-api.ts`: zero results
- `grep "AdminLoginError" frontend/backoffice/src/lib/admin-api.ts`: zero results
- `grep "window.location.href.*auth/login" frontend/backoffice/src/lib/admin-auth-context.tsx`: match
- `grep "window.location.href.*auth/logout" frontend/backoffice/src/lib/admin-auth-context.tsx`: match
- Commit 14485a6: EXISTS
