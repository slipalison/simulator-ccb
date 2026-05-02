using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Handler: LGPD delete an employee — anonymizes PII in DB + deletes Keycloak user (MGMT-05, T-38-09).
/// Idempotent on Anonymize(). Company isolation enforced. Captures email BEFORE Anonymize for Keycloak deletion.
/// </summary>
public sealed class DeleteEmployeeCommandHandler : ICommandHandler<DeleteEmployeeCommand, Unit>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;

    public DeleteEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IKeycloakUserService keycloakUserService,
        IAuditService auditService)
    {
        _employeeRepository = employeeRepository;
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
    }

    public async Task<Unit> HandleAsync(DeleteEmployeeCommand command, CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, ct)
            ?? throw new KeyNotFoundException($"Employee with ID {command.EmployeeId} not found.");

        // Company isolation (T-38-08)
        if (employee.CompanyId != command.CompanyId)
            throw new InvalidOperationException("Employee does not belong to the specified company.");

        // If already anonymized, still proceed with Keycloak deletion and audit (idempotent)
        // But skip second Anonymize() call — it's a no-op internally anyway
        if (employee.IsDeleted)
        {
            // Still audit the attempt for compliance
            await _auditService.RecordAsync(
                actorSub: command.ActorSub,
                actorEmail: command.ActorEmail,
                action: ActionType.EmployeeDeleted,
                targetUserId: employee.Id,
                targetUserName: employee.Nome,
                details: "LGPD deletion requested on already-deleted employee (idempotent)",
                ipAddress: command.IpAddress,
                ct: ct);

            // Try to delete Keycloak user if KeycloakUserId exists (may have been deleted already)
            if (!string.IsNullOrEmpty(employee.KeycloakUserId))
            {
                try
                {
                    await _keycloakUserService.DeleteUserByEmailAsync("client", employee.Email.Value, ct);
                }
                catch
                {
                    // Keycloak deletion best-effort on already-deleted — user may not exist
                }
            }

            return Unit.Value;
        }

        // Capture email BEFORE anonymizing (T-38-09: need original email for Keycloak deletion)
        var originalEmail = employee.Email.Value;

        // Anonymize PII data (LGPD) — sets Nome="Usuário Excluído", Email=deleted-{Id}@internal.local, Cpf=null
        employee.Anonymize();
        await _employeeRepository.SaveAsync(employee, ct);

        // Delete from Keycloak using ORIGINAL email (before anonymize changed it)
        await _keycloakUserService.DeleteUserByEmailAsync("client", originalEmail, ct);

        // Audit (MGMT-04, T-38-10)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.EmployeeDeleted,
            targetUserId: employee.Id,
            targetUserName: "Usuário Excluído",
            details: "LGPD deletion — data anonymized, Keycloak user deleted",
            ipAddress: command.IpAddress,
            ct: ct);

        return Unit.Value;
    }
}