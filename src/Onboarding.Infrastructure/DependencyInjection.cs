using Duende.AccessTokenManagement;
using Keycloak.AuthServices.Sdk;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Application.Common;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Keycloak;
using Onboarding.Infrastructure.Persistence;
using Onboarding.Infrastructure.Repositories;

namespace Onboarding.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core — PostgreSQL (REG-05, REG-06)
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("AppDb")
                ?? throw new InvalidOperationException(
                    "Connection string 'AppDb' not found in configuration.")));

        services.AddScoped<IClientRepository, ClientRepository>();

        // Keycloak Admin API — service account CC grant (REG-06)
        var keycloakBaseUrl = configuration["Keycloak:AuthServerUrl"]
            ?? throw new InvalidOperationException("Keycloak:AuthServerUrl not configured.");
        var realm = configuration["Keycloak:Realm"] ?? "onboarding";
        var adminClientId = configuration["Keycloak:AdminClientId"]
            ?? throw new InvalidOperationException("Keycloak:AdminClientId not configured.");
        var adminClientSecret = configuration["Keycloak:AdminClientSecret"]
            ?? throw new InvalidOperationException("Keycloak:AdminClientSecret not configured.");

        // IDistributedCache is required by Duende.AccessTokenManagement for CC token caching.
        // AddDistributedMemoryCache() must be called in Program.cs (also needed by IdempotencyFilter).
        services.AddClientCredentialsTokenManagement()
            .AddClient("keycloak-admin", client =>
            {
                client.ClientId = ClientId.Parse(adminClientId);
                client.ClientSecret = ClientSecret.Parse(adminClientSecret);
                client.TokenEndpoint = new Uri(
                    $"{keycloakBaseUrl.TrimEnd('/')}/realms/{realm}" +
                    "/protocol/openid-connect/token");
            });

        services.AddKeycloakAdminHttpClient(new KeycloakAdminClientOptions
        {
            AuthServerUrl = keycloakBaseUrl,
            Realm = realm,
            Resource = adminClientId,
        }).AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse("keycloak-admin"));

        services.AddScoped<IKeycloakUserService, KeycloakUserService>();

        // Keycloak token endpoint — ROPC/refresh calls (D-11, D-12)
        // Named client without auth handler — ROPC calls do not carry outbound Bearer token
        services.AddHttpClient("keycloak-token", client =>
        {
            // [Claude's Discretion] 10 second timeout — balances UX and network reliability
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<IKeycloakTokenService, KeycloakTokenService>();

        return services;
    }
}
