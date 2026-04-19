using NSubstitute;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.PasswordReset;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Onboarding.Domain.Tests.Application.Auth;

/// <summary>
/// Unit tests for ResetPasswordCommandHandler.
/// Tests password reset: valid token, expired token, used token, invalid password.
/// </summary>
public class ResetPasswordCommandHandlerTests
{
    private readonly IPasswordResetTokenRepository _tokenRepo = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IKeycloakUserService _keycloakService = Substitute.For<IKeycloakUserService>();
    private readonly ILogger<ResetPasswordCommandHandler> _logger = Substitute.For<ILogger<ResetPasswordCommandHandler>>();

    private ResetPasswordCommandHandler CreateHandler() => new(_tokenRepo, _keycloakService, _logger);

    [Fact]
    public async Task HandleAsync_ValidToken_ResetsPassword()
    {
        // Arrange
        var resetToken = PasswordResetToken.Create("user@example.com");
        _tokenRepo.GetByTokenAsync(resetToken.Token, Arg.Any<CancellationToken>())
            .Returns(resetToken);
        _keycloakService.GetUserByEmailAsync("client", "user@example.com", Arg.Any<CancellationToken>())
            .Returns(new KeycloakUser("kc-id-123", "user@example.com"));

        var handler = CreateHandler();
        var command = new ResetPasswordCommand(resetToken.Token, "NewStr0ng@Pass");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.ShouldBe(Unit.Value);
        resetToken.IsUsed.ShouldBeTrue();
        await _keycloakService.Received(1).UpdateUserPasswordAsync("client", "kc-id-123", "NewStr0ng@Pass", Arg.Any<CancellationToken>());
        await _tokenRepo.Received(1).UpdateAsync(resetToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidToken_ThrowsBadRequestException()
    {
        // Arrange
        _tokenRepo.GetByTokenAsync("nonexistent-token", Arg.Any<CancellationToken>())
            .Returns((PasswordResetToken?)null);

        var handler = CreateHandler();
        var command = new ResetPasswordCommand("nonexistent-token", "NewStr0ng@Pass");

        // Act & Assert
        await Should.ThrowAsync<BadRequestException>(async () => await handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_ExpiredToken_ThrowsBadRequestException()
    {
        // Arrange - create a real token and set it to expired via reflection
        var expiredToken = PasswordResetToken.Create("user@example.com");
        var expiresAtProp = typeof(PasswordResetToken).GetProperty(nameof(PasswordResetToken.ExpiresAt),
            BindingFlags.Public | BindingFlags.Instance)!;
        expiresAtProp.SetValue(expiredToken, DateTime.UtcNow.AddMinutes(-30));

        _tokenRepo.GetByTokenAsync(expiredToken.Token, Arg.Any<CancellationToken>())
            .Returns(expiredToken);

        var handler = CreateHandler();
        var command = new ResetPasswordCommand(expiredToken.Token, "NewStr0ng@Pass");

        // Act & Assert
        var ex = await Should.ThrowAsync<BadRequestException>(async () => await handler.HandleAsync(command));
        ex.Message.ShouldContain("expirado");
    }

    [Fact]
    public async Task HandleAsync_UsedToken_ThrowsBadRequestException()
    {
        // Arrange
        var usedToken = PasswordResetToken.Create("user@example.com");
        usedToken.MarkAsUsed();

        _tokenRepo.GetByTokenAsync(usedToken.Token, Arg.Any<CancellationToken>())
            .Returns(usedToken);

        var handler = CreateHandler();
        var command = new ResetPasswordCommand(usedToken.Token, "NewStr0ng@Pass");

        // Act & Assert
        var ex = await Should.ThrowAsync<BadRequestException>(async () => await handler.HandleAsync(command));
        ex.Message.ShouldContain("ja utilizado");
    }

    [Fact]
    public async Task HandleAsync_WeakPassword_ThrowsBadRequestException()
    {
        // Arrange
        var resetToken = PasswordResetToken.Create("user@example.com");
        _tokenRepo.GetByTokenAsync(resetToken.Token, Arg.Any<CancellationToken>())
            .Returns(resetToken);

        var handler = CreateHandler();

        // Act & Assert - too short
        var command1 = new ResetPasswordCommand(resetToken.Token, "Short1!");
        await Should.ThrowAsync<BadRequestException>(async () => await handler.HandleAsync(command1));

        // Act & Assert - no uppercase
        var command2 = new ResetPasswordCommand(resetToken.Token, "nouppercase1!");
        await Should.ThrowAsync<BadRequestException>(async () => await handler.HandleAsync(command2));

        // Act & Assert - no digit
        var command3 = new ResetPasswordCommand(resetToken.Token, "NoDigitHere!");
        await Should.ThrowAsync<BadRequestException>(async () => await handler.HandleAsync(command3));

        // Act & Assert - no special char
        var command4 = new ResetPasswordCommand(resetToken.Token, "NoSpecial1Char");
        await Should.ThrowAsync<BadRequestException>(async () => await handler.HandleAsync(command4));
    }

    [Fact]
    public async Task HandleAsync_UserNotFoundInKeycloak_ThrowsBadRequestException()
    {
        // Arrange
        var resetToken = PasswordResetToken.Create("user@example.com");
        _tokenRepo.GetByTokenAsync(resetToken.Token, Arg.Any<CancellationToken>())
            .Returns(resetToken);
        _keycloakService.GetUserByEmailAsync("client", "user@example.com", Arg.Any<CancellationToken>())
            .Returns((KeycloakUser?)null);

        var handler = CreateHandler();
        var command = new ResetPasswordCommand(resetToken.Token, "NewStr0ng@Pass");

        // Act & Assert
        await Should.ThrowAsync<BadRequestException>(async () => await handler.HandleAsync(command));
    }
}
