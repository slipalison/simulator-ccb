using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command: block user account (ADMIN-04).
/// </summary>
public sealed record BlockUserCommand(
    Guid UserId,
    string AdminSub,
    string AdminEmail);

public sealed class BlockUserCommandHandler : ICommandHandler<BlockUserCommand, Unit>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly ILogger<BlockUserCommandHandler> _logger;

    public BlockUserCommandHandler(
        IAdminRepository adminRepository,
        IAuditLogRepository auditLogRepository,
        IKeycloakUserService keycloakUserService,
        ILogger<BlockUserCommandHandler> logger)
    {
        _adminRepository = adminRepository;
        _auditLogRepository = auditLogRepository;
        _keycloakUserService = keycloakUserService;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(BlockUserCommand command, CancellationToken ct = default)
    {
        var client = await _adminRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        // Get Keycloak user
        var kcUser = await _keycloakUserService.GetUserByEmailAsync(client.Email.Value, ct)
            ?? throw new InvalidOperationException("Keycloak user not found.");

        // Block via Keycloak service abstraction
        await _keycloakUserService.BlockUserAsync(kcUser.Id, ct);

        // Audit log
        var auditLog = AuditLog.Create(
            command.AdminSub,
            command.AdminEmail,
            AuditActions.UserBlocked,
            command.UserId,
            client.Email.Value,
            "{ \"Enabled\": true }",
            "{ \"Enabled\": false }",
            null,
            null
        );

        await _auditLogRepository.AddAsync(auditLog, ct);
        await _auditLogRepository.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
