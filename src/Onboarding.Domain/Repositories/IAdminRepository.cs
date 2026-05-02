using Onboarding.Domain.Aggregates.CompanyAggregate;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Admin operations on Company aggregate.
/// Defined in the Domain layer as an abstraction — implementation lives in Infrastructure (Plan 02).
/// </summary>
public interface IAdminRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(IReadOnlyList<Company> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, string? status, CancellationToken ct = default);

    Task UpdateAsync(Company company, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
