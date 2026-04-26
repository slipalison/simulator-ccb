using System.Security.Cryptography;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Handler: resets employee password with crypto-random temp password (MGMT-04, T-38-12).
/// Forces UPDATE_PASSWORD on next login via Keycloak. Company isolation enforced.
/// </summary>
public sealed class ResetEmployeePasswordCommandHandler : ICommandHandler<ResetEmployeePasswordCommand, ResetEmployeePasswordResult>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;

    public ResetEmployeePasswordCommandHandler(
        IEmployeeRepository employeeRepository,
        IKeycloakUserService keycloakUserService,
        IAuditService auditService)
    {
        _employeeRepository = employeeRepository;
        _keycloakUserService = keycloakUserService;
        _auditService = auditService;
    }

    public async Task<ResetEmployeePasswordResult> HandleAsync(
        ResetEmployeePasswordCommand command, CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, ct)
            ?? throw new KeyNotFoundException($"Employee with ID {command.EmployeeId} not found.");

        // Company isolation (T-38-08)
        if (employee.CompanyId != command.CompanyId)
            throw new InvalidOperationException("Employee does not belong to the specified company.");

        // Generate crypto-random temp password (T-38-12)
        var tempPassword = GenerateTempPassword();

        // Reset password as temporary in Keycloak — forces UPDATE_PASSWORD on next login
        await _keycloakUserService.ResetPasswordAsTemporaryAsync(
            "client", employee.KeycloakUserId!, tempPassword, ct);

        // Audit (MGMT-04, T-38-10)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.EmployeePasswordReset,
            targetUserId: employee.Id,
            targetUserName: employee.Nome,
            details: "Password reset — temporary password issued",
            ipAddress: command.IpAddress,
            ct: ct);

        return new ResetEmployeePasswordResult(tempPassword);
    }

    private static string GenerateTempPassword()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "x").Replace("/", "y").Replace("=", "") + "!Aa1";
    }
}