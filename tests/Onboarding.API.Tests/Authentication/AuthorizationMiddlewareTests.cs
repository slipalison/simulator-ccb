using Shouldly;
using System.Net;

namespace Onboarding.API.Tests.Authentication;

/// <summary>
/// RED stubs for AUTH-02: [Authorize] returns 401 without Bearer token.
/// Test name matches VALIDATION.md task 06-01-02: AuthorizationMiddlewareTests.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class AuthorizationMiddlewareTests : IAsyncLifetime
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
    [Trait("Category", "Unit")]
    public async Task GetClientsMe_WithoutToken_Returns401()
    {
        // RED stub — implement when GET /api/clients/me exists with [Authorize] in Plan 03
        true.ShouldBeFalse("RED stub — not implemented yet");
        await Task.CompletedTask;
    }
}
