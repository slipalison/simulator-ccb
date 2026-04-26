using Microsoft.Extensions.Logging;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Handler: blocks or unblocks an employee in Keycloak (MGMT-03, T-38-08).
/// Block also revokes all sessions. Company isolation enforced.
/// </summary>
public sealed class ToggleEmployeeStatusCommandHandler : ICommandHandler<ToggleEmployeeStatusCommand, Unit>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ToggleEmployeeStatusCommandHandler> _logger;

    public ToggleEmployeeStatusCommandHandler(
        IEmployeeRepository employeeRepository,
        IKeycloakUserService keycloakUserService,
        IAuditService auditService,
        ILogger<ToggleEmployeeStatusCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(ToggleEmployeeStatusCommand command, CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, ct)
            ?? throw new KeyNotFoundException($"Employee with ID {command.EmployeeId} not found.");

        // Company isolation (T-38-08)
        if (employee.CompanyId != command.CompanyId)
            throw new InvalidOperationException("Employee does not belong to the specified company.");

        var action = command.Activate ? ActionType.EmployeeUnblocked : ActionType.EmployeeBlocked;
        var originalName = employee.Nome;

        if (command.Activate)
        {
            // Unblock in Keycloak
            await _keycloakUserService.UnblockUserAsync("client", employee.KeycloakUserId!, ct);
        }
        else
        {
            // Block in Keycloak and revoke sessions (T-38-08)
            await _keycloakUserService.BlockUserAsync("client", employee.KeycloakUserId!, ct);
            await _keycloakUserService.LogoutAllSessionsAsync("client", employee.KeycloakUserId!, ct);
        }

        // Audit (MGMT-04, T-38-10)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: action,
            targetUserId: employee.Id,
            targetUserName: originalName,
            details: command.Activate ? "Employee unblocked" : "Employee blocked",
            ipAddress: command.IpAddress,
            ct: ct);

        return Unit.Value;
    }
}