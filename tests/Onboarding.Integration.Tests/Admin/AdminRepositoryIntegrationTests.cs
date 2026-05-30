using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Persistence;
using Onboarding.Infrastructure.Repositories;
using Onboarding.Integration.Tests.Fixtures;
using Shouldly;

namespace Onboarding.Integration.Tests.Admin;

/// <summary>
/// Integration tests for AdminRepository — requires PostgreSQL (real provider).
///
/// AdminRepository.ApplySearch uses EF.Functions.ILike which requires the Npgsql provider.
/// InMemory cannot translate ILike → these tests must run against Testcontainers PostgreSQL.
///
/// Uncovered methods / branches targeted:
///   - ApplySearch: ILike on RazaoSocial (case-insensitive substring)
///   - ApplySearch: ILike on Email (case-insensitive substring)
///   - ApplySearch: CNPJ digits-only path (digitsOnly.Length > 0 branch)
///   - GetPagedAsync: search path (non-null search triggers ApplySearch)
///   - GetPagedAsync: status="deleted" with search combined
///   - GetPagedAsync with paging + search
///
/// All tests hit the repository directly via IAdminRepository (not via HTTP) to isolate
/// repository logic from controller mapping; each test uses BeginTransactionAsync +
/// RollbackTransactionAsync for isolation without restarting the container.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AdminRepositoryIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    public AdminRepositoryIntegrationTests(PostgreSqlFixture fixture) : base(fixture) { }

    // =========================================================================
    // Helper — creates a company in the given DbContext (no EnsureSeed needed
    // because each test uses its own rolled-back transaction)
    // =========================================================================

    private static Company CreateCompany(
        string razaoSocial,
        string cnpj,
        string email,
        bool deleted = false)
    {
        var c = Company.Register(
            razaoSocial, cnpj, email, "+5511000009999",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.10.10.10"));
        if (deleted)
            c.Anonymize();
        return c;
    }

    // =========================================================================
    // ApplySearch — ILike on RazaoSocial (case-insensitive, mixed case input)
    // =========================================================================

    [Fact]
    public async Task GetPagedAsync_SearchByRazaoSocialILike_ReturnsCaseInsensitiveMatch()
    {
        // Arrange — unique CNPJ per test via TestCnpj + Fixture counter
        var db = await BeginTransactionAsync();
        var repo = new AdminRepository(db);

        var target = CreateCompany(
            $"ILike Target Empresa {TestId}",
            GenerateCnpj(Fixture.NextCnpjSlot()),
            $"ilike.target.{TestId}@test.com");
        var other = CreateCompany(
            $"Other Empresa {TestId}",
            GenerateCnpj(Fixture.NextCnpjSlot()),
            $"other.{TestId}@test.com");
        db.Companies.AddRange(target, other);
        await db.SaveChangesAsync();

        // Act — "ILIKE TARGET" upper-case search must hit ILike (case-insensitive)
        var (items, total) = await repo.GetPagedAsync(
            1, 50, $"ILIKE TARGET EMPRESA {TestId}", null);

        // Assert
        total.ShouldBeGreaterThanOrEqualTo(1);
        items.ShouldContain(c => c.Id == target.Id);
        items.ShouldNotContain(c => c.Id == other.Id);
    }

    // =========================================================================
    // ApplySearch — ILike on Email value object
    // =========================================================================

    [Fact]
    public async Task GetPagedAsync_SearchByEmailILike_ReturnsCaseInsensitiveMatch()
    {
        var db = await BeginTransactionAsync();
        var repo = new AdminRepository(db);

        var emailPart = $"emailsrch{TestId}";
        var target = CreateCompany(
            $"Email Search Company {TestId}",
            GenerateCnpj(Fixture.NextCnpjSlot()),
            $"{emailPart}@testdomain.com");
        var other = CreateCompany(
            $"Other Email Empresa {TestId}",
            GenerateCnpj(Fixture.NextCnpjSlot()),
            $"unrelated.{TestId}@other.com");
        db.Companies.AddRange(target, other);
        await db.SaveChangesAsync();

        // Act — search by email partial (all uppercase to exercise ILike case-insensitivity)
        var (items, total) = await repo.GetPagedAsync(
            1, 50, emailPart.ToUpperInvariant(), null);

        // Assert
        total.ShouldBeGreaterThanOrEqualTo(1);
        items.ShouldContain(c => c.Id == target.Id);
        items.ShouldNotContain(c => c.Id == other.Id);
    }

    // =========================================================================
    // ApplySearch — CNPJ digits-only branch (digitsOnly.Length > 0)
    // =========================================================================

    [Fact]
    public async Task GetPagedAsync_SearchByCnpjDigits_ReturnsMatchingCompany()
    {
        var db = await BeginTransactionAsync();
        var repo = new AdminRepository(db);

        var cnpj = GenerateCnpj(Fixture.NextCnpjSlot());
        var target = CreateCompany(
            $"CNPJ Digits Search {TestId}",
            cnpj,
            $"cnpjsrch.{TestId}@test.com");
        db.Companies.Add(target);
        await db.SaveChangesAsync();

        // Act — search by digits of the CNPJ (the digitsOnly branch in ApplySearch)
        var digitsSubstring = cnpj[..6]; // first 6 digits — unambiguous enough
        var (items, _) = await repo.GetPagedAsync(1, 50, digitsSubstring, null);

        // Assert
        items.ShouldContain(c => c.Id == target.Id);
    }

    // =========================================================================
    // GetPagedAsync — search combined with status=deleted filter
    // =========================================================================

    [Fact]
    public async Task GetPagedAsync_SearchDeletedStatus_ReturnsOnlyDeletedMatchingRows()
    {
        var db = await BeginTransactionAsync();
        var repo = new AdminRepository(db);

        // Active company whose name matches the search — must NOT appear in deleted filter
        var active = CreateCompany(
            $"Shared Name Prefix Active {TestId}",
            GenerateCnpj(Fixture.NextCnpjSlot()),
            $"active.shared.{TestId}@test.com",
            deleted: false);
        // Deleted company with matching name — must appear
        var deleted = CreateCompany(
            $"Deleted Empresa StatusTest {TestId}",
            GenerateCnpj(Fixture.NextCnpjSlot()),
            $"deleted.statustest.{TestId}@test.com",
            deleted: true);
        db.Companies.AddRange(active, deleted);
        await db.SaveChangesAsync();

        // Act — status=deleted + search by the unique TestId
        var (items, total) = await repo.GetPagedAsync(1, 50, $"StatusTest {TestId}", "deleted");

        // Assert — only the deleted company matches
        total.ShouldBe(1);
        items.ShouldContain(c => c.IsDeleted);
        // The returned name is anonymized by Anonymize() → "Empresa Excluída"
        items.ShouldAllBe(c => c.IsDeleted);
    }

    // =========================================================================
    // GetPagedAsync — paging with search (Skip/Take + ILike combined)
    // =========================================================================

    [Fact]
    public async Task GetPagedAsync_PagingWithSearch_ReturnsCorrectPage()
    {
        var db = await BeginTransactionAsync();
        var repo = new AdminRepository(db);

        // Seed 3 companies all matching the search prefix
        for (var i = 0; i < 3; i++)
        {
            db.Companies.Add(CreateCompany(
                $"Pageable Admin {TestId} Item {i}",
                GenerateCnpj(Fixture.NextCnpjSlot()),
                $"page.admin.{TestId}.item{i}@test.com"));
        }
        await db.SaveChangesAsync();

        // Act — page 2 of pageSize 2 (Skip 2, Take 2) → should return 1 item out of 3
        var (items, total) = await repo.GetPagedAsync(2, 2, $"Pageable Admin {TestId}", null);

        // Assert
        total.ShouldBe(3);
        items.Count.ShouldBe(1);
    }

    // =========================================================================
    // GetPagedAsync — empty search term (no-search path still uses paging)
    // =========================================================================

    [Fact]
    public async Task GetPagedAsync_EmptySearch_ReturnsActiveCompaniesWithPaging()
    {
        var db = await BeginTransactionAsync();
        var repo = new AdminRepository(db);

        db.Companies.Add(CreateCompany(
            $"NoSearch Active {TestId}",
            GenerateCnpj(Fixture.NextCnpjSlot()),
            $"nosearch.active.{TestId}@test.com"));
        db.Companies.Add(CreateCompany(
            $"NoSearch Deleted {TestId}",
            GenerateCnpj(Fixture.NextCnpjSlot()),
            $"nosearch.deleted.{TestId}@test.com",
            deleted: true));
        await db.SaveChangesAsync();

        // Act — null search triggers the no-search path; default status = active
        var (items, _) = await repo.GetPagedAsync(1, 50, null, null);

        // Assert — default filter excludes deleted
        items.ShouldAllBe(c => !c.IsDeleted);
    }

    // =========================================================================
    // Security: GET /api/admin/companies with search — exercises the HTTP stack
    // through to AdminRepository.ApplySearch against real PostgreSQL
    // =========================================================================

    [Fact]
    public async Task GetAdminCompanies_SearchViaHttp_Returns200()
    {
        // This exercises the full HTTP → controller → handler → repository chain
        // against real PostgreSQL, confirming ILike doesn't throw in production path.
        using var admin = CreateAdminJwt("adminrep-http-admin");
        var resp = await admin.GetAsync(
            "/api/admin/companies?page=1&pageSize=20&search=testdomain");

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<PaginatedCompaniesResponseDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.ShouldNotBeNull();
        // No assertion on count — the search term "testdomain" may or may not match seeded rows;
        // the test validates that ILike executes without error.
    }

    // =========================================================================
    // Local DTO types — self-contained deserialization
    // =========================================================================

    private sealed class PaginatedCompaniesResponseDto
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
        public bool IsDeleted { get; set; }
    }
}
