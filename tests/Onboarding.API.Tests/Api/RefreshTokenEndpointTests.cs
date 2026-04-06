using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Onboarding.API.Tests.Authentication;

namespace Onboarding.API.Tests.Api;

/// <summary>
/// RED stubs for AUTH-04: POST /api/auth/refresh returns new access_token.
/// Test name matches VALIDATION.md task 06-02-02: RefreshTokenEndpointTests.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class RefreshTokenEndpointTests : IAsyncLifetime
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
    public async Task Refresh_WithValidRefreshToken_Returns200WithNewTokens()
    {
        // RED stub — implement when POST /api/auth/refresh + AuthController exist in Plan 03
        true.ShouldBeFalse("RED stub — not implemented yet");
        await Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Refresh_WithInvalidRefreshToken_Returns401WithGenericMessage()
    {
        // RED stub — D-13: SEC-08 generic message for expired/invalid refresh token
        true.ShouldBeFalse("RED stub — not implemented yet");
        await Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Refresh_WithMissingRefreshToken_Returns422()
    {
        // RED stub — FluentValidation: refresh_token required
        true.ShouldBeFalse("RED stub — not implemented yet");
        await Task.CompletedTask;
    }
}
