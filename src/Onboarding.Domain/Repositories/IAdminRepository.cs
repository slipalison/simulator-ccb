using Onboarding.Domain.Aggregates.ClientAggregate;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Admin operations on Client aggregate.
/// Defined in the Domain layer as an abstraction — implementation lives in Infrastructure (Plan 02).
/// </summary>
public interface IAdminRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, string? status, CancellationToken ct = default);

    Task UpdateAsync(Client client, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
