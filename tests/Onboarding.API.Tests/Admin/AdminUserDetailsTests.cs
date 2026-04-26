using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

[Collection(WebAppFactoryCollection.Name)]
public sealed class AdminUserDetailsTests : IAsyncLifetime
{
    private AdminTestFactory? _factory;
    private HttpClient? _client;

    private static Company CreateTestCompany(Guid? id = null, string email = "empresa@test.com")
    {
        var terms = TermsAcceptance.Create("1.0", "127.0.0.1");
        var company = Company.Register("Empresa Teste", "11222333000181", email, "11999999999", terms);
        if (id.HasValue)
            typeof(Company).BaseType!.GetProperty("Id")!.SetValue(company, id.Value);
        return company;
    }

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

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUserDetails_ValidId_ReturnsFullData()
    {
        var companyId = Guid.NewGuid();
        var company = CreateTestCompany(companyId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(company);

        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("client", "empresa@test.com", Arg.Any<CancellationToken>())
            .Returns(new KeycloakUser("kc-uuid", "empresa@test.com"));

        var response = await _client!.GetAsync($"/api/admin/users/{companyId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserDetailDto>();
        body.ShouldNotBeNull();
        body.RazaoSocial.ShouldBe("Empresa Teste");
        body.Email.ShouldBe("empresa@test.com");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUserDetails_InvalidId_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        _factory!.AdminRepositoryMock
            .GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>())
            .Returns((Company?)null);

        var response = await _client!.GetAsync($"/api/admin/users/{nonExistentId}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUserDetails_Company_ReturnsCnpj()
    {
        var companyId = Guid.NewGuid();
        var company = CreateTestCompany(companyId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(company);

        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("client", "empresa@test.com", Arg.Any<CancellationToken>())
            .Returns(new KeycloakUser("kc-uuid", "empresa@test.com"));

        var response = await _client!.GetAsync($"/api/admin/users/{companyId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserDetailDto>();
        body.ShouldNotBeNull();
        body.Cnpj.ShouldNotBeNull();
    }
}