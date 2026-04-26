namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Command to register a new employee (PF) linked to a company (PJ).
/// The PJ owner manages employees through this endpoint (REG-03, MGMT-01).
/// </summary>
public sealed record RegisterEmployeeCommand(
    Guid CompanyId,
    string Nome,
    string Cpf,
    string Email,
    string Phone,
    Guid? AccessGroupId,
    string ActorSub,
    string ActorEmail,
    string? IpAddress);