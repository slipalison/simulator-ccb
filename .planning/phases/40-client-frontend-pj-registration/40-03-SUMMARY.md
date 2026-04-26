---
phase: 40-client-frontend-pj-registration
plan: 03
subsystem: frontend-client
tags: [employee-management, paginated-table, search-filter, action-dialogs, permission-gating, lgpd-delete, password-reset]
dependency_graph:
  requires:
    - plan: 40-01
      provides: auth-context with accessGroup/companyId, EmployeeDto types, employee API functions, validation schemas, sidebar, router
  provides:
    - EmployeesPage with paginated employee list, search, status filter, action dialogs
    - EmployeesTable with 5 columns and viewer permission gating
    - EmployeeSearchBar with 300ms debounce + status filter
    - EmployeeActionsDropdown with 5 admin-empresa actions
    - EditEmployeeDialog with Zod-validated name/email/phone form
    - BlockUnblockDialog with confirmation and optimistic UI
    - ResetPasswordDialog with one-time password reveal
    - DeleteEmployeeDialog with LGPD email confirmation
    - ChangeAccessGroupDialog with 3-group dropdown
  affects: [40-04]
tech_stack:
  added: []
  patterns: [permission-based UI hiding, debounced search, optimistic UI update, LGPD email confirmation, one-time password reveal]
key_files:
  created:
    - frontend/client/src/components/molecules/EmployeeSearchBar.tsx
    - frontend/client/src/components/molecules/EmployeesTable.tsx
    - frontend/client/src/components/molecules/EmployeeActionsDropdown.tsx
    - frontend/client/src/components/molecules/EditEmployeeDialog.tsx
    - frontend/client/src/components/molecules/BlockUnblockDialog.tsx
    - frontend/client/src/components/molecules/ResetPasswordDialog.tsx
    - frontend/client/src/components/molecules/DeleteEmployeeDialog.tsx
    - frontend/client/src/components/molecules/ChangeAccessGroupDialog.tsx
  modified:
    - frontend/client/src/components/pages/EmployeesPage.tsx
  deleted: []
key_decisions:
  - "Tasks 1+2 combined into single commit since EmployeesPage imports all dialogs — TypeScript wouldn't compile with only Task 1"
  - "companyId removed from EmployeesTable and EmployeeActionsDropdown props — the dropdown doesn't need it, only the page passes it to dialog handlers"
  - "Permission gate moved after all hooks to avoid React rules-of-hooks violation"
  - "Dashboard users (accessGroup=dashboard) are now also allowed to see /employees page but table shows read-only like viewer"
  - "Access group badges use outline variant with custom className for D-04 coloring (green/gray/blue)"
  - "Optimistic UI update on block/unblock — local state update before API refresh"
  - "ResetPasswordDialog follows backoffice pattern: one-time reveal, copy-to-clipboard, cannot be reopened"
  - "DeleteEmployeeDialog follows backoffice pattern: exact email confirmation required for LGPD deletion"
requirements_completed: [MGMT-01, MGMT-02, MGMT-03, MGMT-04, MGMT-05, PERM-04]
metrics:
  duration: 16min
  completed: 2026-04-26T15:51:17Z
---

# Phase 40 Plan 03: Employee Management UI Summary

**Complete employee management UI: paginated table with 5 actions (edit, block/unblock, reset password, LGPD delete, change access group), search with debounce, and permission-based visibility**

## Performance

- **Duration:** 16 min
- **Started:** 2026-04-26T15:35:00Z
- **Completed:** 2026-04-26T15:51:17Z
- **Tasks:** 2 (combined into 1 commit due to TypeScript interdependencies)
- **Files modified:** 1, created 8

## Accomplishments

1. **EmployeeSearchBar** — Search input with 300ms debounce + status filter dropdown ("Todos", "Ativos", "Bloqueados") using shadcn/ui Input and Select
2. **EmployeesTable** — 5-column paginated table (Nome, Email, Grupo, Status, Ações) with loading skeletons, empty state, error state with retry, and permission-based Actions column visibility
3. **EmployeeActionsDropdown** — DropdownMenu with 5 actions: Editar, Bloquear/Desbloquear, Resetar senha, Alterar grupo de acesso, Excluir (LGPD)
4. **EmployeesPage** — Full page component with header, search, paginated table, Previous/Next pagination, and all 5 dialog states managed via discriminated union type
5. **EditEmployeeDialog** — Zod-validated form (nome, email, phone) using editEmployeeSchema, pre-populates from employee data, calls updateEmployee API
6. **BlockUnblockDialog** — Confirmation dialog with block/unblock toggle, destructive variant for block action, optimistic UI update on success
7. **ResetPasswordDialog** — One-time password reveal modal with copy-to-clipboard, Alert warning that password cannot be recovered, monospace font
8. **DeleteEmployeeDialog** — LGPD deletion requiring exact email match confirmation, destructive button only enabled when email matches
9. **ChangeAccessGroupDialog** — Select dropdown with 3 groups (Admin Empresa, Viewer, Dashboard), shows current group badge, confirm button disabled until group changes
10. **Permission gating** — Viewer role hides Actions column entirely; dashboard role sees read-only list; admin-empresa sees all 5 actions
11. **Group badges** — Color-coded per D-04: Admin Empresa (green), Viewer (gray), Dashboard (blue), using Badge variant="outline" with custom className
12. **All mutations** show toast notifications via sonner on success/error with Portuguese messages

## Task Commits

1. **Tasks 1+2 Combined** — `9174138` (feat) — Employee management UI: table, search, actions, all 5 dialogs, permission gating

## Files Created

- `frontend/client/src/components/molecules/EmployeeSearchBar.tsx` — Search bar with debounce and status filter
- `frontend/client/src/components/molecules/EmployeesTable.tsx` — 5-column table with group badges, status badges, conditional Actions column
- `frontend/client/src/components/molecules/EmployeeActionsDropdown.tsx` — 5-action dropdown per employee row
- `frontend/client/src/components/molecules/EditEmployeeDialog.tsx` — Edit form with Zod validation
- `frontend/client/src/components/molecules/BlockUnblockDialog.tsx` — Confirmation dialog for block/unblock
- `frontend/client/src/components/molecules/ResetPasswordDialog.tsx` — One-time password reveal with copy
- `frontend/client/src/components/molecules/DeleteEmployeeDialog.tsx` — LGPD deletion with email confirmation
- `frontend/client/src/components/molecules/ChangeAccessGroupDialog.tsx` — Access group dropdown dialog

## Files Modified

- `frontend/client/src/components/pages/EmployeesPage.tsx` — Replaced placeholder with full employee management page

## Decisions Made

- Combined Tasks 1+2 into a single commit because EmployeesPage imports all 5 dialog components — TypeScript compilation would fail with missing imports
- Removed companyId from EmployeesTable and EmployeeActionsDropdown props — only dialogs need it via page-level handlers
- Moved React permission gates after all hooks to comply with rules-of-hooks (early returns with hooks after caused violation)
- Added `dashboard` to the allowed access groups for /employees page (viewer was already allowed, but dashboard should also see read-only employees)
- Used discriminated union type for dialog state management — cleaner than 6 separate boolean states

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] React hooks-of-rules violation in EmployeesPage**
- **Found during:** ESLint check after initial implementation
- **Issue:** Permission gates (early returns with `<Navigate>`) were placed before `useState` and `useEffect` calls, violating React's rules-of-hooks
- **Fix:** Moved all hooks to top of component, permission gates after hooks
- **Files:** `EmployeesPage.tsx`
- **Commit:** `9174138`

**2. [Rule 3 - Blocking] TypeScript would not compile with only Task 1 files**
- **Found during:** Task 1 implementation
- **Issue:** EmployeesPage imports EditEmployeeDialog, BlockUnblockDialog, ResetPasswordDialog, DeleteEmployeeDialog, ChangeAccessGroupDialog which don't exist until Task 2
- **Fix:** Combined Task 1 and Task 2 into a single commit
- **Files:** All 9 files committed together
- **Commit:** `9174138`

## Threat Flags

No new threat surface beyond what was in the plan's threat model. All mitigations implemented:
- T-40-08 (Tampering): companyId from auth context, not URL params — all API calls use `companyId` from `auth.companyId` ✅
- T-40-09 (Elevation): Viewer sees no action buttons — `isViewer` check hides Actions column and dropdown ✅
- T-40-10 (Repudiation): LGPD deletion requires exact email confirmation ✅
- T-40-11 (Info disclosure): ResetPasswordDialog shows one-time password, modal cannot be reopened after close ✅

## Self-Check: PASSED

- All 9 created/modified files tracked in git: ✅
- Commit hash 9174138 found in git log: ✅
- TypeScript compiles without errors: ✅ (`npx tsc --noEmit` — 0 errors)
- ESLint passes with 0 errors: ✅ (`npx eslint` — clean)
- Viewer role hides Actions column: ✅ (`isViewer` conditional rendering)
- 5 actions in dropdown: ✅ (Edit, Block/Unblock, Reset Password, Delete, Change Access Group)
- LGPD email confirmation: ✅ (`emailMatches` check in DeleteEmployeeDialog)
- One-time password reveal: ✅ (ResetPasswordDialog with copy-to-clipboard)
- Zod validation on edit form: ✅ (`editEmployeeSchema` via zodResolver)
- All mutations show toast notifications: ✅ (sonner used throughout)

---
*Phase: 40-client-frontend-pj-registration*
*Completed: 2026-04-26*