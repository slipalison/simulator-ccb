using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Domain.Aggregates.Audit;

namespace Onboarding.Infrastructure.Persistence.Configurations;

public sealed class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.ToTable("admin_audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(a => a.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        builder.Property(a => a.AdminUserId)
            .HasColumnName("admin_user_id")
            .IsRequired();

        builder.Property(a => a.AdminUserName)
            .HasColumnName("admin_user_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.ActionType)
            .HasColumnName("action_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.TargetUserId)
            .HasColumnName("target_user_id");

        builder.Property(a => a.TargetUserName)
            .HasColumnName("target_user_name")
            .HasMaxLength(200);

        builder.Property(a => a.Details)
            .HasColumnName("details")
            .HasColumnType("jsonb");

        builder.Property(a => a.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);

        // Indexes for efficient filtering
        builder.HasIndex(a => a.Timestamp).HasDatabaseName("IX_admin_audit_logs_timestamp");
        builder.HasIndex(a => a.ActionType).HasDatabaseName("IX_admin_audit_logs_action_type");
        builder.HasIndex(a => a.AdminUserId).HasDatabaseName("IX_admin_audit_logs_admin_user_id");
    }
}
