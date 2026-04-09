namespace Onboarding.Application.Admin.DTOs;

/// <summary>
/// Detailed user data including Keycloak status (ADMIN-02).
/// </summary>
public sealed record UserDetailDto(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    string? Document,
    string Type,
    string? RazaoSocial,
    DateTime CreatedAt,
    DateTime? DeletedAt,
    bool KeycloakEnabled,
    bool KeycloakEmailVerified,
    string? KeycloakUserId);
