namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Command to delete a custom access group (PERM-06).
/// Default groups cannot be deleted. Groups with linked employees cannot be deleted.
/// </summary>
public sealed record DeleteAccessGroupCommand(
    Guid CompanyId,
    Guid AccessGroupId,
    string ActorSub,
    string ActorEmail,
    string? IpAddress);