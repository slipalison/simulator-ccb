using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

[Collection(WebAppFactoryCollection.Name)]
public sealed class AdminFullFlowTests : IAsyncLifetime
{
    private AdminTestFactory? _factory;
    private HttpClient? _client;

    private static Company CreateTestCompany(Guid id, string email)
    {
        var terms = TermsAcceptance.Create("1.0", "127.0.0.1");
        var company = Company.Register("Empresa Flow Test", "11222333000181", email, "11999999999", terms);
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
    public async Task AdminFullLifecycle_RegisterListBlockUpdateDelete_VerifyAuditTrail()
    {
        var companyId = Guid.NewGuid();
        var testEmail = "flow.test@test.com";
        var company = CreateTestCompany(companyId, testEmail);

        // 1. List users — start with empty list
        _factory!.AdminRepositoryMock
            .GetPagedAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns((new List<Company>().AsReadOnly(), 0));

        var listResponse = await _client!.GetAsync("/api/admin/users?page=1&pageSize=20");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // 2. List users — new company appears
        _factory.AdminRepositoryMock
            .GetPagedAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns((new List<Company> { company }.AsReadOnly(), 1));

        listResponse = await _client!.GetAsync("/api/admin/users?page=1&pageSize=20");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // 3. Get company details
        _factory.AdminRepositoryMock.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);
        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("client", testEmail, Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", testEmail));

        var detailsResponse = await _client!.GetAsync($"/api/admin/users/{companyId}");
        detailsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // 4. Block
        var blockResponse = await _client!.PostAsync($"/api/admin/users/{companyId}/block", null);
        blockResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _factory.KeycloakUserServiceMock.Received(1).BlockUserAsync("client", "kc-uuid", Arg.Any<CancellationToken>());

        // 5. Unblock
        var unblockResponse = await _client!.PostAsync($"/api/admin/users/{companyId}/unblock", null);
        unblockResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _factory.KeycloakUserServiceMock.Received(1).UnblockUserAsync("client", "kc-uuid", Arg.Any<CancellationToken>());

        // 6. Update
        _factory.AdminRepositoryMock.ClearReceivedCalls();
        _factory.CompanyRepositoryMock
            .ExistsByEmailAsync(testEmail, Arg.Any<CancellationToken>()).Returns(false);
        _factory.AdminRepositoryMock.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);

        var updatePayload = new { razaoSocial = "Flow Updated", email = testEmail, phone = "11988888888" };
        var updateResponse = await _client!.PutAsJsonAsync($"/api/admin/users/{companyId}", updatePayload);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // 7. Delete
        _factory.AdminRepositoryMock.ClearReceivedCalls();
        _factory.AdminRepositoryMock.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);
        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("client", testEmail, Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", testEmail));

        var deletePayload = new { confirmEmail = testEmail };
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{companyId}")
        {
            Content = JsonContent.Create(deletePayload)
        };
        var deleteResponse = await _client!.SendAsync(deleteRequest);
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        company.RazaoSocial.ShouldBe("Empresa Excluída");
        company.Cnpj.ShouldBeNull();
        company.DeletedAt.ShouldNotBeNull();

        await _factory.KeycloakUserServiceMock.Received(1).DeleteUserByEmailAsync("client", testEmail, Arg.Any<CancellationToken>());

        await _factory!.AuditServiceMock.Received(1).RecordAsync(
            Arg.Any<string>(), Arg.Any<string>(), ActionType.UserBlocked,
            companyId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());

        await _factory.AuditServiceMock.Received(1).RecordAsync(
            Arg.Any<string>(), Arg.Any<string>(), ActionType.UserUnblocked,
            companyId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());

        await _factory.AuditServiceMock.Received(1).RecordAsync(
            Arg.Any<string>(), Arg.Any<string>(), ActionType.UserUpdated,
            companyId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());

        await _factory.AuditServiceMock.Received(1).RecordAsync(
            Arg.Any<string>(), Arg.Any<string>(), ActionType.UserDeleted,
            companyId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}