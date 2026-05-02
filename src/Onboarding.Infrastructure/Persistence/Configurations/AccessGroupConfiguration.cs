using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.EmployeeAggregate;

namespace Onboarding.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for AccessGroup entity (D-06, D-17).
/// HasQueryFilter ensures company isolation — access groups from other companies are never returned.
/// </summary>
public sealed class AccessGroupConfiguration : IEntityTypeConfiguration<AccessGroup>
{
    private readonly ICurrentCompanyService _currentCompanyService;

    public AccessGroupConfiguration(ICurrentCompanyService currentCompanyService)
    {
        _currentCompanyService = currentCompanyService;
    }

    public void Configure(EntityTypeBuilder<AccessGroup> builder)
    {
        builder.ToTable("access_groups");
        builder.HasKey(a => a.Id);

        // Company FK — no navigation property
        builder.Property(a => a.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.HasOne<Onboarding.Domain.Aggregates.CompanyAggregate.Company>()
            .WithMany()
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        // Permissions stored as comma-separated string (D-07)
        builder.Property(a => a.Permissions)
            .HasColumnName("permissions")
            .HasConversion(
                v => string.Join(",", v),
                s => s.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // HasQueryFilter — company isolation (D-17, T-37-03-02)
        builder.HasQueryFilter(a => a.CompanyId == _currentCompanyService.CompanyId);

        // Unique composite index — prevents duplicate group names within same company
        builder.HasIndex(a => new { a.CompanyId, a.Name })
            .IsUnique()
            .HasDatabaseName("IX_access_groups_company_id_name");

        builder.HasIndex(a => a.CompanyId)
            .HasDatabaseName("IX_access_groups_company_id");
    }
}