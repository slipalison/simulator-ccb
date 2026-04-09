using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Onboarding.API.Tests.Authentication;

/// <summary>
/// Generates unsigned JWT tokens for testing protected endpoints without a real Keycloak.
/// Used with AuthTestApiFactory which disables signature validation via PostConfigure.
/// </summary>
public static class FakeJwtTokenHelper
{
    public static string GenerateFakeJwt(string email, string? sub = null)
    {
        var claims = new List<Claim>
        {
            new("email", email),
            new("sub", sub ?? Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "http://localhost",
            audience: "http://localhost",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a JWT that mimics Keycloak's token structure with resource_access.admin-api.roles = ["admin"].
    /// This is needed for the KeycloakRolesClaimsTransformation to work correctly.
    /// </summary>
    public static string GenerateAdminJwt(string email = "admin@test.com", string? sub = null)
    {
        var claims = new List<Claim>
        {
            new("email", email),
            new("sub", sub ?? Guid.NewGuid().ToString()),
            // Simple "role" claim — TestClaimsTransformation maps this to ClaimTypes.Role
            new("role", "admin")
        };

        var token = new JwtSecurityToken(
            issuer: "http://localhost",
            audience: "http://localhost",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a JWT WITHOUT the admin role — used to test 403 Forbidden responses.
    /// </summary>
    public static string GenerateNonAdminJwt(string email = "user@test.com", string? sub = null)
    {
        var claims = new List<Claim>
        {
            new("email", email),
            new("sub", sub ?? Guid.NewGuid().ToString())
            // No "role" claim — simulates a regular user
        };

        var token = new JwtSecurityToken(
            issuer: "http://localhost",
            audience: "http://localhost",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
