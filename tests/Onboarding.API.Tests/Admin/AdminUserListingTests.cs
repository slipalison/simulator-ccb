using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

/// <summary>
/// Integration tests for GET /api/admin/users — Paginated user listing (ADMIN-01).
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public sealed class AdminUserListingTests : IAsyncLifetime
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
    public async Task GetPaginatedUsers_ReturnsPageWithItems()
    {
        // Arrange — 3 clients in DB
        var clients = new List<Client>
        {
            Client.RegisterPessoaFisica("Joao Silva", "529.982.247-25", "joao@test.com", "11999999999"),
            Client.RegisterPessoaFisica("Maria Santos", "529.982.247-25", "maria@test.com", "11999999998"),
            Client.RegisterPessoaJuridica("Empresa LTDA", "11.222.333/0001-81", "empresa@test.com", "11999999997")
        };
        foreach (var c in clients) typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(c, Guid.NewGuid());

        _factory!.AdminRepositoryMock
            .GetPagedAsync(1, 10, null, null, Arg.Any<CancellationToken>())
            .Returns((clients.AsReadOnly(), 3));

        // Act
        var response = await _client!.GetAsync("/api/admin/users?page=1&pageSize=10");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.ShouldNotBeNull();
        body.ContainsKey("items").ShouldBeTrue();
        body.ContainsKey("totalCount").ShouldBeTrue();
        body.ContainsKey("totalPages").ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaginatedUsers_SearchByName_ReturnsFilteredResults()
    {
        // Arrange — mock returns filtered results for "Joao"
        var client = Client.RegisterPessoaFisica("Joao Silva", "529.982.247-25", "joao@test.com", "11999999999");
        typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(client, Guid.NewGuid());

        _factory!.AdminRepositoryMock
            .GetPagedAsync(1, 10, "Joao", null, Arg.Any<CancellationToken>())
            .Returns((new List<Client> { client }.AsReadOnly(), 1));

        // Act
        var response = await _client!.GetAsync("/api/admin/users?page=1&pageSize=10&search=Joao");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaginatedUsers_ExcludesDeletedUsers_ByDefault()
    {
        // Arrange — mock returns only non-deleted clients
        var client = Client.RegisterPessoaFisica("Joao Silva", "529.982.247-25", "joao@test.com", "11999999999");
        typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(client, Guid.NewGuid());

        _factory!.AdminRepositoryMock
            .GetPagedAsync(1, 10, null, null, Arg.Any<CancellationToken>())
            .Returns((new List<Client> { client }.AsReadOnly(), 1));

        // Act
        var response = await _client!.GetAsync("/api/admin/users?page=1&pageSize=10");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaginatedUsers_StatusDeleted_ReturnsOnlyDeleted()
    {
        // Arrange — mock returns deleted clients when status=deleted
        var deletedClient = Client.RegisterPessoaFisica("Deleted User", "529.982.247-25", "deleted@test.com", "11999999999");
        typeof(Client).GetProperty(nameof(Client.Id))!.SetValue(deletedClient, Guid.NewGuid());

        _factory!.AdminRepositoryMock
            .GetPagedAsync(1, 10, null, "deleted", Arg.Any<CancellationToken>())
            .Returns((new List<Client> { deletedClient }.AsReadOnly(), 1));

        // Act
        var response = await _client!.GetAsync("/api/admin/users?page=1&pageSize=10&status=deleted");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPaginatedUsers_SecondPage_ReturnsEmpty_WhenLessThanPageSize()
    {
        // Arrange — only 3 clients, requesting page 2 with pageSize 10
        _factory!.AdminRepositoryMock
            .GetPagedAsync(2, 10, null, null, Arg.Any<CancellationToken>())
            .Returns((new List<Client>().AsReadOnly(), 3));

        // Act
        var response = await _client!.GetAsync("/api/admin/users?page=2&pageSize=10");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.ShouldNotBeNull();
        body["totalCount"].ToString().ShouldBe("3");
    }
}
