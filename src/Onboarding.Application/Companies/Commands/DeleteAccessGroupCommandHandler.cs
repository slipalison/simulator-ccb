using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Companies.Commands;

public sealed class DeleteAccessGroupCommandHandler
    : ICommandHandler<DeleteAccessGroupCommand, Unit>
{
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ILogger<DeleteAccessGroupCommandHandler> _logger;

    public DeleteAccessGroupCommandHandler(
        IAccessGroupRepository accessGroupRepository,
        IEmployeeRepository employeeRepository,
        IKeycloakUserService keycloakUserService,
        IAuditService auditService,
        ILogger<DeleteAccessGroupCommandHandler> logger)
    {
        _accessGroupRepository = accessGroupRepository;
        _employeeRepository = employeeRepository;
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(DeleteAccessGroupCommand command, CancellationToken ct = default)
    {
        var group = await _accessGroupRepository.GetByIdAsync(command.AccessGroupId, ct)
            ?? throw new KeyNotFoundException($"Access group with ID {command.AccessGroupId} not found.");

        if (group.CompanyId != command.CompanyId)
            throw new InvalidOperationException("Access group does not belong to the specified company.");

        if (group.IsDefault)
            throw new BadRequestException($"Default group '{group.Name}' cannot be deleted.");

        var hasEmployees = await _employeeRepository.ExistsByAccessGroupIdAsync(command.AccessGroupId, ct);
        if (hasEmployees)
            throw new BadRequestException($"Cannot delete access group '{group.Name}' because it has employees assigned to it. Move or reassign employees first.");

        await _accessGroupRepository.DeleteAsync(command.AccessGroupId, ct);

        try
        {
            var keycloakGroupId = await _keycloakUserService.GetGroupByNameAsync("client", group.Name, ct);
            if (keycloakGroupId is not null)
            {
                await _keycloakUserService.DeleteGroupAsync("client", keycloakGroupId, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete Keycloak group '{GroupName}'. DB is source of truth.", group.Name);
        }

        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.AccessGroupDeleted,
            targetUserId: group.Id,
            targetUserName: group.Name,
            details: $"CompanyId={command.CompanyId}",
            ipAddress: command.IpAddress,
            ct: ct);

        return Unit.Value;
    }
}