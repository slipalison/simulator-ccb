using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Keycloak.AuthServices.Sdk.Admin.Requests.Users;
using Microsoft.Extensions.Configuration;
using Onboarding.Application.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Onboarding.Infrastructure.Keycloak;

public sealed class KeycloakUserService : IKeycloakUserService
{
    private readonly IKeycloakUserClient _keycloakUserClient;
    private readonly HttpClient _adminHttpClient;
    private readonly string _realm;

    public KeycloakUserService(
        IKeycloakUserClient keycloakUserClient,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _keycloakUserClient = keycloakUserClient;
        _adminHttpClient = httpClientFactory.CreateClient("keycloak-admin-api");
        _realm = configuration["Keycloak:Realm"] ?? "onboarding";
    }

    public async Task<string> CreateUserAsync(
        string username,
        string email,
        string password,
        string firstName,
        CancellationToken ct = default)
    {
        var user = new UserRepresentation
        {
            Username = username,
            Email = email,
            FirstName = firstName,
            Enabled = true,
            EmailVerified = true,
            // Explicitly clear required actions to prevent "Account is not fully set up" error
            // See: https://github.com/keycloak/keycloak/issues/32595
            RequiredActions = [],
        };

        // Keycloak Admin API: POST /admin/realms/{realm}/users → 201 Created (no body, Location header)
        // Credentials set via UserRepresentation may be ignored in Keycloak 26.x — we set password explicitly.
        await _keycloakUserClient.CreateUserAsync(_realm, user, ct);

        var users = await _keycloakUserClient.GetUsersAsync(
            _realm,
            new GetUsersRequestParameters { Email = email, Exact = true },
            ct);

        var userId = users.First().Id
            ?? throw new InvalidOperationException(
                $"Keycloak created user for email but returned no ID.");

        // Explicitly set the password via PUT /admin/realms/{realm}/users/{id}/reset-password
        // This ensures the password is correctly stored regardless of Keycloak version quirks.
        var passwordPayload = new { type = "password", value = password, temporary = false };
        var response = await _adminHttpClient.PutAsJsonAsync(
            $"admin/realms/{_realm}/users/{userId}/reset-password",
            passwordPayload,
            ct);

        response.EnsureSuccessStatusCode();

        return userId;
    }

    public async Task DeleteUserByEmailAsync(string email, CancellationToken ct = default)
    {
        var users = await _keycloakUserClient.GetUsersAsync(
            _realm,
            new GetUsersRequestParameters { Email = email, Exact = true },
            ct);

        var userId = users.FirstOrDefault()?.Id;
        if (userId is not null)
            await _keycloakUserClient.DeleteUserAsync(_realm, userId, ct);
        // No-op if user does not exist
    }
}
