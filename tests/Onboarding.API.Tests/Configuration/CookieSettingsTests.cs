using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Onboarding.API.Configuration;
using Shouldly;

namespace Onboarding.API.Tests.Configuration;

/// <summary>
/// Tests for CookieSettings configuration via IOptions (Task 4).
/// </summary>
public class CookieSettingsTests
{
    [Fact]
    public void DevelopmentConfig_SecureIsFalse()
    {
        // Arrange & Act
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:AppDb", "Host=localhost;Port=5432;Database=test;Username=test;Password=test");
                builder.UseSetting("Keycloak:RealmUrl", "http://localhost:8180/realms/onboarding");
                builder.UseSetting("Keycloak:AuthServerUrl", "http://localhost:8180/");
                builder.UseSetting("Keycloak:AdminClientId", "onboarding-api-admin");
                builder.UseSetting("Keycloak:AdminClientSecret", "test-secret");
                builder.UseSetting("Keycloak:Realm", "onboarding");
                builder.UseSetting("Keycloak:PublicClientId", "onboarding-app");
                builder.UseSetting("Keycloak:ValidIssuer", "http://localhost:8180/realms/onboarding");
                builder.UseSetting("CookieSettings:Secure", "false");
                builder.ConfigureTestServices(services =>
                {
                    var healthCheckType = typeof(IConfigureOptions<HealthCheckServiceOptions>);
                    var toRemove = services.Where(d => d.ServiceType == healthCheckType).ToList();
                    foreach (var d in toRemove)
                        services.Remove(d);
                    services.AddHealthChecks()
                        .AddCheck("stub", () => HealthCheckResult.Healthy());
                });
            });

        var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<CookieSettings>>();
        options.Value.Secure.ShouldBeFalse();
    }

    [Fact]
    public void ProductionConfig_SecureIsTrue()
    {
        // Arrange & Act
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:AppDb", "Host=localhost;Port=5432;Database=test;Username=test;Password=test");
                builder.UseSetting("Keycloak:RealmUrl", "http://localhost:8180/realms/onboarding");
                builder.UseSetting("Keycloak:AuthServerUrl", "http://localhost:8180/");
                builder.UseSetting("Keycloak:AdminClientId", "onboarding-api-admin");
                builder.UseSetting("Keycloak:AdminClientSecret", "test-secret");
                builder.UseSetting("Keycloak:Realm", "onboarding");
                builder.UseSetting("Keycloak:PublicClientId", "onboarding-app");
                builder.UseSetting("Keycloak:ValidIssuer", "http://localhost:8180/realms/onboarding");
                builder.UseSetting("CookieSettings:Secure", "true");
                builder.ConfigureTestServices(services =>
                {
                    var healthCheckType = typeof(IConfigureOptions<HealthCheckServiceOptions>);
                    var toRemove = services.Where(d => d.ServiceType == healthCheckType).ToList();
                    foreach (var d in toRemove)
                        services.Remove(d);
                    services.AddHealthChecks()
                        .AddCheck("stub", () => HealthCheckResult.Healthy());
                });
            });

        var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<CookieSettings>>();
        options.Value.Secure.ShouldBeTrue();
    }

    [Fact]
    public void CookieSettings_IsInjectableViaDI()
    {
        // Arrange & Act
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:AppDb", "Host=localhost;Port=5432;Database=test;Username=test;Password=test");
                builder.UseSetting("Keycloak:RealmUrl", "http://localhost:8180/realms/onboarding");
                builder.UseSetting("Keycloak:AuthServerUrl", "http://localhost:8180/");
                builder.UseSetting("Keycloak:AdminClientId", "onboarding-api-admin");
                builder.UseSetting("Keycloak:AdminClientSecret", "test-secret");
                builder.UseSetting("Keycloak:Realm", "onboarding");
                builder.UseSetting("Keycloak:PublicClientId", "onboarding-app");
                builder.UseSetting("Keycloak:ValidIssuer", "http://localhost:8180/realms/onboarding");
                builder.ConfigureTestServices(services =>
                {
                    var healthCheckType = typeof(IConfigureOptions<HealthCheckServiceOptions>);
                    var toRemove = services.Where(d => d.ServiceType == healthCheckType).ToList();
                    foreach (var d in toRemove)
                        services.Remove(d);
                    services.AddHealthChecks()
                        .AddCheck("stub", () => HealthCheckResult.Healthy());
                });
            });

        var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<CookieSettings>>();
        options.ShouldNotBeNull();
    }
}
