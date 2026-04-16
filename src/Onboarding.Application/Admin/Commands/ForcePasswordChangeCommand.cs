using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;

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
    private readonly IAuditService _auditService;

    public ForcePasswordChangeCommandHandler(
        IKeycloakUserService keycloakUserService,
        IAuditService auditService)
    {
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
    }

    public async Task<Unit> HandleAsync(ForcePasswordChangeCommand command, CancellationToken ct = default)
    {
        // Update password in Keycloak (temporary = false since this is the permanent password)
        await _keycloakUserService.UpdateUserPasswordAsync(command.KeycloakUserId, command.NewPassword, ct);

        // Remove UPDATE_PASSWORD required action
        await _keycloakUserService.RemoveUpdatePasswordRequiredActionAsync(command.KeycloakUserId, ct);

        // Audit log via IAuditService
        await _auditService.RecordAsync(
            actorSub: command.KeycloakUserId,
            actorEmail: command.AdminEmail,
            action: ActionType.AdminPasswordChanged,
            details: "{\"action\": \"password_changed\"}",
            ipAddress: command.IpAddress,
            ct: ct);

        return Unit.Value;
    }
}
