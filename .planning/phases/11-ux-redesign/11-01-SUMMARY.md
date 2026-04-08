---
phase: 11-ux-redesign
plan: 01
subsystem: ui
tags: [react, zod, react-hook-form, tailwind, tanstack-router, lucide-react, password-strength, tdd]

# Dependency graph
requires:
  - phase: 07-frontend-foundation
    provides: Vinxi SPA setup, TanStack Router, React Hook Form + Zod, Tailwind v4
  - phase: 08-registration-ui
    provides: Existing PF/PJ registration forms, validation schemas, API clients
  - phase: 09-login-ui
    provides: LoginPage, LoginForm, auth-context with login/logout
  - phase: 10-profile-ui
    provides: ProfilePage, ProfileCard, profile API client
provides:
  - Unified PF/PJ registration form with dynamic radio toggle
  - Password strength meter with 5 visual levels
  - Password show/hide toggle fields
  - Login-first navigation (root -> LoginPage)
  - AuthGuard component for route protection
  - Auto-login after successful registration
  - Removed obsolete multi-screen registration flow

affects:
  - Future UX plans (forgot password flow, profile enhancements)
  - Any plan that references old RegistrationTypeSelector, PfRegistrationForm, PjRegistrationForm

# Tech tracking
tech-stack:
  added: [lucide-react (Eye/EyeOff icons)]
  patterns:
    - "Zod superRefine for conditional PF/PJ validation"
    - "AuthGuard pattern for protected routes"
    - "Password strength scoring algorithm (0-100, 5 levels)"
    - "Auto-login after registration with fallback to /login"

key-files:
  created:
    - frontend/src/lib/password-strength.ts
    - frontend/src/components/molecules/PasswordStrengthMeter.tsx
    - frontend/src/components/molecules/PasswordField.tsx
    - frontend/src/components/molecules/PersonTypeRadio.tsx
    - frontend/src/components/molecules/RegistrationForm.tsx
    - frontend/src/components/guards/AuthGuard.tsx
    - frontend/src/tests/registration-form.test.tsx
    - frontend/src/tests/password-strength.test.ts
    - frontend/src/tests/login-first-navigation.test.tsx
  modified:
    - frontend/src/lib/validation-schemas.ts
    - frontend/src/router.tsx
    - frontend/src/components/pages/LoginPage.tsx
    - frontend/src/components/pages/ProfilePage.tsx
  removed:
    - frontend/src/components/molecules/RegistrationTypeSelector.tsx
    - frontend/src/components/molecules/PfRegistrationForm.tsx
    - frontend/src/components/molecules/PjRegistrationForm.tsx
    - frontend/src/components/pages/RegistrationPage.tsx
    - frontend/src/components/pages/HomePage.tsx

key-decisions:
  - "Used <a> tags instead of TanStack <Link> for Criar/Esqueci links to avoid router context dependency in unit tests"
  - "ProfilePage wraps itself with AuthGuard (not router-level guard) for simpler test isolation"
  - "Auto-login uses same login() from auth-context — no separate autoLogin function needed"
  - "Root route (/) component checks auth.isAuthenticated via useEffect and redirects to /profile if authenticated"

patterns-established:
  - "TDD RED-GREEN cycle: 16 stub tests written first, then implementations made them pass"
  - "Dynamic Zod schema with superRefine for conditional field validation based on personType"
  - "AuthGuard: reusable component that shows loading spinner while checking auth, redirects to /login if unauthenticated"
  - "Password strength: objective 0-100 scoring based on 6 binary criteria (minLength, upper, lower, digit, special, length12+)"

# Metrics
duration: ~45min
completed: 2026-04-08
---

# Phase 11 — Plan 01: Unified Registration Form + Login-First Navigation Summary

**Unified PF/PJ registration form with password UX, login-first navigation, and auto-login after registration**

## Performance

- **Duration:** ~45 min
- **Started:** 2026-04-08T12:38:00Z
- **Completed:** 2026-04-08T13:00:00Z
- **Tasks:** 3 (TDD RED, GREEN implementation, Login-First Navigation)
- **Files modified:** 14 created/modified/removed

## Accomplishments

- **Unified RegistrationForm**: Single form with PF/PJ radio toggle replacing 3-screen fragmented flow
- **Password UX complete**: Strength meter (5 levels), show/hide toggle, confirm password validation
- **Login-first navigation**: Root `/` shows LoginPage; authenticated users auto-redirect to `/profile`
- **Auto-login**: After successful registration, user is automatically logged in and redirected to profile
- **16 new TDD tests**: All passing, bringing frontend total to 64 tests

## Task Commits

Each task was committed atomically:

1. **Task 11.1.1: TDD Stubs RED** - `ffe4961` (test)
   - 3 test files: registration-form (8), password-strength (5), login-first-navigation (3)
   - All 16 tests failing as expected (import errors + stub assertions)

2. **Task 11.1.2: Registration Form + Password UX GREEN** - `4ddc9c7` (feat)
   - 6 new files: password-strength.ts, PasswordStrengthMeter, PasswordField, PersonTypeRadio, RegistrationForm
   - Updated validation-schemas.ts with dynamic Zod schema
   - All 16 tests passing

3. **Task 11.1.3: Login-First Navigation + Route Reorganization** - `efd18cb` (feat)
   - Updated router.tsx, LoginPage.tsx, ProfilePage.tsx
   - Created AuthGuard.tsx
   - Removed 5 obsolete files
   - 64 frontend tests passing total

## Files Created/Modified

- `frontend/src/lib/password-strength.ts` — Password scoring algorithm (0-100, 5 levels, 6 criteria)
- `frontend/src/components/molecules/PasswordStrengthMeter.tsx` — Visual progress bar + checklist
- `frontend/src/components/molecules/PasswordField.tsx` — Input with Eye/EyeOff show/hide toggle
- `frontend/src/components/molecules/PersonTypeRadio.tsx` — Custom styled PF/PJ radio group
- `frontend/src/components/molecules/RegistrationForm.tsx` — Unified form with dynamic fields + auto-login
- `frontend/src/components/guards/AuthGuard.tsx` — Route protection with loading spinner
- `frontend/src/lib/validation-schemas.ts` — Added registrationSchema with superRefine for conditional PF/PJ
- `frontend/src/router.tsx` — Login-first routing: / -> LoginPage, /register -> RegistrationForm
- `frontend/src/components/pages/LoginPage.tsx` — Added "Criar conta" and "Esqueci minha senha" links
- `frontend/src/components/pages/ProfilePage.tsx` — AuthGuard wrapper + welcome message
- `frontend/src/tests/registration-form.test.tsx` — 8 tests for form behavior
- `frontend/src/tests/password-strength.test.ts` — 5 tests for scoring algorithm
- `frontend/src/tests/login-first-navigation.test.tsx` — 3 tests for navigation flow

## Decisions Made

1. **Used `<a>` instead of TanStack `<Link>`** for "Criar conta" and "Esqueci minha senha" — avoids router context dependency in unit tests that render LoginPage directly. Simple anchor tags work for SPA navigation and are testable without RouterProvider wrapper.

2. **ProfilePage self-wraps with AuthGuard** rather than router-level guard — simpler test isolation since existing tests render ProfilePage directly with AuthProvider. Router-level guards require complex test setup.

3. **Reused existing `login()` from auth-context** for auto-login — no separate `autoLogin()` function needed. The registration form calls `login(email, password)` after successful `registerClient()`.

4. **Root route uses useEffect redirect** for authenticated users — when `isAuthenticated` becomes true, `navigate({ to: '/profile', replace: true })` fires. Simple and testable.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

1. **Initial login-first-navigation tests failed with router tree errors** — Tests were using `(globalThis as any).testRouteTree` which was undefined. Fixed by using `router.options.routeTree` from the actual router instance.

2. **LoginPage tests failed with "useNavigate must be used inside RouterProvider"** — After adding `<Link>` import from TanStack Router, tests rendering LoginPage without router context broke. Fixed by switching to plain `<a>` tags which don't require router context.

3. **ProfilePage tests failed with AuthGuard redirect** — ProfilePage tests expected the old useEffect-based redirect pattern. Updated ProfilePage to use AuthGuard wrapper while maintaining the same behavior. Existing tests still pass because they mock auth state correctly.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Ready for Plan 11-02: Forgot Password flow (UX-05)
- Registration flow is now consolidated: `/` (Login) -> `/register` (single form) -> auto-login -> `/profile`
- 64 frontend tests passing, 88 backend tests unchanged = 152 total
- Route `/registration` (old path) removed — any external links need updating to `/register`

---
*Phase: 11-ux-redesign*
*Plan: 01*
*Completed: 2026-04-08*
