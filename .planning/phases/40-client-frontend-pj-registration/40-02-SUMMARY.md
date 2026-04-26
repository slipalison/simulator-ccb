---
phase: 40-client-frontend-pj-registration
plan: 02
subsystem: frontend-client
tags: [registration, wizard, cnpj-validation, password-ux, terms-acceptance, pj-only]
dependency_graph:
  requires:
    - phase: 39-keycloak-groups-permissions
      provides: Keycloak groups, permission policies, CompaniesController endpoints
    - plan: 40-01
      provides: auth-context with accessGroup/companyId, PJ-only validation schemas, sidebar, router
  provides:
    - RegistrationForm 2-step wizard (company data + access data + terms)
    - TermsDialog modal with mock LGPD terms text
    - RegisterPage full-page wrapper with branding
    - CNPJ client-side mask and modulo-11 validation
    - Password UX with 5-level strength meter
    - Mandatory terms acceptance checkbox
    - shadcn/ui Dialog and Checkbox components
  affects: [40-03, 40-04]
tech_stack:
  added: ["@radix-ui/react-dialog", "@radix-ui/react-checkbox"]
  patterns: [2-step wizard form, CNPJ modulo-11 validation, phone mask, terms modal]
key_files:
  created:
    - frontend/client/src/components/ui/dialog.tsx
    - frontend/client/src/components/ui/checkbox.tsx
    - frontend/client/src/components/molecules/TermsDialog.tsx
    - frontend/client/src/components/pages/RegisterPage.tsx
  modified:
    - frontend/client/src/components/molecules/RegistrationForm.tsx
    - frontend/client/src/router.tsx
  deleted: []
key_decisions:
  - "RegistrationForm completely rewritten as 2-step wizard (company data -> access data + terms)"
  - "CNPJ input stores raw digits, displays masked on blur (XX.XXX.XXX/XXXX-XX format)"
  - "Phone input applies (XX) XXXXX-XXXX mask on input"
  - "Registration success redirects to / which triggers ACF login flow (not auto-login)"
  - "TermsDialog uses shadcn/ui Dialog with mock LGPD terms text (version 1.0)"
  - "RegisterPage provides full-page layout with logo, branding, and login link — RegistrationForm is just the Card content"
  - "Error messages are Portuguese: CNPJ duplicado, serviço indisponível"
requirements_completed: [REG-01, REG-05]
metrics:
  duration: 6min
  completed: 2026-04-26T18:28:52Z
---

# Phase 40 Plan 02: PJ Registration Wizard — Summary

**2-step PJ registration wizard with CNPJ validation, password UX, and mandatory terms acceptance replacing PF/PJ form**

## Performance

- **Duration:** 6 min
- **Started:** 2026-04-26T18:22:40Z
- **Completed:** 2026-04-26T18:28:52Z
- **Tasks:** 2
- **Files modified:** 2, created 4

## Accomplishments

1. **TermsDialog component** — Modal dialog with mock LGPD terms text (v1.0), scrollable content area, and "Li e concordo com os Termos de Uso" button
2. **RegisterPage component** — Full-page wrapper with Building2 logo, "Cadastro para Pessoa Jurídica" subtitle, centered RegistrationForm, and "Fazer login" link
3. **RegistrationForm rewritten as 2-step wizard** — Step 1 (Dados da Empresa: razão social + CNPJ with mask/modulo-11), Step 2 (Dados de Acesso: email + phone + password UX + terms checkbox)
4. **CNPJ mask/validation** — Input stores raw digits, displays `XX.XXX.XXX/XXXX-XX` on blur, validates with modulo-11 algorithm on submit
5. **Phone mask** — Applies `(XX) XXXXX-XXXX` format on input
6. **Password UX** — 5-level strength meter, show/hide toggle, confirm password with mismatch validation, "As senhas coincidem" indicator
7. **Terms acceptance** — Required checkbox (z.literal(true)) with "Termos de Uso" link opening TermsDialog modal
8. **Step indicator** — Visual progress bar (2 dots) showing current step
9. **Error handling** — 409 → "CNPJ já cadastrado", 422 → field errors mapped to RHF, 503 → "Serviço temporariamente indisponível"
10. **Registration redirect** — POST → 201 → `window.location.href = "/"` for ACF login flow
11. **shadcn/ui Dialog + Checkbox** — Added @radix-ui/react-dialog and @radix-ui/react-checkbox, created standard shadcn components
12. **Router updated** — /register now points to RegisterPage component

## Task Commits

1. **Task 1: TermsDialog + RegisterPage** — `71daafd` (feat) — TermsDialog modal, RegisterPage wrapper, shadcn Dialog/Checkbox components, router updated
2. **Task 2: RegistrationForm wizard** — `03e1e89` (feat) — 2-step wizard with CNPJ validation, password UX, terms checkbox, error handling

## Files Created

- `frontend/client/src/components/ui/dialog.tsx` — Standard shadcn/ui Dialog (Root, Trigger, Content, Header, Footer, Title, Description)
- `frontend/client/src/components/ui/checkbox.tsx` — Standard shadcn/ui Checkbox with Radix primitive
- `frontend/client/src/components/molecules/TermsDialog.tsx` — Modal with LGPD mock terms, "Li e concordo" button
- `frontend/client/src/components/pages/RegisterPage.tsx` — Full-page wrapper with logo, branding, form, login link

## Files Modified

- `frontend/client/src/components/molecules/RegistrationForm.tsx` — Complete rewrite: single form → 2-step wizard; removed ThemeToggle/Card outer layout (moved to RegisterPage); added CNPJ mask, phone mask, TermsDialog integration, step navigation
- `frontend/client/src/router.tsx` — Changed /register component from RegistrationForm to RegisterPage

## Decisions Made

- RegistrationForm no longer includes full-page layout (ThemeToggle, min-h-screen) — RegisterPage provides that wrapper
- CNPJ mask applied on blur for display; raw digits stored and sent to API
- Phone mask applied on input for immediate visual feedback
- Used `useForm` as two separate forms (step1Form, step2Form) rather than one combined form — cleaner validation per step
- Registration success uses `window.location.href = "/"` instead of `login()` — simpler redirect that triggers ACF flow
- Error messages in Portuguese for user-facing strings
- Step progress indicator uses simple colored dots + bar

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

| File | Stub | Reason |
|------|------|--------|
| `TermsDialog.tsx` | Mock LGPD terms text | Real terms will come from legal/compliance — placeholder is sufficient for v1 |

## Threat Flags

No new threat surface beyond what was in the plan's threat model. All mitigations implemented:
- T-40-05 (CNPJ Tampering): Client-side modulo-11 validation + server-side FluentValidation defense in depth ✅
- T-40-06 (Terms Repudiation): termsAccepted boolean + termsVersion "1.0" sent to backend ✅
- T-40-07 (PF Information Disclosure): All PF references removed, no PF data in form/API ✅

## Self-Check: PASSED

- All 4 created files exist on disk: ✅
- Both commit hashes found in git log: ✅ (`71daafd`, `03e1e89`)
- TypeScript compiles without errors: ✅ (`npx tsc --noEmit` — 0 errors)
- No PF references in client source: ✅ (`PersonTypeRadio`, `pfRegistration`, `PessoaFisica`, `personType` — none found)
- Terms checkbox uses z.literal(true): ✅ (validation-schemas.ts line 117)
- CNPJ validated with modulo-11: ✅ (validateCnpj imported and used)
- Registration redirects to / on 201: ✅ (window.location.href = "/")

---
*Phase: 40-client-frontend-pj-registration*
*Completed: 2026-04-26*