using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Services;
using Onboarding.Domain.Repositories;

namespace Onboarding.API.Tests.Authentication;

/// <summary>
/// WebApplicationFactory for authentication endpoint tests.
/// Disables JWT signature validation via PostConfigure so tests run without a real Keycloak.
/// Uses PostConfigure (not Configure) to ensure override applies after app's own JWT configuration.
/// </summary>
internal sealed class AuthTestApiFactory : WebApplicationFactory<Program>
{
    public IClientRepository RepositoryMock { get; } = Substitute.For<IClientRepository>();
    public IKeycloakTokenService TokenServiceMock { get; } = Substitute.For<IKeycloakTokenService>();
    public IKeycloakUserService KeycloakUserServiceMock { get; } = Substitute.For<IKeycloakUserService>();
    public IPasswordResetTokenRepository TokenRepositoryMock { get; } = Substitute.For<IPasswordResetTokenRepository>();
    public IEmailService EmailServiceMock { get; } = Substitute.For<IEmailService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:AppDb",
            "Host=localhost;Port=5432;Database=test;Username=test;Password=test");
        builder.UseSetting("Keycloak:RealmUrl",
            "http://localhost:8180/realms/onboarding");
        builder.UseSetting("Keycloak:AuthServerUrl", "http://localhost:8180/");
        builder.UseSetting("Keycloak:AdminClientId", "onboarding-api-admin");
        builder.UseSetting("Keycloak:AdminClientSecret", "test-secret");
        builder.UseSetting("Keycloak:Realm", "onboarding");
        builder.UseSetting("Keycloak:PublicClientId", "onboarding-app");

        builder.ConfigureTestServices(services =>
        {
            // Remove real health checks so TestServer starts without real infrastructure
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
            services.AddScoped<IKeycloakTokenService>(_ => TokenServiceMock);
            services.AddScoped<IKeycloakUserService>(_ => KeycloakUserServiceMock);
            services.AddScoped<IPasswordResetTokenRepository>(_ => TokenRepositoryMock);
            services.AddScoped<IEmailService>(_ => EmailServiceMock);

            // Disable JWT validation for tests — PostConfigure overrides app configuration
            // D-04/D-05: in tests we use FakeJwtTokenHelper to generate unsigned tokens
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters.ValidateIssuerSigningKey = false;
                    options.TokenValidationParameters.ValidateIssuer = false;
                    options.TokenValidationParameters.ValidateAudience = false;
                    options.TokenValidationParameters.ValidateLifetime = false;
                    options.TokenValidationParameters.RequireSignedTokens = false;
                });
        });
    }
}
