---
phase: 46-infrastructure-layer
plan: 02
subsystem: database
tags: [ef-core, postgresql, repositories, dependency-injection, migration]

requires:
  - phase: 45-domain-layer-v8
    provides: Domain aggregates, value objects, repository interfaces
  - phase: 46-infrastructure-layer
    plan: 01
    provides: EF Core configurations, AppDbContext DbSets, CedenteDocumentoValueConverter

provides:
  - 5 repository implementations following EmployeeRepository pattern (D-12)
  - DI registration for all 5 fundos module repositories
  - EF Core migration AddFundosModule creating 8 tables per D-17
  - CedenteDocumento DU shadow property sync in CedenteRepository (D-09)

affects: [47-application-layer-v8, 48-api-permissions]

tech-stack:
  added: []
  patterns: [IgnoreQueryFilters for uniqueness, Explicit CompanyId for paged queries, Shadow property sync for DU, Global repository without company filter]

key-files:
  created:
    - src/Onboarding.Infrastructure/Repositories/FundoRepository.cs
    - src/Onboarding.Infrastructure/Repositories/ConsultoriaFundoRepository.cs
    - src/Onboarding.Infrastructure/Repositories/CustodianteRepository.cs
    - src/Onboarding.Infrastructure/Repositories/CedenteRepository.cs
    - src/Onboarding.Infrastructure/Repositories/TipoAtivoRepository.cs
    - src/Onboarding.Infrastructure/Persistence/Migrations/20260503162256_AddFundosModule.cs
    - src/Onboarding.Infrastructure/Persistence/Migrations/20260503162256_AddFundosModule.Designer.cs
  modified:
    - src/Onboarding.Infrastructure/DependencyInjection.cs
    - src/Onboarding.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs

key-decisions:
  - "CedenteDocumento.Match() cannot return void — used IsPf/IsPj pattern matching with explicit casts instead of Match for shadow property sync and ExistsByDocumentoAsync"
  - "TipoAtivoRepository uses plain FirstOrDefaultAsync/AnyAsync without IgnoreQueryFilters (no HasQueryFilter per TEN-03)"

patterns-established:
  - "Repository shadow property sync: Set shadow properties from DU via EntityEntry before Add/Save operations"
  - "Global repository pattern: no IgnoreQueryFilters, no company filter for non-tenant-scoped entities"
  - "ExistsByDU pattern: check DocumentoType shadow property + value shadow property per DU variant"

requirements-completed: [CAD-04, CAD-08, CAD-12, CAD-18, CAD-22, TEN-01, TEN-02, TEN-03]

duration: 8min
completed: 2026-05-03
---

# Phase 46: Infrastructure Layer Summary

**5 repository implementations with shadow property DU persistence, company-scoped IgnoreQueryFilters, and EF Core migration creating 8 fundos tables**

## Performance

- **Duration:** 8 min
- **Started:** 2026-05-03T16:15:00Z
- **Completed:** 2026-05-03T16:23:00Z
- **Tasks:** 2
- **Files modified:** 9 (6 created, 3 modified)

## Accomplishments
- FundoRepository with Include for Cedentes/TiposAtivo, IgnoreQueryFilters for GetById/ExistsByCnpj, company-scoped paged search
- CedenteRepository with shadow property sync (DocumentoTipo, CpfValue, CnpjCedenteValue) for DU persistence per D-09
- TipoAtivoRepository with global scope (no IgnoreQueryFilters, no company filter) per TEN-03
- EF Core migration AddFundosModule creating 8 tables with all indexes, constraints, and FK per D-17
- DI registration for all 5 fundos module repositories

## Task Commits

Each task was committed atomically:

1. **Task 1: Implement 5 repository classes + DI registration** - `c54f598` (feat)
2. **Task 2: Generate EF Core migration AddFundosModule** - `9cc43b0` (feat)

## Files Created/Modified
- `src/Onboarding.Infrastructure/Repositories/FundoRepository.cs` - Fundo repo with Include, IgnoreQueryFilters, company-scoped search
- `src/Onboarding.Infrastructure/Repositories/ConsultoriaFundoRepository.cs` - ConsultoriaFundo repo with CNPJ uniqueness (CAD-04)
- `src/Onboarding.Infrastructure/Repositories/CustodianteRepository.cs` - Custodiante repo with CNPJ uniqueness (CAD-08)
- `src/Onboarding.Infrastructure/Repositories/CedenteRepository.cs` - Cedente repo with DU shadow property sync (D-09) and ExistsByDocumentoAsync (D-10)
- `src/Onboarding.Infrastructure/Repositories/TipoAtivoRepository.cs` - Global TipoAtivo repo (TEN-03)
- `src/Onboarding.Infrastructure/DependencyInjection.cs` - 5 new AddScoped registrations
- `src/Onboarding.Infrastructure/Persistence/Migrations/20260503162256_AddFundosModule.cs` - Migration with 8 tables
- `src/Onboarding.Infrastructure/Persistence/Migrations/20260503162256_AddFundosModule.Designer.cs` - Migration designer
- `src/Onboarding.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs` - Updated snapshot

## Decisions Made
- **CedenteDocumento.Match() type inference**: The `Match<TResult>` method cannot infer TResult when lambdas return `void` (statements, not expressions). Used `IsPf`/`IsPj` checks with explicit casts to `PessoaFisica`/`PessoaJuridica` instead — same semantic result, compiles cleanly.
- **TipoAtivoRepository no IgnoreQueryFilters**: TipoAtivo is a global entity (TEN-03) — no HasQueryFilter exists, so no need to bypass it. Plain FirstOrDefaultAsync/AnyAsync sufficient.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] CedenteDocumento.Match() void-return type inference fails (CS0411)**
- **Found during:** Task 1 (CedenteRepository implementation)
- **Issue:** `cedente.Documento.Match(pf => { ... }, pj => { ... })` fails C# type inference (CS0411) because the lambda bodies are void statements, not expressions returning a common type
- **Fix:** Replaced Match with `IsPf`/`IsPj` pattern checks and explicit casts to `PessoaFisica`/`PessoaJuridica` — same semantic behavior, compiles correctly
- **Files modified:** CedenteRepository.cs (both ExistsByDocumentoAsync and SetShadowProperties)
- **Verification:** `dotnet build` succeeds with 0 errors
- **Committed in:** c54f598 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 missing critical — compilation blocker)
**Impact on plan:** Minimal. Same shadow property behavior, same DU dispatch logic. No scope creep.

## Issues Encountered
None

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- Infrastructure layer complete: configurations (46-01), repositories + DI + migration (46-02)
- Ready for Phase 47 (Application Layer) with handlers using these repositories
- Ready for Phase 48 (API + Permissions) to add admin methods (D-13 deferred)

---
*Phase: 46-infrastructure-layer*
*Completed: 2026-05-03*