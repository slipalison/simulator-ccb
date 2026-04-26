namespace Onboarding.Application.Admin.DTOs;

/// <summary>
/// Detailed company data including Keycloak status (ADMIN-02).
/// </summary>
public sealed record UserDetailDto(
    Guid Id,
    string RazaoSocial,
    string Email,
    string Phone,
    string? Cnpj,
    string Type,  // Always "PJ"
    DateTime CreatedAt,
    DateTime? DeletedAt,
    bool KeycloakEnabled,
    bool KeycloakEmailVerified,
    string? KeycloakUserId);