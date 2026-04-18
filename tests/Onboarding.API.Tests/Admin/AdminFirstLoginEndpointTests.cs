using System.Net;
using System.Net.Http.Headers;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Application.Common;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

/// <summary>
/// Integration tests for POST /api/admin/me/complete-first-login endpoint.
/// Tests: 401 without token, 403 with non-admin token, 204 with admin token.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AdminFirstLoginEndpointTests : IAsyncLifetime
{
    private AdminTestFactory? _factory;
    private HttpClient? _adminClient;
    private HttpClient? _nonAdminClient;
    private HttpClient? _unauthenticatedClient;

    public Task InitializeAsync()
    {
        _factory = new AdminTestFactory();

        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", FakeJwtTokenHelper.GenerateAdminJwt());

        _nonAdminClient = _factory.CreateClient();
        _nonAdminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", FakeJwtTokenHelper.GenerateNonAdminJwt());

        _unauthenticatedClient = _factory.CreateClient();

        // Setup mock: GetUserByEmailAsync returns a valid user so the endpoint can resolve user ID
        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<KeycloakUser?>(new KeycloakUser("user-uuid", "admin@test.com")));

        // Setup mock: ClearFirstLoginFlagAsync is a no-op (returns completed task)
        _factory.KeycloakUserServiceMock
            .ClearFirstLoginFlagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _adminClient?.Dispose();
        _nonAdminClient?.Dispose();
        _unauthenticatedClient?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CompleteFirstLogin_WithoutToken_Returns401()
    {
        // Act
        var response = await _unauthenticatedClient!.PostAsync("/api/admin/me/complete-first-login", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CompleteFirstLogin_WithNonAdminToken_Returns403()
    {
        // Act
        var response = await _nonAdminClient!.PostAsync("/api/admin/me/complete-first-login", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CompleteFirstLogin_WithAdminToken_Returns204_AndCallsClearFirstLoginFlagAsync()
    {
        // Act
        var response = await _adminClient!.PostAsync("/api/admin/me/complete-first-login", null);

        // Assert — 204 No Content
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Assert — ClearFirstLoginFlagAsync was called with the resolved user ID
        await _factory!.KeycloakUserServiceMock.Received(1)
            .ClearFirstLoginFlagAsync("user-uuid", Arg.Any<CancellationToken>());
    }
}
