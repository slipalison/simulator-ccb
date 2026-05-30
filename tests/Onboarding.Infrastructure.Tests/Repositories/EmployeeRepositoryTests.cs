using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Infrastructure.Repositories;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Repositories;

/// <summary>
/// InMemory unit tests for EmployeeRepository.
/// Covers: AddAsync, SaveAsync, GetByIdAsync, GetByKeycloakSubAsync,
///         ExistsByCpfAsync, ExistsByEmailAsync, GetByIdIgnoreFilterAsync,
///         DeleteAsync, ExistsByAccessGroupIdAsync,
///         GetPagedByCompanyAsync (no search), GetPagedAllAsync (no search).
/// Limited fidelity:
///   - EF.Functions.ILike in search paths → Integration-only.
///   - IgnoreQueryFilters bypasses HasQueryFilter; InMemory applies the query filter if
///     CompanyId is set — IgnoreQueryFilters bypasses it so cross-company reads work.
///   - GetPagedByCompanyAsync/GetPagedAllAsync ILike search paths → Integration-only.
/// </summary>
public sealed class EmployeeRepositoryTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid AccessGroupId = Guid.NewGuid();

    private static Employee CreateEmployee(
        Guid? companyId = null,
        Guid? accessGroupId = null,
        string? email = null,
        string? cpf = null,
        string? nome = null)
        => Employee.Register(
            nome ?? "Funcionario Teste",
            cpf ?? "52998224725",
            email ?? $"emp-{Guid.NewGuid():N}@t.com",
            "+5511999990000",
            companyId ?? CompanyId,
            accessGroupId ?? AccessGroupId);

    [Fact]
    public async Task AddAsync_ValidEmployee_PersistsEntity()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);
        var emp = CreateEmployee(nome: "Alice");

        await repo.AddAsync(emp);

        var found = await repo.GetByIdAsync(emp.Id);
        found.ShouldNotBeNull();
        found!.Nome.ShouldBe("Alice");
    }

    [Fact]
    public async Task SaveAsync_UpdatesEmployee()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);
        var emp = CreateEmployee();
        await repo.AddAsync(emp);

        emp.SetKeycloakUserId("kc-emp-sub");
        await repo.SaveAsync(emp);

        var updated = await repo.GetByIdAsync(emp.Id);
        updated.ShouldNotBeNull();
        updated!.KeycloakUserId.ShouldBe("kc-emp-sub");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByKeycloakSubAsync_MatchingSub_ReturnsEmployee()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);
        var emp = CreateEmployee();
        emp.SetKeycloakUserId("kc-test-sub");
        await repo.AddAsync(emp);

        var found = await repo.GetByKeycloakSubAsync("kc-test-sub");

        found.ShouldNotBeNull();
        found!.KeycloakUserId.ShouldBe("kc-test-sub");
    }

    [Fact]
    public async Task GetByKeycloakSubAsync_NoMatch_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);

        var result = await repo.GetByKeycloakSubAsync("no-such-sub");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsByCpfAsync_ExistingCpf_ReturnsTrue()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);
        await repo.AddAsync(CreateEmployee(cpf: "52998224725"));

        var exists = await repo.ExistsByCpfAsync("52998224725");

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByCpfAsync_NotFound_ReturnsFalse()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);

        var exists = await repo.ExistsByCpfAsync("71428794085"); // valid CPF not stored

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsByEmailAsync_ExistingEmail_ReturnsTrue()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);
        await repo.AddAsync(CreateEmployee(email: "unique@emp.com"));

        var exists = await repo.ExistsByEmailAsync("unique@emp.com");

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByEmailAsync_NotFound_ReturnsFalse()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);

        var exists = await repo.ExistsByEmailAsync("nobody@emp.com");

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdIgnoreFilterAsync_AcrossCompanies_ReturnsEmployee()
    {
        // Create employee in a different company — IgnoreQueryFilters must return it
        var otherCompanyId = Guid.NewGuid();
        await using var db = InMemoryDbContextFactory.Create(CompanyId); // context scoped to CompanyId
        var repo = new EmployeeRepository(db);
        var emp = CreateEmployee(companyId: otherCompanyId);
        // Insert directly to bypass query filter
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        // GetByIdIgnoreFilterAsync bypasses HasQueryFilter
        var found = await repo.GetByIdIgnoreFilterAsync(emp.Id);

        found.ShouldNotBeNull();
        found!.CompanyId.ShouldBe(otherCompanyId);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEmployee_RemovesIt()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);
        var emp = CreateEmployee();
        await repo.AddAsync(emp);

        await repo.DeleteAsync(emp.Id);

        var found = await repo.GetByIdAsync(emp.Id);
        found.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNotThrow()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);

        await Should.NotThrowAsync(() => repo.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ExistsByAccessGroupIdAsync_HasEmployeesInGroup_ReturnsTrue()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);
        var agId = Guid.NewGuid();
        await repo.AddAsync(CreateEmployee(accessGroupId: agId));

        var exists = await repo.ExistsByAccessGroupIdAsync(agId);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByAccessGroupIdAsync_NoEmployeesInGroup_ReturnsFalse()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);

        var exists = await repo.ExistsByAccessGroupIdAsync(Guid.NewGuid());

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetPagedByCompanyAsync_DefaultStatus_ReturnsOnlyActiveEmployees()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);
        var active = CreateEmployee(email: "active@emp.com");
        var deleted = CreateEmployee(email: "del@emp.com");
        deleted.Anonymize(); // marks DeletedAt
        db.Employees.AddRange(active, deleted);
        await db.SaveChangesAsync();

        // null status → default: active only
        var (items, total) = await repo.GetPagedByCompanyAsync(CompanyId, 1, 10, null, null);

        total.ShouldBe(1);
        items.ShouldAllBe(e => !e.IsDeleted);
    }

    [Fact]
    public async Task GetPagedByCompanyAsync_StatusDeleted_ReturnsDeletedEmployees()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);
        var active = CreateEmployee(email: "act@emp.com");
        var deleted = CreateEmployee(email: "d@emp.com");
        deleted.Anonymize();
        db.Employees.AddRange(active, deleted);
        await db.SaveChangesAsync();

        var (items, total) = await repo.GetPagedByCompanyAsync(CompanyId, 1, 10, null, "deleted");

        total.ShouldBe(1);
        items.ShouldAllBe(e => e.IsDeleted);
    }

    [Fact]
    public async Task GetPagedByCompanyAsync_StatusActive_ReturnsOnlyActiveEmployees()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);
        var active = CreateEmployee(email: "a2@emp.com");
        var deleted = CreateEmployee(email: "d2@emp.com");
        deleted.Anonymize();
        db.Employees.AddRange(active, deleted);
        await db.SaveChangesAsync();

        var (items, total) = await repo.GetPagedByCompanyAsync(CompanyId, 1, 10, null, "active");

        total.ShouldBe(1);
        items.ShouldAllBe(e => !e.IsDeleted);
    }

    [Fact]
    public async Task GetPagedByCompanyAsync_Page2_ReturnsRemainingItems()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);

        for (var i = 0; i < 5; i++)
        {
            db.Employees.Add(CreateEmployee(email: $"e{i}@emp.com"));
        }
        await db.SaveChangesAsync();

        var (items, total) = await repo.GetPagedByCompanyAsync(CompanyId, 2, 3, null, "active");

        total.ShouldBe(5);
        items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetPagedAllAsync_DefaultStatus_ReturnsActiveEmployeesAcrossCompanies()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);
        var emp1 = CreateEmployee(email: "all1@emp.com");
        var emp2 = CreateEmployee(companyId: Guid.NewGuid(), email: "all2@emp.com"); // different company
        var deleted = CreateEmployee(email: "del3@emp.com");
        deleted.Anonymize();
        db.Employees.AddRange(emp1, emp2, deleted);
        await db.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAllAsync(1, 10, null, null);

        // 2 active employees across companies
        total.ShouldBe(2);
        items.ShouldAllBe(e => !e.IsDeleted);
    }

    [Fact]
    public async Task GetPagedAllAsync_StatusDeleted_ReturnsDeletedEmployees()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new EmployeeRepository(db);
        var emp = CreateEmployee(email: "active5@emp.com");
        var del = CreateEmployee(email: "del5@emp.com");
        del.Anonymize();
        db.Employees.AddRange(emp, del);
        await db.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAllAsync(1, 10, null, "deleted");

        total.ShouldBe(1);
        items.ShouldAllBe(e => e.IsDeleted);
    }
}
