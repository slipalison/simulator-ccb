---
phase: 46-infrastructure-layer
reviewed: 2026-05-03T16:30:00Z
depth: standard
files_reviewed: 13
files_reviewed_list:
  - src/Onboarding.Infrastructure/Persistence/Configurations/FundoConfiguration.cs
  - src/Onboarding.Infrastructure/Persistence/Configurations/ConsultoriaFundoConfiguration.cs
  - src/Onboarding.Infrastructure/Persistence/Configurations/CustodianteConfiguration.cs
  - src/Onboarding.Infrastructure/Persistence/Configurations/CedenteConfiguration.cs
  - src/Onboarding.Infrastructure/Persistence/Configurations/TipoAtivoConfiguration.cs
  - src/Onboarding.Infrastructure/Persistence/Configurations/CedenteDocumentoValueConverter.cs
  - src/Onboarding.Infrastructure/Persistence/AppDbContext.cs
  - src/Onboarding.Infrastructure/Repositories/FundoRepository.cs
  - src/Onboarding.Infrastructure/Repositories/ConsultoriaFundoRepository.cs
  - src/Onboarding.Infrastructure/Repositories/CustodianteRepository.cs
  - src/Onboarding.Infrastructure/Repositories/CedenteRepository.cs
  - src/Onboarding.Infrastructure/Repositories/TipoAtivoRepository.cs
  - src/Onboarding.Infrastructure/DependencyInjection.cs
  - src/Onboarding.Infrastructure/Persistence/Migrations/20260503162256_AddFundosModule.cs
findings:
  critical: 2
  warning: 3
  info: 2
  total: 7
status: issues_found
---

# Phase 46: Code Review Report

**Reviewed:** 2026-05-03T16:30:00Z
**Depth:** standard
**Files Reviewed:** 14
**Status:** issues_found

## Summary

Reviewed 13 source files + 1 migration for the Fundos module Infrastructure Layer (Phase 46). Found **2 critical** multi-tenancy data isolation bugs, **3 warnings**, and **2 info items**. The most urgent issues are: (1) CNPJ unique indexes on Fundo, ConsultoriaFundo, and Custodiante are global instead of per-company, violating tenant isolation and preventing different companies from registering the same CNPJ; (2) Fundo is missing a database FK constraint to Company, unlike every other tenant-scoped entity in this module.

## Critical Issues

### CR-01: CNPJ unique indexes are global — must be scoped per-company

**File:** `src/Onboarding.Infrastructure/Persistence/Configurations/FundoConfiguration.cs:100-103`
**Also affects:** `ConsultoriaFundoConfiguration.cs:86-89`, `CustodianteConfiguration.cs:86-89`

**Issue:** The unique indexes on CNPJ are declared as single-column unique constraints:
```csharp
// FundoConfiguration line 100-103
builder.HasIndex(e => e.Cnpj)
    .IsUnique()
    .HasFilter("cnpj IS NOT NULL")
```
The comments claim "scoped via HasQueryFilter" but `HasQueryFilter` is a runtime EF Core filter — it has **zero effect** on database-level unique constraints. This means two different companies cannot register the same CNPJ, which breaks multi-tenancy. CedenteConfiguration correctly uses composite indexes `(ClienteId, Cnpj)` — Fundo, ConsultoriaFundo, and Custodiante must follow the same pattern.

**Fix:** Replace single-column unique indexes with composite indexes scoped per `ClienteId`:

```csharp
// FundoConfiguration — replace lines 100-103
builder.HasIndex(e => new { e.ClienteId, e.Cnpj })
    .IsUnique()
    .HasDatabaseName("IX_fundos_cliente_id_cnpj");

// ConsultoriaFundoConfiguration — replace lines 86-89
builder.HasIndex(e => new { e.ClienteId, e.Cnpj })
    .IsUnique()
    .HasDatabaseName("IX_consultoria_fundos_cliente_id_cnpj");

// CustodianteConfiguration — replace lines 86-89
builder.HasIndex(e => new { e.ClienteId, e.Cnpj })
    .IsUnique()
    .HasDatabaseName("IX_custodiantes_cliente_id_cnpj");
```

This requires a follow-up migration to drop the old single-column indexes and create the new composite ones.

### CR-02: Fundo misses database FK constraint to Company

**File:** `src/Onboarding.Infrastructure/Persistence/Configurations/FundoConfiguration.cs:32-35`
**Migration confirmation:** `src/Onboarding.Infrastructure/Persistence/Migrations/20260503162256_AddFundosModule.cs:110-141`

**Issue:** FundoConfiguration declares `ClienteId` as a plain property without a foreign key relationship to Company:
```csharp
// FundoConfiguration line 33-35
builder.Property(e => e.ClienteId)
    .HasColumnName("cliente_id")
    .IsRequired();
```
Compare with ConsultoriaFundo and Custodiante, which both declare:
```csharp
builder.HasOne<Company>()
    .WithMany()
    .HasForeignKey(e => e.ClienteId)
    .OnDelete(DeleteBehavior.Restrict);
```
The migration confirms this — no `FK_fundos_companies_cliente_id` constraint exists. Without this FK, PostgreSQL cannot enforce referential integrity between Fundo.ClienteId and the Company table. A Fundo could be created with a non-existent ClienteId.

**Fix:** Add the FK relationship in FundoConfiguration:

```csharp
builder.Property(e => e.ClienteId)
    .HasColumnName("cliente_id")
    .IsRequired();

builder.HasOne<Company>()
    .WithMany()
    .HasForeignKey(e => e.ClienteId)
    .OnDelete(DeleteBehavior.Restrict);
```

This requires a follow-up migration to add the missing FK constraint.

## Warnings

### WR-01: CedenteRepository.GetByIdAsync doesn't reconstruct Documento from shadow properties

**File:** `src/Onboarding.Infrastructure/Repositories/CedenteRepository.cs:39-43`

**Issue:** The `GetByIdAsync` method loads a Cedente entity but does **not** reconstruct the `Documento` property from shadow properties. The `CedenteDocumentoValueConverter` read path returns a placeholder `CedenteDocumento.Pf(Cpf.Create("00000000000"))`. The `SetShadowProperties` method only handles the write path. After reading from the database, `cedente.Documento` will contain the invalid placeholder value, not the real document.

**Fix:** After materializing the entity, reconstruct `Documento` from shadow properties:

```csharp
public async Task<Cedente?> GetByIdAsync(Guid id, CancellationToken ct = default)
{
    var cedente = await _db.Cedentes
        .IgnoreQueryFilters()
        .Include(c => c.TiposAtivo)
        .FirstOrDefaultAsync(c => c.Id == id, ct);

    if (cedente is null) return null;

    // Reconstruct Documento from shadow properties (D-09)
    var entry = _db.Entry(cedente);
    var tipo = entry.Property<string>("DocumentoTipo").CurrentValue;
    if (tipo == "PF")
    {
        var cpfValue = entry.Property<string?>("CpfValue").CurrentValue;
        cedente = cedente with { Documento = CedenteDocumento.Pf(Cpf.Create(cpfValue!)) };
    }
    else if (tipo == "PJ")
    {
        var cnpjValue = entry.Property<string?>("CnpjCedenteValue").CurrentValue;
        cedente = cedente with { Documento = CedenteDocumento.Pj(Cnpj.Create(cnpjValue!)) };
    }

    return cedente;
}
```

Note: Since `Cedente` is a `sealed class` (not a record), the `with` expression won't work directly. A private setter or internal method on `Cedente` is needed to rehydrate the `Documento` property, or the entity needs a `Reconstruct` factory method for this purpose.

### WR-02: LIKE wildcards in user search input not escaped

**Files:** `FundoRepository.cs:63-64`, `ConsultoriaFundoRepository.cs:61-63`, `CustodianteRepository.cs:62`, `CedenteRepository.cs:83-86`

**Issue:** Search uses `EF.Functions.ILike(f.Nome, $"%{normalized}%")` without escaping LIKE wildcard characters (`%` and `_`) in user input. A user searching for "%" or "_" would match unintended records. While EF Core parameterizes the query (preventing SQL injection), the LIKE semantics can be manipulated.

**Fix:** Escape LIKE wildcards before interpolation, or use a helper method:

```csharp
private static string EscapeLikeWildcards(string input)
    => input.Replace("%", "\\%").Replace("_", "\\_");
```
Then use with `EF.Functions.ILike(f.Nome, $"%{EscapeLikeWildcards(normalized)}%", "\\")`.

### WR-03: GetPagedByCompanyAsync queries don't filter out soft-deleted or inactive entities

**Files:** `FundoRepository.cs:49-77`, `ConsultoriaFundoRepository.cs:47-76`, `CustodianteRepository.cs:47-75`, `CedenteRepository.cs:69-99`

**Issue:** The paged listing queries have no default status filter — all entities (including inactive/deleted) are returned. EmployeeRepository explicitly filters by `DeletedAt` status. Fundos module repositories should at minimum default to showing only active entities, or provide a status parameter.

**Fix:** Add a default status filter or a `status` parameter consistent with EmployeeRepository's pattern:

```csharp
// Default: show only active (ATIVO status) entities
query = query.Where(f => f.Status == FundoStatus.ATIVO);
```

## Info

### IN-01: CedenteDocumento ValueConverter uses hardcoded placeholder CPF on read

**File:** `src/Onboarding.Infrastructure/Persistence/Configurations/CedenteDocumentoValueConverter.cs:21`

**Issue:** The read converter returns `CedenteDocumento.Pf(Cpf.Create("00000000000"))` — a real CPF value (albeit invalid). This is a code smell since the value is never supposed to be used. Consider using a distinguished value or making this clearer. However, this is acceptable as documented intentional placeholder behavior per D-09 since the repository pattern is supposed to handle reconstruction.

### IN-02: TipoAtivo correctly has no HasQueryFilter and no ICurrentCompanyService (TEN-03 verified)

**File:** `src/Onboarding.Infrastructure/Persistence/Configurations/TipoAtivoConfiguration.cs`
**File:** `src/Onboarding.Infrastructure/Repositories/TipoAtivoRepository.cs`

**Issue:** None — this is a positive verification. TipoAtivo is correctly implemented as a global entity with no company scope, no `ICurrentCompanyService`, and no `HasQueryFilter`. This matches decision TEN-03.

---

_Reviewed: 2026-05-03T16:30:00Z_
_Reviewer: the agent (gsd-code-reviewer)_
_Depth: standard_