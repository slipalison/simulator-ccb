---
phase: 40-client-frontend-pj-registration
reviewed: 2026-04-26T12:00:00Z
depth: quick
files_reviewed: 23
files_reviewed_list:
  - frontend/client/src/lib/auth-context.tsx
  - frontend/client/src/lib/types.ts
  - frontend/client/src/lib/api.ts
  - frontend/client/src/lib/validation-schemas.ts
  - frontend/client/src/components/atoms/ProfileBadge.tsx
  - frontend/client/src/components/organisms/Header.tsx
  - frontend/client/src/components/organisms/Sidebar.tsx
  - frontend/client/src/components/templates/AppLayout.tsx
  - frontend/client/src/router.tsx
  - frontend/client/src/components/pages/RegisterPage.tsx
  - frontend/client/src/components/pages/DashboardPage.tsx
  - frontend/client/src/components/pages/EmployeesPage.tsx
  - frontend/client/src/components/pages/ProfilePage.tsx
  - frontend/client/src/components/molecules/RegistrationForm.tsx
  - frontend/client/src/components/molecules/TermsDialog.tsx
  - frontend/client/src/components/molecules/DashboardCards.tsx
  - frontend/client/src/components/molecules/EmployeesTable.tsx
  - frontend/client/src/components/molecules/EmployeeActionsDropdown.tsx
  - frontend/client/src/components/molecules/EmployeeSearchBar.tsx
  - frontend/client/src/components/molecules/EditEmployeeDialog.tsx
  - frontend/client/src/components/molecules/BlockUnblockDialog.tsx
  - frontend/client/src/components/molecules/ResetPasswordDialog.tsx
  - frontend/client/src/components/molecules/DeleteEmployeeDialog.tsx
  - frontend/client/src/components/molecules/ChangeAccessGroupDialog.tsx
  - frontend/client/src/components/ui/checkbox.tsx
findings:
  critical: 1
  warning: 5
  info: 4
  total: 10
status: issues_found
---

# Phase 40: Code Review Report

**Reviewed:** 2026-04-26T12:00:00Z
**Depth:** quick (pattern-matching + targeted file reads)
**Files Reviewed:** 23
**Status:** issues_found

## Summary

Reviewed 23 source files for Phase 40 (client-frontend-pj-registration). Found 1 critical security bug, 5 warnings, and 4 info items. The critical issue is a phone validation regex mismatch that will reject valid masked phone input. Additionally, there are multiple `as any` type casts that bypass TanStack Router's type safety, and one debug `console.error` left in production code. PF/PJ remnants check is clean — no PessoaFisica, PersonTypeRadio, or pfRegistration references found in source files. No hardcoded secrets, no `innerHTML`/`dangerouslySetInnerHTML`, no `eval()` usage, and auth tokens are correctly handled via httpOnly cookies (not localStorage).

## Critical Issues

### CR-01: Phone validation regex rejects masked phone input from RegistrationForm

**File:** `frontend/client/src/lib/validation-schemas.ts:114`
**Issue:** The phone validation regex `^\+?\d{10,11}$` only accepts raw digits (e.g., `11987654321`), but the `RegistrationForm` component's phone field (line 305-308) applies a mask via `applyPhoneMask()` which formats input as `(XX) XXXXX-XXXX`. The Zod schema validates the raw `field.value`, which after `applyPhoneMask` contains parentheses, spaces, and hyphens — not just digits. This means the regex will **always fail** for any phone number entered in the registration form, making step 2 submission impossible.

The same regex is used in `editEmployeeSchema` (line 141), but the `EditEmployeeDialog` uses raw `register("phone")` without a mask, so it will accept raw digits but reject any formatted input.

**Fix:** In `validation-schemas.ts`, change the phone regex to strip non-digits before validating length, or adjust the regex to accept the masked format:

```typescript
// Option A: Strip mask before validation (recommended)
phone: z
  .string()
  .min(1, "Telefone é obrigatório")
  .refine(
    (val) => /^\+?\d{10,11}$/.test(val.replace(/\D/g, "")),
    "Telefone deve conter 10 ou 11 dígitos"
  ),

// Option B: Accept masked format in regex
phone: z
  .string()
  .min(1, "Telefone é obrigatório")
  .regex(
    /^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$/,
    "Telefone deve conter 10 ou 11 dígitos"
  ),
```

Also ensure `RegistrationForm` stores the **raw digits** in the form value (not the masked display), with the mask applied only for display via a separate state or `formatValue` prop. Currently `applyPhoneMask` result is passed to `field.onChange`, storing the masked string in form state.

## Warnings

### WR-01: Unsafe `as any` type casts bypass TanStack Router type safety (5 locations)

**File:** `frontend/client/src/router.tsx:54,144`
**File:** `frontend/client/src/components/organisms/Sidebar.tsx:64,69`
**File:** `frontend/client/src/components/pages/DashboardPage.tsx:45`
**Issue:** Multiple uses of `as any` to cast route paths and navigate targets. The `profileRoute` definition (router.tsx:54) uses `as any` on the entire route config, which suppresses type checking on route params. `navigate({ to: defaultRoute as any })` and `matchRoute({ to: item.href as any })` bypass TanStack Router's type-safe routing, allowing navigation to non-existent routes without compile-time errors.

**Fix:** Register all route paths in TanStack Router's type system. Replace `as any` casts with proper typed route references. For `profileRoute`, remove the `as any` and fix any type mismatch (likely missing search params type). For dynamic group-based routes, use a typed route map:

```typescript
const ROUTE_MAP: Record<string, string> = {
  "admin-empresa": "/employees",
  viewer: "/employees",
  dashboard: "/dashboard",
};
// Then navigate({ to: ROUTE_MAP[group] as typeof ROUTE_MAP[keyof typeof ROUTE_MAP] })
```

### WR-02: React rendering side effects — `fieldErrors` processed during render in RegistrationForm

**File:** `frontend/client/src/components/molecules/RegistrationForm.tsx:106-123`
**Issue:** The `fieldErrors` state is processed with `step2Form.setError()` / `step1Form.setError()` calls directly in the component body (outside `useEffect` or event handler). This triggers React state updates during render, which is not allowed and will cause React warnings or unpredictable behavior. The `setFieldErrors(null)` call on line 122 also mutates state during render.

**Fix:** Move the field error mapping into a `useEffect`:

```typescript
useEffect(() => {
  if (fieldErrors) {
    Object.entries(fieldErrors).forEach(([field, messages]) => {
      const step2Fields = ["email", "phone", "password", "confirmPassword", "termsAccepted"];
      if (step2Fields.includes(field)) {
        step2Form.setError(field as keyof CompanyAccessData, {
          type: "server",
          message: messages[0],
        });
      } else {
        step1Form.setError(field as keyof CompanyData, {
          type: "server",
          message: messages[0],
        });
      }
    });
    setFieldErrors(null);
  }
}, [fieldErrors, step1Form, step2Form]);
```

### WR-03: `registerCompany` API call does not include `credentials: "include"` — cookie auth won't be sent

**File:** `frontend/client/src/lib/api.ts:53`
**Issue:** The `registerCompany()` function makes a POST to `/api/companies/registration` without `credentials: "include"`. All other API functions (`getProfileClient`, `getEmployees`, `toggleEmployeeStatus`, etc.) correctly include `credentials: "include"`. While registration is pre-auth and doesn't require cookies, the comment at line 4 explicitly states "All requests MUST include credentials: 'include'". If the backend ever uses CSRF cookies or session affinity on this endpoint, the request would fail.

**Fix:** Add `credentials: "include"` to the `registerCompany` fetch call for consistency:

```typescript
const response = await fetch("/api/companies/registration", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  credentials: "include",
  body: JSON.stringify(data),
});
```

### WR-04: `forgotPasswordClient` and `resetPasswordClient` missing `credentials: "include"`

**File:** `frontend/client/src/lib/api.ts:345,372`
**Issue:** Same as WR-03 — the `forgotPasswordClient` and `resetPasswordClient` functions do not include `credentials: "include"`. While these routes are likely pre-auth, the project convention (line 4 comment) states all requests must include it. This could cause issues with CSRF protection or session affinity.

**Fix:** Add `credentials: "include"` to both fetch calls.

### WR-05: CNPJ mask stripped on blur but stored raw — validation schema expects digits but form displays mask inconsistently

**File:** `frontend/client/src/components/molecules/RegistrationForm.tsx:235-246`
**Issue:** The CNPJ field stores the **raw digits** (via `stripCnpjMask` on line 238) in form state, and on blur, it re-applies the mask (line 242) then strips it again (line 243). This means the user sees the raw 14-digit number briefly during input, then it becomes masked on blur, but form state always holds raw digits. However, the `onBlur` handler sets `field.onChange(stripCnpjMask(masked))`, which is redundant when input was already stripped on `onChange`. The initial render shows raw digits (no mask applied on mount), which is inconsistent UX.

**Fix:** Apply the mask for display on every input change, and store raw digits in form state. Use a controlled pattern that separates display value from form value:

```typescript
// On change: display masked, store raw
onChange={(e) => {
  const raw = stripCnpjMask(e.target.value);
  field.onChange(raw);
}}
// Render: always display masked version
value={applyCnpjMask(field.value)}
```

## Info

### IN-01: `console.error` left in production code

**File:** `frontend/client/src/components/pages/ProfilePage.tsx:27`
**Issue:** `console.error("Failed to fetch profile:", err)` left in production code.
**Fix:** Replace with structured error logging or remove. In production, use the project's Serilog/OTEL pipeline.

### IN-02: `termsAccepted` default value uses unsafe type cast

**File:** `frontend/client/src/components/molecules/RegistrationForm.tsx:97`
**Issue:** `termsAccepted: undefined as unknown as true` — This cast tricks TypeScript into accepting `undefined` as `true`. The Zod schema validates `z.literal(true)`, so the form will fail validation until the checkbox is checked, but the cast hides the real type from TypeScript.
**Fix:** Use a proper default: `termsAccepted: false as boolean as true` or restructure the Zod schema to use `z.boolean()` with `.refine(val => val === true, ...)` instead of `z.literal(true)`.

### IN-01: `companyId` passed but unused in dialog components

**File:** `frontend/client/src/components/molecules/EditEmployeeDialog.tsx:35` and `frontend/client/src/components/molecules/DeleteEmployeeDialog.tsx:32` and `frontend/client/src/components/molecules/ChangeAccessGroupDialog.tsx:54`
**Issue:** `companyId` is passed as a prop to `EditEmployeeDialog`, `DeleteEmployeeDialog`, and `ChangeAccessGroupDialog` but aliased as `_companyId` and never used. The parent `EmployeesPage` already handles all API calls and passes `companyId` to the API functions directly, so the prop is dead code.
**Fix:** Remove the unused `companyId` prop from these three dialog component interfaces.

### IN-04: `ResetPasswordDialog` rendered even when no reset action is active

**File:** `frontend/client/src/components/pages/EmployeesPage.tsx:300-304`
**Issue:** `ResetPasswordDialog` is rendered outside the `{dialog.type === "reset-password" && ...}` conditional guard, unlike all other dialogs. It's always in the DOM with `open={false}` when not active. This works functionally (the Dialog component handles visibility via `open` prop), but is inconsistent with the other dialog rendering patterns and means the DOM always includes the dialog mount point.
**Fix:** For consistency, wrap in the same conditional pattern:

```typescript
{dialog.type === "reset-password" && (
  <ResetPasswordDialog
    open={true}
    temporaryPassword={dialog.temporaryPassword}
    onClose={handleCloseDialog}
  />
)}
```

---

_Reviewed: 2026-04-26T12:00:00Z_
_Reviewer: gsd-code-reviewer_
_Depth: quick_