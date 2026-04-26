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
    public async Task GetAdminUsers_WithAdminToken_ReturnsOk()
    {
        _factory!.AdminRepositoryMock
            .GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<Company>().AsReadOnly(), 0));

        var response = await _adminClient!.GetAsync("/api/admin/users?page=1&pageSize=10");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAdminUsers_WithNonAdminToken_ReturnsForbidden()
    {
        var response = await _nonAdminClient!.GetAsync("/api/admin/users?page=1&pageSize=10");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAdminUsers_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _unauthenticatedClient!.GetAsync("/api/admin/users?page=1&pageSize=10");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BlockUser_WithNonAdminToken_ReturnsForbidden()
    {
        var response = await _nonAdminClient!.PostAsync("/api/admin/users/00000000-0000-0000-0000-000000000001/block", null);
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
    public async Task GetAdministrators_WithAdminToken_ReturnsOk()
    {
        var response = await _adminClient!.GetAsync("/api/admin/administrators");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateAdmin_WithNonAdminToken_ReturnsForbidden()
    {
        var content = new StringContent("{\"fullName\":\"Test\",\"email\":\"test@test.com\"}", System.Text.Encoding.UTF8, "application/json");
        var response = await _nonAdminClient!.PostAsync("/api/admin/administrators", content);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateAdmin_OldRoute_ReturnsMethodNotAllowed()
    {
        var content = new StringContent("{\"fullName\":\"Test\",\"email\":\"test@test.com\"}", System.Text.Encoding.UTF8, "application/json");
        var response = await _adminClient!.PostAsync("/api/admin/users", content);
        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }
}