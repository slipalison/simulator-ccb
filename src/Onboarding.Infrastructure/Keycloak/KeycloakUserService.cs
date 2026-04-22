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
    private readonly IHttpClientFactory _httpClientFactory;

    public KeycloakUserService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient GetClient(string targetRealm)
    {
        return targetRealm == "backoffice" 
            ? _httpClientFactory.CreateClient("keycloak-admin-backoffice")
            : _httpClientFactory.CreateClient("keycloak-admin-client");
    }

    public async Task<string> CreateUserAsync(string targetRealm, 
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
            LastName = "-",          // Keycloak 26.x User Profile requires lastName
            Enabled = true,
            EmailVerified = true,
            RequiredActions = [],
        };

        var client = GetClient(targetRealm);

        // Keycloak Admin API: POST /admin/realms/{realm}/users
        var createResponse = await client.PostAsJsonAsync($"admin/realms/{targetRealm}/users", user, ct);
        if (!createResponse.IsSuccessStatusCode)
        {
            var createBody = await createResponse.Content.ReadAsStringAsync(ct);
            if (createResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
                throw new DuplicateKeycloakUserException($"A Keycloak user with email '{email}' already exists. {createBody}");
            throw new Exception($"Failed to create user in Keycloak: {createBody}");
        }

        var users = await client.GetFromJsonAsync<List<UserRepresentation>>(
            $"admin/realms/{targetRealm}/users?email={Uri.EscapeDataString(email)}&exact=true", ct) ?? [];

        var userId = users.FirstOrDefault()?.Id
            ?? throw new InvalidOperationException($"Keycloak created user for email but returned no ID.");

        // Set password explicitly
        var passwordPayload = new { type = "password", value = password, temporary = false };
        var pwdResp = await client.PutAsJsonAsync($"admin/realms/{targetRealm}/users/{userId}/reset-password", passwordPayload, ct);
        if (!pwdResp.IsSuccessStatusCode)
        {
            var body = await pwdResp.Content.ReadAsStringAsync(ct);
            if (pwdResp.StatusCode == System.Net.HttpStatusCode.Conflict)
                throw new DuplicateKeycloakUserException($"Keycloak rejected password reset for user '{userId}' (409): {body}");
            pwdResp.EnsureSuccessStatusCode();
        }

        return userId;
    }

    public async Task<string> CreateAdminUserAsync(string targetRealm, 
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
            Attributes = new Dictionary<string, ICollection<string>>
            {
                ["isFirstLogin"] = new[] { "true" }
            },
        };

        var client = GetClient(targetRealm);
        var createResponse = await client.PostAsJsonAsync($"admin/realms/{targetRealm}/users", user, ct);
        if (!createResponse.IsSuccessStatusCode)
        {
            if (createResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
                throw new DuplicateKeycloakUserException($"A Keycloak user with email '{email}' already exists.");
            createResponse.EnsureSuccessStatusCode();
        }

        var users = await client.GetFromJsonAsync<List<UserRepresentation>>(
            $"admin/realms/{targetRealm}/users?email={Uri.EscapeDataString(email)}&exact=true", ct) ?? [];

        var userId = users.FirstOrDefault()?.Id
            ?? throw new InvalidOperationException($"Keycloak created user for email but returned no ID.");

        var passwordPayload = new { type = "password", value = temporaryPassword, temporary = true };
        var pwdResp = await client.PutAsJsonAsync($"admin/realms/{targetRealm}/users/{userId}/reset-password", passwordPayload, ct);
        if (!pwdResp.IsSuccessStatusCode)
        {
            var body = await pwdResp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Keycloak rejected password set for admin user '{userId}': {body}");
        }

        await AssignAdminRoleAsync(targetRealm, userId, ct);

        return userId;
    }

    public async Task DeleteUserByEmailAsync(string targetRealm, string email, CancellationToken ct = default)
    {
        var users = await GetClient(targetRealm).GetFromJsonAsync<List<UserRepresentation>>(
            $"admin/realms/{targetRealm}/users?email={Uri.EscapeDataString(email)}&exact=true", ct) ?? [];

        var userId = users.FirstOrDefault()?.Id;
        if (userId is not null)
        {
            await GetClient(targetRealm).DeleteAsync($"admin/realms/{targetRealm}/users/{userId}", ct);
        }
    }

    public async Task<bool> UserExistsByEmailAsync(string targetRealm, string email, CancellationToken ct = default)
    {
        var users = await GetClient(targetRealm).GetFromJsonAsync<List<UserRepresentation>>(
            $"admin/realms/{targetRealm}/users?email={Uri.EscapeDataString(email)}&exact=true", ct) ?? [];
        return users.Any();
    }

    public async Task<KeycloakUser?> GetUserByEmailAsync(string targetRealm, string email, CancellationToken ct = default)
    {
        var users = await GetClient(targetRealm).GetFromJsonAsync<List<UserRepresentation>>(
            $"admin/realms/{targetRealm}/users?email={Uri.EscapeDataString(email)}&exact=true", ct) ?? [];

        var user = users.FirstOrDefault();
        if (user == null) return null;

        return new KeycloakUser(user.Id!.ToString(), user.Email ?? email, user.Enabled ?? true, user.EmailVerified ?? false);
    }

    public async Task UpdateUserPasswordAsync(string targetRealm, string userId, string newPassword, CancellationToken ct = default)
    {
        var passwordPayload = new { type = "password", value = newPassword, temporary = false };
        var response = await GetClient(targetRealm).PutAsJsonAsync(
            $"admin/realms/{targetRealm}/users/{userId}/reset-password", passwordPayload, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Keycloak rejected password update for user '{userId}': {body}");
        }
    }

    public async Task ResetPasswordAsTemporaryAsync(string targetRealm, string userId, string newPassword, CancellationToken ct = default)
    {
        var payload = new { type = "password", value = newPassword, temporary = true };
        var response = await GetClient(targetRealm).PutAsJsonAsync(
            $"admin/realms/{targetRealm}/users/{userId}/reset-password", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task BlockUserAsync(string targetRealm, string keycloakUserId, CancellationToken ct = default)
    {
        var client = GetClient(targetRealm);
        var user = await client.GetFromJsonAsync<UserRepresentation>($"admin/realms/{targetRealm}/users/{keycloakUserId}", ct)
            ?? throw new InvalidOperationException($"Keycloak user '{keycloakUserId}' not found.");

        if (user.Enabled == false) return;

        user.Enabled = false;
        await client.PutAsJsonAsync($"admin/realms/{targetRealm}/users/{keycloakUserId}", user, ct);
    }

    public async Task UnblockUserAsync(string targetRealm, string keycloakUserId, CancellationToken ct = default)
    {
        var client = GetClient(targetRealm);
        var user = await client.GetFromJsonAsync<UserRepresentation>($"admin/realms/{targetRealm}/users/{keycloakUserId}", ct)
            ?? throw new InvalidOperationException($"Keycloak user '{keycloakUserId}' not found.");

        if (user.Enabled == true) return;

        user.Enabled = true;
        await client.PutAsJsonAsync($"admin/realms/{targetRealm}/users/{keycloakUserId}", user, ct);
    }

    public async Task<KeycloakUserDetails?> GetUserByIdAsync(string targetRealm, string userId, CancellationToken ct = default)
    {
        var req = await GetClient(targetRealm).GetAsync($"admin/realms/{targetRealm}/users/{userId}", ct);
        if (!req.IsSuccessStatusCode) return null;

        var user = await req.Content.ReadFromJsonAsync<UserRepresentation>(cancellationToken: ct);
        if (user == null) return null;

        return new KeycloakUserDetails(
            user.Id!,
            user.Email ?? string.Empty,
            user.Enabled ?? true,
            user.EmailVerified ?? false,
            (user.RequiredActions ?? []).ToList().AsReadOnly(),
            BuildFullName(user.FirstName, user.LastName));
    }

    public async Task SetTemporaryPasswordFlagAsync(string targetRealm, string userId, CancellationToken ct = default)
    {
        var client = GetClient(targetRealm);
        var user = await client.GetFromJsonAsync<UserRepresentation>($"admin/realms/{targetRealm}/users/{userId}", ct)
            ?? throw new InvalidOperationException($"Keycloak user '{userId}' not found.");

        var requiredActions = user.RequiredActions?.ToList() ?? [];
        if (!requiredActions.Contains("UPDATE_PASSWORD"))
        {
            requiredActions.Add("UPDATE_PASSWORD");
            user.RequiredActions = requiredActions;
            await client.PutAsJsonAsync($"admin/realms/{targetRealm}/users/{userId}", user, ct);
        }
    }

    public async Task RemoveUpdatePasswordRequiredActionAsync(string targetRealm, string userId, CancellationToken ct = default)
    {
        var client = GetClient(targetRealm);
        var user = await client.GetFromJsonAsync<UserRepresentation>($"admin/realms/{targetRealm}/users/{userId}", ct)
            ?? throw new InvalidOperationException($"Keycloak user '{userId}' not found.");

        var requiredActions = user.RequiredActions?.ToList() ?? [];
        if (requiredActions.Remove("UPDATE_PASSWORD"))
        {
            user.RequiredActions = requiredActions;
            await client.PutAsJsonAsync($"admin/realms/{targetRealm}/users/{userId}", user, ct);
        }
    }

    public async Task AssignAdminRoleAsync(string targetRealm, string userId, CancellationToken ct = default)
    {
        const string adminRoleName = "admin";
        var client = GetClient(targetRealm);

        var roleResponse = await client.GetAsync($"admin/realms/{targetRealm}/roles/{adminRoleName}", ct);
        if (!roleResponse.IsSuccessStatusCode)
        {
            var body = await roleResponse.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to get admin role '{adminRoleName}': {body}");
        }

        var roleJson = await roleResponse.Content.ReadFromJsonAsync<JsonDocument>(ct);
        var roleId = roleJson?.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Admin role ID not found.");

        var roleMappingPayload = new[] { new { id = roleId, name = adminRoleName } };
        var assignResponse = await client.PostAsJsonAsync(
            $"admin/realms/{targetRealm}/users/{userId}/role-mappings/realm", roleMappingPayload, ct);

        if (!assignResponse.IsSuccessStatusCode && assignResponse.StatusCode != System.Net.HttpStatusCode.Conflict)
        {
            var body = await assignResponse.Content.ReadAsStringAsync(ct);
            if (!body.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Failed to assign admin role to user '{userId}': {body}");
        }
    }

    public async Task ClearFirstLoginFlagAsync(string targetRealm, string userId, CancellationToken ct = default)
    {
        var client = GetClient(targetRealm);
        var user = await client.GetFromJsonAsync<UserRepresentation>($"admin/realms/{targetRealm}/users/{userId}", ct)
            ?? throw new InvalidOperationException($"Keycloak user '{userId}' not found.");

        var attributes = user.Attributes;
        if (attributes is null || !attributes.TryGetValue("isFirstLogin", out var current) || current.FirstOrDefault() != "true")
            return;

        attributes["isFirstLogin"] = new[] { "false" };
        user.Attributes = attributes;
        await client.PutAsJsonAsync($"admin/realms/{targetRealm}/users/{userId}", user, ct);
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetUsersByRoleAsync(string targetRealm, string roleName, CancellationToken ct = default)
    {
        var response = await GetClient(targetRealm).GetAsync($"admin/realms/{targetRealm}/roles/{Uri.EscapeDataString(roleName)}/users", ct);
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<UserRepresentation>>(cancellationToken: ct) ?? [];

        return users.Select(u => new AdminUserDto(
            Id: u.Id ?? string.Empty,
            Email: u.Email ?? u.Username ?? string.Empty,
            FullName: BuildFullName(u.FirstName, u.LastName),
            IsEnabled: u.Enabled ?? true,
            HasTemporaryPassword: u.RequiredActions?.Contains("UPDATE_PASSWORD") ?? false
        )).ToList().AsReadOnly();
    }

    private static string BuildFullName(string? firstName, string? lastName)
        => string.Join(" ", new[] { firstName, lastName }.Where(s => !string.IsNullOrWhiteSpace(s) && s != "-")).Trim();

    public async Task UpdateAdminUserAsync(string targetRealm, string userId, string fullName, string email, CancellationToken ct = default)
    {
        var client = GetClient(targetRealm);

        // Check if the new email is already in use by someone else
        var existing = await client.GetFromJsonAsync<List<UserRepresentation>>(
            $"admin/realms/{targetRealm}/users?email={Uri.EscapeDataString(email)}&exact=true", ct) ?? [];

        if (existing.Any(u => u.Id != userId))
            throw new ArgumentException($"Email '{email}' is already in use by another account.");

        var user = await client.GetFromJsonAsync<UserRepresentation>($"admin/realms/{targetRealm}/users/{userId}", ct)
            ?? throw new InvalidOperationException($"Keycloak user '{userId}' not found.");

        user.FirstName = fullName;
        user.LastName = "-";
        user.Email = email;
        user.Username = email;

        var response = await client.PutAsJsonAsync($"admin/realms/{targetRealm}/users/{userId}", user, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                throw new ArgumentException($"Email '{email}' is already in use by another account.");
            throw new InvalidOperationException($"Keycloak rejected user update for '{userId}': {body}");
        }
    }

    public async Task LogoutAllSessionsAsync(string targetRealm, string userId, CancellationToken ct = default)
    {
        var client = GetClient(targetRealm);
        // Keycloak Admin API: POST /admin/realms/{realm}/users/{id}/logout
        var response = await client.PostAsync($"admin/realms/{targetRealm}/users/{userId}/logout", null, ct);
        // 204 = success; 404 = user has no sessions (acceptable)
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to logout all sessions for user '{userId}': {body}");
        }
    }
}
