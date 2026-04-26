namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Command to block (Activate=false) or unblock (Activate=true) an employee in Keycloak.
/// Blocks keycloak user + revokes sessions, or unblocks keycloak user.
/// Company isolation: employee.CompanyId must match command.CompanyId (T-38-08).
/// </summary>
public sealed record ToggleEmployeeStatusCommand(
    Guid EmployeeId,
    Guid CompanyId,
    bool Activate,
    string ActorSub,
    string ActorEmail,
    string? IpAddress);