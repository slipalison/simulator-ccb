using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Onboarding.API.Tests.Authentication;

namespace Onboarding.API.Tests.Api;

/// <summary>
/// RED stubs for AUTH-02/AUTH-04: POST /api/auth/login returns access_token + refresh_token.
/// Test name matches VALIDATION.md task 06-02-01: LoginEndpointTests.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class LoginEndpointTests : IAsyncLifetime
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
    public async Task Login_WithValidCredentials_Returns200WithTokens()
    {
        // RED stub — implement when POST /api/auth/login + AuthController + IKeycloakTokenService exist in Plan 03
        // Expected: { access_token, refresh_token, expires_in, token_type } in response body
        true.ShouldBeFalse("RED stub — not implemented yet");
        await Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Login_WithInvalidCredentials_Returns401WithGenericMessage()
    {
        // RED stub — D-13: SEC-08 generic message, no user enumeration
        true.ShouldBeFalse("RED stub — not implemented yet");
        await Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Login_WithMissingEmail_Returns422()
    {
        // RED stub — FluentValidation: email required
        true.ShouldBeFalse("RED stub — not implemented yet");
        await Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Login_WithMissingPassword_Returns422()
    {
        // RED stub — FluentValidation: password required
        true.ShouldBeFalse("RED stub — not implemented yet");
        await Task.CompletedTask;
    }
}
