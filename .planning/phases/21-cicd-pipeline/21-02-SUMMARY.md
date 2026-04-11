# Plan 21-02 — Summary

**Executed:** 2026-04-11
**Status:** ✅ COMPLETE

## Changes Made

1. **ESLint verification** — Both projects already had ESLint 9.x + plugins installed:
   - `frontend/client`: eslint@9.39.4, typescript-eslint, react-hooks, react-refresh ✅
   - `frontend/backoffice`: same packages ✅
   - Both had `eslint.config.mjs`, `lint` and `typecheck` scripts in package.json ✅

2. **ESLint error fixes:**
   - `frontend/client/src/lib/api.ts`: Removed unused import `PfRegistrationData, PjRegistrationData`
   - `frontend/backoffice/src/lib/admin-auth-context.tsx`: Changed `let adminSession` → `const _adminSession` (prefer-const + unused prefix)

3. **`.github/workflows/ci.yml`** — Added 2 independent frontend jobs:
   - `frontend-client`: checkout → Node.js 22 → cache npm → npm ci → tsc --noEmit → eslint → vinxi build
   - `frontend-backoffice`: same pipeline, independent execution
   - Both jobs run in parallel (no `needs:` relationship)
   - `defaults.run.working-directory` set per project
   - Cache keyed on per-project `package-lock.json`
   - `NODE_ENV: production` set on build steps

## Validation Results

- **tsc --noEmit (client):** ✅ Pass, 0 errors
- **tsc --noEmit (backoffice):** ✅ Pass, 0 errors
- **ESLint (client):** ✅ Pass after fixing 2 unused import errors
- **ESLint (backoffice):** ✅ Pass after fixing prefer-const error
- **vinxi build (client):** ✅ Success, `.output/` generated (565KB JS bundle)
- **vinxi build (backoffice):** ✅ Success, `.output/` generated (584KB JS bundle)

## Route Tree Discovery

Both projects use **manual route definitions** (`createRootRoute`, `addChildren`) in `router.tsx`. No `routeTree.gen.ts` file is generated — routes are defined imperatively. This means `tsc --noEmit` works without any build step prerequisite. Task 3 of Plan 21-03 (reorder steps) was **skipped** — not needed.

## Files Changed

| File | Action |
|------|--------|
| `.github/workflows/ci.yml` | Edited — added frontend-client + frontend-backoffice jobs |
| `frontend/client/src/lib/api.ts` | Fixed — removed unused type imports |
| `frontend/backoffice/src/lib/admin-auth-context.tsx` | Fixed — `let` → `const _` for unused variable |

## Notes

- No ESLint installation was needed — packages were already present
- Node.js 24.14.0 available locally (plan specifies 22 for CI — compatible)
- Both Vinxi builds produced chunks > 500KB — code splitting recommended as future optimization
