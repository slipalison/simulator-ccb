namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Command to change employee's access group — verifies new group belongs to same company (T-38-11).
/// </summary>
public sealed record ChangeEmployeeAccessGroupCommand(
    Guid EmployeeId,
    Guid CompanyId,
    Guid NewAccessGroupId,
    string ActorSub,
    string ActorEmail,
    string? IpAddress);