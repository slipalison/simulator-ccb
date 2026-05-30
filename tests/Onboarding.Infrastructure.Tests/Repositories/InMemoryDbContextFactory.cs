using Microsoft.EntityFrameworkCore;
using Onboarding.Application.Common;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Tests.Repositories;

/// <summary>
/// Helper for creating InMemory AppDbContext instances in unit tests.
/// Each call produces an isolated database with a unique name so tests do not bleed state.
/// NOTE: InMemory provider does NOT enforce provider-specific behaviors:
///   - HasFilter ("status = 'ATIVO'") partial unique indexes (REL-09) — not enforced.
///   - EF.Functions.ILike — throws NotSupportedException; search-path tests are Integration-only.
///   - Shadow properties (_db.Entry) for Cedente discriminated union — requires tracked entity
///     with real column storage, not supported by InMemory.
///   - IgnoreQueryFilters() works but HasQueryFilter is always satisfied since InMemory has no
///     real tenant predicate — all data visible unless filtered explicitly.
/// Primary coverage target: CRUD methods (Add/Save/GetById/Delete/ExistsBy).
/// Integration.Tests (Testcontainers) is the primary coverage for SQL-translated queries.
/// </summary>
internal static class InMemoryDbContextFactory
{
    internal static AppDbContext Create(Guid? companyId = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var companyService = new StubCurrentCompanyService(companyId ?? Guid.Empty);
        return new AppDbContext(options, companyService);
    }

    private sealed class StubCurrentCompanyService(Guid companyId) : ICurrentCompanyService
    {
        public Guid CompanyId { get; } = companyId;
    }
}
