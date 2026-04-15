using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Services;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Commands;

[Trait("Category", "Unit")]
public sealed class AuditServiceTests
{
    private readonly IAdminAuditLogRepository _repoMock = Substitute.For<IAdminAuditLogRepository>();
    private readonly IAuditService _sut;

    public AuditServiceTests() => _sut = new AuditService(_repoMock);

    [Fact]
    public async Task RecordAsync_WithValidGuidSub_CallsAddAndSave()
    {
        var sub = Guid.NewGuid().ToString();
        await _sut.RecordAsync(sub, "admin@test.com", ActionType.UserBlocked, Guid.NewGuid(), "Target User");
        await _repoMock.Received(1).AddAsync(Arg.Any<AdminAuditLog>(), Arg.Any<CancellationToken>());
        await _repoMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordAsync_WithEmailAsSub_DoesNotThrow_UsesGuidEmpty()
    {
        await _sut.RecordAsync("admin@test.com", "admin@test.com", ActionType.UserBlocked, null, null);
        await _repoMock.Received(1).AddAsync(
            Arg.Is<AdminAuditLog>(l => l.AdminUserId == Guid.Empty),
            Arg.Any<CancellationToken>());
    }
}
