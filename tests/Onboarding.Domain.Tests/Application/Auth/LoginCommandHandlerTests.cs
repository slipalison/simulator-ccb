using NSubstitute;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;
using Onboarding.Infrastructure.Keycloak;
using Shouldly;
using Microsoft.Extensions.Logging;

namespace Onboarding.Domain.Tests.Application.Auth;

/// <summary>
/// Unit tests for LoginCommandHandler.
/// Tests the login flow: successful login and error propagation.
/// </summary>
public class LoginCommandHandlerTests
{
    private readonly IKeycloakTokenService _tokenService = Substitute.For<IKeycloakTokenService>();
    private readonly ILogger<LoginCommandHandler> _logger = Substitute.For<ILogger<LoginCommandHandler>>();

    private LoginCommandHandler CreateHandler() => new(_tokenService, _logger);

    [Fact]
    public async Task HandleAsync_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        var expectedTokens = new TokenResponse(
            AccessToken: "fake-access-token",
            RefreshToken: "fake-refresh-token",
            ExpiresIn: 300,
            TokenType: "Bearer",
            RefreshExpiresIn: 1800,
            Scope: "openid");
        _tokenService.ExchangePasswordAsync("user@example.com", "password123", Arg.Any<CancellationToken>())
            .Returns(expectedTokens);

        var handler = CreateHandler();
        var command = new LoginCommand("user@example.com", "password123");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.ShouldBe(expectedTokens);
        await _tokenService.Received(1).ExchangePasswordAsync("user@example.com", "password123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidCredentials_ThrowsKeycloakAuthException()
    {
        // Arrange
        _tokenService.When(x => x.ExchangePasswordAsync("user@example.com", "wrong-password", Arg.Any<CancellationToken>()))
            .Do(x => throw new KeycloakAuthException("Invalid user credentials"));

        var handler = CreateHandler();
        var command = new LoginCommand("user@example.com", "wrong-password");

        // Act & Assert
        await Should.ThrowAsync<KeycloakAuthException>(async () => await handler.HandleAsync(command));
    }
}
