using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// Thin EF Core repository wrapper — tested via integration tests.
/// HasQueryFilter on FundoConfiguration ensures company isolation (D-01).
/// IgnoreQueryFilters for uniqueness checks and direct lookups (D-12).
/// </summary>
public sealed class FundoRepository : IFundoRepository
{
    private readonly AppDbContext _db;

    public FundoRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Fundo fundo, CancellationToken ct = default)
    {
        await _db.Fundos.AddAsync(fundo, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(Fundo fundo, CancellationToken ct = default)
    {
        _db.Fundos.Update(fundo);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Fundo?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Fundos
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(f => f.Cedentes)
            .Include(f => f.TiposAtivo)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<bool> ExistsByCnpjAsync(string cnpj, Guid companyId, CancellationToken ct = default)
    {
        var cnpjVo = Cnpj.Create(cnpj);
        return await _db.Fundos
            .IgnoreQueryFilters()
            .AnyAsync(f => f.Cnpj == cnpjVo && f.ClienteId == companyId, ct);
    }

    public async Task<(IReadOnlyList<Fundo> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = _db.Fundos
            .IgnoreQueryFilters()
            .Where(f => f.ClienteId == companyId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            var digitsOnly = new string(normalized.Where(char.IsDigit).ToArray());

            query = query.Where(f =>
                EF.Functions.ILike(f.Nome, $"%{normalized}%") ||
                (digitsOnly.Length > 0 && f.Cnpj.Value.Contains(digitsOnly)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(f => f.Nome)
            .ThenBy(f => f.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.AsReadOnly(), totalCount);
    }
}