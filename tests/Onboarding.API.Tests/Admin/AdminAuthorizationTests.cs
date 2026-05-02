using System.Net;
using System.Net.Http.Headers;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

[Collection(WebAppFactoryCollection.Name)]
public sealed class AdminAuthorizationTests : IAsyncLifetime
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

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _adminClient?.Dispose();
        _nonAdminClient?.Dispose();
        _unauthenticatedClient?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetCompanies_WithNonAdminToken_ReturnsForbidden()
    {
        var response = await _nonAdminClient!.GetAsync("/api/admin/companies?page=1&pageSize=10");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetCompanies_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _unauthenticatedClient!.GetAsync("/api/admin/companies?page=1&pageSize=10");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BlockEmployee_WithNonAdminToken_ReturnsForbidden()
    {
        var response = await _nonAdminClient!.PostAsync("/api/admin/employees/00000000-0000-0000-0000-000000000001/block", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAdministrators_WithNonAdminToken_ReturnsForbidden()
    {
        var response = await _nonAdminClient!.GetAsync("/api/admin/administrators");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateAdmin_WithNonAdminToken_ReturnsForbidden()
    {
        var content = new StringContent("{\"fullName\":\"Test\",\"email\":\"test@test.com\"}", System.Text.Encoding.UTF8, "application/json");
        var response = await _nonAdminClient!.PostAsync("/api/admin/administrators", content);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}