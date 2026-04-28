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
    public ICompanyRepository RepositoryMock { get; } = Substitute.For<ICompanyRepository>();
    public IKeycloakTokenService TokenServiceMock { get; } = Substitute.For<IKeycloakTokenService>();
    public IKeycloakUserService KeycloakUserServiceMock { get; } = Substitute.For<IKeycloakUserService>();
    public IPasswordResetTokenRepository TokenRepositoryMock { get; } = Substitute.For<IPasswordResetTokenRepository>();
    public IEmailService EmailServiceMock { get; } = Substitute.For<IEmailService>();

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
        builder.UseSetting("Keycloak:PublicClientId", "onboarding-app");
        builder.UseSetting("Keycloak:ValidIssuer", "http://localhost:8180/realms/client");

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
            services.AddScoped<ICompanyRepository>(_ => RepositoryMock);
            services.AddScoped<IKeycloakTokenService>(_ => TokenServiceMock);
            services.AddScoped<IKeycloakUserService>(_ => KeycloakUserServiceMock);
            services.AddScoped<IPasswordResetTokenRepository>(_ => TokenRepositoryMock);
            services.AddScoped<IEmailService>(_ => EmailServiceMock);
            services.AddScoped<IEmployeeRepository>(_ => Substitute.For<IEmployeeRepository>());
            services.AddScoped<IAccessGroupRepository>(_ => Substitute.For<IAccessGroupRepository>());

            // Disable JWT validation for tests — PostConfigure overrides app configuration
            // D-04/D-05: in tests we use FakeJwtTokenHelper to generate HMAC-signed tokens
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    // Provide empty OIDC config to prevent the handler from fetching
                    // /.well-known/openid-configuration from the (unreachable) Authority URL.
                    // Without this, token validation silently fails → 401 in CI.
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
