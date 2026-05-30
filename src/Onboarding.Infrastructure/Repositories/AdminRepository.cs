using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>Thin EF Core repository — tested via integration tests.</summary>
public sealed class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _db;

    public AdminRepository(AppDbContext db) => _db = db;

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Companies.FindAsync([id], ct);

    public async Task<(IReadOnlyList<Company> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, string? status, CancellationToken ct = default)
    {
        IQueryable<Company> query;

        // Search filter: razaoSocial, email, or CNPJ (digits-only).
        // email/cnpj are value-converter columns — opaque to LINQ (.Value cannot be translated).
        // FromSqlInterpolated parameterizes every value (no injection risk).
        // This is cross-company admin — no global query filter applies here (IgnoreQueryFilters
        // is not needed because Companies has no HasQueryFilter).
        if (!string.IsNullOrWhiteSpace(search))
        {
            var like = $"%{search.Trim()}%";
            var digits = new string(search.Where(char.IsDigit).ToArray());
            var digitsLike = $"%{digits}%";
            var hasDigits = digits.Length > 0;
            query = _db.Companies.FromSqlInterpolated($@"
                SELECT * FROM companies
                WHERE razao_social ILIKE {like}
                   OR email ILIKE {like}
                   OR ({hasDigits} AND cnpj ILIKE {digitsLike})");
        }
        else
        {
            query = _db.Companies;
        }

        // Status filter on mapped column (DeletedAt); IsDeleted is a computed prop EF cannot translate.
        if (status?.ToLowerInvariant() == "deleted")
            query = query.Where(c => c.DeletedAt.HasValue);
        else
            query = query.Where(c => !c.DeletedAt.HasValue);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderBy(c => c.RazaoSocial)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.AsReadOnly(), totalCount);
    }

    public Task UpdateAsync(Company company, CancellationToken ct = default)
    {
        _db.Companies.Update(company);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);

}