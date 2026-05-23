using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// Admin audit log repository — append-only. NO Update or Delete methods.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AdminAuditLogRepository : IAdminAuditLogRepository
{
    private readonly AppDbContext _db;

    public AdminAuditLogRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(AdminAuditLog log, CancellationToken ct = default)
        => await _db.AdminAuditLogs.AddAsync(log, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);

    public async Task<(IReadOnlyList<AdminAuditLog> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        ActionType? actionType = null,
        string? adminUserName = null,
        string? entityType = null,
        Guid? entityId = null,
        CancellationToken ct = default)
    {
        var query = _db.AdminAuditLogs.AsNoTracking();

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        if (actionType.HasValue)
            query = query.Where(a => a.ActionType == actionType.Value);

        if (!string.IsNullOrWhiteSpace(adminUserName))
            query = query.Where(a => a.AdminUserName.Contains(adminUserName));

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
