using Onboarding.Domain.Common;

namespace Onboarding.Domain.Aggregates.Audit;

/// <summary>
/// Append-only audit log for admin management actions.
/// Records all administrative actions (create, block, delete, login, password change) for compliance and investigation.
/// NO Update or Delete methods — this entity is truly immutable by design.
/// </summary>
public sealed class AdminAuditLog : Entity<Guid>
{
    public DateTimeOffset Timestamp { get; private set; }
    public Guid AdminUserId { get; private set; }
    public string AdminUserName { get; private set; } = default!;
    public ActionType ActionType { get; private set; }
    public Guid? TargetUserId { get; private set; }
    public string? TargetUserName { get; private set; }
    public string? Details { get; private set; }
    public string? IpAddress { get; private set; }

    /// <summary>
    /// Private parameterless constructor for EF Core materialization.
    /// CS0628: protected constructor in sealed class — intentional EF Core convention pattern.
    /// </summary>
#pragma warning disable CS0628
    private AdminAuditLog() { }
#pragma warning restore CS0628

    /// <summary>
    /// Factory method — creates a complete admin audit log entry.
    /// </summary>
    public static AdminAuditLog Create(
        Guid adminUserId,
        string adminUserName,
        ActionType action,
        Guid? targetUserId = null,
        string? targetUserName = null,
        string? details = null,
        string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(adminUserName))
            throw new ArgumentException("Admin user name cannot be empty.", nameof(adminUserName));
        if (string.IsNullOrWhiteSpace(details))
            details = null;
        if (string.IsNullOrWhiteSpace(ipAddress))
            ipAddress = null;

        return new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            AdminUserId = adminUserId,
            AdminUserName = adminUserName,
            ActionType = action,
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            Details = details,
            IpAddress = ipAddress,
        };
    }
}
