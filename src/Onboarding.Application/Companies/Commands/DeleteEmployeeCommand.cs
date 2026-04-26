namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Command to LGPD-delete an employee — anonymizes PII in DB + deletes Keycloak user (MGMT-05).
/// Idempotent: calling on an already-deleted employee is a no-op on Anonymize().
/// Company isolation enforced (T-38-08, T-38-09).
/// </summary>
public sealed record DeleteEmployeeCommand(
    Guid EmployeeId,
    Guid CompanyId,
    string ActorSub,
    string ActorEmail,
    string? IpAddress);