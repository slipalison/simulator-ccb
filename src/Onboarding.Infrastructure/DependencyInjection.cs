using System.Diagnostics.CodeAnalysis;
using Duende.AccessTokenManagement;
using Keycloak.AuthServices.Sdk;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Queries.Admin;
using Onboarding.Application.Services;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Services;
using Onboarding.Infrastructure.Keycloak;
using Onboarding.Infrastructure.Persistence;
using Onboarding.Infrastructure.Repositories;

// ReSharper disable once RedundantUsingDirective — explicit for DI clarity

namespace Onboarding.Infrastructure;

/// <summary>
/// Infrastructure DI registration — excluded from coverage as it's configuration code
/// tested via integration tests.
/// </summary>
[ExcludeFromCodeCoverage]
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

        // Company isolation service — set per-request from JWT claims (D-17)
        services.AddScoped<ICurrentCompanyService, CurrentCompanyService>();

        // Permissions service — set per-request by ClientClaimsMiddleware (D-10)
        services.AddScoped<ICurrentCompanyPermissionsService, CurrentCompanyPermissionsService>();

        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAccessGroupRepository, AccessGroupRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();
        services.AddScoped<IAuditService, AuditService>();

        // Keycloak Admin API — service account CC grant (REG-06)
        var keycloakBaseUrl = configuration["Keycloak:AuthServerUrl"]
            ?? throw new InvalidOperationException("Keycloak:AuthServerUrl not configured.");
        var adminClientId = configuration["Keycloak:AdminClientId"]
            ?? "onboarding-api-admin";
        var adminClientSecret = configuration["Keycloak:AdminClientSecret"]
            ?? throw new InvalidOperationException("Keycloak:AdminClientSecret not configured.");

        services.AddClientCredentialsTokenManagement()
            .AddClient("keycloak-admin-client", client =>
            {
                client.ClientId = ClientId.Parse(adminClientId);
                client.ClientSecret = ClientSecret.Parse(adminClientSecret);
                client.TokenEndpoint = new Uri(
                    $"{keycloakBaseUrl.TrimEnd('/')}/realms/client/protocol/openid-connect/token");
            })
            .AddClient("keycloak-admin-backoffice", client =>
            {
                client.ClientId = ClientId.Parse(adminClientId);
                client.ClientSecret = ClientSecret.Parse(adminClientSecret);
                client.TokenEndpoint = new Uri(
                    $"{keycloakBaseUrl.TrimEnd('/')}/realms/backoffice/protocol/openid-connect/token");
            });

        services.AddHttpClient("keycloak-admin-client", client =>
            {
                client.BaseAddress = new Uri(keycloakBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse("keycloak-admin-client"));

        services.AddHttpClient("keycloak-admin-backoffice", client =>
            {
                client.BaseAddress = new Uri(keycloakBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse("keycloak-admin-backoffice"));

        services.AddScoped<IKeycloakUserService, KeycloakUserService>();

        // Email service (Resend.com) — Phase 11 UX-05
        services.AddHttpClient<IEmailService, ResendEmailService>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
        });

        // Password reset token repository
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        // Fundos module repositories (Phase 46)
        services.AddScoped<IFundoRepository, FundoRepository>();
        services.AddScoped<IConsultoriaFundoRepository, ConsultoriaFundoRepository>();
        services.AddScoped<ICustodianteRepository, CustodianteRepository>();
        services.AddScoped<ICedenteRepository, CedenteRepository>();
        services.AddScoped<ITipoAtivoRepository, TipoAtivoRepository>();

        // Admin Fundos cross-company query handlers (Phase 48 — T-48.6, D-8).
        // Handlers live in Infrastructure (require AppDbContext) and are registered here.
        // SECURITY: Only consumed by AdminFundosController (BearerBackoffice + CrossCompanyAccess).
        services.AddScoped<IQueryHandler<ListAdminFundoQuery, PaginatedResult<AdminFundoDto>>,
            ListAdminFundoQueryHandler>();
        services.AddScoped<IQueryHandler<ListAdminConsultoriaQuery, PaginatedResult<AdminConsultoriaFundoDto>>,
            ListAdminConsultoriaQueryHandler>();
        services.AddScoped<IQueryHandler<ListAdminCustodianteQuery, PaginatedResult<AdminCustodianteDto>>,
            ListAdminCustodianteQueryHandler>();
        services.AddScoped<IQueryHandler<ListAdminCedenteQuery, PaginatedResult<AdminCedenteDto>>,
            ListAdminCedenteQueryHandler>();

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
