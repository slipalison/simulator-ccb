using NSubstitute;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Commands;

public class ForcePasswordChangeCommandHandlerTests
{
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ForcePasswordChangeCommandHandler _sut;

    public ForcePasswordChangeCommandHandlerTests()
    {
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _sut = new ForcePasswordChangeCommandHandler(_keycloakUserService, _auditService);
    }

    [Fact]
    public async Task HandleAsync_UpdatesPasswordAndRemovesRequiredAction()
    {
        var command = new ForcePasswordChangeCommand(
            KeycloakUserId: "user-123",
            AdminEmail: "admin@test.com",
            NewPassword: "NewStr0ng!Pass",
            IpAddress: "127.0.0.1");

        await _sut.HandleAsync(command);

        await _keycloakUserService.Received(1).UpdateUserPasswordAsync("client", "user-123", "NewStr0ng!Pass", Arg.Any<CancellationToken>());
        await _keycloakUserService.Received(1).RemoveUpdatePasswordRequiredActionAsync("client", "user-123", Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync(
            "user-123",
            "admin@test.com",
            ActionType.AdminPasswordChanged,
            Arg.Any<Guid?>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            "127.0.0.1",
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RecordsAuditWithCorrectActionType()
    {
        var command = new ForcePasswordChangeCommand("user-456", "admin2@test.com", "An0th3r!Pass", null);

        await _sut.HandleAsync(command);

        await _auditService.Received(1).RecordAsync(
            "user-456",
            "admin2@test.com",
            ActionType.AdminPasswordChanged,
            Arg.Any<Guid?>(),
            Arg.Any<string>(),
            "{\"action\": \"password_changed\"}",
            null,
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }
}