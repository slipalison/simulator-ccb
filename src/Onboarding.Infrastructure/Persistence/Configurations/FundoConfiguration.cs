using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for Fundo aggregate (D-01, D-08, D-11, D-15, D-16).
/// HasQueryFilter ensures company isolation — fundos from other companies are never returned.
/// OwnsMany for FundoCedente and FundoTipoAtivo (D-15).
/// </summary>
public sealed class FundoConfiguration : IEntityTypeConfiguration<Fundo>
{
    private readonly ICurrentCompanyService _currentCompanyService;

    public FundoConfiguration(ICurrentCompanyService currentCompanyService)
    {
        _currentCompanyService = currentCompanyService;
    }

    public void Configure(EntityTypeBuilder<Fundo> builder)
    {
        builder.ToTable("fundos");
        builder.HasKey(e => e.Id);

        // Company FK — multi-tenant isolation (D-01, TEN-01)
        builder.Property(e => e.ClienteId)
            .HasColumnName("cliente_id")
            .IsRequired();

        // Value object mapping: Cnpj → string (max 14 chars)
        builder.Property(e => e.Cnpj)
            .HasColumnName("cnpj")
            .HasConversion(
                vo => vo.Value,
                s => Cnpj.Create(s))
            .HasMaxLength(14)
            .IsRequired();

        builder.Property(e => e.Nome)
            .HasColumnName("nome")
            .HasMaxLength(200)
            .IsRequired();

        // FK to ConsultoriaFundo with Restrict delete (cross-aggregate)
        builder.Property(e => e.ConsultoriaFundoId)
            .HasColumnName("consultoria_fundo_id")
            .IsRequired();

        builder.HasOne<ConsultoriaFundo>()
            .WithMany()
            .HasForeignKey(e => e.ConsultoriaFundoId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to Custodiante with Restrict delete (cross-aggregate)
        builder.Property(e => e.CustodianteId)
            .HasColumnName("custodiante_id")
            .IsRequired();

        builder.HasOne<Custodiante>()
            .WithMany()
            .HasForeignKey(e => e.CustodianteId)
            .OnDelete(DeleteBehavior.Restrict);

        // TipoFundo stored as int (enum as int per pattern)
        builder.Property(e => e.TipoFundo)
            .HasColumnName("tipo_fundo")
            .IsRequired();

        builder.Property(e => e.ClasseAnbima)
            .HasColumnName("classe_anbima")
            .HasMaxLength(100);

        builder.Property(e => e.Segmento)
            .HasColumnName("segmento")
            .HasMaxLength(100);

        builder.Property(e => e.DataConstituicao)
            .HasColumnName("data_constituicao");

        // FundoStatus stored as int (D-02, D-07)
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // HasQueryFilter — company isolation (D-01, TEN-01)
        builder.HasQueryFilter(e => e.ClienteId == _currentCompanyService.CompanyId);

        // Unique CNPJ per company per CAD-12 (scoped via HasQueryFilter)
        builder.HasIndex(e => e.Cnpj)
            .IsUnique()
            .HasFilter("cnpj IS NOT NULL")
            .HasDatabaseName("IX_fundos_cnpj");

        builder.HasIndex(e => e.ClienteId)
            .HasDatabaseName("IX_fundos_cliente_id");

        builder.HasIndex(e => e.ConsultoriaFundoId)
            .HasDatabaseName("IX_fundos_consultoria_fundo_id");

        builder.HasIndex(e => e.CustodianteId)
            .HasDatabaseName("IX_fundos_custodiante_id");

        // === OwnsMany: FundoCedente (D-08, D-15) ===
        builder.OwnsMany(f => f.Cedentes, fc =>
        {
            fc.ToTable("fundo_cedentes");
            fc.HasKey(fc => fc.Id);

            fc.Property(fc => fc.FundoId)
                .HasColumnName("fundo_id")
                .IsRequired();

            fc.Property(fc => fc.CedenteId)
                .HasColumnName("cedente_id")
                .IsRequired();

            // D-16: LimiteExposicaoPercentual HasPrecision(5,2)
            fc.Property(fc => fc.LimiteExposicaoPercentual)
                .HasColumnName("limite_exposicao_percentual")
                .HasConversion(
                    vo => vo.Value,
                    s => LimiteExposicaoPercentual.Create(s))
                .HasPrecision(5, 2)
                .IsRequired();

            // D-16: LimiteExposicaoValor HasPrecision(18,4)
            fc.Property(fc => fc.LimiteExposicaoValor)
                .HasColumnName("limite_exposicao_valor")
                .HasPrecision(18, 4);

            fc.Property(fc => fc.DataInicio)
                .HasColumnName("data_inicio")
                .IsRequired();

            fc.Property(fc => fc.DataFim)
                .HasColumnName("data_fim");

            // FundoCedenteStatus stored as int
            fc.Property(fc => fc.Status)
                .HasColumnName("status")
                .IsRequired();

            // FK to Cedente with Restrict delete (cross-aggregate)
            fc.HasOne<Cedente>()
                .WithMany()
                .HasForeignKey(fc => fc.CedenteId)
                .OnDelete(DeleteBehavior.Restrict);

            // D-11: Partial unique index — at most one ATIVO association per Fundo-Cedente pair
            fc.HasIndex(fc => new { fc.FundoId, fc.CedenteId })
                .IsUnique()
                .HasFilter("status = 1")
                .HasDatabaseName("IX_fundo_cedentes_fundo_id_cedente_id_active");
        });

        // === OwnsMany: FundoTipoAtivo (D-15) ===
        builder.OwnsMany(f => f.TiposAtivo, fta =>
        {
            fta.ToTable("fundo_tipos_ativo");
            fta.HasKey(fta => fta.Id);

            fta.Property(fta => fta.FundoId)
                .HasColumnName("fundo_id")
                .IsRequired();

            fta.Property(fta => fta.TipoAtivoId)
                .HasColumnName("tipo_ativo_id")
                .IsRequired();

            // FK to TipoAtivo with Restrict delete (cross-aggregate)
            fta.HasOne<TipoAtivo>()
                .WithMany()
                .HasForeignKey(fta => fta.TipoAtivoId)
                .OnDelete(DeleteBehavior.Restrict);

            fta.HasIndex(fta => new { fta.FundoId, fta.TipoAtivoId })
                .IsUnique()
                .HasDatabaseName("IX_fundo_tipos_ativo_fundo_id_tipo_ativo_id");
        });
    }
}