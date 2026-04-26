using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace Onboarding.API.Tests.Registration;

/// <summary>
/// Integration tests for IdempotencyFilter via WebApplicationFactory (REG-08).
/// Tests the filter behavior via HTTP calls — verifies caching semantics end-to-end.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class IdempotencyFilterTests : IAsyncLifetime
{
    private RegistrationTestApiFactory? _factory;
    private HttpClient? _client;

    private static readonly object ValidPjPayload = new
    {
        razaoSocial = "Empresa Ltda",
        cnpj = "11.222.333/0001-81",
        email = "contato@empresa.com",
        phone = "1133334444",
        password = "Str0ng@Pass"
    };

    public Task InitializeAsync()
    {
        _factory = new RegistrationTestApiFactory();
        _factory.RepositoryMock
            .ExistsByCnpjAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _factory.RepositoryMock
            .ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _factory.RepositoryMock
            .AddAsync(Arg.Any<Company>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _factory.KeycloakMock
            .CreateUserAsync("client", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                             Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("fake-keycloak-id");
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Filter_422Response_IsNotCached()
    {
        var invalidPayload = new { razaoSocial = "Empresa", cnpj = "11.111.111/1111-11",
                                   email = "empresa@example.com", phone = "1133334444",
                                   password = "Str0ng@Pass" };
        var idempotencyKey = Guid.NewGuid().ToString();

        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/registration")
        {
            Content = JsonContent.Create(invalidPayload),
            Headers = { { "Idempotency-Key", idempotencyKey } }
        };
        var response1 = await _client!.SendAsync(request1);
        response1.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/registration")
        {
            Content = JsonContent.Create(invalidPayload),
            Headers = { { "Idempotency-Key", idempotencyKey } }
        };
        var response2 = await _client!.SendAsync(request2);
        response2.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        await _factory!.RepositoryMock
            .DidNotReceive()
            .AddAsync(Arg.Any<Company>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Filter_SameKey_ReturnsCachedResponse()
    {
        var idempotencyKey = Guid.NewGuid().ToString();

        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/registration")
        {
            Content = JsonContent.Create(ValidPjPayload),
            Headers = { { "Idempotency-Key", idempotencyKey } }
        };
        var response1 = await _client!.SendAsync(request1);
        response1.StatusCode.ShouldBe(HttpStatusCode.Created);

        _factory!.RepositoryMock.ClearReceivedCalls();
        _factory.KeycloakMock.ClearReceivedCalls();

        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/registration")
        {
            Content = JsonContent.Create(ValidPjPayload),
            Headers = { { "Idempotency-Key", idempotencyKey } }
        };
        var response2 = await _client!.SendAsync(request2);
        response2.StatusCode.ShouldBe(HttpStatusCode.Created);

        await _factory.RepositoryMock
            .DidNotReceive()
            .AddAsync(Arg.Any<Company>(), Arg.Any<CancellationToken>());
        await _factory.KeycloakMock
            .DidNotReceive()
            .CreateUserAsync("client", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                             Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Filter_NonGuidKey_PassesThrough()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/registration")
        {
            Content = JsonContent.Create(ValidPjPayload),
            Headers = { { "Idempotency-Key", "not-a-guid-value" } }
        };
        var response = await _client!.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}