using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// Thin EF Core repository wrapper — tested via integration tests.
/// HasQueryFilter on CedenteConfiguration ensures company isolation (D-01).
/// IgnoreQueryFilters for uniqueness checks and direct lookups (D-12).
/// Shadow properties for CedenteDocumento DU persistence (D-09).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CedenteRepository : ICedenteRepository
{
    private readonly AppDbContext _db;

    public CedenteRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Cedente cedente, CancellationToken ct = default)
    {
        // Set shadow properties from CedenteDocumento DU per D-09
        SetShadowProperties(cedente);
        await _db.Cedentes.AddAsync(cedente, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(Cedente cedente, CancellationToken ct = default)
    {
        // Sync shadow properties from CedenteDocumento DU per D-09
        SetShadowProperties(cedente);
        _db.Cedentes.Update(cedente);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Cedente?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cedente = await _db.Cedentes
            .IgnoreQueryFilters()
            .Include(c => c.TiposAtivo)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (cedente is not null)
            ReconstructDocumento(cedente);

        return cedente;
    }

    public async Task<bool> ExistsByDocumentoAsync(
        CedenteDocumento documento, Guid companyId, CancellationToken ct = default)
    {
        // Match on CedenteDocumento DU type and query shadow properties per D-09/D-10
        if (documento.IsPf)
        {
            var pf = (CedenteDocumento.PessoaFisica)documento;
            return await _db.Cedentes.IgnoreQueryFilters()
                .AnyAsync(c =>
                    c.ClienteId == companyId
                    && EF.Property<string>(c, "DocumentoTipo") == "PF"
                    && EF.Property<string>(c, "CpfValue") == pf.Cpf.Value, ct);
        }
        else
        {
            var pj = (CedenteDocumento.PessoaJuridica)documento;
            return await _db.Cedentes.IgnoreQueryFilters()
                .AnyAsync(c =>
                    c.ClienteId == companyId
                    && EF.Property<string>(c, "DocumentoTipo") == "PJ"
                    && EF.Property<string>(c, "CnpjCedenteValue") == pj.Cnpj.Value, ct);
        }
    }

    public async Task<(IReadOnlyList<Cedente> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = _db.Cedentes
            .IgnoreQueryFilters()
            .Where(c => c.ClienteId == companyId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            var digitsOnly = new string(normalized.Where(char.IsDigit).ToArray());

            query = query.Where(c =>
                EF.Functions.ILike(c.Nome, $"%{normalized}%") ||
                (digitsOnly.Length > 0 &&
                    (EF.Property<string>(c, "CpfValue").Contains(digitsOnly) ||
                     EF.Property<string>(c, "CnpjCedenteValue").Contains(digitsOnly))));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Nome)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // CR-03: Reconstruct Documento from shadow properties after materialization
        foreach (var cedente in items)
            ReconstructDocumento(cedente);

        return (items.AsReadOnly(), totalCount);
    }

    /// <summary>
    /// Sets shadow properties from CedenteDocumento DU — required for persistence per D-09.
    /// </summary>
    private void SetShadowProperties(Cedente cedente)
    {
        var entry = _db.Entry(cedente);
        if (cedente.Documento.IsPf)
        {
            var pf = (CedenteDocumento.PessoaFisica)cedente.Documento;
            entry.Property("DocumentoTipo").CurrentValue = "PF";
            entry.Property<string?>("CpfValue").CurrentValue = pf.Cpf.Value;
            entry.Property<string?>("CnpjCedenteValue").CurrentValue = null;
        }
        else
        {
            var pj = (CedenteDocumento.PessoaJuridica)cedente.Documento;
            entry.Property("DocumentoTipo").CurrentValue = "PJ";
            entry.Property<string?>("CpfValue").CurrentValue = null;
            entry.Property<string?>("CnpjCedenteValue").CurrentValue = pj.Cnpj.Value;
        }
    }

    /// <summary>
    /// Reconstructs Documento from shadow properties after EF Core materialization (CR-03).
    /// Documento has private setter — reflection required since domain model doesn't expose a setter method.
    /// </summary>
    private void ReconstructDocumento(Cedente cedente)
    {
        var entry = _db.Entry(cedente);
        var tipo = entry.Property<string>("DocumentoTipo").CurrentValue;
        if (tipo == "PF")
        {
            var cpfValue = entry.Property<string?>("CpfValue").CurrentValue;
            if (cpfValue is not null)
                typeof(Cedente).GetProperty(nameof(Cedente.Documento))!
                    .SetValue(cedente, CedenteDocumento.Pf(Cpf.Create(cpfValue)));
        }
        else if (tipo == "PJ")
        {
            var cnpjValue = entry.Property<string?>("CnpjCedenteValue").CurrentValue;
            if (cnpjValue is not null)
                typeof(Cedente).GetProperty(nameof(Cedente.Documento))!
                    .SetValue(cedente, CedenteDocumento.Pj(Cnpj.Create(cnpjValue)));
        }
    }
}