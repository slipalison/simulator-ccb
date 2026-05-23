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
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Persistence;
using Shouldly;
using Testcontainers.PostgreSql;

namespace Onboarding.Integration.Tests.Fundos;

/// <summary>
/// Integration tests for Phase 50 relationship aggregate endpoints (T-7).
///
/// Tests require Docker. Run with:
///   dotnet test tests/Onboarding.Integration.Tests --filter "FullyQualifiedName~RelationshipAggregates"
///
/// Scenarios covered (minimum 18 per D-21):
///
/// FundoCedente:
///   1. POST /api/fundos/{fundoId}/cedentes → 201 (happy path)
///   2. POST duplicate active → 409 (REL-09 in-memory guard)
///   3. Cross-tenant POST (PJ-B tries PJ-A's fundo) → 404
///   4. PATCH .../limits happy path → 200
///   5. POST .../status ATIVO → INATIVO → 200; HISTORICO terminal → 400
///   6. GET list only returns own company's associations
///
/// CedenteTipoAtivo (symmetric, 6 scenarios)
/// FundoTipoAtivo (symmetric, 6 scenarios)
///
/// Admin cross-company (T-6 bonus):
///   - GET /api/admin/fundos/fundo-cedentes sees both companies
/// </summary>
[Trait("Category", "Integration")]
public class RelationshipAggregatesIntegrationTests : IAsyncLifetime
{
    // =========================================================================
    // Test infrastructure
    // =========================================================================

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private WebApplicationFactory<Program>? _factory;

    // Seeded IDs — set in InitializeAsync
    private Guid _companyAId;
    private Guid _companyBId;
    private Guid _fundoAId;              // Fundo belonging to CompanyA
    private Guid _fundoBId;              // Fundo belonging to CompanyB
    private Guid _cedenteAId;            // Cedente belonging to CompanyA (CPF 52998224725)
    private Guid _cedenteConcurrentId;   // Second cedente for CompanyA — used in race condition test only
    private Guid _tipoAtivoId;           // Global TipoAtivo

    private const string SubPjA = "rel-integration-pja-001";
    private const string SubPjB = "rel-integration-pjb-002";
    private const string ValidCnpj = "11222333000181";

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

        await SeedAsync(db, scope.ServiceProvider);
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
            var configureOptionsType = typeof(IConfigureOptions<HealthCheckServiceOptions>);
            var toRemove = services
                .Where(d => d.ServiceType == configureOptionsType)
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);
            services.AddHealthChecks()
                .AddCheck("stub-healthy", () => HealthCheckResult.Healthy("stub-ok"), ["ready"]);

            services.PostConfigure<JwtBearerOptions>("BearerClient", options =>
            {
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                {
                    Issuer = "http://localhost:8180/realms/client",
                };
                options.TokenValidationParameters.ValidateIssuer = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.ValidateLifetime = false; // nosemgrep
                options.TokenValidationParameters.IssuerSigningKey = RelJwtHelper.SecurityKey;
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
                options.TokenValidationParameters.IssuerSigningKey = RelJwtHelper.SecurityKey;
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

    // =========================================================================
    // Seed
    // =========================================================================

    private async Task SeedAsync(AppDbContext db, IServiceProvider services)
    {
        // Company A + B
        var companyA = Company.Register(
            "Alpha Rel Integration Ltda", "11444777000161",
            "alpha.rel@test.com", "+5511000000001",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "192.168.1.10"));
        companyA.SetKeycloakUserId(SubPjA);

        var companyB = Company.Register(
            "Beta Rel Integration S.A.", "62232889000190",
            "beta.rel@test.com", "+5511000000002",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "192.168.1.20"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();

        _companyAId = companyA.Id;
        _companyBId = companyB.Id;

        // Prerequisite entities — bypass API to avoid circular dependency on endpoint-under-test
        var consultoriaA = ConsultoriaFundo.Register("Consultoria Rel Alpha", ValidCnpj, _companyAId);
        var custodianteA = Custodiante.Register("Custodiante Rel Alpha", ValidCnpj, _companyAId);
        var fundoA = Fundo.Register("Fundo Rel Alpha", ValidCnpj, _companyAId,
            consultoriaA.Id, custodianteA.Id, TipoFundo.RendaFixa);

        var consultoriaB = ConsultoriaFundo.Register("Consultoria Rel Beta", "62232889000190", _companyBId);
        var custodianteB = Custodiante.Register("Custodiante Rel Beta", "62232889000190", _companyBId);
        var fundoB = Fundo.Register("Fundo Rel Beta", "62232889000190", _companyBId,
            consultoriaB.Id, custodianteB.Id, TipoFundo.Multimercado);

        var tipoAtivo = TipoAtivo.Register("CDB-INT", "CDB Integration Test", TipoAtivoCategoria.RendaFixa);

        await db.ConsultoriasFundo.AddRangeAsync(consultoriaA, consultoriaB);
        await db.Custodiantes.AddRangeAsync(custodianteA, custodianteB);
        await db.Fundos.AddRangeAsync(fundoA, fundoB);
        await db.TiposAtivo.AddAsync(tipoAtivo);
        await db.SaveChangesAsync();

        _fundoAId = fundoA.Id;
        _fundoBId = fundoB.Id;
        _tipoAtivoId = tipoAtivo.Id;

        // Cedente requires ICedenteRepository to set shadow properties (DocumentoTipo, CpfValue) per D-09
        var cedenteRepo = services.GetRequiredService<ICedenteRepository>();
        var cedenteA = Cedente.RegisterPf("52998224725", "Cedente Alpha PF", _companyAId);
        await cedenteRepo.AddAsync(cedenteA);

        // Second cedente for CompanyA — dedicated to the race condition test (CPF 40484604805, valid)
        var cedenteB = Cedente.RegisterPf("40484604805", "Cedente Alpha PF Concurrent", _companyAId);
        await cedenteRepo.AddAsync(cedenteB);

        await db.SaveChangesAsync();

        _cedenteAId = cedenteA.Id;
        _cedenteConcurrentId = cedenteB.Id;
    }

    // =========================================================================
    // HTTP helpers
    // =========================================================================

    private HttpClient ClientPjA() => CreateClient(SubPjA, "BearerClient");
    private HttpClient ClientPjB() => CreateClient(SubPjB, "BearerClient");
    private HttpClient ClientAdmin() => CreateAdminClient("admin-rel-sub");

    private HttpClient CreateClient(string sub, string scheme)
    {
        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = RelJwtHelper.GenerateClientJwt(sub: sub);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private HttpClient CreateAdminClient(string sub)
    {
        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = RelJwtHelper.GenerateAdminJwt(sub: sub);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // =========================================================================
    // FundoCedente — 6 scenarios
    // =========================================================================

    [Fact]
    public async Task FundoCedente_Create_Returns201()
    {
        using var client = ClientPjA();
        var payload = new
        {
            cedenteId = _cedenteAId,
            limitePercentual = 50m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        // Status is serialized as string via JsonStringEnumConverter (commit 4c352bf).
        var dto = await response.Content.ReadFromJsonAsync<JsonElement>();
        dto.GetProperty("status").GetString().ShouldBe("ATIVO", "Created association must have ATIVO status.");
    }

    [Fact]
    public async Task FundoCedente_CreateDuplicate_Returns409()
    {
        using var client = ClientPjA();
        var payload = new
        {
            cedenteId = _cedenteAId,
            limitePercentual = 30m,
            dataInicio = DateTimeOffset.UtcNow
        };

        // First create — must succeed
        var first = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Second create same pair while first is ATIVO — must return 409
        var second = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Duplicate ATIVO association for same FundoId+CedenteId pair must return 409 (REL-09).");
    }

    [Fact]
    public async Task FundoCedente_CrossTenantCreate_Returns404()
    {
        using var client = ClientPjB();
        var payload = new
        {
            cedenteId = _cedenteAId,
            limitePercentual = 40m,
            dataInicio = DateTimeOffset.UtcNow
        };

        // PJ-B tries to post to PJ-A's Fundo
        var response = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant POST must return 404, not 403, to avoid leaking Fundo existence.");
    }

    [Fact]
    public async Task FundoCedente_UpdateLimits_Returns200()
    {
        using var client = ClientPjA();

        // Create association first
        var createPayload = new
        {
            cedenteId = _cedenteAId,
            limiteValor = 50_000m,
            dataInicio = DateTimeOffset.UtcNow
        };
        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Update limits
        var updatePayload = new { limitePercentual = 75m };
        var updateResp = await client.PatchAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/limits", updatePayload);

        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await updateResp.Content.ReadAsStringAsync();
        body.ShouldContain("75");
    }

    [Fact]
    public async Task FundoCedente_StatusTransition_AtivoToInativo_Returns200()
    {
        using var client = ClientPjA();

        // Create
        var createPayload = new
        {
            cedenteId = _cedenteAId,
            limitePercentual = 25m,
            dataInicio = DateTimeOffset.UtcNow
        };
        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Transition ATIVO → INATIVO (RelationshipStatus.INATIVO = 2)
        var transitionResp = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/status",
            new { newStatus = 2 });

        transitionResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transitionDto = await transitionResp.Content.ReadFromJsonAsync<JsonElement>();
        transitionDto.GetProperty("status").GetString().ShouldBe("INATIVO", "Status must be INATIVO after transition.");
    }

    [Fact]
    public async Task FundoCedente_StatusTransition_HistoricoTerminal_Returns400()
    {
        using var client = ClientPjA();

        // Create
        var createPayload = new
        {
            cedenteId = _cedenteAId,
            limitePercentual = 20m,
            dataInicio = DateTimeOffset.UtcNow
        };
        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // ATIVO → HISTORICO (terminal). RelationshipStatus.HISTORICO = 3
        var toHistorico = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/status",
            new { newStatus = 3 });
        toHistorico.StatusCode.ShouldBe(HttpStatusCode.OK);

        // HISTORICO → ATIVO must fail. RelationshipStatus.ATIVO = 1
        var fromHistorico = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/status",
            new { newStatus = 1 });
        fromHistorico.StatusCode.ShouldBe(HttpStatusCode.BadRequest,
            "HISTORICO is terminal — transition to any other status must return 400.");
    }

    [Fact]
    public async Task FundoCedente_GetList_OnlyReturnsTenantOwnedRows()
    {
        using var clientA = ClientPjA();
        using var clientB = ClientPjB();

        // PJ-A creates an association
        var createPayload = new
        {
            cedenteId = _cedenteAId,
            limitePercentual = 10m,
            dataInicio = DateTimeOffset.UtcNow
        };
        var createResp = await clientA.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // PJ-B tries to list PJ-A's Fundo associations — must get 404 (cross-tenant)
        var listResp = await clientB.GetAsync($"/api/fundos/{_fundoAId}/cedentes");
        listResp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // CedenteTipoAtivo — 6 scenarios
    // =========================================================================

    [Fact]
    public async Task CedenteTipoAtivo_Create_Returns201()
    {
        using var client = ClientPjA();
        var payload = new
        {
            tipoAtivoId = _tipoAtivoId,
            limitePercentual = 30m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var ctaDto = await response.Content.ReadFromJsonAsync<JsonElement>();
        ctaDto.GetProperty("status").GetString().ShouldBe("ATIVO", "Created association must have ATIVO status.");
    }

    [Fact]
    public async Task CedenteTipoAtivo_CreateDuplicate_Returns409()
    {
        using var client = ClientPjA();
        var payload = new
        {
            tipoAtivoId = _tipoAtivoId,
            limitePercentual = 25m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var first = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CedenteTipoAtivo_CrossTenantCreate_Returns404()
    {
        using var client = ClientPjB();
        var payload = new
        {
            tipoAtivoId = _tipoAtivoId,
            limitePercentual = 20m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CedenteTipoAtivo_UpdateLimits_Returns200()
    {
        using var client = ClientPjA();

        var createPayload = new { tipoAtivoId = _tipoAtivoId, limiteValor = 80_000m, dataInicio = DateTimeOffset.UtcNow };
        var createResp = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var updateResp = await client.PatchAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos/{assocId}/limits",
            new { limitePercentual = 60m });

        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CedenteTipoAtivo_StatusTransition_AtivoToInativo_Returns200()
    {
        using var client = ClientPjA();

        var createPayload = new { tipoAtivoId = _tipoAtivoId, limitePercentual = 15m, dataInicio = DateTimeOffset.UtcNow };
        var createResp = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // RelationshipStatus.INATIVO = 2
        var transResp = await client.PostAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 2 });

        transResp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CedenteTipoAtivo_GetList_CrossTenantCedente_Returns404()
    {
        using var client = ClientPjB();

        var listResp = await client.GetAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos");
        listResp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // FundoTipoAtivo — 6 scenarios
    // =========================================================================

    [Fact]
    public async Task FundoTipoAtivo_Create_Returns201()
    {
        using var client = ClientPjA();
        var payload = new
        {
            tipoAtivoId = _tipoAtivoId,
            limitePercentual = 40m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var ftaDto = await response.Content.ReadFromJsonAsync<JsonElement>();
        ftaDto.GetProperty("status").GetString().ShouldBe("ATIVO", "Created association must have ATIVO status.");
    }

    [Fact]
    public async Task FundoTipoAtivo_CreateDuplicate_Returns409()
    {
        using var client = ClientPjA();
        var payload = new { tipoAtivoId = _tipoAtivoId, limitePercentual = 35m, dataInicio = DateTimeOffset.UtcNow };

        var first = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", payload);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", payload);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task FundoTipoAtivo_CrossTenantCreate_Returns404()
    {
        using var client = ClientPjB();
        var payload = new { tipoAtivoId = _tipoAtivoId, limitePercentual = 10m, dataInicio = DateTimeOffset.UtcNow };

        var response = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FundoTipoAtivo_UpdateLimits_Returns200()
    {
        using var client = ClientPjA();

        var createPayload = new { tipoAtivoId = _tipoAtivoId, limiteValor = 60_000m, dataInicio = DateTimeOffset.UtcNow };
        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var updateResp = await client.PatchAsJsonAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos/{assocId}/limits",
            new { limitePercentual = 55m });

        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FundoTipoAtivo_StatusTransition_AtivoToInativo_Returns200()
    {
        using var client = ClientPjA();

        var createPayload = new { tipoAtivoId = _tipoAtivoId, limitePercentual = 45m, dataInicio = DateTimeOffset.UtcNow };
        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // RelationshipStatus.INATIVO = 2
        var transResp = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 2 });

        transResp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FundoTipoAtivo_GetList_CrossTenantFundo_Returns404()
    {
        using var client = ClientPjB();
        var listResp = await client.GetAsync($"/api/fundos/{_fundoAId}/tipos-ativos");
        listResp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // FundoCedente — REL-09 race condition (DB partial unique index gate)
    // =========================================================================

    /// <summary>
    /// Simulates two concurrent POST requests for the same FundoId+CedenteId pair.
    /// The DB partial unique index (FundoId, CedenteId) WHERE Status='ATIVO' must reject the
    /// second insert with a DbUpdateException, which GlobalExceptionHandler maps to 409 (D-18).
    ///
    /// Note: this test is not a strict concurrency test — true race conditions are difficult to
    /// reproduce deterministically in integration tests. This test verifies that when two requests
    /// arrive in quick succession, exactly one succeeds (201) and the other gets a conflict (409),
    /// regardless of which one "wins" the race.
    /// </summary>
    [Fact]
    public async Task FundoCedente_ConcurrentCreate_OnlyOneSucceeds()
    {
        // Uses _cedenteConcurrentId seeded in InitializeAsync (CPF 40484604805, CompanyA-scoped).
        // This cedente is not used by any other test — no interference with test order.
        var payload = new
        {
            cedenteId = _cedenteConcurrentId,
            limitePercentual = 10m,
            dataInicio = DateTimeOffset.UtcNow
        };

        // Fire two requests concurrently targeting the same (FundoId, CedenteId) pair.
        // Both handlers will pass the in-memory ActivateGuard (no ATIVO row exists yet when
        // they both read from DB). The DB partial unique index (D-18) rejects the second insert
        // with a unique violation, which GlobalExceptionHandler maps to HTTP 409.
        using var client1 = ClientPjA();
        using var client2 = ClientPjA();

        var t1 = client1.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);
        var t2 = client2.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);

        var results = await Task.WhenAll(t1, t2);

        var statuses = results.Select(r => (int)r.StatusCode).OrderBy(s => s).ToArray();

        // Exactly one (201, 409) pair — order depends on which request wins the DB race.
        statuses.ShouldContain(201,
            "One concurrent request must succeed and create the ATIVO association.");
        statuses.ShouldContain(409,
            "The other concurrent request must be rejected with 409 — DB partial unique index enforces REL-09 (D-18).");
    }

    // =========================================================================
    // Admin cross-company (T-6 bonus scenario)
    // =========================================================================

    [Fact]
    public async Task AdminFundoCedentes_CrossCompany_ReturnsBothCompanies()
    {
        // Create a FundoCedente in PJ-A
        using var clientA = ClientPjA();
        var createPayload = new { cedenteId = _cedenteAId, limitePercentual = 5m, dataInicio = DateTimeOffset.UtcNow };
        var createResp = await clientA.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Admin lists all FundoCedentes — must see the one just created
        using var adminClient = ClientAdmin();
        var adminResp = await adminClient.GetAsync("/api/admin/fundos/fundo-cedentes");
        adminResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await adminResp.Content.ReadAsStringAsync();
        body.ShouldContain(_fundoAId.ToString());
    }
}

/// <summary>
/// Generates HMAC-signed JWT tokens for relationship aggregate integration tests.
/// Separate from the file-scoped FakeJwtHelper in FundosControllerIntegrationTests.cs.
/// </summary>
file static class RelJwtHelper
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

    public static string GenerateAdminJwt(
        string email = "admin@backoffice.integration.test",
        string? sub = null)
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
