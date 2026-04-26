---
phase: 40-client-frontend-pj-registration
plan: 01
subsystem: frontend-client
tags: [auth-context, types, api, validation, sidebar, router, pj-only]
dependency_graph:
  requires:
    - phase: 39-keycloak-groups-permissions
      provides: Keycloak group claims, permission policies, CompaniesController endpoints
  provides:
    - auth-context with accessGroup/companyId
    - CompanyProfileDto, EmployeeDto, PaginatedEmployeesResult types
    - 6 employee CRUD API functions
    - PJ-only validation schemas (companyDataSchema, companyAccessSchema, editEmployeeSchema)
    - Sidebar with permission-based navigation
    - AppLayout with sidebar + header + Outlet
    - DashboardPage and EmployeesPage placeholders
    - Router with /dashboard, /employees, /profile, /register routes
  affects: [40-02, 40-03, 40-04]
tech_stack:
  added: []
  patterns: [permission-based sidebar visibility, group-based badge display, company-scoped API calls]
key_files:
  created:
    - frontend/client/src/components/organisms/Sidebar.tsx
    - frontend/client/src/components/templates/AppLayout.tsx
    - frontend/client/src/components/pages/DashboardPage.tsx
    - frontend/client/src/components/pages/EmployeesPage.tsx
  modified:
    - frontend/client/src/lib/auth-context.tsx
    - frontend/client/src/lib/types.ts
    - frontend/client/src/lib/api.ts
    - frontend/client/src/lib/validation-schemas.ts
    - frontend/client/src/components/atoms/ProfileBadge.tsx
    - frontend/client/src/components/organisms/Header.tsx
    - frontend/client/src/components/molecules/ProfileCard.tsx
    - frontend/client/src/components/molecules/RegistrationForm.tsx
    - frontend/client/src/components/pages/ProfilePage.tsx
    - frontend/client/src/router.tsx
  deleted:
    - frontend/client/src/components/molecules/PersonTypeRadio.tsx
key_decisions:
  - "Tasks 1+2 combined into single commit since types/api/schemas changes were interdependent for TypeScript compilation"
  - "CompanyProfileDto endpoint changed from /api/clients/me to /api/companies/me (matching backend Phase 37+)"
  - "registerCompany endpoint changed from /api/registration to /api/companies/registration (matching backend refactoring)"
  - "Sidebar uses TanStack Router's useMatchRoute for active link highlighting"
  - "AppLayout uses ml-64 offset for authenticated content (width of fixed sidebar)"
  - "DashboardPage and EmployeesPage are placeholders — will be filled in Plans 03 and 04"
  - "RegistrationForm simplified to PJ-only with terms acceptance checkbox (wizard in Plan 02)"
requirements_completed: [REG-05, PERM-04, DASH-01]
metrics:
  duration: 25min
  completed: 2026-04-26
---

# Phase 40 Plan 01: Client Foundation — Auth, Types, API, Schemas, Sidebar, Router Summary

**Auth context extended with group/permissions, employee API client added, PJ-only schemas created,_sidebar with permission-based navigation, router restructured**

## Performance

- **Duration:** 25 min
- **Started:** 2026-04-26T14:49:00Z
- **Completed:** 2026-04-26T15:14:00Z
- **Tasks:** 3 (Tasks 1+2 combined due to interdependencies)
- **Files modified:** 16, created 4, deleted 1

## Accomplishments

1. **Auth Context Extended** — `/auth/me` response now parses `accessGroup` and `companyId` fields with null fallback for backward compatibility
2. **Types Replaced** — `ClientProfileDto` (PF/PJ combined) replaced with `CompanyProfileDto` (PJ-only); added `EmployeeDto` and `PaginatedEmployeesResult`
3. **ProfileBadge Updated** — Shows group-based badges: Admin Empresa (green), Viewer (gray), Dashboard (blue)
4. **Employee API Client Added** — 6 functions: `getEmployees`, `toggleEmployeeStatus`, `resetEmployeePassword`, `updateEmployee`, `deleteEmployee`, `changeEmployeeAccessGroup`
5. **PJ-Only Schemas Created** — `companyDataSchema` (step 1), `companyAccessSchema` (step 2), `editEmployeeSchema`; `pfRegistrationSchema`, `registrationSchema`, `PersonTypeRadio` removed
6. **Sidebar Created** — Fixed left sidebar with `LayoutDashboard`, `Users`, `Building2` icons; permission-based link visibility (admin-empresa: all, viewer: Employees+Profile, dashboard: Dashboard+Profile)
7. **AppLayout Created** — Wraps authenticated routes with sidebar + header + `<Outlet />`
8. **Router Restructured** — `/dashboard` (default), `/employees`, `/profile`, `/register` (PJ wizard, no sidebar); authenticated routes use AppLayout; index `/` redirects to `/dashboard`
9. **Header Updated** — Shows access group badge when authenticated
10. **`getProfileClient` endpoint** changed from `/api/clients/me` to `/api/companies/me` (matching backend Phase 37+)
11. **`registerCompany` endpoint** changed from `/api/registration` to `/api/companies/registration`

## Task Commits

1. **Tasks 1+2 Combined** — `cb7fe6b` (feat) — Auth context with group/permissions, CompanyProfileDto, employee API, PJ-only schemas, PersonTypeRadio deleted
2. **Task 3: Sidebar + AppLayout + Router** — `756fb84` (feat) — Sidebar with permission-based navigation, AppLayout, Header group badge, DashboardPage/EmployeesPage placeholders, router restructured

## Files Created

- `frontend/client/src/components/organisms/Sidebar.tsx` — Permission-based sidebar navigation with lucide-react icons
- `frontend/client/src/components/templates/AppLayout.tsx` — Authenticated layout wrapper (sidebar + header + Outlet)
- `frontend/client/src/components/pages/DashboardPage.tsx` — Placeholder dashboard page
- `frontend/client/src/components/pages/EmployeesPage.tsx` — Placeholder employees page

## Files Modified

- `frontend/client/src/lib/auth-context.tsx` — Added `accessGroup` and `companyId` to AuthContextValue, parsing from /auth/me
- `frontend/client/src/lib/types.ts` — Replaced `ClientProfileDto` with `CompanyProfileDto`, added `EmployeeDto`, `PaginatedEmployeesResult`
- `frontend/client/src/lib/api.ts` — Replaced `registerClient`/`RegisterClientRequest` with `registerCompany`/`RegisterCompanyRequest`, updated endpoint to `/api/companies/registration`, changed `getProfileClient` to `/api/companies/me`, added 6 employee CRUD functions
- `frontend/client/src/lib/validation-schemas.ts` — Removed `pfRegistrationSchema`, `registrationSchema`, `PfRegistrationData`, `RegistrationData`; added `companyDataSchema`, `companyAccessSchema`, `editEmployeeSchema`; exported `validateCpf`, `validateCnpj`, `passwordSchema`
- `frontend/client/src/components/atoms/ProfileBadge.tsx` — Changed from PF/PJ type to group-based (admin-empresa/viewer/dashboard) with color coding
- `frontend/client/src/components/organisms/Header.tsx` — Added group badge display when authenticated
- `frontend/client/src/components/molecules/ProfileCard.tsx` — Updated to use `CompanyProfileDto` and `AccessGroup` props
- `frontend/client/src/components/molecules/RegistrationForm.tsx` — Simplified to PJ-only form with terms acceptance checkbox; removed PersonTypeRadio and PF fields
- `frontend/client/src/components/pages/ProfilePage.tsx` — Updated to PJ-only company profile display
- `frontend/client/src/router.tsx` — Added `/dashboard`, `/employees`, `/profile` under authenticated layout; `/register` outside AppLayout; index redirects to `/dashboard`
- All test files updated to reflect new types, group badges, and PJ-only forms

## Files Deleted

- `frontend/client/src/components/molecules/PersonTypeRadio.tsx` — Removed (PF/PJ selector no longer needed for PJ-only flow)

## Decisions Made

- Combined Tasks 1+2 into one commit since `types.ts` changes cascaded into `api.ts`, validation schemas, and multiple test files
- Changed API endpoint from `/api/clients/me` to `/api/companies/me` and `/api/registration` to `/api/companies/registration` to match backend Phase 37+ domain model redesign
- Sidebar uses `useMatchRoute` from TanStack Router for active link highlighting
- RegistrationForm is simplified (not yet wizard) — Plan 02 will create the full 2-step wizard
- `validateCpf` remains exported even though PF flow is removed — it's used by `EmployeeDto.cpf`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Critical] Zod 4 `z.literal(true)` API incompatibility**
- **Found during:** Task 2 implementation
- **Issue:** Plan specified `z.literal(true, { errorMap: () => ... })` which is Zod 3 API
- **Fix:** Changed to `z.literal(true, { message: "..." })` which is Zod 4 API
- **Files:** `validation-schemas.ts`
- **Commit:** `cb7fe6b`

**2. [Rule 3 - Blocking] TypeScript compilation required interleaved task execution**
- **Found during:** Task 1 verification
- **Issue:** Changing `ClientProfileDto` to `CompanyProfileDto` in `types.ts` broke imports in `api.ts`, `ProfileCard.tsx`, `ProfilePage.tsx`, and all test files — TypeScript wouldn't compile between Task 1 and Task 2
- **Fix:** Combined Tasks 1+2 into a single atomic commit since the changes were interdependent
- **Files:** All affected files updated together
- **Commit:** `cb7fe6b`

**3. [Rule 3 - Blocking] ESLint unused variable errors in test files**
- **Issue:** `mockCompanyProfile` unused in simplified e2e test; `AccessGroup` type imported but not used
- **Fix:** Removed unused imports and variables
- **Files:** `profile-e2e.test.tsx`, `profile-page.test.tsx`

## Known Stubs

| File | Stub | Reason |
|------|------|--------|
| `RegistrationForm.tsx` | `razaoSocial: ""` and `cnpj: ""` in onSubmit | Plan 02 will create the 2-step wizard that provides these values from step 1 |
| `DashboardPage.tsx` | "Em construção" placeholder | Plan 04 will fill in dashboard cards |
| `EmployeesPage.tsx` | "Em construção" placeholder | Plan 03 will fill in employee table |

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| threat_flag: client-side-route-hiding | `Sidebar.tsx` | Sidebar link visibility is UX-only — backend permission policies (Phase 39) enforce real access control. Accepted per T-40-04. |
| threat_flag: company-id-from-auth | `api.ts` | All employee API functions use `companyId` parameter — must come from auth context (server-validated claim), not from user input. Per T-40-02. |

## Self-Check: PASSED

- All 4 created files exist on disk: ✅
- All 2 commit hashes found in git log: ✅ (`cb7fe6b`, `756fb84`)
- TypeScript compiles without errors: ✅ (`npx tsc --noEmit` — 0 errors)
- ESLint passes with 0 warnings: ✅
- No PF references in client source: ✅ (`PessoaFisica`, `pfRegistration`, `PersonTypeRadio`, `tipo=pf` — none found)
- `accessGroup` in auth-context.tsx: ✅ (parsing from /auth/me response)
- 6 employee CRUD functions in api.ts: ✅ (`getEmployees`, `toggleEmployeeStatus`, `resetEmployeePassword`, `updateEmployee`, `deleteEmployee`, `changeEmployeeAccessGroup`)
- Routes in router.tsx: ✅ (`/dashboard`, `/employees`, `/profile`, `/register`)
- PersonTypeRadio.tsx deleted: ✅

---
*Phase: 40-client-frontend-pj-registration*
*Completed: 2026-04-26*