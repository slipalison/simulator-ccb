using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;

namespace Onboarding.Infrastructure.Keycloak;

/// <summary>
/// Implements IKeycloakTokenService using IHttpClientFactory (named client "keycloak-token").
/// Makes ROPC calls to Keycloak token endpoint.
/// D-11: uses IHttpClientFactory directly — NOT Duende.AccessTokenManagement (which is for service account CC grant only).
/// D-12: client_id = onboarding-app (public client, no secret).
/// </summary>
public sealed class KeycloakTokenService : IKeycloakTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _realmUrl;
    private readonly string _clientId;

    public KeycloakTokenService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _realmUrl = configuration["Keycloak:RealmUrl"]
            ?? throw new InvalidOperationException("Keycloak:RealmUrl not configured.");
        _clientId = configuration["Keycloak:PublicClientId"]
            ?? throw new InvalidOperationException("Keycloak:PublicClientId not configured.");
    }

    public async Task<TokenResponse> ExchangePasswordAsync(
        string email, string password, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("keycloak-token");
        var response = await client.PostAsync(
            $"{_realmUrl}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = _clientId,
                ["username"] = email,
                ["password"] = password,
                ["scope"] = "openid email profile"
            }),
            ct);

        if (!response.IsSuccessStatusCode)
            // D-13: SEC-08 — do not expose Keycloak error details
            throw new KeycloakAuthException("Invalid credentials.");

        return await DeserializeTokenResponse(response, ct);
    }

    public async Task<TokenResponse> RefreshTokenAsync(
        string refreshToken, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("keycloak-token");
        var response = await client.PostAsync(
            $"{_realmUrl}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _clientId,
                ["refresh_token"] = refreshToken
            }),
            ct);

        if (!response.IsSuccessStatusCode)
            // D-13: SEC-08 — do not expose Keycloak error details
            throw new KeycloakAuthException("Invalid or expired refresh token.");

        return await DeserializeTokenResponse(response, ct);
    }

    private static async Task<TokenResponse> DeserializeTokenResponse(
        HttpResponseMessage response, CancellationToken ct)
    {
        var json = await response.Content.ReadFromJsonAsync<KeycloakTokenJson>(
            cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty token response from Keycloak.");

        return new TokenResponse(
            json.AccessToken,
            json.RefreshToken,
            json.ExpiresIn,
            json.TokenType,
            json.RefreshExpiresIn,
            json.Scope ?? string.Empty);
    }
}

// Internal DTO for JSON deserialization — not exposed outside this class
internal sealed record KeycloakTokenJson(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("refresh_expires_in")] int RefreshExpiresIn,
    [property: JsonPropertyName("scope")] string? Scope);
