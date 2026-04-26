using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Handler: block an employee account (admin, MGMT-03, T-38-14).
/// Blocks in Keycloak, revokes all sessions, and records audit log.
/// </summary>
public sealed record BlockEmployeeCommand(
    Guid EmployeeId,
    string ActorSub);

public sealed class BlockEmployeeCommandHandler : ICommandHandler<BlockEmployeeCommand, Unit>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;

    public BlockEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IKeycloakUserService keycloakUserService,
        IAuditService auditService)
    {
        _employeeRepository = employeeRepository;
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
    }

    public async Task<Unit> HandleAsync(BlockEmployeeCommand command, CancellationToken ct = default)
    {
        // Admin bypasses HasQueryFilter — can block employee from any company
        var employee = await _employeeRepository.GetByIdIgnoreFilterAsync(command.EmployeeId, ct)
            ?? throw new KeyNotFoundException($"Employee with ID {command.EmployeeId} not found.");

        // Block in Keycloak (T-38-14: admin endpoint)
        await _keycloakUserService.BlockUserAsync("client", employee.KeycloakUserId!, ct);

        // Revoke all active sessions (T-38-08: defense in depth)
        await _keycloakUserService.LogoutAllSessionsAsync("client", employee.KeycloakUserId!, ct);

        // Audit (T-38-14: repudiation mitigation)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: "",
            action: ActionType.EmployeeBlocked,
            targetUserId: employee.Id,
            targetUserName: employee.Nome,
            details: "Employee blocked by admin",
            ct: ct);

        return Unit.Value;
    }
}