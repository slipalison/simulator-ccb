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
/// Integration tests for ListAdminConsultoriaQueryHandler — requires PostgreSQL (real provider).
///
/// The handler in FundosAdminQueryHandlers.cs has two conditional branches that were uncovered:
///   1. Line 101 — query.CompanyId.HasValue (true branch): WHERE Consultoria.ClienteId = companyId
///   2. Lines 104-113 — search path: ILike on RazaoSocial + NomeFantasia null check + CNPJ digits
///
/// Existing tests (AdminFundosByIdIntegrationTests.AdminListConsultorias_*) hit only the no-search,
/// no-companyId path, leaving both branches uncovered in the merged report.
///
/// These tests exercise:
///   - companyId filter (true branch of HasValue) → scopes result to one company
///   - search by RazaoSocial substring (ILike path)
///   - search with NomeFantasia non-null (NomeFantasia branch within search predicate)
///   - search by CNPJ digits (digitsOnly.Length > 0 branch)
///   - empty result when companyId has no matching consultorias
///
/// Security: all tests go through BearerBackoffice + CrossCompanyAccess policy.
/// Multi-tenant note: these are admin-only cross-company reads (IgnoreQueryFilters applied).
/// </summary>
[Trait("Category", "Integration")]
public sealed class ListAdminConsultoriaIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private Guid _companyAId;
    private Guid _companyBId;

    private const string SubPjA = "lac-pja-sub-010";
    private const string SubPjB = "lac-pjb-sub-011";

    // Valid CNPJs generated at construction time — guarantees valid check digits + uniqueness.
    private readonly string _cnpjCompanyA;
    private readonly string _cnpjCompanyB;

    public ListAdminConsultoriaIntegrationTests(PostgreSqlFixture fixture) : base(fixture)
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
            "Empresa Alpha LAC Ltda",
            cnpjCompanyA,
            "alpha.lac@test.com",
            "+5511000002001",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.1.1"));
        companyA.SetKeycloakUserId(SubPjA);

        // PJ-B
        var companyB = Company.Register(
            "Empresa Beta LAC S.A.",
            cnpjCompanyB,
            "beta.lac@test.com",
            "+5511000002002",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.1.2"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();
    }

    // =========================================================================
    // HTTP client helpers
    // =========================================================================

    private HttpClient AdminClient() => CreateAdminJwt("lac-admin-backoffice");
    private HttpClient ClientPjA() => CreateClientJwt(SubPjA);

    // =========================================================================
    // Branch 1: CompanyId filter (query.CompanyId.HasValue == true)
    // Covered by: passing ?companyId=<guid> to GET /api/admin/fundos/consultorias
    // =========================================================================

    [Fact]
    public async Task ListAdminConsultorias_WithCompanyIdFilter_ReturnsOnlyThatCompanyItems()
    {
        // Arrange — PJ-A creates a consultoria in its own scope
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Consultoria LAC Alpha {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — admin filters by PJ-A's companyId (CompanyId.HasValue = true branch)
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            $"/api/admin/fundos/consultorias?page=1&pageSize=50&companyId={_companyAId}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminConsultoriaListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        // All returned items must belong to company A
        body!.Items.ShouldNotBeNull();
        body.Items.ShouldAllBe(x => x.ClienteId == _companyAId);
        // At least the one we just created
        body.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // Branch 1 + empty: CompanyId with no consultorias in that company
    // =========================================================================

    [Fact]
    public async Task ListAdminConsultorias_CompanyIdWithNoConsultorias_ReturnsEmptyResult()
    {
        // Arrange — PJ-B has no consultorias in the seed; we only seed companies
        // Act — filter by PJ-B companyId (has no consultorias seeded for it in this class)
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            $"/api/admin/fundos/consultorias?page=1&pageSize=50&companyId={_companyBId}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminConsultoriaListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        // PJ-B created no consultorias in this class → companyId filter returns 0 for PJ-B's scope
        // (other test classes may have created consultorias for PJ-B's company but they have
        // different company IDs since each test class has its own container fixture)
        body!.TotalCount.ShouldBe(0);
    }

    // =========================================================================
    // Branch 2: Search by RazaoSocial (ILike path)
    // =========================================================================

    [Fact]
    public async Task ListAdminConsultorias_SearchByRazaoSocial_ReturnsMatchingItems()
    {
        // Arrange — PJ-A creates a consultoria with a unique recognisable name
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"ILikeSearchable LAC {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — admin searches by the unique substring (uppercase to exercise ILike case-insensitivity)
        using var admin = AdminClient();
        var searchTerm = $"ILIKESEARCHABLE LAC {TestId}".ToUpperInvariant();
        var resp = await admin.GetAsync(
            $"/api/admin/fundos/consultorias?page=1&pageSize=50&search={Uri.EscapeDataString(searchTerm)}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminConsultoriaListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        body.Items.ShouldNotBeNull();
        body.Items.ShouldContain(x => x.RazaoSocial.ToLower().Contains($"ilikesearchable lac {TestId}".ToLower()));
    }

    // =========================================================================
    // Branch 2 + NomeFantasia: Search by NomeFantasia (non-null NomeFantasia branch)
    // =========================================================================

    [Fact]
    public async Task ListAdminConsultorias_SearchByNomeFantasia_ReturnsMatchingItems()
    {
        // Arrange — create a consultoria with nomeFantasia via HTTP
        using var clientA = ClientPjA();
        var nomeFant = $"FantasiaLAC {TestId}";
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Razao Social Nf {TestId}",
            cnpj = TestCnpj,
            nomeFantasia = nomeFant
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — search by nomeFantasia substring (uppercase to exercise ILike case-insensitivity
        // and confirm the non-null NomeFantasia branch is exercised)
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            $"/api/admin/fundos/consultorias?page=1&pageSize=50&search={Uri.EscapeDataString(nomeFant.ToUpperInvariant())}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminConsultoriaListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.Items.ShouldNotBeNull();
        // The item with matching nomeFantasia should appear
        body.Items.ShouldContain(x => x.NomeFantasia != null &&
            x.NomeFantasia.ToLower().Contains(nomeFant.ToLower()));
    }

    // =========================================================================
    // Branch 2 + CNPJ digits: Search by CNPJ digit substring (digitsOnly branch)
    // =========================================================================

    [Fact]
    public async Task ListAdminConsultorias_SearchByCnpjDigits_ReturnsMatchingItems()
    {
        // Arrange — create a consultoria; we know TestCnpj digits
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"CNPJ Digits LAC {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — search with a 6-digit prefix of TestCnpj (numeric chars → digitsOnly.Length > 0)
        using var admin = AdminClient();
        var digitsSubstring = TestCnpj[..6]; // first 6 digits — specific enough
        var resp = await admin.GetAsync(
            $"/api/admin/fundos/consultorias?page=1&pageSize=50&search={digitsSubstring}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminConsultoriaListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        // At minimum the consultoria we just created should appear
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // CompanyId + Search combined (both branches active simultaneously)
    // =========================================================================

    [Fact]
    public async Task ListAdminConsultorias_CompanyIdAndSearch_ReturnsFilteredItems()
    {
        // Arrange — PJ-A creates a consultoria
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/consultorias", new
        {
            razaoSocial = $"Combined Filter LAC {TestId}",
            cnpj = TestCnpj
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — admin filters by companyId AND searches by name (both branches active)
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            $"/api/admin/fundos/consultorias?page=1&pageSize=50" +
            $"&companyId={_companyAId}" +
            $"&search={Uri.EscapeDataString($"Combined Filter LAC {TestId}")}");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminConsultoriaListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        body.Items.ShouldNotBeNull();
        body.Items.ShouldAllBe(x => x.ClienteId == _companyAId);
        body.Items.ShouldContain(x => x.RazaoSocial.Contains($"Combined Filter LAC {TestId}"));
    }

    // =========================================================================
    // Security: no token → 401
    // =========================================================================

    [Fact]
    public async Task ListAdminConsultorias_NoToken_Returns401()
    {
        using var anon = CreateUnauthenticatedClient();
        var resp = await anon.GetAsync($"/api/admin/fundos/consultorias?companyId={_companyAId}");
        resp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Local DTO types — self-contained deserialization
    // =========================================================================

    private sealed class AdminConsultoriaListDto
    {
        public AdminConsultoriaItemDto[]? Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private sealed class AdminConsultoriaItemDto
    {
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public string EmpresaNome { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string? NomeFantasia { get; set; }
        public string Cnpj { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
