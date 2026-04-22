using FluentValidation;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using System.Security.Cryptography;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Result returned after a successful admin password reset.
/// The temporary password is visible ONCE — never logged or stored.
/// </summary>
public sealed record ResetAdministratorPasswordResult(string TemporaryPassword);

/// <summary>
/// Command to reset an administrator's password.
/// SEC-01: actor cannot target themselves.
/// SEC-03: generates a 16-character cryptographically secure password
///         with ambiguous characters removed (O, 0, l, 1, I).
/// AUD-05: audit record is written without the password value.
/// </summary>
public sealed record ResetAdministratorPasswordCommand(
    string TargetKeycloakId,
    string TargetUserName,
    string ActorSub,
    string ActorEmail,
    string? IpAddress);

public sealed class ResetAdministratorPasswordCommandValidator : AbstractValidator<ResetAdministratorPasswordCommand>
{
    public ResetAdministratorPasswordCommandValidator()
    {
        RuleFor(x => x.TargetKeycloakId).NotEmpty();
        RuleFor(x => x.ActorSub).NotEmpty();
        RuleFor(x => x.ActorEmail).NotEmpty();

        // SEC-01: admin cannot reset their own password via this endpoint
        RuleFor(x => x.TargetKeycloakId)
            .Must((cmd, targetId) => targetId != cmd.ActorSub)
            .WithMessage("An administrator cannot reset their own password via this endpoint.");
    }
}

public sealed class ResetAdministratorPasswordCommandHandler
    : ICommandHandler<ResetAdministratorPasswordCommand, ResetAdministratorPasswordResult>
{
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;

    public ResetAdministratorPasswordCommandHandler(
        IKeycloakUserService keycloakUserService,
        IAuditService auditService)
    {
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
    }

    public async Task<ResetAdministratorPasswordResult> HandleAsync(
        ResetAdministratorPasswordCommand command, CancellationToken ct = default)
    {
        // SEC-01 guard (belt-and-suspenders beyond validator)
        if (command.TargetKeycloakId == command.ActorSub)
            throw new InvalidOperationException("An administrator cannot reset their own password via this endpoint.");

        // Ensure target exists
        var target = await _keycloakUserService.GetUserByIdAsync("backoffice", command.TargetKeycloakId, ct)
            ?? throw new KeyNotFoundException($"Administrator '{command.TargetKeycloakId}' not found.");

        // SEC-03: generate cryptographically secure 16-char password without ambiguous chars
        var temporaryPassword = GenerateTemporaryPassword();

        // Set the temporary password in Keycloak (temporary=true → Keycloak clears it on next login)
        var passwordPayload = new { type = "password", value = temporaryPassword, temporary = true };
        // We call UpdateUserPasswordAsync with temporary flag via the existing Keycloak infra,
        // but that method sets temporary=false. To set temporary=true we use SetTemporaryPasswordFlagAsync
        // pattern: first set password, then ensure UPDATE_PASSWORD action is present.
        await _keycloakUserService.UpdateUserPasswordAsync("backoffice", command.TargetKeycloakId, temporaryPassword, ct);
        await _keycloakUserService.SetTemporaryPasswordFlagAsync("backoffice", command.TargetKeycloakId, ct);

        // AUD-05: actor + target only — password never recorded
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.AdminPasswordReset,
            targetUserId: Guid.TryParse(command.TargetKeycloakId, out var g) ? g : null,
            targetUserName: command.TargetUserName,
            details: null,   // intentionally empty — password must not be logged
            ipAddress: command.IpAddress,
            ct: ct);

        return new ResetAdministratorPasswordResult(temporaryPassword);
    }

    /// <summary>
    /// SEC-03: Generates a 16-character cryptographically secure password.
    /// Removes visually ambiguous characters: O, 0, l, 1, I.
    /// Guarantees at least 1 char from each category (upper, lower, digit, special).
    /// </summary>
    private static string GenerateTemporaryPassword()
    {
        // Pools with ambiguous chars removed
        const string upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";      // no O, I
        const string lower   = "abcdefghjkmnpqrstuvwxyz";       // no l, o (mapped to 0)
        const string digits  = "23456789";                       // no 0, 1
        const string special = "!@#$%^&*";
        const string all     = upper + lower + digits + special;

        const int length = 16;
        var chars = new char[length];

        // Guarantee one of each category
        chars[0] = GetRandomChar(upper);
        chars[1] = GetRandomChar(lower);
        chars[2] = GetRandomChar(digits);
        chars[3] = GetRandomChar(special);

        // Fill rest from full pool
        for (var i = 4; i < length; i++)
            chars[i] = GetRandomChar(all);

        // Fisher-Yates shuffle with crypto randomness
        for (var i = length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(0, i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static char GetRandomChar(string pool)
        => pool[RandomNumberGenerator.GetInt32(0, pool.Length)];
}
