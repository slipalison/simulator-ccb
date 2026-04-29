using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.Aggregates.CompanyAggregate;

namespace Onboarding.Application.Companies.Commands;

public sealed class CreateAccessGroupCommandHandler
    : ICommandHandler<CreateAccessGroupCommand, AccessGroupDto>
{
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ILogger<CreateAccessGroupCommandHandler> _logger;

    public CreateAccessGroupCommandHandler(
        IAccessGroupRepository accessGroupRepository,
        ICompanyRepository companyRepository,
        IKeycloakUserService keycloakUserService,
        IAuditService auditService,
        ILogger<CreateAccessGroupCommandHandler> logger)
    {
        _accessGroupRepository = accessGroupRepository;
        _companyRepository = companyRepository;
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<AccessGroupDto> HandleAsync(CreateAccessGroupCommand command, CancellationToken ct = default)
    {
        var company = await _companyRepository.GetByIdAsync(command.CompanyId, ct)
            ?? throw new KeyNotFoundException($"Company with ID {command.CompanyId} not found.");

        var existingGroup = await _accessGroupRepository.GetByCompanyAndNameAsync(command.CompanyId, command.Name, ct);
        if (existingGroup is not null)
            throw new BadRequestException($"An access group with name '{command.Name}' already exists.");

        foreach (var perm in command.Permissions)
        {
            if (!Permissions.All.Contains(perm))
                throw new BadRequestException($"Invalid permission: '{perm}'.");
        }

        var accessGroup = AccessGroup.Create(command.CompanyId, command.Name, command.Permissions);
        await _accessGroupRepository.AddAsync(accessGroup, ct);

        try
        {
            await _keycloakUserService.CreateGroupAsync("client", accessGroup.Name, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Keycloak group creation failed for '{GroupName}'. DB is source of truth; group may need manual sync.", accessGroup.Name);
        }

        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.AccessGroupCreated,
            targetUserId: accessGroup.Id,
            targetUserName: accessGroup.Name,
            details: $"CompanyId={command.CompanyId}, Permissions={string.Join(",", command.Permissions)}",
            ipAddress: command.IpAddress,
            ct: ct);

        return new AccessGroupDto(accessGroup.Id, accessGroup.Name, accessGroup.Permissions);
    }
}