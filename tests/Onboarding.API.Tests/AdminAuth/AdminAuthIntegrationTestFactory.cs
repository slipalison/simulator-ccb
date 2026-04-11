using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Onboarding.API.Configuration;
using Onboarding.Application.Common;
using Onboarding.Application.Services;
using Onboarding.Domain.Repositories;
using System.Security.Claims;

namespace Onboarding.API.Tests.AdminAuth;

/// <summary>
/// Separate factory for integration tests — avoids mock state contamination
/// with the endpoint tests that share the same AdminAuthTestFactory.
/// </summary>
public sealed class AdminAuthIntegrationTestFactory : WebApplicationFactory<Program>
{
    public IKeycloakTokenService TokenServiceMock { get; } = Substitute.For<IKeycloakTokenService>();
    public IClientRepository ClientRepositoryMock { get; } = Substitute.For<IClientRepository>();
    public IKeycloakUserService KeycloakUserServiceMock { get; } = Substitute.For<IKeycloakUserService>();
    public IPasswordResetTokenRepository TokenRepositoryMock { get; } = Substitute.For<IPasswordResetTokenRepository>();
    public IEmailService EmailServiceMock { get; } = Substitute.For<IEmailService>();
    public IAdminRepository AdminRepositoryMock { get; } = Substitute.For<IAdminRepository>();
    public IAuditLogRepository AuditLogRepositoryMock { get; } = Substitute.For<IAuditLogRepository>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:AppDb",
            "Host=localhost;Port=5432;Database=test;Username=test;Password=test");
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
            var configureOptionsType = typeof(IConfigureOptions<HealthCheckServiceOptions>);
            var toRemove = services.Where(d => d.ServiceType == configureOptionsType).ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            services.AddHealthChecks()
                .AddCheck("stub-healthy", () => HealthCheckResult.Healthy("stub-ok"), ["ready"]);

            services.AddScoped<IKeycloakTokenService>(_ => TokenServiceMock);
            services.AddScoped<IClientRepository>(_ => ClientRepositoryMock);
            services.AddScoped<IKeycloakUserService>(_ => KeycloakUserServiceMock);
            services.AddScoped<IPasswordResetTokenRepository>(_ => TokenRepositoryMock);
            services.AddScoped<IEmailService>(_ => EmailServiceMock);
            services.AddScoped<IAdminRepository>(_ => AdminRepositoryMock);
            services.AddScoped<IAuditLogRepository>(_ => AuditLogRepositoryMock);

            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters.ValidateIssuerSigningKey = false;
                    options.TokenValidationParameters.ValidateIssuer = false;
                    options.TokenValidationParameters.ValidateAudience = false;
                    options.TokenValidationParameters.ValidateLifetime = false;
                    options.TokenValidationParameters.RequireSignedTokens = false;
                });

            var transforms = services.Where(d => d.ServiceType == typeof(IClaimsTransformation)).ToList();
            foreach (var d in transforms)
                services.Remove(d);
            services.AddSingleton<IClaimsTransformation>(new IntegrationTestClaimsTransformation());
        });
    }
}

internal sealed class IntegrationTestClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null) return Task.FromResult(principal);

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
