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
/// Integration tests for CompanyRepository.GetPagedAsync — requires PostgreSQL (real provider).
///
/// GetPagedAsync uses:
///   - c.IsDeleted (computed property backed by DeletedAt column — not translatable by InMemory)
///   - c.RazaoSocial.ToLower().Contains() / c.Cnpj.Value.ToLower().Contains() (SQL string ops)
///   - c.Email.Value.ToLower().Contains() (owned value-object navigation)
///   - OrderByDescending(c => c.TermsAcceptance.AcceptedAt) (owned-type ordering)
///
/// These are covered here via GET /api/admin/companies which calls GetPaginatedCompaniesHandler
/// which delegates to CompanyRepository.GetPagedAsync.
///
/// Uncovered methods targeted:
///   - GetPagedAsync: search term path (ToLower().Contains)
///   - GetPagedAsync: status="active" filter (non-deleted)
///   - GetPagedAsync: status="deleted" filter (IsDeleted computed)
///   - GetPagedAsync: empty search (no filter — ordering path)
///   - GetPagedAsync: paging (Skip/Take)
///
/// Security: all tests go through BearerBackoffice + CrossCompanyAccess policy.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CompanyRepositoryIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    // Unique subs to avoid conflict with other test classes sharing the same container
    private const string SubAlpha = "crep-alpha-sub-cr-001";
    private const string SubBeta  = "crep-beta-sub-cr-002";
    private const string SubGamma = "crep-gamma-sub-cr-003";

    // Hardcoded valid CNPJs distinct from all other test classes
    private const string CnpjAlpha = "65527436000136";
    private const string CnpjBeta  = "28536107000107";
    private const string CnpjGamma = "54276337000161";

    public CompanyRepositoryIntegrationTests(PostgreSqlFixture fixture) : base(fixture) { }

    // =========================================================================
    // Seed — runs once per class via EnsureSeedAsync guard
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
    }

    private static async Task SeedAsync(AppDbContext db)
    {
        // Alpha — active company, searchable by "Alpha Pesquisavel"
        var alpha = Company.Register(
            "Alpha Pesquisavel CRUD Ltda",
            CnpjAlpha,
            "alpha.crep@test.com",
            "+5511000001001",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.0.1"));
        alpha.SetKeycloakUserId(SubAlpha);

        // Beta — active company
        var beta = Company.Register(
            "Beta Empresa CRUD S.A.",
            CnpjBeta,
            "beta.crep@test.com",
            "+5511000001002",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.0.2"));
        beta.SetKeycloakUserId(SubBeta);

        // Gamma — soft-deleted (anonymised)
        var gamma = Company.Register(
            "Gamma Empresa CRUD Ltda",
            CnpjGamma,
            "gamma.crep@test.com",
            "+5511000001003",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.0.3"));
        gamma.SetKeycloakUserId(SubGamma);
        gamma.Anonymize(); // sets DeletedAt → IsDeleted = true

        await db.Companies.AddRangeAsync(alpha, beta, gamma);
        await db.SaveChangesAsync();
    }

    // =========================================================================
    // HTTP client helpers
    // =========================================================================

    private HttpClient AdminClient() => CreateAdminJwt("crep-admin-backoffice");

    // =========================================================================
    // GetPagedAsync — empty search, no status filter (exercises OrderBy path)
    // =========================================================================

    [Fact]
    public async Task GetCompanies_NoFilters_Returns200WithActiveAndDeleted()
    {
        // Arrange
        using var client = AdminClient();

        // Act
        var resp = await client.GetAsync("/api/admin/companies?page=1&pageSize=50");

        // Assert — 200 with at least the seeded rows (other test classes may add more)
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<PaginatedCompaniesDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(3); // alpha + beta + gamma
    }

    // =========================================================================
    // GetPagedAsync — status="active" filter (IsDeleted computed property path)
    // =========================================================================

    [Fact]
    public async Task GetCompanies_StatusActive_ExcludesDeletedCompanies()
    {
        // Arrange
        using var client = AdminClient();

        // Act — status=active must exclude the soft-deleted gamma company
        var resp = await client.GetAsync("/api/admin/companies?page=1&pageSize=50&status=active");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<PaginatedCompaniesDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        // All returned items must not be deleted
        body!.Items.ShouldNotBeNull();
        body.Items.ShouldAllBe(c => !c.IsDeleted);
        // Gamma (deleted) must not appear
        body.Items.ShouldNotContain(c => c.Email.Contains("deleted-") && c.RazaoSocial == "Empresa Excluída");
    }

    // =========================================================================
    // GetPagedAsync — status="deleted" filter (IsDeleted computed property path)
    // =========================================================================

    [Fact]
    public async Task GetCompanies_StatusDeleted_ReturnsOnlyDeletedCompanies()
    {
        // Arrange
        using var client = AdminClient();

        // Act — status=deleted must include the anonymised gamma company
        var resp = await client.GetAsync("/api/admin/companies?page=1&pageSize=50&status=deleted");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<PaginatedCompaniesDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        // All returned items must be deleted
        body.Items.ShouldNotBeNull();
        body.Items.ShouldAllBe(c => c.IsDeleted);
    }

    // =========================================================================
    // GetPagedAsync — search by RazaoSocial (ToLower().Contains path)
    // =========================================================================

    [Fact]
    public async Task GetCompanies_SearchByRazaoSocial_ReturnsMatchingCompany()
    {
        // Arrange — "pesquisavel" substring (lowercase) should match "Alpha Pesquisavel CRUD Ltda"
        using var client = AdminClient();

        // Act
        var resp = await client.GetAsync(
            "/api/admin/companies?page=1&pageSize=20&search=pesquisavel");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<PaginatedCompaniesDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        body.Items.ShouldNotBeNull();
        body.Items.ShouldContain(c => c.RazaoSocial.ToLower().Contains("pesquisavel"));
    }

    // =========================================================================
    // GetPagedAsync — search by email (ToLower().Contains path on owned VO)
    // =========================================================================

    [Fact]
    public async Task GetCompanies_SearchByEmailSubstring_ReturnsMatchingCompany()
    {
        // Arrange — "alpha.crep" substring should match alpha company
        using var client = AdminClient();

        // Act
        var resp = await client.GetAsync(
            "/api/admin/companies?page=1&pageSize=20&search=alpha.crep");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<PaginatedCompaniesDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.Items.ShouldNotBeNull();
        body.Items.ShouldContain(c => c.Email.Contains("alpha.crep"));
    }

    // =========================================================================
    // GetPagedAsync — search with no match (non-matching term)
    // =========================================================================

    [Fact]
    public async Task GetCompanies_SearchNoMatch_ReturnsEmptyResult()
    {
        // Arrange — a search term that cannot possibly match any seeded company
        using var client = AdminClient();

        // Act
        var resp = await client.GetAsync(
            "/api/admin/companies?page=1&pageSize=20&search=zzznomatch999crep");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<PaginatedCompaniesDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBe(0);
        body.Items.ShouldNotBeNull();
        body.Items.ShouldBeEmpty();
    }

    // =========================================================================
    // GetPagedAsync — paging (Skip/Take path)
    // =========================================================================

    [Fact]
    public async Task GetCompanies_PagingPage2_ReturnsSecondPage()
    {
        // Arrange — page=2 with pageSize=1 to force Skip(1).Take(1)
        // We have at least 3 companies seeded — page 2 should return exactly 1 item
        using var client = AdminClient();

        // Act
        var resp = await client.GetAsync(
            "/api/admin/companies?page=2&pageSize=1");

        // Assert
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<PaginatedCompaniesDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.Items.ShouldNotBeNull();
        body.Items.Length.ShouldBe(1);
        body.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    // =========================================================================
    // Security: no token → 401
    // =========================================================================

    [Fact]
    public async Task GetCompanies_NoToken_Returns401()
    {
        using var anon = CreateUnauthenticatedClient();
        var resp = await anon.GetAsync("/api/admin/companies?page=1&pageSize=10");
        resp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Local DTO types — self-contained deserialization
    // =========================================================================

    private sealed class PaginatedCompaniesDto
    {
        public CompanyItemDto[]? Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private sealed class CompanyItemDto
    {
        public Guid Id { get; set; }
        public string RazaoSocial { get; set; } = string.Empty;
        public string Cnpj { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public string? KeycloakUserId { get; set; }
    }
}
