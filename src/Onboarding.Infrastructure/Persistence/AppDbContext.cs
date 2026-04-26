using Microsoft.EntityFrameworkCore;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Aggregates.PasswordReset;
using Onboarding.Infrastructure.Persistence.Configurations;

namespace Onboarding.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly ICurrentCompanyService _currentCompanyService;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentCompanyService currentCompanyService) : base(options)
    {
        _currentCompanyService = currentCompanyService;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AccessGroup> AccessGroups => Set<AccessGroup>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CompanyConfiguration(_currentCompanyService));
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration(_currentCompanyService));
        modelBuilder.ApplyConfiguration(new AccessGroupConfiguration(_currentCompanyService));
        modelBuilder.ApplyConfiguration(new PasswordResetTokenConfiguration());
        modelBuilder.ApplyConfiguration(new AdminAuditLogConfiguration());
    }
}