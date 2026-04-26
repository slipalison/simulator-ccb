namespace Onboarding.Domain.Aggregates.Audit;

/// <summary>
/// Represents the type of admin action recorded in the audit log.
/// </summary>
public enum ActionType
{
    AdminCreated = 1,
    AdminBlocked = 2,
    AdminUnblocked = 3,
    AdminDeleted = 4,
    AdminLogin = 5,
    AdminLogout = 6,
    AdminPasswordChanged = 7,
    AdminProfileUpdated = 8,
    UserCreated = 9,
    UserBlocked = 10,
    UserUnblocked = 11,
    UserDeleted = 12,
    UserUpdated = 13,
    // Phase 35 — Admin Management
    AdminEdited = 14,
    AdminPasswordReset = 15,
    AdminDisabled = 16,
    AdminReactivated = 17,
    // Phase 37 — Company/Employee actions
    CompanyRegistered = 18,
    EmployeeCreated = 19,
    EmployeeEdited = 20,
    EmployeeBlocked = 21,
    EmployeeUnblocked = 22,
    EmployeePasswordReset = 23,
    EmployeeDeleted = 24,
    AccessGroupChanged = 25,
}
