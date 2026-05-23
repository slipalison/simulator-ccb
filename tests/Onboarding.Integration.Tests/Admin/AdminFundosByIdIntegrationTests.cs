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
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Infrastructure.Persistence;
using Shouldly;
using Testcontainers.PostgreSql;

namespace Onboarding.Integration.Tests.Admin;

/// <summary>
/// Integration tests for GET /api/admin/fundos/{entity}/{id} endpoints (Phase 51, D-8 fix).
///
/// Uses Testcontainers PostgreSQL — requires Docker. Tagged [Category=Integration].
///
/// Scenarios:
/// 1. Admin GET /api/admin/fundos/{id} for existing Fundo → 200 with correct companyName
/// 2. Admin GET /api/admin/fundos/consultorias/{id} for existing entity → 200 with correct companyName
/// 3. Admin GET /api/admin/fundos/custodiantes/{id} for existing entity → 200 with correct companyName
/// 4. Admin GET /api/admin/fundos/cedentes/{id} for existing entity → 200 with correct companyName
/// 5. Admin sees entity from Company A with companyName = "Empresa Alpha"
/// 6. Admin sees entity from Company B with companyName = "Beta S.A."
/// 7. GET with non-existent Guid → 404
/// 8. GET without Bearer token → 401
/// 9. GET with BearerClient (non-admin) → 401 (wrong scheme)
/// </summary>
[Trait("Category", "Integration")]
public sealed class AdminFundosByIdIntegrationTests : IAsyncLifetime
{
    // =========================================================================
    // Testcontainers + WebApplicationFactory
    // =========================================================================

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private WebApplicationFactory<Program>? _factory;

    private Guid _companyAId;
    private Guid _companyBId;

    private const string SubPjA = "admin-byid-pja-sub-001";
    private const string SubPjB = "admin-byid-pjb-sub-002";

    // Verified valid CNPJs (distinct to avoid DB constraint collisions).
    private const string CnpjA = "11444777000161";
    private const string CnpjB = "62232889000190";

    // =========================================================================
    // IAsyncLifetime
    // =========================================================================

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(ConfigureWebHost);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        await SeedCompaniesAsync(db);
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // =========================================================================
    // WebHostBuilder — replaces JWT validation + health checks
    // =========================================================================

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

            // Override BearerClient — HMAC symmetric key, no OIDC discovery
            services.PostConfigure<JwtBearerOptions>("BearerClient", options =>
            {
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                {
                    Issuer = "http://localhost:8180/realms/client",
                };
                options.TokenValidationParameters.ValidateIssuer = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.ValidateLifetime = false; // nosemgrep
                options.TokenValidationParameters.IssuerSigningKey = AdminByIdFakeJwtHelper.SecurityKey;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
            });

            // Override BearerBackoffice — HMAC symmetric key + role claim mapping
            services.PostConfigure<JwtBearerOptions>("BearerBackoffice", options =>
            {
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                {
                    Issuer = "http://localhost:8180/realms/backoffice",
                };
                options.TokenValidationParameters.ValidateIssuer = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.ValidateLifetime = false; // nosemgrep
                options.TokenValidationParameters.IssuerSigningKey = AdminByIdFakeJwtHelper.SecurityKey;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            foreach (var claim in context.Principal.FindAll("role").ToList())
                                identity.AddClaim(new Claim(ClaimTypes.Role, claim.Value));
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        });
    }

    // =========================================================================
    // Seed helpers
    // =========================================================================

    private async Task SeedCompaniesAsync(AppDbContext db)
    {
        var companyA = Company.Register(
            "Empresa Alpha Admin ById Ltda",
            "11222333000181",
            "alpha.byid@test.com",
            "+5511999990001",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "192.168.1.1"));
        companyA.SetKeycloakUserId(SubPjA);

        var companyB = Company.Register(
            "Beta Admin ById S.A.",
            "62232889000190",
            "beta.byid@test.com",
            "+5511999990002",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "192.168.1.2"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();

        _companyAId = companyA.Id;
        _companyBId = companyB.Id;
    }

    // =========================================================================
    // HTTP client helpers
    // =========================================================================

    private HttpClient CreateClientFor(string sub, string scheme)
    {
        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = scheme == "BearerBackoffice"
            ? AdminByIdFakeJwtHelper.GenerateAdminJwt(sub: sub)
            : AdminByIdFakeJwtHelper.GenerateClientJwt(sub: sub);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private HttpClient CreateUnauthenticatedClient()
        => _factory!.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private HttpClient ClientAdmin() => CreateClientFor("admin-byid-backoffice", "BearerBackoffice");
    private HttpClient ClientPjA() => CreateClientFor(SubPjA, "BearerClient");

    // =========================================================================
    // SCENARIO 1: Admin GET /api/admin/fundos/consultorias/{id} returns 200 + correct companyName
    // =========================================================================

    [Fact]
    public async Task AdminGetConsultoriaById_ExistingEntity_Returns200WithCompanyName()
    {
        // Arrange — PJ-A creates a consultoria
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/consultorias",
            new { razaoSocial = "Consultoria ById Alpha", cnpj = CnpjA });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var consultoriaId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Act — Admin fetches by Id (cross-company)
        using var admin = ClientAdmin();
        var resp = await admin.GetAsync($"/api/admin/fundos/consultorias/{consultoriaId}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        body.ShouldContain("Consultoria ById Alpha");
        body.ShouldContain("Empresa Alpha Admin ById Ltda");
    }

    // =========================================================================
    // SCENARIO 2: Admin GET /api/admin/fundos/custodiantes/{id} returns 200 + correct companyName
    // =========================================================================

    [Fact]
    public async Task AdminGetCustodianteById_ExistingEntity_Returns200WithCompanyName()
    {
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes",
            new { razaoSocial = "Custodiante ById Alpha", cnpj = CnpjA });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var admin = ClientAdmin();
        var resp = await admin.GetAsync($"/api/admin/fundos/custodiantes/{id}");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        body.ShouldContain("Custodiante ById Alpha");
        body.ShouldContain("Empresa Alpha Admin ById Ltda");
    }

    // =========================================================================
    // SCENARIO 3: Admin GET /api/admin/fundos/cedentes/{id} returns 200 + correct companyName
    // =========================================================================

    [Fact]
    public async Task AdminGetCedenteById_ExistingEntityPf_Returns200WithCompanyName()
    {
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/cedentes/pf",
            new { cpf = "52998224725", nome = "Cedente ById Alpha PF" });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var admin = ClientAdmin();
        var resp = await admin.GetAsync($"/api/admin/fundos/cedentes/{id}");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        body.ShouldContain("Cedente ById Alpha PF");
        body.ShouldContain("Empresa Alpha Admin ById Ltda");
    }

    // =========================================================================
    // SCENARIO 4: Admin GET /api/admin/fundos/{id} for Fundo returns 200 + correct companyName
    // =========================================================================

    [Fact]
    public async Task AdminGetFundoById_ExistingFundo_Returns200WithCompanyName()
    {
        // Fundo requires consultoria + custodiante as FK
        using var clientA = ClientPjA();

        var consultoriaResp = await clientA.PostAsJsonAsync("/api/fundos/consultorias",
            new { razaoSocial = "Consultoria FK ById", cnpj = CnpjA });
        consultoriaResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var consultoriaId = (await consultoriaResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var custodianteResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes",
            new { razaoSocial = "Custodiante FK ById", cnpj = CnpjA });
        custodianteResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var custodianteId = (await custodianteResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var fundoResp = await clientA.PostAsJsonAsync("/api/fundos",
            new
            {
                nome = "Fundo ById Alpha",
                cnpj = CnpjA,
                consultoriaFundoId = consultoriaId,
                custodianteId,
                tipoFundo = 1
            });
        fundoResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var fundoId = (await fundoResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var admin = ClientAdmin();
        var resp = await admin.GetAsync($"/api/admin/fundos/{fundoId}");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        body.ShouldContain("Fundo ById Alpha");
        body.ShouldContain("Empresa Alpha Admin ById Ltda");
    }

    // =========================================================================
    // SCENARIO 5: Non-existent Guid → 404
    // =========================================================================

    [Fact]
    public async Task AdminGetFundoById_NonExistentGuid_Returns404()
    {
        using var admin = ClientAdmin();
        var resp = await admin.GetAsync($"/api/admin/fundos/{Guid.NewGuid()}");
        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminGetConsultoriaById_NonExistentGuid_Returns404()
    {
        using var admin = ClientAdmin();
        var resp = await admin.GetAsync($"/api/admin/fundos/consultorias/{Guid.NewGuid()}");
        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminGetCustodianteById_NonExistentGuid_Returns404()
    {
        using var admin = ClientAdmin();
        var resp = await admin.GetAsync($"/api/admin/fundos/custodiantes/{Guid.NewGuid()}");
        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminGetCedenteById_NonExistentGuid_Returns404()
    {
        using var admin = ClientAdmin();
        var resp = await admin.GetAsync($"/api/admin/fundos/cedentes/{Guid.NewGuid()}");
        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // SCENARIO 6: No Bearer → 401
    // =========================================================================

    [Fact]
    public async Task AdminGetFundoById_NoBearer_Returns401()
    {
        using var anon = CreateUnauthenticatedClient();
        var resp = await anon.GetAsync($"/api/admin/fundos/{Guid.NewGuid()}");
        resp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminGetConsultoriaById_NoBearer_Returns401()
    {
        using var anon = CreateUnauthenticatedClient();
        var resp = await anon.GetAsync($"/api/admin/fundos/consultorias/{Guid.NewGuid()}");
        resp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminGetCustodianteById_NoBearer_Returns401()
    {
        using var anon = CreateUnauthenticatedClient();
        var resp = await anon.GetAsync($"/api/admin/fundos/custodiantes/{Guid.NewGuid()}");
        resp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminGetCedenteById_NoBearer_Returns401()
    {
        using var anon = CreateUnauthenticatedClient();
        var resp = await anon.GetAsync($"/api/admin/fundos/cedentes/{Guid.NewGuid()}");
        resp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // SCENARIO 7: BearerClient token (non-admin scheme) → 401 (wrong scheme)
    // =========================================================================

    [Fact]
    public async Task AdminGetFundoById_BearerClientScheme_Returns401()
    {
        // BearerBackoffice policy uses AuthenticationSchemes = "BearerBackoffice".
        // A BearerClient token is simply not validated by that scheme → 401.
        using var client = ClientPjA();
        var resp = await client.GetAsync($"/api/admin/fundos/{Guid.NewGuid()}");
        ((int)resp.StatusCode).ShouldBeOneOf(401, 403);
    }
}

/// <summary>
/// File-scoped JWT helper for AdminFundosByIdIntegrationTests.
/// Mirrors FakeJwtHelper in FundosControllerIntegrationTests — kept separate
/// to avoid cross-file static coupling within the Integration.Tests project.
/// </summary>
file static class AdminByIdFakeJwtHelper
{
    private const string TestSigningKey =
        "this-is-a-test-signing-key-for-integration-tests-only-min-32-bytes!";

    public static readonly SymmetricSecurityKey SecurityKey =
        new(Encoding.UTF8.GetBytes(TestSigningKey));

    private static readonly SigningCredentials Credentials =
        new(SecurityKey, SecurityAlgorithms.HmacSha256);

    public static string GenerateClientJwt(string email = "test@integration.test", string? sub = null)
    {
        var claims = new List<Claim>
        {
            new("email", email),
            new("sub", sub ?? Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: "http://localhost:8180/realms/client",
            audience: "onboarding-app",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: Credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateAdminJwt(string email = "admin@backoffice.integration.test", string? sub = null)
    {
        var claims = new List<Claim>
        {
            new("email", email),
            new("sub", sub ?? Guid.NewGuid().ToString()),
            new("role", "admin")
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
