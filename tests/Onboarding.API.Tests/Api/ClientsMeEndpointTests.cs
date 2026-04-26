using Microsoft.AspNetCore.Mvc.Testing;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.API.Tests.Authentication;
using NSubstitute;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Onboarding.API.Tests.Api;

/// <summary>
/// Tests for GET /api/companies/me behavior with JWT authentication (AUTH-03).
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class ClientsMeEndpointTests : IAsyncLifetime
{
    private AuthTestApiFactory? _factory;
    private HttpClient? _client;

    private const string TestEmail = "empresa@example.com";
    private const string TestSub = "d3f1a2b4-5678-4c9d-a012-e3f4567890ab";

    public Task InitializeAsync()
    {
        _factory = new AuthTestApiFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetMe_WithValidToken_Returns200WithCompanyProfile()
    {
        var terms = TermsAcceptance.Create("1.0", "127.0.0.1");
        var company = Company.Register("Empresa Teste", "11222333000181", TestEmail, "11999999999", terms);
        _factory!.RepositoryMock
            .GetByKeycloakSubAsync(TestSub, Arg.Any<CancellationToken>())
            .Returns(company);

        _client!.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", FakeJwtTokenHelper.GenerateFakeJwt(TestEmail, TestSub));

        var response = await _client.GetAsync("/api/companies/me");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.ShouldNotBeNull();
        body.ContainsKey("email").ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await _client!.GetAsync("/api/companies/me");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetMe_WhenCompanyNotFoundInDb_Returns404()
    {
        _factory!.RepositoryMock
            .GetByKeycloakSubAsync(TestSub, Arg.Any<CancellationToken>())
            .Returns((Company?)null);

        _client!.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", FakeJwtTokenHelper.GenerateFakeJwt(TestEmail, TestSub));

        var response = await _client.GetAsync("/api/companies/me");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}