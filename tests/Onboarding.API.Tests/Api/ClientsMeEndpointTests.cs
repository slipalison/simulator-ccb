using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using Onboarding.API.Tests.Authentication;

namespace Onboarding.API.Tests.Api;

/// <summary>
/// RED stubs for AUTH-03: GET /api/clients/me returns 200 with valid JWT.
/// Test name matches VALIDATION.md task 06-01-03: ClientsMeEndpointTests.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class ClientsMeEndpointTests : IAsyncLifetime
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
    public async Task GetMe_WithValidToken_Returns200WithClientProfile()
    {
        // RED stub — implement when GET /api/clients/me + GetByEmailAsync + ClientsController exist in Plan 03
        true.ShouldBeFalse("RED stub — not implemented yet");
        await Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetMe_WithoutToken_Returns401()
    {
        // RED stub — implement when [Authorize] middleware is wired in Plan 02
        true.ShouldBeFalse("RED stub — not implemented yet");
        await Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetMe_WhenClientNotFoundInDb_Returns404()
    {
        // RED stub — implement when D-09 (404 ProblemDetails genérico) exists in Plan 03
        true.ShouldBeFalse("RED stub — not implemented yet");
        await Task.CompletedTask;
    }
}
