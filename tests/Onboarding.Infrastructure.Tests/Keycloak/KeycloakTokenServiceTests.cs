using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Onboarding.Infrastructure.Keycloak;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Keycloak;

/// <summary>
/// Unit tests for KeycloakTokenService — covers ExchangePasswordAsync and RefreshTokenAsync
/// using a mock HttpMessageHandler. D-11: uses IHttpClientFactory named client "keycloak-token".
/// </summary>
public sealed class KeycloakTokenServiceTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MockTokenHttpMessageHandler _handler;
    private readonly KeycloakTokenService _sut;

    private const string RealmUrl = "http://keycloak:8080/realms/client";
    private const string ClientId = "onboarding-app";

    public KeycloakTokenServiceTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _handler = new MockTokenHttpMessageHandler();

        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://keycloak:8080/")
        };
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:RealmUrl"] = RealmUrl,
                ["Keycloak:PublicClientId"] = ClientId,
            })
            .Build();

        _sut = new KeycloakTokenService(_httpClientFactory, configuration);
    }

    // --- ExchangePasswordAsync ---

    [Fact]
    public async Task ExchangePasswordAsync_ValidCredentials_ReturnsTokenResponse()
    {
        // Arrange
        var tokenJson = new
        {
            access_token = "access-abc",
            refresh_token = "refresh-xyz",
            expires_in = 300,
            token_type = "Bearer",
            refresh_expires_in = 1800,
            scope = "openid email profile"
        };
        _handler.SetupResponse(HttpStatusCode.OK, tokenJson);

        // Act
        var result = await _sut.ExchangePasswordAsync("user@test.com", "password123");

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldBe("access-abc");
        result.RefreshToken.ShouldBe("refresh-xyz");
        result.ExpiresIn.ShouldBe(300);
        result.TokenType.ShouldBe("Bearer");
        result.RefreshExpiresIn.ShouldBe(1800);
        result.Scope.ShouldBe("openid email profile");
    }

    [Fact]
    public async Task ExchangePasswordAsync_InvalidCredentials_ThrowsKeycloakAuthException()
    {
        // Arrange
        _handler.SetupResponse(HttpStatusCode.Unauthorized, null);

        // Act & Assert
        await Should.ThrowAsync<KeycloakAuthException>(
            () => _sut.ExchangePasswordAsync("bad@test.com", "wrong"));
    }

    [Fact]
    public async Task ExchangePasswordAsync_ServerError_ThrowsKeycloakAuthException()
    {
        // Arrange
        _handler.SetupResponse(HttpStatusCode.InternalServerError, null);

        // Act & Assert
        await Should.ThrowAsync<KeycloakAuthException>(
            () => _sut.ExchangePasswordAsync("user@test.com", "pass"));
    }

    [Fact]
    public async Task ExchangePasswordAsync_ResponseWithNullScope_ReturnsEmptyScope()
    {
        // Arrange
        var tokenJson = new
        {
            access_token = "tok",
            refresh_token = "ref",
            expires_in = 60,
            token_type = "Bearer",
            refresh_expires_in = 3600,
            scope = (string?)null
        };
        _handler.SetupResponse(HttpStatusCode.OK, tokenJson);

        // Act
        var result = await _sut.ExchangePasswordAsync("user@test.com", "pass");

        // Assert
        result.Scope.ShouldBe(string.Empty);
    }

    // --- RefreshTokenAsync ---

    [Fact]
    public async Task RefreshTokenAsync_ValidRefreshToken_ReturnsNewTokenResponse()
    {
        // Arrange
        var tokenJson = new
        {
            access_token = "new-access",
            refresh_token = "new-refresh",
            expires_in = 300,
            token_type = "Bearer",
            refresh_expires_in = 1800,
            scope = "openid email roles profile"
        };
        _handler.SetupResponse(HttpStatusCode.OK, tokenJson);

        // Act
        var result = await _sut.RefreshTokenAsync("old-refresh-token");

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldBe("new-access");
        result.RefreshToken.ShouldBe("new-refresh");
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredRefreshToken_ThrowsKeycloakAuthException()
    {
        // Arrange
        _handler.SetupResponse(HttpStatusCode.BadRequest, null);

        // Act & Assert
        await Should.ThrowAsync<KeycloakAuthException>(
            () => _sut.RefreshTokenAsync("expired-refresh-token"));
    }

    [Fact]
    public async Task RefreshTokenAsync_Forbidden_ThrowsKeycloakAuthException()
    {
        // Arrange
        _handler.SetupResponse(HttpStatusCode.Forbidden, null);

        // Act & Assert
        await Should.ThrowAsync<KeycloakAuthException>(
            () => _sut.RefreshTokenAsync("invalid-token"));
    }

    // --- Constructor validation ---

    [Fact]
    public void Constructor_MissingRealmUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:PublicClientId"] = "app"
            })
            .Build();

        // Act & Assert
        Should.Throw<InvalidOperationException>(
            () => new KeycloakTokenService(_httpClientFactory, cfg));
    }

    [Fact]
    public void Constructor_MissingPublicClientId_ThrowsInvalidOperationException()
    {
        // Arrange
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:RealmUrl"] = "http://kc/realm/test"
            })
            .Build();

        // Act & Assert
        Should.Throw<InvalidOperationException>(
            () => new KeycloakTokenService(_httpClientFactory, cfg));
    }

    [Fact]
    public async Task ExchangePasswordAsync_UsesBearerClientName()
    {
        // Arrange
        var tokenJson = new
        {
            access_token = "tok",
            refresh_token = "ref",
            expires_in = 60,
            token_type = "Bearer",
            refresh_expires_in = 600,
            scope = "openid"
        };
        _handler.SetupResponse(HttpStatusCode.OK, tokenJson);

        // Act
        await _sut.ExchangePasswordAsync("u@t.com", "p");

        // Assert — factory must be called with named client
        _httpClientFactory.Received(1).CreateClient("keycloak-token");
    }

    [Fact]
    public async Task RefreshTokenAsync_UsesKeycloakTokenClientName()
    {
        // Arrange
        var tokenJson = new
        {
            access_token = "tok",
            refresh_token = "ref",
            expires_in = 60,
            token_type = "Bearer",
            refresh_expires_in = 600,
            scope = "openid"
        };
        _handler.SetupResponse(HttpStatusCode.OK, tokenJson);

        // Act
        await _sut.RefreshTokenAsync("some-refresh");

        // Assert
        _httpClientFactory.Received(1).CreateClient("keycloak-token");
    }
}

/// <summary>
/// Simple mock handler — returns the configured response for any request.
/// </summary>
internal sealed class MockTokenHttpMessageHandler : HttpMessageHandler
{
    private HttpResponseMessage _response = new(HttpStatusCode.OK);

    public void SetupResponse(HttpStatusCode statusCode, object? body)
    {
        var r = new HttpResponseMessage(statusCode);
        if (body is not null)
            r.Content = JsonContent.Create(body);
        _response = r;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_response);
}
