using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Domain.Aggregates.Audit;

namespace Onboarding.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(a => a.AdminSub)
            .HasColumnName("admin_sub")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.AdminEmail)
            .HasColumnName("admin_email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(a => a.Action)
            .HasColumnName("action")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.TargetUserId)
            .HasColumnName("target_user_id");

        builder.Property(a => a.TargetEmail)
            .HasColumnName("target_email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(a => a.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        // JSONB columns for audit snapshots — stored as text, serialized by the application layer.
        builder.Property(a => a.SnapshotBefore)
            .HasColumnName("snapshot_before")
            .HasColumnType("jsonb");

        builder.Property(a => a.SnapshotAfter)
            .HasColumnName("snapshot_after")
            .HasColumnType("jsonb");

        builder.Property(a => a.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);

        builder.Property(a => a.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500);
    }
}
