using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Infrastructure.Persistence;
using Onboarding.Integration.Tests.Fixtures;
using Shouldly;

namespace Onboarding.Integration.Tests.Fundos;

/// <summary>
/// Integration tests for FundoRepository.GetPagedByCompanyAsync — requires PostgreSQL (real provider).
///
/// GetPagedByCompanyAsync has two branches that were uncovered:
///   1. Search path: FromSqlInterpolated ILIKE on nome + CNPJ digits — not translatable by InMemory.
///   2. No-search path: IgnoreQueryFilters + AsNoTracking (baseline — exercises paging).
///
/// These are covered here via GET /api/fundos which calls ListFundoQueryHandler
/// which delegates to FundoRepository.GetPagedByCompanyAsync.
///
/// Branches covered:
///   - search by nome (ILIKE case-insensitive, uppercase input proves case-insensitivity)
///   - search by CNPJ digits (digitsOnly.Length > 0 branch)
///   - search with no match → empty result
///   - no-search path (baseline listing with paging)
///   - multi-tenant: PJ-B cannot see PJ-A's fundos (HasQueryFilter)
///
/// Security: BearerClient + funds:read policy. Client endpoint is company-scoped.
/// Prerequisite seed: ConsultoriaFundo + Custodiante must exist before Fundo can be registered.
/// </summary>
[Trait("Category", "Integration")]
public sealed class FundoRepositorySearchIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private const string SubPjA = "fundo-srch-pja-sub-040";
    private const string SubPjB = "fundo-srch-pjb-sub-041";

    // Valid CNPJs generated at construction time — guarantees valid check digits + uniqueness.
    private readonly string _cnpjCompanyA;
    private readonly string _cnpjCompanyB;
    private readonly string _cnpjConsultoria;
    private readonly string _cnpjCustodiante;

    public FundoRepositorySearchIntegrationTests(PostgreSqlFixture fixture) : base(fixture)
    {
        _cnpjCompanyA    = GenerateCnpj(fixture.NextCnpjSlot());
        _cnpjCompanyB    = GenerateCnpj(fixture.NextCnpjSlot());
        _cnpjConsultoria = GenerateCnpj(fixture.NextCnpjSlot());
        _cnpjCustodiante = GenerateCnpj(fixture.NextCnpjSlot());
    }

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
            await SeedAsync(db, _cnpjCompanyA, _cnpjCompanyB, _cnpjConsultoria, _cnpjCustodiante);
        });
    }

    private static async Task SeedAsync(
        AppDbContext db,
        string cnpjCompanyA,
        string cnpjCompanyB,
        string cnpjConsultoria,
        string cnpjCustodiante)
    {
        // PJ-A — will register fundos in test methods
        var companyA = Company.Register(
            "Alpha Fundo Search Ltda",
            cnpjCompanyA,
            "alpha.fundo.srch@test.com",
            "+5511000005001",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.5.1"));
        companyA.SetKeycloakUserId(SubPjA);

        // PJ-B — will not register fundos; used for tenant isolation test
        var companyB = Company.Register(
            "Beta Fundo Search S.A.",
            cnpjCompanyB,
            "beta.fundo.srch@test.com",
            "+5511000005002",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.5.2"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();

        // Prerequisite entities for Fundo creation under PJ-A
        var consultoria = ConsultoriaFundo.Register("Consultoria Fundo Search Alpha", cnpjConsultoria, companyA.Id);
        var custodiante = Custodiante.Register("Custodiante Fundo Search Alpha", cnpjCustodiante, companyA.Id);

        await db.ConsultoriasFundo.AddAsync(consultoria);
        await db.Custodiantes.AddAsync(custodiante);
        await db.SaveChangesAsync();
    }

    // =========================================================================
    // HTTP client helpers
    // =========================================================================

    private HttpClient ClientPjA() => CreateClientJwt(SubPjA);
    private HttpClient ClientPjB() => CreateClientJwt(SubPjB);

    // =========================================================================
    // Helpers to read prerequisite IDs needed for Fundo creation
    // =========================================================================

    private (Guid ConsultoriaId, Guid CustodianteId) GetPrerequisiteIds()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var consultoriaId = db.ConsultoriasFundo.IgnoreQueryFilters()
            .Where(c => c.RazaoSocial == "Consultoria Fundo Search Alpha")
            .Select(c => c.Id).First();
        var custodianteId = db.Custodiantes.IgnoreQueryFilters()
            .Where(c => c.RazaoSocial == "Custodiante Fundo Search Alpha")
            .Select(c => c.Id).First();
        return (consultoriaId, custodianteId);
    }

    // =========================================================================
    // Search branch: ILIKE on nome (case-insensitive)
    // =========================================================================

    [Fact]
    public async Task GetPagedByCompany_SearchByNome_ReturnsMatchingFundos()
    {
        // Arrange — PJ-A creates a fundo with a unique recognisable name
        var (consultoriaId, custodianteId) = GetPrerequisiteIds();
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"ILikeSearch Fundo SrchTest {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = consultoriaId,
            custodianteId = custodianteId,
            tipoFundo = 1  // TipoFundo.RendaFixa
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — search by uppercase substring (exercises ILIKE case-insensitivity)
        var searchTerm = $"ILIKESEARCH FUNDO SRCHTEST {TestId}".ToUpperInvariant();
        var resp = await clientA.GetAsync(
            $"/api/fundos?page=1&pageSize=50&search={Uri.EscapeDataString(searchTerm)}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<FundoListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        body.Items.ShouldNotBeNull();
        body.Items.ShouldContain(x =>
            x.Nome.ToLower().Contains($"ilikesearch fundo srchtest {TestId}".ToLower()));
    }

    // =========================================================================
    // Search branch: ILIKE on CNPJ digits (digitsOnly.Length > 0)
    // =========================================================================

    [Fact]
    public async Task GetPagedByCompany_SearchByCnpjDigits_ReturnsMatchingFundos()
    {
        // Arrange — PJ-A creates a fundo with known CNPJ
        var (consultoriaId, custodianteId) = GetPrerequisiteIds();
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"CNPJ Digits Fundo Srch {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = consultoriaId,
            custodianteId = custodianteId,
            tipoFundo = 1  // TipoFundo.RendaFixa
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — search using a 6-digit prefix of TestCnpj (digits → digitsOnly.Length > 0)
        var digitsSubstring = TestCnpj[..6];
        var resp = await clientA.GetAsync(
            $"/api/fundos?page=1&pageSize=50&search={digitsSubstring}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<FundoListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // Search branch: no match → empty result
    // =========================================================================

    [Fact]
    public async Task GetPagedByCompany_SearchNoMatch_ReturnsEmptyResult()
    {
        using var clientA = ClientPjA();
        var resp = await clientA.GetAsync(
            "/api/fundos?page=1&pageSize=50&search=zzznomatchfundosrch999xyz");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<FundoListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBe(0);
        body.Items.ShouldNotBeNull();
        body.Items.ShouldBeEmpty();
    }

    // =========================================================================
    // No-search path: baseline listing (IgnoreQueryFilters + paging path)
    // =========================================================================

    [Fact]
    public async Task GetPagedByCompany_NoSearch_Returns200WithPaging()
    {
        // Arrange — create at least one fundo for paging to be meaningful
        var (consultoriaId, custodianteId) = GetPrerequisiteIds();
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"NoSearch Fundo Srch {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = consultoriaId,
            custodianteId = custodianteId,
            tipoFundo = 1  // TipoFundo.RendaFixa
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — no search filter
        var resp = await clientA.GetAsync("/api/fundos?page=1&pageSize=50");

        // Assert — 200 with at least one item
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<FundoListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        body.Items.ShouldNotBeNull();
        // All returned items carry a valid Status string
        body.Items.ShouldAllBe(f => !string.IsNullOrEmpty(f.Status));
    }

    // =========================================================================
    // Multi-tenant: PJ-B cannot see PJ-A's fundos (HasQueryFilter isolation)
    // =========================================================================

    [Fact]
    public async Task GetPagedByCompany_PjBCannotSeePjAFundos()
    {
        // Arrange — create a fundo for PJ-A
        var (consultoriaId, custodianteId) = GetPrerequisiteIds();
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Tenant Isolation Fundo Srch {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = consultoriaId,
            custodianteId = custodianteId,
            tipoFundo = 1  // TipoFundo.RendaFixa
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var fundoId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Act — PJ-B lists its own fundos (should not contain PJ-A's fundo)
        using var clientB = ClientPjB();
        var resp = await clientB.GetAsync("/api/fundos?page=1&pageSize=50");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<FundoListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.Items.ShouldNotBeNull();
        body.Items.ShouldNotContain(f => f.Id == fundoId);
    }

    // =========================================================================
    // Security: no token → 401
    // =========================================================================

    [Fact]
    public async Task GetPagedByCompany_NoToken_Returns401()
    {
        using var anon = CreateUnauthenticatedClient();
        var resp = await anon.GetAsync("/api/fundos?page=1&pageSize=10");
        resp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Local DTO types — self-contained deserialization
    // Status/TipoFundo are serialized as strings (JsonStringEnumConverter) — typed as string, NOT int.
    // =========================================================================

    private sealed class FundoListDto
    {
        public FundoItemDto[]? Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private sealed class FundoItemDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cnpj { get; set; } = string.Empty;
        public Guid ConsultoriaFundoId { get; set; }
        public Guid CustodianteId { get; set; }
        public string TipoFundo { get; set; } = string.Empty;  // serialized as string, NOT int
        public string? ClasseAnbima { get; set; }
        public string? Segmento { get; set; }
        public string Status { get; set; } = string.Empty;     // serialized as string, NOT int
    }
}
