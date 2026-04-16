namespace Onboarding.Domain.Common;

/// <summary>
/// Standardized audit action names used across the admin API.
/// </summary>
public static class AuditActions
{
    public const string UserViewed = "USER_VIEWED";
    public const string UserDetailsViewed = "USER_DETAILS_VIEWED";
    public const string UserUpdated = "USER_UPDATED";
    public const string UserBlocked = "USER_BLOCKED";
    public const string UserUnblocked = "USER_UNBLOCKED";
    public const string UserDeleted = "USER_DELETED";
    public const string UserAnonymized = "USER_ANONYMIZED";
}
