using NSubstitute;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Commands;

public class ToggleAdministratorStatusCommandHandlerTests
{
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ToggleAdministratorStatusCommandHandler _sut;

    public ToggleAdministratorStatusCommandHandlerTests()
    {
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _sut = new ToggleAdministratorStatusCommandHandler(_keycloakUserService, _auditService);
    }

    [Fact]
    public async Task HandleAsync_ShouldActivateAdmin_WhenValid()
    {
        // Arrange
        var targetId = Guid.NewGuid().ToString();
        var command = new ToggleAdministratorStatusCommand(
            TargetKeycloakId: targetId,
            TargetUserName: "Admin",
            Activate: true,
            Reason: null,
            ActorSub: Guid.NewGuid().ToString(),
            ActorEmail: "actor@test.com",
            IpAddress: "127.0.0.1"
        );

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _keycloakUserService.Received(1).UnblockUserAsync("backoffice", targetId);
        await _auditService.Received(1).RecordAsync(
            action: ActionType.AdminReactivated,
            targetUserId: Guid.Parse(targetId),
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            targetUserName: command.TargetUserName,
            details: null,
            ipAddress: command.IpAddress);
    }

    [Fact]
    public async Task HandleAsync_ShouldDisableAdmin_WhenValid()
    {
        // Arrange
        var targetId = Guid.NewGuid().ToString();
        var command = new ToggleAdministratorStatusCommand(
            TargetKeycloakId: targetId,
            TargetUserName: "Admin",
            Activate: false,
            Reason: "Performance issues",
            ActorSub: Guid.NewGuid().ToString(),
            ActorEmail: "actor@test.com",
            IpAddress: "127.0.0.1"
        );

        var admins = new List<AdminUserDto>
        {
            new(targetId, "admin1@test.com", "Admin 1", true, false),
            new(Guid.NewGuid().ToString(), "admin2@test.com", "Admin 2", true, false)
        }.AsReadOnly();

        _keycloakUserService.GetUsersByRoleAsync("backoffice", "admin").Returns(admins);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _keycloakUserService.Received(1).BlockUserAsync("backoffice", targetId);
        await _keycloakUserService.Received(1).LogoutAllSessionsAsync("backoffice", targetId);
        await _auditService.Received(1).RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.AdminDisabled,
            targetUserId: Guid.Parse(targetId),
            targetUserName: command.TargetUserName,
            details: Arg.Is<string>(d => d.Contains("Performance issues")),
            ipAddress: command.IpAddress);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenDisablingLastAdmin()
    {
        // Arrange
        var targetId = Guid.NewGuid().ToString();
        var command = new ToggleAdministratorStatusCommand(targetId, "Admin", false, null, Guid.NewGuid().ToString(), "actor@test.com", null);

        var admins = new List<AdminUserDto>
        {
            new(targetId, "admin1@test.com", "Admin 1", true, false)
        }.AsReadOnly();

        _keycloakUserService.GetUsersByRoleAsync("backoffice", "admin").Returns(admins);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => _sut.HandleAsync(command));
        ex.Message.ShouldContain("last active administrator");
    }
}
