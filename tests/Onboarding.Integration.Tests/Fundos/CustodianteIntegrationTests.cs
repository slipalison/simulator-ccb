using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Infrastructure.Persistence;
using Onboarding.Integration.Tests.Fixtures;
using Shouldly;

namespace Onboarding.Integration.Tests.Fundos;

/// <summary>
/// Integration tests for Custodiante CRUD + company-scoped CNPJ uniqueness (D-10) + multi-tenant.
///
/// DoD T-2 coverage for Custodiante (analogous to ConsultoriaFundo):
///   - POST /api/fundos/custodiantes → 201
///   - GET /api/fundos/custodiantes/{id} → 200
///   - GET /api/fundos/custodiantes → 200 paginated
///   - PUT /api/fundos/custodiantes/{id} → 200
///   - Duplicate CNPJ same company → 409 (D-10 composite unique index)
///   - Same CNPJ in different company → 201 (D-10: uniqueness is company-scoped)
///   - Multi-tenant: PJ-B GET/PUT of PJ-A entity → 404
///   - Validation: missing required field → 422
///   - No auth → 401
///
/// Requires Docker (Testcontainers PostgreSQL). Tag: [Category=Integration]
/// </summary>
[Trait("Category", "Integration")]
public sealed class CustodianteIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private Guid _companyAId;
    private Guid _companyBId;

    private const string SubPjA = "cust-crud-pja-sub-001";
    private const string SubPjB = "cust-crud-pjb-sub-002";

    public CustodianteIntegrationTests(PostgreSqlFixture fixture) : base(fixture) { }

    // =========================================================================
    // IAsyncLifetime — per-class seed
    // =========================================================================

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await Fixture.EnsureSeedAsync(async () =>
        {
            using var scope = CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedAsync(db);
        });

        using var readScope = CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _companyAId = readDb.Companies.IgnoreQueryFilters()
            .Where(c => c.KeycloakUserId == SubPjA).Select(c => c.Id).First();
        _companyBId = readDb.Companies.IgnoreQueryFilters()
            .Where(c => c.KeycloakUserId == SubPjB).Select(c => c.Id).First();
    }

    private static async Task SeedAsync(AppDbContext db)
    {
        var companyA = Company.Register(
            "Alpha Custodiante CRUD Ltda", "45343410000131",
            "alpha.cust.crud@test.com", "+5511000000030",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "192.168.1.50"));
        companyA.SetKeycloakUserId(SubPjA);

        var companyB = Company.Register(
            "Beta Custodiante CRUD S.A.", "57838178000197",
            "beta.cust.crud@test.com", "+5511000000031",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "192.168.1.51"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();
    }

    // =========================================================================
    // HTTP client helpers
    // =========================================================================

    private HttpClient ClientPjA() => CreateClientJwt(SubPjA);
    private HttpClient ClientPjB() => CreateClientJwt(SubPjB);

    // =========================================================================
    // SCENARIO: POST → 201 (happy path)
    // =========================================================================

    [Fact]
    public async Task RegisterCustodiante_HappyPath_Returns201()
    {
        using var client = ClientPjA();
        var response = await client.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante CRUD {TestId}",
            cnpj = TestCnpj
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
        (body.GetProperty("razaoSocial").GetString() ?? "").ShouldContain("Custodiante CRUD");
        // Custodiante starts as ATIVO (domain factory default)
        body.GetProperty("status").GetString().ShouldBe("ATIVO");
    }

    // =========================================================================
    // SCENARIO: GET /api/fundos/custodiantes/{id} → 200
    // =========================================================================

    [Fact]
    public async Task GetCustodianteById_OwnedByPjA_Returns200()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante GET {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var getResp = await client.GetAsync($"/api/fundos/custodiantes/{id}");
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().ShouldBe(id);
    }

    // =========================================================================
    // SCENARIO: GET /api/fundos/custodiantes → 200 paginated
    // =========================================================================

    [Fact]
    public async Task ListCustodiantes_PjA_Returns200Paginated()
    {
        using var client = ClientPjA();

        await client.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante List {TestId}",
            cnpj = TestCnpj
        });

        var listResp = await client.GetAsync("/api/fundos/custodiantes?page=1&pageSize=20");
        listResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("items", out _).ShouldBeTrue("Paginated result must have 'items' property.");
        body.TryGetProperty("totalCount", out var tc).ShouldBeTrue();
        tc.GetInt32().ShouldBeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // SCENARIO: PUT /api/fundos/custodiantes/{id} → 200
    // =========================================================================

    [Fact]
    public async Task UpdateCustodiante_HappyPath_Returns200WithUpdatedName()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante Before {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var updateResp = await client.PutAsJsonAsync($"/api/fundos/custodiantes/{id}", new
        {
            razaoSocial = $"Custodiante After {TestId}",
            codigoInterno = (string?)null,
            email = (string?)null,
            telefone = (string?)null,
            status = 1  // CustodianteStatus.ATIVO
        });

        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await updateResp.Content.ReadFromJsonAsync<JsonElement>();
        (body.GetProperty("razaoSocial").GetString() ?? "").ShouldContain("After");
    }

    // =========================================================================
    // SCENARIO: PUT /api/fundos/custodiantes/{id} with codigoInterno → 200
    // =========================================================================

    [Fact]
    public async Task UpdateCustodiante_WithCodigoInterno_Returns200()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante CodigoInterno {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var updateResp = await client.PutAsJsonAsync($"/api/fundos/custodiantes/{id}", new
        {
            razaoSocial = $"Custodiante CodigoInterno {TestId}",
            codigoInterno = $"COD-{TestId}",
            email = (string?)null,
            telefone = (string?)null,
            status = 1
        });

        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await updateResp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("codigoInterno").GetString().ShouldBe($"COD-{TestId}");
    }

    // =========================================================================
    // SCENARIO: Duplicate CNPJ same company → 409 (D-10)
    // =========================================================================

    [Fact]
    public async Task RegisterCustodiante_DuplicateCnpjSameCompany_Returns409()
    {
        using var client = ClientPjA();
        var sharedCnpj = TestCnpj;

        var first = await client.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante Dup 1 {TestId}",
            cnpj = sharedCnpj
        });
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante Dup 2 {TestId}",
            cnpj = sharedCnpj
        });
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Duplicate CNPJ within same company must return 409 — D-10 composite unique index (ClientId, Cnpj).");
    }

    // =========================================================================
    // SCENARIO: Same CNPJ different company → 201 (D-10: uniqueness is company-scoped)
    // =========================================================================

    [Fact]
    public async Task RegisterCustodiante_SameCnpjDifferentCompany_Returns201ForBoth()
    {
        var sharedCnpj = TestCnpj;

        using var clientA = ClientPjA();
        var respA = await clientA.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante CrossCnpj A {TestId}",
            cnpj = sharedCnpj
        });
        respA.StatusCode.ShouldBe(HttpStatusCode.Created);

        // PJ-B creates with SAME CNPJ — must succeed because uniqueness is company-scoped (D-10)
        using var clientB = ClientPjB();
        var respB = await clientB.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante CrossCnpj B {TestId}",
            cnpj = sharedCnpj
        });
        respB.StatusCode.ShouldBe(HttpStatusCode.Created,
            "Same CNPJ in different company must succeed — uniqueness is (ClientId, Cnpj), not just Cnpj (D-10).");
    }

    // =========================================================================
    // SCENARIO: GET /api/fundos/custodiantes/{id} cross-tenant → 404
    // =========================================================================

    [Fact]
    public async Task GetCustodianteById_CrossTenant_Returns404()
    {
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante CrossTenant GET {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var clientB = ClientPjB();
        var resp = await clientB.GetAsync($"/api/fundos/custodiantes/{id}");

        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant GET-by-id must return 404 to avoid leaking entity existence.");
    }

    // =========================================================================
    // SCENARIO: PUT /api/fundos/custodiantes/{id} cross-tenant → 404
    // =========================================================================

    [Fact]
    public async Task UpdateCustodiante_CrossTenant_Returns404()
    {
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante CrossTenant PUT {TestId}",
            cnpj = TestCnpj
        });
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var clientB = ClientPjB();
        var resp = await clientB.PutAsJsonAsync($"/api/fundos/custodiantes/{id}", new
        {
            razaoSocial = "Hijacked Custodiante",
            codigoInterno = (string?)null,
            email = (string?)null,
            telefone = (string?)null,
            status = 1
        });

        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant PUT must return 404 to avoid leaking entity existence.");
    }

    // =========================================================================
    // SCENARIO: Multi-tenant list isolation
    // =========================================================================

    [Fact]
    public async Task ListCustodiantes_PjB_DoesNotSeePjARows()
    {
        using var clientA = ClientPjA();
        await clientA.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante Isolation {TestId}",
            cnpj = TestCnpj
        });

        using var clientB = ClientPjB();
        var listResp = await clientB.GetAsync("/api/fundos/custodiantes");
        listResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await listResp.Content.ReadAsStringAsync();
        // PJ-B must not see PJ-A's custodiantes via HasQueryFilter multi-tenant isolation
        body.ShouldNotContain($"Custodiante Isolation {TestId}");
    }

    // =========================================================================
    // SCENARIO: Missing required field → 422
    // =========================================================================

    [Fact]
    public async Task RegisterCustodiante_MissingRazaoSocial_Returns422()
    {
        using var client = ClientPjA();
        var response = await client.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            // razaoSocial intentionally missing
            cnpj = TestCnpj
        });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity,
            "Missing required 'razaoSocial' must return 422 UnprocessableEntity.");
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("error", Case.Insensitive);
    }

    // =========================================================================
    // SCENARIO: Invalid CNPJ format → 422
    // =========================================================================

    [Fact]
    public async Task RegisterCustodiante_InvalidCnpj_Returns422()
    {
        using var client = ClientPjA();
        var response = await client.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante Bad CNPJ {TestId}",
            cnpj = "00000000000000"  // all-zeros: rejected by Cnpj domain validator
        });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity,
            "Invalid CNPJ must return 422 UnprocessableEntity.");
    }

    // =========================================================================
    // SCENARIO: No auth → 401
    // =========================================================================

    [Fact]
    public async Task RegisterCustodiante_NoAuth_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = "No Auth Custodiante",
            cnpj = TestCnpj
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListCustodiantes_NoAuth_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/fundos/custodiantes");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // SCENARIO: Search parameter filters results
    // =========================================================================

    [Fact]
    public async Task ListCustodiantes_SearchByName_FiltersResults()
    {
        using var client = ClientPjA();

        var uniqueLabel = $"SearchCust{TestId}";
        await client.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"{uniqueLabel} Custodiante",
            cnpj = TestCnpj
        });

        var listResp = await client.GetAsync($"/api/fundos/custodiantes?search={uniqueLabel}");
        listResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await listResp.Content.ReadAsStringAsync();
        // Search parameter must filter results to matching rows
        body.ShouldContain(uniqueLabel);
    }

    // =========================================================================
    // SCENARIO: INATIVO status update via PUT
    // =========================================================================

    [Fact]
    public async Task UpdateCustodiante_SetStatusInativo_Returns200()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante ToInativo {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Update status to INATIVO via PUT (CustodianteStatus.INATIVO = 2)
        var updateResp = await client.PutAsJsonAsync($"/api/fundos/custodiantes/{id}", new
        {
            razaoSocial = $"Custodiante ToInativo {TestId}",
            codigoInterno = (string?)null,
            email = (string?)null,
            telefone = (string?)null,
            status = 2  // CustodianteStatus.INATIVO
        });

        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await updateResp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().ShouldBe("INATIVO");
    }
}
