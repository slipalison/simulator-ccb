using System.Net;
using System.Net.Http.Headers;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Application.Common;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

[Collection(WebAppFactoryCollection.Name)]
public sealed class AdminCompanyDetailsTests : IAsyncLifetime
{
    private AdminTestFactory? _factory;
    private HttpClient? _client;

    public Task InitializeAsync()
    {
        _factory = new AdminTestFactory();
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", FakeJwtTokenHelper.GenerateAdminJwt());
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
    }

    [Fact(Skip = "Stub handler — full implementation in Phase 38")]
    [Trait("Category", "Integration")]
    public async Task GetCompanyDetails_ValidId_ReturnsData()
    {
        var companyId = Guid.NewGuid();
        var response = await _client!.GetAsync($"/api/admin/companies/{companyId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(Skip = "Stub handler — full implementation in Phase 38")]
    [Trait("Category", "Integration")]
    public async Task GetCompanyDetails_InvalidId_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await _client!.GetAsync($"/api/admin/companies/{nonExistentId}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}