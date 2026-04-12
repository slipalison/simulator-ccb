using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Onboarding.API.Tests.Authentication;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Exceptions;
using Onboarding.Infrastructure.Keycloak;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Onboarding.API.Tests.Api;

/// <summary>
/// Tests for AuthController endpoints not covered by AuthEndpointTests:
/// logout, me, login success, login unauthorized, refresh with cookie.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class AuthControllerExtraTests : IAsyncLifetime
{
    private AuthTestApiFactory? _factory;
    private HttpClient? _client;

    public Task InitializeAsync()
    {
        _factory = new AuthTestApiFactory();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Logout_Returns204()
    {
        var response = await _client!.PostAsync("/api/auth/logout", null);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetMe_WithoutCookie_Returns401()
    {
        var response = await _client!.GetAsync("/api/auth/me");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithAccessToken()
    {
        _factory!.TokenServiceMock
            .ExchangePasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TokenResponse("fake-access-token", "fake-refresh-token", 300, "Bearer", 1800, "openid"));

        var payload = new { email = "test@example.com", password = "Test123!@" };
        var response = await _client!.PostAsJsonAsync("/api/auth/login", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("fake-access-token");
        body.ShouldNotContain("fake-refresh-token"); // refresh token only in cookie
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        _factory!.TokenServiceMock
            .ExchangePasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeycloakAuthException("Invalid credentials"));

        var payload = new { email = "test@example.com", password = "WrongPass1!" };
        var response = await _client!.PostAsJsonAsync("/api/auth/login", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithValidRefreshCookie_Returns200()
    {
        _factory!.TokenServiceMock
            .RefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TokenResponse("new-access-token", "new-refresh-token", 300, "Bearer", 1800, "openid"));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("Cookie", "refreshToken=valid-refresh-token");
        var response = await _client!.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMe_WithExpiredRefreshCookie_Returns401()
    {
        _factory!.TokenServiceMock
            .RefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeycloakAuthException("Token expired"));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("Cookie", "refreshToken=expired-refresh-token");
        var response = await _client!.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithValidCookie_Returns200()
    {
        _factory!.TokenServiceMock
            .RefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TokenResponse("new-access", "new-refresh", 300, "Bearer", 1800, "openid"));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", "refreshToken=valid-token");
        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("new-access");
        body.ShouldNotContain("new-refresh"); // refresh only in cookie
    }

    [Fact]
    public async Task Refresh_WithExpiredCookie_Returns401()
    {
        _factory!.TokenServiceMock
            .RefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeycloakAuthException("Token expired"));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", "refreshToken=expired-token");
        var response = await _client!.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
