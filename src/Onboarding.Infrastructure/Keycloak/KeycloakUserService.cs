using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Keycloak.AuthServices.Sdk.Admin.Requests.Users;
using Microsoft.Extensions.Configuration;
using Onboarding.Application.Common;
using Onboarding.Domain.Exceptions;
using System.Net.Http.Json;
using System.Text.Json;

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
            LastName = "-",          // Keycloak 26.x User Profile requires lastName; use placeholder
            Enabled = true,
            EmailVerified = true,
            RequiredActions = [],
        };

        try
        {
            // Keycloak Admin API: POST /admin/realms/{realm}/users → 201 Created (no body, Location header)
            // Credentials set via UserRepresentation may be ignored in Keycloak 26.x — we set password explicitly.
            await _keycloakUserClient.CreateUserAsync(_realm, user, ct);
        }
        catch (Exception ex) when (IsConflictException(ex))
        {
            // Keycloak returns 409 when a user with the same username, email, or email+realm already exists
            throw new DuplicateKeycloakUserException(
                $"A Keycloak user with email '{email}' already exists.", ex);
        }

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

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                throw new DuplicateKeycloakUserException(
                    $"Keycloak rejected password reset for user '{userId}' (409): {body}");

            response.EnsureSuccessStatusCode();
        }

        return userId;
    }

    public async Task<string> CreateAdminUserAsync(
        string email,
        string temporaryPassword,
        string fullName,
        CancellationToken ct = default)
    {
        var user = new UserRepresentation
        {
            Username = email,
            Email = email,
            FirstName = fullName,
            LastName = "-",
            Enabled = true,
            EmailVerified = true,
            RequiredActions = ["UPDATE_PASSWORD"],
        };

        try
        {
            await _keycloakUserClient.CreateUserAsync(_realm, user, ct);
        }
        catch (Exception ex) when (IsConflictException(ex))
        {
            throw new DuplicateKeycloakUserException(
                $"A Keycloak user with email '{email}' already exists.", ex);
        }

        var users = await _keycloakUserClient.GetUsersAsync(
            _realm,
            new GetUsersRequestParameters { Email = email, Exact = true },
            ct);

        var userId = users.First().Id
            ?? throw new InvalidOperationException(
                $"Keycloak created user for email but returned no ID.");

        // Set temporary password with temporary = true
        var passwordPayload = new { type = "password", value = temporaryPassword, temporary = true };
        var response = await _adminHttpClient.PutAsJsonAsync(
            $"admin/realms/{_realm}/users/{userId}/reset-password",
            passwordPayload,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Keycloak rejected password set for admin user '{userId}': {body}");
        }

        // Assign admin role
        await AssignAdminRoleAsync(userId, ct);

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

    public async Task<bool> UserExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        var users = await _keycloakUserClient.GetUsersAsync(
            _realm,
            new GetUsersRequestParameters { Email = email, Exact = true },
            ct);

        return users.Any();
    }

    public async Task<KeycloakUser?> GetUserByEmailAsync(string email, CancellationToken ct = default)
    {
        var users = await _keycloakUserClient.GetUsersAsync(
            _realm,
            new GetUsersRequestParameters { Email = email, Exact = true },
            ct);

        var user = users.FirstOrDefault();
        if (user == null) return null;

        return new KeycloakUser(user.Id!.ToString(), user.Email ?? email);
    }

    public async Task UpdateUserPasswordAsync(string userId, string newPassword, CancellationToken ct = default)
    {
        var passwordPayload = new { type = "password", value = newPassword, temporary = false };
        var response = await _adminHttpClient.PutAsJsonAsync(
            $"admin/realms/{_realm}/users/{userId}/reset-password",
            passwordPayload,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Keycloak rejected password update for user '{userId}': {body}");
        }
    }

    public async Task BlockUserAsync(string keycloakUserId, CancellationToken ct = default)
    {
        var user = await _keycloakUserClient.GetUserAsync(_realm, keycloakUserId, cancellationToken: ct)
            ?? throw new InvalidOperationException($"Keycloak user '{keycloakUserId}' not found.");

        // Idempotency: if already disabled, skip update call
        if (user.Enabled == false) return;

        user.Enabled = false;
        await _keycloakUserClient.UpdateUserAsync(_realm, keycloakUserId, user, ct);
    }

    public async Task UnblockUserAsync(string keycloakUserId, CancellationToken ct = default)
    {
        var user = await _keycloakUserClient.GetUserAsync(_realm, keycloakUserId, cancellationToken: ct)
            ?? throw new InvalidOperationException($"Keycloak user '{keycloakUserId}' not found.");

        // Idempotency: if already enabled, skip update call
        if (user.Enabled == true) return;

        user.Enabled = true;
        await _keycloakUserClient.UpdateUserAsync(_realm, keycloakUserId, user, ct);
    }

    public async Task<KeycloakUserDetails?> GetUserByIdAsync(string userId, CancellationToken ct = default)
    {
        var user = await _keycloakUserClient.GetUserAsync(_realm, userId, cancellationToken: ct);
        if (user == null) return null;

        return new KeycloakUserDetails(
            user.Id!,
            user.Email ?? string.Empty,
            user.Enabled ?? true,
            user.EmailVerified ?? false,
            (user.RequiredActions ?? []).ToList().AsReadOnly());
    }

    public async Task SetTemporaryPasswordFlagAsync(string userId, CancellationToken ct = default)
    {
        var user = await _keycloakUserClient.GetUserAsync(_realm, userId, cancellationToken: ct)
            ?? throw new InvalidOperationException($"Keycloak user '{userId}' not found.");

        var requiredActions = user.RequiredActions?.ToList() ?? [];
        if (!requiredActions.Contains("UPDATE_PASSWORD"))
        {
            requiredActions.Add("UPDATE_PASSWORD");
            user.RequiredActions = requiredActions;
            await _keycloakUserClient.UpdateUserAsync(_realm, userId, user, ct);
        }
    }

    public async Task RemoveUpdatePasswordRequiredActionAsync(string userId, CancellationToken ct = default)
    {
        var user = await _keycloakUserClient.GetUserAsync(_realm, userId, cancellationToken: ct)
            ?? throw new InvalidOperationException($"Keycloak user '{userId}' not found.");

        var requiredActions = user.RequiredActions?.ToList() ?? [];
        if (requiredActions.Remove("UPDATE_PASSWORD"))
        {
            user.RequiredActions = requiredActions;
            await _keycloakUserClient.UpdateUserAsync(_realm, userId, user, ct);
        }
    }

    public async Task AssignAdminRoleAsync(string userId, CancellationToken ct = default)
    {
        // The admin role name from realm.json
        const string adminRoleName = "admin";

        // Get realm-level role ID
        var roleResponse = await _adminHttpClient.GetAsync(
            $"admin/realms/{_realm}/roles/{adminRoleName}",
            ct);

        if (!roleResponse.IsSuccessStatusCode)
        {
            var body = await roleResponse.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Failed to get admin role '{adminRoleName}': {body}");
        }

        var roleJson = await roleResponse.Content.ReadFromJsonAsync<JsonDocument>(ct);
        var roleId = roleJson?.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Admin role ID not found.");

        // Assign the realm role to the user
        var roleMappingPayload = new[]
        {
            new { id = roleId, name = adminRoleName }
        };

        var assignResponse = await _adminHttpClient.PostAsJsonAsync(
            $"admin/realms/{_realm}/users/{userId}/role-mappings/realm",
            roleMappingPayload,
            ct);

        if (!assignResponse.IsSuccessStatusCode && assignResponse.StatusCode != System.Net.HttpStatusCode.Conflict)
        {
            var body = await assignResponse.Content.ReadAsStringAsync(ct);
            // If role already assigned, ignore
            if (body.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                return;
            throw new InvalidOperationException(
                $"Failed to assign admin role to user '{userId}': {body}");
        }
    }

    /// <summary>
    /// Detects if an exception indicates a 409 Conflict from Keycloak.
    /// The SDK may wrap the original HttpResponseMessage or throw HttpRequestException.
    /// </summary>
    private static bool IsConflictException(Exception ex)
    {
        // Check for HttpRequestException with 409 status code
        if (ex is System.Net.Http.HttpRequestException hrex && hrex.StatusCode == System.Net.HttpStatusCode.Conflict)
            return true;

        // Check inner exceptions too — the SDK may wrap the original
        if (ex.InnerException != null && IsConflictException(ex.InnerException))
            return true;

        // Check exception message for 409 indicators
        if (ex.Message.Contains("409", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
