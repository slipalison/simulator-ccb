namespace Onboarding.Application.Admin.DTOs;

/// <summary>
/// Summary item for paginated user listing (ADMIN-01).
/// </summary>
public sealed record UserSummaryDto(
    Guid Id,
    string Name,
    string Email,
    string? Document,
    string Type,
    bool Enabled,
    DateTime? DeletedAt);
