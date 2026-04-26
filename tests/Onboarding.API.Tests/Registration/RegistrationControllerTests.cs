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
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace Onboarding.API.Tests.Registration;

/// <summary>
/// Test factory that configures the API for integration tests without real infrastructure.
/// Replaces ICompanyRepository and IKeycloakUserService with NSubstitute mocks so that
/// TestServer can start without a real PostgreSQL or Keycloak instance.
/// </summary>
internal sealed class RegistrationTestApiFactory : WebApplicationFactory<Program>
{
    // Exposed for individual test configuration
    public ICompanyRepository RepositoryMock { get; } = Substitute.For<ICompanyRepository>();
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
            var configureOptionsType = typeof(IConfigureOptions<HealthCheckServiceOptions>);
            var toRemove = services
                .Where(d => d.ServiceType == configureOptionsType)
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            services.AddHealthChecks()
                .AddCheck("stub-healthy", () => HealthCheckResult.Healthy("stub-ok"), ["ready"]);

            services.AddScoped<ICompanyRepository>(_ => RepositoryMock);
            services.AddScoped<IKeycloakUserService>(_ => KeycloakMock);
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
/// Integration tests for RegistrationController — PJ-only registration.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class RegistrationControllerTests : IAsyncLifetime
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
    public async Task PostPj_ValidCnpj_Returns201()
    {
        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPjPayload);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostPj_InvalidCnpjCheckDigit_Returns422()
    {
        var payload = new { razaoSocial = "Empresa", cnpj = "11.111.111/1111-11",
                            email = "empresa@example.com", phone = "1133334444", password = "Str0ng@Pass" };
        var response = await _client!.PostAsJsonAsync("/api/registration", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PostPj_DuplicateCnpj_Returns409()
    {
        _factory!.RepositoryMock
            .ExistsByCnpjAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPjPayload);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PostPj_DuplicateEmail_Returns409()
    {
        _factory!.RepositoryMock
            .ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPjPayload);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // SEC-08: 409 response body must not mention which field caused conflict
    [Fact]
    public async Task PostPj_DuplicateCnpj_ResponseBodyDoesNotLeakFieldName()
    {
        _factory!.RepositoryMock
            .ExistsByCnpjAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPjPayload);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("cnpj", Case.Insensitive);
        body.ShouldNotContain("email", Case.Insensitive);
    }

    [Fact]
    public async Task PostRegistration_EndpointExists_NotMinimalApi()
    {
        var response = await _client!.PostAsync("/api/registration",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        ((int)response.StatusCode).ShouldNotBe(404);
    }

    [Fact]
    public async Task PostPj_SameIdempotencyKey_SecondCallReturnsCached201()
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
    }

    [Fact]
    public async Task PostPj_NoIdempotencyKey_ProceedsNormally()
    {
        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPjPayload);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}