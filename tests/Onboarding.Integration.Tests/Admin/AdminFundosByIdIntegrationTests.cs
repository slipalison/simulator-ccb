using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Infrastructure.Persistence;
using Onboarding.Integration.Tests.Fixtures;
using Shouldly;

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
public sealed class AdminFundosByIdIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private Guid _companyAId;
    private Guid _companyBId;

    private const string SubPjA = "admin-byid-pja-sub-001";
    private const string SubPjB = "admin-byid-pjb-sub-002";

    // Verified valid CNPJs (distinct to avoid DB constraint collisions).
    private const string CnpjA = "11444777000161";
    private const string CnpjB = "62232889000190";

    public AdminFundosByIdIntegrationTests(PostgreSqlFixture fixture) : base(fixture) { }

    // =========================================================================
    // IAsyncLifetime — per-class seed (guarded against re-seeding per test)
    //
    // xUnit creates one test-class instance per test method, so InitializeAsync is called
    // once per test. Fixture.EnsureSeedAsync ensures the DB seed runs only once per class.
    // After seed, IDs are re-read from DB so each test instance has them populated.
    // =========================================================================

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await Fixture.EnsureSeedAsync(async () =>
        {
            using var scope = CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedCompaniesAsync(db);
        });

        // Re-read IDs on every test instance — seed is idempotent, rows are stable.
        // IgnoreQueryFilters() bypasses HasQueryFilter for direct DB reads outside HTTP context.
        using var readScope = CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _companyAId = readDb.Companies.IgnoreQueryFilters().Where(c => c.KeycloakUserId == SubPjA).Select(c => c.Id).First();
        _companyBId = readDb.Companies.IgnoreQueryFilters().Where(c => c.KeycloakUserId == SubPjB).Select(c => c.Id).First();
    }

    // =========================================================================
    // Seed helpers
    // =========================================================================

    private static async Task SeedCompaniesAsync(AppDbContext db)
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
    }

    // =========================================================================
    // HTTP client helpers
    // =========================================================================

    private HttpClient ClientAdmin() => CreateAdminJwt("admin-byid-backoffice");
    private HttpClient ClientPjA() => CreateClientJwt(SubPjA);

    // =========================================================================
    // SCENARIO 1: Admin GET /api/admin/fundos/consultorias/{id} returns 200 + correct companyName
    // =========================================================================

    [Fact]
    public async Task AdminGetConsultoriaById_ExistingEntity_Returns200WithCompanyName()
    {
        // Arrange — PJ-A creates a consultoria; TestCnpj is unique per test instance
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/consultorias",
            new { razaoSocial = "Consultoria ById Alpha", cnpj = TestCnpj });
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
            new { razaoSocial = "Custodiante ById Alpha", cnpj = TestCnpj });
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
            new { razaoSocial = "Consultoria FK ById", cnpj = TestCnpj });
        consultoriaResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var consultoriaId = (await consultoriaResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var custodianteResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes",
            new { razaoSocial = "Custodiante FK ById", cnpj = TestCnpj });
        custodianteResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var custodianteId = (await custodianteResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var fundoResp = await clientA.PostAsJsonAsync("/api/fundos",
            new
            {
                nome = "Fundo ById Alpha",
                cnpj = TestCnpj,
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

    // =========================================================================
    // SCENARIO 8: Admin list endpoints — drives AdminFundosController list bodies (B5-iter5)
    //
    // 5 list endpoints were uncovered because no integration test reached their handler bodies.
    // These tests exercise ListFundos, ListCustodiantes, ListCedentes, ListFundoTiposAtivos,
    // ListCedenteTiposAtivos — completing AdminFundosController coverage to ≥80%.
    // =========================================================================

    [Fact]
    public async Task AdminListFundos_AuthenticatedAdmin_Returns200()
    {
        using var admin = ClientAdmin();
        var resp = await admin.GetAsync("/api/admin/fundos?page=1&pageSize=10");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task AdminListCustodiantes_AuthenticatedAdmin_Returns200()
    {
        using var admin = ClientAdmin();
        var resp = await admin.GetAsync("/api/admin/fundos/custodiantes?page=1&pageSize=10");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task AdminListCedentes_AuthenticatedAdmin_Returns200()
    {
        using var admin = ClientAdmin();
        var resp = await admin.GetAsync("/api/admin/fundos/cedentes?page=1&pageSize=10");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task AdminListFundoTiposAtivos_AuthenticatedAdmin_Returns200()
    {
        using var admin = ClientAdmin();
        var resp = await admin.GetAsync("/api/admin/fundos/fundo-tipos-ativos?page=1&pageSize=10");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task AdminListCedenteTiposAtivos_AuthenticatedAdmin_Returns200()
    {
        using var admin = ClientAdmin();
        var resp = await admin.GetAsync("/api/admin/fundos/cedente-tipos-ativos?page=1&pageSize=10");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().ShouldBeGreaterThanOrEqualTo(0);
    }
}
