using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for Cedente aggregate (D-01, D-09, D-10, CAD-18).
/// HasQueryFilter ensures company isolation — cedentes from other companies are never returned.
/// CedenteDocumento DU persisted via shadow properties (D-09).
/// Composite filtered indexes for CPF/CNPJ uniqueness per company (D-10).
/// </summary>
public sealed class CedenteConfiguration : IEntityTypeConfiguration<Cedente>
{
    private readonly ICurrentCompanyService _currentCompanyService;

    public CedenteConfiguration(ICurrentCompanyService currentCompanyService)
    {
        _currentCompanyService = currentCompanyService;
    }

    public void Configure(EntityTypeBuilder<Cedente> builder)
    {
        builder.ToTable("cedentes");
        builder.HasKey(e => e.Id);

        // Company FK — multi-tenant isolation (D-01, TEN-02)
        builder.Property(e => e.ClienteId)
            .HasColumnName("cliente_id")
            .IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(e => e.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        // D-09: Shadow properties for CedenteDocumento discriminated union persistence
        // Documento is reconstructed from shadow properties via repository (CR-03)
        builder.Ignore(e => e.Documento);

        // documento_tipo stores "PF" or "PJ" discriminator
        builder.Property<string>("DocumentoTipo")
            .HasColumnName("documento_tipo")
            .HasMaxLength(2)
            .IsRequired();

        // cpf stores CPF value when documento_tipo = "PF", null otherwise
        builder.Property<string?>("CpfValue")
            .HasColumnName("cpf")
            .HasMaxLength(11);

        // cnpj_cedente stores CNPJ value when documento_tipo = "PJ", null otherwise
        builder.Property<string?>("CnpjCedenteValue")
            .HasColumnName("cnpj_cedente")
            .HasMaxLength(14);

        // CR-03: Documento property ignored above — no HasConversion, no documento column.
        // Shadow properties (DocumentoTipo, CpfValue, CnpjCedenteValue) handle persistence.
        // Repository reconstructs Documento from shadows after reads.

        builder.Property(e => e.Nome)
            .HasColumnName("nome")
            .HasMaxLength(200)
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

        builder.Property(e => e.Endereco)
            .HasColumnName("endereco")
            .HasMaxLength(500);

        // CedenteStatus stored as int
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // HasQueryFilter — company isolation (D-01, TEN-02)
        builder.HasQueryFilter(e => e.ClienteId == _currentCompanyService.CompanyId);

        // D-10: Composite filtered indexes for CPF/CNPJ uniqueness per company scope
        // Unique CPF per company (documento_tipo = 'PF')
        builder.HasIndex("ClienteId", "CpfValue")
            .IsUnique()
            .HasFilter("documento_tipo = 'PF' AND cpf IS NOT NULL")
            .HasDatabaseName("IX_cedentes_cliente_id_cpf");

        // Unique CNPJ per company (documento_tipo = 'PJ')
        builder.HasIndex("ClienteId", "CnpjCedenteValue")
            .IsUnique()
            .HasFilter("documento_tipo = 'PJ' AND cnpj_cedente IS NOT NULL")
            .HasDatabaseName("IX_cedentes_cliente_id_cnpj_cedente");

        builder.HasIndex(e => e.ClienteId)
            .HasDatabaseName("IX_cedentes_cliente_id");

        // === OwnsMany: CedenteTipoAtivo (D-15) ===
        builder.OwnsMany(c => c.TiposAtivo, cta =>
        {
            cta.ToTable("cedente_tipos_ativo");
            cta.HasKey(cta => cta.Id);

            cta.Property(cta => cta.CedenteId)
                .HasColumnName("cedente_id")
                .IsRequired();

            cta.Property(cta => cta.TipoAtivoId)
                .HasColumnName("tipo_ativo_id")
                .IsRequired();

            // FK to TipoAtivo with Restrict delete (cross-aggregate)
            cta.HasOne<TipoAtivo>()
                .WithMany()
                .HasForeignKey(cta => cta.TipoAtivoId)
                .OnDelete(DeleteBehavior.Restrict);

            cta.HasIndex(cta => new { cta.CedenteId, cta.TipoAtivoId })
                .IsUnique()
                .HasDatabaseName("IX_cedente_tipos_ativo_cedente_id_tipo_ativo_id");
        });
    }
}