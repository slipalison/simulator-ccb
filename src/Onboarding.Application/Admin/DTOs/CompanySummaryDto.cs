namespace Onboarding.Application.Admin.DTOs;

/// <summary>
/// Summary item for paginated company listing (ADMIN-01).
/// </summary>
public sealed record CompanySummaryDto(
    Guid Id,
    string RazaoSocial,
    string Cnpj,
    string Email,
    string Phone,
    bool IsDeleted,
    string? KeycloakUserId);