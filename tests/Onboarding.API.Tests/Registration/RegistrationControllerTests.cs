using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Application.Clients.Commands;
using Onboarding.Application.Clients.Validators;
using Onboarding.Application.Common;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace Onboarding.API.Tests.Registration;

/// <summary>
/// Test factory that configures the API for integration tests without real infrastructure.
/// Replaces IClientRepository and IKeycloakUserService with NSubstitute mocks so that
/// TestServer can start without a real PostgreSQL or Keycloak instance.
/// </summary>
internal sealed class RegistrationTestApiFactory : WebApplicationFactory<Program>
{
    // Exposed for individual test configuration
    public IClientRepository RepositoryMock { get; } = Substitute.For<IClientRepository>();
    public IKeycloakUserService KeycloakMock { get; } = Substitute.For<IKeycloakUserService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:AppDb",
            "Host=localhost;Port=5432;Database=test;Username=test;Password=test");
        builder.UseSetting("Keycloak:RealmUrl",
            "http://localhost:8180/realms/client");
        builder.UseSetting("Keycloak:AuthServerUrl", "http://localhost:8180/");
        builder.UseSetting("Keycloak:AdminClientId", "onboarding-api-admin");
        builder.UseSetting("Keycloak:AdminClientSecret", "test-secret");
        builder.UseSetting("Keycloak:Realm", "client");
        builder.UseSetting("Keycloak:ValidIssuer", "http://localhost:8180/realms/client");

        builder.ConfigureTestServices(services =>
        {
            // Remove real health checks so TestServer can start without real infrastructure
            var configureOptionsType = typeof(IConfigureOptions<HealthCheckServiceOptions>);
            var toRemove = services
                .Where(d => d.ServiceType == configureOptionsType)
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            services.AddHealthChecks()
                .AddCheck("stub-healthy", () => HealthCheckResult.Healthy("stub-ok"), ["ready"]);

            // Replace real infrastructure with mocks (no DB, no Keycloak required)
            services.AddScoped<IClientRepository>(_ => RepositoryMock);
            services.AddScoped<IKeycloakUserService>(_ => KeycloakMock);

            // Ensure IValidator<RegisterClientCommand> is registered
            services.AddScoped<IValidator<RegisterClientCommand>,
                RegisterClientCommandValidator>();

            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                    {
                        Issuer = "http://localhost:8180/realms/client",
                    };
                    options.TokenValidationParameters.ValidateIssuer = false;
                    options.TokenValidationParameters.ValidateAudience = false;
                    options.TokenValidationParameters.ValidateLifetime = false;
                    options.TokenValidationParameters.IssuerSigningKey = FakeJwtTokenHelper.SecurityKey;
                    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                });

            services.PostConfigure<JwtBearerOptions>(
                "BearerClient", options =>
                {
                    options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                    {
                        Issuer = "http://localhost:8180/realms/client",
                    };
                    options.TokenValidationParameters.ValidateIssuer = false;
                    options.TokenValidationParameters.ValidateAudience = false;
                    options.TokenValidationParameters.ValidateLifetime = false;
                    options.TokenValidationParameters.IssuerSigningKey = FakeJwtTokenHelper.SecurityKey;
                    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                });
        });
    }
}

/// <summary>
/// Integration tests for RegistrationController (Phase 5 Plans 02/03).
/// WebApplicationFactory spins up the real API with TestServer — infrastructure replaced by mocks.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class RegistrationControllerTests : IAsyncLifetime
{
    private RegistrationTestApiFactory? _factory;
    private HttpClient? _client;

    // Valid test payloads
    private static readonly object ValidPfPayload = new
    {
        nome = "João Silva",
        cpf = "529.982.247-25",
        email = "joao@example.com",
        phone = "11999998888",
        password = "Str0ng@Pass"
    };

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
        // Default mock setup: no duplicates, Keycloak succeeds
        _factory.RepositoryMock
            .ExistsByCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _factory.RepositoryMock
            .ExistsByCnpjAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _factory.RepositoryMock
            .ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _factory.RepositoryMock
            .AddAsync(Arg.Any<Domain.Aggregates.ClientAggregate.Client>(), Arg.Any<CancellationToken>())
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

    // REG-03: Valid PF payload → 201
    [Fact]
    public async Task PostPf_ValidCpf_Returns201()
    {
        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPfPayload);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // REG-04: Valid PJ payload with CNPJ → 201
    [Fact]
    public async Task PostPj_ValidCnpj_Returns201()
    {
        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPjPayload);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // REG-03: Invalid CPF check digit → 422
    [Fact]
    public async Task PostPf_InvalidCpfCheckDigit_Returns422()
    {
        var payload = new { nome = "João Silva", cpf = "000.000.000-00",
                            email = "joao@example.com", phone = "11999998888", password = "Str0ng@Pass" };
        var response = await _client!.PostAsJsonAsync("/api/registration", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // REG-04: Invalid CNPJ check digit → 422
    [Fact]
    public async Task PostPj_InvalidCnpjCheckDigit_Returns422()
    {
        var payload = new { razaoSocial = "Empresa", cnpj = "11.111.111/1111-11",
                            email = "empresa@example.com", phone = "1133334444", password = "Str0ng@Pass" };
        var response = await _client!.PostAsJsonAsync("/api/registration", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // REG-05: Duplicate CPF → 409
    [Fact]
    public async Task PostPf_DuplicateCpf_Returns409()
    {
        _factory!.RepositoryMock
            .ExistsByCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPfPayload);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // REG-05: Duplicate email → 409
    [Fact]
    public async Task PostPf_DuplicateEmail_Returns409()
    {
        _factory!.RepositoryMock
            .ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPfPayload);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // SEC-08: 409 response body must not mention which field caused conflict
    [Fact]
    public async Task PostPf_DuplicateCpf_ResponseBodyDoesNotLeakFieldName()
    {
        _factory!.RepositoryMock
            .ExistsByCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPfPayload);
        var body = await response.Content.ReadAsStringAsync();
        // SEC-08: body must not hint at which field caused the conflict
        body.ShouldNotContain("cpf", Case.Insensitive);
        body.ShouldNotContain("email", Case.Insensitive);
        body.ShouldNotContain("already registered", Case.Insensitive);
    }

    // SEC-08: 422 response body must not leak domain exception messages
    [Fact]
    public async Task PostPf_InvalidCpf_ResponseBodyIsGeneric()
    {
        var payload = new { nome = "João", cpf = "000.000.000-00",
                            email = "x@example.com", phone = "11999998888", password = "Str0ng@Pass" };
        var response = await _client!.PostAsJsonAsync("/api/registration", payload);
        var body = await response.Content.ReadAsStringAsync();
        // SEC-08: domain ex.Message ("CPF check digit invalid" etc.) must NOT appear
        body.ShouldNotContain("check digit", Case.Insensitive);
        body.ShouldNotContain("ArgumentException", Case.Insensitive);
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // BACK-05: Endpoint exists at POST /api/registration (not Minimal API)
    [Fact]
    public async Task PostRegistration_EndpointExists_NotMinimalApi()
    {
        // Sending empty body triggers 400 or 422 — either way proves endpoint exists and uses controller routing
        var response = await _client!.PostAsync("/api/registration",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        // 400 (bad request) or 422 (validation fail) — NOT 404 (endpoint missing)
        ((int)response.StatusCode).ShouldNotBe(404);
    }

    // REG-08: Idempotency key → segunda chamada retorna 201 cacheado
    [Fact]
    public async Task PostPf_SameIdempotencyKey_SecondCallReturnsCached201()
    {
        var idempotencyKey = Guid.NewGuid().ToString();

        // Primeira chamada
        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/registration")
        {
            Content = JsonContent.Create(ValidPfPayload),
            Headers = { { "Idempotency-Key", idempotencyKey } }
        };
        var response1 = await _client!.SendAsync(request1);
        response1.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Segunda chamada com o mesmo Idempotency-Key — deve retornar 201 do cache
        _factory!.RepositoryMock.ClearReceivedCalls();
        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/registration")
        {
            Content = JsonContent.Create(ValidPfPayload),
            Headers = { { "Idempotency-Key", idempotencyKey } }
        };
        var response2 = await _client!.SendAsync(request2);
        response2.StatusCode.ShouldBe(HttpStatusCode.Created);

        // AddAsync NÃO deve ter sido chamado na segunda requisição (servida do cache)
        await _factory.RepositoryMock
            .DidNotReceive()
            .AddAsync(Arg.Any<Domain.Aggregates.ClientAggregate.Client>(),
                      Arg.Any<CancellationToken>());
    }

    // REG-08: Sem Idempotency-Key header → request prossegue normalmente
    [Fact]
    public async Task PostPf_NoIdempotencyKey_ProceedsNormally()
    {
        // Sem header Idempotency-Key → filter deve deixar passar (key é opcional)
        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPfPayload);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
