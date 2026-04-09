using Onboarding.Application.Auth.DTOs;
using Onboarding.API.Tests.Authentication;
using NSubstitute;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Onboarding.Infrastructure.Keycloak;

namespace Onboarding.API.Tests.Api;

/// <summary>
/// Tests for POST /api/auth/login behavior (AUTH-02) and token response.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class LoginEndpointTests : IAsyncLifetime
{
    private AuthTestApiFactory? _factory;
    private HttpClient? _client;

    private static readonly TokenResponse FakeTokens = new(
        AccessToken: "fake-access-token",
        RefreshToken: "fake-refresh-token",
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
    public async Task Login_WithValidCredentials_Returns200WithTokens()
    {
        // Arrange — mock token service returns fake token pair
        _factory!.TokenServiceMock
            .ExchangePasswordAsync("joao@example.com", "Senha@123!", Arg.Any<CancellationToken>())
            .Returns(FakeTokens);

        var payload = new { email = "joao@example.com", password = "Senha@123!" };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/login", payload);

        // Assert — AUTH-02: 200 with access_token + refresh_token
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.ShouldNotBeNull();
        body.ContainsKey("accessToken").ShouldBeTrue();
        body.ContainsKey("refreshToken").ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Login_WithInvalidCredentials_Returns401WithGenericMessage()
    {
        // Arrange — mock throws KeycloakAuthException (Keycloak 401)
        _factory!.TokenServiceMock
            .ExchangePasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TokenResponse>(new KeycloakAuthException("Invalid credentials.")));

        var payload = new { email = "joao@example.com", password = "WrongPassword" };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/login", payload);

        // Assert — D-13 + SEC-08: 401 with generic message (no user enumeration)
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Login_WithMissingEmail_Returns422()
    {
        var payload = new { email = (string?)null, password = "Senha@123!" };

        var response = await _client!.PostAsJsonAsync("/api/auth/login", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Login_WithMissingPassword_Returns422()
    {
        var payload = new { email = "joao@example.com", password = (string?)null };

        var response = await _client!.PostAsJsonAsync("/api/auth/login", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}
