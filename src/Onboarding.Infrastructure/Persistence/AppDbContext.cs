using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.Aggregates.PasswordReset;
using Onboarding.Infrastructure.Persistence.Configurations;

namespace Onboarding.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new PasswordResetTokenConfiguration());
    }
}
