using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Persistence;
using Shouldly;
using Testcontainers.PostgreSql;

namespace Onboarding.Integration.Tests.Admin;

/// <summary>
/// Integration tests for AdminAuditLog entity filter — Phase 52 T-1.
///
/// Verifies the full round-trip:
///   1. Write an AdminAuditLog row with EntityType + EntityId via IAdminAuditLogRepository.
///   2. Query via GET /api/admin/audit-log?entityType=X&entityId=Y.
///   3. Assert only matching rows are returned.
///   4. Assert unfiltered query returns all rows (backward-compat).
///
/// Uses Testcontainers PostgreSQL — requires Docker. Tagged [Category=Integration].
/// </summary>
[Trait("Category", "Integration")]
public sealed class AuditLogEntityFilterIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private WebApplicationFactory<Program>? _factory;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(ConfigureWebHost);

        // Apply schema
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:AppDb", _postgres.GetConnectionString());
        builder.UseSetting("Keycloak:BackofficeRealmUrl", "http://localhost:8180/realms/backoffice");
        builder.UseSetting("Keycloak:ClientRealmUrl", "http://localhost:8180/realms/client");
        builder.UseSetting("Keycloak:AuthServerUrl", "http://localhost:8180/");
        builder.UseSetting("Keycloak:AdminClientId", "onboarding-api-admin");
        builder.UseSetting("Keycloak:AdminClientSecret", "test-secret");
        builder.UseSetting("Keycloak:Realm", "client");
        builder.UseSetting("Keycloak:PublicClientId", "onboarding-app");
        builder.UseSetting("Keycloak:ValidIssuer", "http://localhost:8180/realms/client");

        builder.ConfigureTestServices(services =>
        {
            // Remove health checks that require real Keycloak/PostgreSQL probes
            var configureOptionsType = typeof(IConfigureOptions<HealthCheckServiceOptions>);
            var toRemove = services.Where(d => d.ServiceType == configureOptionsType).ToList();
            foreach (var d in toRemove) services.Remove(d);
            services.AddHealthChecks()
                .AddCheck("stub-healthy", () => HealthCheckResult.Healthy("stub-ok"), ["ready"]);

            // Override BearerClient JWT
            services.PostConfigure<JwtBearerOptions>("BearerClient", options =>
            {
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                {
                    Issuer = "http://localhost:8180/realms/client",
                };
                options.TokenValidationParameters.ValidateIssuer = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.ValidateLifetime = false; // nosemgrep
                options.TokenValidationParameters.IssuerSigningKey = AuditLogFakeJwtHelper.SecurityKey;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
            });

            // Override BearerBackoffice JWT + map "role" → ClaimTypes.Role
            services.PostConfigure<JwtBearerOptions>("BearerBackoffice", options =>
            {
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                {
                    Issuer = "http://localhost:8180/realms/backoffice",
                };
                options.TokenValidationParameters.ValidateIssuer = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.ValidateLifetime = false; // nosemgrep
                options.TokenValidationParameters.IssuerSigningKey = AuditLogFakeJwtHelper.SecurityKey;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            var roleClaims = context.Principal.FindAll("role").ToList();
                            foreach (var claim in roleClaims)
                                identity.AddClaim(new Claim(ClaimTypes.Role, claim.Value));
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        });
    }

    private HttpClient CreateAdminClient()
    {
        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var token = AuditLogFakeJwtHelper.GenerateAdminJwt();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task SeedAuditRowAsync(string entityType, Guid entityId, string? altEntityType = null, Guid? altEntityId = null)
    {
        using var scope = _factory!.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAdminAuditLogRepository>();

        var log1 = AdminAuditLog.Create(
            Guid.NewGuid(), "admin@test.com", ActionType.FundoStatusChanged,
            entityId, "FundoAlpha", "RASCUNHO -> ATIVO",
            entityType: entityType, entityId: entityId);
        await repo.AddAsync(log1);

        // Legacy row without entityType — should not appear in filtered query but in unfiltered
        var legacyLog = AdminAuditLog.Create(
            Guid.NewGuid(), "admin@test.com", ActionType.AdminCreated,
            Guid.NewGuid(), "OtherUser", "legacy entry");
        await repo.AddAsync(legacyLog);

        if (altEntityType is not null && altEntityId.HasValue)
        {
            var log2 = AdminAuditLog.Create(
                Guid.NewGuid(), "admin@test.com", ActionType.RelFundoCedenteStatusChanged,
                altEntityId.Value, "FundoCedenteX", "ATIVO -> SUSPENSO",
                entityType: altEntityType, entityId: altEntityId.Value);
            await repo.AddAsync(log2);
        }

        await repo.SaveChangesAsync();
    }

    // =========================================================================
    // T-1 round-trip: write then filter by entityType + entityId → 200 filtered
    // =========================================================================

    [Fact]
    public async Task GetAuditLog_FilterByEntityTypeAndEntityId_ReturnsOnlyMatchingRow()
    {
        // Arrange — seed a Fundo audit row and an unrelated row
        var fundoId = Guid.NewGuid();
        await SeedAuditRowAsync("Fundo", fundoId);

        using var client = CreateAdminClient();

        // Act — filter by both entityType + entityId
        var response = await client.GetAsync(
            $"/api/admin/audit-log?entityType=Fundo&entityId={fundoId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResultDto>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(1);
        result.Items.ShouldNotBeNull();
        result.Items.Length.ShouldBe(1);
        result.Items[0].EntityType.ShouldBe("Fundo");
        result.Items[0].EntityId.ShouldBe(fundoId);
    }

    [Fact]
    public async Task GetAuditLog_FilterByEntityTypeOnly_ReturnsAllRowsWithThatType()
    {
        // Arrange — seed two Fundo rows (different IDs) + one FundoCedente row
        var fundoId1 = Guid.NewGuid();
        var fundoCedenteId = Guid.NewGuid();
        await SeedAuditRowAsync("Fundo", fundoId1, "FundoCedente", fundoCedenteId);

        using var client = CreateAdminClient();

        // Act — filter only by entityType=Fundo
        var response = await client.GetAsync("/api/admin/audit-log?entityType=Fundo");

        // Assert — only Fundo row(s) returned
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResultDto>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        result.ShouldNotBeNull();
        result.Items.ShouldNotBeNull();
        result.Items!.ShouldAllBe(i => i.EntityType == "Fundo");
    }

    [Fact]
    public async Task GetAuditLog_NoFilter_BackwardCompat_ReturnsAllRows()
    {
        // Arrange — seed rows with and without entityType
        var fundoId = Guid.NewGuid();
        await SeedAuditRowAsync("Fundo", fundoId);

        using var client = CreateAdminClient();

        // Act — no entity filter
        var response = await client.GetAsync("/api/admin/audit-log");

        // Assert — response includes both scoped and legacy rows
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResultDto>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        result.ShouldNotBeNull();
        // At least 2 rows: the seeded Fundo row + the legacy row (no entityType)
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAuditLog_FilterByEntityId_WithNonExistentId_ReturnsEmpty()
    {
        // Arrange — no rows for this ID
        var unknownId = Guid.NewGuid();
        using var client = CreateAdminClient();

        // Act
        var response = await client.GetAsync(
            $"/api/admin/audit-log?entityType=Fundo&entityId={unknownId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResultDto>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(0);
        result.Items!.Length.ShouldBe(0);
    }

    [Fact]
    public async Task GetAuditLog_WithoutAdminBearer_Returns401()
    {
        using var client = _factory!.CreateClient();
        var response = await client.GetAsync("/api/admin/audit-log?entityType=Fundo");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // JSON DTO projection types for deserialization (self-contained)
    // =========================================================================

    private sealed class PaginatedResultDto
    {
        public AuditLogItemDto[]? Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private sealed class AuditLogItemDto
    {
        public Guid Id { get; set; }
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public string? ActionType { get; set; }
        public string? AdminUserName { get; set; }
    }
}

/// <summary>
/// HMAC JWT helper for AuditLog integration tests — file-scoped to avoid cross-project dependency.
/// </summary>
file static class AuditLogFakeJwtHelper
{
    private const string TestSigningKey =
        "this-is-a-test-signing-key-for-integration-tests-only-min-32-bytes!";

    public static readonly SymmetricSecurityKey SecurityKey =
        new(Encoding.UTF8.GetBytes(TestSigningKey));

    private static readonly SigningCredentials Credentials =
        new(SecurityKey, SecurityAlgorithms.HmacSha256);

    public static string GenerateAdminJwt(string email = "admin@backoffice.integration.test", string? sub = null)
    {
        var claims = new List<Claim>
        {
            new("email", email),
            new("sub", sub ?? Guid.NewGuid().ToString()),
            new("role", "admin"),
        };

        var token = new JwtSecurityToken(
            issuer: "http://localhost:8180/realms/backoffice",
            audience: "http://localhost:8180/realms/backoffice",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: Credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
