using NSubstitute;
using Onboarding.Application.Admin.Queries;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Commands;

[Trait("Category", "Unit")]
public class GetAuditLogQueryHandlerTests
{
    private readonly IAdminAuditLogRepository _repository;
    private readonly GetAuditLogQueryHandler _sut;

    public GetAuditLogQueryHandlerTests()
    {
        _repository = Substitute.For<IAdminAuditLogRepository>();
        _sut = new GetAuditLogQueryHandler(_repository);
    }

    private static IReadOnlyList<AdminAuditLog> CreateAuditLogs() => new List<AdminAuditLog>
    {
        AdminAuditLog.Create(Guid.NewGuid(), "admin1", ActionType.AdminCreated, Guid.NewGuid(), "User1", "created", "127.0.0.1"),
        AdminAuditLog.Create(Guid.NewGuid(), "admin2", ActionType.AdminDisabled, Guid.NewGuid(), "User2", "disabled", "127.0.0.2"),
    }.AsReadOnly();

    [Fact]
    public async Task HandleAsync_ReturnsPaginatedAuditLogDtos()
    {
        var logs = CreateAuditLogs();
        _repository.GetPagedAsync(1, 20, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns((logs, 2));

        var result = await _sut.HandleAsync(new GetAuditLogQuery(1, 20));

        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(2);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(20);
    }

    [Fact]
    public async Task HandleAsync_MapsActionTypeToString()
    {
        var logs = CreateAuditLogs();
        _repository.GetPagedAsync(1, 20, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns((logs, 2));

        var result = await _sut.HandleAsync(new GetAuditLogQuery(1, 20));

        result.Items[0].ActionType.ShouldBe("AdminCreated");
        result.Items[1].ActionType.ShouldBe("AdminDisabled");
    }

    [Fact]
    public async Task HandleAsync_PassesFiltersToRepository()
    {
        var startDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.Zero);
        _repository.GetPagedAsync(2, 10, startDate, endDate, ActionType.AdminCreated, "admin1", Arg.Any<CancellationToken>())
            .Returns((new List<AdminAuditLog>().AsReadOnly(), 0));

        var result = await _sut.HandleAsync(new GetAuditLogQuery(2, 10, startDate, endDate, ActionType.AdminCreated, "admin1"));

        result.Items.Count.ShouldBe(0);
        result.TotalCount.ShouldBe(0);
        await _repository.Received(1).GetPagedAsync(2, 10, startDate, endDate, ActionType.AdminCreated, "admin1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ParsesActionType_WhenProvided()
    {
        _repository.GetPagedAsync(1, 20, null, null, ActionType.AdminPasswordReset, null, Arg.Any<CancellationToken>())
            .Returns((new List<AdminAuditLog>().AsReadOnly(), 0));

        var result = await _sut.HandleAsync(new GetAuditLogQuery(1, 20, null, null, ActionType.AdminPasswordReset, null));

        result.Items.Count.ShouldBe(0);
        await _repository.Received(1).GetPagedAsync(1, 20, null, null, ActionType.AdminPasswordReset, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyResult_WhenNoLogs()
    {
        _repository.GetPagedAsync(1, 20, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns((new List<AdminAuditLog>().AsReadOnly(), 0));

        var result = await _sut.HandleAsync(new GetAuditLogQuery(1, 20));

        result.Items.Count.ShouldBe(0);
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_MapsAllDtoFields()
    {
        var log = AdminAuditLog.Create(
            Guid.NewGuid(), "admin1", ActionType.AdminCreated, 
            Guid.NewGuid(), "User1", "created admin", "192.168.0.1");
        var logs = new List<AdminAuditLog> { log }.AsReadOnly();

        _repository.GetPagedAsync(1, 20, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns((logs, 1));

        var result = await _sut.HandleAsync(new GetAuditLogQuery(1, 20));

        var dto = result.Items[0];
        dto.Id.ShouldBe(log.Id);
        dto.AdminUserName.ShouldBe("admin1");
        dto.ActionType.ShouldBe("AdminCreated");
        dto.Details.ShouldBe("created admin");
        dto.IpAddress.ShouldBe("192.168.0.1");
    }
}