using System.Net;
using System.Net.Http.Headers;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

[Collection(WebAppFactoryCollection.Name)]
public sealed class AdminUserBlockTests : IAsyncLifetime
{
    private AdminTestFactory? _factory;
    private HttpClient? _client;

    private static Company CreateTestCompany(Guid id) =>
        CreateTestCompanyWith(id, "empresa@test.com");

    private static Company CreateTestCompanyWith(Guid id, string email)
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
    public async Task BlockUser_ReturnsNoContent_DisablesKeycloak()
    {
        var companyId = Guid.NewGuid();
        var company = CreateTestCompany(companyId);

        _factory!.AdminRepositoryMock.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);
        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("client", "empresa@test.com", Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", "empresa@test.com"));

        var response = await _client!.PostAsync($"/api/admin/users/{companyId}/block", null);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _factory.KeycloakUserServiceMock.Received(1).BlockUserAsync("client", "kc-uuid", Arg.Any<CancellationToken>());
        await _factory.AuditServiceMock.Received(1).RecordAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ActionType>(),
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UnblockUser_ReturnsNoContent_EnablesKeycloak()
    {
        var companyId = Guid.NewGuid();
        var company = CreateTestCompany(companyId);

        _factory!.AdminRepositoryMock.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);
        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("client", "empresa@test.com", Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", "empresa@test.com"));

        var response = await _client!.PostAsync($"/api/admin/users/{companyId}/unblock", null);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _factory.KeycloakUserServiceMock.Received(1).UnblockUserAsync("client", "kc-uuid", Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BlockUser_AlreadyBlocked_NoOp_ReturnsNoContent()
    {
        var companyId = Guid.NewGuid();
        var company = CreateTestCompany(companyId);

        _factory!.AdminRepositoryMock.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);
        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("client", "empresa@test.com", Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", "empresa@test.com"));

        await _client!.PostAsync($"/api/admin/users/{companyId}/block", null);
        var response = await _client.PostAsync($"/api/admin/users/{companyId}/block", null);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BlockUser_NonExistentId_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        _factory!.AdminRepositoryMock.GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>()).Returns((Company?)null);

        var response = await _client!.PostAsync($"/api/admin/users/{nonExistentId}/block", null);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BlockUnblock_AuditLogCreated()
    {
        var companyId = Guid.NewGuid();
        var company = CreateTestCompany(companyId);

        _factory!.AdminRepositoryMock.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);
        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("client", "empresa@test.com", Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", "empresa@test.com"));

        await _client!.PostAsync($"/api/admin/users/{companyId}/block", null);
        await _client.PostAsync($"/api/admin/users/{companyId}/unblock", null);

        await _factory.AuditServiceMock.Received(1).RecordAsync(
            Arg.Any<string>(), Arg.Any<string>(), ActionType.UserBlocked,
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _factory.AuditServiceMock.Received(1).RecordAsync(
            Arg.Any<string>(), Arg.Any<string>(), ActionType.UserUnblocked,
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}