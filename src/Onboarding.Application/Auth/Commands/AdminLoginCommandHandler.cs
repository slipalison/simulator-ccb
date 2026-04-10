using System.IdentityModel.Tokens.Jwt;
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
        // 2. "realm_access": {"roles": [...]} — realm roles
        // 3. "resource_access": {client: {roles: [...]}} — client roles
        var roles = jwt.Claims
            .Where(c => c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        // Check realm_access for realm roles (e.g., "admin")
        var realmAccessClaim = jwt.Claims.FirstOrDefault(c => c.Type == "realm_access");
        if (realmAccessClaim != null)
        {
            try
            {
                var json = System.Text.Json.JsonDocument.Parse(realmAccessClaim.Value);
                if (json.RootElement.TryGetProperty("roles", out var rolesArray))
                {
                    foreach (var role in rolesArray.EnumerateArray())
                    {
                        roles.Add(role.GetString()!);
                    }
                }
            }
            catch
            {
                // Ignore parse errors — roles may still come from other sources
            }
        }

        // Also check resource_access claim for client-specific roles
        var resourceAccessClaim = jwt.Claims.FirstOrDefault(c => c.Type == "resource_access");
        if (resourceAccessClaim != null)
        {
            try
            {
                // Parse the JSON to find all role arrays
                var json = System.Text.Json.JsonDocument.Parse(resourceAccessClaim.Value);
                foreach (var client in json.RootElement.EnumerateObject())
                {
                    if (client.Value.TryGetProperty("roles", out var rolesArray))
                    {
                        foreach (var role in rolesArray.EnumerateArray())
                        {
                            roles.Add(role.GetString()!);
                        }
                    }
                }
            }
            catch
            {
                // Ignore parse errors
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
