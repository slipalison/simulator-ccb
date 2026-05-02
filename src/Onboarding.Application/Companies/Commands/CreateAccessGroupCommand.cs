using Onboarding.Domain.Aggregates.EmployeeAggregate;

namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Command to create a custom access group for a company (PERM-06).
/// Only users with access-groups:manage permission can execute this.
/// </summary>
public sealed record CreateAccessGroupCommand(
    Guid CompanyId,
    string Name,
    IReadOnlyList<string> Permissions,
    string ActorSub,
    string ActorEmail,
    string? IpAddress);