using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.CedenteTipoAtivoAggregate;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;

namespace Onboarding.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for CedenteTipoAtivoAggregate — standalone relationship aggregate (D-21, Phase 50).
/// NO HasQueryFilter — tenant scoping via parent Cedente (D-5).
/// Partial unique index: (CedenteId, TipoAtivoId) WHERE Status='ATIVO' for uniformity with D-21.
/// </summary>
public sealed class CedenteTipoAtivoAggregateConfiguration : IEntityTypeConfiguration<CedenteTipoAtivoAggregate>
{
    public void Configure(EntityTypeBuilder<CedenteTipoAtivoAggregate> builder)
    {
        builder.ToTable("rel_cedente_tipo_ativo");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CedenteId)
            .HasColumnName("cedente_id")
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

        // FK to Cedente — RESTRICT
        builder.HasOne<Cedente>()
            .WithMany()
            .HasForeignKey(e => e.CedenteId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to TipoAtivo — RESTRICT
        builder.HasOne<TipoAtivo>()
            .WithMany()
            .HasForeignKey(e => e.TipoAtivoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Performance indexes
        builder.HasIndex(e => e.CedenteId)
            .HasDatabaseName("IX_rel_cedente_tipo_ativo_cedente_id");

        builder.HasIndex(e => e.TipoAtivoId)
            .HasDatabaseName("IX_rel_cedente_tipo_ativo_tipo_ativo_id");

        // Partial unique index: at most one ATIVO per (CedenteId, TipoAtivoId) — D-21 uniformity
        builder.HasIndex(e => new { e.CedenteId, e.TipoAtivoId })
            .IsUnique()
            .HasFilter("status = 'ATIVO'")
            .HasDatabaseName("IX_rel_cedente_tipo_ativo_active");
    }
}
