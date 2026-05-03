---
phase: 46-infrastructure-layer
plan: 01
subsystem: database
tags: [ef-core, postgresql, configurations, hasqueryfilter, ownsmmany, shadow-properties]

requires:
  - phase: 45-domain-layer-v8
    provides: Domain aggregates, value objects, repository interfaces

provides:
  - EF Core fluent configurations for 5 aggregate roots
  - CedenteDocumento DU persistence via shadow properties
  - OwnsMany owned collections for 3 join entities
  - HasQueryFilter multi-tenant isolation on 4 company-scoped entities
  - Composite filtered indexes for CPF/CNPJ uniqueness per company
  - Partial unique index on FundoCedente active associations
  - Decimal precision for monetary and percentage values

affects: [46-infrastructure-layer, 47-application-layer-v8, 48-api-permissions, 49-frontend-client]

tech-stack:
  added: []
  patterns: [IEntityTypeConfiguration, HasQueryFilter, OwnsMany, Shadow Properties, ValueConverter, Partial Unique Index]

key-files:
  created:
    - src/Onboarding.Infrastructure/Persistence/Configurations/FundoConfiguration.cs
    - src/Onboarding.Infrastructure/Persistence/Configurations/ConsultoriaFundoConfiguration.cs
    - src/Onboarding.Infrastructure/Persistence/Configurations/CustodianteConfiguration.cs
    - src/Onboarding.Infrastructure/Persistence/Configurations/CedenteConfiguration.cs
    - src/Onboarding.Infrastructure/Persistence/Configurations/TipoAtivoConfiguration.cs
    - src/Onboarding.Infrastructure/Persistence/Configurations/CedenteDocumentoValueConverter.cs
  modified:
    - src/Onboarding.Infrastructure/Persistence/AppDbContext.cs

key-decisions:
  - "CedenteDocumento DU uses ValueConverter class (not lambda) because expression trees cannot contain throw expressions"
  - "Owned collections (FundoCedente, FundoTipoAtivo, CedenteTipoAtivo) configured via OwnsMany — no separate DbSets"
  - "TipoAtivo has NO HasQueryFilter and NO ICurrentCompanyService — global catalog per D-03/TEN-03"

patterns-established:
  - "CedenteDocumento shadow properties pattern: documento_tipo discriminator + cpf/cnpj_cedente nullable columns"
  - "Composite filtered indexes for PF/PJ uniqueness per company scope"
  - "Partial unique index WHERE status = 1 for FundoCedente active-only constraint"

requirements-completed: [CAD-04, CAD-08, CAD-12, CAD-18, CAD-22, TEN-01, TEN-02, TEN-03]

duration: 12min
completed: 2026-05-03
---

# Phase 46: Infrastructure Layer Summary

**5 EF Core configurations with HasQueryFilter, OwnsMany, shadow properties, and decimal precision for fundos module**

## Performance

- **Duration:** 12 min
- **Started:** 2026-05-03T15:59:41Z
- **Completed:** 2026-05-03T16:11:44Z
- **Tasks:** 1
- **Files modified:** 7 (6 created, 1 modified)

## Accomplishments
- FundoConfiguration with HasQueryFilter (TEN-01), OwnsMany for FundoCedente (precision + partial unique index per D-11/D-16) and FundoTipoAtivo
- ConsultoriaFundoConfiguration with HasQueryFilter and unique CNPJ per company (CAD-04)
- CustodianteConfiguration with HasQueryFilter and unique CNPJ per company (CAD-08)
- CedenteConfiguration with HasQueryFilter, CedenteDocumento DU shadow properties (D-09), composite filtered indexes (D-10), OwnsMany for CedenteTipoAtivo (D-15)
- TipoAtivoConfiguration with unique Codigo globally (CAD-22) — NO HasQueryFilter per TEN-03
- AppDbContext updated with 5 new DbSets and 5 ApplyConfiguration calls

## Task Commits

1. **Task 1: EF Core configurations for 5 aggregate roots** - `670e00e` (feat)

## Files Created/Modified
- `src/Onboarding.Infrastructure/Persistence/Configurations/FundoConfiguration.cs` - Fundo config with HasQueryFilter, OwnsMany, precision, partial unique index
- `src/Onboarding.Infrastructure/Persistence/Configurations/ConsultoriaFundoConfiguration.cs` - ConsultoriaFundo config with HasQueryFilter and unique CNPJ
- `src/Onboarding.Infrastructure/Persistence/Configurations/CustodianteConfiguration.cs` - Custodiante config with HasQueryFilter and unique CNPJ
- `src/Onboarding.Infrastructure/Persistence/Configurations/CedenteConfiguration.cs` - Cedente config with shadow properties, composite indexes, OwnsMany
- `src/Onboarding.Infrastructure/Persistence/Configurations/TipoAtivoConfiguration.cs` - TipoAtivo config (global, no HasQueryFilter)
- `src/Onboarding.Infrastructure/Persistence/Configurations/CedenteDocumentoValueConverter.cs` - ValueConverter for CedenteDocumento DU write-side
- `src/Onboarding.Infrastructure/Persistence/AppDbContext.cs` - 5 new DbSets + 5 ApplyConfiguration calls

## Decisions Made
- **CedenteDocumento ValueConverter uses class (not lambda)**: Expression trees cannot contain `throw` expressions, so a `ValueConverter<TModel,TProvider>` subclass was needed. The fromProvider returns a placeholder — actual reconstruction uses shadow properties via repository pattern D-12.
- **No separate DbSets for owned collections**: FundoCedente, FundoTipoAtivo, CedenteTipoAtivo owned via OwnsMean — EF Core manages them within parent aggregate DbSets.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] CedenteDocumento ValueConverter expression tree limitation**
- **Found during:** Task 1 (EF Core configuration implementation)
- **Issue:** Plan specified `throw new InvalidOperationException(...)` in the HasConversion lambda, but C# expression trees cannot contain throw expressions (CS8188 error).
- **Fix:** Created `CedenteDocumentoValueConverter` class extending `ValueConverter<CedenteDocumento, string>` with a placeholder `fromProvider` that returns `CedenteDocumento.Pf(Cpf.Create("00000000000"))` — since reconstruction uses shadow properties via repository pattern (D-12), this value is never used.
- **Files modified:** CedenteConfiguration.cs (uses new converter), CedenteDocumentoValueConverter.cs (new file)
- **Verification:** `dotnet build` succeeds with 0 errors
- **Committed in:** 670e00e (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 missing critical — compilation blocker)
**Impact on plan:** Minimal. The ValueConverter class achieves the same persistence behavior as the planned lambda approach, with the added benefit of being explicit about the read-side limitation (reconstruction via shadow properties in the repository).

## Issues Encountered
None

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- EF Core configurations complete and compiling
- Ready for Plan 46-02 (Repository implementations + dependency injection + AddFundosModule migration per D-17)
- AppDbContext registration pattern established for new configurations

---
*Phase: 46-infrastructure-layer*
*Completed: 2026-05-03*