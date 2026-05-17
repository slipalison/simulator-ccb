using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;

namespace Onboarding.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for FundoCedenteAggregate — standalone relationship aggregate (D-21, Phase 50).
/// NO HasQueryFilter — tenant scoping via parent Fundo/Cedente (D-5; aggregates already filtered).
/// Status stored as text to enable Postgres partial unique index with literal string filter (D-18).
/// Partial unique index: (FundoId, CedenteId) WHERE Status='ATIVO' (REL-09, D-18).
/// </summary>
public sealed class FundoCedenteAggregateConfiguration : IEntityTypeConfiguration<FundoCedenteAggregate>
{
    public void Configure(EntityTypeBuilder<FundoCedenteAggregate> builder)
    {
        builder.ToTable("rel_fundo_cedente");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FundoId)
            .HasColumnName("fundo_id")
            .IsRequired();

        builder.Property(e => e.CedenteId)
            .HasColumnName("cedente_id")
            .IsRequired();

        // LimiteExposicao — owned as separate columns (VO flattened)
        builder.OwnsOne(e => e.Limite, lo =>
        {
            lo.Property(l => l.Percentual)
                .HasColumnName("limite_percentual")
                .HasPrecision(5, 2);

            lo.Property(l => l.Valor)
                .HasColumnName("limite_valor")
                .HasPrecision(18, 4);
        });

        // JanelaVigencia — owned as separate columns (VO flattened, D-20)
        builder.OwnsOne(e => e.Janela, jo =>
        {
            jo.Property(j => j.DataInicio)
                .HasColumnName("data_inicio")
                .IsRequired();

            jo.Property(j => j.DataFim)
                .HasColumnName("data_fim");
        });

        // Status stored as string for Postgres partial index (D-18, D-19)
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // FK to Fundo — RESTRICT delete (cross-aggregate ref)
        builder.HasOne<Fundo>()
            .WithMany()
            .HasForeignKey(e => e.FundoId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to Cedente — RESTRICT delete (cross-aggregate ref)
        builder.HasOne<Cedente>()
            .WithMany()
            .HasForeignKey(e => e.CedenteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Performance indexes
        builder.HasIndex(e => e.FundoId)
            .HasDatabaseName("IX_rel_fundo_cedente_fundo_id");

        builder.HasIndex(e => e.CedenteId)
            .HasDatabaseName("IX_rel_fundo_cedente_cedente_id");

        // REL-09 partial unique index (D-18): at most one ATIVO per (FundoId, CedenteId)
        builder.HasIndex(e => new { e.FundoId, e.CedenteId })
            .IsUnique()
            .HasFilter("status = 'ATIVO'")
            .HasDatabaseName("IX_rel_fundo_cedente_active");
    }
}
