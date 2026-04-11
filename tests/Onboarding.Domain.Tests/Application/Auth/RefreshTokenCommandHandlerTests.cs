using NSubstitute;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;
using Onboarding.Infrastructure.Keycloak;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Auth;

/// <summary>
/// Unit tests for RefreshTokenCommandHandler.
/// Tests the refresh token flow.
/// </summary>
public class RefreshTokenCommandHandlerTests
{
    private readonly IKeycloakTokenService _tokenService = Substitute.For<IKeycloakTokenService>();

    private RefreshTokenCommandHandler CreateHandler() => new(_tokenService);

    [Fact]
    public async Task HandleAsync_ValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange
        var expectedTokens = new TokenResponse(
            AccessToken: "new-access-token",
            RefreshToken: "new-refresh-token",
            ExpiresIn: 300,
            TokenType: "Bearer",
            RefreshExpiresIn: 1800,
            Scope: "openid");
        _tokenService.RefreshTokenAsync("old-refresh-token", Arg.Any<CancellationToken>())
            .Returns(expectedTokens);

        var handler = CreateHandler();
        var command = new RefreshTokenCommand("old-refresh-token");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.ShouldBe(expectedTokens);
        await _tokenService.Received(1).RefreshTokenAsync("old-refresh-token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ExpiredToken_ThrowsKeycloakAuthException()
    {
        // Arrange
        _tokenService.When(x => x.RefreshTokenAsync("expired-token", Arg.Any<CancellationToken>()))
            .Do(x => throw new KeycloakAuthException("Token expired"));

        var handler = CreateHandler();
        var command = new RefreshTokenCommand("expired-token");

        // Act & Assert
        await Should.ThrowAsync<KeycloakAuthException>(async () => await handler.HandleAsync(command));
    }
}
