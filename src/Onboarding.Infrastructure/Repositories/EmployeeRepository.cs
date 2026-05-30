using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// Thin EF Core repository wrapper — tested via integration tests.
/// HasQueryFilter on EmployeeConfiguration ensures company isolation (D-17).
/// </summary>
public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _db;

    public EmployeeRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Employee employee, CancellationToken ct = default)
    {
        await _db.Employees.AddAsync(employee, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(Employee employee, CancellationToken ct = default)
    {
        _db.Employees.Update(employee);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Employees
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Employee?> GetByKeycloakSubAsync(string keycloakSub, CancellationToken ct = default)
        => await _db.Employees
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.KeycloakUserId == keycloakSub, ct);

    public async Task<bool> ExistsByCpfAsync(string cpf, CancellationToken ct = default)
    {
        var cpfVo = Cpf.Create(cpf);
        return await _db.Employees
            .IgnoreQueryFilters()
            .AnyAsync(e => e.Cpf == cpfVo, ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        var emailVo = Email.Create(email);
        return await _db.Employees
            .IgnoreQueryFilters()
            .AnyAsync(e => e.Email == emailVo, ct);
    }

    /// <summary>
    /// Paginated employee listing scoped to a company (D-17).
    /// HasQueryFilter already enforces CompanyId, but we pass it explicitly for
    /// admin endpoints that bypass the filter.
    /// </summary>
    public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int page, int pageSize, string? search, string? status,
        CancellationToken ct = default)
    {
        // email/cpf are value-converter columns — opaque to LINQ (.Value cannot be translated).
        // When search is present, use FromSqlInterpolated (parameterized, no injection) on raw
        // columns, then compose CompanyId/status/order/paging as LINQ.
        // IgnoreQueryFilters() suppresses HasQueryFilter (ICurrentCompanyService.CompanyId = Guid.Empty at runtime).
        IQueryable<Employee> query;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var like = $"%{search.Trim()}%";
            var digits = new string(search.Where(char.IsDigit).ToArray());
            var digitsLike = $"%{digits}%";
            var hasDigits = digits.Length > 0;
            query = _db.Employees
                .FromSqlInterpolated($@"
                    SELECT * FROM employees
                    WHERE nome ILIKE {like}
                       OR email ILIKE {like}
                       OR ({hasDigits} AND cpf ILIKE {digitsLike})")
                .IgnoreQueryFilters()
                .AsNoTracking();
        }
        else
        {
            query = _db.Employees.IgnoreQueryFilters().AsNoTracking();
        }

        // Explicit CompanyId filter — controller validates companyId matches current user's company.
        query = query.Where(e => e.CompanyId == companyId);

        // Status filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.ToLowerInvariant();
            if (normalizedStatus == "active")
                query = query.Where(e => !e.DeletedAt.HasValue);
            else if (normalizedStatus == "deleted")
                query = query.Where(e => e.DeletedAt.HasValue);
        }
        else
        {
            // Default: show only active employees
            query = query.Where(e => !e.DeletedAt.HasValue);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(e => e.Nome)
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.AsReadOnly(), totalCount);
    }

    /// <summary>
    /// Paginated employee listing across ALL companies — admin bypasses HasQueryFilter (MGMT-01).
    /// </summary>
    public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAllAsync(
        int page, int pageSize, string? search, string? status,
        CancellationToken ct = default)
    {
        // Admin endpoint — bypasses HasQueryFilter to see all companies' employees.
        // email/cpf are value-converter columns — opaque to LINQ (.Value cannot be translated).
        // When search is present, use FromSqlInterpolated (parameterized, no injection) on raw columns.
        IQueryable<Employee> query;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var like = $"%{search.Trim()}%";
            var digits = new string(search.Where(char.IsDigit).ToArray());
            var digitsLike = $"%{digits}%";
            var hasDigits = digits.Length > 0;
            query = _db.Employees
                .FromSqlInterpolated($@"
                    SELECT * FROM employees
                    WHERE nome ILIKE {like}
                       OR email ILIKE {like}
                       OR ({hasDigits} AND cpf ILIKE {digitsLike})")
                .IgnoreQueryFilters()
                .AsNoTracking();
        }
        else
        {
            query = _db.Employees.IgnoreQueryFilters().AsNoTracking();
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.ToLowerInvariant();
            if (normalizedStatus == "active")
                query = query.Where(e => !e.DeletedAt.HasValue);
            else if (normalizedStatus == "deleted")
                query = query.Where(e => e.DeletedAt.HasValue);
        }
        else
        {
            // Default: show only active employees
            query = query.Where(e => !e.DeletedAt.HasValue);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(e => e.Nome)
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.AsReadOnly(), totalCount);
    }

    /// <summary>
    /// Fetches a single employee by ID, bypassing HasQueryFilter — admin lookup (MGMT-02).
    /// </summary>
    public async Task<Employee?> GetByIdIgnoreFilterAsync(Guid id, CancellationToken ct = default)
        => await _db.Employees
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var employee = await _db.Employees
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        if (employee is not null)
        {
            _db.Employees.Remove(employee);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> ExistsByAccessGroupIdAsync(Guid accessGroupId, CancellationToken ct = default)
        => await _db.Employees
            .IgnoreQueryFilters()
            .AnyAsync(e => e.AccessGroupId == accessGroupId, ct);
}