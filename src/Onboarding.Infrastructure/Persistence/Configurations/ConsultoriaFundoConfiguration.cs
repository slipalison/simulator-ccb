using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for ConsultoriaFundo aggregate (D-01, CAD-04).
/// HasQueryFilter ensures company isolation — consultorias from other companies are never returned.
/// Unique CNPJ per company prevents duplicate registrations.
/// </summary>
public sealed class ConsultoriaFundoConfiguration : IEntityTypeConfiguration<ConsultoriaFundo>
{
    private readonly ICurrentCompanyService _currentCompanyService;

    public ConsultoriaFundoConfiguration(ICurrentCompanyService currentCompanyService)
    {
        _currentCompanyService = currentCompanyService;
    }

    public void Configure(EntityTypeBuilder<ConsultoriaFundo> builder)
    {
        builder.ToTable("consultoria_fundos");
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

        builder.Property(e => e.NomeFantasia)
            .HasColumnName("nome_fantasia")
            .HasMaxLength(200);

        // Value object mapping: Cnpj → string (max 14 chars)
        builder.Property(e => e.Cnpj)
            .HasColumnName("cnpj")
            .HasConversion(
                vo => vo.Value,
                s => Cnpj.Create(s))
            .HasMaxLength(14)
            .IsRequired();

        // Shadow string property for ILike search translation (B3-iter3).
        // EF.Property<string>(c, "CnpjRaw") is translatable to SQL in expression trees;
        // c.Cnpj.Value is not, because EF Core 10 cannot bridge HasConversion in LINQ-to-SQL.
        // Maps to the same column so no migration is needed.
        builder.Property<string>("CnpjRaw")
            .HasColumnName("cnpj")
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

        // ConsultoriaFundoStatus stored as int
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // HasQueryFilter — company isolation (D-01, TEN-02)
        builder.HasQueryFilter(e => e.ClienteId == _currentCompanyService.CompanyId);

        // Composite unique index — CNPJ uniqueness scoped per company (CR-01)
        // HasQueryFilter is runtime-only; DB constraint must be composite
        builder.HasIndex(e => new { e.ClienteId, e.Cnpj })
            .IsUnique()
            .HasDatabaseName("IX_consultoria_fundos_cliente_id_cnpj");

        builder.HasIndex(e => e.ClienteId)
            .HasDatabaseName("IX_consultoria_fundos_cliente_id");
    }
}