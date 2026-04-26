namespace Onboarding.Domain.Aggregates.EmployeeAggregate;

/// <summary>
/// Predefined permission constants using resource:action pattern (D-07).
/// Backend is the authority on all valid permissions — no arbitrary injection (T-37-04).
/// </summary>
public static class Permissions
{
    public const string EmployeesRead = "employees:read";
    public const string EmployeesWrite = "employees:write";
    public const string EmployeesDelete = "employees:delete";
    public const string AuditRead = "audit:read";
    public const string DashboardAccess = "dashboard:access";
    public const string AccessGroupsManage = "access-groups:manage";

    public static readonly string[] All =
    {
        EmployeesRead, EmployeesWrite, EmployeesDelete,
        AuditRead, DashboardAccess, AccessGroupsManage
    };
}