using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Companies.Commands;

public sealed class UpdateAccessGroupCommandHandler
    : ICommandHandler<UpdateAccessGroupCommand, AccessGroupDto>
{
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateAccessGroupCommandHandler> _logger;

    public UpdateAccessGroupCommandHandler(
        IAccessGroupRepository accessGroupRepository,
        IKeycloakUserService keycloakUserService,
        IAuditService auditService,
        ILogger<UpdateAccessGroupCommandHandler> logger)
    {
        _accessGroupRepository = accessGroupRepository;
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<AccessGroupDto> HandleAsync(UpdateAccessGroupCommand command, CancellationToken ct = default)
    {
        var group = await _accessGroupRepository.GetByIdAsync(command.AccessGroupId, ct)
            ?? throw new KeyNotFoundException($"Access group with ID {command.AccessGroupId} not found.");

        if (group.CompanyId != command.CompanyId)
            throw new InvalidOperationException("Access group does not belong to the specified company.");

        if (group.IsDefault)
            throw new BadRequestException($"Default group '{group.Name}' cannot be modified.");

        var previousName = group.Name;

        if (command.Name is not null && command.Name != group.Name)
        {
            var existingName = await _accessGroupRepository.GetByCompanyAndNameAsync(command.CompanyId, command.Name, ct);
            if (existingName is not null)
                throw new BadRequestException($"An access group with name '{command.Name}' already exists.");

            group.UpdateName(command.Name);
        }

        if (command.Permissions is not null)
        {
            foreach (var perm in command.Permissions)
            {
                if (!Permissions.All.Contains(perm))
                    throw new BadRequestException($"Invalid permission: '{perm}'.");
            }

            group.UpdatePermissions(command.Permissions);
        }

        await _accessGroupRepository.SaveAsync(group, ct);

        if (command.Name is not null && command.Name != previousName)
        {
            try
            {
                var oldKeycloakGroupId = await _keycloakUserService.GetGroupByNameAsync("client", previousName, ct);
                if (oldKeycloakGroupId is not null)
                {
                    await _keycloakUserService.DeleteGroupAsync("client", oldKeycloakGroupId, ct);
                }

                await _keycloakUserService.CreateGroupAsync("client", group.Name, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Keycloak group rename from '{OldName}' to '{NewName}' failed. DB is source of truth.", previousName, group.Name);
            }
        }

        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.AccessGroupUpdated,
            targetUserId: group.Id,
            targetUserName: group.Name,
            details: $"CompanyId={command.CompanyId}",
            ipAddress: command.IpAddress,
            ct: ct);

        return new AccessGroupDto(group.Id, group.Name, group.Permissions, group.IsDefault);
    }
}