---
phase: quick-260417-eu6
plan: 01
subsystem: backoffice-auth
tags: [backoffice, keycloak, acf, pkce, scope, fix]
requires: []
provides:
  - "backoffice ACF login works without offline_access scope error"
affects:
  - frontend/backoffice/src/lib/auth-code-flow.ts
tech-stack:
  added: []
  patterns:
    - "OIDC scope: openid + profile + email (no offline_access)"
key-files:
  created: []
  modified:
    - frontend/backoffice/src/lib/auth-code-flow.ts
decisions:
  - "Backoffice does not need offline tokens — session expires when admin logs out; regular refresh token covers active session"
  - "Force-added auth-code-flow.ts because src/lib/ matched the global .gitignore pattern lib/ (Python build dir); other sibling files under src/lib/ were already tracked under the same exception"
metrics:
  duration: "~5 min"
  completed: "2026-04-17"
  tasks_completed: 1
  tasks_total: 2
  checkpoint_pending: true
requirements:
  - QUICK-260417-EU6
---

# Quick Task 260417-eu6: Fix Backoffice ACF Token Exchange — Remove offline_access Summary

**One-liner:** Backoffice authorization URL now requests `scope=openid profile email` instead of `openid offline_access`, unblocking ACF login that was failing with `Offline tokens not allowed for the user or client`.

## What Changed

Single-line change in `frontend/backoffice/src/lib/auth-code-flow.ts`, inside `buildAuthorizationUrl`:

```diff
-    scope: "openid offline_access",
+    scope: "openid profile email",
```

**Scopes:**

| Before               | After                   |
| -------------------- | ----------------------- |
| `openid`             | `openid`                |
| `offline_access` ❌  | `profile`, `email` ✅  |

**Rationale:**

- `openid` — mandatory for OIDC (ID token issuance).
- `profile` + `email` — standard OIDC claims required by `/auth/me` handler in `auth-server.ts` (lines 178-188) to populate `name`, `preferred_username`, `email`.
- `offline_access` removed — client `onboarding-backoffice` and/or admin user lacks the `offline_access` role in Keycloak, which was causing the 400 `not_allowed` error on `/auth/callback`. Backoffice does not need offline tokens; sessions should end when the admin logs out.

## Root Cause

User's browser received:

```
/auth/error?error=Token%20exchange%20failed%3A%20400%20%7B%22error%22%3A%22not_allowed%22%2C%22error_description%22%3A%22Offline%20tokens%20not%20allowed%20for%20the%20user%20or%20client%22%7D
```

The authorization URL was requesting `scope=openid offline_access`, and when Keycloak tried to issue an offline refresh token at the `/token` endpoint, it denied the request because the client/user lacks the `offline_access` role mapping.

## Verification Performed

- [x] Line 37 of `frontend/backoffice/src/lib/auth-code-flow.ts` now contains `scope: "openid profile email",`
- [x] `grep -rn 'offline_access' frontend/backoffice/src` returns no results
- [x] `npx tsc --noEmit` in backoffice — no NEW TypeScript errors introduced by this change (pre-existing test-file errors about removed `loginAdmin` are unrelated, see Deferred Issues)
- [x] Commit created: `54b3995`
- [ ] **Manual smoke test PENDING** — requires user to perform Task 2 end-to-end verification (see Checkpoint below)

## Commit

- `54b3995 fix(quick-260417-eu6): remove offline_access from backoffice ACF scope`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocker] Force-added auth-code-flow.ts to git tracking**

- **Found during:** Task 1 commit
- **Issue:** `frontend/backoffice/src/lib/auth-code-flow.ts` was not tracked by git on `master@57012b5` because the project's top-level `.gitignore` has a generic Python pattern `lib/` (line 95) that also ignores `frontend/backoffice/src/lib/`. The file existed on disk and was imported by `auth-server.ts` (tracked), but attempting `git add` did nothing and `git diff` showed empty after the edit. The plan assumed the file was already tracked.
- **Fix:** Used `git add -f frontend/backoffice/src/lib/auth-code-flow.ts` to bypass the gitignore. Sibling files under `src/lib/` (admin-api.ts, utils.ts, admin-auth-context.tsx, etc.) were already tracked via the same mechanism — this is consistent with existing repo practice.
- **Side effect:** This commit shows `1 file changed, 122 insertions(+)` rather than the expected 1-line diff, because the file is brand new to git's tracked state on this branch. The actual semantic change is still just the scope line. A follow-up task could narrow the `lib/` gitignore rule (e.g., to `/lib/` to only match the project root) but that is out of scope here.
- **Files modified:** `frontend/backoffice/src/lib/auth-code-flow.ts`
- **Commit:** `54b3995`

## Deferred Issues

**Pre-existing TypeScript errors in backoffice tests (OUT OF SCOPE):**
`npx tsc --noEmit` reports 7 errors in `src/tests/admin-api.test.ts`, `admin-auth-context.test.tsx`, and `admin-login-flow.test.tsx` that reference the removed ROPC function `loginAdmin` and the removed `login` method on `AdminAuthValue`. These errors existed before this task and were introduced by commit `82a3d4e fix(backoffice): wire AdminLoginPage to ACF redirect flow via /auth/login` which removed `loginAdmin` from `admin-api.ts` and `login` from the auth context but did not update/remove the corresponding tests. Logged to this Deferred Issues section per executor SCOPE BOUNDARY rule; not fixed in this quick task.

Suggested follow-up: update or delete the legacy ROPC tests to align with the ACF flow.

**Pre-existing overbroad `.gitignore` rule (OUT OF SCOPE):**
`.gitignore` line 95 has `lib/` (Python build output pattern) which also matches `frontend/backoffice/src/lib/`. Source files in that directory must be force-added. Narrowing the rule to `/lib/` or adding an explicit `!frontend/backoffice/src/lib/` unignore would be cleaner, but that's a separate cleanup task.

## Checkpoint (Task 2 — Human Verify)

**Type:** human-verify
**Status:** Awaiting user verification

### How to Verify

1. Ensure the Docker Compose stack is up (Keycloak on http://localhost:8180, Postgres, .NET backend, frontends).
2. Start the backoffice Vinxi app (expected at http://localhost:5174) if not already running.
3. Open the browser at http://localhost:5174/auth/login.
4. Expected: redirect to Keycloak login at `http://localhost:8180/realms/onboarding/protocol/openid-connect/auth?...`.
5. Inspect the redirect URL — the `scope` query param must be `openid profile email` (URL-encoded as `openid+profile+email` or `openid%20profile%20email`). `offline_access` must NOT appear.
6. Log in with valid admin credentials from the `onboarding` realm.
7. Expected: redirect back to `http://localhost:5174/auth/callback?code=...&state=...`, then auto-redirect to `http://localhost:5174/admin/users`.
8. Expected: NO redirect to `/auth/error?error=Token%20exchange%20failed...Offline%20tokens%20not%20allowed...`.
9. DevTools → Application → Cookies should show httpOnly `backoffice_access_token` and `backoffice_refresh_token`.
10. (Optional) `curl http://localhost:5174/auth/me` (with cookies) should return `{ isAuthenticated: true, adminName, email, sub }`.

### Resume Signal

Reply `approved` if login reaches `/admin/users` without error, or describe the observed error (full URL + message) for investigation.

## Known Stubs

None.

## Threat Flags

None — this change reduces the token scope surface (no offline tokens), which is a security tightening, not a new surface.

## Self-Check: PASSED

- FOUND: `frontend/backoffice/src/lib/auth-code-flow.ts` (on disk with new scope)
- FOUND: commit `54b3995` in `git log --oneline -3`
- FOUND: grep of `scope:` in target file returns only `scope: "openid profile email",`
- FOUND: no matches for `offline_access` under `frontend/backoffice/src`
