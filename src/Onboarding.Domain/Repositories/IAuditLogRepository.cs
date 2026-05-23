using Onboarding.Domain.Aggregates.Audit;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Admin audit log repository — append-only. NO Update or Delete methods.
/// </summary>
public interface IAdminAuditLogRepository
{
    Task AddAsync(AdminAuditLog log, CancellationToken ct = default);
    Task<(IReadOnlyList<AdminAuditLog> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        ActionType? actionType = null,
        string? adminUserName = null,
        string? entityType = null,
        Guid? entityId = null,
        CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
