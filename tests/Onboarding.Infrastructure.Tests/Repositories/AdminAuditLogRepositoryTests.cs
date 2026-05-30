using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Infrastructure.Repositories;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Repositories;

/// <summary>
/// InMemory unit tests for AdminAuditLogRepository.
/// Covers: AddAsync + SaveChangesAsync, GetPagedAsync (no-filter and with-filter paths).
/// Limited fidelity: date-range, actionType, entityId filters work fine in InMemory.
/// </summary>
public sealed class AdminAuditLogRepositoryTests
{
    private static AdminAuditLog CreateLog(
        ActionType action = ActionType.UserCreated,
        string? entityType = null,
        Guid? entityId = null)
        => AdminAuditLog.Create(
            adminUserId: Guid.NewGuid(),
            adminUserName: "admin@test.com",
            action: action,
            entityType: entityType,
            entityId: entityId);

    [Fact]
    public async Task AddAsync_AndSaveChangesAsync_PersistsLog()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminAuditLogRepository(db);
        var log = CreateLog();

        await repo.AddAsync(log);
        await repo.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAsync(1, 10);
        total.ShouldBe(1);
        items[0].AdminUserName.ShouldBe("admin@test.com");
    }

    [Fact]
    public async Task GetPagedAsync_NoFilters_ReturnsAllLogs()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminAuditLogRepository(db);

        for (var i = 0; i < 5; i++)
        {
            await repo.AddAsync(CreateLog());
        }
        await repo.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAsync(1, 10);
        total.ShouldBe(5);
        items.Count.ShouldBe(5);
    }

    [Fact]
    public async Task GetPagedAsync_WithActionTypeFilter_ReturnsMatchingLogs()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminAuditLogRepository(db);
        await repo.AddAsync(CreateLog(ActionType.UserCreated));
        await repo.AddAsync(CreateLog(ActionType.UserBlocked));
        await repo.AddAsync(CreateLog(ActionType.UserBlocked));
        await repo.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAsync(1, 10, actionType: ActionType.UserBlocked);
        total.ShouldBe(2);
        items.ShouldAllBe(l => l.ActionType == ActionType.UserBlocked);
    }

    [Fact]
    public async Task GetPagedAsync_WithAdminUserNameFilter_ReturnsMatchingLogs()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminAuditLogRepository(db);

        await repo.AddAsync(AdminAuditLog.Create(Guid.NewGuid(), "alice@t.com", ActionType.UserCreated));
        await repo.AddAsync(AdminAuditLog.Create(Guid.NewGuid(), "bob@t.com", ActionType.UserCreated));
        await repo.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAsync(1, 10, adminUserName: "alice");
        total.ShouldBe(1);
        items[0].AdminUserName.ShouldBe("alice@t.com");
    }

    [Fact]
    public async Task GetPagedAsync_WithEntityTypeFilter_ReturnsMatchingLogs()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminAuditLogRepository(db);
        await repo.AddAsync(CreateLog(entityType: "Fundo"));
        await repo.AddAsync(CreateLog(entityType: "Cedente"));
        await repo.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAsync(1, 10, entityType: "Fundo");
        total.ShouldBe(1);
        items[0].EntityType.ShouldBe("Fundo");
    }

    [Fact]
    public async Task GetPagedAsync_WithEntityIdFilter_ReturnsMatchingLog()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminAuditLogRepository(db);
        var targetId = Guid.NewGuid();
        await repo.AddAsync(CreateLog(entityId: targetId));
        await repo.AddAsync(CreateLog(entityId: Guid.NewGuid()));
        await repo.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAsync(1, 10, entityId: targetId);
        total.ShouldBe(1);
        items[0].EntityId.ShouldBe(targetId);
    }

    [Fact]
    public async Task GetPagedAsync_WithDateRange_ReturnsLogsWithinRange()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminAuditLogRepository(db);
        await repo.AddAsync(CreateLog());
        await repo.SaveChangesAsync();

        var start = DateTimeOffset.UtcNow.AddHours(-1);
        var end = DateTimeOffset.UtcNow.AddHours(1);

        var (items, total) = await repo.GetPagedAsync(1, 10, startDate: start, endDate: end);

        total.ShouldBe(1);
    }

    [Fact]
    public async Task GetPagedAsync_PaginationSecondPage_ReturnsRemainingItems()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new AdminAuditLogRepository(db);

        for (var i = 0; i < 5; i++)
        {
            await repo.AddAsync(CreateLog());
        }
        await repo.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAsync(2, 3);

        total.ShouldBe(5);
        items.Count.ShouldBe(2);
    }
}
