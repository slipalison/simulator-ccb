namespace Onboarding.Application.Common;

/// <summary>
/// Scoped service providing the current user's resolved permissions and company ID (D-10, D-09).
/// Populated per-request by ClientClaimsMiddleware from JWT claims + DB AccessGroup lookup.
/// </summary>
public interface ICurrentCompanyPermissionsService
{
    /// <summary>Company ID of the authenticated PJ or Employee's company. Guid.Empty if unauthenticated.</summary>
    Guid CompanyId { get; }

    /// <summary>Resolved permissions (resource:action strings) from AccessGroup lookup. Empty if unauthenticated.</summary>
    IReadOnlyList<string> Permissions { get; }

    /// <summary>Whether the current user is the PJ owner (Company.KeycloakUserId == JWT sub).</summary>
    bool IsCompanyOwner { get; }
}