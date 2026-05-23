using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;

namespace Onboarding.Infrastructure.Services;

/// <summary>
/// Implementação de IAuditService. Injeta IAdminAuditLogRepository, cria AdminAuditLog.Create(...) e persiste.
/// actorSub = Keycloak user ID (UUID string) ou email como fallback — Guid.TryParse trata os dois casos.
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly IAdminAuditLogRepository _repo;

    public AuditService(IAdminAuditLogRepository repo) => _repo = repo;

    public async Task RecordAsync(
        string actorSub,
        string actorEmail,
        ActionType action,
        Guid? targetUserId = null,
        string? targetUserName = null,
        string? details = null,
        string? ipAddress = null,
        string? entityType = null,
        Guid? entityId = null,
        CancellationToken ct = default)
    {
        var adminId = Guid.TryParse(actorSub, out var parsed) ? parsed : Guid.Empty;
        var log = AdminAuditLog.Create(adminId, actorEmail, action, targetUserId, targetUserName, details, ipAddress, entityType, entityId);
        await _repo.AddAsync(log, ct);
        await _repo.SaveChangesAsync(ct);
    }
}
