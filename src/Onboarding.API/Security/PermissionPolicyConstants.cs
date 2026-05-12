namespace Onboarding.API.Security;

/// <summary>
/// Policy name constants for permission-based authorization (D-06, D-07, PERM-02).
/// 10 permission-based policies + 1 role-based policy for cross-company admin access.
/// Used as [Authorize(Policy = PermissionPolicies.EmployeeRead)] on controller endpoints.
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

    // Fund permission policies (PERM-02)
    public const string FundRead = "FundRead";
    public const string FundWrite = "FundWrite";
    public const string FundDelete = "FundDelete";
    public const string FundManage = "FundManage";
}