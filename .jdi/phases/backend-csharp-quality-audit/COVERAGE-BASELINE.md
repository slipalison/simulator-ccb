# Coverage Baseline — Phase 54 (backend-csharp-quality-audit)

> **⚠️ CORRIGIDO 2026-05-30 (D-57):** Os números por-projeto abaixo SUBCONTAM. Medição **merged** das 4 suites (incl. Integration.Tests full-stack) = **91.1% line** real (Domain 98 / Application **96.7** / API 86.8 / Infrastructure 79), só 710 linhas descobertas. O "Application 45.58% / 93 arquivos a 0%" é artefato de medir Application.Tests isolado — Integration.Tests cobrem esses handlers via HTTP. Gate D-49 = por-arquivo >80%. Ver COVERAGE-FINAL.md (T-8) pros números autoritativos.

Measured: 2026-05-30. Tool: coverlet.msbuild (Domain, API) + coverlet.collector XPlat (Application). Infrastructure: cached iter6 XML (integration tests require Docker; noted below).

## Summary table (layer × %line × %branch)

| Layer | Files (src, excl. Migrations) | %Line | %Branch | Method% | Status |
|---|---|---|---|---|---|
| Onboarding.Domain | 36 authorial files | **95.95%** | 86.42% | 93.01% | PASS |
| Onboarding.Application | 178 authorial files | **45.58%** | 29.62% | — | FAIL — 93 files below 80% |
| Onboarding.Infrastructure | ~21 authorial files (repos all `[ExcludeFromCodeCoverage]`) | **94.86%** | 8.33% | — | NOTE: all repos exempt; non-exempt files mostly OK |
| Onboarding.API | 23 files | **70.28%** | 43.55% | 78.54% | FAIL — 5 files below 80% |
| **Combined (Domain.Tests run)** | | **95.95%** | 86.42% | | |
| **Application.Tests run** | | **48.08%** (incl. Domain) | 37.98% | | |
| **API.Tests run** | | **70.28%** | 43.55% | | |
| **Integration.Tests (Docker, iter6)** | All layers | **78.68%** | 29.27% | | Cached, Docker not available |

## Notes on measurement methodology

- **Domain**: `coverlet.msbuild` with `Include=[Onboarding.Domain]*`. 481 tests, all pass. Reliable per-file data.
- **Application**: `coverlet.collector` (XPlat). Covers Application+Domain together. 150 tests, all pass. Per-file data from collector XML.
- **Infrastructure**: All repositories carry `[ExcludeFromCodeCoverage]` — they are tested exclusively via Integration.Tests (Testcontainers/Docker). The 94.86% Infrastructure line-rate in the iter6 integration run confirms the EF configurations and non-repo classes are well-covered. Repositories are explicitly opted out by prior decision; this is consistent with D-49's intent (autoral code) but note: D-49 as locked says "no exclusions". This is a **conflict requiring orchestrator decision** (see note below).
- **Integration.Tests**: Docker/Testcontainers not available in this environment. Iter6 XML used as proxy (run from prior CI iteration). It is representative but not freshly measured this iteration.
- **Migrations**: Excluded per D-49 assumption (EF-generated code, not autoral). Confirmed: migration files are code-generated scaffolding, not domain logic.

## Domain — files below 80% line coverage

| File | %Line | Note |
|---|---|---|
| `Exceptions\RegistrationFailedException.cs` | 0.0% | Exception subclass, no constructor test |
| `Exceptions\DuplicateEntityException.cs` | 41.7% | Partially covered; message constructor untested |
| `Common\Entity.cs` | 50.0% | Base class, some abstract members |
| `Exceptions\DomainException.cs` | 50.0% | Abstract base — only one ctor path tested |
| `Exceptions\DuplicateCompanyException.cs` | 50.0% | Only one ctor tested |
| `Exceptions\DuplicateKeycloakUserException.cs` | 50.0% | Only one ctor tested |

**Domain files at exactly 80%:** `Exceptions\InvalidStateTransitionException.cs` (80.0%)

## Application — files below 80% line coverage (93 files at 0%)

The uncovered Application files fall into clear groups:

### Group A: Admin module — 0% (entirely untested)
All Admin/* handlers, commands, queries, validators, DTOs:
- `Admin\Commands\*` (9 files, ~700 LoC total)
- `Admin\Queries\*` (8 files, ~400 LoC total)
- `Admin\DTOs\*` (2 files)
- `Admin\Validators\*` (2 files)

### Group B: Companies module — 0% (entirely untested)
- `Companies\Commands\*` (14 files — handlers, validators, commands, DTOs, ~850 LoC total)
- `Companies\Queries\*` (2 files)
- `Companies\DTOs\*` (4 files)

### Group C: Auth module — 0% (entirely untested)
- `Auth\Commands\*` (4 commands, ~200 LoC total)
- `Auth\DTOs\*` (2 files)
- `Auth\Validators\*` (4 validators)

### Group D: Common interfaces/DTOs — 0%
- `Common\BadRequestException.cs`, `Common\IAuditService.cs`, `Common\IKeycloakUserService.cs`, `Common\Unit.cs`

### Group E: Fundos validators — 0%
- 8 validator files for Fundos commands that were not covered in Application.Tests:
  `RegisterCedentePjCommandValidator`, `RegisterCustodianteCommandValidator`, `TransitionFundoStatusCommandValidator`, `UpdateCedenteCommandValidator`, `UpdateConsultoriaFundoCommandValidator`, `UpdateCustodianteCommandValidator`, `UpdateFundoCommandValidator`, `UpdateTipoAtivoCommandValidator`

### Group F: Fundos DTOs — partially covered (50–70%)
- `FundoCedenteDto` 0%, `CedenteDto` 50%, `FundoDto` 50%, `RelCedenteTipoAtivoDto` 50%, `ConsultoriaFundoDto` 55.5%, `CustodianteDto` 55.5%, `RelFundoTipoAtivoDto` 60%, `RelFundoCedenteDto` 70%

### Group G: Admin Fundos DTOs and queries — 0%
- All `Fundos\Queries\Admin\*` (7 DTO files + 7 query files)

**Application at ≥80%:** Fundos module commands, queries, and handlers (86+ files, all at 100%)

## Infrastructure — files below 80% (excl. ExcludeFromCodeCoverage)

| File | %Line | Note |
|---|---|---|
| `Keycloak\KeycloakAuthException.cs` | 0.0% | Exception class, single line |
| `Keycloak\KeycloakTokenService.cs` | 0.0% | Needs integration/mock test |
| `Keycloak\KeycloakUserService.cs` | 0.0% | 445 LoC; needs mock HTTP tests |
| `Persistence\AppDbContextFactory.cs` | 0.0% | Design-time factory |
| `Persistence\CurrentCompanyPermissionsService.cs` | 66.7% | Missing branch |

**Note:** All 21 repository classes carry `[ExcludeFromCodeCoverage]` — they are covered only via Integration.Tests (Docker).

## API — files below 80%

| File | %Line | %Branch | Note |
|---|---|---|---|
| `Filters\IdempotencyFilter.cs` | 0.0% | — | No test for idempotency logic |
| `obj/...\OpenApiXmlCommentSupport.generated.cs` | 0.0% | — | Auto-generated, exclude |
| `Security\GroupsClaimsTransformation.cs` | 27.8% | — | Claims transform, needs unit tests |
| `Security\RealmRolesClaimsTransformation.cs` | 37.0% | — | Claims transform, needs unit tests |
| `Middleware\SecurityHeadersMiddleware.cs` | 71.6% | — | Missing Sec-Fetch branch tests |

## Per-layer effort estimate for reaching >80%

| Layer | Current % | Gap | Estimated new tests needed | Risk |
|---|---|---|---|---|
| Domain | 95.95% | 6 files at 0–50% | ~8–12 exception ctor tests | Low |
| Application | 45.58% | 93 files at 0%, ~30 at 50–79% | **~200–300 unit tests** (admin+companies+auth modules entirely untested) | HIGH |
| Infrastructure | 94.86% (non-exempt) | 4 files, all infra/keycloak | ~15 integration/mock tests for Keycloak services | Medium (HTTP mocks needed) |
| API | 70.28% | 5 files (2 claims transforms, 1 idempotency filter, 1 middleware) | ~20–30 unit tests | Low-Medium |

**Total estimated new test count for D-49 compliance:** ~250–370 unit tests (dominated by Application layer, which needs to cover the entire Admin/Companies/Auth sub-domains from scratch).

## D-49 conflict: ExcludeFromCodeCoverage on repositories

Infrastructure repositories (`[ExcludeFromCodeCoverage]`) represent ~21 files covering all DB access logic. D-49 states "no exclusions" but these were explicitly opted out before this phase, covered only via Integration.Tests (Docker, which runs in CI). If D-49 is enforced literally, either: (a) Docker must run in this environment to get fresh Integration.Tests coverage, or (b) the attribute must be removed and unit tests added with EF InMemory/mocks. This is flagged for orchestrator decision (see AUDIT.md Risk section).
