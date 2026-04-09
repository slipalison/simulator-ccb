using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using System.Net;

namespace Onboarding.API.Tests.Authentication;

/// <summary>
/// Tests for [Authorize] middleware behavior — returns 401 without Bearer token.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class AuthorizationMiddlewareTests : IAsyncLifetime
{
    private AuthTestApiFactory? _factory;
    private HttpClient? _client;

    public Task InitializeAsync()
    {
        _factory = new AuthTestApiFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
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
        // D-03: [Authorize] on GET /api/clients/me — no Bearer = 401
        var response = await _client!.GetAsync("/api/clients/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
