using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

[Collection(WebAppFactoryCollection.Name)]
public sealed class AdminUserUpdateTests : IAsyncLifetime
{
    private AdminTestFactory? _factory;
    private HttpClient? _client;

    private static Company CreateTestCompany(Guid id, string email = "empresa@test.com")
    {
        var terms = TermsAcceptance.Create("1.0", "127.0.0.1");
        var company = Company.Register("Empresa Teste", "11222333000181", email, "11999999999", terms);
        typeof(Company).BaseType!.GetProperty("Id")!.SetValue(company, id);
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
    public async Task UpdateUser_ValidData_ReturnsNoContent()
    {
        var companyId = Guid.NewGuid();
        var company = CreateTestCompany(companyId);

        _factory!.AdminRepositoryMock.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);
        _factory.CompanyRepositoryMock
            .ExistsByEmailAsync("updated@test.com", Arg.Any<CancellationToken>()).Returns(false);

        var payload = new { razaoSocial = "Empresa Updated", email = "updated@test.com", phone = "11988888888" };
        var response = await _client!.PutAsJsonAsync($"/api/admin/users/{companyId}", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await _factory.AuditServiceMock.Received(1).RecordAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ActionType>(),
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateUser_DuplicateEmail_ReturnsConflict()
    {
        var companyId = Guid.NewGuid();
        var company = CreateTestCompany(companyId);

        _factory!.AdminRepositoryMock.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);
        _factory.CompanyRepositoryMock
            .ExistsByEmailAsync("other@test.com", Arg.Any<CancellationToken>()).Returns(true);

        var payload = new { razaoSocial = "Empresa", email = "other@test.com", phone = "11999999999" };
        var response = await _client!.PutAsJsonAsync($"/api/admin/users/{companyId}", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateUser_InvalidName_ReturnsUnprocessableEntity()
    {
        var companyId = Guid.NewGuid();
        var payload = new { razaoSocial = "", email = "empresa@test.com", phone = "11999999999" };
        var response = await _client!.PutAsJsonAsync($"/api/admin/users/{companyId}", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateUser_NonExistentId_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        _factory!.AdminRepositoryMock.GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>()).Returns((Company?)null);

        var payload = new { razaoSocial = "Empresa", email = "empresa@test.com", phone = "11999999999" };
        var response = await _client!.PutAsJsonAsync($"/api/admin/users/{nonExistentId}", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateUser_AuditLogCreated()
    {
        var companyId = Guid.NewGuid();
        var company = CreateTestCompany(companyId);

        _factory!.AdminRepositoryMock.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);
        _factory.CompanyRepositoryMock
            .ExistsByEmailAsync("empresa@test.com", Arg.Any<CancellationToken>()).Returns(false);

        var payload = new { razaoSocial = "Empresa Updated", email = "empresa@test.com", phone = "11999999999" };
        await _client!.PutAsJsonAsync($"/api/admin/users/{companyId}", payload);

        await _factory.AuditServiceMock.Received(1).RecordAsync(
            Arg.Any<string>(), Arg.Any<string>(), ActionType.UserUpdated,
            companyId, Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}