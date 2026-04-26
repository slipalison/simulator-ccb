using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Onboarding.API.Tests.Authentication;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using FluentValidation;
using Onboarding.Application.Clients.Commands;
using Onboarding.Application.Clients.Validators;
using Onboarding.Application.Services;

namespace Onboarding.API.Tests.Api;

/// <summary>
/// Tests for RegistrationController error paths — Keycloak transient errors and duplicate user scenarios.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class RegistrationErrorPathTests : IAsyncLifetime
{
    private ErrorPathTestFactory? _factory;
    private HttpClient? _client;
    private ICompanyRepository? _repoMock;
    private IKeycloakUserService? _keycloakMock;

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
        _repoMock = Substitute.For<ICompanyRepository>();
        _keycloakMock = Substitute.For<IKeycloakUserService>();

        _repoMock.ExistsByCnpjAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repoMock.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repoMock.AddAsync(Arg.Any<Company>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _repoMock.SaveAsync(Arg.Any<Company>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _repoMock.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var repoMock = _repoMock;
        var keycloakMock = _keycloakMock;

        _factory = new ErrorPathTestFactory(repoMock, keycloakMock);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Register_KeycloakTransientError_Returns503()
    {
        _keycloakMock!
            .CreateUserAsync("client", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Keycloak unreachable"));

        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPjPayload);
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Register_DuplicateKeycloakUser_Returns409()
    {
        _keycloakMock!
            .CreateUserAsync("client", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateKeycloakUserException("User already exists in Keycloak"));

        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPjPayload);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    private sealed class ErrorPathTestFactory : WebApplicationFactory<Program>
    {
        private readonly ICompanyRepository _repoMock;
        private readonly IKeycloakUserService _keycloakMock;

        public ErrorPathTestFactory(ICompanyRepository repoMock, IKeycloakUserService keycloakMock)
        {
            _repoMock = repoMock;
            _keycloakMock = keycloakMock;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:AppDb", "Host=localhost;Port=5432;Database=test;Username=test;Password=test");
            builder.UseSetting("Keycloak:RealmUrl", "http://localhost:8180/realms/client");
            builder.UseSetting("Keycloak:AuthServerUrl", "http://localhost:8180/");
            builder.UseSetting("Keycloak:AdminClientId", "onboarding-api-admin");
            builder.UseSetting("Keycloak:AdminClientSecret", "test-secret");
            builder.UseSetting("Keycloak:Realm", "client");
            builder.UseSetting("Keycloak:PublicClientId", "onboarding-app");
            builder.UseSetting("Keycloak:ValidIssuer", "http://localhost:8180/realms/client");

            builder.ConfigureServices(services =>
            {
                var configureOptionsType = typeof(IConfigureOptions<HealthCheckServiceOptions>);
                var toRemove = services.Where(d => d.ServiceType == configureOptionsType).ToList();
                foreach (var d in toRemove) services.Remove(d);
                services.AddHealthChecks().AddCheck("stub-healthy", () => HealthCheckResult.Healthy("stub-ok"), ["ready"]);

                services.AddScoped<ICompanyRepository>(_ => _repoMock);
                services.AddScoped<IKeycloakUserService>(_ => _keycloakMock);
                services.AddScoped<IValidator<RegisterClientCommand>, RegisterClientCommandValidator>();
            });
        }
    }
}