using System.Text.Json;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command: LGPD-compliant user deletion (ADMIN-05).
/// </summary>
public sealed record DeleteUserCommand(
    Guid UserId,
    string ConfirmEmail,
    string AdminSub,
    string AdminEmail);

public sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, Unit>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IAuditService _auditService;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(
        IAdminRepository adminRepository,
        IAuditService auditService,
        IKeycloakUserService keycloakUserService,
        ILogger<DeleteUserCommandHandler> logger)
    {
        _adminRepository = adminRepository;
        _auditService = auditService;
        _keycloakUserService = keycloakUserService;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(DeleteUserCommand command, CancellationToken ct = default)
    {
        var client = await _adminRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        // Validate confirm email
        if (!command.ConfirmEmail.Equals(client.Email.Value, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Email confirmation does not match.");

        // Check already deleted
        if (client.IsDeleted)
            throw new InvalidOperationException("User is already deleted.");

        // Capture original email BEFORE anonymization (critical!)
        var originalEmail = client.Email.Value;

        // Capture snapshot BEFORE anonymization
        var before = JsonSerializer.Serialize(new
        {
            client.Name,
            Email = client.Email.Value,
            Phone = client.Phone.Value,
            client.Cpf,
            client.Cnpj,
            client.RazaoSocial,
            Type = client.Type.ToString()
        });

        // Anonymize PII in database first
        client.Anonymize();
        await _adminRepository.UpdateAsync(client, ct);
        await _adminRepository.SaveChangesAsync(ct);

        // Delete from Keycloak — use ORIGINAL email
        try
        {
            await _keycloakUserService.DeleteUserByEmailAsync(originalEmail, ct);
        }
        catch (Exception ex)
        {
            // CRITICAL: PII is already scrubbed. Do NOT rollback.
            _logger.LogCritical(ex, "Keycloak deletion failed for user {Email} after DB anonymization", originalEmail);
        }

        // Capture snapshot AFTER anonymization
        var after = JsonSerializer.Serialize(new
        {
            client.Name,
            Email = client.Email.Value,
            Phone = client.Phone.Value,
            Cpf = (string?)null,
            Cnpj = (string?)null,
            RazaoSocial = (string?)null,
            Type = client.Type.ToString(),
            client.DeletedAt
        });

        // Audit log
        await _auditService.RecordAsync(
            actorSub: command.AdminSub,
            actorEmail: command.AdminEmail,
            action: ActionType.UserDeleted,
            targetUserId: command.UserId,
            targetUserName: originalEmail,
            details: JsonSerializer.Serialize(new { Before = before, After = after }),
            ct: ct);

        return Unit.Value;
    }
}
