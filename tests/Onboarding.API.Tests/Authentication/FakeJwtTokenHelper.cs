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
}
