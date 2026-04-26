using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for Employee aggregate (D-02, D-17).
/// HasQueryFilter ensures company isolation — employees from other companies are never returned.
/// </summary>
public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    private readonly ICurrentCompanyService _currentCompanyService;

    public EmployeeConfiguration(ICurrentCompanyService currentCompanyService)
    {
        _currentCompanyService = currentCompanyService;
    }

    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Nome)
            .HasColumnName("nome")
            .HasMaxLength(200)
            .IsRequired();

        // Value object mapping: Cpf → string (max 11 digits)
        // Nullable — set to null! during LGPD Anonymize()
        builder.Property(e => e.Cpf)
            .HasColumnName("cpf")
            .HasConversion(
                vo => vo == null ? null : vo.Value,
                s => s == null ? null! : Cpf.Create(s))
            .HasMaxLength(11)
            .IsRequired(false);

        // Value object mapping: Email → string
        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasConversion(
                vo => vo.Value,
                s => Email.Create(s))
            .HasMaxLength(320)
            .IsRequired();

        // Value object mapping: PhoneNumber → string
        builder.Property(e => e.Phone)
            .HasColumnName("phone")
            .HasConversion(
                vo => vo.Value,
                s => PhoneNumber.Create(s))
            .HasMaxLength(20);

        // Company FK — no navigation property (D-02)
        builder.Property(e => e.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.HasOne<Onboarding.Domain.Aggregates.CompanyAggregate.Company>()
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // AccessGroup FK
        builder.Property(e => e.AccessGroupId)
            .HasColumnName("access_group_id")
            .IsRequired();

        builder.HasOne<AccessGroup>()
            .WithMany()
            .HasForeignKey(e => e.AccessGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Keycloak user ID (nullable)
        builder.Property(e => e.KeycloakUserId)
            .HasColumnName("keycloak_user_id")
            .HasMaxLength(36);

        // LGPD soft-delete
        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false);

        // HasQueryFilter — company isolation (D-17, T-37-03-01)
        builder.HasQueryFilter(e => e.CompanyId == _currentCompanyService.CompanyId);

        // Indexes
        builder.HasIndex(e => e.Cpf)
            .IsUnique()
            .HasFilter("cpf IS NOT NULL")
            .HasDatabaseName("IX_employees_cpf");

        builder.HasIndex(e => e.Email)
            .IsUnique()
            .HasDatabaseName("IX_employees_email");

        builder.HasIndex(e => e.CompanyId)
            .HasDatabaseName("IX_employees_company_id");

        builder.HasIndex(e => e.AccessGroupId)
            .HasDatabaseName("IX_employees_access_group_id");
    }
}