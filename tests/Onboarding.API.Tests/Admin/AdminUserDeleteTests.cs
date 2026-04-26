using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

/// <summary>
/// Integration tests for DELETE /api/admin/users/{id} — LGPD-compliant deletion (ADMIN-05).
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public sealed class AdminUserDeleteTests : IAsyncLifetime
{
    private AdminTestFactory? _factory;
    private HttpClient? _client;

    private static Company CreateTestCompany(Guid? id = null)
    {
        var terms = TermsAcceptance.Create("1.0", "127.0.0.1");
        var company = Company.Register("Empresa Teste", "11222333000181", "empresa@test.com", "11999999999", terms);
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
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteUser_CorrectEmail_ReturnsNoContent_PiiScrubbed()
    {
        var companyId = Guid.NewGuid();
        var company = CreateTestCompany(companyId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(company);

        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("client", "empresa@test.com", Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", "empresa@test.com"));

        var payload = new { confirmEmail = "empresa@test.com" };

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{companyId}")
        {
            Content = JsonContent.Create(payload)
        };
        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        company.RazaoSocial.ShouldBe("Empresa Excluída");
        company.Cnpj.ShouldBeNull();
        company.DeletedAt.ShouldNotBeNull();

        await _factory.AuditServiceMock.Received(1).RecordAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ActionType>(),
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteUser_WrongEmail_ReturnsBadRequest()
    {
        var companyId = Guid.NewGuid();
        var company = CreateTestCompany(companyId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(company);

        var payload = new { confirmEmail = "wrong@email.com" };

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{companyId}")
        {
            Content = JsonContent.Create(payload)
        };
        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteUser_AlreadyDeleted_ReturnsConflict()
    {
        var companyId = Guid.NewGuid();
        var company = CreateTestCompany(companyId);
        company.Anonymize();

        _factory!.AdminRepositoryMock
            .GetByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(company);

        var payload = new { confirmEmail = company.Email.Value };

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{companyId}")
        {
            Content = JsonContent.Create(payload)
        };
        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteUser_RemovesFromKeycloak()
    {
        var companyId = Guid.NewGuid();
        var company = CreateTestCompany(companyId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(company);

        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("client", "empresa@test.com", Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", "empresa@test.com"));

        var payload = new { confirmEmail = "empresa@test.com" };

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{companyId}")
        {
            Content = JsonContent.Create(payload)
        };
        var response = await _client!.SendAsync(request);

        await _factory.KeycloakUserServiceMock.Received(1).DeleteUserByEmailAsync("client", "empresa@test.com", Arg.Any<CancellationToken>());
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteUser_AuditLogWithPiiSnapshot()
    {
        var companyId = Guid.NewGuid();
        var company = CreateTestCompany(companyId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(companyId, Arg.Any<CancellationToken>())
            .Returns(company);

        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("client", "empresa@test.com", Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", "empresa@test.com"));

        var payload = new { confirmEmail = "empresa@test.com" };

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{companyId}")
        {
            Content = JsonContent.Create(payload)
        };
        await _client!.SendAsync(request);

        await _factory.AuditServiceMock.Received(1).RecordAsync(
            Arg.Any<string>(), Arg.Any<string>(), ActionType.UserDeleted,
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteUser_NonExistentId_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        _factory!.AdminRepositoryMock
            .GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>())
            .Returns((Company?)null);

        var payload = new { confirmEmail = "any@email.com" };

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{nonExistentId}")
        {
            Content = JsonContent.Create(payload)
        };
        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}