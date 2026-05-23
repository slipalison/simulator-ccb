using NSubstitute;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Commands;

public class CreateAdminCommandHandlerTests
{
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IAuditService _auditService;
    private readonly CreateAdminCommandHandler _sut;

    public CreateAdminCommandHandlerTests()
    {
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _auditService = Substitute.For<IAuditService>();
        _sut = new CreateAdminCommandHandler(_keycloakUserService, _auditService);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateAdminAndRecordAudit_WhenValid()
    {
        // Arrange
        var command = new CreateAdminCommand(
            FullName: "Admin Test",
            Email: "admin@test.com",
            CreatorSub: Guid.NewGuid().ToString(),
            CreatorEmail: "creator@test.com",
            IpAddress: "127.0.0.1"
        );

        var keycloakUserId = Guid.NewGuid().ToString();
        _keycloakUserService.GetUserByEmailAsync("backoffice", command.Email).Returns((KeycloakUser?)null);
        _keycloakUserService.CreateAdminUserAsync("backoffice", command.Email, Arg.Any<string>(), command.FullName)
            .Returns(keycloakUserId);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.AdminId.ShouldBe(Guid.Parse(keycloakUserId));
        result.TemporaryPassword.Length.ShouldBe(14);

        await _keycloakUserService.Received(1).CreateAdminUserAsync(
            "backoffice", command.Email, Arg.Any<string>(), command.FullName);

        await _auditService.Received(1).RecordAsync(
            actorSub: command.CreatorSub,
            actorEmail: command.CreatorEmail,
            action: ActionType.AdminCreated,
            targetUserId: result.AdminId,
            targetUserName: command.FullName,
            details: Arg.Is<string>(d => d.Contains(command.Email)),
            ipAddress: command.IpAddress);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenEmailAlreadyExists()
    {
        // Arrange
        var command = new CreateAdminCommand("Admin", "existing@test.com", "sub", "creator@test.com", null);
        _keycloakUserService.GetUserByEmailAsync("backoffice", command.Email)
            .Returns(new KeycloakUser("123", command.Email, true, true));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.HandleAsync(command));

        await _keycloakUserService.DidNotReceive().CreateAdminUserAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
