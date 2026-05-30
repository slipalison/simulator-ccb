using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// Thin EF Core repository wrapper — tested via integration tests.
/// </summary>
public sealed class CompanyRepository : ICompanyRepository
{
    private readonly AppDbContext _db;

    public CompanyRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Company company, CancellationToken ct = default)
    {
        await _db.Companies.AddAsync(company, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(Company company, CancellationToken ct = default)
    {
        _db.Companies.Update(company);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Company>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids as ICollection<Guid> ?? ids.ToList();
        if (idList.Count == 0)
            return [];

        return await _db.Companies
            .AsNoTracking()
            .Where(c => idList.Contains(c.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsByCnpjAsync(string cnpj, CancellationToken ct = default)
    {
        var cnpjVo = Cnpj.Create(cnpj);
        return await _db.Companies.AnyAsync(c => c.Cnpj == cnpjVo, ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        var emailVo = Email.Create(email);
        return await _db.Companies.AnyAsync(c => c.Email == emailVo, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var company = await _db.Companies.FindAsync([id], ct);
        if (company is not null)
        {
            _db.Companies.Remove(company);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<Company?> GetByKeycloakSubAsync(string keycloakSub, CancellationToken ct = default)
    {
        return await _db.Companies
            .FirstOrDefaultAsync(c => c.KeycloakUserId == keycloakSub, ct);
    }

    public async Task<Company?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.ToLowerInvariant();
        var emailVo = Email.Create(normalized);
        return await _db.Companies
            .FirstOrDefaultAsync(c => c.Email == emailVo, ct);
    }

    public async Task<(IReadOnlyList<Company> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, string? status, CancellationToken ct = default)
    {
        IQueryable<Company> query;

        // Search filter: razaoSocial, email, or CNPJ.
        // email/cnpj are value-converter columns — opaque to LINQ (`c.Email.Value` cannot be
        // translated, threw at runtime). Use a parameterized SQL ILIKE over the raw columns;
        // status/order/paging compose as normal LINQ. FromSqlInterpolated parameterizes every
        // value (no SQL injection). Company has no global query filter, so D-5 is unaffected.
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

        // Status filter: active (not deleted), deleted. Filter on the mapped deleted_at column —
        // IsDeleted (=> DeletedAt.HasValue) is a computed property EF cannot translate.
        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusLower = status.ToLowerInvariant();
            query = statusLower switch
            {
                "active" => query.Where(c => c.DeletedAt == null),
                "deleted" => query.Where(c => c.DeletedAt != null),
                _ => query
            };
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.TermsAcceptance.AcceptedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}