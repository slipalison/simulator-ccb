using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;

namespace Onboarding.Application.Auth.Commands;

/// <summary>
/// Handler for admin login — exchanges credentials via ROPC, validates admin role, returns session data.
/// ADMIN-06: Reuses IKeycloakTokenService for ROPC, then decodes JWT to check for "admin" role.
/// </summary>
public sealed class AdminLoginCommandHandler : ICommandHandler<AdminLoginCommand, AdminSessionResponse>
{
    private readonly IKeycloakTokenService _tokenService;
    private readonly ILogger<AdminLoginCommandHandler> _logger;

    public AdminLoginCommandHandler(
        IKeycloakTokenService tokenService,
        ILogger<AdminLoginCommandHandler> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AdminSessionResponse> HandleAsync(AdminLoginCommand command, CancellationToken ct = default)
    {
        // Step 1: ROPC token exchange
        var tokens = await _tokenService.ExchangePasswordAsync(command.Email, command.Password, ct);

        // Step 2: Decode JWT to extract roles and validate admin access
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokens.AccessToken);

        // Keycloak stores roles in multiple places:
        // 1. "role" claims (flat, mapped via client scopes)
        // 2. "realm_access": {"roles": [...]} — realm roles (nested JSON, not a string claim)
        // 3. "resource_access": {client: {roles: [...]}} — client roles (nested JSON)
        var roles = jwt.Claims
            .Where(c => c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        // Use the raw Payload to access nested JSON claims (realm_access, resource_access)
        // JwtSecurityTokenHandler does NOT convert nested JSON objects to string claims,
        // so jwt.Claims.FirstOrDefault(c => c.Type == "realm_access") always returns null.
        // In System.IdentityModel.Tokens.Jwt v8, nested objects are deserialized as JsonElement.
        var payload = jwt.Payload;

        // Check realm_access for realm roles (e.g., "admin")
        if (payload.TryGetValue("realm_access", out var realmAccessObj) && realmAccessObj is JsonElement realmAccessJson &&
            realmAccessJson.TryGetProperty("roles", out var realmRolesArray))
        {
            foreach (var role in realmRolesArray.EnumerateArray())
            {
                if (role.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(role.GetString()))
                {
                    roles.Add(role.GetString()!);
                }
            }
        }

        // Also check resource_access for client-specific roles
        if (payload.TryGetValue("resource_access", out var resourceAccessObj) && resourceAccessObj is JsonElement resourceAccessJson)
        {
            foreach (var client in resourceAccessJson.EnumerateObject())
            {
                if (client.Value.TryGetProperty("roles", out var clientRolesArray))
                {
                    foreach (var role in clientRolesArray.EnumerateArray())
                    {
                        if (role.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(role.GetString()))
                        {
                            roles.Add(role.GetString()!);
                        }
                    }
                }
            }
        }

        // Step 3: Validate admin role
        if (!roles.Contains("admin"))
        {
            _logger.LogWarning("Login attempt for {Email} denied — missing admin role. Roles: {Roles}",
                command.Email, string.Join(", ", roles));
            throw new UnauthorizedAccessException("Access denied: admin role required.");
        }

        // Step 4: Extract user info
        var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? command.Email;
        var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value
            ?? jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
            ?? email;

        _logger.LogInformation("Admin login successful for {Email}", command.Email);

        return new AdminSessionResponse(
            tokens.RefreshToken,
            tokens.RefreshExpiresIn,
            name,
            email);
    }
}
