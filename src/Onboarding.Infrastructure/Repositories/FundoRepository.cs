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
        IQueryable<Fundo> query;

        // cnpj is a value-converter column — opaque to LINQ (.Value cannot be translated at runtime).
        // When search is present, use FromSqlInterpolated (parameterized, no injection) on the
        // raw columns, then compose CompanyId/order/paging as LINQ. IgnoreQueryFilters() suppresses
        // the HasQueryFilter (which captures ICurrentCompanyService.CompanyId = Guid.Empty at runtime).
        if (!string.IsNullOrWhiteSpace(search))
        {
            var like = $"%{search.Trim()}%";
            var digits = new string(search.Where(char.IsDigit).ToArray());
            var digitsLike = $"%{digits}%";
            var hasDigits = digits.Length > 0;
            query = _db.Fundos
                .FromSqlInterpolated($@"
                    SELECT * FROM fundos
                    WHERE nome ILIKE {like}
                       OR ({hasDigits} AND cnpj ILIKE {digitsLike})")
                .IgnoreQueryFilters()
                .AsNoTracking();
        }
        else
        {
            query = _db.Fundos.IgnoreQueryFilters().AsNoTracking();
        }

        query = query.Where(f => f.ClienteId == companyId);

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