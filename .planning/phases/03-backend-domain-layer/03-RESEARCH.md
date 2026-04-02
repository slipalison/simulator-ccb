# Phase 3: Backend Domain Layer - Research

**Researched:** 2026-04-02
**Domain:** DDD domain model, value objects, CQRS without MediatR, CPF/CNPJ validation algorithms
**Confidence:** HIGH

---

## Project Constraints (from CLAUDE.md)

### Mandatory Directives
- **Runtime**: .NET 10 (net10.0 target framework) — user-specified
- **API Style**: ASP.NET Controllers only — Minimal API explicitly excluded
- **CQRS**: Manual DI only — MediatR excluded (commercial license)
- **Testing**: xUnit + Shouldly (MIT) — FluentAssertions excluded (Xceed commercial)
- **Mocking**: NSubstitute — Moq excluded (SponsorLink controversy)
- **ORM**: Entity Framework Core 10 — Dapper alongside EF excluded
- **Logging**: Serilog + OpenTelemetry — mandatory from the start
- **License rule**: All NuGet packages must be Apache 2.0 / MIT or equivalent permissive

### What NOT to Use (relevant to this phase)
| Package | Reason |
|---------|--------|
| MediatR | Commercial license — use manual CQRS via DI |
| FluentAssertions | v8+ commercial (Xceed) — use Shouldly |
| Moq | SponsorLink controversy — use NSubstitute |
| ASP.NET Core Identity | Keycloak manages users — don't mix identity systems |

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| BACK-01 | Arquitetura DDD — Domain, Application, Infrastructure, API layers | Layer boundary enforcement via project references; Domain has no external deps |
| BACK-02 | Value objects: CPF, CNPJ, Email, Phone com auto-validação | CPF mod-11 algorithm; alphanumeric CNPJ mod-11 (July 2026 format); record types in C# 10+ |
| BACK-03 | Client aggregate com factory methods (RegisterPessoaFisica, RegisterPessoaJuridica) | Aggregate root pattern with private constructor + static factory methods |
| BACK-04 | TDD — testes unitários no domain, integração nos endpoints | xUnit 2.9.3 + Shouldly 4.3.0; Theory/InlineData for invalid inputs |
| BACK-06 | CQRS manual via DI (commands/handlers injetados diretamente, sem MediatR) | ICommandHandler<TCommand,TResult> pattern wired via AddScoped in DI |
</phase_requirements>

---

## Summary

Phase 3 builds the domain model (Onboarding.Domain) and application layer (Onboarding.Application) with full unit test coverage. Both projects are already scaffolded as empty `.csproj` files — the Application project already references Domain. The test project for the domain does not yet exist and must be created.

The core technical work splits into three areas: (1) CPF and CNPJ value objects implementing the modulo-11 check-digit algorithm (including the July 2026 alphanumeric CNPJ variant), (2) the `Client` aggregate root with two factory methods enforcing invariants, and (3) a CQRS command/handler pair wired through .NET's built-in DI — no MediatR.

**Primary recommendation:** Use C# `record` types for value objects (immutable by design, structural equality built-in), a plain POCO class deriving from an `Entity<Guid>` base for the aggregate, and hand-rolled `ICommandHandler<TCommand, TResult>` interfaces registered with `AddScoped`. Add a dedicated unit test project (`Onboarding.Domain.Tests`) with xUnit 2.9.3 + Shouldly 4.3.0. The alphanumeric CNPJ format (effective July 2026) must be handled from the start per REQUIREMENTS (REG-04).

---

## Standard Stack

### Core (verified against NuGet registry 2026-04-02)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| xunit | 2.9.3 | Unit test runner | Microsoft default; most widely used in .NET ecosystem |
| xunit.runner.visualstudio | 3.1.5 | VS/dotnet test adapter | Required for `dotnet test` to discover xUnit tests |
| Microsoft.NET.Test.Sdk | 18.3.0 | MSBuild test integration | Required boilerplate for any .NET test project |
| Shouldly | 4.3.0 | Assertion library (MIT) | Project-mandated replacement for FluentAssertions |
| NSubstitute | 5.3.0 | Mocking (MIT) | Project-mandated replacement for Moq |
| coverlet.collector | 8.0.1 | Code coverage | Standard coverage collector |
| FluentValidation | 12.1.1 | Command DTO validation in Application layer | Apache 2.0 — complies with OSS-only rule |

> **Version note for CLAUDE.md**: CLAUDE.md lists FluentValidation 11.x but 12.1.1 is current on NuGet (Apache 2.0 license confirmed). Use 12.1.1.

### Domain Layer (no external packages required)
The Domain project must have **zero NuGet dependencies** — pure C# only. All value object validation is hand-rolled (CPF/CNPJ algorithms).

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| coverlet.msbuild | 6.0.4 | Alternative coverage driver | Only if coverlet.collector has issues |

### Installation (new test project)
```bash
dotnet new xunit -n Onboarding.Domain.Tests -o tests/Onboarding.Domain.Tests --framework net10.0
dotnet add tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj reference src/Onboarding.Domain/Onboarding.Domain.csproj
dotnet add tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj reference src/Onboarding.Application/Onboarding.Application.csproj
dotnet add tests/Onboarding.Domain.Tests package Shouldly --version 4.3.0
dotnet add tests/Onboarding.Domain.Tests package NSubstitute --version 5.3.0
dotnet add tests/Onboarding.Domain.Tests package Microsoft.NET.Test.Sdk --version 18.3.0
dotnet add tests/Onboarding.Domain.Tests package xunit.runner.visualstudio --version 3.1.5
dotnet add tests/Onboarding.Domain.Tests package coverlet.collector --version 8.0.1
dotnet sln Onboarding.slnx add tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj
# FluentValidation in Application layer only
dotnet add src/Onboarding.Application package FluentValidation --version 12.1.1
dotnet add src/Onboarding.Application package FluentValidation.DependencyInjectionExtensions --version 12.1.1
```

---

## Architecture Patterns

### Recommended Project Structure
```
src/
├── Onboarding.Domain/               # Zero external dependencies — pure C#
│   ├── Common/
│   │   ├── Entity.cs                # Base class with Guid Id, private setter
│   │   └── ValueObject.cs           # Optional base (or use record types)
│   ├── Aggregates/
│   │   └── ClientAggregate/
│   │       ├── Client.cs            # Aggregate root
│   │       ├── ClientType.cs        # enum: PessoaFisica | PessoaJuridica
│   │       └── ClientStatus.cs      # enum: Active | Inactive (future-proof)
│   ├── ValueObjects/
│   │   ├── Cpf.cs
│   │   ├── Cnpj.cs
│   │   ├── Email.cs
│   │   └── PhoneNumber.cs
│   └── Repositories/
│       └── IClientRepository.cs     # Interface only — no implementation here
│
├── Onboarding.Application/          # References Domain only
│   ├── Clients/
│   │   ├── Commands/
│   │   │   ├── RegisterClientCommand.cs
│   │   │   └── RegisterClientCommandHandler.cs
│   │   └── DTOs/
│   │       └── RegisterClientDto.cs (input/output records)
│   ├── Common/
│   │   ├── ICommandHandler.cs
│   │   └── IQueryHandler.cs
│   └── DependencyInjection.cs       # AddApplication() extension method
│
tests/
└── Onboarding.Domain.Tests/
    ├── ValueObjects/
    │   ├── CpfTests.cs
    │   ├── CnpjTests.cs
    │   ├── EmailTests.cs
    │   └── PhoneNumberTests.cs
    └── Aggregates/
        └── ClientTests.cs
```

### Pattern 1: Value Object as C# Record

C# `record` types are the idiomatic value object: structural equality, immutability, and concise syntax.
Validation happens in the constructor via `static Create` factory or constructor guard.

```csharp
// Source: Microsoft DDD guidance + C# record pattern
public sealed record Cpf
{
    public string Value { get; }

    private Cpf(string value) => Value = value;

    public static Cpf Create(string raw)
    {
        var digits = raw?.Replace(".", "").Replace("-", "") ?? "";
        if (!IsValid(digits))
            throw new ArgumentException($"CPF inválido: '{raw}'");
        return new Cpf(digits);
    }

    private static bool IsValid(string digits) { /* see CPF algorithm */ }

    public override string ToString() => Value;
}
```

**When to use:** All value objects (Cpf, Cnpj, Email, PhoneNumber). Records provide equality semantics for free.

### Pattern 2: Aggregate Root with Private Constructor + Factory Methods

```csharp
// Source: Microsoft eShopOnContainers DDD pattern
public sealed class Client : Entity<Guid>
{
    public string Name { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public PhoneNumber Phone { get; private set; } = default!;
    public ClientType Type { get; private set; }

    // PF-specific (null for PJ)
    public Cpf? Cpf { get; private set; }

    // PJ-specific (null for PF)
    public Cnpj? Cnpj { get; private set; }
    public string? RazaoSocial { get; private set; }

    // Private constructor: prevents external construction
    private Client() { }

    public static Client RegisterPessoaFisica(
        string nome, string cpf, string email, string phone)
    {
        // Invariant enforcement happens here via value object constructors
        return new Client
        {
            Id = Guid.NewGuid(),
            Name = nome ?? throw new ArgumentNullException(nameof(nome)),
            Cpf = Cpf.Create(cpf),
            Email = Email.Create(email),
            Phone = PhoneNumber.Create(phone),
            Type = ClientType.PessoaFisica
        };
    }

    public static Client RegisterPessoaJuridica(
        string razaoSocial, string cnpj, string email, string phone)
    {
        return new Client
        {
            Id = Guid.NewGuid(),
            Name = razaoSocial ?? throw new ArgumentNullException(nameof(razaoSocial)),
            Cnpj = Cnpj.Create(cnpj),
            Email = Email.Create(email),
            Phone = PhoneNumber.Create(phone),
            Type = ClientType.PessoaJuridica,
            RazaoSocial = razaoSocial
        };
    }
}
```

### Pattern 3: CQRS Manual DI — ICommandHandler Interface

```csharp
// Source: dotnetcopilot.com — CQRS without MediatR in .NET 10

// In Onboarding.Application/Common/
public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}

public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}

// Unit type for void commands
public readonly struct Unit
{
    public static readonly Unit Value = new();
}

// Command record
public sealed record RegisterClientCommand(
    string Nome,
    string? Cpf,
    string? Cnpj,
    string? RazaoSocial,
    string Email,
    string Phone,
    string Password) : ICommand;

// Handler — lives in Application, depends on Domain interfaces
public sealed class RegisterClientCommandHandler
    : ICommandHandler<RegisterClientCommand, Guid>
{
    private readonly IClientRepository _repository;

    public RegisterClientCommandHandler(IClientRepository repository)
        => _repository = repository;

    public async Task<Guid> HandleAsync(
        RegisterClientCommand command, CancellationToken ct = default)
    {
        var client = command.Cpf is not null
            ? Client.RegisterPessoaFisica(command.Nome, command.Cpf, command.Email, command.Phone)
            : Client.RegisterPessoaJuridica(command.RazaoSocial!, command.Cnpj!, command.Email, command.Phone);

        await _repository.AddAsync(client, ct);
        return client.Id;
    }
}

// Registration in DependencyInjection.cs
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<
            ICommandHandler<RegisterClientCommand, Guid>,
            RegisterClientCommandHandler>();
        return services;
    }
}
```

**Key insight:** The controller injects `ICommandHandler<RegisterClientCommand, Guid>` directly — no dispatcher required for simple cases. A dispatcher can be added later for cross-cutting concerns without changing the handler interface.

### Pattern 4: Entity Base Class

```csharp
// Domain/Common/Entity.cs — zero external dependencies
public abstract class Entity<TId> where TId : struct
{
    public TId Id { get; protected set; }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => left?.Equals(right) ?? right is null;

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        => !(left == right);
}
```

### Anti-Patterns to Avoid
- **Public setters on aggregate properties**: Bypass invariant enforcement. All setters must be `private set`.
- **Domain depending on Application/Infrastructure**: The Domain `.csproj` must have zero `<ProjectReference>` entries.
- **Anemic domain model**: Business logic (CPF check digit) inside Application or Infrastructure instead of the value object itself.
- **Using `new` to construct Client outside the factory**: EF Core needs special handling (see Pitfall 3).
- **Throwing domain exceptions that carry too much info**: Use generic messages to avoid information leakage (SEC-08 preparation).

---

## CPF Validation Algorithm

### Algorithm (HIGH confidence — cross-referenced with Receita Federal specification)

CPF format: `NNN.NNN.NNN-DD` (11 digits total; first 9 are base, last 2 are check digits).

**Edge case rejections (must check first):**
- All-same-digit sequences are invalid: `000.000.000-00`, `111.111.111-11`, ..., `999.999.999-99`
- Length != 11 digits (after stripping `.` and `-`)

**First check digit (D1):**
```
Weights for positions 0-8: [10, 9, 8, 7, 6, 5, 4, 3, 2]
sum = Σ(digit[i] × weight[i]) for i in 0..8
remainder = sum % 11
D1 = (remainder < 2) ? 0 : (11 - remainder)
D1 must equal digit[9]
```

**Second check digit (D2):**
```
Weights for positions 0-9: [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]
sum = Σ(digit[i] × weight[i]) for i in 0..9
remainder = sum % 11
D2 = (remainder < 2) ? 0 : (11 - remainder)
D2 must equal digit[10]
```

**C# implementation sketch:**
```csharp
private static bool IsValid(string digits)
{
    if (digits.Length != 11) return false;
    if (!digits.All(char.IsDigit)) return false;
    // Reject all-same-digit (000...0, 111...1, etc.)
    if (digits.Distinct().Count() == 1) return false;

    static int CalcDigit(string d, int[] weights)
    {
        var sum = weights.Select((w, i) => (d[i] - '0') * w).Sum();
        var rem = sum % 11;
        return rem < 2 ? 0 : 11 - rem;
    }

    var w1 = new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
    var w2 = new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

    var d1 = CalcDigit(digits, w1);
    if (d1 != digits[9] - '0') return false;

    var d2 = CalcDigit(digits, w2);
    return d2 == digits[10] - '0';
}
```

---

## CNPJ Validation Algorithm

### Numeric CNPJ (current + backward compatible) — HIGH confidence

CNPJ format: `NN.NNN.NNN/NNNN-DD` (14 characters; first 12 are base, last 2 are check digits).

**Edge case rejections:** All-same-digit sequences are invalid (same rule as CPF).

**First check digit (D1):**
```
Weights for positions 0-11: [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]
sum = Σ(digit[i] × weight[i]) for i in 0..11
remainder = sum % 11
D1 = (remainder < 2) ? 0 : (11 - remainder)
```

**Second check digit (D2):**
```
Weights for positions 0-12: [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]
sum = Σ(digit[i] × weight[i]) for i in 0..12
remainder = sum % 11
D2 = (remainder < 2) ? 0 : (11 - remainder)
```

### Alphanumeric CNPJ (effective July 2026) — MEDIUM confidence

**Source:** GitHub — FRACerqueira/CnpjAlfaNumerico (C# implementation); KPMG tax advisory; Fiscal Solutions.

**What changes:** The first 8 characters of the base (positions 0-7) may now be letters A-Z in addition to digits 0-9. The last 4 characters of the base (branch qualifier, positions 8-11) remain numeric. The 2 check digits remain numeric.

**Character mapping:**
```
ASCII value of character - 48
  Digits 0-9  → values 0-9  (same as numeric)
  Letters A-Z → values 17-42 (A='A'(65)-48=17, B=18, ..., Z=42)
```

**Algorithm:** Identical to numeric CNPJ modulo-11, substituting each character's mapped value in place of its digit value. This ensures backward compatibility: all-numeric CNPJs produce the same check digits under both systems.

**C# implementation sketch:**
```csharp
private static int CharValue(char c) => c - 48; // Works for '0'-'9' and 'A'-'Z'

private static bool IsValidAlphanumericCnpj(string cnpj)
{
    // cnpj: 14 chars, positions 0-7 alphanumeric, 8-11 numeric, 12-13 check digits
    if (cnpj.Length != 14) return false;
    if (cnpj.Distinct().Count() == 1 && char.IsDigit(cnpj[0])) return false; // all-same numeric

    static int CalcDigit(string s, int[] weights)
    {
        var sum = weights.Select((w, i) => CharValue(s[i]) * w).Sum();
        var rem = sum % 11;
        return rem < 2 ? 0 : 11 - rem;
    }

    var w1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
    var w2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

    var d1 = CalcDigit(cnpj, w1);
    if (d1 != cnpj[12] - '0') return false;
    var d2 = CalcDigit(cnpj, w2);
    return d2 == cnpj[13] - '0';
}
```

**Practical approach for the CNPJ value object:** A single `IsValid` method that uses the alphanumeric algorithm — since the ASCII-48 mapping is backward-compatible, this handles both numeric and alphanumeric CNPJs without branching.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Test assertions | Custom matcher helpers | Shouldly 4.3.0 | Should.Be, ShouldThrow, ShouldNotThrow — concise, readable error messages |
| Mock objects | Manual stub classes | NSubstitute 5.3.0 | Type-safe, no code generation step needed |
| Command validation | if-else chains in handler | FluentValidation 12.1.1 | Composable, testable validation rules; integrates with Application layer |
| Structural equality on value objects | Manual Equals/GetHashCode | C# `record` types | Records provide structural equality, immutability, and deconstruct for free |

**Key insight:** CPF and CNPJ algorithms are intentionally hand-rolled — they are core domain rules. No third-party validation library should wrap them, as that would create an external dependency in the Domain layer.

---

## Common Pitfalls

### Pitfall 1: Domain Project Accruing External Dependencies
**What goes wrong:** A developer adds `FluentValidation` or `Microsoft.Extensions.Logging` to Onboarding.Domain.csproj to "keep validation together."
**Why it happens:** Convenience — validation and logging abstractions are small, feel harmless.
**How to avoid:** Domain has zero `<PackageReference>` entries. If you find yourself adding one, it belongs in Application or Infrastructure.
**Warning signs:** `dotnet list src/Onboarding.Domain package` shows any output.

### Pitfall 2: CPF/CNPJ All-Same-Digit Edge Case
**What goes wrong:** The modulo-11 algorithm happens to produce valid check digits for "000.000.000-00" — it's mathematically valid but legally invalid.
**Why it happens:** Tests only check known-valid and known-invalid values, not edge cases.
**How to avoid:** Add `digits.Distinct().Count() == 1` rejection before the math. Test it explicitly with `[InlineData("00000000000")]`.
**Warning signs:** `Cpf.Create("000.000.000-00")` does not throw.

### Pitfall 3: EF Core Cannot Construct Aggregate via Private Constructor
**What goes wrong:** Phase 5 (persistence) fails because EF Core cannot instantiate `Client` — private constructor + no parameterless constructor.
**Why it happens:** EF Core 7+ supports private parameterless constructors but needs special configuration or a protected parameterless constructor.
**How to avoid:** Add a `protected Client() { }` constructor (EF-only entry point) alongside the private aggregate logic. This is a deliberate DDD + EF Core convention.
**Warning signs:** EF Core throws "No suitable constructor found for entity type 'Client'" at runtime.

### Pitfall 4: xUnit v2 vs v3 Package Confusion
**What goes wrong:** `xunit.runner.visualstudio 3.1.5` is installed but the test project targets `xunit 2.9.3`, causing test discovery failures.
**Why it happens:** The runner package version is independent of the core xunit package version. v3 runner works with v2 tests, but the package names diverge in xUnit v3 (xunit.v3 vs xunit).
**How to avoid:** Use `xunit 2.9.3` (stable) + `xunit.runner.visualstudio 3.1.5` (compatible). Do not mix `xunit.v3.*` packages with `xunit 2.9.x`.
**Warning signs:** `dotnet test` runs zero tests.

### Pitfall 5: CNPJ Alphanumeric Format — Mask Stripping
**What goes wrong:** The string `"12.ABC.456/0001-00"` is stripped of `.`, `/`, `-` before validation, but letters `A`, `B`, `C` remain — the code handles them as unknown chars.
**Why it happens:** Numeric mask stripping is a one-liner; alphanumeric needs explicit handling.
**How to avoid:** Normalize to uppercase, strip only `.`, `/`, `-`, then validate that remaining chars are `[A-Z0-9]` before the algorithm. Reject any other character.
**Warning signs:** `Cnpj.Create("12.abc.456/0001-00")` either throws or silently produces wrong result.

### Pitfall 6: CQRS Handler Registered as Transient Instead of Scoped
**What goes wrong:** `AddTransient<ICommandHandler<RegisterClientCommand, Guid>, RegisterClientCommandHandler>()` — the repository inside may have scoped lifetime, causing a "captured dependency" DI error at runtime.
**Why it happens:** Transient is the default many developers reach for; EF Core DbContext is Scoped.
**How to avoid:** Always `AddScoped` for command handlers when they depend on EF Core (DbContext is scoped per request).
**Warning signs:** InvalidOperationException about captured scoped service in singleton/transient.

---

## Code Examples

### xUnit + Shouldly Test for Value Object

```csharp
// Source: xUnit.net docs + Shouldly docs
public class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25")]   // valid
    [InlineData("52998224725")]      // valid no mask
    public void Create_ValidCpf_ReturnsInstance(string input)
    {
        var cpf = Cpf.Create(input);
        cpf.ShouldNotBeNull();
        cpf.Value.ShouldBe("52998224725");
    }

    [Theory]
    [InlineData("000.000.000-00")]   // all-same
    [InlineData("111.111.111-11")]   // all-same
    [InlineData("529.982.247-26")]   // wrong check digit
    [InlineData("123")]              // wrong length
    [InlineData("")]
    [InlineData(null)]
    public void Create_InvalidCpf_Throws(string? input)
    {
        Should.Throw<ArgumentException>(() => Cpf.Create(input!));
    }

    [Fact]
    public void TwoCpfsWithSameValue_AreEqual()
    {
        var a = Cpf.Create("529.982.247-25");
        var b = Cpf.Create("529.982.247-25");
        a.ShouldBe(b);       // Record structural equality
    }
}
```

### Layer Boundary Enforcement Test (compile-time)

The project reference graph enforces boundaries at build time:
- `Onboarding.Domain.csproj` — zero `<ProjectReference>` entries
- `Onboarding.Application.csproj` — one reference to Domain
- `Onboarding.Infrastructure.csproj` — references Domain (and optionally Application)
- `Onboarding.API.csproj` — references Application and Infrastructure

A ArchUnit-style test is optional but the project reference constraint is the primary guard.

### DI Registration Pattern

```csharp
// Onboarding.Application/DependencyInjection.cs
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register all command handlers here
        services.AddScoped<
            ICommandHandler<RegisterPessoaFisicaCommand, Guid>,
            RegisterPessoaFisicaCommandHandler>();

        services.AddScoped<
            ICommandHandler<RegisterPessoaJuridicaCommand, Guid>,
            RegisterPessoaJuridicaCommandHandler>();

        // FluentValidation auto-registration
        services.AddValidatorsFromAssemblyContaining<RegisterPessoaFisicaCommandValidator>();

        return services;
    }
}
```

---

## Runtime State Inventory

> This is a greenfield phase. No runtime state migration applies. Skipped.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | All .NET compilation and tests | ✓ | 10.0.201 | — |
| dotnet-ef (global tool) | Not needed for this phase | ✓ | 10.0.5 | — |
| Docker | Not needed for this phase | Not checked | — | Not needed |
| PostgreSQL | Not needed for this phase | Not needed | — | — |

**Missing dependencies with no fallback:** None.

**Notes:** This phase requires only the .NET SDK. No database or container runtime is needed — success criterion explicitly states "no database or network dependencies."

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 |
| Config file | `xunit.runner.json` (none yet — Wave 0 creates it) |
| Quick run command | `dotnet test tests/Onboarding.Domain.Tests/ --no-build` |
| Full suite command | `dotnet test tests/Onboarding.Domain.Tests/` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| BACK-02 | `Cpf.Create` rejects bad check digit | unit | `dotnet test --filter "FullyQualifiedName~CpfTests"` | ❌ Wave 0 |
| BACK-02 | `Cpf.Create` rejects all-same-digit (000..0) | unit | `dotnet test --filter "FullyQualifiedName~CpfTests"` | ❌ Wave 0 |
| BACK-02 | `Cnpj.Create` rejects bad check digit | unit | `dotnet test --filter "FullyQualifiedName~CnpjTests"` | ❌ Wave 0 |
| BACK-02 | `Cnpj.Create` accepts alphanumeric CNPJ (2026 format) | unit | `dotnet test --filter "FullyQualifiedName~CnpjTests"` | ❌ Wave 0 |
| BACK-02 | `Email.Create` rejects invalid formats | unit | `dotnet test --filter "FullyQualifiedName~EmailTests"` | ❌ Wave 0 |
| BACK-03 | `Client.RegisterPessoaFisica` creates valid aggregate | unit | `dotnet test --filter "FullyQualifiedName~ClientTests"` | ❌ Wave 0 |
| BACK-03 | `Client.RegisterPessoaJuridica` creates valid aggregate | unit | `dotnet test --filter "FullyQualifiedName~ClientTests"` | ❌ Wave 0 |
| BACK-03 | `Client.RegisterPessoaFisica` with invalid CPF throws | unit | `dotnet test --filter "FullyQualifiedName~ClientTests"` | ❌ Wave 0 |
| BACK-04 | Domain compiles with no external package references | unit (build) | `dotnet build src/Onboarding.Domain/` | ❌ Wave 0 |
| BACK-06 | `RegisterClientCommandHandler.HandleAsync` delegates to repository | unit | `dotnet test --filter "FullyQualifiedName~RegisterClientCommandHandlerTests"` | ❌ Wave 0 |
| BACK-01 | Application project only references Domain | build | `dotnet build src/Onboarding.Application/` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test tests/Onboarding.Domain.Tests/ --no-build -x`
- **Per wave merge:** `dotnet test tests/Onboarding.Domain.Tests/`
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `tests/Onboarding.Domain.Tests/` — entire test project does not yet exist
- [ ] `tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj` — covers all BACK-0x tests
- [ ] Framework install: `dotnet new xunit -n Onboarding.Domain.Tests -o tests/Onboarding.Domain.Tests --framework net10.0`

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Value objects as class with Equals override | C# `record` types | .NET 5 / C# 9 | Structural equality, immutability, and deconstruct built in |
| MediatR for CQRS | Manual DI with ICommandHandler<T,R> | 2024 (MediatR commercial) | No change to architecture; slightly more boilerplate in DI wiring |
| FluentValidation 11.x | FluentValidation 12.1.1 (Apache 2.0) | Dec 2025 | Minor API changes; CLAUDE.md references 11.x but 12.1.1 is current |
| CNPJ numeric-only | CNPJ alphanumeric (ASCII-48 mapping) | July 2026 (effective) | Must handle from the start per REG-04 |
| xUnit v2 only | xUnit v2 (stable) + v3 runner | 2025 | v3 runner works with v2 tests; v3 core requires exe project output type |

**Deprecated/outdated:**
- `xunit.runner.visualstudio 2.x`: Superseded by 3.1.5 (backward-compatible with xunit 2.9.3)
- CNPJ numeric-only validators: Must support alphanumeric by July 2026 (build it right from day one)

---

## Open Questions

1. **Single RegisterClientCommand vs. separate PF/PJ commands**
   - What we know: Phase 3 scope is domain + one CQRS command for registration (BACK-06)
   - What's unclear: Whether one `RegisterClientCommand` with a discriminator field or two commands (`RegisterPessoaFisicaCommand`, `RegisterPessoaJuridicaCommand`) is preferred
   - Recommendation: Two separate commands — type safety, no nullable fields, clearer validation rules per type. Planner decides.

2. **IClientRepository interface scope**
   - What we know: Repository interface belongs in Domain; implementation in Infrastructure (Phase 5)
   - What's unclear: Whether Phase 3 should define the full `IClientRepository` contract now or keep it minimal
   - Recommendation: Define `AddAsync(Client client, CancellationToken ct)` and `ExistsWithCpfAsync / ExistsWithCnpjAsync` in Phase 3 — these are needed by the handler stub. No implementation yet.

3. **Domain exceptions vs. standard exceptions**
   - What we know: Value object constructors throw on invalid input
   - What's unclear: Whether to use `ArgumentException` (standard) or a custom `DomainException` hierarchy
   - Recommendation: `ArgumentException` for this phase; a custom `DomainException` base class is optional and can be added without breaking changes. Planner decides.

---

## Sources

### Primary (HIGH confidence)
- [Microsoft .NET Architecture — DDD domain model](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/net-core-microservice-domain-model) — Aggregate root pattern, value objects, private setters, factory methods
- NuGet registry (2026-04-02 via `dotnet package search`) — xunit 2.9.3, Shouldly 4.3.0, NSubstitute 5.3.0, FluentValidation 12.1.1, coverlet.collector 8.0.1, Microsoft.NET.Test.Sdk 18.3.0, xunit.runner.visualstudio 3.1.5

### Secondary (MEDIUM confidence)
- [FRACerqueira/CnpjAlfaNumerico on GitHub](https://github.com/FRACerqueira/CnpjAlfaNumerico) — Alphanumeric CNPJ C# implementation; ASCII-48 mapping (A=17..Z=42); verified against KPMG advisory
- [KPMG — Brazil: New alphanumeric format for taxpayer registry](https://kpmg.com/us/en/taxnewsflash/news/2026/03/tnf-brazi-new-alphanumeric-format-for-taxpayer-registry.html) — July 2026 effective date confirmed
- [dotnetcopilot.com — CQRS without MediatR in .NET 10](https://dotnetcopilot.com/implementing-cqrs-without-mediatr-in-net-10-using-clean-architecture/) — ICommandHandler<TCommand,TResult> interface + Dispatcher pattern
- [DEV.to — Demystifying CPF and CNPJ Check Digit Algorithms](https://dev.to/leandrostl/demystifying-cpf-and-cnpj-check-digit-algorithms-a-clear-and-concise-approach-f3j) — Unified algorithm explanation
- [xUnit.net — What's New in v3](https://xunit.net/docs/getting-started/v3/whats-new) — v2 vs v3 compatibility information
- [Fiscal Solutions — CNPJ alphanumeric format](https://www.fiscal-requirements.com/news/5177) — July 2026 deadline and backward compatibility rules

### Tertiary (LOW confidence — cross-verified where possible)
- [Milan Jovanovic — Stop Conflating CQRS and MediatR](https://www.milanjovanovic.tech/blog/stop-conflating-cqrs-and-mediatr) — CQRS as pattern vs. library

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all versions verified against NuGet registry 2026-04-02
- CPF algorithm: HIGH — well-documented, cross-verified with multiple sources
- CNPJ alphanumeric algorithm: MEDIUM — official Receita Federal spec not directly accessed; algorithm verified via C# open-source implementation (FRACerqueira/CnpjAlfaNumerico) and KPMG advisory
- Architecture patterns: HIGH — Microsoft eShopOnContainers reference pattern; used across .NET ecosystem
- CQRS without MediatR: HIGH — straightforward DI pattern, multiple verified sources
- Pitfalls: HIGH — identified from codebase constraints (EF Core private constructor, all-same CPF, DI scoping)

**Research date:** 2026-04-02
**Valid until:** 2026-07-01 (stable domain — main expiry trigger is CNPJ July 2026 effective date)
