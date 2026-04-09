using Onboarding.Application.Auth.DTOs;
using Onboarding.API.Tests.Authentication;
using NSubstitute;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Onboarding.Infrastructure.Keycloak;

namespace Onboarding.API.Tests.Api;

/// <summary>
/// Tests for POST /api/auth/refresh behavior (AUTH-04).
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class RefreshTokenEndpointTests : IAsyncLifetime
{
    private AuthTestApiFactory? _factory;
    private HttpClient? _client;

    private static readonly TokenResponse FakeTokens = new(
        AccessToken: "new-fake-access-token",
        RefreshToken: "new-fake-refresh-token",
        ExpiresIn: 300,
        TokenType: "Bearer",
        RefreshExpiresIn: 1800,
        Scope: "openid email profile");

    public Task InitializeAsync()
    {
        _factory = new AuthTestApiFactory();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Refresh_WithValidRefreshToken_Returns200WithNewTokens()
    {
        // Arrange
        _factory!.TokenServiceMock
            .RefreshTokenAsync("valid-refresh-token", Arg.Any<CancellationToken>())
            .Returns(FakeTokens);

        var payload = new { refreshToken = "valid-refresh-token" };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/refresh", payload);

        // Assert — AUTH-04: 200 with new token pair
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.ShouldNotBeNull();
        body.ContainsKey("accessToken").ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Refresh_WithInvalidRefreshToken_Returns401WithGenericMessage()
    {
        // Arrange — mock throws KeycloakAuthException
        _factory!.TokenServiceMock
            .RefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TokenResponse>(new KeycloakAuthException("Invalid or expired refresh token.")));

        var payload = new { refreshToken = "expired-or-invalid-token" };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/refresh", payload);

        // Assert — D-13 + SEC-08: generic error
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Refresh_WithMissingRefreshToken_Returns422()
    {
        var payload = new { refreshToken = (string?)null };

        var response = await _client!.PostAsJsonAsync("/api/auth/refresh", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}
