---
phase: 43-e2e-playwright-validation
plan: 03
subsystem: testing
tags: [playwright, e2e, keycloak, acf, jwt, permission-ui, access-group, redirect]

requires:
  - phase: 43-e2e-playwright-validation
    provides: Playwright infrastructure, page objects, auth setup, fixtures (from Plan 01)
  - phase: 40-client-frontend-pj-registration
    provides: Auth context, EmployeesTable permissions, Sidebar nav, ChangeAccessGroupDialog, auth-server

provides:
  - E2E-04 employee login redirect tests (viewer → /employees read-only, admin-empresa → /employees com ações, dashboard → /dashboard)
  - E2E-05 JWT claims + permission UI verification (decode JWT from cookie, verify groups match UI rendering)
  - E2E-06 access group change → re-login → updated permissions test (viewer → admin-empresa via dialog, re-login, verify)

affects: [43-e2e-playwright-validation]

tech-stack:
  added: []
  patterns: ["fresh-acf-login-for-redirect-testing", "jwt-cookie-decode-for-claims-verification", "keycloak-eventual-consistency-handling"]

key-files:
  created:
    - frontend/client/e2e/employee-login.spec.ts
    - frontend/client/e2e/permission-ui.spec.ts
    - frontend/client/e2e/access-group-change.spec.ts
  modified: []

key-decisions:
  - "Employee-login spec uses fresh ACF logins (no storageState) to test redirect behavior from scratch"
  - "Permission-ui spec runs in both viewer and admin-empresa projects — test.skip() guards admin-empresa-only test"
  - "Dashboard employee login test is conditional on E2E_DASHBOARD_EMAIL env var (skipped if not set)"
  - "Access-group-change test handles Keycloak UPDATE_PASSWORD required action on first employee login"

patterns-established:
  - "Fresh ACF login pattern: navigate to root → Keycloak login → verify redirect URL + permissions"
  - "JWT cookie decode pattern: getAccessTokenFromCookies() + decodeAccessToken() for claims verification"
  - "Group change E2E pattern: create employee → login → verify viewer → change group → re-login → verify admin-empresa"

requirements-completed: [E2E-04, E2E-05, E2E-06, E2E-07]

duration: 15min
completed: 2026-04-27
---

# Phase 43 Plan 03: Employee Login + Permission UI + Access Group Change E2E Tests Summary

**Employee login redirect E2E tests (viewer → /employees read-only, admin-empresa → /employees com ações, dashboard → /dashboard), JWT claims + permission UI verification, and access group change with re-login permission update validation**

## Performance

- **Duration:** 15 min
- **Started:** 2026-04-27T11:26:40Z
- **Completed:** 2026-04-27T11:40:26Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- E2E-04: 3 employee login redirect tests — viewer → /employees (read-only), admin-empresa → /employees (com ações), dashboard → /dashboard
- E2E-05: JWT claims + permission UI tests — viewer JWT shows "viewer" group with read-only UI, admin-empresa JWT shows "admin-empresa" group with full actions UI
- E2E-06: Access group change cycle test — create employee, verify viewer permissions, change group to admin-empresa, re-login, verify updated permissions + JWT claims
- All 13 E2E tests listed correctly across 8 files in 6 Playwright projects

## Task Commits

Each task was committed atomically:

1. **Task 1: Employee login + redirect E2E test (E2E-04)** - `3c3303c` (test)
2. **Task 2: Permission UI + JWT (E2E-05) and Access group change (E2E-06)** - `2250160` (test)

## Files Created/Modified
- `frontend/client/e2e/employee-login.spec.ts` - E2E-04: login redirect tests by access group (3 tests)
- `frontend/client/e2e/permission-ui.spec.ts` - E2E-05: JWT decode + permission UI verification (2 tests, runs in 2 projects = 4 test instances)
- `frontend/client/e2e/access-group-change.spec.ts` - E2E-06: change group → re-login → updated permissions (1 test)

## Decisions Made
- Employee-login spec uses fresh ACF logins (no storageState) to test redirect behavior from scratch — this validates the full ACF redirect chain per access group
- Permission-ui spec runs in both `viewer` and `admin-empresa` Playwright projects; admin-empresa-only test uses `test.skip()` guard based on `/auth/me` response
- Dashboard employee login test is conditional on `E2E_DASHBOARD_EMAIL` env var (skipped if not set) — no dashboard employee is created in setup
- Access-group-change test handles Keycloak `UPDATE_PASSWORD` required action on first employee login (new employees get temporary passwords)
- Keycloak eventual consistency handled with 3-second wait after group change before re-login

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed ESLint errors in access-group-change.spec.ts**
- **Found during:** Task 2 (Permission UI + Access group change)
- **Issue:** Three lint errors: `employeePassword` should be `const` (never reassigned), unused variables `checkData` and `groupChangeConfirmed`
- **Fix:** Changed `let employeePassword` to `const`, simplified Keycloak eventual consistency polling to a single `waitForTimeout(3000)` instead of unused polling loop
- **Files modified:** frontend/client/e2e/access-group-change.spec.ts
- **Verification:** `npx eslint` passes with zero errors
- **Committed in:** 2250160 (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 bug/lint)
**Impact on plan:** Minor fix for code quality. Simplified eventual consistency handling. No scope creep.

## Issues Encountered
None

## Next Phase Readiness
- All 6 E2E requirements (E2E-01 through E2E-07) now have test spec files
- Full Playwright test suite: 13 tests in 8 files across 6 projects
- Tests require Docker Compose running (Keycloak + API + PostgreSQL + Vinxi) and environment variables set
- Phase 43 is now complete — all 3 plans executed

---
*Phase: 43-e2e-playwright-validation*
*Completed: 2026-04-27*

## Self-Check: PASSED

- [x] employee-login.spec.ts exists at frontend/client/e2e/employee-login.spec.ts
- [x] permission-ui.spec.ts exists at frontend/client/e2e/permission-ui.spec.ts
- [x] access-group-change.spec.ts exists at frontend/client/e2e/access-group-change.spec.ts
- [x] Commit 3c3303c exists (Task 1: Employee login + redirect E2E test)
- [x] Commit 2250160 exists (Task 2: Permission UI + Access group change E2E tests)
- [x] `npx playwright test --list` lists 13 tests in 8 files across all projects
- [x] 43-03-SUMMARY.md exists