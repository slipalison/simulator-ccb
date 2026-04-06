using Onboarding.Application.Auth.DTOs;

namespace Onboarding.Application.Common;

/// <summary>
/// Application-layer abstraction over the Keycloak OIDC token endpoint.
/// Infrastructure implements this with KeycloakTokenService via IHttpClientFactory.
/// Keeping this in Application allows unit-testing auth handlers without HTTP dependencies.
/// </summary>
public interface IKeycloakTokenService
{
    /// <summary>
    /// Exchanges user credentials for a token pair using ROPC grant (grant_type=password).
    /// Throws KeycloakAuthException if credentials are invalid (Keycloak returns 401).
    /// </summary>
    Task<TokenResponse> ExchangePasswordAsync(
        string email, string password, CancellationToken ct = default);

    /// <summary>
    /// Exchanges a refresh token for a new token pair (grant_type=refresh_token).
    /// Throws KeycloakAuthException if the refresh token is invalid or expired.
    /// </summary>
    Task<TokenResponse> RefreshTokenAsync(
        string refreshToken, CancellationToken ct = default);
}
