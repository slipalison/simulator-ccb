using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;

namespace Onboarding.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for FundoTipoAtivoAggregate — standalone relationship aggregate (D-21, Phase 50).
/// NO HasQueryFilter — tenant scoping via parent Fundo (D-5).
/// Partial unique index: (FundoId, TipoAtivoId) WHERE Status='ATIVO' for uniformity with D-21.
/// </summary>
public sealed class FundoTipoAtivoAggregateConfiguration : IEntityTypeConfiguration<FundoTipoAtivoAggregate>
{
    public void Configure(EntityTypeBuilder<FundoTipoAtivoAggregate> builder)
    {
        builder.ToTable("rel_fundo_tipo_ativo");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FundoId)
            .HasColumnName("fundo_id")
            .IsRequired();

        builder.Property(e => e.TipoAtivoId)
            .HasColumnName("tipo_ativo_id")
            .IsRequired();

        // LimiteExposicao — owned columns
        builder.OwnsOne(e => e.Limite, lo =>
        {
            lo.Property(l => l.Percentual)
                .HasColumnName("limite_percentual")
                .HasPrecision(5, 2);

            lo.Property(l => l.Valor)
                .HasColumnName("limite_valor")
                .HasPrecision(18, 4);
        });

        // JanelaVigencia — owned columns (D-20)
        builder.OwnsOne(e => e.Janela, jo =>
        {
            jo.Property(j => j.DataInicio)
                .HasColumnName("data_inicio")
                .IsRequired();

            jo.Property(j => j.DataFim)
                .HasColumnName("data_fim");
        });

        // Status stored as string for Postgres partial index
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // FK to Fundo — RESTRICT
        builder.HasOne<Fundo>()
            .WithMany()
            .HasForeignKey(e => e.FundoId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to TipoAtivo — RESTRICT
        builder.HasOne<TipoAtivo>()
            .WithMany()
            .HasForeignKey(e => e.TipoAtivoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Performance indexes
        builder.HasIndex(e => e.FundoId)
            .HasDatabaseName("IX_rel_fundo_tipo_ativo_fundo_id");

        builder.HasIndex(e => e.TipoAtivoId)
            .HasDatabaseName("IX_rel_fundo_tipo_ativo_tipo_ativo_id");

        // Partial unique index: at most one ATIVO per (FundoId, TipoAtivoId) — D-21 uniformity
        builder.HasIndex(e => new { e.FundoId, e.TipoAtivoId })
            .IsUnique()
            .HasFilter("status = 'ATIVO'")
            .HasDatabaseName("IX_rel_fundo_tipo_ativo_active");
    }
}
