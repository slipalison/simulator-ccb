using NSubstitute;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Commands;

public class UpdateAdministratorCommandHandlerTests
{
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly UpdateAdministratorCommandHandler _sut;

    public UpdateAdministratorCommandHandlerTests()
    {
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _sut = new UpdateAdministratorCommandHandler(_keycloakUserService, _auditService);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateAdminAndRecordAudit_WhenValid()
    {
        // Arrange
        var targetId = Guid.NewGuid().ToString();
        var actorSub = Guid.NewGuid().ToString();
        var command = new UpdateAdministratorCommand(
            TargetKeycloakId: targetId,
            FullName: "New Name",
            Email: "new@test.com",
            ActorSub: actorSub,
            ActorEmail: "actor@test.com",
            IpAddress: "127.0.0.1"
        );

        var current = new KeycloakUserDetails(targetId, "old@test.com", true, true, new List<string>().AsReadOnly(), "Old Name");
        _keycloakUserService.GetUserByIdAsync("backoffice", targetId).Returns(current);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _keycloakUserService.Received(1).UpdateAdminUserAsync(
            "backoffice", targetId, command.FullName, command.Email);

        await _auditService.Received(1).RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.AdminEdited,
            targetUserId: Guid.Parse(targetId),
            targetUserName: command.FullName,
            details: Arg.Is<string>(d => d.Contains("Old Name") && d.Contains("New Name")),
            ipAddress: command.IpAddress);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenAdminEditsThemselves()
    {
        // Arrange
        var targetId = Guid.NewGuid().ToString();
        var command = new UpdateAdministratorCommand(targetId, "Name", "email@test.com", targetId, "email@test.com", null);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowKeyNotFoundException_WhenAdminDoesNotExist()
    {
        // Arrange
        var targetId = Guid.NewGuid().ToString();
        var command = new UpdateAdministratorCommand(targetId, "Name", "email@test.com", Guid.NewGuid().ToString(), "actor@test.com", null);
        _keycloakUserService.GetUserByIdAsync("backoffice", targetId).Returns((KeycloakUserDetails?)null);

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }
}
