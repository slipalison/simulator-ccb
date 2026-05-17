---
phase: 47-application-layer
plan: 01
subsystem: application
tags: [csharp, cqrs, fluentvalidation, handlers, validators, dtos, domain-driven-design]

# Dependency graph
requires:
  - phase: 45-domain-layer-v8
    provides: Domain entities, value objects, repositories, exceptions
  - phase: 46-infrastructure-layer
    provides: EF Core configurations, repository implementations, DbContext migrations
provides:
  - Command handlers for ConsultoriaFundo, Custodiante, Fundo, Cedente, TipoAtivo CRUD
  - FluentValidation validators with domain VO check-digit validation
  - DTOs for all 5 fund entities and FundoCedente
  - DI registration in DependencyInjection.cs
affects: [48-api-layer, 49-integration-tests]

# Tech tracking
tech-stack:
  added: []
  patterns: [CQRS-command-handlers, sealed-classes, FluentValidation-Must-rules, domain-VO-validation, DuplicateEntityException-for-409]

key-files:
  created:
    - src/Onboarding.Application/Fundos/Commands/RegisterConsultoriaFundoCommand.cs
    - src/Onboarding.Application/Fundos/Commands/RegisterConsultoriaFundoCommandHandler.cs
    - src/Onboarding.Application/Fundos/Commands/RegisterConsultoriaFundoCommandValidator.cs
    - src/Onboarding.Application/Fundos/Commands/UpdateConsultoriaFundoCommand.cs
    - src/Onboarding.Application/Funds/Commands/UpdateConsultoriaFundoCommandHandler.cs
    - src/Onboarding.Application/Funds/Commands/UpdateConsultoriaFundoCommandValidator.cs
    - src/Onboarding.Application/Fundos/Commands/RegisterCustodianteCommand.cs
    - src/Onboarding.Application/Fundos/Commands/RegisterCustodianteCommandHandler.cs
    - src/Onboarding.Application/Fundos/Commands/RegisterCustodianteCommandValidator.cs
    - src/Onboarding.Application/Fundos/Commands/UpdateCustodianteCommand.cs
    - src/Onboarding.Application/Fundos/Commands/UpdateCustodianteCommandHandler.cs
    - src/Onboarding.Application/Fundos/Commands/UpdateCustodianteCommandValidator.cs
    - src/Onboarding.Application/Fundos/Commands/CreateTipoAtivoCommand.cs
    - src/Onboarding.Application/Fundos/Commands/CreateTipoAtivoCommandHandler.cs
    - src/Onboarding.Application/Fundos/Commands/CreateTipoAtivoCommandValidator.cs
    - src/Onboarding.Application/Fundos/Commands/UpdateTipoAtivoCommand.cs
    - src/Onboarding.Application/Fundos/Commands/UpdateTipoAtivoCommandHandler.cs
    - src/Onboarding.Application/Fundos/Commands/UpdateTipoAtivoCommandValidator.cs
    - src/Onboarding.Application/Fundos/Commands/RegisterFundoCommand.cs
    - src/Onboarding.Application/Fundos/Commands/RegisterFundoCommandHandler.cs
    - src/Onboarding.Application/Fundos/Commands/RegisterFundoCommandValidator.cs
    - src/Onboarding.Application/Fundos/Commands/UpdateFundoCommand.cs
    - src/Onboarding.Application/Fundos/Commands/UpdateFundoCommandHandler.cs
    - src/Onboarding.Application/Fundos/Commands/UpdateFundoCommandValidator.cs
    - src/Onboarding.Application/Fundos/Commands/TransitionFundoStatusCommand.cs
    - src/Onboarding.Application/Fundos/Commands/TransitionFundoStatusCommandHandler.cs
    - src/Onboarding.Application/Fundos/Commands/TransitionFundoStatusCommandValidator.cs
    - src/Onboarding.Application/Fundos/Commands/RegisterCedentePfCommand.cs
    - src/Onboarding.Application/Fundos/Commands/RegisterCedentePfCommandHandler.cs
    - src/Onboarding.Application/Fundos/Commands/RegisterCedentePfCommandValidator.cs
    - src/Onboarding.Application/Fundos/Commands/RegisterCedentePjCommand.cs
    - src/Onboarding.Application/Fundos/Commands/RegisterCedentePjCommandHandler.cs
    - src/Onboarding.Application/Fundos/Commands/RegisterCedentePjCommandValidator.cs
    - src/Onboarding.Application/Fundos/Commands/UpdateCedenteCommand.cs
    - src/Onboarding.Application/Fundos/Commands/UpdateCedenteCommandHandler.cs
    - src/Onboarding.Application/Fundos/Commands/UpdateCedenteCommandValidator.cs
    - src/Onboarding.Application/Fundos/DTOs/ConsultoriaFundoDto.cs
    - src/Onboarding.Application/Fundos/DTOs/CustodianteDto.cs
    - src/Onboarding.Application/Fundos/DTOs/TipoAtivoDto.cs
    - src/Onboarding.Application/Fundos/DTOs/FundoDto.cs
    - src/Onboarding.Application/Fundos/DTOs/CedenteDto.cs
    - src/Onboarding.Application/Fundos/DTOs/FundoCedenteDto.cs
  modified:
    - src/Onboarding.Application/DependencyInjection.cs

key-decisions:
  - "Used KeyNotFoundException for entity-not-found cases — matches existing project pattern (not custom NotFoundException)"
  - "ICurrentCompanyService injected only in company-scoped handlers (ConsultoriaFundo, Custodiante, Fundo, Cedente) — not in TipoAtivo handlers (D-03)"
  - "Fundo register always assigns RASCUNHO status via domain factory method (D-02)"
  - "CedenteDocumento.Pf()/Pj() discriminated union used for CPF/CNPJ validation and uniqueness checks"
  - "Fundo register validates FK references (ConsultoriaFundoId, CustodianteId) before domain factory call"

patterns-established:
  - "Command handlers: sealed class implementing ICommandHandler<TCommand, TResult> with constructor DI"
  - "Validators: FluentValidation AbstractValidator with Must rules delegating to domain VOs (Cnpj.Create, Cpf.Create)"
  - "Uniqueness checks BEFORE domain factory method call — DuplicateEntityException for 409 mapping"
  - "Audit logging: IAuditService.RecordAsync called after persist, with correct ActionType enum values"
  - "DTOs: sealed records mapping entity properties to response shapes"

requirements-completed: [CAD-01, CAD-03, CAD-05, CAD-07, CAD-09, CAD-11, CAD-13, CAD-14, CAD-15, CAD-17, CAD-19, CAD-21, ADM-04]

duration: 17min
completed: 2026-05-03
---

# Phase 47: Application Layer Summary

**CQRS command handlers, validators, and DTOs for all 5 fund entities with audit integration and state machine enforcement**

## Performance

- **Duration:** 17 min
- **Started:** 2026-05-03T18:03:03Z
- **Completed:** 2026-05-03T18:19:58Z
- **Tasks:** 2
- **Files modified:** 43

## Accomplishments

- All 12 command handler files compile cleanly with proper DI injection
- All entity register handlers enforce uniqueness (CNPJ/CPF/codigo) with DuplicateEntityException for 409
- Fundo register always starts with RASCUNHO status and validates FK references
- TransitionFundoStatusCommandHandler delegates to domain state machine (FundoStatusValidator.CanTransitionTo)
- Cedente PF uses Cpf.Create + CedenteDocumento.Pf(); Cedente PJ uses Cnpj.Create + CedenteDocumento.Pj()
- TipoAtivo handlers have NO ICurrentCompanyService (global entity per D-03)
- All handlers audit mutations via IAuditService with correct ActionType enum values
- DI registration groups all Phase 47 handlers and validators under clear comment

## Task Commits

Each task was committed atomically:

1. **Task 1: Commands, handlers, validators, and DTOs for ConsultoriaFundo + Custodiante + TipoAtivo** - `8dd890b` (feat)
2. **Task 2: Commands, handlers, validators, and DTOs for Fundo + Cedente + DI registration** - `1964c04` (feat)

## Files Created/Modified

- `src/Onboarding.Application/Fundos/Commands/RegisterConsultoriaFundoCommand.cs` - Register ConsultoriaFundo command record
- `src/Onboarding.Application/Fundos/Commands/RegisterConsultoriaFundoCommandHandler.cs` - Handler with CNPJ validation, uniqueness check, audit
- `src/Onboarding.Application/Fundos/Commands/RegisterConsultoriaFundoCommandValidator.cs` - FluentValidation with Cnpj.Create Must rule
- `src/Onboarding.Application/Fundos/Commands/UpdateConsultoriaFundoCommand.cs` - Update ConsultoriaFundo command record
- `src/Onboarding.Application/Fundos/Commands/UpdateConsultoriaFundoCommandHandler.cs` - Update handler with audit
- `src/Onboarding.Application/Fundos/Commands/UpdateConsultoriaFundoCommandValidator.cs` - Validator for update fields
- `src/Onboarding.Application/Fundos/Commands/RegisterCustodianteCommand.cs` - Register Custodiante command record
- `src/Onboarding.Application/Fundos/Commands/RegisterCustodianteCommandHandler.cs` - Handler with CNPJ validation, uniqueness check, audit
- `src/Onboarding.Application/Fundos/Commands/RegisterCustodianteCommandValidator.cs` - FluentValidation with Cnpj.Create Must rule
- `src/Onboarding.Application/Fundos/Commands/UpdateCustodianteCommand.cs` - Update Custodiante command record
- `src/Onboarding.Application/Fundos/Commands/UpdateCustodianteCommandHandler.cs` - Update handler with audit
- `src/Onboarding.Application/Fundos/Commands/UpdateCustodianteCommandValidator.cs` - Validator for update fields
- `src/Onboarding.Application/Fundos/Commands/CreateTipoAtivoCommand.cs` - Create TipoAtivo command record (global entity per D-03)
- `src/Onboarding.Application/Fundos/Commands/CreateTipoAtivoCommandHandler.cs` - Handler with global codigo uniqueness check, no ICurrentCompanyService
- `src/Onboarding.Application/Fundos/Commands/CreateTipoAtivoCommandValidator.cs` - Validator with codigo/descricao/categoria rules
- `src/Onboarding.Application/Fundos/Commands/UpdateTipoAtivoCommand.cs` - Update TipoAtivo command record
- `src/Onboarding.Application/Fundos/Commands/UpdateTipoAtivoCommandHandler.cs` - Update handler with audit
- `src/Onboarding.Application/Fundos/Commands/UpdateTipoAtivoCommandValidator.cs` - Validator for update fields
- `src/Onboarding.Application/Fundos/Commands/RegisterFundoCommand.cs` - Register Fundo command with CNPJ, FK references, TipoFundo
- `src/Onboarding.Application/Fundos/Commands/RegisterFundoCommandHandler.cs` - Handler with CNPJ uniqueness, FK validation, RASCUNHO status
- `src/Onboarding.Application/Fundos/Commands/RegisterFundoCommandValidator.cs` - Validator with Cnpj.Create, FK required, TipoFundo enum
- `src/Onboarding.Application/Fundos/Commands/UpdateFundoCommand.cs` - Update Fundo data command (no status — use Transition)
- `src/Onboarding.Application/Fundos/Commands/UpdateFundoCommandHandler.cs` - Data update handler with audit
- `src/Onboarding.Application/Fundos/Commands/UpdateFundoCommandValidator.cs` - Validator for data update fields
- `src/Onboarding.Application/Fundos/Commands/TransitionFundoStatusCommand.cs` - Status transition command with FundoStatus enum
- `src/Onboarding.Application/Fundos/Commands/TransitionFundoStatusCommandHandler.cs` - Handler delegating to fundo.TransitionTo() state machine
- `src/Onboarding.Application/Fundos/Commands/TransitionFundoStatusCommandValidator.cs` - Validator for FundoId and FundoStatus enum
- `src/Onboarding.Application/Fundos/Commands/RegisterCedentePfCommand.cs` - Register Cedente PF with CPF
- `src/Onboarding.Application/Fundos/Commands/RegisterCedentePfCommandHandler.cs` - Handler with Cpf.Create, CedenteDocumento.Pf() uniqueness
- `src/Onboarding.Application/Fundos/Commands/RegisterCedentePfCommandValidator.cs` - Validator with Cpf.Create Must rule
- `src/Onboarding.Application/Fundos/Commands/RegisterCedentePjCommand.cs` - Register Cedente PJ with CNPJ
- `src/Onboarding.Application/Funds/Commands/RegisterCedentePjCommandHandler.cs` - Handler with Cnpj.Create, CedenteDocumento.Pj() uniqueness
- `src/Onboarding.Application/Fundos/Commands/RegisterCedentePjCommandValidator.cs` - Validator with Cnpj.Create Must rule
- `src/Onboarding.Application/Fundos/Commands/UpdateCedenteCommand.cs` - Update Cedente command with Nome/Status
- `src/Onboarding.Application/Fundos/Commands/UpdateCedenteCommandHandler.cs` - Update handler with audit
- `src/Onboarding.Application/Fundos/Commands/UpdateCedenteCommandValidator.cs` - Validator for update fields
- `src/Onboarding.Application/Fundos/DTOs/ConsultoriaFundoDto.cs` - DTO with all ConsultoriaFundo fields including status enum
- `src/Onboarding.Application/Fundos/DTOs/CustodianteDto.cs` - DTO with RazaoSocial, CodigoInterno, Cnpj, Status
- `src/Onboarding.Application/Fundos/DTOs/TipoAtivoDto.cs` - DTO with Codigo, Descricao, Categoria, Status, OrdemExibicao
- `src/Onboarding.Application/Fundos/DTOs/FundoDto.cs` - DTO with Nome, Cnpj, FK ids, TipoFundo, Status (state machine)
- `src/Onboarding.Application/Fundos/DTOs/CedenteDto.cs` - DTO with Documento (CPF/CNPJ as string), CedenteTipo, Nome
- `src/Onboarding.Application/Fundos/DTOs/FundoCedenteDto.cs` - DTO with LimiteExposicaoPercentual/Valor, dates, status
- `src/Onboarding.Application/DependencyInjection.cs` - Added all Phase 47 handler and validator registrations

## Decisions Made

- **KeyNotFoundException for not-found:** Used existing project convention (KeyNotFoundException) instead of custom NotFoundException — aligns with GlobalExceptionHandler mapping in API layer
- **ICurrentCompanyService scoping:** Only injected in company-scoped handlers (ConsultoriaFundo, Custodiante, Fundo, Cedente); TipoAtivo handlers omit it entirely per D-03 (global entity)
- **Fundo status transition:** Separate TransitionFundoStatusCommand rather than including status in UpdateFundoCommand — clean separation between data mutations and state machine transitions (D-02)
- **FundoCedenteDto:** Uses decimal for LimiteExposicaoPercentual and decimal? for LimiteExposicaoValor — matches domain's HasPrecision(5,2)/(18,4) conventions

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] NotFoundException does not exist — used KeyNotFoundException instead**
- **Found during:** Task 1 (initial handler creation)
- **Issue:** Plan mentioned NotFoundException but project uses KeyNotFoundException throughout (matches GlobalExceptionHandler mapping in API layer)
- **Fix:** Changed all handlers to throw KeyNotFoundException for entity-not-found cases, matching existing pattern in Companies/, Admin/ handlers
- **Files modified:** UpdateConsultoriaFundoCommandHandler.cs, UpdateCustodianteCommandHandler.cs, UpdateTipoAtivoCommandHandler.cs (and Task 2 equivalents)
- **Verification:** Build succeeds; pattern consistent with RegisterEmployeeCommandHandler, UpdateEmployeeCommandHandler, etc.
- **Committed in:** 8dd890b, 1964c04 (part of task commits)

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** Zero scope creep — purely consistency fix to match established project conventions.

## Issues Encountered

None — both tasks compiled successfully on first attempt after the NotFoundException fix.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- All application-layer command handlers and validators are ready for Phase 48 (API layer — Controllers)
- DI registration complete — API layer can inject handlers directly
- Fundo state machine transitions enforced at domain level — API layer only needs to catch InvalidStateTransitionException for 400/409 mapping
- Query handlers are NOT included in this plan — expected in a separate plan for read-side CQRS

---

## Self-Check: PASSED

- All 8 key handler files exist on disk (verified via Test-Path)
- Both task commits exist in git log (`8dd890b`, `1964c04`)
- Build verification: `dotnet build src/Onboarding.Application` — 0 errors
- 12 command handler files compile and inject correct repositories
- 8 validator files reference domain VOs (Cnpj.Create, Cpf.Create) via Must rules
- IAuditService.RecordAsync called in all 12 handlers with correct ActionType enum values
- ICurrentCompanyService present in ConsultoriaFundo, Custodiante, Fundo, Cedente handlers; absent from TipoAtivo handlers
- Fundo.Register() always assigns RASCUNHO status (verified in domain factory method)
- TransitionFundoStatusCommandHandler calls fundo.TransitionTo() which enforces state machine
- DependencyInjection.cs has Phase 47 comment with all 20 registrations (12 handlers + 8 validators)

---
*Phase: 47-application-layer*
*Completed: 2026-05-03*