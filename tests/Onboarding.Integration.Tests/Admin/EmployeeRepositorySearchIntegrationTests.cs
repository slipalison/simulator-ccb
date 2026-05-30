using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Infrastructure.Persistence;
using Onboarding.Integration.Tests.Fixtures;
using Shouldly;

namespace Onboarding.Integration.Tests.Admin;

/// <summary>
/// Integration tests for EmployeeRepository.GetPagedByCompanyAsync + GetPagedAllAsync
/// — requires PostgreSQL (real provider).
///
/// Both methods use FromSqlInterpolated with ILIKE which is not translatable by InMemory.
/// These are covered via GET /api/admin/employees which calls GetPaginatedEmployeesHandler:
///   - CompanyId.HasValue == true  → GetPagedByCompanyAsync (company-scoped path)
///   - CompanyId.HasValue == false → GetPagedAllAsync       (admin all-companies path)
///
/// Branches covered:
///   GetPagedByCompanyAsync:
///     - search by nome (ILIKE case-insensitive, uppercase input proves case-insensitivity)
///     - search by email (ILIKE path — employees table has an email column)
///     - search by CPF digits (digitsOnly.Length > 0 branch)
///     - search with no match → empty result
///     - status=active filter (only active employees)
///     - status=deleted filter (only soft-deleted employees)
///     - default status (active-only, DeletedAt IS NULL)
///   GetPagedAllAsync:
///     - search by nome across companies (ILIKE path, no companyId filter)
///     - search by CPF digits (digitsOnly.Length > 0 branch)
///     - status filter across companies
///     - empty search → default active-only listing
///
/// Security: all tests go through BearerBackoffice + CrossCompanyAccess policy.
/// Seed strategy: employees are seeded directly via EF Core (no Keycloak call needed).
///   GetPaginatedEmployeesHandler does NOT call Keycloak — safe for integration tests.
/// </summary>
[Trait("Category", "Integration")]
public sealed class EmployeeRepositorySearchIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private Guid _companyAId;
    private Guid _companyBId;
    private Guid _adminGroupAId;

    private const string SubPjA = "emp-srch-pja-sub-050";
    private const string SubPjB = "emp-srch-pjb-sub-051";

    // Valid CNPJs / CPFs generated at construction time — guarantees valid check digits + uniqueness.
    private readonly string _cnpjCompanyA;
    private readonly string _cnpjCompanyB;

    // CPF values for the seeded employees — generated once per fixture lifetime.
    private readonly string _cpfActiveA;
    private readonly string _cpfDeletedA;
    private readonly string _cpfActiveB;

    public EmployeeRepositorySearchIntegrationTests(PostgreSqlFixture fixture) : base(fixture)
    {
        _cnpjCompanyA = GenerateCnpj(fixture.NextCnpjSlot());
        _cnpjCompanyB = GenerateCnpj(fixture.NextCnpjSlot());
        // CPF slots — use NextCnpjSlot() for the counter (fixture provides a single shared counter)
        _cpfActiveA   = GenerateCpf(fixture.NextCnpjSlot());
        _cpfDeletedA  = GenerateCpf(fixture.NextCnpjSlot());
        _cpfActiveB   = GenerateCpf(fixture.NextCnpjSlot());
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
            await SeedAsync(db, _cnpjCompanyA, _cnpjCompanyB, _cpfActiveA, _cpfDeletedA, _cpfActiveB);
        });

        // Re-read company and access group IDs after seed
        using var readScope = CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _companyAId = readDb.Companies.IgnoreQueryFilters()
            .Where(c => c.KeycloakUserId == SubPjA).Select(c => c.Id).First();
        _companyBId = readDb.Companies.IgnoreQueryFilters()
            .Where(c => c.KeycloakUserId == SubPjB).Select(c => c.Id).First();
        _adminGroupAId = readDb.AccessGroups.IgnoreQueryFilters()
            .Where(ag => ag.CompanyId == _companyAId && ag.Name == "admin-empresa")
            .Select(ag => ag.Id).First();
    }

    private static async Task SeedAsync(
        AppDbContext db,
        string cnpjCompanyA,
        string cnpjCompanyB,
        string cpfActiveA,
        string cpfDeletedA,
        string cpfActiveB)
    {
        // PJ-A
        var companyA = Company.Register(
            "Empresa Alpha Emp Search Ltda",
            cnpjCompanyA,
            "alpha.emp.srch@test.com",
            "+5511000006001",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.6.1"));
        companyA.SetKeycloakUserId(SubPjA);

        // PJ-B
        var companyB = Company.Register(
            "Empresa Beta Emp Search S.A.",
            cnpjCompanyB,
            "beta.emp.srch@test.com",
            "+5511000006002",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.6.2"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();

        // Access groups for company A (required FK for Employee.Register)
        var groups = AccessGroup.CreateDefaultGroups(companyA.Id);
        await db.AccessGroups.AddRangeAsync(groups);
        await db.SaveChangesAsync();

        var adminGroupA = groups.First(g => g.Name == "admin-empresa");

        // Employee A-Active — searchable by nome "SearchableAlpha" + cpfActiveA
        var empActiveA = Employee.Register(
            "SearchableAlpha Funcionario Srch",
            cpfActiveA,
            "searchablealpha.srch@testdomain.com",
            "+5511000006011",
            companyA.Id,
            adminGroupA.Id);

        // Employee A-Deleted — soft-deleted; searchable by cpfDeletedA
        var empDeletedA = Employee.Register(
            "DeletedAlpha Funcionario Srch",
            cpfDeletedA,
            "deletedalpha.srch@testdomain.com",
            "+5511000006012",
            companyA.Id,
            adminGroupA.Id);
        empDeletedA.Anonymize(); // sets DeletedAt

        // Employee B-Active — belongs to company B (cross-company admin test)
        var accessGroupsB = AccessGroup.CreateDefaultGroups(companyB.Id);
        await db.AccessGroups.AddRangeAsync(accessGroupsB);
        await db.SaveChangesAsync();

        var adminGroupB = accessGroupsB.First(g => g.Name == "admin-empresa");

        var empActiveB = Employee.Register(
            "SearchableBeta Funcionario Srch",
            cpfActiveB,
            "searchablebeta.srch@testdomain.com",
            "+5511000006021",
            companyB.Id,
            adminGroupB.Id);

        await db.Employees.AddRangeAsync(empActiveA, empDeletedA, empActiveB);
        await db.SaveChangesAsync();
    }

    // =========================================================================
    // HTTP client helpers
    // =========================================================================

    private HttpClient AdminClient() => CreateAdminJwt("emp-srch-admin-backoffice");

    // =========================================================================
    // GetPagedByCompanyAsync — search by nome (ILIKE, case-insensitive)
    // =========================================================================

    [Fact]
    public async Task GetPagedByCompany_SearchByNome_ReturnsMatchingEmployee()
    {
        // "SearchableAlpha" was seeded under PJ-A; searching UPPERCASE exercises ILike
        using var admin = AdminClient();
        var searchTerm = "SEARCHABLEALPHA";
        var resp = await admin.GetAsync(
            $"/api/admin/employees?page=1&pageSize=50" +
            $"&companyId={_companyAId}" +
            $"&search={Uri.EscapeDataString(searchTerm)}");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminEmployeeListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        body.Items.ShouldNotBeNull();
        // All returned items must belong to company A
        body.Items.ShouldAllBe(e => e.CompanyId == _companyAId);
        body.Items.ShouldContain(e => e.Nome.ToLower().Contains("searchablealpha"));
    }

    // =========================================================================
    // GetPagedByCompanyAsync — search by email (ILIKE path)
    // =========================================================================

    [Fact]
    public async Task GetPagedByCompany_SearchByEmail_ReturnsMatchingEmployee()
    {
        // email "searchablealpha.srch@testdomain.com" — search by partial (uppercase)
        using var admin = AdminClient();
        var searchTerm = "SEARCHABLEALPHA.SRCH@TESTDOMAIN";
        var resp = await admin.GetAsync(
            $"/api/admin/employees?page=1&pageSize=50" +
            $"&companyId={_companyAId}" +
            $"&search={Uri.EscapeDataString(searchTerm)}");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminEmployeeListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        body.Items.ShouldNotBeNull();
        body.Items.ShouldContain(e =>
            e.Email.ToLower().Contains("searchablealpha.srch@testdomain"));
    }

    // =========================================================================
    // GetPagedByCompanyAsync — search by CPF digits (digitsOnly.Length > 0)
    // =========================================================================

    [Fact]
    public async Task GetPagedByCompany_SearchByCpfDigits_ReturnsMatchingEmployee()
    {
        // Use a 6-digit prefix of the active employee's CPF (numeric chars → digitsOnly.Length > 0)
        using var admin = AdminClient();
        var digitsSubstring = _cpfActiveA[..6];
        var resp = await admin.GetAsync(
            $"/api/admin/employees?page=1&pageSize=50" +
            $"&companyId={_companyAId}" +
            $"&search={digitsSubstring}");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminEmployeeListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        // Must find the active employee whose CPF starts with those digits
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // GetPagedByCompanyAsync — search no match → empty result
    // =========================================================================

    [Fact]
    public async Task GetPagedByCompany_SearchNoMatch_ReturnsEmptyResult()
    {
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            $"/api/admin/employees?page=1&pageSize=50" +
            $"&companyId={_companyAId}" +
            $"&search=zzznomatchemp999srchxyz");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminEmployeeListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBe(0);
        body.Items.ShouldNotBeNull();
        body.Items.ShouldBeEmpty();
    }

    // =========================================================================
    // GetPagedByCompanyAsync — status=active filter (only active employees)
    // =========================================================================

    [Fact]
    public async Task GetPagedByCompany_StatusActive_ExcludesDeletedEmployees()
    {
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            $"/api/admin/employees?page=1&pageSize=50" +
            $"&companyId={_companyAId}" +
            $"&status=active");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminEmployeeListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.Items.ShouldNotBeNull();
        // All returned items must NOT be deleted
        body.Items.ShouldAllBe(e => !e.IsDeleted);
    }

    // =========================================================================
    // GetPagedByCompanyAsync — status=deleted filter (only soft-deleted employees)
    // =========================================================================

    [Fact]
    public async Task GetPagedByCompany_StatusDeleted_ReturnsOnlyDeletedEmployees()
    {
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            $"/api/admin/employees?page=1&pageSize=50" +
            $"&companyId={_companyAId}" +
            $"&status=deleted");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminEmployeeListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.Items.ShouldNotBeNull();
        // All returned items must be deleted
        body.Items.ShouldAllBe(e => e.IsDeleted);
        body.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // GetPagedAllAsync — search by nome across ALL companies (no companyId)
    // =========================================================================

    [Fact]
    public async Task GetPagedAll_SearchByNome_ReturnsMatchingAcrossCompanies()
    {
        // Both "SearchableAlpha" (PJ-A) and "SearchableBeta" (PJ-B) were seeded
        // Searching "Srch" without companyId exercises GetPagedAllAsync + ILIKE path
        using var admin = AdminClient();
        var searchTerm = "FUNCIONARIO SRCH";
        var resp = await admin.GetAsync(
            $"/api/admin/employees?page=1&pageSize=50" +
            $"&search={Uri.EscapeDataString(searchTerm)}");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminEmployeeListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        // Should find both seeded active employees from PJ-A and PJ-B
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
        body.Items.ShouldNotBeNull();
        body.Items.ShouldContain(e => e.CompanyId == _companyAId);
        body.Items.ShouldContain(e => e.CompanyId == _companyBId);
    }

    // =========================================================================
    // GetPagedAllAsync — search by CPF digits across ALL companies
    // =========================================================================

    [Fact]
    public async Task GetPagedAll_SearchByCpfDigits_ReturnsMatchingAcrossCompanies()
    {
        // Use CPF digits from the PJ-B active employee — admin must see it cross-company
        using var admin = AdminClient();
        var digitsSubstring = _cpfActiveB[..6];
        var resp = await admin.GetAsync(
            $"/api/admin/employees?page=1&pageSize=50" +
            $"&search={digitsSubstring}");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminEmployeeListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // GetPagedAllAsync — status=deleted across ALL companies
    // =========================================================================

    [Fact]
    public async Task GetPagedAll_StatusDeleted_ReturnsOnlyDeletedAcrossCompanies()
    {
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            "/api/admin/employees?page=1&pageSize=50&status=deleted");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminEmployeeListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.Items.ShouldNotBeNull();
        // All returned items must be deleted
        body.Items.ShouldAllBe(e => e.IsDeleted);
        body.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // GetPagedAllAsync — no search, no status (default active-only)
    // =========================================================================

    [Fact]
    public async Task GetPagedAll_NoFilters_ReturnsActiveEmployees()
    {
        using var admin = AdminClient();
        var resp = await admin.GetAsync(
            "/api/admin/employees?page=1&pageSize=50");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<AdminEmployeeListDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        body!.Items.ShouldNotBeNull();
        // Default filter shows only active employees — no deleted ones
        body.Items.ShouldAllBe(e => !e.IsDeleted);
        body.TotalCount.ShouldBeGreaterThanOrEqualTo(2); // at least PJ-A + PJ-B active employees
    }

    // =========================================================================
    // Security: no token → 401
    // =========================================================================

    [Fact]
    public async Task GetAdminEmployees_NoToken_Returns401()
    {
        using var anon = CreateUnauthenticatedClient();
        var resp = await anon.GetAsync("/api/admin/employees?page=1&pageSize=10");
        resp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Local DTO types — self-contained deserialization
    // IsDeleted is a boolean — no enum serialization concern here.
    // =========================================================================

    private sealed class AdminEmployeeListDto
    {
        public AdminEmployeeItemDto[]? Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private sealed class AdminEmployeeItemDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public Guid CompanyId { get; set; }
        public string? CompanyRazaoSocial { get; set; }
        public Guid AccessGroupId { get; set; }
        public string? AccessGroupName { get; set; }
        public bool IsDeleted { get; set; }
        public string? KeycloakUserId { get; set; }
    }
}
