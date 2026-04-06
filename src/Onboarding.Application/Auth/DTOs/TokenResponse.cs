namespace Onboarding.Application.Auth.DTOs;

/// <summary>
/// Token response returned by Keycloak OIDC token endpoint.
/// Maps to Keycloak JSON: access_token, refresh_token, expires_in, token_type, refresh_expires_in, scope.
/// [Claude's Discretion] — all Keycloak fields included for full compatibility with Phase 9 frontend.
/// </summary>
public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    int RefreshExpiresIn,
    string Scope);
