---
phase: 30-audit-log-admin-backend
plan: "05"
subsystem: backend + frontend
tags: [gap-closure, code-review, CR-01, CR-02, CR-03]
dependency_graph:
  requires:
    - 30-04 (createAdmin URL fix)
  provides:
    - CR-01: RandomNumberGenerator resource leak fixed
    - CR-02: deleteUser() sends confirmEmail body
    - CR-03: CreateAdmin returns 200 OK (not 201 CreatedAtAction)
  affects:
    - src/Onboarding.Application/Admin/Commands/CreateAdminCommand.cs
    - src/Onboarding.API/Controllers/AdminUserController.cs
    - frontend/backoffice/src/lib/admin-api.ts
    - frontend/backoffice/src/components/molecules/DeleteDialog.tsx
    - frontend/backoffice/src/components/pages/AdminUsersPage.tsx
    - frontend/backoffice/src/components/pages/AdminUserDetailPage.tsx
tech_stack:
  added: []
  patterns:
    - "RandomNumberGenerator.GetInt32() — static, no allocation, no modulo bias"
    - "DeleteDialog passes confirmEmail to onDelete callback"
key_files:
  created: []
  modified:
    - src/Onboarding.Application/Admin/Commands/CreateAdminCommand.cs
    - src/Onboarding.API/Controllers/AdminUserController.cs
    - frontend/backoffice/src/lib/admin-api.ts
    - frontend/backoffice/src/components/molecules/DeleteDialog.tsx
    - frontend/backoffice/src/components/pages/AdminUsersPage.tsx
    - frontend/backoffice/src/components/pages/AdminUserDetailPage.tsx
decisions:
  - "CR-01: Used RandomNumberGenerator.GetInt32() instead of Create().GetBytes() — eliminates both resource leak and modulo bias"
  - "CR-02: Changed DeleteDialog.onDelete signature from () => Promise<void> to (confirmEmail: string) => Promise<void> — callers updated"
  - "CR-03: Used Ok(result) instead of CreatedAtAction — no GET by ID endpoint exists for individual admins"
metrics:
  duration: "~10 min"
  completed_date: "2026-04-16"
  tasks_completed: 3
  files_changed: 6
requirements:
  - ADM-01
  - ADM-02
  - ADM-03
  - ADM-04
---

# Phase 30 Plan 05: Critical Code Review Issues — Gap Closure Summary

**One-liner:** Fixed 3 critical issues from 30-REVIEW.md: RNG resource leak, deleteUser body mismatch, and invalid Location header.

## What Was Fixed

### CR-01: RandomNumberGenerator Resource Leak + Modulo Bias

**File:** `src/Onboarding.Application/Admin/Commands/CreateAdminCommand.cs`

**Problem:** `RandomNumberGenerator.Create()` called inside loops without disposal — leaks OS entropy handles. Modulo bias in Fisher-Yates shuffle.

**Fix:** Replaced with `RandomNumberGenerator.GetInt32()` (static, no allocation, rejection sampling for bias-free distribution):
- `GetRandomChar(charSet)` → `charSet[RandomNumberGenerator.GetInt32(charSet.Length)]`
- Fisher-Yates: `RandomNumberGenerator.GetInt32(i + 1)` instead of `randomBytes[0] % (i + 1)`

### CR-02: deleteUser() Frontend Body Mismatch

**Files:** `admin-api.ts`, `DeleteDialog.tsx`, `AdminUsersPage.tsx`, `AdminUserDetailPage.tsx`

**Problem:** Backend DELETE endpoint requires `{ confirmEmail }` body for LGPD compliance. Frontend sent no body — always got 400.

**Fix:**
- `deleteUser(userId)` → `deleteUser(userId, confirmEmail: string)` with `body: JSON.stringify({ confirmEmail })`
- `DeleteDialog.onDelete` signature changed to `(confirmEmail: string) => Promise<void>`
- Dialog passes `emailInput.trim()` to onDelete callback
- Both callers (AdminUsersPage, AdminUserDetailPage) updated

### CR-03: Invalid Location Header in CreateAdmin

**File:** `src/Onboarding.API/Controllers/AdminUserController.cs`

**Problem:** `CreatedAtAction(nameof(CreateAdmin), ...)` produced Location header pointing to POST endpoint — RFC 9110 violation. Any client following Location would get 405.

**Fix:** `return Ok(result)` instead of `CreatedAtAction(...)` — appropriate when no individual resource URI exists.

## Verification Results

| Check | Result |
|-------|--------|
| `RandomNumberGenerator.Create` removed from CreateAdminCommand.cs | PASS |
| `RandomNumberGenerator.GetInt32` used in GetRandomChar + Fisher-Yates | PASS |
| `deleteUser()` accepts confirmEmail parameter | PASS |
| `JSON.stringify({ confirmEmail })` in deleteUser body | PASS |
| DeleteDialog passes email to onDelete callback | PASS |
| `CreatedAtAction` removed from AdminUserController | PASS |
| `dotnet build src/Onboarding.API` | PASS |
| `dotnet test tests/Onboarding.API.Tests` — 59/59 passing | PASS |
| `npx vitest run` — 22/22 passing | PASS |
| `npx tsc --noEmit` | PASS |

## Deviations from Plan

None. All 3 tasks executed exactly as planned.

## Known Stubs

None.

## Threat Flags

No new security surfaces. CR-01 fix improves security (proper RNG usage). CR-02 fix enforces LGPD compliance. CR-03 fix is RFC compliance only.

## Self-Check: PASSED

- Commit 71ee349 — EXISTS
- 6 files modified, 1 plan file created
- All acceptance criteria met
- 30-VERIFICATION.md re-verification should maintain 9/9 score
