using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Onboarding.API.Security;

/// <summary>
/// Claims transformation that extracts realm_access.roles from Keycloak JWT
/// and adds them as flat "role" claims for standard ASP.NET Core authorization.
///
/// Keycloak stores realm roles in the JWT as a nested JSON object:
///   realm_access: { roles: ["admin", "user"] }
///
/// JwtSecurityTokenHandler stores nested JSON objects in the Payload dictionary
/// as JsonElement, NOT as string Claims. So we must access jwt.Payload directly.
/// </summary>
public sealed class RealmRolesClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // The JWT access token is available as the first identity
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null)
        {
            return Task.FromResult(principal);
        }

        // Find the JWT — it's stored as the bootstrap context or we can get it from the actor claim
        // Actually, the simplest way is to find the jti claim and work backwards
        // But the JWT is already validated by JwtBearer, so we need to get it from AuthenticationProperties
        //
        // Better approach: the JwtSecurityTokenHandler adds the raw JWT as a claim
        // with type "actort" or we can use the bootstrap context.
        //
        // Simplest: iterate all claims and look for ones that came from JWT
        // The JWT payload is accessible via the identity.BootstrapContext

        var bootstrapContext = identity.BootstrapContext as JwtSecurityToken;
        if (bootstrapContext == null)
        {
            return Task.FromResult(principal);
        }

        // Access realm_access from the JWT payload (stored as JsonElement)
        if (bootstrapContext.Payload.TryGetValue("realm_access", out var realmAccessValue) &&
            realmAccessValue is JsonElement realmAccess)
        {
            if (realmAccess.TryGetProperty("roles", out var rolesArray))
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

        return Task.FromResult(principal);
    }
}
