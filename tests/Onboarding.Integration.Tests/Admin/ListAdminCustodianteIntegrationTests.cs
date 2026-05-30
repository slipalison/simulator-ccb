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
/// Integration tests for ListAdminCustodianteQueryHandler — requires PostgreSQL (real provider).
///
/// The handler in FundosAdminQueryHandlers.cs has two conditional branches that were uncovered:
///   1. query.CompanyId.HasValue (true branch): WHERE Custodiante.ClienteId = companyId
///   2. Search path: FromSqlInterpolated ILIKE on razao_social + CNPJ digits
///
/// Branches covered:
///   - companyId filter (true branch of HasValue) → scopes result to one company
///   - search by razaoSocial substring (ILike path, uppercase input → proves case-insensitivity)
///   - search by CNPJ digits (digitsOnly.Length > 0 branch)
///   - empty result when search matches nothing
///   - companyId with no matching custodiantes → empty result
///
/// Security: all tests go through BearerBackoffice + CrossCompanyAccess policy.
/// Multi-tenant note: these are admin-only cross-company reads (IgnoreQueryFilters applied).
/// </summary>
[Trait("Category", "Integration")]
public sealed class ListAdminCustodianteIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private Guid _companyAId;
    private Guid _companyBId;

    private const string SubPjA = "lac-cust-pja-sub-020";
    private const string SubPjB = "lac-cust-pjb-sub-021";

    // Valid CNPJs generated at construction time — guarantees valid check digits + uniqueness.
    private readonly string _cnpjCompanyA;
    private readonly string _cnpjCompanyB;

    public ListAdminCustodianteIntegrationTests(PostgreSqlFixture fixture) : base(fixture)
    {
        _cnpjCompanyA = GenerateCnpj(fixture.NextCnpjSlot());
        _cnpjCompanyB = GenerateCnpj(fixture.NextCnpjSlot());
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
            await SeedAsync(db, _cnpjCompanyA, _cnpjCompanyB);
        });

        // Re-read company IDs after seed
        using var readScope = CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _companyAId = readDb.Companies.IgnoreQueryFilters()
            .Where(c => c.KeycloakUserId == SubPjA).Select(c => c.Id).First();
        _companyBId = readDb.Companies.IgnoreQueryFilters()
            .Where(c => c.KeycloakUserId == SubPjB).Select(c => c.Id).First();
    }

    private static async Task SeedAsync(AppDbContext db, string cnpjCompanyA, string cnpjCompanyB)
    {
        // PJ-A
        var companyA = Company.Register(
            "Empresa Alpha LAC Cust Ltda",
            cnpjCompanyA,
            "alpha.lac.cust@test.com",
            "+5511000003001",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.3.1"));
        companyA.SetKeycloakUserId(SubPjA);

        // PJ-B
        var companyB = Company.Register(
            "Empresa Beta LAC Cust S.A.",
            cnpjCompanyB,
            "beta.lac.cust@test.com",
            "+5511000003002",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.3.2"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();
    }

    // =========================================================================
    // HTTP client helpers
    // =========================================================================

    private HttpClient AdminClient() => CreateAdminJwt("lac-cust-admin-backoffice");
    private HttpClient ClientPjA() => CreateClientJwt(SubPjA);

    // =========================================================================
    // Branch 1: CompanyId filter (query.CompanyId.HasValue == true)
    // =========================================================================

    [Fact]
    public async Task ListAdminCustodiantes_WithCompanyIdFilter_ReturnsOnlyThatCompanyItems()
    {
        // Arrange — PJ-A creates a custodiante in its own scope
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Custodiante LAC Alpha {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — admin filters by PJ-A's companyId (CompanyId.HasValue = true branch)
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            $"/api/admin/fundos/custodiantes?page=1&pageSize=50&companyId={_companyAId}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminCustodianteListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.Items.ShouldNotBeNull();
        // All returned items must belong to company A
        body.Items.ShouldAllBe(x => x.ClienteId == _companyAId);
        body.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // Branch 1 + empty: CompanyId with no custodiantes in that company
    // =========================================================================

    [Fact]
    public async Task ListAdminCustodiantes_CompanyIdWithNoCustodiantes_ReturnsEmptyResult()
    {
        // Arrange — PJ-B has no custodiantes (this class seeds no custodiantes for PJ-B)
        // Act — filter by PJ-B companyId
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            $"/api/admin/fundos/custodiantes?page=1&pageSize=50&companyId={_companyBId}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminCustodianteListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBe(0);
    }

    // =========================================================================
    // Branch 2: Search by RazaoSocial (ILike path — case-insensitive)
    // =========================================================================

    [Fact]
    public async Task ListAdminCustodiantes_SearchByRazaoSocial_ReturnsMatchingItems()
    {
        // Arrange — PJ-A creates a custodiante with a unique recognisable name
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"ILikeSearchable Cust LAC {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — admin searches by the unique substring (UPPERCASE to exercise ILike case-insensitivity)
        using var admin = AdminClient();
        var searchTerm = $"ILIKESEARCHABLE CUST LAC {TestId}".ToUpperInvariant();
        var resp = await admin.GetAsync(
            $"/api/admin/fundos/custodiantes?page=1&pageSize=50&search={Uri.EscapeDataString(searchTerm)}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminCustodianteListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        body.Items.ShouldNotBeNull();
        body.Items.ShouldContain(x =>
            x.RazaoSocial.ToLower().Contains($"ilikesearchable cust lac {TestId}".ToLower()));
    }

    // =========================================================================
    // Branch 2 + CNPJ digits: Search by CNPJ digit substring (digitsOnly branch)
    // =========================================================================

    [Fact]
    public async Task ListAdminCustodiantes_SearchByCnpjDigits_ReturnsMatchingItems()
    {
        // Arrange — create a custodiante so TestCnpj is known
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"CNPJ Digits Cust LAC {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — search with a 6-digit prefix of TestCnpj (numeric chars → digitsOnly.Length > 0)
        using var admin = AdminClient();
        var digitsSubstring = TestCnpj[..6]; // first 6 digits — specific enough
        var resp = await admin.GetAsync(
            $"/api/admin/fundos/custodiantes?page=1&pageSize=50&search={digitsSubstring}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminCustodianteListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // Search with no match → empty result
    // =========================================================================

    [Fact]
    public async Task ListAdminCustodiantes_SearchNoMatch_ReturnsEmptyResult()
    {
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            "/api/admin/fundos/custodiantes?page=1&pageSize=50&search=zzznomatchcust999lacxyz");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminCustodianteListDto>(
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
    public async Task ListAdminCustodiantes_CompanyIdAndSearch_ReturnsFilteredItems()
    {
        // Arrange — PJ-A creates a custodiante
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes", new
        {
            razaoSocial = $"Combined Filter Cust LAC {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — admin filters by companyId AND searches by name (both branches active)
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            $"/api/admin/fundos/custodiantes?page=1&pageSize=50" +
            $"&companyId={_companyAId}" +
            $"&search={Uri.EscapeDataString($"Combined Filter Cust LAC {TestId}")}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminCustodianteListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        body.Items.ShouldNotBeNull();
        body.Items.ShouldAllBe(x => x.ClienteId == _companyAId);
        body.Items.ShouldContain(x => x.RazaoSocial.Contains($"Combined Filter Cust LAC {TestId}"));
    }

    // =========================================================================
    // Security: no token → 401
    // =========================================================================

    [Fact]
    public async Task ListAdminCustodiantes_NoToken_Returns401()
    {
        using var anon = CreateUnauthenticatedClient();
        var resp = await anon.GetAsync("/api/admin/fundos/custodiantes?page=1&pageSize=10");
        resp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Local DTO types — self-contained deserialization
    // Status is serialized as string (JsonStringEnumConverter) — typed as string, NOT int.
    // =========================================================================

    private sealed class AdminCustodianteListDto
    {
        public AdminCustodianteItemDto[]? Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private sealed class AdminCustodianteItemDto
    {
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public string EmpresaNome { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string? CodigoInterno { get; set; }
        public string Cnpj { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public string Status { get; set; } = string.Empty; // serialized as string, NOT int
    }
}
