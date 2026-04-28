---
phase: 40-client-frontend-pj-registration
plan: 04
subsystem: frontend-client
tags: [dashboard, mock-data, permission-routing, profile-pj, pf-removal, svg-sparkline, progress-bar, group-redirect]
dependency_graph:
  requires:
    - plan: 40-01
      provides: auth-context with accessGroup/companyId, CompanyProfileDto, sidebar, router, shadcn/ui Card component
    - plan: 40-03
      provides: EmployeesPage with employee API functions, AppLayout
  provides:
    - DashboardPage with 6 mock data cards and mini charts
    - DashboardCards molecule with progress bars and SVG sparklines
    - Group-based default route helper (getDefaultRouteForGroup)
    - Root route redirect based on access group (admin-empresa/viewer → /employees, dashboard → /dashboard)
    - Sidebar visibility finalized (dashboard group sees Employees link)
    - ProfilePage adapted for PJ-only company data (no standalone layout)
    - Zero PF references verified in client frontend source
  affects: []
tech_stack:
  added: []
  patterns: [svg-polyline-sparkline, progress-bar-component, group-based-default-route, error-state-in-profile]
key_files:
  created:
    - frontend/client/src/components/molecules/DashboardCards.tsx
  modified:
    - frontend/client/src/components/pages/DashboardPage.tsx
    - frontend/client/src/components/pages/ProfilePage.tsx
    - frontend/client/src/components/organisms/Sidebar.tsx
    - frontend/client/src/lib/auth-context.tsx
    - frontend/client/src/router.tsx
  deleted: []
key_decisions:
  - "SVG sparklines (polyline) chosen over Chart.js to minimize bundle size per plan guidance"
  - "ProfilePage removed standalone Header/min-h-screen — rendered inside AppLayout with sidebar"
  - "Group-based redirect: admin-empresa/viewer → /employees, dashboard → /dashboard, null → /profile (D-22)"
  - "Sidebar updated: dashboard group now sees Employees link (read-only per Plan 03 decision)"
  - "Total Funcionários card fetches real API count via getEmployees with page=1, pageSize=1"
requirements_completed: [DASH-01, PERM-04]
metrics:
  duration: 8min
  completed: 2026-04-26
---

# Phase 40 Plan 04: Dashboard + Permission Routing Summary

**Dashboard with 6 mock cards (progress bars + SVG sparklines), group-based routing, PJ-only ProfilePage, and zero PF references**

## Performance

- **Duration:** 8 min
- **Started:** 2026-04-26T15:59:58Z
- **Completed:** 2026-04-26T16:08:00Z
- **Tasks:** 2
- **Files modified:** 5, created 1

## Accomplishments

1. **DashboardCards molecule** — 6 card components: Total Funcionários (24), Ativos (22/24 with green progress bar), Bloqueados (2/24 with red progress bar), Logins Recentes 7d (45 with blue sparkline), Ações Recentes 7d (128 with purple sparkline), Último Login ("há 2h")
2. **DashboardPage** — Grid layout (2 cols desktop, 1 mobile), auth guard, permission guard for admin-empresa/dashboard groups, optional real totalCount fetch from API
3. **Group-based routing** — `getDefaultRouteForGroup()` helper returns `/employees` for admin-empresa/viewer, `/dashboard` for dashboard group, `/profile` as fallback
4. **Root route redirect** — `RootRoute` now redirects authenticated users to their group-specific default route instead of hardcoded `/dashboard`
5. **Sidebar finalized** — Dashboard group now sees Employees link (consistent with Plan 03's read-only employee access for dashboard)
6. **ProfilePage cleaned** — Removed standalone Header and min-h-screen layout; now renders inside AppLayout with sidebar. Added error state for profile fetch failure. Shows PJ-only: Razão Social, CNPJ, Email, Telefone
7. **PF removal verified** — Zero references to PessoaFisica, PersonTypeRadio, pfRegistration, tipo=pf, or ClientProfileDto in frontend/client/src/

## Task Commits

1. **Task 1: Dashboard page with 6 mock cards and mini charts** — `4ea15eb` (feat) — DashboardCards molecule, DashboardPage grid, progress bars, sparklines
2. **Task 2: Permission routing, ProfilePage PJ-only, PF removal** — `71e3544` (feat) — Group-based redirect, Sidebar update, ProfilePage cleanup

## Files Created

- `frontend/client/src/components/molecules/DashboardCards.tsx` — 6 dashboard card components with mock data, progress bars, and SVG sparklines

## Files Modified

- `frontend/client/src/components/pages/DashboardPage.tsx` — Replaced placeholder with full dashboard: grid layout, 6 cards, auth/permission guards, optional API fetch
- `frontend/client/src/components/pages/ProfilePage.tsx` — Removed standalone Header, added error state, rendered inside AppLayout
- `frontend/client/src/components/organisms/Sidebar.tsx` — Added dashboard group to Employees visibility
- `frontend/client/src/lib/auth-context.tsx` — Added `getDefaultRouteForGroup()` helper and GROUP_DEFAULT_ROUTES map
- `frontend/client/src/router.tsx` — RootRoute redirects based on access group using getDefaultRouteForGroup

## Decisions Made

- SVG sparklines (polyline) chosen over Chart.js — no extra bundle weight, simple and lightweight
- ProfilePage no longer has its own Header since it renders inside AppLayout (sidebar + header already provided)
- Total Funcionários card fetches real `totalCount` from API via `getEmployees(companyId, { page: 1, pageSize: 1 })` — falls back to mock 24 on error
- Dashboard group added to Employees sidebar visibility — consistent with Plan 03 which added dashboard to /employees route with read-only table

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Unused useCallback import in auth-context.tsx**
- **Found during:** Task 2 ESLint check
- **Issue:** Added `useCallback` to import but didn't use it in the final code
- **Fix:** Removed `useCallback` from the import statement
- **Files modified:** `frontend/client/src/lib/auth-context.tsx`
- **Verification:** ESLint passes with 0 errors
- **Committed in:** `71e3544`

---

**Total deviations:** 1 auto-fixed (1 unused import bug)
**Impact on plan:** No scope creep. Fix was trivial — remove unused import to pass linting.

## Known Stubs

| File | Stub | Reason |
|------|------|--------|
| `DashboardCards.tsx` | All 5 non-Total cards use hardcoded mock data | D-18 specifies mock data hardcoded — real API will come in future milestone |
| `DashboardCards.tsx` | TotalEmployeesCard falls back to 24 when API fails | D-18 allows API call but mock fallback is needed for offline/error scenarios |

## Threat Flags

No new threat surface beyond what was in the plan's threat model. All mitigations maintained:
- T-40-12 (Elevation): Client-side route hiding is UX-only — backend enforces permissions ✅
- T-40-13 (Information disclosure): Mock data contains no real user data ✅
- T-40-14 (Tampering): PF references completely removed from frontend ✅

## Self-Check: PASSED

- DashboardCards.tsx exists on disk: ✅
- DashboardPage.tsx renders 6-card grid: ✅
- ProfilePage shows PJ-only data (Razão Social, CNPJ, Email, Telefone): ✅
- Auth context redirects by group (admin-empresa/viewer → /employees, dashboard → /dashboard): ✅
- Sidebar shows permitted links per group: ✅ (admin-empresa: all, viewer: Employees+Profile, dashboard: all three)
- Zero PF references in client frontend: ✅ (only comment in types.ts)
- TypeScript compiles without errors: ✅ (`npx tsc --noEmit` — 0 errors)
- Commit hashes 4ea15eb, 71e3544, c0038db found in git log: ✅

---
*Phase: 40-client-frontend-pj-registration*
*Completed: 2026-04-26*