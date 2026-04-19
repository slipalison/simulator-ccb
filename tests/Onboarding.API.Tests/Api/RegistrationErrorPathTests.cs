using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Onboarding.API.Tests.Authentication;
using Onboarding.Application.Common;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using FluentValidation;
using Onboarding.Application.Clients.Commands;
using Onboarding.Application.Clients.Validators;
using Onboarding.Application.Services;

namespace Onboarding.API.Tests.Api;

/// <summary>
/// Tests for RegistrationController error paths not covered by RegistrationControllerTests.
/// Covers the RegistrationFailedException → 503 and DuplicateKeycloakUserException → 409 paths.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class RegistrationErrorPathTests : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private IClientRepository? _repoMock;
    private IKeycloakUserService? _keycloakMock;

    private static readonly object ValidPfPayload = new
    {
        nome = "João Silva",
        cpf = "529.982.247-25",
        email = "joao@example.com",
        phone = "11999998888",
        password = "Str0ng@Pass"
    };

    public Task InitializeAsync()
    {
        _repoMock = Substitute.For<IClientRepository>();
        _keycloakMock = Substitute.For<IKeycloakUserService>();

        _repoMock.ExistsByCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repoMock.ExistsByCnpjAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repoMock.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repoMock.AddAsync(Arg.Any<Domain.Aggregates.ClientAggregate.Client>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _repoMock.SaveAsync(Arg.Any<Domain.Aggregates.ClientAggregate.Client>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _repoMock.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var repoMock = _repoMock;
        var keycloakMock = _keycloakMock;

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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

            builder.ConfigureTestServices(services =>
            {
                var configureOptionsType = typeof(IConfigureOptions<HealthCheckServiceOptions>);
                var toRemove = services.Where(d => d.ServiceType == configureOptionsType).ToList();
                foreach (var d in toRemove) services.Remove(d);
                services.AddHealthChecks().AddCheck("stub-healthy", () => HealthCheckResult.Healthy("stub-ok"), ["ready"]);

                services.AddScoped<IClientRepository>(_ => repoMock);
                services.AddScoped<IKeycloakUserService>(_ => keycloakMock);
                services.AddScoped<IValidator<RegisterClientCommand>, RegisterClientCommandValidator>();
            });
        });

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

        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPfPayload);
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Register_DuplicateKeycloakUser_Returns409()
    {
        _keycloakMock!
            .CreateUserAsync("client", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateKeycloakUserException("User already exists in Keycloak"));

        var response = await _client!.PostAsJsonAsync("/api/registration", ValidPfPayload);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
