# Phase 20 Plan 02 — LGPD Deletion Flow — SUMMARY

## Status: COMPLETE

All 8 tasks implemented and verified. All 156 tests pass, build succeeds.

## Tasks Completed

### Task 1: Add deleteUser API Function
- **File:** `D:\REPO\keycloak-tests\frontend\backoffice\src\lib\admin-api.ts`
- Added `deleteUser(userId)` function calling DELETE /api/admin/users/{id}
- Returns `Promise<void>` (204 No Content)
- Throws `AdminApiError` with status 404 (not found) or 409 (already deleted)
- Uses existing `credentials: "include"` for cookie-based auth

### Task 2: Create DeleteDialog Molecule
- **File:** `D:\REPO\keycloak-tests\frontend\backoffice\src\components\molecules\DeleteDialog.tsx` (NEW)
- shadcn/ui Dialog with "Delete User" title
- Red warning alert: "This action is PERMANENT and cannot be undone" + LGPD compliance details
- Displays user info (name, email, document)
- Email confirmation input — user must type exact email (case-insensitive) to enable delete
- Loading state with spinner during submission
- Success/error toast handling (including 409 specific error)
- Calls `onSuccess()` callback for table refresh after successful deletion

### Task 3: Add Delete Button to UsersTable
- **File:** `D:\REPO\keycloak-tests\frontend\backoffice\src\components\molecules\AdminUsersTable.tsx`
- Added `onDelete?: (id: string) => void` prop
- Trash icon button with "Delete user (LGPD)" tooltip
- Disabled for deleted users (`deletedAt` set)
- Only renders when `onDelete` prop is provided

### Task 4: Add Delete Button to UserDetailPage
- **File:** `D:\REPO\keycloak-tests\frontend\backoffice\src\components\pages\AdminUserDetailPage.tsx`
- Wired `onDelete` on UserDetailCard to open DeleteDialog
- Added `deleteDialogOpen` state and `handleDelete` function
- On success: shows toast and navigates back to /admin/users
- Includes DeleteDialog component with user data

### Task 5: Handle Deleted Users in Table
- **File:** `D:\REPO\keycloak-tests\frontend\backoffice\src\components\molecules\AdminUsersTable.tsx`
- Deleted users show "Deletado" badge with destructive (red) variant
- Edit, Block/Unblock, and Delete buttons all disabled/hidden for deleted users
- Deleted rows have `opacity-60` class for visual distinction

### Task 6: Write Tests for DeleteDialog
- **File:** `D:\REPO\keycloak-tests\frontend\backoffice\src\tests\delete-dialog.test.tsx` (NEW)
- 12 tests covering:
  - Renders with title and warning message
  - Displays user info (name, email, document)
  - Hides document when not provided
  - Confirm button disabled when email doesn't match
  - Confirm button enabled when email matches (case-insensitive)
  - Calls onDelete on confirm
  - Shows loading state during submit
  - Shows success toast and calls onSuccess
  - Shows error toast on 409
  - Shows error toast on other API failures
  - Cancel button works
  - Clears input on close/reopen

### Task 7: Write Integration Tests for Deletion
- **File:** `D:\REPO\keycloak-tests\frontend\backoffice\src\tests\admin-delete-flow.test.tsx` (NEW)
- 5 tests covering:
  - Delete from detail page -> dialog -> type email -> confirm -> success -> redirect
  - Attempt to delete already-deleted user -> 409 error toast
  - Delete from table -> dialog -> type email -> confirm -> success -> table refresh
  - Cancel delete closes dialog without calling API
  - Delete button not shown for deleted user in detail page

### Task 8: Update AdminUsersTable Tests
- **File:** `D:\REPO\keycloak-tests\frontend\backoffice\src\tests\admin-users-table.test.tsx` (APPENDED)
- 8 new tests in "Admin Users Table - Delete and Deleted Users" describe block:
  - Delete button disabled for deleted users
  - Delete button enabled for non-deleted users
  - Deleted badge shows for users with deletedAt
  - Row styling different for deleted users (opacity-60)
  - Edit button disabled for deleted users
  - Block/Unblock buttons not shown for deleted users
  - Calls onDelete when delete button clicked
  - Delete button not rendered when onDelete prop not provided

### Additional: AdminUsersPage Integration
- **File:** `D:\REPO\keycloak-tests\frontend\backoffice\src\components\pages\AdminUsersPage.tsx`
- Wired `onDelete` to AdminUsersTable with DeleteDialog
- Added `deleteUserId` state and `handleDelete` function
- Automatic table refresh via `fetchUsers()` after successful deletion

## Verification Results

- **Tests:** 156 passed, 0 failed (18 test files)
- **Build:** Success (vinxi build completed)
- **No cross-imports:** No imports from frontend/client or monolith paths

## Files Changed

### Created (3)
- `frontend/backoffice/src/components/molecules/DeleteDialog.tsx`
- `frontend/backoffice/src/tests/delete-dialog.test.tsx`
- `frontend/backoffice/src/tests/admin-delete-flow.test.tsx`

### Modified (5)
- `frontend/backoffice/src/lib/admin-api.ts` — added `deleteUser()`
- `frontend/backoffice/src/components/molecules/AdminUsersTable.tsx` — added delete button, deleted user handling
- `frontend/backoffice/src/components/pages/AdminUserDetailPage.tsx` — wired delete dialog
- `frontend/backoffice/src/components/pages/AdminUsersPage.tsx` — wired delete dialog + table refresh
- `frontend/backoffice/src/tests/admin-users-table.test.tsx` — 8 new tests for delete/deleted users

## Ready for Phase 21 (E2E Tests)
