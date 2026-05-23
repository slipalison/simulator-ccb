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
/// Integration tests for TipoAtivo global catalog endpoints (CAD-19..CAD-21).
///
/// TipoAtivo is a GLOBAL entity (D-5/TEN-03) — no company scope, no HasQueryFilter.
/// All authenticated users with funds:read/write see the same global catalog.
///
/// Scenarios covered:
/// - POST /api/fundos/tipos-ativo → 201 with Codigo + Descricao
/// - GET /api/fundos/tipos-ativo → 200 paginated (global — both companies see same rows)
/// - GET /api/fundos/tipos-ativo/{id} → 200
/// - PUT /api/fundos/tipos-ativo/{id} → 200 with updated data
/// - Duplicate codigo → 409 Conflict
/// - Missing required fields → 422 UnprocessableEntity
/// - No auth → 401; no permission → 401/403
///
/// NOT tested (by design):
/// - Cross-tenant 404: TipoAtivo is global, no tenant filter — any authenticated user can access.
/// - Company-scoped uniqueness: TipoAtivo.Codigo uniqueness is GLOBAL (no ClientId).
/// </summary>
[Trait("Category", "Integration")]
public sealed class TipoAtivoCrudIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    // Sub claims — seeded companies needed for auth pipeline to resolve company + permissions
    private const string SubPjA = "tipoativo-tests-pja-sub";
    private const string SubPjB = "tipoativo-tests-pjb-sub";
    private const string SubNoCompany = "tipoativo-tests-noperm-sub";

    public TipoAtivoCrudIntegrationTests(PostgreSqlFixture fixture) : base(fixture) { }

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
    }

    // =========================================================================
    // Seed helpers
    // =========================================================================

    private static async Task SeedCompaniesAsync(AppDbContext db)
    {
        // Use GenerateCnpj with large fixed counters to produce valid CNPJs that are unique
        // across test classes (counter range 900_003..900_004 reserved for TipoAtivoCrudIntegrationTests).
        var cnpjA = PostgreSqlIntegrationTestBase.GenerateCnpj(900_003);
        var cnpjB = PostgreSqlIntegrationTestBase.GenerateCnpj(900_004);

        var companyA = Company.Register(
            "TipoAtivo Test Empresa Alpha Ltda",
            cnpjA,
            "tipoativo.alpha@test.com",
            "+5511999990021",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.1.1"));
        companyA.SetKeycloakUserId(SubPjA);

        var companyB = Company.Register(
            "TipoAtivo Test Empresa Beta S.A.",
            cnpjB,
            "tipoativo.beta@test.com",
            "+5511999990022",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.1.2"));
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
    // POST — create TipoAtivo
    // =========================================================================

    /// <summary>POST /api/fundos/tipos-ativo — Returns 201 with Codigo and Descricao.</summary>
    [Fact]
    public async Task PostTipoAtivo_ValidPayload_Returns201WithCodigoAndDescricao()
    {
        using var client = ClientPjA();
        var codigo = $"TA-{TestId}"; // unique per test via TestId
        var payload = new
        {
            codigo,
            descricao = $"Ativo Renda Fixa {TestId}",
            categoria = 1, // TipoAtivoCategoria.RendaFixa = 1
            ordemExibicao = 10
        };

        var response = await client.PostAsJsonAsync("/api/fundos/tipos-ativo", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain(codigo);
        body.ShouldContain($"Ativo Renda Fixa {TestId}");
    }

    /// <summary>POST with all optional fields populated — Returns 201.</summary>
    [Fact]
    public async Task PostTipoAtivo_WithSubcategoria_Returns201()
    {
        using var client = ClientPjA();
        var codigo = $"TA-SUB-{TestId}";
        var payload = new
        {
            codigo,
            descricao = $"Ativo Com Subcategoria {TestId}",
            categoria = 2, // TipoAtivoCategoria.RendaVariavel = 2
            subcategoria = "Acoes",
            ordemExibicao = 5
        };

        var response = await client.PostAsJsonAsync("/api/fundos/tipos-ativo", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Acoes");
    }

    // =========================================================================
    // GET single
    // =========================================================================

    /// <summary>GET /api/fundos/tipos-ativo/{id} — Returns 200 for existing TipoAtivo.</summary>
    [Fact]
    public async Task GetTipoAtivoById_ExistingId_Returns200()
    {
        using var client = ClientPjA();

        var codigo = $"TA-GET-{TestId}";
        var createResponse = await client.PostAsJsonAsync("/api/fundos/tipos-ativo", new
        {
            codigo,
            descricao = $"Get By Id Test {TestId}",
            categoria = 1,
            ordemExibicao = 0
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TipoAtivoResponseDto>();
        created.ShouldNotBeNull();

        var getResponse = await client.GetAsync($"/api/fundos/tipos-ativo/{created!.Id}");

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadAsStringAsync();
        body.ShouldContain(codigo);
        body.ShouldContain($"Get By Id Test {TestId}");
    }

    /// <summary>GET /api/fundos/tipos-ativo/{id} — Returns 404 for non-existent id.</summary>
    [Fact]
    public async Task GetTipoAtivoById_NonExistentId_Returns404()
    {
        using var client = ClientPjA();
        var nonExistentId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/fundos/tipos-ativo/{nonExistentId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // GET list — global catalog visible to all companies
    // =========================================================================

    /// <summary>GET /api/fundos/tipos-ativo — Returns 200 paginated list.</summary>
    [Fact]
    public async Task GetTiposAtivo_PjA_Returns200PaginatedList()
    {
        using var client = ClientPjA();
        var codigo = $"TA-LIST-{TestId}";
        await client.PostAsJsonAsync("/api/fundos/tipos-ativo", new
        {
            codigo,
            descricao = $"List Test {TestId}",
            categoria = 1,
            ordemExibicao = 0
        });

        var listResponse = await client.GetAsync("/api/fundos/tipos-ativo?page=1&pageSize=20");

        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await listResponse.Content.ReadAsStringAsync();
        body.ShouldContain(codigo);
    }

    /// <summary>
    /// Global catalog — TipoAtivo created by PJ-A is visible to PJ-B (D-5/TEN-03: no tenant filter).
    /// This is by design: TipoAtivo is a shared CVM catalog, not company-scoped.
    /// </summary>
    [Fact]
    public async Task GetTiposAtivo_PjB_CanSeeTipoAtivoCreatedByPjA()
    {
        // PJ-A creates a TipoAtivo
        using var clientA = ClientPjA();
        var codigo = $"TA-GLOBAL-{TestId}";
        var createResponse = await clientA.PostAsJsonAsync("/api/fundos/tipos-ativo", new
        {
            codigo,
            descricao = $"Global Visibility Test {TestId}",
            categoria = 1,
            ordemExibicao = 99
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TipoAtivoResponseDto>();
        created.ShouldNotBeNull();

        // PJ-B can see the same TipoAtivo by id (global, no HasQueryFilter)
        using var clientB = ClientPjB();
        var getResponse = await clientB.GetAsync($"/api/fundos/tipos-ativo/{created!.Id}");

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
            "TipoAtivo is a global catalog (D-5/TEN-03) — all companies see the same entries.");
        var body = await getResponse.Content.ReadAsStringAsync();
        body.ShouldContain(codigo);
    }

    // =========================================================================
    // PUT — update TipoAtivo
    // =========================================================================

    /// <summary>PUT /api/fundos/tipos-ativo/{id} — Returns 200 with updated descricao.</summary>
    [Fact]
    public async Task PutTipoAtivo_ValidUpdate_Returns200WithNewDescricao()
    {
        using var client = ClientPjA();
        var codigo = $"TA-UPD-{TestId}";

        // Create
        var createResponse = await client.PostAsJsonAsync("/api/fundos/tipos-ativo", new
        {
            codigo,
            descricao = $"Before Update {TestId}",
            categoria = 1,
            ordemExibicao = 1
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TipoAtivoResponseDto>();
        created.ShouldNotBeNull();

        // Update descricao
        var updateResponse = await client.PutAsJsonAsync($"/api/fundos/tipos-ativo/{created!.Id}", new
        {
            descricao = $"After Update {TestId}",
            status = 1, // TipoAtivoStatus.ATIVO = 1
            ordemExibicao = 2
        });

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await updateResponse.Content.ReadAsStringAsync();
        body.ShouldContain($"After Update {TestId}");
    }

    /// <summary>PUT /api/fundos/tipos-ativo/{id} — Non-existent id returns 404.</summary>
    [Fact]
    public async Task PutTipoAtivo_NonExistentId_Returns404()
    {
        using var client = ClientPjA();
        var nonExistentId = Guid.NewGuid();

        var response = await client.PutAsJsonAsync($"/api/fundos/tipos-ativo/{nonExistentId}", new
        {
            descricao = "Should Not Exist",
            status = 1,
            ordemExibicao = 0
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Duplicate codigo → 409
    // =========================================================================

    /// <summary>POST with duplicate Codigo (global uniqueness) → 409 Conflict.</summary>
    [Fact]
    public async Task PostTipoAtivo_DuplicateCodigo_Returns409()
    {
        using var client = ClientPjA();
        var codigo = $"TA-DUP-{TestId}"; // unique per test, same for both requests

        // First creation — should succeed
        var first = await client.PostAsJsonAsync("/api/fundos/tipos-ativo", new
        {
            codigo,
            descricao = $"TipoAtivo First {TestId}",
            categoria = 1,
            ordemExibicao = 0
        });
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Second creation with same codigo — must conflict
        var second = await client.PostAsJsonAsync("/api/fundos/tipos-ativo", new
        {
            codigo,
            descricao = $"TipoAtivo Duplicate {TestId}",
            categoria = 2,
            ordemExibicao = 1
        });

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Duplicate Codigo for TipoAtivo must return 409 (global uniqueness constraint).");
    }

    /// <summary>
    /// Same Codigo submitted by DIFFERENT companies → 409 on second (global catalog uniqueness).
    /// TipoAtivo has no ClientId — codigo uniqueness is global, not per-company.
    /// </summary>
    [Fact]
    public async Task PostTipoAtivo_SameCodigoDifferentCompanies_SecondReturns409()
    {
        var codigo = $"TA-XCOMP-{TestId}";

        using var clientA = ClientPjA();
        var responseA = await clientA.PostAsJsonAsync("/api/fundos/tipos-ativo", new
        {
            codigo,
            descricao = $"TipoAtivo From CompanyA {TestId}",
            categoria = 1,
            ordemExibicao = 0
        });
        responseA.StatusCode.ShouldBe(HttpStatusCode.Created);

        // PJ-B also tries the same codigo — must conflict (global, no ClientId partition)
        using var clientB = ClientPjB();
        var responseB = await clientB.PostAsJsonAsync("/api/fundos/tipos-ativo", new
        {
            codigo,
            descricao = $"TipoAtivo From CompanyB {TestId}",
            categoria = 2,
            ordemExibicao = 1
        });

        responseB.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "TipoAtivo Codigo is globally unique (no ClientId) — second company must get 409.");
    }

    // =========================================================================
    // Validation tests
    // =========================================================================

    /// <summary>POST missing Codigo → 422 UnprocessableEntity.</summary>
    [Fact]
    public async Task PostTipoAtivo_MissingCodigo_Returns422()
    {
        using var client = ClientPjA();
        var payload = new
        {
            descricao = "Missing Codigo Test",
            categoria = 1,
            ordemExibicao = 0
            // codigo intentionally omitted
        };

        var response = await client.PostAsJsonAsync("/api/fundos/tipos-ativo", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>POST missing Descricao → 422 UnprocessableEntity.</summary>
    [Fact]
    public async Task PostTipoAtivo_MissingDescricao_Returns422()
    {
        using var client = ClientPjA();
        var codigo = $"TA-NODESC-{TestId}";
        var payload = new
        {
            codigo,
            categoria = 1,
            ordemExibicao = 0
            // descricao intentionally omitted
        };

        var response = await client.PostAsJsonAsync("/api/fundos/tipos-ativo", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // =========================================================================
    // Auth tests
    // =========================================================================

    /// <summary>No auth → 401 on GET list.</summary>
    [Fact]
    public async Task GetTiposAtivo_NoAuth_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/fundos/tipos-ativo");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>No auth → 401 on POST.</summary>
    [Fact]
    public async Task PostTipoAtivo_NoAuth_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/fundos/tipos-ativo", new
        {
            codigo = $"TA-NOAUTH-{TestId}",
            descricao = "No Auth Test",
            categoria = 1,
            ordemExibicao = 0
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>No-permission user (no company in DB) → 401 or 403 on GET list.</summary>
    [Fact]
    public async Task GetTiposAtivo_NoPermissions_Returns401Or403()
    {
        using var client = ClientNoPermissions();
        var response = await client.GetAsync("/api/fundos/tipos-ativo");

        var statusCode = (int)response.StatusCode;
        (statusCode == 401 || statusCode == 403).ShouldBeTrue(
            $"Expected 401 or 403 for no-permission user, got {statusCode}.");
    }

    /// <summary>No-permission user → 401 or 403 on POST.</summary>
    [Fact]
    public async Task PostTipoAtivo_NoPermissions_Returns401Or403()
    {
        using var client = ClientNoPermissions();
        var response = await client.PostAsJsonAsync("/api/fundos/tipos-ativo", new
        {
            codigo = $"TA-NOPERM-{TestId}",
            descricao = "No Permission Test",
            categoria = 1,
            ordemExibicao = 0
        });

        var statusCode = (int)response.StatusCode;
        (statusCode == 401 || statusCode == 403).ShouldBeTrue(
            $"Expected 401 or 403 for no-permission user, got {statusCode}.");
    }

    // =========================================================================
    // Response shape DTO — used for local deserialization only
    // =========================================================================

    // Enum fields declared as string because the API serializes enums as strings via JsonStringEnumConverter
    private sealed record TipoAtivoResponseDto(
        Guid Id,
        string Codigo,
        string Descricao,
        string Categoria,
        string? Subcategoria,
        string Status,
        int OrdemExibicao);
}
