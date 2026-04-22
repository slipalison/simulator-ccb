using FluentValidation;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command to enable or disable an administrator account.
/// SEC-01: actor cannot target themselves.
/// SEC-05: disabling the last active admin is blocked.
/// AUD-06: toggle action recorded with optional reason.
/// </summary>
public sealed record ToggleAdministratorStatusCommand(
    string TargetKeycloakId,
    string TargetUserName,
    bool Activate,          // true = reactivate; false = disable
    string? Reason,
    string ActorSub,
    string ActorEmail,
    string? IpAddress);

public sealed class ToggleAdministratorStatusCommandValidator : AbstractValidator<ToggleAdministratorStatusCommand>
{
    public ToggleAdministratorStatusCommandValidator()
    {
        RuleFor(x => x.TargetKeycloakId).NotEmpty();
        RuleFor(x => x.ActorSub).NotEmpty();
        RuleFor(x => x.ActorEmail).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);

        // SEC-01: admin cannot disable/reactivate themselves
        RuleFor(x => x.TargetKeycloakId)
            .Must((cmd, targetId) => targetId != cmd.ActorSub)
            .WithMessage("An administrator cannot change their own account status.");
    }
}

public sealed class ToggleAdministratorStatusCommandHandler : ICommandHandler<ToggleAdministratorStatusCommand, Unit>
{
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;

    public ToggleAdministratorStatusCommandHandler(
        IKeycloakUserService keycloakUserService,
        IAuditService auditService)
    {
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
    }

    public async Task<Unit> HandleAsync(ToggleAdministratorStatusCommand command, CancellationToken ct = default)
    {
        // SEC-01 guard
        if (command.TargetKeycloakId == command.ActorSub)
            throw new InvalidOperationException("An administrator cannot change their own account status.");

        if (!command.Activate)
        {
            // SEC-05: refuse to disable the last active administrator
            var allAdmins = await _keycloakUserService.GetUsersByRoleAsync("backoffice", "admin", ct);
            var activeAdmins = allAdmins.Where(a => a.IsEnabled).ToList();

            if (activeAdmins.Count <= 1 && activeAdmins.Any(a => a.Id == command.TargetKeycloakId))
                throw new InvalidOperationException(
                    "Cannot disable the last active administrator. At least one active administrator must remain.");

            // Disable in Keycloak then revoke all active sessions immediately
            await _keycloakUserService.BlockUserAsync("backoffice", command.TargetKeycloakId, ct);
            await _keycloakUserService.LogoutAllSessionsAsync("backoffice", command.TargetKeycloakId, ct);
        }
        else
        {
            await _keycloakUserService.UnblockUserAsync("backoffice", command.TargetKeycloakId, ct);
        }

        // AUD-06: disable/reactivate with optional reason
        var action = command.Activate ? ActionType.AdminReactivated : ActionType.AdminDisabled;
        var details = command.Reason is not null
            ? System.Text.Json.JsonSerializer.Serialize(new { reason = command.Reason })
            : null;

        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: action,
            targetUserId: Guid.TryParse(command.TargetKeycloakId, out var g) ? g : null,
            targetUserName: command.TargetUserName,
            details: details,
            ipAddress: command.IpAddress,
            ct: ct);

        return Unit.Value;
    }
}
