using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Infrastructure.Repositories;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Repositories;

/// <summary>
/// InMemory unit tests for AdminRepository.
/// Covers: GetByIdAsync, UpdateAsync + SaveChangesAsync, GetPagedAsync (status + search filters).
/// Limited fidelity: ApplySearch uses EF.Functions.ILike which throws in InMemory.
/// Search-path tests are documented as Integration-only.
/// Covers: no-search path, status filter "deleted", status filter active (default), GetByIdAsync.
/// </summary>
public sealed class AdminRepositoryTests
{
    private static Company CreateCompany(
        string razaoSocial = "Empresa A",
        string cnpj = "11222333000181",
        string email = "a@empresa.com")
        => Company.Register(razaoSocial, cnpj, email, "+5511900000000",
            TermsAcceptance.Create("1.0", "10.0.0.1"));

    [Fact]
    public async Task GetByIdAsync_ExistingCompany_ReturnsCompany()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminRepository(db);
        var company = CreateCompany();
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var result = await repo.GetByIdAsync(company.Id);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(company.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_AndSaveChangesAsync_PersistsChanges()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminRepository(db);
        var company = CreateCompany();
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        company.SetKeycloakUserId("kc-admin-sub");
        await repo.UpdateAsync(company);
        await repo.SaveChangesAsync();

        var updated = await repo.GetByIdAsync(company.Id);
        updated.ShouldNotBeNull();
        updated!.KeycloakUserId.ShouldBe("kc-admin-sub");
    }

    [Fact]
    public async Task GetPagedAsync_NoFilters_ReturnsActiveCompanies()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminRepository(db);
        var c1 = CreateCompany("Beta", "11222333000181", "b@t.com");
        var c2 = CreateCompany("Alpha", "11222333000181", "a@t.com");
        var deleted = CreateCompany("Deleted", "11222333000181", "d@t.com");
        deleted.Anonymize();
        db.Companies.AddRange(c1, c2, deleted);
        await db.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAsync(1, 10, null, null);

        // Default: excludes deleted
        total.ShouldBe(2);
        items.ShouldAllBe(c => !c.IsDeleted);
    }

    [Fact]
    public async Task GetPagedAsync_StatusDeleted_ReturnsDeletedCompanies()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminRepository(db);
        var active = CreateCompany("Active", "11222333000181", "ac@t.com");
        var deleted = CreateCompany("Deleted", "11222333000181", "dl@t.com");
        deleted.Anonymize();
        db.Companies.AddRange(active, deleted);
        await db.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAsync(1, 10, null, "deleted");

        total.ShouldBe(1);
        items.ShouldAllBe(c => c.IsDeleted);
    }

    [Fact]
    public async Task GetPagedAsync_PaginationPage2_ReturnsRemainingItems()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminRepository(db);
        for (var i = 0; i < 5; i++)
        {
            db.Companies.Add(CreateCompany($"Company{i}", "11222333000181", $"c{i}@t.com"));
        }
        await db.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAsync(2, 3, null, null);

        total.ShouldBe(5);
        items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetPagedAsync_EmptyDatabase_ReturnsEmptyResult()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminRepository(db);

        var (items, total) = await repo.GetPagedAsync(1, 10, null, null);

        total.ShouldBe(0);
        items.ShouldBeEmpty();
    }
}
