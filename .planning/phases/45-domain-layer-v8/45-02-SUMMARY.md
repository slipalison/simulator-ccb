# Phase 45 Plan 02 — Summary

**Phase:** 45-domain-layer-v8  
**Plan:** 45-02 (Wave 2)  
**Status:** ✅ Complete  
**Date:** 2026-05-03

## Execution Results

### Task 1: Fundo aggregate with FundoCedente + FundoTipoAtivo

**Files created/modified:**
- `src/Onboarding.Domain/Aggregates/FundoAggregate/Fundo.cs` — Sealed aggregate root with Register() factory, TransitionTo() state machine, Update(), AddCedente/UpdateCedente/RemoveCedente, AddTipoAtivo/RemoveTipoAtivo
- `src/Onboarding.Domain/Aggregates/FundoAggregate/FundoCedente.cs` — Join entity with LimiteExposicaoPercentual VO, LimiteExposicaoValor, DataInicio/DataFim, FundoCedenteStatus
- `src/Onboarding.Domain/Aggregates/FundoAggregate/FundoCedenteStatus.cs` — Enum: ATIVO, INATIVO
- `src/Onboarding.Domain/Aggregates/FundoAggregate/FundoTipoAtivo.cs` — Simple join entity with FundoId + TipoAtivoId FKs
- `tests/Onboarding.Domain.Tests/Aggregates/FundoTests.cs` — 30 tests
- `tests/Onboarding.Domain.Tests/Aggregates/FundoCedenteTests.cs` — 4 tests

**Key behaviors verified:**
- FundoStatus state machine: 6 valid transitions pass, 4+ invalid transitions throw InvalidStateTransitionException
- REL-09 invariant: at most one active FundoCedente per CedenteId per Fundo (DuplicateEntityException on violation)
- Inactive-then-active same Cedente allowed (multiple INATIVO permitted)
- LimiteExposicaoPercentual sentinel -1 = unlimited (REL-08)
- FundoTipoAtivo duplicates rejected, removal idempotent

**Commit:** `b9875ee` — feat(45): implement Fundo aggregate with FundoCedente and FundoTipoAtivo

### Task 2: ConsultoriaFundo, Custodiante, Cedente, TipoAtivo + CedenteTipoAtivo

**Files created:**
- `src/Onboarding.Domain/Aggregates/ConsultoriaFundoAggregate/ConsultoriaFundo.cs` — Sealed aggregate, Register(), Update(), ATIVO/INATIVO
- `src/Onboarding.Domain/Aggregates/ConsultoriaFundoAggregate/ConsultoriaFundoStatus.cs` — Enum
- `src/Onboarding.Domain/Aggregates/CustodianteAggregate/Custodiante.cs` — Sealed aggregate, Register(), Update(), CodigoInterno optional
- `src/Onboarding.Domain/Aggregates/CustodianteAggregate/CustodianteStatus.cs` — Enum
- `src/Onboarding.Domain/Aggregates/CedenteAggregate/Cedente.cs` — Sealed aggregate, RegisterPf()/RegisterPj(), CedenteDocumento DU
- `src/Onboarding.Domain/Aggregates/CedenteAggregate/CedenteStatus.cs` — Enum
- `src/Onboarding.Domain/Aggregates/CedenteAggregate/CedenteTipo.cs` — Enum (moved from FundoAggregate)
- `src/Onboarding.Domain/Aggregates/CedenteAggregate/CedenteTipoAtivo.cs` — Simple join entity
- `src/Onboarding.Domain/Aggregates/TipoAtivoAggregate/TipoAtivo.cs` — Sealed aggregate, NO ClientId (D-03)
- `src/Onboarding.Domain/Aggregates/TipoAtivoAggregate/TipoAtivoStatus.cs` — Enum
- `tests/Onboarding.Domain.Tests/Aggregates/ConsultoriaFundoTests.cs` — 7 tests
- `tests/Onboarding.Domain.Tests/Aggregates/CustodianteTests.cs` — 7 tests
- `tests/Onboarding.Domain.Tests/Aggregates/CedenteTests.cs` — 14 tests
- `tests/Onboarding.Domain.Tests/Aggregates/TipoAtivoTests.cs` — 9 tests (including NO ClientId verification)

**Files deleted (stubs replaced):**
- `src/Onboarding.Domain/Aggregates/FundoAggregate/Cedente.cs` (stub)
- `src/Onboarding.Domain/Aggregates/FundoAggregate/CedenteTipo.cs` (moved to CedenteAggregate)
- `src/Onboarding.Domain/Aggregates/FundoAggregate/ConsultoriaFundo.cs` (stub)
- `src/Onboarding.Domain/Aggregates/FundoAggregate/Custodiante.cs` (stub)
- `src/Onboarding.Domain/Aggregates/TipoAtivoAggregate/TipoAtivo.cs` (stub, replaced with full implementation)

**Files modified:**
- Repository interfaces updated to use correct namespaces (CedenteAggregate, ConsultoriaFundoAggregate, CustodianteAggregate)

**Key behaviors verified:**
- Cedente PF path: RegisterPf() with Cpf validation, CedenteDocumento.Pf
- Cedente PJ path: RegisterPj() with Cnpj validation, CedenteDocumento.Pj
- TipoAtivo: NO ClientId property (global entity per TEN-03)
- All company-scoped entities (Fundo, ConsultoriaFundo, Custodiante, Cedente) have ClienteId
- CedenteTipoAtivo duplicates rejected, removal idempotent

**Commit:** `8705cfb` — feat(45): implement ConsultoriaFundo, Custodiante, Cedente, TipoAtivo aggregates

## Verification Results

| Check | Result |
|-------|--------|
| `dotnet build src/Onboarding.Domain` | ✅ 0 errors, 0 warnings |
| New aggregate tests (FundoTests + FundoCedenteTests + ConsultoriaFundoTests + CustodianteTests + CedenteTests + TipoAtivoTests) | ✅ 71 passed, 0 failed |
| Existing domain tests | ✅ 371 passed, 1 pre-existing failure (AccessGroupTests) |
| Fundo.TransitionTo enforces state machine | ✅ Verified by tests |
| FundoCedente REL-09: at most 1 active per pair | ✅ Verified by test (DuplicateEntityException) |
| Cedente has RegisterPf/RegisterPj | ✅ Verified by tests |
| TipoAtivo has NO ClientId | ✅ Verified by test + grep |
| Company-scoped entities have ClienteId | ✅ Verified by grep |

## Decisions Applied

- **D-01:** ConsultoriaFundo, Custodiante, Cedente are company-scoped (ClienteId FK)
- **D-02:** FundoStatus state machine with FundoStatusValidator whitelist
- **D-03/TEN-03:** TipoAtivo is global — no ClientId
- **D-04:** LimiteExposicaoPercentual sentinel -1 = unlimited
- **D-05/D-06:** Cedente uses CedenteDocumento discriminated union (PF/PJ)
- **D-07:** Simple enum + validator method (YAGNI — no State Pattern)
- **D-08:** FundoCedente is entity inside Fundo aggregate, not a separate aggregate root
- **REL-08:** LimiteExposicaoPercentual with -1 sentinel
- **REL-09:** Enforced via DuplicateEntityException in Fundo.AddCedente()

## Threat Mitigations

- **T-45-05:** Fundo.TransitionTo() uses whitelist-based state machine — all undefined transitions throw InvalidStateTransitionException (✅ verified)
- **T-45-06:** Factory methods require clientId — no entity can exist without tenant scope (✅ verified)
- **T-45-07:** FundoCedente REL-09 enforced in aggregate — DuplicateEntityException on violation (✅ verified)
- **T-45-08:** TipoAtivo global — accepted, no PII in catalog data (✅ verified)