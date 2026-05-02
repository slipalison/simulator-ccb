using Onboarding.Application.Common;

namespace Onboarding.Infrastructure.Persistence;

/// <summary>
/// Scoped service that holds the current user's resolved permissions for the request (D-10).
/// Populated by ClientClaimsMiddleware from JWT sub → Company/Employee → AccessGroup lookup.
/// Defaults to empty/Guid.Empty when not set — HasQueryFilter returns no data for unauthenticated requests.
/// </summary>
public sealed class CurrentCompanyPermissionsService : ICurrentCompanyPermissionsService
{
    public Guid CompanyId { get; set; }
    public List<string> PermissionList { get; set; } = [];
    public bool IsCompanyOwnerFlag { get; set; }

    Guid ICurrentCompanyPermissionsService.CompanyId => CompanyId;
    IReadOnlyList<string> ICurrentCompanyPermissionsService.Permissions => PermissionList;
    bool ICurrentCompanyPermissionsService.IsCompanyOwner => IsCompanyOwnerFlag;
}