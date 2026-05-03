---
phase: 45-domain-layer-v8
reviewed: 2026-05-03T12:00:00Z
depth: standard
files_reviewed: 30
files_reviewed_list:
  - src/Onboarding.Domain/ValueObjects/LimiteExposicaoPercentual.cs
  - src/Onboarding.Domain/ValueObjects/CedenteDocumento.cs
  - src/Onboarding.Domain/Aggregates/FundoAggregate/FundoStatus.cs
  - src/Onboarding.Domain/Aggregates/FundoAggregate/TipoFundo.cs
  - src/Onboarding.Domain/Aggregates/TipoAtivoAggregate/TipoAtivoCategoria.cs
  - src/Onboarding.Domain/Aggregates/CedenteAggregate/CedenteTipo.cs
  - src/Onboarding.Domain/Exceptions/DomainException.cs
  - src/Onboarding.Domain/Exceptions/InvalidStateTransitionException.cs
  - src/Onboarding.Domain/Exceptions/DuplicateEntityException.cs
  - src/Onboarding.Domain/Aggregates/FundoAggregate/Fundo.cs
  - src/Onboarding.Domain/Aggregates/FundoAggregate/FundoCedente.cs
  - src/Onboarding.Domain/Aggregates/FundoAggregate/FundoCedenteStatus.cs
  - src/Onboarding.Domain/Aggregates/FundoAggregate/FundoTipoAtivo.cs
  - src/Onboarding.Domain/Aggregates/ConsultoriaFundoAggregate/ConsultoriaFundo.cs
  - src/Onboarding.Domain/Aggregates/ConsultoriaFundoAggregate/ConsultoriaFundoStatus.cs
  - src/Onboarding.Domain/Aggregates/CustodianteAggregate/Custodiante.cs
  - src/Onboarding.Domain/Aggregates/CustodianteAggregate/CustodianteStatus.cs
  - src/Onboarding.Domain/Aggregates/CedenteAggregate/Cedente.cs
  - src/Onboarding.Domain/Aggregates/CedenteAggregate/CedenteStatus.cs
  - src/Onboarding.Domain/Aggregates/CedenteAggregate/CedenteTipoAtivo.cs
  - src/Onboarding.Domain/Aggregates/TipoAtivoAggregate/TipoAtivo.cs
  - src/Onboarding.Domain/Aggregates/TipoAtivoAggregate/TipoAtivoStatus.cs
  - src/Onboarding.Domain/Aggregates/EmployeeAggregate/Permissions.cs
  - src/Onboarding.Domain/Aggregates/Audit/ActionType.cs
  - src/Onboarding.Domain/Repositories/IFundoRepository.cs
  - src/Onboarding.Domain/Repositories/IConsultoriaFundoRepository.cs
  - src/Onboarding.Domain/Repositories/ICustodianteRepository.cs
  - src/Onboarding.Domain/Repositories/ICedenteRepository.cs
  - src/Onboarding.Domain/Repositories/ITipoAtivoRepository.cs
  - tests/Onboarding.Domain.Tests/ValueObjects/LimiteExposicaoPercentualTests.cs
  - tests/Onboarding.Domain.Tests/ValueObjects/CedenteDocumentoTests.cs
  - tests/Onboarding.Domain.Tests/Aggregates/FundoStatusTests.cs
  - tests/Onboarding.Domain.Tests/Aggregates/PermissionsTests.cs
  - tests/Onboarding.Domain.Tests/Aggregates/FundoTests.cs
  - tests/Onboarding.Domain.Tests/Aggregates/FundoCedenteTests.cs
  - tests/Onboarding.Domain.Tests/Aggregates/ConsultoriaFundoTests.cs
  - tests/Onboarding.Domain.Tests/Aggregates/CustodianteTests.cs
  - tests/Onboarding.Domain.Tests/Aggregates/CedenteTests.cs
  - tests/Onboarding.Domain.Tests/Aggregates/TipoAtivoTests.cs
findings:
  critical: 1
  warning: 4
  info: 3
  total: 8
status: issues_found
---

# Phase 45: Code Review Report

**Reviewed:** 2026-05-03T12:00:00Z
**Depth:** standard
**Files Reviewed:** 30
**Status:** issues_found

## Summary

Reviewed all 30 source and test files from phase 45 (Domain Layer v8.0 Fundos) at standard depth. The implementation is well-structured overall — sealed aggregate roots, factory methods, whitelist state machine, discriminated union VO, correct multi-tenancy scoping, and thorough test coverage.

One **critical** issue found: `DuplicateCompanyException` does not inherit from the new `DomainException` base class, creating an inconsistency in the exception hierarchy. Four **warnings**: missing self-transition guard in `Fundo.TransitionTo`, `FundoCedente` and join entities not sealed, missing `ClientId` validation in factory methods, and missing `CedenteTipo` enum (declared in CedenteAggregate but never referenced by `Cedente.cs`). Three **info** items noted.

## Critical Issues

### CR-01: DuplicateCompanyException does not inherit DomainException — inconsistent hierarchy

**File:** `src/Onboarding.Domain/Exceptions/DuplicateCompanyException.cs:8`
**Issue:** `DuplicateCompanyException` inherits directly from `Exception`, while the new `DuplicateEntityException` inherits from `DomainException`. This creates an inconsistent exception hierarchy — application layer code that catches `DomainException` to handle business rule violations will miss `DuplicateCompanyException`, leading to unhandled exceptions or separate catch paths for semantically equivalent scenarios (duplicate entity violations).

**Fix:**
```csharp
// DuplicateCompanyException.cs — change base class
public sealed class DuplicateCompanyException : DomainException
{
    public DuplicateCompanyException(string message) : base(message) { }
    public DuplicateCompanyException(string message, Exception inner) : base(message, inner) { }
}
```

This requires adding `using Onboarding.Domain.Exceptions;` (or it may already be in the same namespace) and making `DomainException` constructor `protected` (it already is). Also verify that all callers catching `DuplicateCompanyException` still work — they will, since `DomainException` is a broader catch path.

## Warnings

### WR-01: Fundo.TransitionTo does not guard against self-transition at the call site

**File:** `src/Onboarding.Domain/Aggregates/FundoAggregate/Fundo.cs:66-72`
**Issue:** `Fundo.TransitionTo()` delegates to `FundoStatusValidator.CanTransitionTo()`, which correctly returns `false` for same-status transitions (e.g., `ATIVO → ATIVO`). However, the thrown `InvalidStateTransitionException` message will say `"'Fundo' cannot transition from ATIVO to ATIVO"` — this is technically correct but could be confusing in logs/API responses. More importantly, the `_validTransitions` dictionary in `FundoStatusValidator` does not contain `(ATIVO, ATIVO)` at all — `TryGetValue` returns `false`, and the `&& valid` check evaluates to `false`. This works but is fragile: if someone later changes the dictionary values or adds a transition with `false` value, the logic could break. The current implementation is functionally correct — flagging as warning because the implicit `false` for missing keys is non-obvious.

**Fix:** Document the design contract explicitly with a code comment:
```csharp
/// <summary>
/// Returns true only for whitelisted transitions. Missing entries return false — 
/// this includes self-transitions (same status) and all undefined paths.
/// </summary>
public static bool CanTransitionTo(FundoStatus from, FundoStatus to) =>
    _validTransitions.TryGetValue((from, to), out var valid) && valid;
```

### WR-02: FundoCedente, FundoTipoAtivo, and CedenteTipoAtivo are not sealed

**File:** `src/Onboarding.Domain/Aggregates/FundoAggregate/FundoCedente.cs:11`, `FundoTipoAtivo.cs:9`, `CedenteTipoAtivo.cs:9`
**Issue:** The aggregate roots (`Fundo`, `ConsultoriaFundo`, `Custodiante`, `Cedente`, `TipoAtivo`) are correctly `sealed`. However, the join entities `FundoCedente`, `FundoTipoAtivo`, and `CedenteTipoAtivo` are not sealed. Per the project's established pattern, all domain entities within an aggregate boundary should be sealed to prevent unintended inheritance, consistent with how the aggregate roots are modeled.

**Fix:**
```csharp
// FundoCedente.cs:11
public sealed class FundoCedente : Entity<Guid>

// FundoTipoAtivo.cs:9
public sealed class FundoTipoAtivo : Entity<Guid>

// CedenteTipoAtivo.cs:9
public sealed class CedenteTipoAtivo : Entity<Guid>
```

### WR-03: No validation that clientId is not Guid.Empty in factory methods

**File:** `src/Onboarding.Domain/Aggregates/FundoAggregate/Fundo.cs:32-41`, `ConsultoriaFundo.cs:26-32`, `Custodiante.cs:26-32`, `Cedente.cs:29-35,57-63`
**Issue:** All four company-scoped aggregate roots accept `Guid clientId` / `Guid clientId` as a required parameter, but none validate that `clientId != Guid.Empty`. An empty GUID would result in a `ClienteId` of `00000000-0000-0000-0000-000000000000` in the database, which would fail tenant isolation silently or cause subtle bugs in the Infrastructure layer's `HasQueryFilter`. This is a defense-in-depth concern — the API layer should validate, but domain should also enforce its own invariants.

**Fix:** Add validation in each factory method:
```csharp
public static Fundo Register(string nome, string cnpj, Guid clientId, ...)
{
    if (clientId == Guid.Empty)
        throw new ArgumentException("ClientId is required.", nameof(clientId));
    // ... existing validation ...
}
```

Apply the same pattern to `ConsultoriaFundo.Register()`, `Custodiante.Register()`, `Cedente.RegisterPf()`, and `Cedente.RegisterPj()`.

### WR-04: CedenteTipo enum defined but never referenced by Cedente aggregate

**File:** `src/Onboarding.Domain/Aggregates/CedenteAggregate/CedenteTipo.cs`
**Issue:** The `CedenteTipo` enum (`PF`, `PJ`) was created but is never used in `Cedente.cs`. The `CedenteDocumento` discriminated union already provides a type-safe PF/PJ distinction via `IsPf`/`IsPj` properties and the `Match` method. The `CedenteTipo` enum duplicates this information without being referenced. This creates a confusion risk: future developers might think they should add a `CedenteTipo` property to `Cedente`, but the document DU already handles this.

**Fix:** Either remove `CedenteTipo.cs` (since `CedenteDocumento` already encodes PF/PJ), or add a computed property to `Cedente` that derives `CedenteTipo` from the `Documento` for convenience:
```csharp
// Option A: Remove CedenteTipo.cs entirely
// Option B: Add computed property to Cedente
public CedenteTipo Tipo => Documento.IsPf ? CedenteTipo.PF : CedenteTipo.PJ;
```

## Info

### IN-01: DomainException message strings contain user-facing language (Portuguese)

**File:** `src/Onboarding.Domain/Exceptions/InvalidStateTransitionException.cs:16`, `DuplicateEntityException.cs:15`
**Issue:** Exception messages like `"'Fundo' cannot transition from ATIVO to SUSPENSO"` and `"'CedenteTipoAtivo' with key '...' already exists"` contain structured data. The threat model (T-45-04) notes these messages should not be exposed to API consumers directly — controllers must translate. This is already documented in comments on `DomainException.cs` and `DuplicateEntityException.cs`, so the risk is mitigated by design. Flagging as info to confirm the Application/API layer hasn't been built yet (it comes in later phases).

**Fix:** No code change needed — existing documentation is adequate. Ensure API layer maps `DomainException` → appropriate HTTP status codes without leaking message text.

### IN-02: FundoCedente.Create and FundoTipoAtivo.Create are `internal` — correct but limits testability

**File:** `src/Onboarding.Domain/Aggregates/FundoAggregate/FundoCedente.cs:23`, `FundoTipoAtivo.cs:16`, `CedenteTipoAtivo.cs:16`
**Issue:** Factory methods are `internal`, which is exactly correct — these are managed through their aggregate root (`Fundo.AddCedente()`, `Fundo.AddTipoAtivo()`, `Cedente.AddTipoAtivo()`). Tests correctly go through the aggregate root. This is the right DDD pattern. Flagging as info only to confirm this was intentional.

**Fix:** No change needed — pattern is correct.

### IN-03: ICustodianteRepository has inconsistent parameter name

**File:** `src/Onboarding.Domain/Repositories/ICustodianteRepository.cs:12`
**Issue:** The `SaveAsync` method has parameter name `custodiante` (with 'e' at the end) while the interface name is `ICustodianteRepository` and other methods use `Custodiante` type. The parameter `Task SaveAsync(Custodiante custodiante, ...)` uses the Portuguese feminine noun form correctly, but this is inconsistent with the type name `Custodiante` — both are the same word, just the parameter is lowercase. This is actually correct Portuguese noun-adjective agreement and not a bug. Flagging only because the type name and parameter name are identical except for case, which is standard C# convention.

**Fix:** No change needed — follows standard C# naming conventions.

---

_Reviewed: 2026-05-03T12:00:00Z_
_Reviewer: the agent (gsd-code-reviewer)_
_Depth: standard_