using Onboarding.Domain.Common;

namespace Onboarding.Domain.Aggregates.Audit;

/// <summary>
/// Audit log entity — records every admin action for LGPD compliance and incident investigation.
/// All properties are privately set and assigned only via the factory method Create().
/// </summary>
public sealed class AuditLog : Entity<Guid>
{
    public string AdminSub { get; private set; } = default!;
    public string AdminEmail { get; private set; } = default!;
    public string Action { get; private set; } = default!;
    public Guid? TargetUserId { get; set; }
    public string TargetEmail { get; set; } = default!;
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public string? SnapshotBefore { get; set; }
    public string? SnapshotAfter { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Protected parameterless constructor for EF Core materialization.
    // CS0628: protected constructor in sealed class — intentional EF Core convention pattern.
#pragma warning disable CS0628
    protected AuditLog() { }
#pragma warning restore CS0628

    /// <summary>
    /// Factory method — creates a complete audit log entry.
    /// </summary>
    public static AuditLog Create(
        string adminSub,
        string adminEmail,
        string action,
        Guid? targetUserId,
        string targetEmail,
        string? snapshotBefore,
        string? snapshotAfter,
        string? ip,
        string? ua)
    {
        if (string.IsNullOrWhiteSpace(adminSub))
            throw new ArgumentException("Admin sub cannot be empty.", nameof(adminSub));
        if (string.IsNullOrWhiteSpace(adminEmail))
            throw new ArgumentException("Admin email cannot be empty.", nameof(adminEmail));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty.", nameof(action));

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            AdminSub = adminSub,
            AdminEmail = adminEmail,
            Action = action,
            TargetUserId = targetUserId,
            TargetEmail = targetEmail,
            Timestamp = DateTime.UtcNow,
            SnapshotBefore = snapshotBefore,
            SnapshotAfter = snapshotAfter,
            IpAddress = ip,
            UserAgent = ua,
        };
    }
}
