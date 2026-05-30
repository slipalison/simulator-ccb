using Onboarding.Domain.Aggregates.EmployeeAggregate;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Repository contract for AccessGroup entity.
/// </summary>
public interface IAccessGroupRepository
{
    Task AddAsync(AccessGroup accessGroup, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<AccessGroup> accessGroups, CancellationToken ct = default);
    Task SaveAsync(AccessGroup accessGroup, CancellationToken ct = default);
    Task<AccessGroup?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AccessGroup>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default);

    /// <summary>
    /// Batch fetch by IDs — single query, AsNoTracking (PERF-03).
    /// Used by paginated employee listing to avoid N sequential GetByIdAsync calls.
    /// </summary>
    Task<IReadOnlyList<AccessGroup>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<AccessGroup?> GetByCompanyAndNameAsync(Guid companyId, string name, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}