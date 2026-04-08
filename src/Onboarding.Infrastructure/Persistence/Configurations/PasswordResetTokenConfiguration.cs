using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Domain.Aggregates.PasswordReset;

namespace Onboarding.Infrastructure.Persistence.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Token)
            .HasColumnName("token")
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(x => x.Token)
            .IsUnique()
            .HasDatabaseName("ix_password_reset_tokens_token");

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(x => x.IsUsed)
            .HasColumnName("is_used")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => x.Email)
            .HasDatabaseName("ix_password_reset_tokens_email");
    }
}
