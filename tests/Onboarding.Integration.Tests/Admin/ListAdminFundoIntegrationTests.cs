using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Infrastructure.Persistence;
using Onboarding.Integration.Tests.Fixtures;
using Shouldly;

namespace Onboarding.Integration.Tests.Admin;

/// <summary>
/// Integration tests for ListAdminFundoQueryHandler — requires PostgreSQL (real provider).
///
/// The handler in FundosAdminQueryHandlers.cs has branches that were uncovered:
///   1. Search path: FromSqlInterpolated ILIKE on nome + CNPJ digits
///   2. query.CompanyId.HasValue (true branch): WHERE Fundo.ClienteId = companyId
///
/// Branches covered:
///   - search by nome substring (ILike path, uppercase input → proves case-insensitivity)
///   - search by CNPJ digits (digitsOnly.Length > 0 branch)
///   - companyId filter (HasValue = true) → scopes result to one company
///   - empty result when search matches nothing
///   - companyId with no matching fundos → empty result
///   - companyId + search combined (both branches simultaneously)
///
/// Security: all tests go through BearerBackoffice + CrossCompanyAccess policy.
/// Prerequisite seed: ConsultoriaFundo + Custodiante must exist before Fundo can be registered.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ListAdminFundoIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private Guid _companyAId;
    private Guid _companyBId;

    private const string SubPjA = "lac-fundo-pja-sub-030";
    private const string SubPjB = "lac-fundo-pjb-sub-031";

    // Valid CNPJs generated at construction time — guarantees valid check digits + uniqueness.
    private readonly string _cnpjCompanyA;
    private readonly string _cnpjCompanyB;
    private readonly string _cnpjConsultoria;
    private readonly string _cnpjCustodiante;

    public ListAdminFundoIntegrationTests(PostgreSqlFixture fixture) : base(fixture)
    {
        _cnpjCompanyA = GenerateCnpj(fixture.NextCnpjSlot());
        _cnpjCompanyB = GenerateCnpj(fixture.NextCnpjSlot());
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

        // Re-read company IDs after seed
        using var readScope = CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _companyAId = readDb.Companies.IgnoreQueryFilters()
            .Where(c => c.KeycloakUserId == SubPjA).Select(c => c.Id).First();
        _companyBId = readDb.Companies.IgnoreQueryFilters()
            .Where(c => c.KeycloakUserId == SubPjB).Select(c => c.Id).First();
    }

    private static async Task SeedAsync(
        AppDbContext db,
        string cnpjCompanyA,
        string cnpjCompanyB,
        string cnpjConsultoria,
        string cnpjCustodiante)
    {
        // PJ-A
        var companyA = Company.Register(
            "Empresa Alpha LAC Fundo Ltda",
            cnpjCompanyA,
            "alpha.lac.fundo@test.com",
            "+5511000004001",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.4.1"));
        companyA.SetKeycloakUserId(SubPjA);

        // PJ-B
        var companyB = Company.Register(
            "Empresa Beta LAC Fundo S.A.",
            cnpjCompanyB,
            "beta.lac.fundo@test.com",
            "+5511000004002",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.4.2"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();

        // Prerequisite entities for Fundo creation (must belong to companyA)
        var consultoria = ConsultoriaFundo.Register("Consultoria LAC Fundo Alpha", cnpjConsultoria, companyA.Id);
        var custodiante = Custodiante.Register("Custodiante LAC Fundo Alpha", cnpjCustodiante, companyA.Id);

        await db.ConsultoriasFundo.AddAsync(consultoria);
        await db.Custodiantes.AddAsync(custodiante);
        await db.SaveChangesAsync();
    }

    // =========================================================================
    // HTTP client helpers
    // =========================================================================

    private HttpClient AdminClient() => CreateAdminJwt("lac-fundo-admin-backoffice");
    private HttpClient ClientPjA() => CreateClientJwt(SubPjA);

    // =========================================================================
    // Helpers to read prerequisite IDs needed for Fundo creation
    // =========================================================================

    private (Guid ConsultoriaId, Guid CustodianteId) GetPrerequisiteIds()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var consultoriaId = db.ConsultoriasFundo.IgnoreQueryFilters()
            .Where(c => c.RazaoSocial == "Consultoria LAC Fundo Alpha")
            .Select(c => c.Id).First();
        var custodianteId = db.Custodiantes.IgnoreQueryFilters()
            .Where(c => c.RazaoSocial == "Custodiante LAC Fundo Alpha")
            .Select(c => c.Id).First();
        return (consultoriaId, custodianteId);
    }

    // =========================================================================
    // Branch: Search by nome (ILike path — case-insensitive)
    // =========================================================================

    [Fact]
    public async Task ListAdminFundos_SearchByNome_ReturnsMatchingItems()
    {
        // Arrange — PJ-A creates a fundo with a unique recognisable name
        var (consultoriaId, custodianteId) = GetPrerequisiteIds();
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"ILikeSearchable Fundo LAC {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = consultoriaId,
            custodianteId = custodianteId,
            tipoFundo = 1  // TipoFundo.RendaFixa
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — admin searches by unique substring (UPPERCASE → proves ILike case-insensitivity)
        using var admin = AdminClient();
        var searchTerm = $"ILIKESEARCHABLE FUNDO LAC {TestId}".ToUpperInvariant();
        var resp = await admin.GetAsync(
            $"/api/admin/fundos?page=1&pageSize=50&search={Uri.EscapeDataString(searchTerm)}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminFundoListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        body.Items.ShouldNotBeNull();
        body.Items.ShouldContain(x =>
            x.Nome.ToLower().Contains($"ilikesearchable fundo lac {TestId}".ToLower()));
    }

    // =========================================================================
    // Branch: Search by CNPJ digits (digitsOnly.Length > 0)
    // =========================================================================

    [Fact]
    public async Task ListAdminFundos_SearchByCnpjDigits_ReturnsMatchingItems()
    {
        // Arrange — create a fundo so TestCnpj is registered
        var (consultoriaId, custodianteId) = GetPrerequisiteIds();
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"CNPJ Digits Fundo LAC {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = consultoriaId,
            custodianteId = custodianteId,
            tipoFundo = 1  // TipoFundo.RendaFixa
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — search with a 6-digit prefix of TestCnpj (numeric chars → digitsOnly.Length > 0)
        using var admin = AdminClient();
        var digitsSubstring = TestCnpj[..6];
        var resp = await admin.GetAsync(
            $"/api/admin/fundos?page=1&pageSize=50&search={digitsSubstring}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminFundoListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // Branch: CompanyId filter (HasValue = true)
    // =========================================================================

    [Fact]
    public async Task ListAdminFundos_WithCompanyIdFilter_ReturnsOnlyThatCompanyItems()
    {
        // Arrange — PJ-A creates a fundo
        var (consultoriaId, custodianteId) = GetPrerequisiteIds();
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"CompanyId Filter Fundo LAC {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = consultoriaId,
            custodianteId = custodianteId,
            tipoFundo = 1  // TipoFundo.RendaFixa
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — admin filters by PJ-A's companyId
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            $"/api/admin/fundos?page=1&pageSize=50&companyId={_companyAId}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminFundoListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.Items.ShouldNotBeNull();
        body.Items.ShouldAllBe(x => x.ClienteId == _companyAId);
        body.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // CompanyId with no fundos → empty result
    // =========================================================================

    [Fact]
    public async Task ListAdminFundos_CompanyIdWithNoFundos_ReturnsEmptyResult()
    {
        // PJ-B has no fundos seeded in this class
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            $"/api/admin/fundos?page=1&pageSize=50&companyId={_companyBId}");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminFundoListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBe(0);
    }

    // =========================================================================
    // Search with no match → empty result
    // =========================================================================

    [Fact]
    public async Task ListAdminFundos_SearchNoMatch_ReturnsEmptyResult()
    {
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            "/api/admin/fundos?page=1&pageSize=50&search=zzznomatchfundo999lacxyz");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminFundoListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBe(0);
        body.Items.ShouldNotBeNull();
        body.Items.ShouldBeEmpty();
    }

    // =========================================================================
    // CompanyId + Search combined (both branches active simultaneously)
    // =========================================================================

    [Fact]
    public async Task ListAdminFundos_CompanyIdAndSearch_ReturnsFilteredItems()
    {
        // Arrange — PJ-A creates a fundo with a unique name
        var (consultoriaId, custodianteId) = GetPrerequisiteIds();
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Combined Filter Fundo LAC {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = consultoriaId,
            custodianteId = custodianteId,
            tipoFundo = 1  // TipoFundo.RendaFixa
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — admin filters by companyId AND searches by nome (both branches active)
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            $"/api/admin/fundos?page=1&pageSize=50" +
            $"&companyId={_companyAId}" +
            $"&search={Uri.EscapeDataString($"Combined Filter Fundo LAC {TestId}")}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminFundoListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        body.Items.ShouldNotBeNull();
        body.Items.ShouldAllBe(x => x.ClienteId == _companyAId);
        body.Items.ShouldContain(x => x.Nome.Contains($"Combined Filter Fundo LAC {TestId}"));
    }

    // =========================================================================
    // Security: no token → 401
    // =========================================================================

    [Fact]
    public async Task ListAdminFundos_NoToken_Returns401()
    {
        using var anon = CreateUnauthenticatedClient();
        var resp = await anon.GetAsync("/api/admin/fundos?page=1&pageSize=10");
        resp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Local DTO types — self-contained deserialization
    // Status serialized as string (JsonStringEnumConverter) — typed as string, NOT int.
    // TipoFundo similarly serialized as string.
    // =========================================================================

    private sealed class AdminFundoListDto
    {
        public AdminFundoItemDto[]? Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private sealed class AdminFundoItemDto
    {
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public string EmpresaNome { get; set; } = string.Empty;
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
