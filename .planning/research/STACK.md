# Technology Stack — Fundos de Investimento Module

**Project:** Onboarding v8.0 — Fundos de Investimento
**Researched:** 2026-05-02
**Scope:** NEW additions only — existing stack fully covers the domain

---

## Executive Summary

**No new NuGet packages are required.** The existing stack (EF Core 10.0.7, Npgsql 10.0.1, FluentValidation 12.1.1, xUnit/Shouldly/NSubstitute/Testcontainers) fully supports everything the Fundos module needs. The work is entirely about modeling domain concepts correctly — aggregates, value objects, enums, join entities, EF Core configurations, and decimal precision mapping.

The three key technical decisions for this module are:

1. **N-N relationships with payload** → explicit join entities (EF Core has no `HasMany().WithMany()` with payload syntax). Each join table (FundoCedente, CedenteTipoAtivo, FundoTipoAtivo) becomes its own entity inheriting from `Entity<Guid>`.

2. **Decimal precision for monetary values** → EF Core `.HasPrecision(p, s)` maps to PostgreSQL `numeric(p,s)`. Use `numeric(18,2)` for PL/limits, `numeric(20,8)` for quota values per CVM巴西监管惯例.

3. **Brazilian financial domain enums** → `StatusFundo`, `TipoFundo`, `ClasseAnbima` as C# enums in Domain layer, mapped to PostgreSQL `integer` columns (EF Core default for enums). Not strings — performance + referential integrity.

---

## Existing Stack — Confirmed Compatible

These packages are already in the project and require NO version changes:

| Package | Current Version | Role in Fundos Module | Status |
|---------|----------------|----------------------|--------|
| Microsoft.EntityFrameworkCore | 10.0.7 | ORM — N-N join entities, decimal precision, migrations | ✅ No change |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 | PostgreSQL provider — `numeric(p,s)` mapping native | ✅ No change |
| FluentValidation | 12.1.1 | Command/DTO validation — fund registration, limits | ✅ No change |
| xUnit | 2.9.3 | Test framework — TDD for domain entities, handlers | ✅ No change |
| Shouldly | 4.3.0 | Test assertions — value object, enum, decimal equality | ✅ No change |
| NSubstitute | 5.3.0 | Mocking — repository mocks in handler unit tests | ✅ No change |
| Testcontainers.PostgreSql | 4.11.0 | Integration tests — full DB round-trip for N-N joins | ✅ No change |
| Serilog / OpenTelemetry | 4.3.1 / 1.15.x | Structured logging + traces — fund CRUD operations | ✅ No change |

**Confidence: HIGH** — Verified against NuGet current versions and Context7 docs. All packages are stable, .NET 10 compatible, and have native support for everything this module needs.

---

## New Code Additions (No Packages — Domain Modeling)

### 1. Enums in Domain Layer

| Enum | Values | Purpose | Storage |
|------|--------|---------|---------|
| `StatusFundo` | `Ativo = 1`, `Inativo = 2`, `EmLiquidacao = 3`, `Liquidado = 4` | Fund lifecycle status — ANBIMA/CVM compliant | `integer` (EF Core default) |
| `TipoFundo` | `RendaFixa = 1`, `Acoes = 2`, `Cambial = 3`, `Multimercado = 4`, `CreditoPrivado = 5` | CVM 555 classification | `integer` |
| `ClasseAnbima` | `FIA = 1`, `FIC = 2`, `FI = 3`, `FIDC = 4`, `FMIA = 5`, `FMP = 6`, `Outro = 99` | ANBIMA fund class taxonomy | `integer` |

**Why `integer` not `string` enums:**
- PostgreSQL `integer` → 4 bytes vs `varchar(N)` → 5+ bytes. Faster joins, indexes, comparisons.
- EF Core maps C# `enum` → `integer` by default. Zero config.
- Prevents typos at DB level (only valid integers stored).
- Domain validates enum range — no invalid enum values survive into persistence.

**Why not PostgreSQL `enum` type (`CREATE TYPE ...`):**
- Npgsql supports it via `HasConversion<string>()` + `HasColumnType("status_fundo")`, but adds migration complexity (ALTER TYPE for new values).
- `integer` is simpler, equally fast, and allows adding enum members without DDL changes. For a system with known/controlled enum values, `integer` wins.

### 2. Value Objects — Reuse Existing

| Value Object | Already Exists | Used By |
|-------------|----------------|---------|
| `Cnpj` | ✅ `Onboarding.Domain.ValueObjects.Cnpj` | ConsultoriaFundo, Custodiante, Cedente |
| `Cpf` | ✅ `Onboarding.Domain.ValueObjects.Cpf` | Cedente (when seller is PF) |

**No new value objects needed for CNPJ/CPF.** The existing `Cnpj.Create()` and `Cpf.Create()` factory methods with full check-digit validation are production-ready and support the alphanumeric CNPJ format (July 2026 spec).

**Potential new value object — `RazaoSocial`:**
- NOT recommended as a value object. Simple `string` property with `HasMaxLength(200)` + FluentValidation `NotEmpty()` + `MaximumLength(200)` is sufficient. Over-engineering to wrap it.

### 3. Aggregates and Join Entities

| Entity | Type | Key | Table |
|--------|------|-----|-------|
| `ConsultoriaFundo` | Aggregate Root | `Guid` | `consultorias_fundo` |
| `Custodiante` | Aggregate Root | `Guid` | `custodiantes` |
| `Fundo` | Aggregate Root | `Guid` | `fundos` |
| `Cedente` | Aggregate Root | `Guid` | `cedentes` |
| `TipoAtivo` | Aggregate Root | `Guid` | `tipos_ativo` |
| `FundoCedente` | Join Entity | Compound (`FundoId` + `CedenteId`) | `fundo_cedente` |
| `CedenteTipoAtivo` | Join Entity | Compound (`CedenteId` + `TipoAtivoId`) | `cedente_tipo_ativo` |
| `FundoTipoAtivo` | Join Entity | Compound (`FundoId` + `TipoAtivoId`) | `fundo_tipo_ativo` |

**Why explicit join entities instead of EF Core's `HasMany().WithMany()`:**
- EF Core's skip-navigation only works for pure join tables (no payload columns).
- All three N-N relationships carry payload (e.g., `LimiteCompra` on Fundo↔Cedente, `PercentualMaximo` on Cedente↔TipoAtivo).
- Explicit join entity inherits from `Entity<Guid>` with a composite key defined via Fluent API:
  ```csharp
  builder.HasKey(f => new { f.FundoId, f.CedenteId });
  ```

**Why compound key on join entities vs surrogate `Guid`:**
- Composite key (`FundoId`, `CedenteId`) enforces uniqueness at DB level — prevents duplicate associations.
- Smaller index footprint than `Guid` PK + separate unique constraint.
- Pattern consistent with CVM regulatory requirements (no duplicate fund-cedente relationships).

### 4. Decimal Precision Mapping

| Domain Concept | C# Type | EF Core Config | PostgreSQL Type | Rationale |
|---------------|---------|----------------|-----------------|-----------|
| PL (Patrimônio Líquido) | `decimal` | `.HasPrecision(18, 2)` | `numeric(18,2)` | CVM standard — up to 999 trillion reais, 2 decimal places |
| Limite de compra (per cedente) | `decimal?` | `.HasPrecision(18, 2)` | `numeric(18,2)` | Monetary limit per fund-cedente pair |
| Percentual máximo (per tipo ativo) | `decimal?` | `.HasPrecision(5, 4)` | `numeric(5,4)` | 0.0000 to 1.0000 = 0% to 100% |
| Taxa de administração | `decimal?` | `.HasPrecision(8, 4)` | `numeric(8,4)` | Up to 9999.9999% annual |
| Taxa de performance | `decimal?` | `.HasPrecision(8, 4)` | `numeric(8,4)` | Same as taxa admin |
| Valor cota | `decimal` | `.HasPrecision(20, 8)` | `numeric(20,8)` | Quota values can have 8 decimal places per CVM |

**Pattern in EF Core Fluent API:**
```csharp
builder.Property(f => f.Pl)
    .HasColumnName("pl")
    .HasPrecision(18, 2)
    .IsRequired();

builder.Property(f => f.TaxaAdministracao)
    .HasColumnName("taxa_administracao")
    .HasPrecision(8, 4);
```

**Why not PostgreSQL `money` type:**
- `money` has fixed precision (locale-dependent), no control over scale.
- `numeric(p,s)` is the PostgreSQL standard for financial data. Full precision control.
- EF Core `HasPrecision()` maps cleanly to `numeric(p,s)` via Npgsql. No custom converter needed.

**Why `decimal` not `double` or `float`:**
- IEEE 754 floating-point has rounding errors. `0.1 + 0.2 ≠ 0.3`.
- `decimal` in C# is a 128-bit base-10 type — exact for financial calculations.
- PostgreSQL `numeric` maps to C# `decimal` natively via Npgsql. Zero precision loss.

### 5. Audit Log Extensions

Existing `ActionType` enum needs new entries:

```csharp
// v8.0 — Fundos de Investimento
ConsultoriaFundoCreated = 30,
ConsultoriaFundoUpdated = 31,
CustodianteCreated = 32,
CustodianteUpdated = 33,
FundoCreated = 34,
FundoUpdated = 35,
CedenteCreated = 36,
CedenteUpdated = 37,
TipoAtivoCreated = 38,
TipoAtivoUpdated = 39,
```

### 6. Permission Extensions

Existing `Permissions` class needs fund-related entries:

```csharp
public const string FundsRead = "funds:read";
public const string FundsWrite = "funds:write";
```

These map to the existing AccessGroup permission model — no new infrastructure needed.

---

## What NOT to Add

| Package / Concept | Why Avoid | What to Do Instead |
|------------------|-----------|-------------------|
| `Npgsql.EntityFrameworkCore.PostgreSQL.NodaTime` | No date/time ranges in domain. All temporal fields are `DateTimeOffset`. | Use `DateTimeOffset` for `CreatedAt`, `UpdatedAt` |
| `Dapper` alongside EF Core | Adds query complexity for zero benefit. EF Core handles all needed queries including N-N joins. | Use EF Core `Include()` / explicit join LINQ |
| `MediatR` | Commercial license. Already decided (D-XX). | Manual DI for CQRS — same pattern as existing |
| `FluentAssertions` | v8+ commercial license. Already replaced with Shouldly. | Use `Shouldly` (MIT) |
| `protobuf-net` or `Google.Protobuf` | No gRPC or protobuf serialization needed. | JSON via built-in `System.Text.Json` |
| Custom `CnpjCustodiante` value object | Over-engineering. Same validation rules as existing `Cnpj`. | Reuse `Onboarding.Domain.ValueObjects.Cnpj` |
| PostgreSQL `enum` types for StatusFundo/TipoFundo | Adds migration complexity for new values. Composite change management. | Store as `integer` — C# enum validation in domain |
| `Microsoft.EntityFrameworkCore.InMemory` for tests | In-memory provider doesn't support N-N relationships correctly. No real SQL validation. | Use Testcontainers PostgreSQL for integration tests |
| `Ardalis.Specification` | Full query encapsulation that the project doesn't need. Over-engineering. | Use repository pattern (already in project) + LINQ queries |

---

## EF Core Configuration Patterns Reference

### N-N with Payload — Join Entity Configuration

```csharp
public sealed class FundoCedenteConfiguration : IEntityTypeConfiguration<FundoCedente>
{
    private readonly ICurrentCompanyService _currentCompanyService;

    public FundoCedenteConfiguration(ICurrentCompanyService currentCompanyService)
        => _currentCompanyService = currentCompanyService;

    public void Configure(EntityTypeBuilder<FundoCedente> builder)
    {
        builder.ToTable("fundo_cedente");

        // Composite key — prevents duplicate associations
        builder.HasKey(fc => new { fc.FundoId, fc.CedenteId });

        builder.Property(fc => fc.FundoId)
            .HasColumnName("fundo_id")
            .IsRequired();

        builder.Property(fc => fc.CedenteId)
            .HasColumnName("cedente_id")
            .IsRequired();

        // Payload column — decimal precision for monetary limit
        builder.Property(fc => fc.LimiteCompra)
            .HasColumnName("limite_compra")
            .HasPrecision(18, 2);

        // Relationships — restrict delete to prevent orphan issues
        builder.HasOne<Fundo>()
            .WithMany()
            .HasForeignKey(fc => fc.FundoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Cedente>()
            .WithMany()
            .HasForeignKey(fc => fc.CedenteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Company isolation — same pattern as existing entities
        builder.HasQueryFilter(fc => fc.Fundo!.CompanyId == _currentCompanyService.CompanyId);
    }
}
```

### Enum Storage Configuration

```csharp
// No special config needed — EF Core maps enum → integer by default
builder.Property(f => f.Status)
    .HasColumnName("status")
    .IsRequired();

builder.Property(f => f.TipoFundo)
    .HasColumnName("tipo_fundo")
    .IsRequired();

builder.Property(f => f.ClasseAnbima)
    .HasColumnName("classe_anbima")
    .IsRequired();
```

### Value Object Reuse — Cnpj on New Entities

```csharp
// Same pattern as CompanyConfiguration — reuse Cnpj value object
builder.Property(c => c.Cnpj)
    .HasColumnName("cnpj")
    .HasConversion(
        vo => vo == null ? null : vo.Value,
        s => s == null ? null! : Cnpj.Create(s))
    .HasMaxLength(14)
    .IsRequired();

builder.HasIndex(c => c.Cnpj)
    .IsUnique()
    .HasFilter("cnpj IS NOT NULL");
```

---

## Database Schema Impact

### New Tables (6)

| Table | Purpose | Key Columns |
|-------|---------|-------------|
| `consultorias_fundo` | Fund advisory companies | `id`, `company_id`, `nome`, `cnpj` |
| `custodiantes` | Custodian institutions | `id`, `company_id`, `nome`, `cnpj` |
| `fundos` | Investment funds | `id`, `company_id`, `nome`, `cnpj`, `status`, `tipo_fundo`, `classe_anbima`, `pl`, `taxa_administracao`, `taxa_performance`, `consultoria_fundo_id`, `custodiante_id` |
| `cedentes` | Assignors/sellers of receivables | `id`, `company_id`, `razao_social`, `cnpj`, `cpf` |
| `tipos_ativo` | Asset type classification | `id`, `company_id`, `nome`, `descricao` |
| `fundo_cedente` | Fund ↔ Cedente with limit | `fundo_id`, `cedente_id`, `limite_compra` |
| `cedente_tipo_ativo` | Cedente ↔ TipoAtivo with % | `cedente_id`, `tipo_ativo_id`, `percentual_maximo` |
| `fundo_tipo_ativo` | Fund ↔ TipoAtivo with % | `fundo_id`, `tipo_ativo_id`, `percentual_maximo` |

### All Tables Get Company Isolation

Every new entity carries `CompanyId` FK to the `companies` table. Every EF Core configuration applies `HasQueryFilter` using the existing `ICurrentCompanyService`. This is the established pattern — no new middleware or services required.

---

## Frontend Impact

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| Zod schemas for fund forms | New schemas needed — `fundoSchema`, `cedenteSchema`, etc. | Mirror backend validation rules (CNPJ check digits, enums, decimal ranges) |
| TanStack Router routes | New route tree: `/fundos`, `/fundos/:id`, `/cedentes`, etc. | Standard CRUD routing pattern |
| React Hook Form | Wrap fund registration/edit forms | Performance + Zod resolver integration |
| Tailwind CSS 4 | No changes — component styling via existing utility classes | Atomic Design atoms/molecules already established |
| shadcn/ui components | May need `Select` for enums, `Input[type=number]` for decimals | Already available — no new package needed |

---

## Installation

```bash
# No new packages to install — existing stack covers everything
# Only code additions needed:

# Domain layer
#   - Enums: StatusFundo, TipoFundo, ClasseAnbima
#   - Aggregates: ConsultoriaFundo, Custodiante, Fundo, Cedente, TipoAtivo
#   - Join entities: FundoCedente, CedenteTipoAtivo, FundoTipoAtivo
#   - Repositories: IFundoRepository, ICedenteRepository, etc.
#   - Permissions: add "funds:read", "funds:write"
#   - ActionType: add fund-related enum values

# Application layer
#   - Commands: CreateFundoCommand, UpdateFundoCommand, etc.
#   - Queries: GetFundosQuery, GetFundoDetailsQuery, etc.
#   - Validators: CreateFundoCommandValidator, etc.
#   - DTOs: FundoDto, CedenteDto, etc.
#   - Handlers: command & query handlers via manual DI

# Infrastructure layer
#   - EF Core configurations for all new entities
#   - Repository implementations
#   - Migration: AddFundosModule

# API layer
#   - Controllers: FundosController, CedentesController, etc.
#   - Permission additions in security policies
```

---

## Sources

- [FluentValidation 12.1.1 — NuGet](https://www.nuget.org/packages/FluentValidation/12.1.1) — verified Apache-2.0, current stable
- [Npgsql EF Core Provider — Context7](https://context7.com/npgsql/efcore.pg) — confirmed `numeric(p,s)` mapping via EF Core `HasPrecision()`
- [EF Core 10 Relationships — Microsoft Docs](https://learn.microsoft.com/en-us/ef/core/modeling/relationships) — N-N with payload pattern using explicit join entities
- [CVM Instruction 555 — Brazilian Financial Regulations](https://www.cvm.gov.br/) — fund classification taxonomy (TipoFundo values)
- [ANBIMA Fund Classification — anbima.com.br](https://www.anbima.com.br/) — ClasseAnbima enum values
- Existent codebase analysis — `Cnpj.cs`, `Cpf.cs`, `Entity.cs`, `CompanyConfiguration.cs`, `EmployeeConfiguration.cs`, `AppDbContext.cs`, `Permissions.cs`, `ActionType.cs`