using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Infrastructure.Repositories;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Repositories;

/// <summary>
/// InMemory unit tests for AccessGroupRepository.
/// Covers: AddAsync, AddRangeAsync, SaveAsync, GetByIdAsync,
///         GetByCompanyAndNameAsync, GetByCompanyIdAsync, GetByIdsAsync, DeleteAsync.
/// Limited fidelity: IgnoreQueryFilters is called but InMemory does not enforce HasQueryFilter anyway.
/// Integration.Tests (Testcontainers) is the primary coverage for query-filter isolation.
/// </summary>
public sealed class AccessGroupRepositoryTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    private static AccessGroup CreateGroup(string name, Guid? companyId = null)
        => AccessGroup.Create(companyId ?? CompanyId, name, ["employees:read"]);

    [Fact]
    public async Task AddAsync_ValidGroup_PersistsEntity()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new AccessGroupRepository(db);
        var group = CreateGroup("Admins");

        await repo.AddAsync(group);

        var found = await repo.GetByIdAsync(group.Id);
        found.ShouldNotBeNull();
        found!.Name.ShouldBe("Admins");
    }

    [Fact]
    public async Task AddRangeAsync_MultipleGroups_AllPersisted()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new AccessGroupRepository(db);
        var groups = AccessGroup.CreateDefaultGroups(CompanyId);

        await repo.AddRangeAsync(groups);

        var found = await repo.GetByCompanyIdAsync(CompanyId);
        found.Count.ShouldBe(3);
    }

    [Fact]
    public async Task SaveAsync_UpdatesPermissions()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new AccessGroupRepository(db);
        var group = CreateGroup("Viewers");
        await repo.AddAsync(group);

        group.UpdatePermissions(["funds:read", "audit:read"]);
        await repo.SaveAsync(group);

        var found = await repo.GetByIdAsync(group.Id);
        found.ShouldNotBeNull();
        found!.Permissions.ShouldContain("funds:read");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AccessGroupRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByCompanyAndNameAsync_Found_ReturnsGroup()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new AccessGroupRepository(db);
        var group = CreateGroup("Managers");
        await repo.AddAsync(group);

        var found = await repo.GetByCompanyAndNameAsync(CompanyId, "Managers");

        found.ShouldNotBeNull();
        found!.Name.ShouldBe("Managers");
    }

    [Fact]
    public async Task GetByCompanyAndNameAsync_NotFound_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new AccessGroupRepository(db);

        var found = await repo.GetByCompanyAndNameAsync(CompanyId, "DoesNotExist");

        found.ShouldBeNull();
    }

    [Fact]
    public async Task GetByCompanyIdAsync_ReturnsGroupsForCompanyOrderedByName()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new AccessGroupRepository(db);
        await repo.AddAsync(CreateGroup("Zebra"));
        await repo.AddAsync(CreateGroup("Alpha"));

        var result = await repo.GetByCompanyIdAsync(CompanyId);

        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Alpha");
        result[1].Name.ShouldBe("Zebra");
    }

    [Fact]
    public async Task GetByIdsAsync_EmptyList_ReturnsEmpty()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new AccessGroupRepository(db);

        var result = await repo.GetByIdsAsync([]);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByIdsAsync_ValidIds_ReturnsMatchingGroups()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new AccessGroupRepository(db);
        var g1 = CreateGroup("G1");
        var g2 = CreateGroup("G2");
        var g3 = CreateGroup("G3");
        await repo.AddRangeAsync([g1, g2, g3]);

        var result = await repo.GetByIdsAsync([g1.Id, g3.Id]);

        result.Count.ShouldBe(2);
        result.ShouldContain(r => r.Id == g1.Id);
        result.ShouldContain(r => r.Id == g3.Id);
    }

    [Fact]
    public async Task DeleteAsync_ExistingGroup_RemovesIt()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new AccessGroupRepository(db);
        var group = CreateGroup("ToDelete");
        await repo.AddAsync(group);

        await repo.DeleteAsync(group.Id);

        var found = await repo.GetByIdAsync(group.Id);
        found.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNotThrow()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new AccessGroupRepository(db);

        await Should.NotThrowAsync(() => repo.DeleteAsync(Guid.NewGuid()));
    }
}
