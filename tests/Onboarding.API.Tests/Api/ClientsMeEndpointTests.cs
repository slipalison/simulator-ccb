using Microsoft.AspNetCore.Mvc.Testing;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.API.Tests.Authentication;
using NSubstitute;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Onboarding.API.Tests.Api;

/// <summary>
/// GREEN tests for AUTH-03: GET /api/clients/me behavior with JWT.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class ClientsMeEndpointTests : IAsyncLifetime
{
    private AuthTestApiFactory? _factory;
    private HttpClient? _client;

    private const string TestEmail = "joao@example.com";

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
    [Trait("Category", "Integration")]
    public async Task GetMe_WithValidToken_Returns200WithClientProfile()
    {
        // Arrange — mock repository returns a PF client for the authenticated email
        var client = Client.RegisterPessoaFisica("João Silva", "529.982.247-25", TestEmail, "11999999999");
        _factory!.RepositoryMock
            .GetByEmailAsync(TestEmail.ToLowerInvariant(), Arg.Any<CancellationToken>())
            .Returns(client);

        _client!.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", FakeJwtTokenHelper.GenerateFakeJwt(TestEmail));

        // Act
        var response = await _client.GetAsync("/api/clients/me");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.ShouldNotBeNull();
        body.ContainsKey("email").ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetMe_WithoutToken_Returns401()
    {
        // No Authorization header
        var response = await _client!.GetAsync("/api/clients/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetMe_WhenClientNotFoundInDb_Returns404()
    {
        // Arrange — repository returns null (client authenticated but not in app_db)
        _factory!.RepositoryMock
            .GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Client?)null);

        _client!.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", FakeJwtTokenHelper.GenerateFakeJwt(TestEmail));

        // Act
        var response = await _client.GetAsync("/api/clients/me");

        // Assert — D-09: 404 with generic ProblemDetails (not "user does not exist")
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
