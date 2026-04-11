# Plan 21-01 — Summary

**Executed:** 2026-04-11
**Status:** ✅ COMPLETE

## Changes Made

1. **`.github/workflows/ci.yml`** — Created with full backend job:
   - Triggers: push to main, PRs to main, workflow_dispatch
   - .NET 10 SDK setup (10.0.x)
   - NuGet caching with `actions/cache@v5`
   - PostgreSQL 16 Alpine service container (for API tests requiring DB)
   - Build with `--configuration Release`
   - Test with coverlet.msbuild: 80% line coverage threshold (total aggregate)
   - Cobertura output format

2. **Coverlet migration** — All 3 test projects already had `coverlet.msbuild` v6.0.3 (no changes needed):
   - `Onboarding.Domain.Tests` ✅
   - `Onboarding.API.Tests` ✅
   - `Onboarding.Integration.Tests` ✅

## Validation Results

- **Build:** ✅ Success (Release mode, 15 non-critical warnings)
- **YAML syntax:** ✅ Valid (validated with Python yaml.safe_load)
- **Domain tests:** ✅ 73/73 passing, 81.86% line coverage
- **API tests:** ❌ Require PostgreSQL (service container in CI will provide)
- **Integration tests:** ❌ Require Docker/Testcontainers (available in GitHub Actions runners)
- **Overall coverage:** 31.11% locally (Domain tests only) — 80% threshold expected to pass in CI when all tests run

## Known Issues / Concerns

- **Program.cs line 214** (`db.Database.Migrate()`) runs before test service replacement — API tests need a PostgreSQL instance on localhost:5432. Resolved in CI via service container.
- **Coverlet threshold** at 80% may need adjustment after first CI run if combined coverage is below target.
- **Integration tests** use deprecated Testcontainers constructors (CS0618 warnings) — should migrate to image-based constructors in a future task.

## Files Changed

| File | Action |
|------|--------|
| `.github/workflows/ci.yml` | Created |
| `tests/Onboarding.Domain.Tests/*.csproj` | Already migrated (no change) |
| `tests/Onboarding.API.Tests/*.csproj` | Already migrated (no change) |
| `tests/Onboarding.Integration.Tests/*.csproj` | Already migrated (no change) |
