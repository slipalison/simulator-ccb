using NSubstitute;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Common;
using Onboarding.Application.Services;
using Onboarding.Domain.Aggregates.PasswordReset;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Onboarding.Domain.Tests.Application.Auth;

/// <summary>
/// Unit tests for ForgotPasswordCommandHandler.
/// Tests the forgot password flow: token creation, rate limiting, email sending,
/// and info-disclosure prevention for non-existent emails.
/// </summary>
public class ForgotPasswordCommandHandlerTests
{
    private readonly IPasswordResetTokenRepository _tokenRepo = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IKeycloakUserService _keycloakService = Substitute.For<IKeycloakUserService>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly ILogger<ForgotPasswordCommandHandler> _logger = Substitute.For<ILogger<ForgotPasswordCommandHandler>>();

    private ForgotPasswordCommandHandler CreateHandler() => new(
        _tokenRepo, _keycloakService, _emailService, _configuration, _logger);

    [Fact]
    public async Task HandleAsync_UserExists_SendsEmailAndCreatesToken()
    {
        // Arrange
        _keycloakService.UserExistsByEmailAsync("user@example.com", Arg.Any<CancellationToken>())
            .Returns(true);
        _tokenRepo.CountRecentTokensAsync("user@example.com", TimeSpan.FromHours(1), Arg.Any<CancellationToken>())
            .Returns(0);
        _configuration["Frontend:BaseUrl"].Returns("http://localhost:3000");

        var handler = CreateHandler();
        var command = new ForgotPasswordCommand("user@example.com");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.ShouldBe(Unit.Value);
        await _tokenRepo.Received(1).AddAsync(Arg.Is<PasswordResetToken>(t => t.Email == "user@example.com"), Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendPasswordResetEmailAsync(
            "user@example.com", Arg.Is<string>(link => link.Contains("localhost:3000")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UserDoesNotExist_ReturnsSuccessWithoutSendingEmail()
    {
        // Arrange - user doesn't exist
        _keycloakService.UserExistsByEmailAsync("nobody@example.com", Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = CreateHandler();
        var command = new ForgotPasswordCommand("nobody@example.com");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert - returns success (no info disclosure) and no email sent
        result.ShouldBe(Unit.Value);
        await _tokenRepo.DidNotReceive().AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
        await _emailService.DidNotReceive().SendPasswordResetEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RateLimitExceeded_ThrowsException()
    {
        // Arrange - 3 recent tokens (rate limit reached)
        _keycloakService.UserExistsByEmailAsync("user@example.com", Arg.Any<CancellationToken>())
            .Returns(true);
        _tokenRepo.CountRecentTokensAsync("user@example.com", TimeSpan.FromHours(1), Arg.Any<CancellationToken>())
            .Returns(3);

        var handler = CreateHandler();
        var command = new ForgotPasswordCommand("user@example.com");

        // Act & Assert
        await Should.ThrowAsync<RateLimitExceededException>(async () => await handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_UsesDefaultBaseUrlWhenConfigMissing()
    {
        // Arrange
        _keycloakService.UserExistsByEmailAsync("user@example.com", Arg.Any<CancellationToken>())
            .Returns(true);
        _tokenRepo.CountRecentTokensAsync("user@example.com", TimeSpan.FromHours(1), Arg.Any<CancellationToken>())
            .Returns(0);
        _configuration["Frontend:BaseUrl"].Returns((string?)null);

        var handler = CreateHandler();
        var command = new ForgotPasswordCommand("user@example.com");

        // Act
        await handler.HandleAsync(command);

        // Assert - default URL is localhost:5173
        await _emailService.Received(1).SendPasswordResetEmailAsync(
            "user@example.com", Arg.Is<string>(link => link.Contains("localhost:5173")), Arg.Any<CancellationToken>());
    }
}
