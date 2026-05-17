---
phase: 47-application-layer
plan: 02
subsystem: application
tags: [csharp, cqrs, query-handlers, pagination, search, fundos, company-scoped, global-scope]

# Dependency graph
requires:
  - phase: 45-domain-layer-v8
    provides: Domain entities, value objects, repository pagination interfaces
  - phase: 46-infrastructure-layer
    provides: EF Core repository implementations with GetPagedByCompanyAsync/GetPagedAsync
  - phase: 47-application-layer-plan-01
    provides: DTOs for all 5 fund entities (ConsultoriaFundoDto, CustodianteDto, FundoDto, CedenteDto, TipoAtivoDto)
provides:
  - Paginated list query handlers for all 5 fund entities
  - Search by name/document with company isolation
  - DI registration for all query handlers
affects: [48-api-layer, 49-integration-tests]

# Tech tracking
tech-stack:
  added: []
  patterns: [CQRS-query-handlers, ICurrentCompanyService-scoping, PaginatedResult-listing, sealed-classes, Match-discriminated-union]

key-files:
  created:
    - src/Onboarding.Application/Fundos/Queries/ListConsultoriaFundoQuery.cs
    - src/Onboarding.Application/Fundos/Queries/ListConsultoriaFundoQueryHandler.cs
    - src/Onboarding.Application/Fundos/Queries/ListCustodianteQuery.cs
    - src/Onboarding.Application/Fundos/Queries/ListCustodianteQueryHandler.cs
    - src/Onboarding.Application/Fundos/Queries/ListFundoQuery.cs
    - src/Onboarding.Application/Fundos/Queries/ListFundoQueryHandler.cs
    - src/Onboarding.Application/Fundos/Queries/ListCedenteQuery.cs
    - src/Onboarding.Application/Fundos/Queries/ListCedenteQueryHandler.cs
    - src/Onboarding.Application/Fundos/Queries/ListTipoAtivoQuery.cs
    - src/Onboarding.Application/Fundos/Queries/ListTipoAtivoQueryHandler.cs
  modified:
    - src/Onboarding.Application/DependencyInjection.cs

key-decisions:
  - "Cedente Documento mapped via Match discriminated union — PF→Cpf.Value, PJ→Cnpj.Value with CedenteTipo enum derivation"
  - "TipoAtivo handler uses GetPagedAsync (no companyId) per D-03/TEN-03 — no ICurrentCompanyService injection"
  - "All query records default Page=1, PageSize=20 matching CAD-02/06/10/16/20 pagination requirement"

patterns-established:
  - "List query handlers: sealed class implementing IQueryHandler<TQuery, PaginatedResult<Dto>>"
  - "Company-scoped list queries inject ICurrentCompanyService; global queries do not"
  - "DTO mapping via LINQ Select with VO property extraction (Cnpj.Value, Email?.Value, Telefone?.Value)"

requirements-completed: [CAD-02, CAD-06, CAD-10, CAD-16, CAD-20]

duration: 6min
completed: 2026-05-03
---

# Phase 47: Application Layer Summary

**Paginated list query handlers for all 5 fund entities with company-scoped isolation and global TipoAtivo scope**

## Performance

- **Duration:** 6 min
- **Started:** 2026-05-03T18:30:59Z
- **Completed:** 2026-05-03T18:37:00Z
- **Tasks:** 1
- **Files modified:** 11

## Accomplishments

- All 5 list query handlers compile cleanly with proper DI injection
- Company-scoped handlers (ConsultoriaFundo, Custodiante, Fundo, Cedente) filter by ICurrentCompanyService.CompanyId
- TipoAtivo handler uses global GetPagedAsync — no company filter per D-03/TEN-03
- Cedente Documento mapped via Match discriminated union with CedenteTipo derivation (PF/PJ)
- Default Page=1, PageSize=20 matching CAD-02/06/10/16/20 pagination requirements
- DI registration groups all 5 query handlers under Phase 47 comment

## Task Commits

Each task was committed atomically:

1. **Task 1: Query handlers for all 5 fund entities with pagination and search** - `73c58d5` (feat)

## Files Created/Modified

- `src/Onboarding.Application/Fundos/Queries/ListConsultoriaFundoQuery.cs` - Paginated listing query record for ConsultoriaFundo (CAD-02)
- `src/Onboarding.Application/Fundos/Queries/ListConsultoriaFundoQueryHandler.cs` - Company-scoped handler with ICurrentCompanyService
- `src/Onboarding.Application/Fundos/Queries/ListCustodianteQuery.cs` - Paginated listing query record for Custodiante (CAD-06)
- `src/Onboarding.Application/Fundos/Queries/ListCustodianteQueryHandler.cs` - Company-scoped handler with ICurrentCompanyService
- `src/Onboarding.Application/Fundos/Queries/ListFundoQuery.cs` - Paginated listing query record for Fundo (CAD-10)
- `src/Onboarding.Application/Fundos/Queries/ListFundoQueryHandler.cs` - Company-scoped handler with ICurrentCompanyService
- `src/Onboarding.Application/Fundos/Queries/ListCedenteQuery.cs` - Paginated listing query record for Cedente (CAD-16)
- `src/Onboarding.Application/Fundos/Queries/ListCedenteQueryHandler.cs` - Company-scoped handler with Documento Match mapping
- `src/Onboarding.Application/Fundos/Queries/ListTipoAtivoQuery.cs` - Paginated listing query record for TipoAtivo (CAD-20)
- `src/Onboarding.Application/Fundos/Queries/ListTipoAtivoQueryHandler.cs` - Global handler, no ICurrentCompanyService (D-03)
- `src/Onboarding.Application/DependencyInjection.cs` - Added 5 IQueryHandler registrations for fund queries

## Decisions Made

- **Cedente Documento mapping:** Used Match discriminated union pattern — PF→Cpf.Value, PJ→Cnpj.Value with CedenteTipo derived from IsPf/IsPj check. Consistent with domain's D-05/D-06 polymorphic design.
- **TipoAtivo global scope:** Handler uses GetPagedAsync (no companyId parameter) and does NOT inject ICurrentCompanyService. Enforces D-03/TEN-03 at application layer.
- **Query record defaults:** Page=1, PageSize=20 as record defaults matching pagination requirements across all CAD-* specs.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed Namespace typo in ListTipoAtivoQueryHandler (Funds → Fundos)**
- **Found during:** Task 1 (initial build)
- **Issue:** Used `Onboarding.Application.Funds.Queries` (English) instead of `Onboarding.Application.Fundos.Queries` (Portuguese, matching project convention)
- **Fix:** Changed namespace to `Fundos.Queries` — consistent with all other handler files in the project
- **Files modified:** ListTipoAtivoQueryHandler.cs
- **Verification:** Build succeeds with 0 errors
- **Committed in:** 73c58d5 (part of task commit)

**2. [Rule 3 - Blocking] Added missing using for CedenteTipo enum in ListCedenteQueryHandler**
- **Found during:** Task 1 (build after first fix)
- **Issue:** CedenteTipo enum in `Onboarding.Domain.Aggregates.CedenteAggregate` namespace not imported
- **Fix:** Added `using Onboarding.Domain.Aggregates.CedenteAggregate;` directive
- **Files modified:** ListCedenteQueryHandler.cs
- **Verification:** Build succeeds with 0 errors
- **Committed in:** 73c58d5 (part of task commit)

---

**Total deviations:** 2 auto-fixed (1 bug, 1 blocking)
**Impact on plan:** Zero scope creep — both fixes are consistency corrections required for compilation.

## Issues Encountered

None — both build errors fixed inline during single task execution.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- All application-layer query handlers ready for Phase 48 (API layer — Controllers)
- DI registration complete — API layer can inject IQueryHandler directly
- Query records provide clean contract for Controller parameter binding
- Company isolation enforced at repository level via HasQueryFilter + ICurrentCompanyService

---

## Self-Check: PASSED

- All 10 query files exist on disk (verified via Test-Path)
- Task commit exists in git log (`73c58d5`)
- Build verification: `dotnet build src/Onboarding.Application` — 0 errors, 0 warnings
- 4 company-scoped handlers inject ICurrentCompanyService (ConsultoriaFundo, Custodiante, Fundo, Cedente)
- 1 global handler does NOT inject ICurrentCompanyService (TipoAtivo per D-03)
- DependencyInjection.cs has Phase 47 queries comment with 5 IQueryHandler registrations
- All handlers use PaginatedResult with correct DTO types
- Cedente handler maps Documento via Match (PF=Cpf, PJ=Cnpj) with CedenteTipo derivation

---
*Phase: 47-application-layer*
*Completed: 2026-05-03*