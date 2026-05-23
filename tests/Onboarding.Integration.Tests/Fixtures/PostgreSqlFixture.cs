using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Onboarding.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Onboarding.Integration.Tests.Fixtures;

/// <summary>
/// xUnit class fixture that owns one Testcontainers PostgreSQL container + one WebApplicationFactory
/// for the lifetime of a test class.  Each test class that uses <see cref="PostgreSqlIntegrationTestBase"/>
/// receives its own isolated container instance via IClassFixture&lt;PostgreSqlFixture&gt;.
///
/// D-37: one container per class — test isolation is achieved by not sharing containers across
/// classes, and optionally by per-test transaction rollback via
/// <see cref="PostgreSqlIntegrationTestBase.BeginTransactionAsync"/>.
///
/// Seeding pattern: test classes that need seed data should call
/// <see cref="EnsureSeedAsync"/> with an async delegate. The delegate is executed exactly once
/// per fixture lifetime, even though <see cref="PostgreSqlIntegrationTestBase.InitializeAsync"/>
/// is called once per test method (xUnit creates one test-class instance per method).
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    /// <summary>
    /// Exposes the Testcontainers-supplied connection string so derived tests can create scopes
    /// directly against the DB if needed (e.g. seeding via IServiceProvider).
    /// </summary>
    public string ConnectionString => _postgres.GetConnectionString();

    // Guards per-class seed against running once per test method
    private readonly SemaphoreSlim _seedLock = new(1, 1);
    private bool _seeded;

    // Thread-safe CNPJ counter — each test method gets a unique slot via NextCnpjSlot()
    private int _cnpjCounter;

    /// <summary>
    /// Returns the next unique integer slot for <see cref="PostgreSqlIntegrationTestBase.GenerateCnpj"/>.
    /// Each test method in the class must call this once per COMPANY CONTEXT to get unique CNPJs
    /// that don't collide on the (ClientId, Cnpj) index.
    /// </summary>
    public int NextCnpjSlot() => Interlocked.Increment(ref _cnpjCounter);

    /// <summary>
    /// Runs <paramref name="seedAction"/> exactly once for the lifetime of this fixture.
    /// Subsequent calls (one per test method) return immediately without re-seeding.
    /// Thread-safe via an async semaphore.
    /// </summary>
    public async Task EnsureSeedAsync(Func<Task> seedAction)
    {
        if (_seeded) return;

        await _seedLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_seeded) return; // double-check after acquiring lock
            await seedAction().ConfigureAwait(false);
            _seeded = true;
        }
        finally
        {
            _seedLock.Release();
        }
    }

    // =========================================================================
    // IAsyncLifetime
    // =========================================================================

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Factory = BuildFactory();

        // Apply EF Core schema once for the entire class run
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // =========================================================================
    // WebApplicationFactory construction
    // =========================================================================

    private WebApplicationFactory<Program> BuildFactory()
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                // Point at Testcontainers PostgreSQL
                builder.UseSetting("ConnectionStrings:AppDb", _postgres.GetConnectionString());

                // Keycloak config stubs — never contacted in tests; JWT validation is overridden below
                builder.UseSetting("Keycloak:BackofficeRealmUrl", "http://localhost:8180/realms/backoffice");
                builder.UseSetting("Keycloak:ClientRealmUrl", "http://localhost:8180/realms/client");
                builder.UseSetting("Keycloak:AuthServerUrl", "http://localhost:8180/");
                builder.UseSetting("Keycloak:AdminClientId", "onboarding-api-admin");
                builder.UseSetting("Keycloak:AdminClientSecret", "test-secret");
                builder.UseSetting("Keycloak:Realm", "client");
                builder.UseSetting("Keycloak:PublicClientId", "onboarding-app");
                builder.UseSetting("Keycloak:ValidIssuer", "http://localhost:8180/realms/client");

                builder.ConfigureTestServices(services =>
                {
                    // Remove real infrastructure health checks (PostgreSQL + Keycloak probes)
                    var configureOptionsType = typeof(IConfigureOptions<HealthCheckServiceOptions>);
                    var toRemove = services
                        .Where(d => d.ServiceType == configureOptionsType)
                        .ToList();
                    foreach (var d in toRemove)
                        services.Remove(d);
                    services.AddHealthChecks()
                        .AddCheck("stub-healthy", () => HealthCheckResult.Healthy("stub-ok"), ["ready"]);

                    // Override BearerClient JWT validation — HMAC symmetric key, no OIDC discovery
                    services.PostConfigure<JwtBearerOptions>("BearerClient", options =>
                    {
                        options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                        {
                            Issuer = "http://localhost:8180/realms/client",
                        };
                        options.TokenValidationParameters.ValidateIssuer = false;
                        options.TokenValidationParameters.ValidateAudience = false;
                        options.TokenValidationParameters.ValidateLifetime = false; // nosemgrep
                        options.TokenValidationParameters.IssuerSigningKey = IntegrationFakeJwt.SecurityKey;
                        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                    });

                    // Override BearerBackoffice JWT validation — HMAC symmetric key + role claim mapping
                    services.PostConfigure<JwtBearerOptions>("BearerBackoffice", options =>
                    {
                        options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                        {
                            Issuer = "http://localhost:8180/realms/backoffice",
                        };
                        options.TokenValidationParameters.ValidateIssuer = false;
                        options.TokenValidationParameters.ValidateAudience = false;
                        options.TokenValidationParameters.ValidateLifetime = false; // nosemgrep
                        options.TokenValidationParameters.IssuerSigningKey = IntegrationFakeJwt.SecurityKey;
                        options.TokenValidationParameters.ValidateIssuerSigningKey = true;

                        // Map flat "role" claim → ClaimTypes.Role so RequireRole("admin") works
                        options.Events = new JwtBearerEvents
                        {
                            OnTokenValidated = context =>
                            {
                                if (context.Principal?.Identity is ClaimsIdentity identity)
                                {
                                    foreach (var claim in context.Principal.FindAll("role").ToList())
                                        identity.AddClaim(new Claim(ClaimTypes.Role, claim.Value));
                                }
                                return Task.CompletedTask;
                            }
                        };
                    });
                });
            });
}

/// <summary>
/// Shared HMAC JWT token factory for all integration tests.
/// Single source replaces the four per-file <c>file static class *FakeJwtHelper</c>
/// that were duplicated across FundosControllerIntegrationTests, RelationshipAggregatesIntegrationTests,
/// AdminFundosByIdIntegrationTests, and AuditLogEntityFilterIntegrationTests.
///
/// Exposed as <c>internal</c> so all test classes in the project can use it without
/// introducing cross-project dependencies.
/// </summary>
internal static class IntegrationFakeJwt
{
    private const string TestSigningKey =
        "this-is-a-test-signing-key-for-integration-tests-only-min-32-bytes!";

    public static readonly SymmetricSecurityKey SecurityKey =
        new(Encoding.UTF8.GetBytes(TestSigningKey));

    private static readonly SigningCredentials Credentials =
        new(SecurityKey, SecurityAlgorithms.HmacSha256);

    /// <summary>
    /// Generates a BearerClient JWT for a PJ owner user.
    /// ClientClaimsMiddleware resolves company from DB by this sub value.
    /// </summary>
    public static string GenerateClientJwt(
        string sub,
        string email = "test@integration.test")
    {
        var claims = new List<Claim>
        {
            new("email", email),
            new("sub", sub),
        };

        var token = new JwtSecurityToken(
            issuer: "http://localhost:8180/realms/client",
            audience: "onboarding-app",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: Credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a BearerBackoffice JWT with role=admin claim.
    /// PostConfigure on BearerBackoffice maps "role" claim → ClaimTypes.Role
    /// so RequireRole("admin") in CrossCompanyAccess policy succeeds.
    /// </summary>
    public static string GenerateAdminJwt(
        string sub,
        string email = "admin@backoffice.integration.test")
    {
        var claims = new List<Claim>
        {
            new("email", email),
            new("sub", sub),
            new("role", "admin"),
        };

        var token = new JwtSecurityToken(
            issuer: "http://localhost:8180/realms/backoffice",
            audience: "http://localhost:8180/realms/backoffice",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: Credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
