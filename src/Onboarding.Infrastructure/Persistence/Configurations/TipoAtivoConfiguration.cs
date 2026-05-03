using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;

namespace Onboarding.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for TipoAtivo aggregate (D-03/TEN-03, CAD-22).
/// NO HasQueryFilter — global entity shared across all companies.
/// NO ICurrentCompanyService — no company scope.
/// Unique Codigo constraint enforced globally (CAD-22).
/// </summary>
public sealed class TipoAtivoConfiguration : IEntityTypeConfiguration<TipoAtivo>
{
    public void Configure(EntityTypeBuilder<TipoAtivo> builder)
    {
        builder.ToTable("tipos_ativo");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(500)
            .IsRequired();

        // TipoAtivoCategoria stored as int (enum as int per pattern)
        builder.Property(e => e.Categoria)
            .HasColumnName("categoria")
            .IsRequired();

        builder.Property(e => e.Subcategoria)
            .HasColumnName("subcategoria")
            .HasMaxLength(200);

        // TipoAtivoStatus stored as int
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(e => e.OrdemExibicao)
            .HasColumnName("ordem_exibicao")
            .IsRequired();

        // Unique Codigo globally per CAD-22 (NO HasFilter — no company scope)
        builder.HasIndex(e => e.Codigo)
            .IsUnique()
            .HasDatabaseName("IX_tipos_ativo_codigo");
    }
}