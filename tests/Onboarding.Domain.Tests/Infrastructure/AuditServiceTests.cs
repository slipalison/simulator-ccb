using NSubstitute;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Services;

namespace Onboarding.Domain.Tests.Infrastructure;

public class AuditServiceTests
{
    private readonly IAdminAuditLogRepository _repo;
    private readonly AuditService _sut;

    public AuditServiceTests()
    {
        _repo = Substitute.For<IAdminAuditLogRepository>();
        _sut = new AuditService(_repo);
    }

    [Fact]
    public async Task RecordAsync_ShouldCreateAndSaveLog()
    {
        // Arrange
        var actorSub = Guid.NewGuid().ToString();
        var actorEmail = "actor@test.com";
        var action = ActionType.AdminCreated;

        // Act
        await _sut.RecordAsync(actorSub, actorEmail, action);

        // Assert
        await _repo.Received(1).AddAsync(Arg.Is<AdminAuditLog>(log =>
            log.AdminUserId == Guid.Parse(actorSub) &&
            log.AdminUserName == actorEmail &&
            log.ActionType == action), Arg.Any<CancellationToken>());

        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordAsync_ShouldUseEmptyGuid_WhenActorSubIsInvalid()
    {
        // Arrange
        var actorSub = "invalid-guid";
        var actorEmail = "actor@test.com";
        var action = ActionType.AdminCreated;

        // Act
        await _sut.RecordAsync(actorSub, actorEmail, action);

        // Assert
        await _repo.Received(1).AddAsync(Arg.Is<AdminAuditLog>(log =>
            log.AdminUserId == Guid.Empty), Arg.Any<CancellationToken>());
    }
}
