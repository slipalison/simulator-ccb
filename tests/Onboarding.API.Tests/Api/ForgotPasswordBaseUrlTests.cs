using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Common;
using Onboarding.Application.Services;
using Onboarding.Domain.Repositories;

namespace Onboarding.API.Tests.Api;

/// <summary>
/// Tests for forgot password reset link base URL configuration (Phase 13 — P0 audit fix).
/// Verifies that reset emails contain configurable Frontend:BaseUrl instead of hardcoded localhost:3001.
/// </summary>
public class ForgotPasswordBaseUrlTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ForgotPassword_ResetLinkContainsConfiguredFrontendBaseUrl()
    {
        // Arrange
        string? capturedLink = null;
        var emailMock = Substitute.For<IEmailService>();
        emailMock.SendPasswordResetEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => capturedLink = ci.ArgAt<string>(1));

        var keycloakMock = Substitute.For<IKeycloakUserService>();
        keycloakMock.UserExistsByEmailAsync("user@example.com", Arg.Any<CancellationToken>())
            .Returns(true);

        var tokenRepoMock = Substitute.For<IPasswordResetTokenRepository>();
        tokenRepoMock.CountRecentTokensAsync("user@example.com", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = new ForgotPasswordCommandHandler(
            tokenRepoMock,
            keycloakMock,
            emailMock,
            BuildConfiguration("http://localhost:5173"),
            Substitute.For<Microsoft.Extensions.Logging.ILogger<ForgotPasswordCommandHandler>>()
        );

        // Act
        await handler.HandleAsync(new ForgotPasswordCommand("user@example.com"));

        // Assert
        capturedLink.ShouldNotBeNull();
        capturedLink.ShouldContain("http://localhost:5173/reset-password?token=");
        capturedLink.ShouldNotContain("localhost:3001");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ForgotPassword_ResetLinkUsesDefaultFallback_WhenConfigMissing()
    {
        // Arrange — no Frontend:BaseUrl configured, should default to http://localhost:5173
        string? capturedLink = null;
        var emailMock = Substitute.For<IEmailService>();
        emailMock.SendPasswordResetEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => capturedLink = ci.ArgAt<string>(1));

        var keycloakMock = Substitute.For<IKeycloakUserService>();
        keycloakMock.UserExistsByEmailAsync("fallback@example.com", Arg.Any<CancellationToken>())
            .Returns(true);

        var tokenRepoMock = Substitute.For<IPasswordResetTokenRepository>();
        tokenRepoMock.CountRecentTokensAsync("fallback@example.com", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = new ForgotPasswordCommandHandler(
            tokenRepoMock,
            keycloakMock,
            emailMock,
            BuildConfiguration(null), // no config
            Substitute.For<Microsoft.Extensions.Logging.ILogger<ForgotPasswordCommandHandler>>()
        );

        // Act
        await handler.HandleAsync(new ForgotPasswordCommand("fallback@example.com"));

        // Assert — should default to http://localhost:5173
        capturedLink.ShouldNotBeNull();
        capturedLink.ShouldContain("http://localhost:5173/reset-password?token=");
        capturedLink.ShouldNotContain("localhost:3001");
    }

    private static IConfiguration BuildConfiguration(string? baseUrl)
    {
        var dict = new Dictionary<string, string?>();
        if (!string.IsNullOrEmpty(baseUrl))
        {
            dict["Frontend:BaseUrl"] = baseUrl;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }
}
