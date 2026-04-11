# Plan 21-03 — Summary

**Executed:** 2026-04-11
**Status:** ✅ COMPLETE

## Changes Made

1. **Coverage baseline verification:**
   - Current aggregate coverage: **31.11% line** (Domain tests only, without API/Integration tests)
   - Domain tests: 81.86% line coverage ✅
   - Application layer: 16.48% line coverage
   - API + Integration tests require PostgreSQL to run — will execute in CI with service container
   - **Risk:** 80% threshold may fail on first CI run if API tests don't contribute enough coverage
   - **Decision:** Keep 80% threshold — CI will reveal actual coverage after first run. Adjust if needed.

2. **Route tree verification:**
   - Both frontend projects use **manual route definitions** (no `routeTree.gen.ts`)
   - `tsc --noEmit` works without build prerequisite — no reordering needed
   - Task 3 (reorder frontend steps) was **skipped**

3. **Backend test step env vars:**
   - Added `CI: true` and `TESTCONTAINERS_CONNECT_TIMEOUT: "120"` to test step

4. **Cache key validation:**
   - Backend: `hashFiles('**/*.csproj')` → matches all 7 .csproj files ✅
   - Frontend client: `hashFiles('frontend/client/package-lock.json')` → file exists ✅
   - Frontend backoffice: `hashFiles('frontend/backoffice/package-lock.json')` → file exists ✅

5. **Independent job failure behavior:**
   - All 3 jobs have **no `needs:`** relationship → parallel execution ✅
   - Each job is an independent quality gate
   - Failure in one job does not block others

6. **YAML validation:**
   - Validated with Python `yaml.safe_load` ✅
   - `actionlint` not available on Windows — validated via Python fallback

7. **Full local validation:**
   - Backend: restore ✅, build ✅, tests require DB (deferred to CI)
   - Frontend client: npm ci ✅, tsc ✅, eslint ✅, build ✅
   - Frontend backoffice: npm ci ✅, tsc ✅, eslint ✅, build ✅

## Files Changed

| File | Action |
|------|--------|
| `.github/workflows/ci.yml` | Edited — added TESTCONTAINERS_CONNECT_TIMEOUT, NODE_ENV on frontend builds |

## CI Pipeline Summary (All 3 Jobs)

```
┌─────────────────┐  ┌────────────────────┐  ┌────────────────────────┐
│  Backend (.NET) │  │ Frontend Client    │  │ Frontend Backoffice    │
│  PostgreSQL svc │  │ Node.js 22         │  │ Node.js 22             │
│  coverlet 80%   │  │ tsc + eslint + bld │  │ tsc + eslint + build   │
└─────────────────┘  └────────────────────┘  └────────────────────────┘
       │                      │                       │
       └──────────────────────┴───────────────────────┘
                              │
                    All run in parallel
                    Independent failure
```

## Risk Notes

- **Coverage threshold (80%):** Current local coverage is 31% (Domain only). API tests + Integration tests will run in CI with PostgreSQL service container. If aggregate < 80%, threshold will need adjustment or more tests added.
- **Testcontainers timeout:** `TESTCONTAINERS_CONNECT_TIMEOUT=120` should handle slow CI VMs.
- **Frontend chunk size:** Both projects produce > 500KB bundles — code splitting recommended.
