using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Handler: unblock an employee account (admin, MGMT-03, T-38-14).
/// Unblocks in Keycloak and records audit log.
/// </summary>
public sealed record UnblockEmployeeCommand(
    Guid EmployeeId,
    string ActorSub);

public sealed class UnblockEmployeeCommandHandler : ICommandHandler<UnblockEmployeeCommand, Unit>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;

    public UnblockEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IKeycloakUserService keycloakUserService,
        IAuditService auditService)
    {
        _employeeRepository = employeeRepository;
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
    }

    public async Task<Unit> HandleAsync(UnblockEmployeeCommand command, CancellationToken ct = default)
    {
        // Admin bypasses HasQueryFilter — can unblock employee from any company
        var employee = await _employeeRepository.GetByIdIgnoreFilterAsync(command.EmployeeId, ct)
            ?? throw new KeyNotFoundException($"Employee with ID {command.EmployeeId} not found.");

        // Unblock in Keycloak
        await _keycloakUserService.UnblockUserAsync("client", employee.KeycloakUserId!, ct);

        // Audit (T-38-14: repudiation mitigation)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: "",
            action: ActionType.EmployeeUnblocked,
            targetUserId: employee.Id,
            targetUserName: employee.Nome,
            details: "Employee unblocked by admin",
            ct: ct);

        return Unit.Value;
    }
}