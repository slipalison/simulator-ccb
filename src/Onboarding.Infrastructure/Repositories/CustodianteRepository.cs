using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// Thin EF Core repository wrapper — tested via integration tests.
/// HasQueryFilter on CustodianteConfiguration ensures company isolation (D-01).
/// IgnoreQueryFilters for uniqueness checks and direct lookups (D-12).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CustodianteRepository : ICustodianteRepository
{
    private readonly AppDbContext _db;

    public CustodianteRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Custodiante custodiante, CancellationToken ct = default)
    {
        await _db.Custodiantes.AddAsync(custodiante, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(Custodiante custodiante, CancellationToken ct = default)
    {
        _db.Custodiantes.Update(custodiante);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Custodiante?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Custodiantes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<bool> ExistsByCnpjAsync(string cnpj, Guid companyId, CancellationToken ct = default)
    {
        var cnpjVo = Cnpj.Create(cnpj);
        return await _db.Custodiantes
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Cnpj == cnpjVo && c.ClienteId == companyId, ct);
    }

    public async Task<(IReadOnlyList<Custodiante> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = _db.Custodiantes
            .IgnoreQueryFilters()
            .Where(c => c.ClienteId == companyId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            var digitsOnly = new string(normalized.Where(char.IsDigit).ToArray());

            query = query.Where(c =>
                EF.Functions.ILike(c.RazaoSocial, $"%{normalized}%") ||
                (digitsOnly.Length > 0 && EF.Functions.ILike(EF.Property<string>(c, "cnpj"), "%" + digitsOnly + "%")));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.RazaoSocial)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.AsReadOnly(), totalCount);
    }
}