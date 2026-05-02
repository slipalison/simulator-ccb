using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Onboarding.API.Security;

/// <summary>
/// Claims transformation that extracts the "groups" claim from Keycloak JWT
/// (client realm) and adds each group as a ClaimTypes.Role claim.
/// This makes [Authorize(Roles = "viewer")] work for client realm tokens (D-03).
///
/// Keycloak stores group membership in the JWT as a flat array:
///   groups: ["admin-empresa", "viewer"]
///
/// Pattern follows RealmRolesClaimsTransformation but reads "groups" claim
/// instead of "realm_access.roles". Only applies to BearerClient tokens.
/// </summary>
public sealed class GroupsClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null)
        {
            return Task.FromResult(principal);
        }

        var bootstrapContext = identity.BootstrapContext as JwtSecurityToken;
        if (bootstrapContext == null)
        {
            return Task.FromResult(principal);
        }

        // Access "groups" from the JWT payload (stored as JsonElement array)
        if (bootstrapContext.Payload.TryGetValue("groups", out var groupsValue) &&
            groupsValue is JsonElement groupsElement)
        {
            // Handle both array and single string
            if (groupsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var group in groupsElement.EnumerateArray())
                {
                    var groupName = group.GetString();
                    if (!string.IsNullOrEmpty(groupName) &&
                        !principal.IsInRole(groupName))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, groupName));
                    }
                }
            }
            else if (groupsElement.ValueKind == JsonValueKind.String)
            {
                var groupName = groupsElement.GetString();
                if (!string.IsNullOrEmpty(groupName) &&
                    !principal.IsInRole(groupName))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, groupName));
                }
            }
        }

        return Task.FromResult(principal);
    }
}