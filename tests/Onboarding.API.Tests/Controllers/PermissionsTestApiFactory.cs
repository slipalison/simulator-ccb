using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Onboarding.API.Tests.Authentication;
using Onboarding.Application.Common;
using Onboarding.Application.Services;
using Onboarding.Domain.Repositories;

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// WebApplicationFactory for PermissionsController integration tests.
/// Disables JWT signature validation for BearerClient and replaces all infrastructure
/// with mocks. Repositories return null (no-match) by default so the endpoint returns
/// an empty permissions array (ClientClaimsMiddleware no-match path).
/// </summary>
public sealed class PermissionsTestApiFactory : WebApplicationFactory<Program>
{
    public ICompanyRepository CompanyRepositoryMock { get; } = Substitute.For<ICompanyRepository>();
    public IEmployeeRepository EmployeeRepositoryMock { get; } = Substitute.For<IEmployeeRepository>();
    public IAccessGroupRepository AccessGroupRepositoryMock { get; } = Substitute.For<IAccessGroupRepository>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:AppDb",
            "Host=localhost;Port=5432;Database=test;Username=test;Password=test");
        builder.UseSetting("Keycloak:ClientRealmUrl",
            "http://localhost:8180/realms/client");
        builder.UseSetting("Keycloak:BackofficeRealmUrl",
            "http://localhost:8180/realms/backoffice");
        builder.UseSetting("Keycloak:AuthServerUrl", "http://localhost:8180/");
        builder.UseSetting("Keycloak:AdminClientId", "onboarding-api-admin");
        builder.UseSetting("Keycloak:AdminClientSecret", "test-secret");
        builder.UseSetting("Keycloak:Realm", "client");
        builder.UseSetting("Keycloak:PublicClientId", "onboarding-app");
        builder.UseSetting("Keycloak:ValidIssuer", "http://localhost:8180/realms/client");

        builder.ConfigureTestServices(services =>
        {
            // Remove real health checks — test server starts without real infrastructure
            var configureOptionsType = typeof(IConfigureOptions<HealthCheckServiceOptions>);
            var toRemove = services
                .Where(d => d.ServiceType == configureOptionsType)
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            services.AddHealthChecks()
                .AddCheck("stub-healthy", () => HealthCheckResult.Healthy("stub-ok"), ["ready"]);

            // Replace infrastructure with mocks
            services.AddScoped<ICompanyRepository>(_ => CompanyRepositoryMock);
            services.AddScoped<IEmployeeRepository>(_ => EmployeeRepositoryMock);
            services.AddScoped<IAccessGroupRepository>(_ => AccessGroupRepositoryMock);
            services.AddScoped<IEmailService>(_ => Substitute.For<IEmailService>());
            services.AddScoped<IPasswordResetTokenRepository>(_ => Substitute.For<IPasswordResetTokenRepository>());

            // Disable JWT signature validation for BearerClient — tests use FakeJwtTokenHelper
            services.PostConfigure<JwtBearerOptions>("BearerClient", options =>
            {
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                {
                    Issuer = "http://localhost:8180/realms/client",
                };
                options.TokenValidationParameters.ValidateIssuer = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.ValidateLifetime = false; // nosemgrep
                options.TokenValidationParameters.IssuerSigningKey = FakeJwtTokenHelper.SecurityKey;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
            });

            services.PostConfigure<JwtBearerOptions>("BearerBackoffice", options =>
            {
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                {
                    Issuer = "http://localhost:8180/realms/backoffice",
                };
                options.TokenValidationParameters.ValidateIssuer = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.ValidateLifetime = false; // nosemgrep
                options.TokenValidationParameters.IssuerSigningKey = FakeJwtTokenHelper.SecurityKey;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
            });
        });
    }
}
