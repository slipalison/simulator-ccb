using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

/// <summary>
/// Integration tests for POST /api/admin/users/{id}/block and /unblock (ADMIN-04).
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public sealed class AdminUserBlockTests : IAsyncLifetime
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
    public async Task BlockUser_ReturnsNoContent_DisablesKeycloak()
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

        // Act
        var response = await _client!.PostAsync($"/api/admin/users/{clientId}/block", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify Keycloak block was called
        await _factory.KeycloakUserServiceMock.Received(1).BlockUserAsync("kc-uuid", Arg.Any<CancellationToken>());

        // Verify audit log
        await _factory.AuditLogRepositoryMock.Received(1).AddAsync(
            Arg.Any<Onboarding.Domain.Aggregates.Audit.AuditLog>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UnblockUser_ReturnsNoContent_EnablesKeycloak()
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

        // Act
        var response = await _client!.PostAsync($"/api/admin/users/{clientId}/unblock", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify Keycloak unblock was called
        await _factory.KeycloakUserServiceMock.Received(1).UnblockUserAsync("kc-uuid", Arg.Any<CancellationToken>());

        // Verify audit log
        await _factory.AuditLogRepositoryMock.Received(1).AddAsync(
            Arg.Any<Onboarding.Domain.Aggregates.Audit.AuditLog>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BlockUser_AlreadyBlocked_NoOp_ReturnsNoContent()
    {
        // Arrange — mock BlockUserAsync to succeed (idempotent in KeycloakUserService)
        var clientId = Guid.NewGuid();
        var client = Client.RegisterPessoaFisica("Joao Silva", "529.982.247-25", "joao@test.com", "11999999999");
        typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(client, clientId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(clientId, Arg.Any<CancellationToken>())
            .Returns(client);

        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("joao@test.com", Arg.Any<CancellationToken>())
            .Returns(new Application.Common.KeycloakUser("kc-uuid", "joao@test.com"));

        // First block
        await _client!.PostAsync($"/api/admin/users/{clientId}/block", null);

        // Second block — should still return 204 (idempotent)
        var response = await _client.PostAsync($"/api/admin/users/{clientId}/block", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BlockUser_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _factory!.AdminRepositoryMock
            .GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>())
            .Returns((Client?)null);

        // Act
        var response = await _client!.PostAsync($"/api/admin/users/{nonExistentId}/block", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BlockUnblock_AuditLogCreated()
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

        // Act — block then unblock
        await _client!.PostAsync($"/api/admin/users/{clientId}/block", null);
        await _client.PostAsync($"/api/admin/users/{clientId}/unblock", null);

        // Assert — two audit log entries
        var auditCalls = _factory.AuditLogRepositoryMock.ReceivedCalls()
            .Where(c => c.GetArguments().First() is Onboarding.Domain.Aggregates.Audit.AuditLog)
            .Select(c => (Onboarding.Domain.Aggregates.Audit.AuditLog)c.GetArguments().First())
            .ToList();

        auditCalls.ShouldContain(log => log.Action == "USER_BLOCKED");
        auditCalls.ShouldContain(log => log.Action == "USER_UNBLOCKED");
    }
}
