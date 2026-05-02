using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Handler: changes employee's access group — verifies new group belongs to same company (T-38-11).
/// Syncs group membership to Keycloak (add to new + remove from old, D-15).
/// Keycloak sync is best-effort: failures logged but not rethrown (eventual consistency).
/// </summary>
public sealed class ChangeEmployeeAccessGroupCommandHandler : ICommandHandler<ChangeEmployeeAccessGroupCommand, Unit>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ChangeEmployeeAccessGroupCommandHandler> _logger;

    public ChangeEmployeeAccessGroupCommandHandler(
        IEmployeeRepository employeeRepository,
        IAccessGroupRepository accessGroupRepository,
        IKeycloakUserService keycloakUserService,
        IAuditService auditService,
        ILogger<ChangeEmployeeAccessGroupCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _accessGroupRepository = accessGroupRepository;
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(ChangeEmployeeAccessGroupCommand command, CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, ct)
            ?? throw new KeyNotFoundException($"Employee with ID {command.EmployeeId} not found.");

        // Company isolation (T-38-08)
        if (employee.CompanyId != command.CompanyId)
            throw new InvalidOperationException("Employee does not belong to the specified company.");

        // Verify new access group belongs to same company (T-38-11)
        var newGroup = await _accessGroupRepository.GetByIdAsync(command.NewAccessGroupId, ct)
            ?? throw new KeyNotFoundException($"Access group with ID {command.NewAccessGroupId} not found.");

        if (newGroup.CompanyId != command.CompanyId)
            throw new InvalidOperationException("Access group does not belong to the specified company.");

        // Change access group
        var previousAccessGroupId = employee.AccessGroupId;
        employee.SetAccessGroup(command.NewAccessGroupId);
        await _employeeRepository.SaveAsync(employee, ct);

        // Sync Keycloak group membership (D-15): add to new group, remove from old group
        // Best-effort: DB is source of truth; Keycloak sync failure is logged but not rethrown
        if (!string.IsNullOrEmpty(employee.KeycloakUserId))
        {
            try
            {
                // Add to new group
                var newKeycloakGroupId = await _keycloakUserService.GetGroupByNameAsync("client", newGroup.Name, ct);
                if (newKeycloakGroupId is not null)
                {
                    await _keycloakUserService.AddUserToGroupAsync("client", employee.KeycloakUserId, newKeycloakGroupId, ct);
                }
                else
                {
                    _logger.LogWarning("Keycloak group '{GroupName}' not found. Employee {EmployeeId} added to DB group but not synced to Keycloak.", newGroup.Name, employee.Id);
                }

                // Remove from old group
                var previousGroup = await _accessGroupRepository.GetByIdAsync(previousAccessGroupId, ct);
                if (previousGroup is not null)
                {
                    var oldKeycloakGroupId = await _keycloakUserService.GetGroupByNameAsync("client", previousGroup.Name, ct);
                    if (oldKeycloakGroupId is not null)
                    {
                        await _keycloakUserService.RemoveUserFromGroupAsync("client", employee.KeycloakUserId, oldKeycloakGroupId, ct);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync Keycloak group membership for employee {EmployeeId}. DB update is source of truth; Keycloak may need manual sync.", employee.Id);
            }
        }

        // Audit (T-38-10)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.AccessGroupChanged,
            targetUserId: employee.Id,
            targetUserName: employee.Nome,
            details: $"Access group changed to {command.NewAccessGroupId}",
            ipAddress: command.IpAddress,
            ct: ct);

        return Unit.Value;
    }
}