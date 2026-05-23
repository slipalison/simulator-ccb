using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Integration.Tests.Fixtures;

/// <summary>
/// Base class for integration tests that use <see cref="PostgreSqlFixture"/>.
///
/// D-37: provides per-test transaction rollback support — each test can call
/// <see cref="BeginTransactionAsync"/> to open a transaction on the <see cref="AppDbContext"/>
/// obtained from the DI container, then <see cref="RollbackTransactionAsync"/> (or allow
/// <see cref="DisposeAsync"/> to roll back) to isolate its writes without restarting the container.
///
/// HTTP client helpers (<see cref="CreateClientJwt"/>, <see cref="CreateAdminJwt"/>,
/// <see cref="CreateUnauthenticatedClient"/>) are shared here, eliminating per-class duplication.
///
/// Usage:
/// <code>
/// [Trait("Category", "Integration")]
/// public sealed class MyTests : PostgreSqlIntegrationTestBase, IClassFixture&lt;PostgreSqlFixture&gt;
/// {
///     public MyTests(PostgreSqlFixture fixture) : base(fixture) { }
///     // override InitializeAsync / DisposeAsync for class-specific seeding
/// }
/// </code>
/// </summary>
public abstract class PostgreSqlIntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlFixture Fixture;
    protected WebApplicationFactory<Program> Factory => Fixture.Factory;

    private IDbContextTransaction? _transaction;
    private IServiceScope? _transactionScope;

    /// <summary>
    /// A short unique identifier (8 hex chars) generated per test instance.
    /// Use as a suffix on entity names in tests to avoid DB conflicts when multiple tests
    /// in the same class share the same PostgreSQL container.
    /// Example: <c>$"Consultoria Test {TestId}"</c>
    /// </summary>
    protected readonly string TestId = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// A unique valid CNPJ generated for this test instance via <see cref="GenerateCnpj"/>.
    /// Each test method automatically receives a different CNPJ, preventing (ClientId, Cnpj)
    /// uniqueness violations when multiple tests in the same class share the container.
    ///
    /// The value is lazily allocated on first access using the fixture's counter.
    /// </summary>
    protected string TestCnpj => _testCnpj ??= GenerateCnpj(Fixture.NextCnpjSlot());
    private string? _testCnpj;

    /// <summary>
    /// A unique valid CPF generated for this test instance via <see cref="GenerateCpf"/>.
    /// Each test method automatically receives a different CPF, preventing (ClientId, Cpf)
    /// uniqueness violations when multiple tests in the same class share the container.
    ///
    /// The value is lazily allocated on first access using the fixture's counter.
    /// </summary>
    protected string TestCpf => _testCpf ??= GenerateCpf(Fixture.NextCnpjSlot());
    private string? _testCpf;

    /// <summary>
    /// Generates a valid CNPJ deterministically from a 10-digit base (12345678000100 style).
    /// Uses the same modulo-11 double-digit algorithm as <c>Cnpj.IsValid</c> in the domain.
    ///
    /// Usage: <c>GenerateCnpj(testCounter)</c> where testCounter is a per-class counter dispensed
    /// by the fixture to ensure each test method within the class gets a unique CNPJ for PJ-A
    /// and PJ-B entities without (ClientId, Cnpj) collision.
    ///
    /// The base is constructed as: <c>counterValue.ToString("D8") + "0001"</c> (12 digits total),
    /// then two check digits are appended.
    /// </summary>
    protected static string GenerateCnpj(int counter)
    {
        // Build 12-digit base: 8-digit counter prefix + "0001" (branch = 0001 = matriz)
        var prefix = (counter % 100_000_000).ToString("D8");
        var cnpj12 = prefix + "0001";

        static int Digit(string s, int[] weights)
        {
            var sum = 0;
            for (var i = 0; i < weights.Length; i++)
                sum += (s[i] - '0') * weights[i];
            var rem = sum % 11;
            return rem < 2 ? 0 : 11 - rem;
        }

        var d1 = Digit(cnpj12, new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 });
        var cnpj13 = cnpj12 + d1;
        var d2 = Digit(cnpj13, new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 });

        var result = cnpj13 + d2;
        // Skip all-same sequences (rejected by domain validator)
        if (result.Distinct().Count() == 1)
        {
            return GenerateCnpj(counter + 1);
        }

        return result;
    }

    /// <summary>
    /// Generates a valid CPF deterministically from a counter value.
    /// Uses the same modulo-11 two-digit algorithm as <c>Cpf.IsValid</c> in the domain.
    ///
    /// The 9-digit base is padded from <c>counter</c>; two check digits are appended.
    /// Skips all-same-digit sequences (rejected by domain validator).
    /// </summary>
    protected static string GenerateCpf(int counter)
    {
        // Build 9-digit base from counter — offset by 100_000_000 to avoid leading zeros on first few
        var cpf9 = (100_000_000 + (counter % 900_000_000)).ToString("D9");

        static int Digit(string s, int[] weights)
        {
            var sum = 0;
            for (var i = 0; i < weights.Length; i++)
                sum += (s[i] - '0') * weights[i];
            var rem = sum % 11;
            return rem < 2 ? 0 : 11 - rem;
        }

        var d1 = Digit(cpf9, new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 });
        var cpf10 = cpf9 + d1;
        var d2 = Digit(cpf10, new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 });

        var result = cpf10 + d2;
        // Skip all-same sequences (rejected by domain validator)
        if (result.Distinct().Count() == 1)
        {
            return GenerateCpf(counter + 1);
        }

        return result;
    }

    protected PostgreSqlIntegrationTestBase(PostgreSqlFixture fixture)
    {
        Fixture = fixture;
    }

    // =========================================================================
    // IAsyncLifetime — per-test setup/teardown
    // Per-test logic runs AFTER IClassFixture.InitializeAsync (container + schema).
    //
    // IMPORTANT: xUnit creates one test-class instance per test METHOD, so InitializeAsync
    // and DisposeAsync are both called once per test. Seeding must be protected against
    // duplicate execution — use Fixture.EnsureSeedAsync(async () => { ... }) which runs
    // the delegate only once per fixture lifetime (once per class).
    // =========================================================================

    /// <summary>
    /// Override to add per-test-class seeding.  Wrap seed logic in
    /// <c>await Fixture.EnsureSeedAsync(async () => { ... })</c> to prevent re-seeding
    /// on every test method invocation.  Call <c>await base.InitializeAsync()</c> when overriding.
    /// </summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Rolls back any open transaction, then disposes the scope.
    /// Override to add per-test teardown — call <c>await base.DisposeAsync()</c> last.
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        await RollbackTransactionAsync();
    }

    // =========================================================================
    // Transaction rollback helpers (D-37)
    // =========================================================================

    /// <summary>
    /// Opens a service scope and begins a database transaction on <see cref="AppDbContext"/>.
    /// The transaction is rolled back automatically in <see cref="DisposeAsync"/>, isolating
    /// test writes from subsequent tests without restarting the container.
    ///
    /// Use this in tests that write directly via EF Core (not via HTTP stack).
    /// Tests using the HTTP stack manage their own connections — direct DB transactions
    /// do not encompass the HTTP-dispatched writes.
    /// </summary>
    protected async Task<AppDbContext> BeginTransactionAsync()
    {
        _transactionScope = Factory.Services.CreateScope();
        var db = _transactionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await db.Database.BeginTransactionAsync();
        return db;
    }

    /// <summary>
    /// Rolls back the open transaction (if any) and disposes its scope.
    /// Safe to call even if <see cref="BeginTransactionAsync"/> was never called.
    /// </summary>
    protected async Task RollbackTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_transactionScope is not null)
        {
            _transactionScope.Dispose();
            _transactionScope = null;
        }
    }

    // =========================================================================
    // Service scope helper
    // =========================================================================

    /// <summary>
    /// Creates a DI scope from the factory for direct service/repository access in tests.
    /// Caller is responsible for disposal: <c>using var scope = CreateScope();</c>
    /// </summary>
    protected IServiceScope CreateScope() => Factory.Services.CreateScope();

    // =========================================================================
    // HTTP client helpers
    // =========================================================================

    /// <summary>
    /// Creates an HTTP client configured with a BearerClient JWT for the given sub.
    /// The sub must be seeded in the DB for ClientClaimsMiddleware to resolve the company.
    /// </summary>
    protected HttpClient CreateClientJwt(string sub, string email = "test@integration.test")
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IntegrationFakeJwt.GenerateClientJwt(sub, email));
        return client;
    }

    /// <summary>
    /// Creates an HTTP client configured with a BearerBackoffice JWT with role=admin.
    /// CrossCompanyAccess policy (RequireRole("admin")) will succeed.
    /// </summary>
    protected HttpClient CreateAdminJwt(string sub = "admin-sub-backoffice")
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IntegrationFakeJwt.GenerateAdminJwt(sub));
        return client;
    }

    /// <summary>
    /// Creates an unauthenticated HTTP client (no Authorization header).
    /// Use to assert 401 on protected endpoints.
    /// </summary>
    protected HttpClient CreateUnauthenticatedClient()
        => Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
}
