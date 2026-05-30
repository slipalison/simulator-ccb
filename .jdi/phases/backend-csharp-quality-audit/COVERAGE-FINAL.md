# Coverage Final — Phase 54 (backend-csharp-quality-audit)

Measured 2026-05-30 via **merged** measurement (coverlet on all 5 suites + ReportGenerator union — D-57; per-project measurement undercounts because Integration.Tests credit cross-assembly coverage). Migrations (EF-generated) excluded per D-49.

## Before → After (per assembly, %line)

| Assembly | Baseline (merged) | Final (merged) | Δ |
|---|---|---|---|
| Onboarding.Domain | 98.0% | **98.0%** | — |
| Onboarding.Application | 96.7% | **98.2%** | +1.5 |
| Onboarding.API | 86.8% | **95.3%** | +8.5 |
| Onboarding.Infrastructure | 79.0% | **98.2%** | +19.2 |
| **Total src** | **91.1%** | **97.4%** | **+6.3** |

**Per-file D-49 gate (every authorial `.cs` > 80% line): MET — 0 files below 80%.**

> Note: the T-1 per-project baseline reported "Application 45.58% / 93 files at 0%". That was a measurement artifact (Application.Tests run in isolation, ignoring Integration.Tests' full-stack handler coverage). The true merged baseline was 91.1% — the ~250-test estimate was never real (D-57).

## Full test suite (final)

| Suite | Pass | Fail | Skip |
|---|---|---|---|
| Onboarding.Domain.Tests | 513 | 0 | 0 |
| Onboarding.Application.Tests | 222 | 0 | 0 |
| Onboarding.API.Tests | 504 | 0 | 4 (pre-existing) |
| Onboarding.Infrastructure.Tests (new) | 200 | 0 | 0 |
| Onboarding.Integration.Tests | 248 | 0 | 0 |
| **Total** | **1687** | **0** | **4** |

Baseline suite at phase start ≈ 1204. Net new tests this phase ≈ **+483** (Domain +32, API +118, Application +72, Infrastructure.Tests +200, Integration +53 incl. search-bug regression).

## Tests added per coverage task

- W4-1 Domain: +32 (exception ctors, Entity base) → Domain 98.08%.
- W4-2 API: +53 (IdempotencyFilter, claims transforms, SecurityHeaders).
- W4-A API: +65 (CompaniesController + AccessGroup/Employee endpoints + request DTOs).
- W4-B Application: +72 (DTOs + RegisterEmployeeCommandValidator).
- W4-C Infrastructure: +200 InMemory (21 repos, `[ExcludeFromCodeCoverage]` removed — D-56) + Keycloak service mock tests.
- W4-D Integration: +31 (search paths for the 4 sites fixed by the FromSql bug fix).

## Methodology

- Coverlet: `coverlet.msbuild` (Domain/API/Infrastructure.Tests/Integration) + `coverlet.collector` XPlat (Application).
- Merge: `reportgenerator -assemblyfilters:+Onboarding.* -filefilters:-**/Migrations/**;-**/obj/**`.
- Generated code excluded: EF Migrations, `OpenApiXmlCommentSupport.generated.cs` (D-49 generated-code carve-out).
- `[ExcludeFromCodeCoverage]` removed from the 21 EF repos (D-56) — they are now counted and covered (InMemory + Testcontainers Integration).
