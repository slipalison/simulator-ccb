using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Domain.Aggregates.ClientAggregate;
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
        // Arrange
        var clientId = Guid.NewGuid();
        var client = Client.RegisterPessoaFisica("Joao Silva", "529.982.247-25", "joao@test.com", "11999999999");
        typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(client, clientId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(clientId, Arg.Any<CancellationToken>())
            .Returns(client);

        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("joao@test.com", Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", "joao@test.com"));

        var payload = new { confirmEmail = "joao@test.com" };

        // Act
        var response = await _client!.PostAsJsonAsync($"/api/admin/users/{clientId}/delete", payload);

        // Note: The controller uses HttpDelete with body, but HttpClient may not send it correctly.
        // Let me use SendAsync with DELETE method and body.
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{clientId}")
        {
            Content = JsonContent.Create(payload)
        };
        response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify PII was scrubbed (Anonymize was called)
        client.Name.ShouldBe("Usuário Excluído");
        client.Cpf.ShouldBeNull();
        client.DeletedAt.ShouldNotBeNull();

        // Verify audit log
        await _factory.AuditLogRepositoryMock.Received(1).AddAsync(
            Arg.Any<Onboarding.Domain.Aggregates.Audit.AuditLog>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteUser_WrongEmail_ReturnsBadRequest()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var client = Client.RegisterPessoaFisica("Joao Silva", "529.982.247-25", "joao@test.com", "11999999999");
        typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(client, clientId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(clientId, Arg.Any<CancellationToken>())
            .Returns(client);

        var payload = new { confirmEmail = "wrong@email.com" };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{clientId}")
        {
            Content = JsonContent.Create(payload)
        };
        var response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteUser_AlreadyDeleted_ReturnsConflict()
    {
        // Arrange — client already deleted
        var clientId = Guid.NewGuid();
        var client = Client.RegisterPessoaFisica("Joao Silva", "529.982.247-25", "joao@test.com", "11999999999");
        typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(client, clientId);
        client.Anonymize(); // Already deleted — email is now deleted-{id}@internal.local

        _factory!.AdminRepositoryMock
            .GetByIdAsync(clientId, Arg.Any<CancellationToken>())
            .Returns(client);

        var payload = new { confirmEmail = client.Email.Value };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{clientId}")
        {
            Content = JsonContent.Create(payload)
        };
        var response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteUser_RemovesFromKeycloak()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var client = Client.RegisterPessoaFisica("Joao Silva", "529.982.247-25", "joao@test.com", "11999999999");
        typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(client, clientId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(clientId, Arg.Any<CancellationToken>())
            .Returns(client);

        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("joao@test.com", Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", "joao@test.com"));

        var payload = new { confirmEmail = "joao@test.com" };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{clientId}")
        {
            Content = JsonContent.Create(payload)
        };
        var response = await _client!.SendAsync(request);

        // Assert — Keycloak delete was called with original email
        await _factory.KeycloakUserServiceMock.Received(1).DeleteUserByEmailAsync("joao@test.com", Arg.Any<CancellationToken>());
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteUser_AuditLogWithPiiSnapshot()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var client = Client.RegisterPessoaFisica("Joao Silva", "529.982.247-25", "joao@test.com", "11999999999");
        typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(client, clientId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(clientId, Arg.Any<CancellationToken>())
            .Returns(client);

        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("joao@test.com", Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", "joao@test.com"));

        var payload = new { confirmEmail = "joao@test.com" };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{clientId}")
        {
            Content = JsonContent.Create(payload)
        };
        await _client!.SendAsync(request);

        // Assert — audit log with snapshot before/after
        await _factory.AuditLogRepositoryMock.Received(1).AddAsync(
            Arg.Is<Onboarding.Domain.Aggregates.Audit.AuditLog>(log =>
                log.Action == "USER_DELETED" &&
                log.SnapshotBefore != null &&
                log.SnapshotAfter != null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteUser_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _factory!.AdminRepositoryMock
            .GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>())
            .Returns((Client?)null);

        var payload = new { confirmEmail = "any@email.com" };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/users/{nonExistentId}")
        {
            Content = JsonContent.Create(payload)
        };
        var response = await _client!.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
