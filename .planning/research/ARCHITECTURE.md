# Architecture Patterns — Fundos de Investimento Module

**Domain:** Investment Funds management module added to existing PJ onboarding system
**Project:** Onboarding de Clientes (v8.0)
**Researched:** 2026-05-02
**Overall confidence:** HIGH — based on direct codebase analysis, existing patterns well-established

---

## Executive Summary

The Fundos module integrates into the existing DDD architecture as a **new bounded context within the same Company aggregate boundary**. The Company (PJ) is the tenancy root — Fundo, ConsultoriaFundo, Custodiante, Cedente, and TipoAtivo are all scoped to a CompanyId. This means: same `Onboarding.Domain` project, same `AppDbContext`, same multi-tenancy `HasQueryFilter` pattern, same CQRS manual DI wiring. No separate microservice, no separate database, no separate bounded context.

The module introduces 5 new aggregate roots + 3 relationship entities (join tables). The `Cnpj` value object is reused for Custodiante and ConsultoriaFundo. Two new value objects are needed: `TipoAtivoEnum` (smart enum) and `FundoCnpj` validation (already covered by existing `Cnpj`). New API controller: `FundosController` under `api/companies/{companyId}/fundos/`. Frontend: new `FundosPage` in client SPA, new `AdminFundosPage` in backoffice, following existing Atomic Design.

---

## Recommended Architecture

### System Overview (with Fundos module added)

```
┌─────────────────────────────────────────────────────────────────────┐
│  Docker Compose Network (internal)                                  │
│                                                                     │
│  ┌──────────────┐  ┌──────────────┐     HTTPS    ┌──────────────┐   │
│  │ Client SPA   │  │ Backoffice   │─────────────▶│  .NET 10 API │   │
│  │ :5173        │  │ SPA :5174    │              │  :8080        │   │
│  └──────┬───────┘  └──────┬───────┘              └──────┬───────┘   │
│         │                  │                              │           │
│         │  credentials:include    credentials:include   │ EF Core   │
│         │  (httpOnly cookies)     (httpOnly cookies)    │           │
│         │                  │                       ┌────▼────┐      │
│         │                  │                       │PostgreSQL│      │
│         │                  │                       │app_db    │      │
│         │                  │                       │:5432     │      │
│         │                  │                       └──────────┘      │
│         │                  │                              │           │
│         │    ACF+PKCE      │    ACF+PKCE          ┌──────▼──────┐   │
│         └──────────────────┴──────────────────────│  Keycloak   │   │
│                                                     │  :8180      │   │
│                                                     └─────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

No infrastructure changes needed — same PostgreSQL app_db, same Keycloak, same Docker services. New tables are added via EF Core migrations to the existing `app_db`.

---

## Bounded Context Decision: SAME Context

**Recommendation:** Fundos stays in the same bounded context as Company/Employee.

**Rationale:**

1. **Company is the tenancy root.** Every Fundo, Cedente, ConsultoriaFundo, Custodiante is scoped to `CompanyId`. Cross-context communication would require shared Company identity — which means they're not really separate contexts.

2. **No independent lifecycle.** A Fundo cannot exist without a Company. A Cedente cannot exist without a Company. These are not separate domains with their own ubiquitous language — they're subdomains of the Company management context.

3. **No separate database needed.** Same `app_db`, same `AppDbContext`, same connection string. Adding tables to an existing PostgreSQL is trivial; adding a new service for 5 entities is overengineering.

4. **Existing patterns handle it.** `HasQueryFilter` on CompanyId already provides tenant isolation. CQRS manual DI scales linearly. Repository pattern is consistent.

5. **Frontend is same SPA.** Client users manage their fundos alongside their employees and access groups in the same Vinxi app. No separate SPA needed.

**When to split into a separate bounded context:** Only if Fundos develops its own complex domain rules (e.g., NAV calculation, compliance workflows, automated trading) that would bloat the Company aggregate beyond 300+ lines of domain logic per aggregate. Current scope (CRUD + relationships) does not warrant this.

---

## New Domain Model

### Entity Map

```
Company (EXISTING — aggregate root, tenancy boundary)
  │
  ├── Employee (EXISTING — aggregate root, CompanyId FK)
  ├── AccessGroup (EXISTING — entity, CompanyId FK)
  │
  ├── Fundo (NEW — aggregate root, CompanyId FK)
  │     ├── FundoCedente (NEW — join entity, FundoId + CedenteId)
  │     └── FundoTipoAtivo (NEW — join entity, FundoId + TipoAtivoId)
  │
  ├── ConsultoriaFundo (NEW — aggregate root, CompanyId FK, Cnpj)
  ├── Custodiante (NEW — aggregate root, CompanyId FK, Cnpj)
  │
  ├── Cedente (NEW — aggregate root, CompanyId FK)
  │     └── CedenteTipoAtivo (NEW — join entity, CedenteId + TipoAtivoId)
  │
  └── TipoAtivo (NEW — entity, CompanyId FK)
```

### Aggregate Root Justification

| Entity | Aggregate Root? | Why |
|--------|----------------|-----|
| Fundo | YES | Central entity of the module. Owns FundoCedente and FundoTipoAtivo collections. Invariant: a fundo must have at least one cedente |
| ConsultoriaFundo | YES | Independent lifecycle. Owns its CNPJ. Created/edited/deleted by company independently of Fundo |
| Custodiante | YES | Independent lifecycle. Owns its CNPJ. Referenced by Fundo via FK |
| Cedente | YES | Independent lifecycle. Owns CedenteTipoAtivo. Referenced by FundoCedente |
| TipoAtivo | NO (entity) | Lookup entity scoped to Company. No independent invariants — it's a named classification |

### Aggregate Invariants

**Fundo:**
- Must have `Nome` (non-empty)
- Must reference exactly one `CustodianteId`
- Must have at least one `FundoCedente` (enforced on creation)
- `FundoCedente` and `FundoTipoAtivo` are managed through the aggregate (add/remove methods)

**ConsultoriaFundo:**
- Must have `Nome` (non-empty)
- Must have valid `Cnpj`

**Custodiante:**
- Must have `Nome` (non-empty)
- Must have valid `Cnpj`

**Cedente:**
- Must have `Nome` (non-empty)
- Must have valid `Cnpj` (Cedente is always a PJ)

**TipoAtivo:**
- Must have `Nome` (non-empty, unique within Company)

---

## Value Objects — Reuse vs New

| Value Object | Reuse? | Notes |
|-------------|--------|-------|
| `Cnpj` | **REUSE** for ConsultoriaFundo, Custodiante, Cedente | Existing `Cnpj.cs` already validates alphanumeric format (July 2026). Same rules apply. |
| `Email` | NOT needed for Fundos entities | Fundos entities don't have email addresses. |
| `PhoneNumber` | NOT needed for Fundos entities | Fundos entities don't have phone numbers. |
| `Cpf` | NOT needed for Fundos entities | All Fundos entities are PJ. |

**No new value objects required.** The `Cnpj` value object with its self-validation (length 14, alphanumeric, check digits) is sufficient for all Fundos entities that need CNPJ.

`TipoAtivo` is modeled as a simple entity (not a value object or smart enum) because:
- It has a Guid `Id` (needed for relationship tables)
- It's scoped to `CompanyId` (tenant-specific, not global)
- It needs CRUD operations (create, rename, delete)

If global TipoAtivo (shared across all companies) were needed, a smart enum pattern would be appropriate. But the requirement is company-scoped, so entity with CompanyId is correct.

---

## Domain Layer — File Structure

```
src/Onboarding.Domain/
├── Aggregates/
│   ├── CompanyAggregate/                 ← EXISTS
│   │   ├── Company.cs
│   │   └── TermsAcceptance.cs
│   ├── EmployeeAggregate/                ← EXISTS
│   │   ├── Employee.cs
│   │   ├── AccessGroup.cs
│   │   └── Permissions.cs
│   ├── Audit/                             ← EXISTS
│   ├── PasswordReset/                    ← EXISTS
│   │
│   └── FundosAggregate/                  ← NEW
│       ├── Fundo.cs                       ← Aggregate root
│       ├── ConsultoriaFundo.cs            ← Aggregate root
│       ├── Custodiante.cs                 ← Aggregate root
│       ├── Cedente.cs                     ← Aggregate root
│       ├── CedenteTipoAtivo.cs            ← Join entity (owned by Cedente)
│       ├── FundoCedente.cs                ← Join entity (owned by Fundo)
│       ├── FundoTipoAtivo.cs              ← Join entity (owned by Fundo)
│       └── TipoAtivo.cs                   ← Entity (CompanyId-scoped lookup)
│
├── ValueObjects/                          ← EXISTS (Cnpj, Cpf, Email, PhoneNumber)
├── Repositories/                          ← ADD new interfaces
│   ├── ICompanyRepository.cs             ← EXISTS
│   ├── IEmployeeRepository.cs             ← EXISTS
│   ├── IAccessGroupRepository.cs          ← EXISTS
│   ├── IFundoRepository.cs               ← NEW
│   ├── IConsultoriaFundoRepository.cs     ← NEW
│   ├── ICustodianteRepository.cs          ← NEW
│   ├── ICedenteRepository.cs             ← NEW
│   └── ITipoAtivoRepository.cs           ← NEW
├── Common/                                ← EXISTS
│   └── Entity.cs
└── Exceptions/                            ← EXISTS
    └── (add DuplicateFundoException if needed)
```

**Namespace choice:** `Onboarding.Domain.Aggregates.FundosAggregate` — follows the existing pattern of one folder per aggregate group. Even though Fundos contains multiple aggregate roots, they're conceptually related (all part of the investment funds domain) and belong together.

### Aggregate Root: Fundo.cs (sketch)

```csharp
namespace Onboarding.Domain.Aggregates.FundosAggregate;

public sealed class Fundo : Entity<Guid>
{
    public string Nome { get; private set; } = default!;
    public string? Cnpj { get; private set; }          // optional — some funds have CNPJ
    public Guid CustodianteId { get; private set; }     // FK — no navigation property
    public Guid ConsultoriaFundoId { get; private set; } // FK — no navigation property
    public Guid CompanyId { get; private set; }         // tenancy FK — no navigation property
    public DateTimeOffset CreatedAt { get; private set; }

    // Owned collections — managed through aggregate methods
    private readonly List<FundoCedente> _fundosCedentes = [];
    public IReadOnlyList<FundoCedente> FundosCedentes => _fundosCedentes.AsReadOnly();

    private readonly List<FundoTipoAtivo> _fundosTiposAtivo = [];
    public IReadOnlyList<FundoTipoAtivo> FundosTiposAtivo => _fundosTiposAtivo.AsReadOnly();

    private Fundo() { }

    public static Fundo Create(
        string nome,
        string? cnpj,
        Guid custodianteId,
        Guid consultoriaFundoId,
        Guid companyId,
        IEnumerable<Guid> cedenteIds,
        IEnumerable<Guid> tipoAtivoIds)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome is required.", nameof(nome));

        var fundo = new Fundo
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Cnpj = cnpj, // optional — validate if provided
            CustodianteId = custodianteId,
            ConsultoriaFundoId = consultoriaFundoId,
            CompanyId = companyId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var cedenteId in cedenteIds)
            fundo._fundosCedentes.Add(new FundoCedente(fundo.Id, cedenteId));

        foreach (var tipoAtivoId in tipoAtivoIds)
            fundo._fundosTiposAtivo.Add(new FundoTipoAtivo(fundo.Id, tipoAtivoId));

        return fundo;
    }

    public void AddCedente(Guid cedenteId)
        => _fundosCedentes.Add(new FundoCedente(Id, cedenteId));

    public void RemoveCedente(Guid cedenteId)
        => _fundosCedentes.RemoveAll(fc => fc.CedenteId == cedenteId);

    public void AddTipoAtivo(Guid tipoAtivoId)
        => _fundosTiposAtivo.Add(new FundoTipoAtivo(Id, tipoAtivoId));

    public void RemoveTipoAtivo(Guid tipoAtivoId)
        => _fundosTiposAtivo.RemoveAll(fta => fta.TipoAtivoId == tipoAtivoId);

    public void Update(string nome, Guid custodianteId, Guid consultoriaFundoId)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome is required.", nameof(nome));
        Nome = nome;
        CustodianteId = custodianteId;
        ConsultoriaFundoId = consultoriaFundoId;
    }
}
```

### Join Entity: FundoCedente.cs (sketch)

```csharp
namespace Onboarding.Domain.Aggregates.FundosAggregate;

/// <summary>
/// Join entity — links Fundo to Cedente. Owned by Fundo aggregate.
/// </summary>
public sealed class FundoCedente
{
    public Guid FundoId { get; private set; }
    public Guid CedenteId { get; private set; }

    internal FundoCedente(Guid fundoId, Guid cedenteId)
    {
        FundoId = fundoId;
        CedenteId = cedenteId;
    }

    // EF Core needs parameterless ctor for materialization
    private FundoCedente() { }
}
```

### Entity: TipoAtivo.cs (sketch)

```csharp
namespace Onboarding.Domain.Aggregates.FundosAggregate;

public sealed class TipoAtivo : Entity<Guid>
{
    public string Nome { get; private set; } = default!;
    public Guid CompanyId { get; private set; }

    private TipoAtivo() { }

    public static TipoAtivo Create(string nome, Guid companyId)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome is required.", nameof(nome));
        return new TipoAtivo
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            CompanyId = companyId
        };
    }

    public void Update(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome is required.", nameof(nome));
        Nome = nome;
    }
}
```

---

## Repository Interfaces

Follow existing pattern — one interface per aggregate root. Join entities (FundoCedente, FundoTipoAtivo, CedenteTipoAtivo) are NOT exposed via separate repositories; they're managed through their owning aggregate.

```csharp
// IFundoRepository.cs
public interface IFundoRepository
{
    Task AddAsync(Fundo fundo, CancellationToken ct = default);
    Task SaveAsync(Fundo fundo, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Fundo?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Fundo?> GetByIdWithCedentesAsync(Guid id, CancellationToken ct = default); // includes + split query
    Task<Fundo?> GetByIdWithTiposAtivoAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNomeAsync(string nome, Guid companyId, CancellationToken ct = default);
    Task<(IReadOnlyList<Fundo> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int page, int pageSize, string? search, CancellationToken ct = default);
}

// IConsultoriaFundoRepository.cs
public interface IConsultoriaFundoRepository
{
    Task AddAsync(ConsultoriaFundo consultoria, CancellationToken ct = default);
    Task SaveAsync(ConsultoriaFundo consultoria, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ConsultoriaFundo?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByCnpjAsync(string cnpj, Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<ConsultoriaFundo>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default);
    Task<(IReadOnlyList<ConsultoriaFundo> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int page, int pageSize, string? search, CancellationToken ct = default);
}

// ICustodianteRepository.cs — same shape as ConsultoriaFundo

// ICedenteRepository.cs
public interface ICedenteRepository
{
    Task AddAsync(Cedente cedente, CancellationToken ct = default);
    Task SaveAsync(Cedente cedente, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Cedente?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Cedente?> GetByIdWithTiposAtivoAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByCnpjAsync(string cnpj, Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<Cedente>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default);
    Task<(IReadOnlyList<Cedente> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int page, int pageSize, string? search, CancellationToken ct = default);
}

// ITipoAtivoRepository.cs
public interface ITipoAtivoRepository
{
    Task AddAsync(TipoAtivo tipoAtivo, CancellationToken ct = default);
    Task SaveAsync(TipoAtivo tipoAtivo, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<TipoAtivo?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNomeAsync(string nome, Guid companyId, CancellationToken ct = default);
    Task<IReadOnlyList<TipoAtivo>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default);
}
```

---

## Multi-Tenancy — HasQueryFilter Pattern

All new entities with `CompanyId` get the same `HasQueryFilter` pattern as `Employee` and `AccessGroup`:

```csharp
// In FundoConfiguration.cs
public sealed class FundoConfiguration : IEntityTypeConfiguration<Fundo>
{
    private readonly ICurrentCompanyService _currentCompanyService;

    public FundoConfiguration(ICurrentCompanyService currentCompanyService)
    {
        _currentCompanyService = currentCompanyService;
    }

    public void Configure(EntityTypeBuilder<Fundo> builder)
    {
        builder.ToTable("fundos");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(f => f.Cnpj).HasColumnName("cnpj").HasMaxLength(14).IsRequired(false);
        builder.Property(f => f.CustodianteId).HasColumnName("custodiante_id").IsRequired();
        builder.Property(f => f.ConsultoriaFundoId).HasColumnName("consultoria_fundo_id").IsRequired();
        builder.Property(f => f.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(f => f.CreatedAt).HasColumnName("created_at").IsRequired();

        // FK to Custodiante — Restrict delete (custodiante in use)
        builder.HasOne<Custodiante>().WithMany().HasForeignKey(f => f.CustodianteId).OnDelete(DeleteBehavior.Restrict);
        // FK to ConsultoriaFundo — Restrict delete
        builder.HasOne<ConsultoriaFundo>().WithMany().HasForeignKey(f => f.ConsultoriaFundoId).OnDelete(DeleteBehavior.Restrict);

        // Owned collection: FundoCedente (join table)
        builder.HasMany(f => f.FundosCedentes)
            .WithOne()
            .HasForeignKey(fc => fc.FundoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Owned collection: FundoTipoAtivo (join table)
        builder.HasMany(f => f.FundosTiposAtivo)
            .WithOne()
            .HasForeignKey(fta => fta.FundoId)
            .OnDelete(DeleteBehavior.Cascade);

        // TENANT ISOLATION — same pattern as EmployeeConfiguration
        builder.HasQueryFilter(f => f.CompanyId == _currentCompanyService.CompanyId);

        builder.HasIndex(f => f.CompanyId).HasDatabaseName("IX_fundos_company_id");
        builder.HasIndex(f => new { f.Nome, f.CompanyId }).IsUnique().HasDatabaseName("IX_fundos_nome_company");
    }
}
```

Same pattern applies to `ConsultoriaFundoConfiguration`, `CustodianteConfiguration`, `CedenteConfiguration`, and `TipoAtivoConfiguration`.

**Admin bypass:** Admin endpoints that need cross-company access use `IgnoreQueryFilters()` on the specific query — same pattern as `GetPagedAllAsync` in `IEmployeeRepository`.

---

## CQRS — Commands and Queries

### Directory Structure

```
src/Onboarding.Application/
├── Fundos/                         ← NEW
│   ├── Commands/
│   │   ├── CreateFundoCommand.cs
│   │   ├── CreateFundoCommandHandler.cs
│   │   ├── CreateFundoCommandValidator.cs
│   │   ├── UpdateFundoCommand.cs
│   │   ├── UpdateFundoCommandHandler.cs
│   │   ├── DeleteFundoCommand.cs
│   │   ├── DeleteFundoCommandHandler.cs
│   │   ├── CreateConsultoriaFundoCommand.cs
│   │   ├── CreateConsultoriaFundoCommandHandler.cs
│   │   ├── CreateConsultoriaFundoCommandValidator.cs
│   │   ├── UpdateConsultoriaFundoCommand.cs
│   │   ├── UpdateConsultoriaFundoCommandHandler.cs
│   │   ├── DeleteConsultoriaFundoCommand.cs
│   │   ├── DeleteConsultoriaFundoCommandHandler.cs
│   │   ├── CreateCustodianteCommand.cs
│   │   ├── CreateCustodianteCommandHandler.cs
│   │   ├── CreateCustodianteCommandValidator.cs
│   │   ├── UpdateCustodianteCommand.cs
│   │   ├── UpdateCustodianteCommandHandler.cs
│   │   ├── DeleteCustodianteCommand.cs
│   │   ├── DeleteCustodianteCommandHandler.cs
│   │   ├── CreateCedenteCommand.cs
│   │   ├── CreateCedenteCommandHandler.cs
│   │   ├── CreateCedenteCommandValidator.cs
│   │   ├── UpdateCedenteCommand.cs
│   │   ├── UpdateCedenteCommandHandler.cs
│   │   ├── DeleteCedenteCommand.cs
│   │   ├── DeleteCedenteCommandHandler.cs
│   │   ├── CreateTipoAtivoCommand.cs
│   │   ├── CreateTipoAtivoCommandHandler.cs
│   │   ├── CreateTipoAtivoCommandValidator.cs
│   │   ├── UpdateTipoAtivoCommand.cs
│   │   ├── UpdateTipoAtivoCommandHandler.cs
│   │   ├── DeleteTipoAtivoCommand.cs
│   │   └── DeleteTipoAtivoCommandHandler.cs
│   ├── Queries/
│   │   ├── GetFundosQuery.cs
│   │   ├── GetFundosQueryHandler.cs
│   │   ├── GetFundoDetailsQuery.cs
│   │   ├── GetFundoDetailsQueryHandler.cs
│   │   ├── GetConsultoriasFundoQuery.cs
│   │   ├── GetConsultoriasFundoQueryHandler.cs
│   │   ├── GetCustodiantesQuery.cs
│   │   ├── GetCustodiantesQueryHandler.cs
│   │   ├── GetCedentesQuery.cs
│   │   ├── GetCedentesQueryHandler.cs
│   │   ├── GetTiposAtivoQuery.cs
│   │   └── GetTiposAtivoQueryHandler.cs
│   └── DTOs/
│       ├── FundoDto.cs
│       ├── FundoListItemDto.cs
│       ├── ConsultoriaFundoDto.cs
│       ├── CustodianteDto.cs
│       ├── CedenteDto.cs
│       ├── TipoAtivoDto.cs
│       └── FundoDetailsDto.cs         ← includes cedentes + tipos ativo
```

### Command Example

```csharp
// CreateFundoCommand.cs
namespace Onboarding.Application.Fundos.Commands;

public sealed record CreateFundoCommand(
    Guid CompanyId,
    string Nome,
    string? Cnpj,
    Guid CustodianteId,
    Guid ConsultoriaFundoId,
    IReadOnlyList<Guid> CedenteIds,
    IReadOnlyList<Guid> TipoAtivoIds,
    string ActorSub,
    string ActorEmail,
    string? IpAddress) : ICommand<CreateFundoResult>;

public sealed record CreateFundoResult(Guid FundoId);
```

```csharp
// CreateFundoCommandHandler.cs
public sealed class CreateFundoCommandHandler : ICommandHandler<CreateFundoCommand, CreateFundoResult>
{
    private readonly IFundoRepository _fundoRepository;
    private readonly ICustodianteRepository _custodianteRepository;
    private readonly IConsultoriaFundoRepository _consultoriaRepository;
    private readonly IAuditService _auditService;

    public CreateFundoCommandHandler(
        IFundoRepository fundoRepository,
        ICustodianteRepository custodianteRepository,
        IConsultoriaFundoRepository consultoriaRepository,
        IAuditService auditService)
    {
        _fundoRepository = fundoRepository;
        _custodianteRepository = custodianteRepository;
        _consultoriaRepository = consultoriaRepository;
        _auditService = auditService;
    }

    public async Task<CreateFundoResult> HandleAsync(CreateFundoCommand command, CancellationToken ct)
    {
        // Validate Custodiante exists and belongs to company
        var custodiante = await _custodianteRepository.GetByIdAsync(command.CustodianteId, ct);
        if (custodiante is null || custodiante.CompanyId != command.CompanyId)
            throw new BadRequestException("Custodiante not found or does not belong to this company.");

        // Validate ConsultoriaFundo exists and belongs to company
        var consultoria = await _consultoriaRepository.GetByIdAsync(command.ConsultoriaFundoId, ct);
        if (consultoria is null || consultoria.CompanyId != command.CompanyId)
            throw new BadRequestException("Consultoria not found or does not belong to this company.");

        // Create aggregate
        var fundo = Fundo.Create(
            command.Nome,
            command.Cnpj,
            command.CustodianteId,
            command.ConsultoriaFundoId,
            command.CompanyId,
            command.CedenteIds,
            command.TipoAtivoIds);

        await _fundoRepository.AddAsync(fundo, ct);

        // Audit
        await _auditService.LogAsync(
            command.CompanyId,
            command.ActorSub,
            command.ActorEmail,
            ActionType.FundoCreated,
            $"Fundo '{command.Nome}' created",
            command.IpAddress,
            ct);

        return new CreateFundoResult(fundo.Id);
    }
}
```

### DI Registration

Add to `DependencyInjection.cs` in Application layer:

```csharp
// Fundos module commands
services.AddScoped<ICommandHandler<CreateFundoCommand, CreateFundoResult>, CreateFundoCommandHandler>();
services.AddScoped<ICommandHandler<UpdateFundoCommand, Unit>, UpdateFundoCommandHandler>();
services.AddScoped<ICommandHandler<DeleteFundoCommand, Unit>, DeleteFundoCommandHandler>();
services.AddScoped<IValidator<CreateFundoCommand>, CreateFundoCommandValidator>();

services.AddScoped<ICommandHandler<CreateConsultoriaFundoCommand, CreateConsultoriaFundoResult>, CreateConsultoriaFundoCommandHandler>();
services.AddScoped<ICommandHandler<UpdateConsultoriaFundoCommand, Unit>, UpdateConsultoriaFundoCommandHandler>();
services.AddScoped<ICommandHandler<DeleteConsultoriaFundoCommand, Unit>, DeleteConsultoriaFundoCommandHandler>();
services.AddScoped<IValidator<CreateConsultoriaFundoCommand>, CreateConsultoriaFundoCommandValidator>();

services.AddScoped<ICommandHandler<CreateCustodianteCommand, CreateCustodianteResult>, CreateCustodianteCommandHandler>();
services.AddScoped<ICommandHandler<UpdateCustodianteCommand, Unit>, UpdateCustodianteCommandHandler>();
services.AddScoped<ICommandHandler<DeleteCustodianteCommand, Unit>, DeleteCustodianteCommandHandler>();
services.AddScoped<IValidator<CreateCustodianteCommand>, CreateCustodianteCommandValidator>();

services.AddScoped<ICommandHandler<CreateCedenteCommand, CreateCedenteResult>, CreateCedenteCommandHandler>();
services.AddScoped<ICommandHandler<UpdateCedenteCommand, Unit>, UpdateCedenteCommandHandler>();
services.AddScoped<ICommandHandler<DeleteCedenteCommand, Unit>, DeleteCedenteCommandHandler>();
services.AddScoped<IValidator<CreateCedenteCommand>, CreateCedenteCommandValidator>();

services.AddScoped<ICommandHandler<CreateTipoAtivoCommand, CreateTipoAtivoResult>, CreateTipoAtivoCommandHandler>();
services.AddScoped<ICommandHandler<UpdateTipoAtivoCommand, Unit>, UpdateTipoAtivoCommandHandler>();
services.AddScoped<ICommandHandler<DeleteTipoAtivoCommand, Unit>, DeleteTipoAtivoCommandHandler>();
services.AddScoped<IValidator<CreateTipoAtivoCommand>, CreateTipoAtivoCommandValidator>();

// Fundos module queries
services.AddScoped<IQueryHandler<GetFundosQuery, PaginatedResult<FundoListItemDto>>, GetFundosQueryHandler>();
services.AddScoped<IQueryHandler<GetFundoDetailsQuery, FundoDetailsDto>, GetFundoDetailsQueryHandler>();
services.AddScoped<IQueryHandler<GetConsultoriasFundoQuery, PaginatedResult<ConsultoriaFundoDto>>, GetConsultoriasFundoQueryHandler>();
services.AddScoped<IQueryHandler<GetCustodiantesQuery, PaginatedResult<CustodianteDto>>, GetCustodiantesQueryHandler>();
services.AddScoped<IQueryHandler<GetCedentesQuery, PaginatedResult<CedenteDto>>, GetCedentesQueryHandler>();
services.AddScoped<IQueryHandler<GetTiposAtivoQuery, IReadOnlyList<TipoAtivoDto>>, GetTiposAtivoQueryHandler>();
```

Add to `DependencyInjection.cs` in Infrastructure layer:

```csharp
services.AddScoped<IFundoRepository, FundoRepository>();
services.AddScoped<IConsultoriaFundoRepository, ConsultoriaFundoRepository>();
services.AddScoped<ICustodianteRepository, CustodianteRepository>();
services.AddScoped<ICedenteRepository, CedenteRepository>();
services.AddScoped<ITipoAtivoRepository, TipoAtivoRepository>();
```

---

## API Controller Design

### New Controller: FundosController

Route: `api/companies/{companyId}/fundos/...`

All endpoints follow the existing pattern from `CompaniesController`:
- Company isolation via `ICurrentCompanyService`
- Permission-based `[Authorize]` with new policy constants
- FluentValidation before handler call
- ActorSub/ActorEmail/IpAddress extraction for audit

```csharp
[ApiController]
[Route("api/companies/{companyId:guid}/fundos")]
public sealed class FundosController : ControllerBase
{
    // Fundo CRUD
    [HttpGet]                                           // GET  .../fundos
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundosRead)]
    public async Task<IActionResult> GetFundos(...)

    [HttpGet("{fundoId:guid}")]                         // GET  .../fundos/{id}
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundosRead)]
    public async Task<IActionResult> GetFundoDetails(...)

    [HttpPost]                                          // POST .../fundos
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundosWrite)]
    public async Task<IActionResult> CreateFundo(...)

    [HttpPut("{fundoId:guid}")]                         // PUT  .../fundos/{id}
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundosWrite)]
    public async Task<IActionResult> UpdateFundo(...)

    [HttpDelete("{fundoId:guid}")]                     // DELETE .../fundos/{id}
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundosDelete)]
    public async Task<IActionResult> DeleteFundo(...)

    // ConsultoriaFundo sub-resource
    [HttpGet("consultorias")]                           // GET  .../fundos/consultorias
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundosRead)]
    public async Task<IActionResult> GetConsultorias(...)

    [HttpPost("consultorias")]                          // POST .../fundos/consultorias
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundosWrite)]
    public async Task<IActionResult> CreateConsultoria(...)

    [HttpPut("consultorias/{id:guid}")]                 // PUT  .../fundos/consultorias/{id}
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundosWrite)]
    public async Task<IActionResult> UpdateConsultoria(...)

    [HttpDelete("consultorias/{id:guid}")]              // DELETE .../fundos/consultorias/{id}
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundosDelete)]
    public async Task<IActionResult> DeleteConsultoria(...)

    // Custodiante sub-resource
    [HttpGet("custodiantes")]                           // GET  .../fundos/custodiantes
    [Authorize(...)]
    [HttpPost("custodiantes")]                           // POST .../fundos/custodiantes
    [HttpPut("custodiantes/{id:guid}")]                  // PUT  .../fundos/custodiantes/{id}
    [HttpDelete("custodiantes/{id:guid}")]               // DELETE .../fundos/custodiantes/{id}

    // Cedente sub-resource
    [HttpGet("cedentes")]                               // GET  .../fundos/cedentes
    [HttpPost("cedentes")]                              // POST .../fundos/cedentes
    [HttpPut("cedentes/{id:guid}")]                     // PUT  .../fundos/cedentes/{id}
    [HttpDelete("cedentes/{id:guid}")]                  // DELETE .../fundos/cedentes/{id}

    // TipoAtivo sub-resource
    [HttpGet("tipos-ativo")]                            // GET  .../fundos/tipos-ativo
    [HttpPost("tipos-ativo")]                           // POST .../fundos/tipos-ativo
    [HttpPut("tipos-ativo/{id:guid}")]                  // PUT  .../fundos/tipos-ativo/{id}
    [HttpDelete("tipos-ativo/{id:guid}")]               // DELETE .../fundos/tipos-ativo/{id}
}
```

### New Permission Policies

```csharp
// Add to PermissionPolicyConstants.cs
public const string FundosRead = "FundosRead";
public const string FundosWrite = "FundosWrite";
public const string FundosDelete = "FundosDelete";
```

### Updated Permissions List

```csharp
// Add to Permissions.cs (EmployeeAggregate)
public const string FundosRead = "fundos:read";
public const string FundosWrite = "fundos:write";
public const string FundosDelete = "fundos:delete";

public static readonly string[] All = [
    EmployeesRead, EmployeesWrite, EmployeesDelete,
    AuditRead, DashboardAccess, AccessGroupsManage,
    FundosRead, FundosWrite, FundosDelete    // ← NEW
];
```

### Admin Fundos Controller (Backoffice)

```csharp
[ApiController]
[Route("api/admin/fundos")]
[Authorize(AuthenticationSchemes = "BearerBackoffice", Policy = PermissionPolicies.CrossCompanyAccess)]
public sealed class AdminFundosController : ControllerBase
{
    // Cross-company fundos listing for admin support
    [HttpGet]
    public async Task<IActionResult> GetAllFundos(...)

    [HttpGet("{fundoId:guid}")]
    public async Task<IActionResult> GetFundoDetails(...)

    [HttpGet("by-company/{companyId:guid}")]
    public async Task<IActionResult> GetFundosByCompany(Guid companyId, ...)
}
```

---

## AppDbContext — New DbSets

```csharp
// Add to AppDbContext.cs
public DbSet<Fundo> Fundos => Set<Fundo>();
public DbSet<ConsultoriaFundo> ConsultoriasFundo => Set<ConsultoriaFundo>();
public DbSet<Custodiante> Custodiantes => Set<Custodiante>();
public DbSet<Cedente> Cedentes => Set<Cedente>();
public DbSet<TipoAtivo> TiposAtivo => Set<TipoAtivo>();

// Add to OnModelCreating
modelBuilder.ApplyConfiguration(new FundoConfiguration(_currentCompanyService));
modelBuilder.ApplyConfiguration(new ConsultoriaFundoConfiguration(_currentCompanyService));
modelBuilder.ApplyConfiguration(new CustodianteConfiguration(_currentCompanyService));
modelBuilder.ApplyConfiguration(new CedenteConfiguration(_currentCompanyService));
modelBuilder.ApplyConfiguration(new TipoAtivoConfiguration(_currentCompanyService));
```

---

## Audit Integration

Add new `ActionType` values to the existing `ActionType.cs`:

```csharp
// Add to ActionType constants
public const string FundoCreated = "fundo:created";
public const string FundoUpdated = "fundo:updated";
public const string FundoDeleted = "fundo:deleted";
public const string ConsultoriaCreated = "consultoria:created";
public const string ConsultoriaUpdated = "consultoria:updated";
public const string CustodianteCreated = "custodiante:created";
public const string CustodianteUpdated = "custodiante:updated";
public const string CedenteCreated = "cedente:created";
public const string CedenteUpdated = "cedente:updated";
public const string TipoAtivoCreated = "tipo-ativo:created";
public const string TipoAtivoUpdated = "tipo-ativo:updated";
```

---

## Frontend Integration — Client SPA

### Route Addition

```typescript
// Add to router.tsx — under authenticatedRoute
const fundosRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fundos",
  component: FundosPage,
});

// Update route tree
authenticatedRoute.addChildren([
  dashboardRoute,
  employeesRoute,
  accessGroupsRoute,
  fundosRoute,      // ← NEW
  profileRoute,
]);
```

### New Page & Component Structure

```
frontend/client/src/components/
├── pages/
│   ├── FundosPage.tsx                ← NEW — main fundos list + sub-tabs
│   └── (existing pages...)
├── organisms/
│   ├── FundosTable.tsx               ← NEW — paginated table of fundos
│   ├── FundoDetailsPanel.tsx         ← NEW — detail view with cedentes/tipos
│   └── (existing organisms...)
├── molecules/
│   ├── CreateFundoDialog.tsx         ← NEW — form with consultoria/custodiante selects
│   ├── EditFundoDialog.tsx           ← NEW — edit fundo name, custodiante, consultoria
│   ├── DeleteFundoDialog.tsx         ← NEW — confirmation delete
│   ├── CreateConsultoriaDialog.tsx   ← NEW
│   ├── CreateCustodianteDialog.tsx   ← NEW
│   ├── CreateCedenteDialog.tsx       ← NEW
│   ├── CedenteTipoAtivoSelector.tsx ← NEW — multi-select for tipos ativo
│   ├── FundoCedenteSelector.tsx     ← NEW — multi-select for cedentes
│   ├── FundoTipoAtivoSelector.tsx   ← NEW — multi-select for tipos ativo
│   ├── ConsultoriasTable.tsx         ← NEW — sub-table in fundos page
│   ├── CustodiantesTable.tsx        ← NEW — sub-table in fundos page
│   ├── CedentesTable.tsx             ← NEW — sub-table in fundos page
│   ├── TiposAtivoTable.tsx           ← NEW — sub-table in fundos page
│   └── (existing molecules...)
└── atoms/
    └── (existing atoms — no new atoms needed)
```

### Sidebar Update

Add "Fundos" navigation item to the `Sidebar.tsx` component:

```typescript
// In Sidebar.tsx — after Access Groups menu item
{ permissions.includes('fundos:read') && (
  <SidebarItem icon={Landmark} label="Fundos" to="/fundos" />
)}
```

### API Client Extension

Add to `api.ts` in the client SPA:

```typescript
// Fundos API client
export interface FundoDto {
  id: string;
  nome: string;
  cnpj: string | null;
  custodianteId: string;
  custodianteNome: string;
  consultoriaFundoId: string;
  consultoriaNome: string;
  cedentes: { id: string; nome: string }[];
  tiposAtivo: { id: string; nome: string }[];
  createdAt: string;
}

export async function getFundos(companyId: string, params?: { page?: number; pageSize?: number; search?: string }): Promise<PaginatedFundosResult> { ... }
export async function getFundoDetails(companyId: string, fundoId: string): Promise<FundoDto> { ... }
export async function createFundo(companyId: string, data: CreateFundoRequest): Promise<{ fundoId: string }> { ... }
export async function updateFundo(companyId: string, fundoId: string, data: UpdateFundoRequest): Promise<void> { ... }
export async function deleteFundo(companyId: string, fundoId: string): Promise<void> { ... }

// Consultoria, Custodiante, Cedente, TipoAtivo endpoints follow same pattern
```

### Types Extension

Add to `types.ts`:

```typescript
export interface FundoListItemDto {
  id: string;
  nome: string;
  cnpj: string | null;
  custodianteNome: string;
  consultoriaNome: string;
  cedenteCount: number;
  tiposAtivoCount: number;
}

export interface PaginatedFundosResult {
  items: FundoListItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}
```

### Zod Validation Schemas

Add to `validation-schemas.ts`:

```typescript
export const createFundoSchema = z.object({
  nome: z.string().min(1, "Nome é obrigatório"),
  cnpj: z.string().optional(),  // validated server-side via Cnpj value object
  custodianteId: z.string().uuid("Custodiante inválido"),
  consultoriaFundoId: z.string().uuid("Consultoria inválida"),
  cedenteIds: z.array(z.string().uuid()).min(1, "Pelo menos um cedente é obrigatório"),
  tipoAtivoIds: z.array(z.string().uuid()).min(1, "Pelo menos um tipo de ativo é obrigatório"),
});
```

---

## Frontend Integration — Backoffice SPA

### New Page

```
frontend/backoffice/src/components/pages/
├── AdminFundosPage.tsx              ← NEW — cross-company fundos listing for admin
└── (existing pages...)
```

### Route Addition

```typescript
// Add to backoffice router.tsx
const adminFundosRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/fundos",
  component: () => <AdminLayout><AdminFundosPage /></AdminLayout>,
});
```

### Sidebar

Add "Fundos" item to `AdminLayout` sidebar — visible to all authenticated admins.

---

## Database Schema — New Tables

```sql
-- Fundos
CREATE TABLE fundos (
    id              UUID PRIMARY KEY,
    nome            VARCHAR(200) NOT NULL,
    cnpj            VARCHAR(14),
    custodiante_id  UUID NOT NULL REFERENCES custodiantes(id) ON DELETE RESTRICT,
    consultoria_fundo_id UUID NOT NULL REFERENCES consultorias_fundo(id) ON DELETE RESTRICT,
    company_id      UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    created_at      TIMESTAMPTZ NOT NULL,
    CONSTRAINT IX_fundos_nome_company UNIQUE (nome, company_id)
);

-- Consultorias
CREATE TABLE consultorias_fundo (
    id          UUID PRIMARY KEY,
    nome        VARCHAR(200) NOT NULL,
    cnpj        VARCHAR(14) NOT NULL,
    company_id  UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    created_at  TIMESTAMPTZ NOT NULL,
    CONSTRAINT IX_consultorias_cnpj_company UNIQUE (cnpj, company_id)
);

-- Custodiantes
CREATE TABLE custodiantes (
    id          UUID PRIMARY KEY,
    nome        VARCHAR(200) NOT NULL,
    cnpj        VARCHAR(14) NOT NULL,
    company_id  UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    created_at  TIMESTAMPTZ NOT NULL,
    CONSTRAINT IX_custodiantes_cnpj_company UNIQUE (cnpj, company_id)
);

-- Cedentes
CREATE TABLE cedentes (
    id          UUID PRIMARY KEY,
    nome        VARCHAR(200) NOT NULL,
    cnpj        VARCHAR(14) NOT NULL,
    company_id  UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    created_at  TIMESTAMPTZ NOT NULL,
    CONSTRAINT IX_cedentes_cnpj_company UNIQUE (cnpj, company_id)
);

-- Tipos Ativo
CREATE TABLE tipos_ativo (
    id          UUID PRIMARY KEY,
    nome        VARCHAR(100) NOT NULL,
    company_id  UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    CONSTRAINT IX_tipos_ativo_nome_company UNIQUE (nome, company_id)
);

-- Join table: Fundo ↔ Cedente
CREATE TABLE fundo_cedentes (
    fundo_id    UUID NOT NULL REFERENCES fundos(id) ON DELETE CASCADE,
    cedente_id UUID NOT NULL REFERENCES cedentes(id) ON DELETE CASCADE,
    PRIMARY KEY (fundo_id, cedente_id)
);

-- Join table: Cedente ↔ TipoAtivo
CREATE TABLE cedente_tipo_ativo (
    cedente_id    UUID NOT NULL REFERENCES cedentes(id) ON DELETE CASCADE,
    tipo_ativo_id UUID NOT NULL REFERENCES tipos_ativo(id) ON DELETE CASCADE,
    PRIMARY KEY (cedente_id, tipo_ativo_id)
);

-- Join table: Fundo ↔ TipoAtivo
CREATE TABLE fundo_tipo_ativo (
    fundo_id      UUID NOT NULL REFERENCES fundos(id) ON DELETE CASCADE,
    tipo_ativo_id UUID NOT NULL REFERENCES tipos_ativo(id) ON DELETE CASCADE,
    PRIMARY KEY (fundo_id, tipo_ativo_id)
);
```

---

## Data Flow — Create Fundo

```
User opens Fundos page → clicks "Novo Fundo"
  │
  ▼
React SPA shows CreateFundoDialog
  ├── Fetches Consultorias dropdown (GET /api/companies/{id}/fundos/consultorias)
  ├── Fetches Custodiantes dropdown (GET /api/companies/{id}/fundos/custodiantes)
  ├── Fetches Cedentes multi-select (GET /api/companies/{id}/fundos/cedentes)
  └── Fetches Tipos Ativo multi-select (GET /api/companies/{id}/fundos/tipos-ativo)
  │
  ▼ User fills form + submits
POST /api/companies/{companyId}/fundos
  credentials: include (httpOnly cookie)
  body: { nome, cnpj?, custodianteId, consultoriaFundoId, cedenteIds[], tipoAtivoIds[] }
  │
  ▼
ClientClaimsMiddleware → resolves CompanyId + Permissions
  │
  ▼
FundosController.CreateFundo
  ├── Company isolation: companyId != _currentCompanyService.CompanyId → 403
  ├── Permission check: [Authorize(Policy = "FundosWrite")]
  ├── FluentValidation
  └── CreateFundoCommandHandler
       ├── Validate Custodiante exists + belongs to company
       ├── Validate ConsultoriaFundo exists + belongs to company
       ├── Fundo.Create() — domain invariants enforced
       ├── IFundoRepository.AddAsync() — EF Core persists
       │    └── HasQueryFilter ensures company isolation
       └── IAuditService.LogAsync()
  │
  ▼
201 Created → { fundoId }
  │
  ▼
React SPA refreshes FundosTable
```

---

## Modified vs New Components

### NEW Components (no modifications to existing code)

| Layer | Component | Notes |
|-------|-----------|-------|
| Domain | `FundosAggregate/` folder + 8 files | New aggregate roots, entities, join entities |
| Domain | `IFundoRepository`, `ICustodianteRepository`, etc. | 5 new interfaces |
| Application | `Fundos/` folder + ~35 files | Commands, queries, handlers, validators, DTOs |
| Infrastructure | `FundoRepository.cs`, etc. | 5 new repositories |
| Infrastructure | `FundoConfiguration.cs`, etc. | 5 new EF Core configs |
| API | `FundosController.cs` | 1 controller with ~20 endpoints |
| API | `AdminFundosController.cs` | Admin cross-company endpoints |
| Frontend Client | `FundosPage.tsx` + organisms/molecules | ~15 new components |
| Frontend Backoffice | `AdminFundosPage.tsx` | 1 new page + molecules |

### MODIFIED Components (existing code changes)

| Layer | Component | Change | Risk |
|-------|-----------|--------|------|
| Domain | `Permissions.cs` | Add 3 permission constants (FundosRead/Write/Delete) | LOW — additive |
| Domain | `ActionType.cs` | Add ~12 audit action constants | LOW — additive |
| API | `PermissionPolicyConstants.cs` | Add 3 policy constants | LOW — additive |
| API | `PermissionAuthorizationHandler.cs` | No change — already reads from `ICurrentCompanyPermissionsService` | NONE |
| API | `ClientClaimsMiddleware.cs` | PJ owner already gets `Permissions.All` — auto-includes new permissions | NONE |
| Infrastructure | `AppDbContext.cs` | Add 5 DbSets + 5 ApplyConfiguration calls | LOW — additive |
| Infrastructure | `DependencyInjection.cs` | Add 5 repository registrations | LOW — additive |
| Application | `DependencyInjection.cs` | Add ~30 handler/validator registrations | LOW — additive |
| Frontend Client | `router.tsx` | Add fundosRoute to tree | LOW — additive |
| Frontend Client | `Sidebar.tsx` | Add Fundos nav item with permission check | LOW — additive |
| Frontend Client | `api.ts` | Add ~15 API functions | LOW — additive |
| Frontend Client | `types.ts` | Add Fundos DTOs | LOW — additive |
| Frontend Client | `validation-schemas.ts` | Add Fundos Zod schemas | LOW — additive |
| Frontend Backoffice | `router.tsx` | Add adminFundosRoute | LOW — additive |

**Zero breaking changes.** All modifications are additive — adding constants, DbSets, DI registrations, routes. No existing behavior is altered.

---

## Build Order — Phase Dependencies

```
1. Domain Layer (Fundos entities + repositories interfaces)
   ├── No dependencies on Infrastructure or API
   ├── Testable in isolation with unit tests
   └── Cnpj value object already exists — reuse

2. Infrastructure Layer (EF Core configs + repositories)
   ├── Depends on: Domain (entities, repository interfaces)
   ├── Requires: new EF Core migration after configs are complete
   └── HasQueryFilter pattern well-established — copy from EmployeeConfiguration

3. Application Layer (Commands, Queries, Handlers, Validators, DTOs)
   ├── Depends on: Domain (entities, repository interfaces)
   ├── AuditService already exists — reuse for fundos audit
   └── Manual DI registration follows established pattern

4. API Layer (Controllers + Permission Policies)
   ├── Depends on: Application (handlers, validators)
   ├── Depends on: Domain (PermissionPolicies constants, ActionType constants)
   └── FundosController follows CompaniesController pattern exactly

5. Frontend — Client SPA (FundosPage + components + routes + API client)
   ├── Depends on: API layer (endpoints must exist)
   ├── Can mock API during development
   └── Atomic Design components follow established patterns

6. Frontend — Backoffice SPA (AdminFundosPage)
   ├── Depends on: admin API endpoints
   └── Simpler — read-only listing for admin support

7. EF Core Migration
   ├── Run after all entity configurations are complete
   └── Single migration for all 8 new tables

8. Integration Tests
   ├── Depends on: all layers complete
   └── Testcontainers for PostgreSQL + Fundo CRUD scenarios
```

**Critical dependency:** Steps 1-4 must be sequential (Domain → Infrastructure → Application → API). Steps 5-6 can run in parallel after step 4. Step 7 runs once after step 2 (migration needs all configs). Step 8 runs after all.

---

## Patterns to Follow

### Pattern 1: Company Isolation via HasQueryFilter
**What:** Every Company-scoped entity gets `HasQueryFilter(e => e.CompanyId == _currentCompanyService.CompanyId)` in its EF Core configuration.
**When:** All new entities with CompanyId.
**Example:** See `EmployeeConfiguration.cs` line 90 — exact same pattern.

### Pattern 2: Aggregate with Owned Collections
**What:** Fundo owns FundoCedente and FundoTipoAtivo as collections. Managed through add/remove methods on aggregate root.
**When:** Entities that have no independent lifecycle — they only exist as relationships.
**Example:** `fundo.AddCedente(cedenteId)` / `fundo.RemoveCedente(cedenteId)`.

### Pattern 3: CQRS Manual DI
**What:** One record type per command, one handler class, one validator. Registered in `DependencyInjection.cs`.
**When:** Every write operation and every read operation.
**Existing example:** `RegisterEmployeeCommand` → `RegisterEmployeeCommandHandler` → `RegisterEmployeeCommandValidator`.

### Pattern 4: Audit on Every Mutation
**What:** Every command handler calls `IAuditService.LogAsync()` with actor, action type, and details.
**When:** After successful persistence in every command handler.
**Existing:** All existing command handlers follow this.

### Pattern 5: FK References Without Navigation Properties
**What:** Fundo stores `CustodianteId` and `ConsultoriaFundoId` as Guid FK, but no `Custodiante` or `ConsultoriaFundo` navigation property.
**When:** Cross-aggregate references. Aggregates reference each other by ID, not by object reference.
**Existing:** Employee.CompanyId — same pattern (line 21-22 of Employee.cs).

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: Navigation Properties Across Aggregates
**What:** Adding `public Custodiante Custodiante { get; }` on Fundo.
**Why bad:** Violates DDD aggregate boundary. Loading a Fundo shouldn't implicitly load a Custodiante. Use explicit repository queries instead. EF Core lazy loading makes this worse.
**Instead:** `CustodianteId` FK only. Handler loads Custodiante explicitly when needed.

### Anti-Pattern 2: Separate Repository Per Join Entity
**What:** `IFundoCedenteRepository`, `ICedenteTipoAtivoRepository`.
**Why bad:** Join entities are owned by aggregates — they have no independent lifecycle. Exposing them through separate repositories breaks aggregate encapsulation.
**Instead:** Manage through aggregate methods (`fundo.AddCedente()`) and persist via the owning aggregate's repository.

### Anti-Pattern 3: Fat FundosController
**What:** One controller with 20+ endpoints and 15+ injected handlers.
**Why bad:** Constructor injection explosion, hard to test, hard to read.
**Instead:** Consider splitting into sub-controllers if it exceeds 20 handlers injected. `FundosController` (CRUD fundo only) + `FundosCatalogController` (consultorias, custodiantes, cedentes, tipos). OR keep in one controller since the existing `CompaniesController` already has 11+ handlers and works fine.

### Anti-Pattern 4: Global TipoAtivo
**What:** Making TipoAtivo a shared lookup across all companies (no CompanyId).
**Why bad:** Breaks multi-tenancy. Company A's "CDB" might mean something different than Company B's "CDB". Each company defines its own classification.
**Instead:** TipoAtivo with CompanyId — company-scoped lookup entity.

### Anti-Pattern 5: Skipping Audit on Fundos Mutations
**What:** Not calling `IAuditService.LogAsync()` in Fundos command handlers.
**Why bad:** Inconsistent with existing patterns. Fundo creation/modification/deletion are auditable actions. The admin backoffice expects audit trail.
**Instead:** Every command handler logs to audit — same as Employee/Company handlers.

---

## Scalability Considerations

| Concern | At 100 fundos | At 10K fundos | At 1M fundos |
|---------|---------------|---------------|--------------|
| Fundo listing query | Simple `Skip/Take` | Add composite index on `company_id + nome` | Partition by `company_id`, materialized views |
| Join table queries | EF Core `Include` | Split queries (`AsSplitQuery`) | Denormalize cedente names into fundo read model |
| CNPJ uniqueness check | `EXISTS` query | Index on `(cnpj, company_id)` | Same — B-tree index handles this fine |
| Dropdown data for Create form | Single query per entity | Cache consultorias/custodiantes per session | Redis cache for dropdowns, 5min TTL |

**Current scope (100-1000 fundos per company):** No optimization needed. Standard EF Core with proper indexes handles this volume.

---

## Component Communication Map

```
┌──────────────┐
│ FundosPage   │──── GET /fundos ────▶ FundosController ──▶ GetFundosQueryHandler ──▶ IFundoRepository
│ (React)      │                                                                              │
│              │──── POST /fundos ──▶ FundosController ──▶ CreateFundoCommandHandler            │
│              │     │                                        ├── ICustodianteRepository     │
│              │     │                                        ├── IConsultoriaFundoRepository│
│              │     │                                        ├── IFundoRepository ─────────┘
│              │     │                                        └── IAuditService
│              │                                                                              │
│              │──── PUT /fundos/{id} ──▶ FundosController ──▶ UpdateFundoCommandHandler        │
│              │     │                                        └── IFundoRepository
│              │                                                                              │
│              │──── DELETE /fundos/{id} ▶ FundosController ──▶ DeleteFundoCommandHandler       │
│              │                                              └── IFundoRepository
│              │                                                                              │
│              │──── GET /fundos/consultorias ▶ FundosController ──▶ GetConsultoriasQueryHandler│
│              │                                              └── IConsultoriaFundoRepository
│              │                                                                              │
│              │──── GET /fundos/custodiantes ▶ FundosController ──▶ GetCustodiantesQueryHandler│
│              │                                              └── ICustodianteRepository
│              │                                                                              │
│              │──── GET /fundos/cedentes ──▶ FundosController ──▶ GetCedentesQueryHandler      │
│              │                                              └── ICedenteRepository
│              │                                                                              │
│              │──── GET /fundos/tipos-ativo ▶ FundosController ──▶ GetTiposAtivoQueryHandler   │
│                                              └── ITipoAtivoRepository
└──────────────┘
```

---

## Sources

- **Direct codebase analysis** — ALL patterns and recommendations derived from existing code in `src/Onboarding.*` and `frontend/` — HIGH confidence
- [Microsoft DDD microservice guide](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice) — Aggregate design patterns — HIGH confidence
- [EF Core HasQueryFilter](https://learn.microsoft.com/en-us/ef/core/querying/filters) — Multi-tenancy pattern — HIGH confidence
- [EF Core Owned Entities / Collections](https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities) — Join entity mapping — HIGH confidence