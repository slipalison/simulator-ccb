using NSubstitute;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Commands;

public class ResetAdministratorPasswordCommandHandlerTests
{
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly ResetAdministratorPasswordCommandHandler _sut;

    public ResetAdministratorPasswordCommandHandlerTests()
    {
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _sut = new ResetAdministratorPasswordCommandHandler(_keycloakUserService, _auditService);
    }

    [Fact]
    public async Task HandleAsync_ShouldResetPasswordAndRecordAudit_WhenValid()
    {
        // Arrange
        var targetId = Guid.NewGuid().ToString();
        var command = new ResetAdministratorPasswordCommand(
            TargetKeycloakId: targetId,
            TargetUserName: "Admin",
            ActorSub: Guid.NewGuid().ToString(),
            ActorEmail: "actor@test.com",
            IpAddress: "127.0.0.1"
        );

        _keycloakUserService.GetUserByIdAsync("backoffice", targetId)
            .Returns(new KeycloakUserDetails(targetId, "admin@test.com", true, true, new List<string>().AsReadOnly(), "Admin"));

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.TemporaryPassword.Length.ShouldBe(16);
        await _keycloakUserService.Received(1).ResetPasswordAsTemporaryAsync("backoffice", targetId, result.TemporaryPassword);
        await _auditService.Received(1).RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.AdminPasswordReset,
            targetUserId: Guid.Parse(targetId),
            targetUserName: command.TargetUserName,
            details: null,
            ipAddress: command.IpAddress);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenAdminResetsThemselves()
    {
        // Arrange
        var targetId = Guid.NewGuid().ToString();
        var command = new ResetAdministratorPasswordCommand(targetId, "Admin", targetId, "actor@test.com", null);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.HandleAsync(command));
    }
}
