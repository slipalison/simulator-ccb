using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

/// <summary>
/// Integration tests for PUT /api/admin/users/{id} — Update user (ADMIN-03).
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public sealed class AdminUserUpdateTests : IAsyncLifetime
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
    public async Task UpdateUser_ValidData_ReturnsNoContent()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var client = Client.RegisterPessoaFisica("Joao Silva", "529.982.247-25", "joao@test.com", "11999999999");
        typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(client, clientId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(clientId, Arg.Any<CancellationToken>())
            .Returns(client);

        _factory.ClientRepositoryMock
            .ExistsByEmailAsync("joao.updated@test.com", Arg.Any<CancellationToken>())
            .Returns(false);

        var payload = new { name = "Joao Silva Updated", email = "joao.updated@test.com", phone = "11988888888", razaoSocial = (string?)null };

        // Act
        var response = await _client!.PutAsJsonAsync($"/api/admin/users/{clientId}", payload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify audit log was created
        await _factory.AuditServiceMock.Received(1).RecordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<ActionType>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateUser_DuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var client = Client.RegisterPessoaFisica("Joao Silva", "529.982.247-25", "joao@test.com", "11999999999");
        typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(client, clientId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(clientId, Arg.Any<CancellationToken>())
            .Returns(client);

        _factory.ClientRepositoryMock
            .ExistsByEmailAsync("other@test.com", Arg.Any<CancellationToken>())
            .Returns(true);

        var payload = new { name = "Joao Silva", email = "other@test.com", phone = "11999999999", razaoSocial = (string?)null };

        // Act
        var response = await _client!.PutAsJsonAsync($"/api/admin/users/{clientId}", payload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateUser_InvalidName_ReturnsUnprocessableEntity()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var payload = new { name = "", email = "joao@test.com", phone = "11999999999", razaoSocial = (string?)null };

        // Act — no need to mock repository; validation fails before handler
        var response = await _client!.PutAsJsonAsync($"/api/admin/users/{clientId}", payload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateUser_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _factory!.AdminRepositoryMock
            .GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>())
            .Returns((Client?)null);

        var payload = new { name = "Joao", email = "joao@test.com", phone = "11999999999", razaoSocial = (string?)null };

        // Act
        var response = await _client!.PutAsJsonAsync($"/api/admin/users/{nonExistentId}", payload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateUser_AuditLogCreated()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var client = Client.RegisterPessoaFisica("Joao Silva", "529.982.247-25", "joao@test.com", "11999999999");
        typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(client, clientId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(clientId, Arg.Any<CancellationToken>())
            .Returns(client);

        _factory.ClientRepositoryMock
            .ExistsByEmailAsync("joao@test.com", Arg.Any<CancellationToken>())
            .Returns(false);

        var payload = new { name = "Joao Updated", email = "joao@test.com", phone = "11999999999", razaoSocial = (string?)null };

        // Act
        await _client!.PutAsJsonAsync($"/api/admin/users/{clientId}", payload);

        // Assert — audit log with USER_UPDATED action
        await _factory.AuditServiceMock.Received(1).RecordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            ActionType.UserUpdated,
            clientId,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}
