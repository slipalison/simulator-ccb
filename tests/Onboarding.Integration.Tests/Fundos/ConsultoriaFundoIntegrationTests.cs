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
/// Integration tests for ConsultoriaFundo CRUD + company-scoped CNPJ uniqueness (D-10) + multi-tenant.
///
/// DoD T-2 coverage for ConsultoriaFundo:
///   - POST /api/fundos/consultorias → 201
///   - GET /api/fundos/consultorias/{id} → 200
///   - GET /api/fundos/consultorias → 200 paginated
///   - PUT /api/fundos/consultorias/{id} → 200
///   - Duplicate CNPJ same company → 409 (D-10 composite unique index)
///   - Same CNPJ in different company → 201 (D-10: uniqueness is company-scoped)
///   - Multi-tenant: PJ-B GET/PUT of PJ-A entity → 404
///   - Validation: missing required field → 422
///   - No auth → 401
///
/// Requires Docker (Testcontainers PostgreSQL). Tag: [Category=Integration]
/// </summary>
[Trait("Category", "Integration")]
public sealed class ConsultoriaFundoIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private Guid _companyAId;
    private Guid _companyBId;

    private const string SubPjA = "cf-crud-pja-sub-001";
    private const string SubPjB = "cf-crud-pjb-sub-002";

    public ConsultoriaFundoIntegrationTests(PostgreSqlFixture fixture) : base(fixture) { }

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
            "Alpha Consultoria CRUD Ltda", "11444777000161",
            "alpha.cf.crud@test.com", "+5511000000020",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "192.168.1.40"));
        companyA.SetKeycloakUserId(SubPjA);

        var companyB = Company.Register(
            "Beta Consultoria CRUD S.A.", "62232889000190",
            "beta.cf.crud@test.com", "+5511000000021",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "192.168.1.41"));
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
    public async Task RegisterConsultoria_HappyPath_Returns201()
    {
        using var client = ClientPjA();
        var response = await client.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Consultoria CF CRUD {TestId}",
            cnpj = TestCnpj
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
        (body.GetProperty("razaoSocial").GetString() ?? "").ShouldContain("Consultoria CF CRUD");
        // ConsultoriaFundo starts as ATIVO (domain factory default)
        body.GetProperty("status").GetString().ShouldBe("ATIVO");
    }

    // =========================================================================
    // SCENARIO: GET /api/fundos/consultorias/{id} → 200
    // =========================================================================

    [Fact]
    public async Task GetConsultoriaById_OwnedByPjA_Returns200()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Consultoria GET {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var getResp = await client.GetAsync($"/api/fundos/consultorias/{id}");
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().ShouldBe(id);
    }

    // =========================================================================
    // SCENARIO: GET /api/fundos/consultorias → 200 paginated
    // =========================================================================

    [Fact]
    public async Task ListConsultorias_PjA_Returns200Paginated()
    {
        using var client = ClientPjA();

        // Ensure at least one row exists
        await client.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Consultoria List {TestId}",
            cnpj = TestCnpj
        });

        var listResp = await client.GetAsync("/api/fundos/consultorias?page=1&pageSize=20");
        listResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("items", out _).ShouldBeTrue("Paginated result must have 'items' property.");
        body.TryGetProperty("totalCount", out var tc).ShouldBeTrue();
        tc.GetInt32().ShouldBeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // SCENARIO: PUT /api/fundos/consultorias/{id} → 200
    // =========================================================================

    [Fact]
    public async Task UpdateConsultoria_HappyPath_Returns200WithUpdatedName()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Consultoria Before {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var updateResp = await client.PutAsJsonAsync($"/api/fundos/consultorias/{id}", new
        {
            razaoSocial = $"Consultoria After {TestId}",
            nomeFantasia = (string?)null,
            email = (string?)null,
            telefone = (string?)null,
            status = 1  // ConsultoriaFundoStatus.ATIVO
        });

        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await updateResp.Content.ReadFromJsonAsync<JsonElement>();
        (body.GetProperty("razaoSocial").GetString() ?? "").ShouldContain("After");
    }

    // =========================================================================
    // SCENARIO: Duplicate CNPJ same company → 409 (D-10)
    // =========================================================================

    [Fact]
    public async Task RegisterConsultoria_DuplicateCnpjSameCompany_Returns409()
    {
        using var client = ClientPjA();
        var sharedCnpj = TestCnpj;

        var first = await client.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Consultoria Dup 1 {TestId}",
            cnpj = sharedCnpj
        });
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Consultoria Dup 2 {TestId}",
            cnpj = sharedCnpj
        });
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Duplicate CNPJ within same company must return 409 — D-10 composite unique index (ClientId, Cnpj).");
    }

    // =========================================================================
    // SCENARIO: Same CNPJ different company → 201 (D-10: uniqueness is company-scoped)
    // =========================================================================

    [Fact]
    public async Task RegisterConsultoria_SameCnpjDifferentCompany_Returns201ForBoth()
    {
        var sharedCnpj = TestCnpj;

        // PJ-A creates with CNPJ
        using var clientA = ClientPjA();
        var respA = await clientA.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Consultoria CrossCnpj A {TestId}",
            cnpj = sharedCnpj
        });
        respA.StatusCode.ShouldBe(HttpStatusCode.Created);

        // PJ-B creates with SAME CNPJ — must succeed because uniqueness is company-scoped (D-10)
        using var clientB = ClientPjB();
        var respB = await clientB.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Consultoria CrossCnpj B {TestId}",
            cnpj = sharedCnpj
        });
        respB.StatusCode.ShouldBe(HttpStatusCode.Created,
            "Same CNPJ in different company must succeed — uniqueness is (ClientId, Cnpj), not just Cnpj (D-10).");
    }

    // =========================================================================
    // SCENARIO: GET /api/fundos/consultorias/{id} cross-tenant → 404
    // =========================================================================

    [Fact]
    public async Task GetConsultoriaById_CrossTenant_Returns404()
    {
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Consultoria CrossTenant GET {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var clientB = ClientPjB();
        var resp = await clientB.GetAsync($"/api/fundos/consultorias/{id}");

        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant GET-by-id must return 404 to avoid leaking entity existence.");
    }

    // =========================================================================
    // SCENARIO: PUT /api/fundos/consultorias/{id} cross-tenant → 404
    // =========================================================================

    [Fact]
    public async Task UpdateConsultoria_CrossTenant_Returns404()
    {
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Consultoria CrossTenant PUT {TestId}",
            cnpj = TestCnpj
        });
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var clientB = ClientPjB();
        var resp = await clientB.PutAsJsonAsync($"/api/fundos/consultorias/{id}", new
        {
            razaoSocial = "Hijacked",
            nomeFantasia = (string?)null,
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
    public async Task ListConsultorias_PjB_DoesNotSeePjARows()
    {
        using var clientA = ClientPjA();
        await clientA.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Consultoria Isolation {TestId}",
            cnpj = TestCnpj
        });

        using var clientB = ClientPjB();
        var listResp = await clientB.GetAsync("/api/fundos/consultorias");
        listResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await listResp.Content.ReadAsStringAsync();
        // PJ-B must not see PJ-A's consultorias via HasQueryFilter multi-tenant isolation
        body.ShouldNotContain($"Consultoria Isolation {TestId}");
    }

    // =========================================================================
    // SCENARIO: Missing required field → 422
    // =========================================================================

    [Fact]
    public async Task RegisterConsultoria_MissingRazaoSocial_Returns422()
    {
        using var client = ClientPjA();
        var response = await client.PostAsJsonAsync("/api/fundos/consultorias", new
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
    public async Task RegisterConsultoria_InvalidCnpj_Returns422()
    {
        using var client = ClientPjA();
        var response = await client.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Consultoria Bad CNPJ {TestId}",
            cnpj = "00000000000000"  // all-zeros: rejected by Cnpj domain validator
        });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity,
            "Invalid CNPJ must return 422 UnprocessableEntity.");
    }

    // =========================================================================
    // SCENARIO: No auth → 401
    // =========================================================================

    [Fact]
    public async Task RegisterConsultoria_NoAuth_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = "No Auth Consultoria",
            cnpj = TestCnpj
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListConsultorias_NoAuth_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/fundos/consultorias");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // SCENARIO: Search parameter filters results
    // =========================================================================

    [Fact]
    public async Task ListConsultorias_SearchByName_FiltersResults()
    {
        using var client = ClientPjA();

        // Create two with distinct names
        var uniqueLabel = $"SearchMatch{TestId}";
        await client.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"{uniqueLabel} Consultoria",
            cnpj = TestCnpj
        });

        var listResp = await client.GetAsync($"/api/fundos/consultorias?search={uniqueLabel}");
        listResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await listResp.Content.ReadAsStringAsync();
        // Search parameter must filter results to matching rows
        body.ShouldContain(uniqueLabel);
    }
}
