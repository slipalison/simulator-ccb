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
    private readonly IAuditService _auditService;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly ILogger<BlockUserCommandHandler> _logger;

    public BlockUserCommandHandler(
        IAdminRepository adminRepository,
        IAuditService auditService,
        IKeycloakUserService keycloakUserService,
        ILogger<BlockUserCommandHandler> logger)
    {
        _adminRepository = adminRepository;
        _auditService = auditService;
        _keycloakUserService = keycloakUserService;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(BlockUserCommand command, CancellationToken ct = default)
    {
        var company = await _adminRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        // Get Keycloak user
        var kcUser = await _keycloakUserService.GetUserByEmailAsync("client", company.Email.Value, ct)
            ?? throw new InvalidOperationException("Keycloak user not found.");

        // Block via Keycloak service abstraction
        await _keycloakUserService.BlockUserAsync("client", kcUser.Id, ct);

        // Audit log
        await _auditService.RecordAsync(
            actorSub: command.AdminSub,
            actorEmail: command.AdminEmail,
            action: ActionType.UserBlocked,
            targetUserId: command.UserId,
            targetUserName: company.Email.Value,
            ct: ct);

        return Unit.Value;
    }
}
