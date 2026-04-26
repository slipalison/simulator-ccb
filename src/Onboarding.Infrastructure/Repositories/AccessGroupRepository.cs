using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// Thin EF Core repository wrapper — tested via integration tests.
/// HasQueryFilter on AccessGroupConfiguration ensures company isolation (D-17).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AccessGroupRepository : IAccessGroupRepository
{
    private readonly AppDbContext _db;

    public AccessGroupRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(AccessGroup accessGroup, CancellationToken ct = default)
    {
        await _db.AccessGroups.AddAsync(accessGroup, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<AccessGroup> accessGroups, CancellationToken ct = default)
    {
        await _db.AccessGroups.AddRangeAsync(accessGroups, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(AccessGroup accessGroup, CancellationToken ct = default)
    {
        _db.AccessGroups.Update(accessGroup);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AccessGroup?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.AccessGroups.FindAsync([id], ct);

    public async Task<IReadOnlyList<AccessGroup>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
        => await _db.AccessGroups
            .Where(a => a.CompanyId == companyId)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

    public async Task<AccessGroup?> GetByCompanyAndNameAsync(Guid companyId, string name, CancellationToken ct = default)
        => await _db.AccessGroups
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Name == name, ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var accessGroup = await _db.AccessGroups.FindAsync([id], ct);
        if (accessGroup is not null)
        {
            _db.AccessGroups.Remove(accessGroup);
            await _db.SaveChangesAsync(ct);
        }
    }
}