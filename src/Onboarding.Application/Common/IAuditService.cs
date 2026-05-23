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
        string? entityType = null,
        Guid? entityId = null,
        CancellationToken ct = default);
}

/// <summary>
/// DTO para representacao de um administrador na listagem GET /api/admin/administrators.
/// HasTemporaryPassword=true quando UPDATE_PASSWORD esta nos requiredActions do Keycloak.
/// </summary>
public sealed record AdminUserDto(
    string Id,
    string Email,
    string FullName,
    bool IsEnabled,
    bool HasTemporaryPassword);
