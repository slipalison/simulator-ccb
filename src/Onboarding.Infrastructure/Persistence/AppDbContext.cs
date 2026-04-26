using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.PasswordReset;
using Onboarding.Infrastructure.Persistence.Configurations;

namespace Onboarding.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new PasswordResetTokenConfiguration());
        modelBuilder.ApplyConfiguration(new AdminAuditLogConfiguration());
    }
}