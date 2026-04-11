using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Infrastructure.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .IsRequired();

        // Value object mapping: store only the normalized Value string.
        // HasConversion Create() calls are safe here: stored values were written by the same
        // app using the same factory methods, so validation never throws during materialization.
        builder.Property(c => c.Email)
            .HasColumnName("email")
            .HasConversion(
                vo => vo.Value,
                s => Email.Create(s))
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(c => c.Phone)
            .HasColumnName("phone")
            .HasConversion(
                vo => vo.Value,
                s => PhoneNumber.Create(s))
            .HasMaxLength(20);

        builder.Property(c => c.Cpf)
            .HasColumnName("cpf")
            .HasConversion(
                vo => vo == null ? null : vo.Value,
                s => s == null ? null : Cpf.Create(s))
            .HasMaxLength(11);

        builder.Property(c => c.Cnpj)
            .HasColumnName("cnpj")
            .HasConversion(
                vo => vo == null ? null : vo.Value,
                s => s == null ? null : Cnpj.Create(s))
            .HasMaxLength(14);

        builder.Property(c => c.RazaoSocial)
            .HasColumnName("razao_social")
            .HasMaxLength(200);

        // Unique indexes — DB-level safety net for REG-05.
        // PostgreSQL treats NULLs as non-equal in unique indexes by default.
        // HasFilter ensures the unique constraint only applies to non-null rows (partial index).
        builder.HasIndex(c => c.Email).IsUnique();

        builder.HasIndex(c => c.Cpf)
            .IsUnique()
            .HasFilter("cpf IS NOT NULL");

        builder.HasIndex(c => c.Cnpj)
            .IsUnique()
            .HasFilter("cnpj IS NOT NULL");

        // LGPD soft-delete column
        builder.Property(c => c.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false);
    }
}
