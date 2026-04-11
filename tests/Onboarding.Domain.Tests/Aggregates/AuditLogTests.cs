using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Common;
using Shouldly;

namespace Onboarding.Domain.Tests.Aggregates.Audit;

public class AuditLogTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateAuditLog()
    {
        // Arrange
        var adminSub = "admin-sub-123";
        var adminEmail = "admin@example.com";
        var action = AuditActions.UserBlocked;
        var targetUserId = Guid.NewGuid();
        var targetEmail = "user@example.com";
        var snapshotBefore = "{\"enabled\": true}";
        var snapshotAfter = "{\"enabled\": false}";
        var ip = "127.0.0.1";
        var ua = "Mozilla/5.0";

        // Act
        var auditLog = AuditLog.Create(
            adminSub, adminEmail, action, targetUserId, targetEmail,
            snapshotBefore, snapshotAfter, ip, ua);

        // Assert
        auditLog.Id.ShouldNotBe(Guid.Empty);
        auditLog.AdminSub.ShouldBe(adminSub);
        auditLog.AdminEmail.ShouldBe(adminEmail);
        auditLog.Action.ShouldBe(action);
        auditLog.TargetUserId.ShouldBe(targetUserId);
        auditLog.TargetEmail.ShouldBe(targetEmail);
        auditLog.SnapshotBefore.ShouldBe(snapshotBefore);
        auditLog.SnapshotAfter.ShouldBe(snapshotAfter);
        auditLog.IpAddress.ShouldBe(ip);
        auditLog.UserAgent.ShouldBe(ua);
        auditLog.Timestamp.ShouldNotBe(default);
    }

    [Fact]
    public void Create_WithEmptyAdminSub_ShouldThrowArgumentException()
    {
        // Arrange + Act + Assert
        var ex = Should.Throw<ArgumentException>(() =>
            AuditLog.Create("", "admin@example.com", AuditActions.UserBlocked,
                Guid.NewGuid(), "user@example.com", null, null, null, null));

        ex.ParamName.ShouldBe("adminSub");
    }

    [Fact]
    public void Create_WithEmptyAction_ShouldThrowArgumentException()
    {
        // Arrange + Act + Assert
        var ex = Should.Throw<ArgumentException>(() =>
            AuditLog.Create("sub", "admin@example.com", "",
                Guid.NewGuid(), "user@example.com", null, null, null, null));

        ex.ParamName.ShouldBe("action");
    }

    [Fact]
    public void Create_WithNullTargetUserId_ShouldAllow()
    {
        // Arrange + Act
        var auditLog = AuditLog.Create(
            "sub", "admin@example.com", AuditActions.UserViewed,
            null, "user@example.com", null, null, null, null);

        // Assert
        auditLog.TargetUserId.ShouldBeNull();
    }
}
