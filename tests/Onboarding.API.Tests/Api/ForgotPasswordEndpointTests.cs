using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using Shouldly;
using Onboarding.API.Tests.Authentication;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.PasswordReset;

namespace Onboarding.API.Tests.Api;

/// <summary>
/// Integration tests for forgot/reset password flow (Task 11.2.1 — GREEN).
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class ForgotPasswordEndpointTests : IAsyncLifetime
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
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ForgotPassword_ExistingEmail_Returns200_GenericMessage()
    {
        // Arrange
        _factory!.KeycloakUserServiceMock
            .UserExistsByEmailAsync("existing@example.com", Arg.Any<CancellationToken>())
            .Returns(true);
        _factory.TokenRepositoryMock
            .CountRecentTokensAsync("existing@example.com", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _factory.EmailServiceMock
            .SendPasswordResetEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var payload = new { email = "existing@example.com" };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/forgot-password", payload);

        // Assert — should return 200 with generic message (no info disclosure)
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.ShouldNotBeNull();
        body.ContainsKey("message").ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ForgotPassword_NonExistingEmail_Returns200_GenericMessage()
    {
        // Arrange
        _factory!.KeycloakUserServiceMock
            .UserExistsByEmailAsync("nonexistent@example.com", Arg.Any<CancellationToken>())
            .Returns(false);

        var payload = new { email = "nonexistent@example.com" };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/forgot-password", payload);

        // Assert — same 200 response (no info disclosure)
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.ShouldNotBeNull();
        body.ContainsKey("message").ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ForgotPassword_InvalidEmail_Returns400()
    {
        // Arrange
        var payload = new { email = "not-an-email" };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/forgot-password", payload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ForgotPassword_RateLimited_Returns429()
    {
        // Arrange — simulate rate limiting
        _factory!.KeycloakUserServiceMock
            .UserExistsByEmailAsync("ratelimit@example.com", Arg.Any<CancellationToken>())
            .Returns(true);
        _factory.TokenRepositoryMock
            .CountRecentTokensAsync("ratelimit@example.com", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(3); // Already at limit

        var payload = new { email = "ratelimit@example.com" };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/forgot-password", payload);

        // Assert — should be rate limited
        response.StatusCode.ShouldBe((HttpStatusCode)429);
    }
}
