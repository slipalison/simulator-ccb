# Plan 45-01 Summary

**Phase:** 45-domain-layer-v8
**Plan:** 45-01 — Foundation Types
**Status:** ✅ Complete

## Tasks Executed

### Task 1: Value objects, enums, and domain exceptions
**Commit:** `9803c59` feat(domain): add VOs, enums, and domain exceptions for fundos

| File | Type | Lines |
|------|------|-------|
| `src/Onboarding.Domain/ValueObjects/LimiteExposicaoPercentual.cs` | New | +28 |
| `src/Onboarding.Domain/ValueObjects/CedenteDocumento.cs` | New | +55 |
| `src/Onboarding.Domain/Aggregates/FundoAggregate/FundoStatus.cs` | New | +42 |
| `src/Onboarding.Domain/Aggregates/FundoAggregate/TipoFundo.cs` | New | +13 |
| `src/Onboarding.Domain/Aggregates/FundoAggregate/CedenteTipo.cs` | New | +12 |
| `src/Onboarding.Domain/Aggregates/TipoAtivoAggregate/TipoAtivoCategoria.cs` | New | +13 |
| `src/Onboarding.Domain/Exceptions/DomainException.cs` | New | +10 |
| `src/Onboarding.Domain/Exceptions/InvalidStateTransitionException.cs` | New | +27 |
| `src/Onboarding.Domain/Exceptions/DuplicateEntityException.cs` | New | +19 |
| `tests/.../ValueObjects/LimiteExposicaoPercentualTests.cs` | New | +68 |
| `tests/.../ValueObjects/CedenteDocumentoTests.cs` | New | +72 |
| `tests/.../Aggregates/FundoStatusTests.cs` | New | +76 |

**Tests:** 32 passing (9 LimiteExposicao, 9 CedenteDocumento, 14 FundoStatus)

### Task 2: Permissions extension, ActionType extension, and repository interfaces
**Commit:** `835f7ce` feat(domain): add permissions, ActionType extensions, and repository interfaces

| File | Type | Lines |
|------|-------|-------|
| `src/.../EmployeeAggregate/Permissions.cs` | Modified | +9/-2 |
| `src/.../Audit/ActionType.cs` | Modified | +16 |
| `src/.../Repositories/IFundoRepository.cs` | New | +28 |
| `src/.../Repositories/IConsultoriaFundoRepository.cs` | New | +27 |
| `src/.../Repositories/ICustodianteRepository.cs` | New | +26 |
| `src/.../Repositories/ICedenteRepository.cs` | New | +28 |
| `src/.../Repositories/ITipoAtivoRepository.cs` | New | +25 |
| `src/.../FundoAggregate/Fundo.cs` | Stub | +10 |
| `src/.../FundoAggregate/ConsultoriaFundo.cs` | Stub | +10 |
| `src/.../FundoAggregate/Custodiante.cs` | Stub | +10 |
| `src/.../FundoAggregate/Cedente.cs` | Stub | +10 |
| `src/.../TipoAtivoAggregate/TipoAtivo.cs` | Stub | +10 |
| `tests/.../Aggregates/PermissionsTests.cs` | Modified | +11/-1 |

**Tests:** 43 passing (11 Permissions, 32 from Task 1)

## Verification Results

- `dotnet build src/Onboarding.Domain` → ✅ 0 warnings, 0 errors
- `dotnet test --filter="LimiteExposicaoPercentual|CedenteDocumento|FundoStatus|PermissionsTests"` → ✅ 43 passed, 0 failed
- `grep -r "funds:read" src/Onboarding.Domain/` → ✅ Found in Permissions.cs
- `grep -r "FundoCreated = 30" src/Onboarding.Domain/` → ✅ Found in ActionType.cs

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| `FundoStatusValidator` static class (not method on enum) | C# enums can't have methods; separate validator class with `FrozenDictionary` for O(1) transition lookup |
| `CedenteDocumento` inner types named `PessoaFisica`/`PessoaJuridica` | Avoids C# name collision between static factory `Pf(Cpf)` and nested type; factory methods keep `Pf`/`Pj` names for ergonomics |
| `LimiteExposicaoPercentual` sentinel -1 | Matches D-04: -1 = unlimited, explicit and avoids nullable confusion |
| DomainException as abstract base | Establishes exception hierarchy: all domain business rule violations inherit from it |
| Stub aggregate types in repository namespaces | Plan 02 will flesh these out; stubs needed for repository interfaces to compile |

## Key Links

- `FundoStatus.cs` → `InvalidStateTransitionException.cs` (transition validation throws domain exception)
- `CedenteDocumento.cs` → `Cnpj.cs` / `Cpf.cs` (Pj wraps Cnpj, Pf wraps Cpf)
- `Permissions.cs` → `funds:*` constants added (PERM-01)
- `ActionType.cs` → values 30-44 added for fund audit events

## Requirements Traceability

| Requirement | Status |
|-------------|--------|
| TEN-03 (TipoAtivo global, no ClienteId) | ✅ ITipoAtivoRepository has no companyId params |
| PERM-01 (funds:* permissions) | ✅ 4 constants added to Permissions.All (total 10) |
| REL-08 (LimiteExposicao sentinel -1) | ✅ LimiteExposicaoPercentual.Create(-1) = unlimited |
| D-01 (company-scoped repos) | ✅ IFundo, IConsultoria, ICustodiante, ICedente all have companyId params |
| D-02 (FundoStatus state machine) | ✅ 5 valid transitions, all others rejected |
| D-04 (sentinel -1) | ✅ LimiteExposicaoPercentual with IsUnlimited |
| D-06 (CedenteDocumento DU) | ✅ Discriminated union with Match pattern, zero null risk |
| D-07 (no State Pattern, YAGNI) | ✅ Simple enum + static validator method |