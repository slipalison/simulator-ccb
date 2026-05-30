using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Infrastructure.Repositories;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Repositories;

/// <summary>
/// InMemory unit tests for CompanyRepository.
/// Covers: AddAsync, SaveAsync, GetByIdAsync, GetByIdsAsync, ExistsByCnpjAsync,
///         ExistsByEmailAsync, DeleteAsync, GetByKeycloakSubAsync, GetByEmailAsync.
/// Limited fidelity:
///   - GetPagedAsync with search uses ToLower().Contains() — works in InMemory (LINQ-evaluated, not SQL).
///   - GetByEmailAsync uses FirstOrDefaultAsync — works in InMemory.
///   - HasQueryFilter (IsDeleted=false) is enforced in InMemory via the configured predicate.
/// Integration.Tests is the primary coverage for correct Npgsql SQL translation.
/// </summary>
public sealed class CompanyRepositoryTests
{
    private static Company CreateCompany(
        string razaoSocial = "Empresa Teste Ltda",
        string cnpj = "11222333000181",
        string email = "contato@empresa.com",
        string phone = "+5511999990000")
        => Company.Register(razaoSocial, cnpj, email, phone,
            TermsAcceptance.Create("1.0", "127.0.0.1"));

    [Fact]
    public async Task AddAsync_ValidCompany_PersistsEntity()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);
        var company = CreateCompany();

        await repo.AddAsync(company);

        var found = await repo.GetByIdAsync(company.Id);
        found.ShouldNotBeNull();
        found!.RazaoSocial.ShouldBe("Empresa Teste Ltda");
    }

    [Fact]
    public async Task SaveAsync_UpdatesCompanyFields()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);
        var company = CreateCompany();
        await repo.AddAsync(company);

        company.SetKeycloakUserId("kc-sub-abc");
        await repo.SaveAsync(company);

        var updated = await repo.GetByIdAsync(company.Id);
        updated.ShouldNotBeNull();
        updated!.KeycloakUserId.ShouldBe("kc-sub-abc");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdsAsync_EmptyList_ReturnsEmpty()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);

        var result = await repo.GetByIdsAsync([]);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByIdsAsync_ValidIds_ReturnsMatchingCompanies()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);
        var c1 = CreateCompany("C1", "11222333000181", "c1@t.com");
        var c2 = CreateCompany("C2", "11222333000181", "c2@t.com");
        var c3 = CreateCompany("C3", "11222333000181", "c3@t.com");
        await repo.AddAsync(c1);
        await repo.AddAsync(c2);
        await repo.AddAsync(c3);

        var result = await repo.GetByIdsAsync([c1.Id, c3.Id]);

        result.Count.ShouldBe(2);
        result.ShouldContain(c => c.Id == c1.Id);
        result.ShouldContain(c => c.Id == c3.Id);
    }

    [Fact]
    public async Task ExistsByCnpjAsync_ExistingCnpj_ReturnsTrue()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);
        var company = CreateCompany(cnpj: "11222333000181");
        await repo.AddAsync(company);

        var exists = await repo.ExistsByCnpjAsync("11222333000181");

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByCnpjAsync_MissingCnpj_ReturnsFalse()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);

        var exists = await repo.ExistsByCnpjAsync("62232889000190");

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsByEmailAsync_ExistingEmail_ReturnsTrue()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);
        var company = CreateCompany(email: "unique@empresa.com");
        await repo.AddAsync(company);

        var exists = await repo.ExistsByEmailAsync("unique@empresa.com");

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByEmailAsync_MissingEmail_ReturnsFalse()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);

        var exists = await repo.ExistsByEmailAsync("nobody@empresa.com");

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ExistingCompany_RemovesIt()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);
        var company = CreateCompany();
        await repo.AddAsync(company);

        await repo.DeleteAsync(company.Id);

        var found = await repo.GetByIdAsync(company.Id);
        found.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNotThrow()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);

        await Should.NotThrowAsync(() => repo.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByKeycloakSubAsync_MatchingKeycloakSub_ReturnsCompany()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);
        var company = CreateCompany();
        company.SetKeycloakUserId("kc-abc-def");
        await repo.AddAsync(company);

        var found = await repo.GetByKeycloakSubAsync("kc-abc-def");

        found.ShouldNotBeNull();
        found!.KeycloakUserId.ShouldBe("kc-abc-def");
    }

    [Fact]
    public async Task GetByKeycloakSubAsync_NoMatch_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);

        var found = await repo.GetByKeycloakSubAsync("non-existent-sub");

        found.ShouldBeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsCompany()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);
        var company = CreateCompany(email: "search@empresa.com");
        await repo.AddAsync(company);

        var found = await repo.GetByEmailAsync("search@empresa.com");

        found.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_NoMatch_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new CompanyRepository(db);

        var found = await repo.GetByEmailAsync("nobody@empresa.com");

        found.ShouldBeNull();
    }

    // NOTE: GetPagedAsync with status filter uses c.IsDeleted (computed property, not mapped)
    // which InMemory cannot translate. These paths are covered by Integration.Tests (Testcontainers).
    // GetPagedAsync with search uses c.RazaoSocial.ToLower().Contains() which also is not translatable
    // by InMemory. Both paths are Integration-only.
    // The null-status, null-search path is also excluded because it uses TermsAcceptance.AcceptedAt
    // in OrderByDescending — owned type ordering is verified in Integration.Tests.
}
