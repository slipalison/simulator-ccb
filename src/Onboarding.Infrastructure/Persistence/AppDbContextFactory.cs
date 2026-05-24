using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Onboarding.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core CLI tools (migrations).
/// Not used at runtime — runtime DbContext is configured via DI in Program.cs.
/// Provides a default CurrentCompanyService for migration generation (HasQueryFilter is ignored during migrations).
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Use actual dev password — matches .env APP_DB_PASSWORD
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=onboarding;Username=appuser;Password=dev_app_pass_2026");

        return new AppDbContext(optionsBuilder.Options, new CurrentCompanyService());
    }
}
