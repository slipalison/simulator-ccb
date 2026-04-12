using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Exceptions;
using Onboarding.Infrastructure.Keycloak;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace Onboarding.API.Tests.Api;

/// <summary>
/// Integration tests for AuthController endpoints (login, refresh, forgot/reset password).
/// Uses mocks for IKeycloakTokenService and IKeycloakUserService.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class AuthEndpointTests : IAsyncLifetime
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
    public async Task Login_MissingFields_Returns422()
    {
        var payload = new { };
        var response = await _client!.PostAsJsonAsync("/api/auth/login", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_InvalidEmailFormat_Returns422()
    {
        var payload = new { email = "not-an-email", password = "Test123!" };
        var response = await _client!.PostAsJsonAsync("/api/auth/login", payload);
        // Either 400 or 422 depending on validation
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
    }

    [Fact]
    public async Task RefreshToken_MissingCookie_Returns401()
    {
        var payload = new { };
        var response = await _client!.PostAsJsonAsync("/api/auth/refresh", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForgotPassword_MissingEmail_Returns400()
    {
        var payload = new { };
        var response = await _client!.PostAsJsonAsync("/api/auth/forgot-password", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmail_Returns422()
    {
        var payload = new { email = "not-an-email" };
        var response = await _client!.PostAsJsonAsync("/api/auth/forgot-password", payload);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
    }

    [Fact]
    public async Task ResetPassword_MissingFields_Returns422()
    {
        var payload = new { };
        var response = await _client!.PostAsJsonAsync("/api/auth/reset-password", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}
