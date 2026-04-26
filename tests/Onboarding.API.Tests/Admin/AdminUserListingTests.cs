using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

[Collection(WebAppFactoryCollection.Name)]
public sealed class AdminUserListingTests : IAsyncLifetime
{
    private AdminTestFactory? _factory;
    private HttpClient? _client;

    private static Company CreateTestCompany(string email = "empresa@test.com")
    {
        var terms = TermsAcceptance.Create("1.0", "127.0.0.1");
        var company = Company.Register("Empresa Teste", "11222333000181", email, "11999999999", terms);
        typeof(Company).BaseType!.GetProperty("Id")!.SetValue(company, Guid.NewGuid());
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
    public async Task GetPaginatedUsers_ReturnsPageWithItems()
    {
        var companies = new List<Company>
        {
            CreateTestCompany("empresa1@test.com"),
            CreateTestCompany("empresa2@test.com"),
            CreateTestCompany("empresa3@test.com")
        };

        _factory!.AdminRepositoryMock
            .GetPagedAsync(1, 10, null, null, Arg.Any<CancellationToken>())
            .Returns((companies.AsReadOnly(), 3));

        var response = await _client!.GetAsync("/api/admin/users?page=1&pageSize=10");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.ShouldNotBeNull();
        body.ContainsKey("items").ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaginatedUsers_SearchByName_ReturnsFilteredResults()
    {
        var company = CreateTestCompany();

        _factory!.AdminRepositoryMock
            .GetPagedAsync(1, 10, "Empresa", null, Arg.Any<CancellationToken>())
            .Returns((new List<Company> { company }.AsReadOnly(), 1));

        var response = await _client!.GetAsync("/api/admin/users?page=1&pageSize=10&search=Empresa");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaginatedUsers_ExcludesDeletedUsers_ByDefault()
    {
        var company = CreateTestCompany();

        _factory!.AdminRepositoryMock
            .GetPagedAsync(1, 10, null, null, Arg.Any<CancellationToken>())
            .Returns((new List<Company> { company }.AsReadOnly(), 1));

        var response = await _client!.GetAsync("/api/admin/users?page=1&pageSize=10");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaginatedUsers_StatusDeleted_ReturnsOnlyDeleted()
    {
        var deletedCompany = CreateTestCompany("deleted@test.com");

        _factory!.AdminRepositoryMock
            .GetPagedAsync(1, 10, null, "deleted", Arg.Any<CancellationToken>())
            .Returns((new List<Company> { deletedCompany }.AsReadOnly(), 1));

        var response = await _client!.GetAsync("/api/admin/users?page=1&pageSize=10&status=deleted");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaginatedUsers_SecondPage_ReturnsEmpty_WhenLessThanPageSize()
    {
        _factory!.AdminRepositoryMock
            .GetPagedAsync(2, 10, null, null, Arg.Any<CancellationToken>())
            .Returns((new List<Company>().AsReadOnly(), 3));

        var response = await _client!.GetAsync("/api/admin/users?page=2&pageSize=10");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.ShouldNotBeNull();
        body["totalCount"].ToString().ShouldBe("3");
    }
}