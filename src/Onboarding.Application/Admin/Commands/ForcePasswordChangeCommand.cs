using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command for an admin to change their own password (forced on first login with temporary password).
/// </summary>
public sealed record ForcePasswordChangeCommand(
    string KeycloakUserId,
    string AdminEmail,
    string NewPassword,
    string? IpAddress);

public sealed class ForcePasswordChangeCommandHandler : ICommandHandler<ForcePasswordChangeCommand, Unit>
{
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAdminAuditLogRepository _adminAuditLogRepository;

    public ForcePasswordChangeCommandHandler(
        IKeycloakUserService keycloakUserService,
        IAdminAuditLogRepository adminAuditLogRepository)
    {
        _keycloakUserService = keycloakUserService;
        _adminAuditLogRepository = adminAuditLogRepository;
    }

    public async Task<Unit> HandleAsync(ForcePasswordChangeCommand command, CancellationToken ct = default)
    {
        // Update password in Keycloak (temporary = false since this is the permanent password)
        await _keycloakUserService.UpdateUserPasswordAsync(command.KeycloakUserId, command.NewPassword, ct);

        // Remove UPDATE_PASSWORD required action
        await _keycloakUserService.RemoveUpdatePasswordRequiredActionAsync(command.KeycloakUserId, ct);

        // Create audit log entry
        var auditLog = AdminAuditLog.Create(
            Guid.Parse(command.KeycloakUserId),
            command.AdminEmail,
            ActionType.AdminPasswordChanged,
            details: "{\"action\": \"password_changed\"}",
            ipAddress: command.IpAddress);

        await _adminAuditLogRepository.AddAsync(auditLog, ct);
        await _adminAuditLogRepository.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
