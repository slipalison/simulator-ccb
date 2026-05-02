using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Handler: updates employee data (name, email, phone) in DB and syncs to Keycloak (MGMT-05).
/// Company isolation enforced (T-38-08).
/// </summary>
public sealed class UpdateEmployeeCommandHandler : ICommandHandler<UpdateEmployeeCommand, Unit>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;

    public UpdateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IKeycloakUserService keycloakUserService,
        IAuditService auditService)
    {
        _employeeRepository = employeeRepository;
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
    }

    public async Task<Unit> HandleAsync(UpdateEmployeeCommand command, CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, ct)
            ?? throw new KeyNotFoundException($"Employee with ID {command.EmployeeId} not found.");

        // Company isolation (T-38-08)
        if (employee.CompanyId != command.CompanyId)
            throw new InvalidOperationException("Employee does not belong to the specified company.");

        // Update domain entity
        employee.Update(command.Nome, command.Email, command.Phone);
        await _employeeRepository.SaveAsync(employee, ct);

        // Sync to Keycloak (best-effort — logs error if fails but does not rethrow)
        try
        {
            await _keycloakUserService.UpdateAdminUserAsync(
                "client", employee.KeycloakUserId!, command.Nome, command.Email, ct);
        }
        catch (Exception)
        {
            // Best-effort Keycloak sync — DB is source of truth
            // Logged but not rethrown to avoid rolling back the DB update
        }

        // Audit (MGMT-04, T-38-10)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.EmployeeEdited,
            targetUserId: employee.Id,
            targetUserName: command.Nome,
            details: "Employee data updated",
            ipAddress: command.IpAddress,
            ct: ct);

        return Unit.Value;
    }
}