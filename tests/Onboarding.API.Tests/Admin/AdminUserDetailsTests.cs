using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

/// <summary>
/// Integration tests for GET /api/admin/users/{id} — User details (ADMIN-02).
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public sealed class AdminUserDetailsTests : IAsyncLifetime
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
    public async Task GetUserDetails_ValidId_ReturnsFullData()
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
            .Returns(new KeycloakUser("kc-uuid", "joao@test.com"));

        // Act
        var response = await _client!.GetAsync($"/api/admin/users/{clientId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserDetailDto>();
        body.ShouldNotBeNull();
        body.Name.ShouldBe("Joao Silva");
        body.Email.ShouldBe("joao@test.com");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUserDetails_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _factory!.AdminRepositoryMock
            .GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>())
            .Returns((Client?)null);

        // Act
        var response = await _client!.GetAsync($"/api/admin/users/{nonExistentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUserDetails_PfUser_ReturnsCpf_NotCnpj()
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
            .Returns(new KeycloakUser("kc-uuid", "joao@test.com"));

        // Act
        var response = await _client!.GetAsync($"/api/admin/users/{clientId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserDetailDto>();
        body.ShouldNotBeNull();
        body.Document.ShouldNotBeNull();
        body.Document.ShouldContain("529.982.247-25"); // CPF formatted
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetUserDetails_PjUser_ReturnsCnpj_NotCpf()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var client = Client.RegisterPessoaJuridica("Empresa LTDA", "11.222.333/0001-81", "empresa@test.com", "11999999999");
        typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(client, clientId);

        _factory!.AdminRepositoryMock
            .GetByIdAsync(clientId, Arg.Any<CancellationToken>())
            .Returns(client);

        _factory.KeycloakUserServiceMock
            .GetUserByEmailAsync("empresa@test.com", Arg.Any<CancellationToken>())
            .Returns(new KeycloakUser("kc-uuid", "empresa@test.com"));

        // Act
        var response = await _client!.GetAsync($"/api/admin/users/{clientId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserDetailDto>();
        body.ShouldNotBeNull();
        body.Document.ShouldNotBeNull();
        body.Document.ShouldContain("11.222.333/0001-81"); // CNPJ formatted
        body.RazaoSocial.ShouldBe("Empresa LTDA");
    }
}
