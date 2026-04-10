using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Onboarding.API.Security;

/// <summary>
/// Claims transformation that extracts realm_access.roles from Keycloak JWT
/// and adds them as flat "role" claims for standard ASP.NET Core authorization.
///
/// Keycloak stores realm roles in the JWT as a nested JSON object:
///   realm_access: { roles: ["admin", "user"] }
///
/// ASP.NET Core's JwtSecurityTokenHandler does not automatically flatten
/// these into role claims, so [Authorize(Roles = "admin")] fails without
/// this transformation.
/// </summary>
public sealed class RealmRolesClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Find the realm_access claim (present in Keycloak JWTs when "roles" scope is enabled)
        var realmAccessClaim = principal.FindFirst("realm_access");
        if (realmAccessClaim == null)
        {
            return Task.FromResult(principal);
        }

        try
        {
            var json = System.Text.Json.JsonDocument.Parse(realmAccessClaim.Value);
            if (json.RootElement.TryGetProperty("roles", out var rolesArray))
            {
                var identity = principal.Identity as ClaimsIdentity;
                if (identity != null)
                {
                    foreach (var role in rolesArray.EnumerateArray())
                    {
                        var roleName = role.GetString();
                        if (!string.IsNullOrEmpty(roleName) &&
                            !principal.IsInRole(roleName))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore parse errors — roles may still be available via other mechanisms
        }

        return Task.FromResult(principal);
    }
}
