using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Infrastructure.Persistence;
using Onboarding.Integration.Tests.Fixtures;
using Shouldly;

namespace Onboarding.Integration.Tests.Fundos;

/// <summary>
/// Integration tests for Cedente PF and PJ CRUD endpoints (CAD-14..CAD-17).
///
/// Scenarios covered:
/// - CedentePf POST → 201 with CPF returned in documento field
/// - CedentePj POST → 201 with CNPJ returned in documento field
/// - GET single Cedente by id → 200
/// - GET list → 200 paginated, contains own rows
/// - PUT update Cedente → 200
/// - D-10: same CPF in DIFFERENT company → 201 (no global uniqueness on CPF)
/// - D-10: same CNPJ in SAME company → 409 Conflict
/// - 404 cross-tenant: PJ-B request Cedente owned by PJ-A → 404
/// - Validation: missing required field → 422 UnprocessableEntity
/// - Auth: no auth → 401; no permission → 401/403
/// </summary>
[Trait("Category", "Integration")]
public sealed class CedenteCrudIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    // Seeded company IDs
    private Guid _companyAId;
    private Guid _companyBId;

    // Sub claims — mapped to Companies via SetKeycloakUserId
    private const string SubPjA = "cedente-tests-pja-sub";
    private const string SubPjB = "cedente-tests-pjb-sub";
    private const string SubNoCompany = "cedente-tests-noperm-sub";

    public CedenteCrudIntegrationTests(PostgreSqlFixture fixture) : base(fixture) { }

    // =========================================================================
    // IAsyncLifetime — per-class seed (guarded against re-seeding per test)
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
        using var readScope = CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _companyAId = readDb.Companies.IgnoreQueryFilters()
            .Where(c => c.KeycloakUserId == SubPjA)
            .Select(c => c.Id)
            .First();
        _companyBId = readDb.Companies.IgnoreQueryFilters()
            .Where(c => c.KeycloakUserId == SubPjB)
            .Select(c => c.Id)
            .First();
    }

    // =========================================================================
    // Seed helpers
    // =========================================================================

    private static async Task SeedCompaniesAsync(AppDbContext db)
    {
        // Use GenerateCnpj with large fixed counters to produce valid CNPJs that are unique
        // across test classes (counter range 900_001..900_002 reserved for CedenteCrudIntegrationTests).
        var cnpjA = PostgreSqlIntegrationTestBase.GenerateCnpj(900_001);
        var cnpjB = PostgreSqlIntegrationTestBase.GenerateCnpj(900_002);

        var companyA = Company.Register(
            "Cedente Test Empresa Alpha Ltda",
            cnpjA,
            "cedente.alpha@test.com",
            "+5511999990011",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.0.1"));
        companyA.SetKeycloakUserId(SubPjA);

        var companyB = Company.Register(
            "Cedente Test Empresa Beta S.A.",
            cnpjB,
            "cedente.beta@test.com",
            "+5511999990012",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.0.2"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();
    }

    // =========================================================================
    // HTTP client helpers
    // =========================================================================

    private HttpClient ClientPjA() => CreateClientJwt(SubPjA);
    private HttpClient ClientPjB() => CreateClientJwt(SubPjB);
    private HttpClient ClientNoPermissions() => CreateClientJwt(SubNoCompany);

    // =========================================================================
    // Cedente PF tests
    // =========================================================================

    /// <summary>POST /api/fundos/cedentes/pf — Returns 201 with CPF in documento field.</summary>
    [Fact]
    public async Task PostCedentePf_ValidCpf_Returns201WithCpfInDocumento()
    {
        using var client = ClientPjA();
        var payload = new
        {
            cpf = TestCpf,
            nome = $"Pessoa Fisica Alpha {TestId}"
        };

        var response = await client.PostAsJsonAsync("/api/fundos/cedentes/pf", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain(TestCpf);
        body.ShouldContain($"Pessoa Fisica Alpha {TestId}");
    }

    /// <summary>GET /api/fundos/cedentes/{id} — Returns 200 for own PF cedente.</summary>
    [Fact]
    public async Task GetCedentePfById_OwnCedente_Returns200()
    {
        using var client = ClientPjA();

        // Create
        var createResponse = await client.PostAsJsonAsync("/api/fundos/cedentes/pf", new
        {
            cpf = TestCpf,
            nome = $"PF Get Test {TestId}"
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CedenteResponseDto>();
        created.ShouldNotBeNull();

        // Get by id
        var getResponse = await client.GetAsync($"/api/fundos/cedentes/{created!.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadAsStringAsync();
        body.ShouldContain($"PF Get Test {TestId}");
    }

    /// <summary>
    /// D-10: CedentePf — SAME CPF in a DIFFERENT company must succeed (201).
    /// Company-scoped uniqueness constraint is (ClientId, Cpf), not global Cpf.
    /// </summary>
    [Fact]
    public async Task PostCedentePf_SameCpfDifferentCompany_BothReturn201()
    {
        // PJ-A creates a PF cedente with a specific CPF
        using var clientA = ClientPjA();
        var sharedCpf = TestCpf; // same CPF used by both companies
        var responseA = await clientA.PostAsJsonAsync("/api/fundos/cedentes/pf", new
        {
            cpf = sharedCpf,
            nome = $"PF Company A {TestId}"
        });
        responseA.StatusCode.ShouldBe(HttpStatusCode.Created,
            "PJ-A must be able to create Cedente PF with any valid CPF (D-10).");

        // PJ-B creates a PF cedente with the SAME CPF — must succeed (D-10)
        using var clientB = ClientPjB();
        var responseB = await clientB.PostAsJsonAsync("/api/fundos/cedentes/pf", new
        {
            cpf = sharedCpf,
            nome = $"PF Company B {TestId}"
        });
        responseB.StatusCode.ShouldBe(HttpStatusCode.Created,
            "D-10: different company may have same CPF — uniqueness is (ClientId, Cpf), not global.");
    }

    /// <summary>Same CPF within the SAME company → 409 Conflict.</summary>
    [Fact]
    public async Task PostCedentePf_SameCpfSameCompany_Returns409()
    {
        using var client = ClientPjA();
        var cpf = TestCpf;

        // First creation — should succeed
        var first = await client.PostAsJsonAsync("/api/fundos/cedentes/pf", new
        {
            cpf,
            nome = $"PF First {TestId}"
        });
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Second creation with same CPF in same company — must conflict (D-10)
        var second = await client.PostAsJsonAsync("/api/fundos/cedentes/pf", new
        {
            cpf,
            nome = $"PF Duplicate {TestId}"
        });
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Same CPF within same company must return 409 (D-10 composite unique index).");
    }

    // =========================================================================
    // Cedente PJ tests
    // =========================================================================

    /// <summary>POST /api/fundos/cedentes/pj — Returns 201 with CNPJ in documento field.</summary>
    [Fact]
    public async Task PostCedentePj_ValidCnpj_Returns201WithCnpjInDocumento()
    {
        using var client = ClientPjA();
        var payload = new
        {
            cnpj = TestCnpj,
            razaoSocial = $"Empresa Cedente PJ {TestId}"
        };

        var response = await client.PostAsJsonAsync("/api/fundos/cedentes/pj", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain(TestCnpj);
        body.ShouldContain($"Empresa Cedente PJ {TestId}");
    }

    /// <summary>GET /api/fundos/cedentes/{id} — Returns 200 for own PJ cedente.</summary>
    [Fact]
    public async Task GetCedentePjById_OwnCedente_Returns200()
    {
        using var client = ClientPjA();

        var createResponse = await client.PostAsJsonAsync("/api/fundos/cedentes/pj", new
        {
            cnpj = TestCnpj,
            razaoSocial = $"PJ Get Test {TestId}"
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CedenteResponseDto>();
        created.ShouldNotBeNull();

        var getResponse = await client.GetAsync($"/api/fundos/cedentes/{created!.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadAsStringAsync();
        body.ShouldContain($"PJ Get Test {TestId}");
    }

    /// <summary>Same CNPJ within SAME company → 409 Conflict (D-10).</summary>
    [Fact]
    public async Task PostCedentePj_SameCnpjSameCompany_Returns409()
    {
        using var client = ClientPjA();
        var cnpj = TestCnpj;

        var first = await client.PostAsJsonAsync("/api/fundos/cedentes/pj", new
        {
            cnpj,
            razaoSocial = $"PJ First {TestId}"
        });
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/fundos/cedentes/pj", new
        {
            cnpj,
            razaoSocial = $"PJ Duplicate {TestId}"
        });
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Same CNPJ within same company must return 409 (D-10 composite unique index).");
    }

    /// <summary>
    /// D-10: CedentePj — SAME CNPJ in a DIFFERENT company must succeed (201).
    /// </summary>
    [Fact]
    public async Task PostCedentePj_SameCnpjDifferentCompany_BothReturn201()
    {
        var sharedCnpj = TestCnpj;

        using var clientA = ClientPjA();
        var responseA = await clientA.PostAsJsonAsync("/api/fundos/cedentes/pj", new
        {
            cnpj = sharedCnpj,
            razaoSocial = $"PJ Company A {TestId}"
        });
        responseA.StatusCode.ShouldBe(HttpStatusCode.Created,
            "PJ-A must be able to create CedentePj with valid CNPJ (D-10).");

        using var clientB = ClientPjB();
        var responseB = await clientB.PostAsJsonAsync("/api/fundos/cedentes/pj", new
        {
            cnpj = sharedCnpj,
            razaoSocial = $"PJ Company B {TestId}"
        });
        responseB.StatusCode.ShouldBe(HttpStatusCode.Created,
            "D-10: different company may have same CNPJ — uniqueness is (ClientId, Cnpj), not global.");
    }

    // =========================================================================
    // List tests
    // =========================================================================

    /// <summary>GET /api/fundos/cedentes — Returns 200 paginated list with own rows.</summary>
    [Fact]
    public async Task GetCedentes_PjA_ReturnsPaginatedListWithOwnRows()
    {
        using var client = ClientPjA();

        // Create one PF and one PJ cedente
        await client.PostAsJsonAsync("/api/fundos/cedentes/pf", new
        {
            cpf = TestCpf,
            nome = $"List Test PF {TestId}"
        });
        await client.PostAsJsonAsync("/api/fundos/cedentes/pj", new
        {
            cnpj = TestCnpj,
            razaoSocial = $"List Test PJ {TestId}"
        });

        var listResponse = await client.GetAsync("/api/fundos/cedentes?page=1&pageSize=20");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await listResponse.Content.ReadAsStringAsync();
        body.ShouldContain($"List Test PF {TestId}");
        body.ShouldContain($"List Test PJ {TestId}");
    }

    // =========================================================================
    // Update tests
    // =========================================================================

    /// <summary>PUT /api/fundos/cedentes/{id} — Returns 200 with updated data.</summary>
    [Fact]
    public async Task PutCedente_ValidUpdate_Returns200WithNewData()
    {
        using var client = ClientPjA();

        // Create a cedente PF first
        var createResponse = await client.PostAsJsonAsync("/api/fundos/cedentes/pf", new
        {
            cpf = TestCpf,
            nome = $"Before Update {TestId}"
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CedenteResponseDto>();
        created.ShouldNotBeNull();

        // Update nome
        var updateResponse = await client.PutAsJsonAsync($"/api/fundos/cedentes/{created!.Id}", new
        {
            nome = $"After Update {TestId}",
            status = 1 // CedenteStatus.ATIVO = 1
        });

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await updateResponse.Content.ReadAsStringAsync();
        body.ShouldContain($"After Update {TestId}");
    }

    // =========================================================================
    // Multi-tenant cross-probe (D-34)
    // =========================================================================

    /// <summary>
    /// Security: PJ-B request GET /api/fundos/cedentes/{id} for a PJ-A cedente → 404.
    /// Must not reveal existence to cross-tenant callers.
    /// </summary>
    [Fact]
    public async Task GetCedenteById_CrossTenant_Returns404()
    {
        // PJ-A creates a cedente
        using var clientA = ClientPjA();
        var createResponse = await clientA.PostAsJsonAsync("/api/fundos/cedentes/pf", new
        {
            cpf = TestCpf,
            nome = $"PjA Cedente Isolation {TestId}"
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CedenteResponseDto>();
        created.ShouldNotBeNull();

        // PJ-B attempts to access PJ-A's cedente — must return 404 (not 403, not data)
        using var clientB = ClientPjB();
        var response = await clientB.GetAsync($"/api/fundos/cedentes/{created!.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant GET must return 404 — do not reveal entity existence (D-34, D-5).");
    }

    /// <summary>
    /// Security: PJ-B GET /api/fundos/cedentes list must not see PJ-A rows.
    /// </summary>
    [Fact]
    public async Task GetCedentes_PjB_DoesNotSeePjARows()
    {
        // PJ-A creates a cedente
        using var clientA = ClientPjA();
        await clientA.PostAsJsonAsync("/api/fundos/cedentes/pf", new
        {
            cpf = TestCpf,
            nome = $"PjA Isolation List {TestId}"
        });

        // PJ-B lists cedentes — must not see PJ-A's row
        using var clientB = ClientPjB();
        var listResponse = await clientB.GetAsync("/api/fundos/cedentes");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await listResponse.Content.ReadAsStringAsync();
        // HasQueryFilter must prevent PJ-B from seeing PJ-A cedentes (D-5 multi-tenant)
        body.ShouldNotContain($"PjA Isolation List {TestId}");
    }

    // =========================================================================
    // Validation tests
    // =========================================================================

    /// <summary>POST /api/fundos/cedentes/pf — Missing CPF → 422 with ProblemDetails errors.</summary>
    [Fact]
    public async Task PostCedentePf_MissingCpf_Returns422()
    {
        using var client = ClientPjA();
        var payload = new { nome = "Missing CPF Test" }; // cpf intentionally omitted

        var response = await client.PostAsJsonAsync("/api/fundos/cedentes/pf", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>POST /api/fundos/cedentes/pj — Missing CNPJ → 422 with ProblemDetails errors.</summary>
    [Fact]
    public async Task PostCedentePj_MissingCnpj_Returns422()
    {
        using var client = ClientPjA();
        var payload = new { razaoSocial = "Missing CNPJ Test" }; // cnpj intentionally omitted

        var response = await client.PostAsJsonAsync("/api/fundos/cedentes/pj", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // =========================================================================
    // Auth tests
    // =========================================================================

    /// <summary>No auth header → 401 on Cedente list endpoint.</summary>
    [Fact]
    public async Task GetCedentes_NoAuth_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/fundos/cedentes");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>No auth header → 401 on Cedente PF POST endpoint.</summary>
    [Fact]
    public async Task PostCedentePf_NoAuth_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/fundos/cedentes/pf", new
        {
            cpf = TestCpf,
            nome = "No Auth Test"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>No auth header → 401 on Cedente PJ POST endpoint.</summary>
    [Fact]
    public async Task PostCedentePj_NoAuth_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/fundos/cedentes/pj", new
        {
            cnpj = TestCnpj,
            razaoSocial = "No Auth PJ Test"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>No-permission user (no company in DB) → 401 or 403 on Cedente list.</summary>
    [Fact]
    public async Task GetCedentes_NoPermissions_Returns401Or403()
    {
        using var client = ClientNoPermissions();
        var response = await client.GetAsync("/api/fundos/cedentes");

        var statusCode = (int)response.StatusCode;
        (statusCode == 401 || statusCode == 403).ShouldBeTrue(
            $"Expected 401 or 403 for no-permission user, got {statusCode}.");
    }

    // =========================================================================
    // Response shape DTO — used for local deserialization only
    // =========================================================================

    // Enum fields declared as string because the API serializes enums as strings via JsonStringEnumConverter
    private sealed record CedenteResponseDto(
        Guid Id,
        string Documento,
        string Nome,
        string? Email,
        string? Telefone,
        string? Endereco,
        string CedenteTipo,
        string Status,
        DateTimeOffset CreatedAt);
}
