using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Handler: LGPD-compliant employee deletion (admin, MGMT-05, T-38-14).
/// Anonymizes PII in DB + deletes Keycloak user. Idempotent on re-delete.
/// Admin bypasses company isolation — can delete any employee from any company.
/// </summary>
public sealed record DeleteEmployeeCommand(
    Guid EmployeeId,
    string ActorSub);

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
        // Admin bypasses HasQueryFilter — can delete employee from any company
        var employee = await _employeeRepository.GetByIdIgnoreFilterAsync(command.EmployeeId, ct)
            ?? throw new KeyNotFoundException($"Employee with ID {command.EmployeeId} not found.");

        // If already anonymized, still proceed with Keycloak deletion and audit (idempotent)
        if (employee.IsDeleted)
        {
            // Still audit the attempt for compliance
            await _auditService.RecordAsync(
                actorSub: command.ActorSub,
                actorEmail: "",
                action: ActionType.EmployeeDeleted,
                targetUserId: employee.Id,
                targetUserName: employee.Nome,
                details: "LGPD deletion requested on already-deleted employee (idempotent)",
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

        // Audit (MGMT-05, T-38-14: repudiation mitigation)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: "",
            action: ActionType.EmployeeDeleted,
            targetUserId: employee.Id,
            targetUserName: "Usuário Excluído",
            details: "LGPD deletion — data anonymized, Keycloak user deleted by admin",
            ct: ct);

        return Unit.Value;
    }
}