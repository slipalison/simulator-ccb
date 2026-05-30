using Onboarding.Infrastructure.Persistence;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Persistence;

/// <summary>
/// Tests for AppDbContextFactory (design-time IDesignTimeDbContextFactory).
/// The factory is only used by EF Core CLI tools — its only job is to produce a valid
/// AppDbContext. We do NOT exercise the SQL connection (that is not testable without a DB).
/// We verify CreateDbContext returns a non-null context. InMemory is NOT used here because
/// the factory hardcodes UseNpgsql; the test only covers object construction paths.
/// </summary>
public sealed class AppDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_NoArgs_ReturnsNonNullAppDbContext()
    {
        // Arrange
        var factory = new AppDbContextFactory();

        // Act
        using var context = factory.CreateDbContext([]);

        // Assert
        context.ShouldNotBeNull();
    }

    [Fact]
    public void CreateDbContext_WithArgs_ReturnsNonNullAppDbContext()
    {
        // Arrange
        var factory = new AppDbContextFactory();

        // Act
        using var context = factory.CreateDbContext(["--verbose"]);

        // Assert
        context.ShouldNotBeNull();
    }
}
