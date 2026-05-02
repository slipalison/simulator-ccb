namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// DTO returned from access group CRUD operations.
/// </summary>
public sealed record AccessGroupDto(Guid Id, string Name, IReadOnlyList<string> Permissions, bool IsDefault = false);