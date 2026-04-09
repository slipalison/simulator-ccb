---
phase: 15-production-cleanup
plan: 01
type: summary
completed: "2026-04-09"
author: AI Assistant
---

# Phase 15-01: Production Cleanup Summary

## What Was Cleaned Up

### 1. Cookie Secure Flag — Environment-Configured via IOptions Pattern

**Before:** `Secure = false` was hardcoded in 5 locations in `AuthController.cs` (3 `CookieOptions.Append`, 2 `CookieOptions.Delete`).

**After:** Created `CookieSettings` class with `Secure` boolean property, registered via `IOptions<CookieSettings>` in Program.cs. All 5 locations now use `_cookieSecure` field injected from configuration.

**Files modified:**
- `src/Onboarding.API/Configuration/CookieSettings.cs` (new)
- `src/Onboarding.API/Program.cs` — added `using Onboarding.API.Configuration` and `builder.Services.Configure<CookieSettings>(...)`
- `src/Onboarding.API/Controllers/AuthController.cs` — injected `IOptions<CookieSettings>`, replaced all 5 hardcoded `Secure = false` with `Secure = _cookieSecure`
- `src/Onboarding.API/appsettings.json` — added `"CookieSettings": { "Secure": true }`
- `src/Onboarding.API/appsettings.Development.json` — added `"CookieSettings": { "Secure": false }`
- `src/Onboarding.API/appsettings.Production.json` (new) — `"CookieSettings": { "Secure": true }`

### 2. Dead Code Files Deleted

Two orphan files removed after confirming zero imports across the frontend codebase:
- `frontend/src/client.tsx` — Phase 1 infrastructure placeholder (13 lines, rendered "Onboarding — placeholder")
- `frontend/src/components/molecules/LabeledField.tsx` — Molecule component replaced by shadcn/ui patterns in Phase 12

### 3. Stale TDD Comments Removed

Replaced `/// GREEN tests for AUTH-XX` comments with descriptive XML doc summaries in 5 test files:
- `JwtBearerConfigurationTests.cs` — "Tests for JWT Bearer authentication configuration"
- `AuthorizationMiddlewareTests.cs` — "Tests for [Authorize] middleware behavior"
- `RefreshTokenEndpointTests.cs` — "Tests for POST /api/auth/refresh behavior (AUTH-04)"
- `LoginEndpointTests.cs` — "Tests for POST /api/auth/login behavior (AUTH-02)"
- `ClientsMeEndpointTests.cs` — "Tests for GET /api/clients/me behavior with JWT authentication (AUTH-03)"

Grep verification: `// TDD|// RED|// GREEN|// TODO.*TDD` returns 0 matches.

## Root Cause of 3 Failing Tests

### Problem
All 3 failures stemmed from the same root cause: **the tests expected refresh tokens in the JSON request/response body, but the actual implementation uses httpOnly cookies**.

### Failing Test 1: `Refresh_WithValidRefreshToken_Returns200WithNewTokens`
- **What it did:** Sent `{ refreshToken: "valid-refresh-token" }` in request body
- **What controller does:** Reads refresh token from `Request.Cookies["refreshToken"]`
- **Result:** Controller returned 401 (no cookie found) instead of 200
- **Fix:** Send refresh token via `Cookie` header instead of JSON body

### Failing Test 2: `Refresh_WithMissingRefreshToken_Returns422`
- **What it did:** Sent `{ refreshToken: null }` in body, expected 422
- **What controller does:** Returns 401 when cookie is absent (not 422 — validation never runs if cookie is missing)
- **Fix:** Renamed test to `Refresh_WithMissingRefreshToken_Returns401`, removed body payload, assert 401

### Failing Test 3: `Login_WithValidCredentials_Returns200WithTokens`
- **What it did:** Asserted `body.ContainsKey("refreshToken")` should be true
- **What controller does:** Returns only access token in JSON body; refresh token is set via `Set-Cookie` header
- **Fix:** Changed assertion to `body.ContainsKey("refreshToken").ShouldBeFalse()` with comment explaining cookie placement

## Final Test Suite Results

| Metric | Value |
|--------|-------|
| Total tests | 55 |
| Passed | 53 |
| Failed | 0 |
| Skipped | 2 (intentional — E2E trace correlation tests verified manually via Grafana) |
| Pass rate | 100% of executed tests |

## Commits Created

1. `feat(15-01): environment-configured Cookie Secure flag via IOptions pattern`
2. `chore(15-01): delete dead code files (client.tsx, LabeledField.tsx)`
3. `chore(15-01): remove stale TDD comments from test files`
4. `fix(15-01): fix 3 failing tests — refresh token in cookie not body`

## Remaining Concerns / Tech Debt

- **2 skipped tests** (`TracePropagationTests`) verify E2E trace correlation manually via Grafana Loki + Tempo. These should be re-enabled when automated trace assertion is feasible.
- **ROPC grant deprecation:** The login flow uses OAuth 2.0 ROPC grant which is deprecated in OAuth 2.1. Documented in STATE.md for future migration to Authorization Code + PKCE.
- **Cookie Secure in tests:** The test factory does not configure `CookieSettings`, so tests use the default value from `CookieSettings.Secure = true`. Since tests run in Development environment, `appsettings.Development.json` overrides to `false`. This is correct behavior — no change needed.
