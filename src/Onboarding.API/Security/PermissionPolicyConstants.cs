namespace Onboarding.API.Security;

/// <summary>
/// Policy name constants for permission-based authorization (D-06, D-07).
/// 6 permission-based policies + 1 role-based policy for cross-company admin access
/// + 4 fund permission policies for the Fundos module (Phase 48, T-48.1).
/// Used as [Authorize(Policy = PermissionPolicies.EmployeeRead)] on controller endpoints.
/// Each constant value intentionally equals its own name — policy names are opaque keys
/// registered in Program.cs and mapped to Keycloak claim values via PermissionRequirement.
/// </summary>
public static class PermissionPolicies
{
    public const string EmployeeRead = "EmployeeRead";
    public const string EmployeeWrite = "EmployeeWrite";
    public const string EmployeeDelete = "EmployeeDelete";
    public const string AuditRead = "AuditRead";
    public const string DashboardAccess = "DashboardAccess";
    public const string AccessGroupsManage = "AccessGroupsManage";
    public const string CrossCompanyAccess = "CrossCompanyAccess";

    // Fund permission policies — map to Permissions.FundsRead/Write/Delete/Manage claim values
    public const string FundRead = "FundRead";
    public const string FundWrite = "FundWrite";
    public const string FundDelete = "FundDelete";
    public const string FundManage = "FundManage";
}