using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Keycloak.AuthServices.Sdk.Admin.Requests.Users;
using Microsoft.Extensions.Configuration;
using Onboarding.Application.Common;

namespace Onboarding.Infrastructure.Keycloak;

/// <summary>
/// Implements IKeycloakUserService using Keycloak.AuthServices.Sdk.
/// Called by the command handler after app_db persistence succeeds (REG-06).
/// If CreateUserAsync fails, the handler compensates by calling IClientRepository.DeleteAsync.
/// </summary>
public sealed class KeycloakUserService : IKeycloakUserService
{
    private readonly IKeycloakUserClient _keycloakUserClient;
    private readonly string _realm;

    public KeycloakUserService(IKeycloakUserClient keycloakUserClient, IConfiguration configuration)
    {
        _keycloakUserClient = keycloakUserClient;
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
            Credentials =
            [
                new CredentialRepresentation
                {
                    Type = "password",
                    Value = password,
                    Temporary = false
                }
            ]
        };

        // Keycloak Admin API: POST /admin/realms/{realm}/users → 201 Created (no body, Location header)
        // We must fetch the user ID separately via GetUsersAsync by email.
        await _keycloakUserClient.CreateUserAsync(_realm, user, ct);

        var users = await _keycloakUserClient.GetUsersAsync(
            _realm,
            new GetUsersRequestParameters { Email = email, Exact = true },
            ct);

        return users.First().Id
            ?? throw new InvalidOperationException(
                $"Keycloak created user for email but returned no ID.");
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
