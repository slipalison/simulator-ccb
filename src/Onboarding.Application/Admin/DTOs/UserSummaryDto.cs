namespace Onboarding.Application.Admin.DTOs;

/// <summary>
/// Summary item for paginated company listing (ADMIN-01).
/// </summary>
public sealed record UserSummaryDto(
    Guid Id,
    string RazaoSocial,
    string Email,
    string? Cnpj,
    string Type,  // Always "PJ" for companies
    bool Enabled,
    DateTime? DeletedAt);