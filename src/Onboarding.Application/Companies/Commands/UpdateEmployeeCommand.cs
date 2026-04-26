namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Command to update employee data (name, email, phone) — syncs to Keycloak (MGMT-05).
/// Company isolation enforced (T-38-08).
/// </summary>
public sealed record UpdateEmployeeCommand(
    Guid EmployeeId,
    Guid CompanyId,
    string Nome,
    string Email,
    string Phone,
    string ActorSub,
    string ActorEmail,
    string? IpAddress);