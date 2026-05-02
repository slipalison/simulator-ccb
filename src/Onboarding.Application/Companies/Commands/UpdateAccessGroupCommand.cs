namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Command to update a custom access group's name and/or permissions (PERM-06).
/// Default groups (admin-empresa, viewer, dashboard) cannot be updated.
/// </summary>
public sealed record UpdateAccessGroupCommand(
    Guid CompanyId,
    Guid AccessGroupId,
    string? Name,
    IReadOnlyList<string>? Permissions,
    string ActorSub,
    string ActorEmail,
    string? IpAddress);