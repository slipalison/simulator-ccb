using Onboarding.Domain.Aggregates.Audit;

namespace Onboarding.Application.Common;

/// <summary>
/// Abstração unificada para gravação de audit log administrativo.
/// Toda ação administrativa passa por aqui — nenhum handler deve injetar IAdminAuditLogRepository diretamente.
/// </summary>
public interface IAuditService
{
    Task RecordAsync(
        string actorSub,
        string actorEmail,
        ActionType action,
        Guid? targetUserId = null,
        string? targetUserName = null,
        string? details = null,
        string? ipAddress = null,
        CancellationToken ct = default);
}
