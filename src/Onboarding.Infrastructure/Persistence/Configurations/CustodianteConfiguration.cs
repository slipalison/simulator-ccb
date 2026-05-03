using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for Custodiante aggregate (D-01, CAD-08).
/// HasQueryFilter ensures company isolation — custodiantes from other companies are never returned.
/// Unique CNPJ per company prevents duplicate registrations.
/// </summary>
public sealed class CustodianteConfiguration : IEntityTypeConfiguration<Custodiante>
{
    private readonly ICurrentCompanyService _currentCompanyService;

    public CustodianteConfiguration(ICurrentCompanyService currentCompanyService)
    {
        _currentCompanyService = currentCompanyService;
    }

    public void Configure(EntityTypeBuilder<Custodiante> builder)
    {
        builder.ToTable("custodiantes");
        builder.HasKey(e => e.Id);

        // Company FK — multi-tenant isolation (D-01, TEN-02)
        builder.Property(e => e.ClienteId)
            .HasColumnName("cliente_id")
            .IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(e => e.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RazaoSocial)
            .HasColumnName("razao_social")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.CodigoInterno)
            .HasColumnName("codigo_interno")
            .HasMaxLength(50);

        // Value object mapping: Cnpj → string (max 14 chars)
        builder.Property(e => e.Cnpj)
            .HasColumnName("cnpj")
            .HasConversion(
                vo => vo.Value,
                s => Cnpj.Create(s))
            .HasMaxLength(14)
            .IsRequired();

        // Value object mapping: Email → string (nullable)
        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasConversion(
                vo => vo == null ? null : vo.Value,
                s => s == null ? null! : Email.Create(s))
            .HasMaxLength(320);

        // Value object mapping: PhoneNumber → string (nullable)
        builder.Property(e => e.Telefone)
            .HasColumnName("telefone")
            .HasConversion(
                vo => vo == null ? null : vo.Value,
                s => s == null ? null! : PhoneNumber.Create(s))
            .HasMaxLength(20);

        // CustodianteStatus stored as int
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // HasQueryFilter — company isolation (D-01, TEN-02)
        builder.HasQueryFilter(e => e.ClienteId == _currentCompanyService.CompanyId);

        // Unique CNPJ per company per CAD-08 (scoped via HasQueryFilter)
        builder.HasIndex(e => e.Cnpj)
            .IsUnique()
            .HasFilter("cnpj IS NOT NULL")
            .HasDatabaseName("IX_custodiantes_cnpj");

        builder.HasIndex(e => e.ClienteId)
            .HasDatabaseName("IX_custodiantes_cliente_id");
    }
}