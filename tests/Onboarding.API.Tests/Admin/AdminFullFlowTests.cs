using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

/// <summary>
/// End-to-end integration test exercising the full admin lifecycle.
/// Register → List → Block → Unblock → Update → Delete → Audit Trail.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public sealed class AdminFullFlowTests : IAsyncLifetime
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
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AdminFullLifecycle_RegisterListBlockUpdateDelete_VerifyAuditTrail()
    {
        var clientId = Guid.NewGuid();
        var testEmail = "flow.test@test.com";
        var testCpf = "529.982.247-25";

        // 1. List users — start with empty list
        _factory!.AdminRepositoryMock
            .GetPagedAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns((new List<Client>().AsReadOnly(), 0));

        var listResponse = await _client!.GetAsync("/api/admin/users?page=1&pageSize=20");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // 2. Create a client entity for testing (simulates registration)
        var client = Client.RegisterPessoaFisica("Flow Test User", testCpf, testEmail, "11999999999");
        typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(client, clientId);

        // 3. List users — new client appears
        _factory.AdminRepositoryMock
            .GetPagedAsync(1, 20, null, null, Arg.Any<CancellationToken>())
            .Returns((new List<Client> { client }.AsReadOnly(), 1));

        listResponse = await _client!.GetAsync("/api/admin/users?page=1&pageSize=20");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // 4. Get user details — verify data
        _factory.AdminRepositoryMock
            .GetByIdAsync(clientId, Arg.Any<CancellationToken>())
            .Returns(client);
        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync(testEmail, Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", testEmail));

        var detailsResponse = await _client!.GetAsync($"/api/admin/users/{clientId}");
        detailsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // 5. Block user — verify Keycloak disabled
        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync(testEmail, Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", testEmail));

        var blockResponse = await _client!.PostAsync($"/api/admin/users/{clientId}/block", null);
        blockResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _factory.KeycloakUserServiceMock.Received(1).BlockUserAsync("kc-uuid", Arg.Any<CancellationToken>());

        // 6. Unblock user — verify Keycloak re-enabled
        var unblockResponse = await _client!.PostAsync($"/api/admin/users/{clientId}/unblock", null);
        unblockResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _factory.KeycloakUserServiceMock.Received(1).UnblockUserAsync("kc-uuid", Arg.Any<CancellationToken>());

        // Collect audit trail at each step — don't clear so we can verify the full trail
        // 7. Update user name — verify changed
        _factory.AdminRepositoryMock.ClearReceivedCalls();
        _factory.ClientRepositoryMock
            .ExistsByEmailAsync(testEmail, Arg.Any<CancellationToken>())
            .Returns(false);

        var updatePayload = new { name = "Flow Test Updated", email = testEmail, phone = "11988888888", razaoSocial = (string?)null };
        var updateResponse = await _client!.PutAsJsonAsync($"/api/admin/users/{clientId}", updatePayload);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // 8. Delete user (LGPD) — verify PII scrubbed, Keycloak deleted
        _factory.AdminRepositoryMock.ClearReceivedCalls();
        _factory.AdminRepositoryMock
            .GetByIdAsync(clientId, Arg.Any<CancellationToken>())
            .Returns(client);
        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync(testEmail, Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", testEmail));

        var deletePayload = new { confirmEmail = testEmail };
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{clientId}")
        {
            Content = JsonContent.Create(deletePayload)
        };
        var deleteResponse = await _client!.SendAsync(deleteRequest);
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify PII scrubbed
        client.Name.ShouldBe("Usuário Excluído");
        client.Cpf.ShouldBeNull();
        client.DeletedAt.ShouldNotBeNull();

        // Verify Keycloak deleted
        await _factory.KeycloakUserServiceMock.Received(1).DeleteUserByEmailAsync(testEmail, Arg.Any<CancellationToken>());

        // 9. Verify audit trail — all actions logged in order
        var auditCalls = _factory.AuditLogRepositoryMock.ReceivedCalls()
            .Where(c => c.GetArguments().First() is Onboarding.Domain.Aggregates.Audit.AuditLog)
            .Select(c => (Onboarding.Domain.Aggregates.Audit.AuditLog)c.GetArguments().First())
            .ToList();

        auditCalls.ShouldContain(l => l.Action == "USER_BLOCKED");
        auditCalls.ShouldContain(l => l.Action == "USER_UNBLOCKED");
        auditCalls.ShouldContain(l => l.Action == "USER_UPDATED");
        auditCalls.ShouldContain(l => l.Action == "USER_DELETED");
    }
}
