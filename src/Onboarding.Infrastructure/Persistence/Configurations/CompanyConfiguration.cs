using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    private readonly ICurrentCompanyService _currentCompanyService;

    public CompanyConfiguration(ICurrentCompanyService currentCompanyService)
    {
        _currentCompanyService = currentCompanyService;
    }

    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.RazaoSocial)
            .HasColumnName("razao_social")
            .HasMaxLength(200)
            .IsRequired();

        // Value object mapping: store only the normalized Value string.
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

        builder.Property(c => c.Cnpj)
            .HasColumnName("cnpj")
            .HasConversion(
                vo => vo == null ? null : vo.Value,
                s => s == null ? null! : Cnpj.Create(s))
            .HasMaxLength(14)
            .IsRequired(false);

        builder.Property(c => c.KeycloakUserId)
            .HasColumnName("keycloak_user_id")
            .HasMaxLength(256);

        // TermsAcceptance as owned type (D-11, D-13)
        builder.OwnsOne(c => c.TermsAcceptance, ta =>
        {
            ta.Property(t => t.AcceptedAt)
                .HasColumnName("terms_accepted_at")
                .IsRequired();
            ta.Property(t => t.TermsVersion)
                .HasColumnName("terms_version")
                .HasMaxLength(20)
                .IsRequired();
            ta.Property(t => t.IpAddress)
                .HasColumnName("terms_ip_address")
                .HasMaxLength(45)
                .IsRequired();
        });

        // Unique indexes — DB-level safety net for REG-02.
        builder.HasIndex(c => c.Email).IsUnique();

        builder.HasIndex(c => c.Cnpj)
            .IsUnique()
            .HasFilter("cnpj IS NOT NULL");

        // LGPD soft-delete column
        builder.Property(c => c.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false);
    }
}