using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
using System.Security.Claims;

namespace Onboarding.API.Tests.Admin;

/// <summary>
/// WebApplicationFactory for admin endpoint integration tests.
/// Disables JWT signature validation and replaces all infrastructure with mocks.
/// Replaces KeycloakRolesClaimsTransformation with a simple test transformer that
/// reads role claims directly from the JWT "role" claim.
/// </summary>
internal sealed class AdminTestFactory : WebApplicationFactory<Program>
{
    public IAdminRepository AdminRepositoryMock { get; } = Substitute.For<IAdminRepository>();
    public IAuditLogRepository AuditLogRepositoryMock { get; } = Substitute.For<IAuditLogRepository>();
    public IClientRepository ClientRepositoryMock { get; } = Substitute.For<IClientRepository>();
    public IKeycloakUserService KeycloakUserServiceMock { get; } = Substitute.For<IKeycloakUserService>();
    public IPasswordResetTokenRepository TokenRepositoryMock { get; } = Substitute.For<IPasswordResetTokenRepository>();
    public IEmailService EmailServiceMock { get; } = Substitute.For<IEmailService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:AppDb",
            "Host=localhost;Port=5432;Database=test;Username=test;Password=test");
        builder.UseSetting("Keycloak:RealmUrl",
            "http://localhost:8180/realms/onboarding");
        builder.UseSetting("Keycloak:AuthServerUrl", "http://localhost:8180/");
        builder.UseSetting("Keycloak:AdminClientId", "onboarding-api-admin");
        builder.UseSetting("Keycloak:AdminClientSecret", "test-secret");
        builder.UseSetting("Keycloak:Realm", "onboarding");
        builder.UseSetting("Keycloak:PublicClientId", "onboarding-app");
        builder.UseSetting("Keycloak:ValidIssuer", "http://localhost:8180/realms/onboarding");

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

            // Replace all infrastructure with mocks
            services.AddScoped<IAdminRepository>(_ => AdminRepositoryMock);
            services.AddScoped<IAuditLogRepository>(_ => AuditLogRepositoryMock);
            services.AddScoped<IClientRepository>(_ => ClientRepositoryMock);
            services.AddScoped<IKeycloakUserService>(_ => KeycloakUserServiceMock);
            services.AddScoped<IPasswordResetTokenRepository>(_ => TokenRepositoryMock);
            services.AddScoped<IEmailService>(_ => EmailServiceMock);

            // Disable JWT validation for tests — PostConfigure overrides app configuration
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration();
                    options.TokenValidationParameters.ValidateIssuer = false;
                    options.TokenValidationParameters.ValidateAudience = false;
                    options.TokenValidationParameters.ValidateLifetime = false;
                    options.TokenValidationParameters.IssuerSigningKey = FakeJwtTokenHelper.SecurityKey;
                });

            // Replace all IClaimsTransformation implementations with our test version.
            // The real KeycloakRolesClaimsTransformation is registered by AddKeycloakAuthorization.
            // We replace it so that role claims from the JWT "role" claim are used directly.
            var transforms = services
                .Where(d => d.ServiceType == typeof(IClaimsTransformation))
                .ToList();
            foreach (var d in transforms)
                services.Remove(d);

            services.AddSingleton<IClaimsTransformation>(new TestClaimsTransformation());
        });
    }
}

/// <summary>
/// Simple claims transformation for tests: copies "role" claims to ClaimTypes.Role
/// so that [Authorize(Roles = "admin")] works with JWTs that have "role" claims.
/// </summary>
internal sealed class TestClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null) return Task.FromResult(principal);

        // Copy "role" claims to ClaimTypes.Role for authorization middleware
        foreach (var roleClaim in principal.FindAll("role").ToList())
        {
            if (!principal.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == roleClaim.Value))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
            }
        }

        return Task.FromResult(principal);
    }
}
