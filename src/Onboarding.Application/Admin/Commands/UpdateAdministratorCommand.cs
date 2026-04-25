using FluentValidation;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using System.Text.Json;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command to edit an administrator's name and/or email.
/// SEC-01: actor cannot target themselves.
/// SEC-04: email uniqueness is checked before persisting.
/// AUD-04: old/new values recorded in audit log.
/// </summary>
public sealed record UpdateAdministratorCommand(
    string TargetKeycloakId,
    string FullName,
    string Email,
    string ActorSub,
    string ActorEmail,
    string? IpAddress);

public sealed class UpdateAdministratorCommandValidator : AbstractValidator<UpdateAdministratorCommand>
{
    public UpdateAdministratorCommandValidator()
    {
        RuleFor(x => x.TargetKeycloakId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.ActorSub).NotEmpty();
        RuleFor(x => x.ActorEmail).NotEmpty();

        // SEC-01: admin cannot edit themselves
        RuleFor(x => x.TargetKeycloakId)
            .Must((cmd, targetId) => targetId != cmd.ActorSub)
            .WithMessage("An administrator cannot edit their own account.");
    }
}

public sealed class UpdateAdministratorCommandHandler : ICommandHandler<UpdateAdministratorCommand, Unit>
{
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;

    public UpdateAdministratorCommandHandler(IKeycloakUserService keycloakUserService, IAuditService auditService)
    {
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
    }

    public async Task<Unit> HandleAsync(UpdateAdministratorCommand command, CancellationToken ct = default)
    {
        // SEC-01 guard (double-check beyond validator)
        if (command.TargetKeycloakId == command.ActorSub)
            throw new InvalidOperationException("An administrator cannot edit their own account.");

        // Fetch current state for audit diff (AUD-04: old+new values)
        var current = await _keycloakUserService.GetUserByIdAsync("backoffice", command.TargetKeycloakId, ct)
            ?? throw new KeyNotFoundException($"Administrator '{command.TargetKeycloakId}' not found.");

        // SEC-04: uniqueness checked inside UpdateAdminUserAsync — throws ArgumentException on conflict
        await _keycloakUserService.UpdateAdminUserAsync(
            "backoffice",
            command.TargetKeycloakId,
            command.FullName,
            command.Email,
            ct);

        // AUD-04: record old→new snapshot (WR-03: include old fullName for complete change record)
        var details = JsonSerializer.Serialize(new
        {
            old = new { fullName = current.FullName, email = current.Email },
            @new = new { fullName = command.FullName, email = command.Email }
        });

        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.AdminEdited,
            targetUserId: Guid.TryParse(command.TargetKeycloakId, out var g) ? g : null,
            targetUserName: command.FullName,
            details: details,
            ipAddress: command.IpAddress,
            ct: ct);

        return Unit.Value;
    }
}
