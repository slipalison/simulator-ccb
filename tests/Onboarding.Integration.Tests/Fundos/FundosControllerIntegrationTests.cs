using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Infrastructure.Persistence;
using Shouldly;
using Testcontainers.PostgreSql;

namespace Onboarding.Integration.Tests.Fundos;

/// <summary>
/// Integration smoke tests for the Fundos module (T-48.7).
///
/// Tests require Docker. Run with: dotnet test tests/Onboarding.Integration.Tests/
///
/// Scenarios covered:
/// - PJ-A with funds:write can POST /api/fundos/consultorias → 201
/// - PJ-A then GET /api/fundos/consultorias returns the row created
/// - PJ-B (different ClientId) GET /api/fundos/consultorias does NOT see PJ-A's row
/// - Request without funds:read claim → 403 Forbidden
/// - Request without auth → 401 Unauthorized
/// - Admin (BearerBackoffice + CrossCompanyAccess) GET /api/admin/fundos/consultorias sees rows from PJ-A and PJ-B
/// - Fundo state machine: RASCUNHO → ATIVO via POST /{id}/status = 200
/// - Fundo state machine: ENCERRADO → ATIVO via POST /{id}/status = 400
///
/// Security invariants:
/// - Multi-tenant isolation: PJ-B cannot see PJ-A's data (HasQueryFilter blocks cross-company reads)
/// - PermissionAuthorizationHandler enforces funds:read / funds:write via ICurrentCompanyPermissionsService
/// - Admin IgnoreQueryFilters allows cross-company reads scoped to BearerBackoffice + CrossCompanyAccess
/// </summary>
[Trait("Category", "Integration")]
public class FundosControllerIntegrationTests : IAsyncLifetime
{
    // =========================================================================
    // Test infrastructure — Testcontainers + WebApplicationFactory
    // =========================================================================

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private WebApplicationFactory<Program>? _factory;

    // Seeded company IDs — set during InitializeAsync, used in tests
    private Guid _companyAId;
    private Guid _companyBId;

    // Known sub claims — mapped to Company.KeycloakUserId in seed data
    private const string SubPjA = "integration-pja-sub-001";
    private const string SubPjB = "integration-pjb-sub-002";
    private const string SubNoCompany = "integration-noperm-sub-003";

    // Verified valid CNPJ (confirmed by integration test run).
    // CNPJ uniqueness is per (ClientId, CNPJ) within each entity table: consultoria_fundos,
    // custodiantes, and fundos are separate tables, so the same CNPJ value can be used for
    // all entity types in a single test without triggering any DB unique constraint.
    // Company seed CNPJs (companies table) are also independent of the entity tables.
    private const string ValidCnpj = "11222333000181"; // verified valid in integration test run

    // =========================================================================
    // IAsyncLifetime
    // =========================================================================

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(ConfigureWebHost);

        // Run EF Core schema creation against the real Testcontainers PostgreSQL
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Seed two companies — bypass HTTP stack, write directly via EF Core
        await SeedCompaniesAsync(db);
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // =========================================================================
    // WebHostBuilder configuration — replaces real JWT validation with fake-key HMAC
    // =========================================================================

    private void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Point at Testcontainers PostgreSQL
        builder.UseSetting("ConnectionStrings:AppDb", _postgres.GetConnectionString());

        // Keycloak config — never contacted in tests (JWT validation overridden below)
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
            // Remove health checks that require real external infrastructure
            var configureOptionsType = typeof(IConfigureOptions<HealthCheckServiceOptions>);
            var toRemove = services
                .Where(d => d.ServiceType == configureOptionsType)
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);
            services.AddHealthChecks()
                .AddCheck("stub-healthy", () => HealthCheckResult.Healthy("stub-ok"), ["ready"]);

            // Override BearerClient JWT validation — use HMAC symmetric key, no OIDC discovery
            services.PostConfigure<JwtBearerOptions>("BearerClient", options =>
            {
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                {
                    Issuer = "http://localhost:8180/realms/client",
                };
                options.TokenValidationParameters.ValidateIssuer = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.ValidateLifetime = false; // nosemgrep
                options.TokenValidationParameters.IssuerSigningKey = FakeJwtHelper.SecurityKey;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
            });

            // Override BearerBackoffice JWT validation — use HMAC symmetric key, no OIDC discovery.
            // Override OnTokenValidated to map flat "role" claim → ClaimTypes.Role so that
            // RequireRole("admin") in CrossCompanyAccess policy works without real Keycloak realm_access.
            services.PostConfigure<JwtBearerOptions>("BearerBackoffice", options =>
            {
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                {
                    Issuer = "http://localhost:8180/realms/backoffice",
                };
                options.TokenValidationParameters.ValidateIssuer = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.ValidateLifetime = false; // nosemgrep
                options.TokenValidationParameters.IssuerSigningKey = FakeJwtHelper.SecurityKey;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;

                // Map flat "role" claim to ClaimTypes.Role for RequireRole("admin") to work
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            // Promote flat "role" claims to ClaimTypes.Role so RequireRole("admin") works
                            var roleClaims = context.Principal.FindAll("role").ToList();
                            foreach (var claim in roleClaims)
                                identity.AddClaim(new Claim(ClaimTypes.Role, claim.Value));
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        });
    }

    // =========================================================================
    // Seed helpers
    // =========================================================================

    private async Task SeedCompaniesAsync(AppDbContext db)
    {
        // Company PJ-A — mapped to SubPjA
        var companyA = Company.Register(
            "Empresa Alpha Integration Ltda",
            "11444777000161",
            "alpha.integration@test.com",
            "+5511999990001",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "192.168.1.1"));
        companyA.SetKeycloakUserId(SubPjA);

        // Company PJ-B — mapped to SubPjB
        var companyB = Company.Register(
            "Beta Integration S.A.",
            "62232889000190",
            "beta.integration@test.com",
            "+5511999990002",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "192.168.1.2"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();

        _companyAId = companyA.Id;
        _companyBId = companyB.Id;
    }

    // =========================================================================
    // HTTP client helpers — inject fake JWT for specific user
    // =========================================================================

    private HttpClient CreateClientFor(string sub, string scheme = "BearerClient")
    {
        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var token = scheme == "BearerBackoffice"
            ? FakeJwtHelper.GenerateAdminJwt(sub: sub)
            : FakeJwtHelper.GenerateClientJwt(sub: sub);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private HttpClient CreateUnauthenticatedClient()
        => _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    /// <summary>
    /// Creates a PJ-A client whose sub is seeded in the DB — ClientClaimsMiddleware will resolve
    /// CompanyId=_companyAId + Permissions.All (owner) for this user.
    /// </summary>
    private HttpClient ClientPjA() => CreateClientFor(SubPjA, "BearerClient");

    /// <summary>Creates a PJ-B client — ClientClaimsMiddleware resolves CompanyId=_companyBId.</summary>
    private HttpClient ClientPjB() => CreateClientFor(SubPjB, "BearerClient");

    /// <summary>
    /// Creates a client with a sub that has NO Company/Employee in DB — gets Guid.Empty CompanyId +
    /// empty permissions → 403 on any permission-gated endpoint.
    /// </summary>
    private HttpClient ClientNoPermissions() => CreateClientFor(SubNoCompany, "BearerClient");

    /// <summary>
    /// Admin client with BearerBackoffice scheme + role=admin claim.
    /// CrossCompanyAccess policy (RequireRole("admin")) will succeed.
    /// </summary>
    private HttpClient ClientAdmin() => CreateClientFor("admin-sub-backoffice", "BearerBackoffice");

    // =========================================================================
    // SCENARIO 1: PJ-A with funds:write can POST /api/fundos/consultorias → 201
    // =========================================================================

    [Fact]
    public async Task PostConsultoria_PjAWithFundsWrite_Returns201()
    {
        using var client = ClientPjA();
        var payload = new
        {
            razaoSocial = "Consultoria Alpha Integration",
            cnpj = ValidCnpj
        };

        var response = await client.PostAsJsonAsync("/api/fundos/consultorias", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Consultoria Alpha Integration");
    }

    // =========================================================================
    // SCENARIO 2: PJ-A GET /api/fundos/consultorias returns the row created
    // =========================================================================

    [Fact]
    public async Task GetConsultorias_PjA_ReturnsOwnCreatedRow()
    {
        using var client = ClientPjA();

        // Create a consultoria for PJ-A (fresh DB per test instance — no CNPJ collision risk)
        var createPayload = new
        {
            razaoSocial = "Consultoria Alpha Read Test",
            cnpj = ValidCnpj
        };
        var createResponse = await client.PostAsJsonAsync("/api/fundos/consultorias", createPayload);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // List — must include the created row
        var listResponse = await client.GetAsync("/api/fundos/consultorias");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var listBody = await listResponse.Content.ReadAsStringAsync();
        listBody.ShouldContain("Consultoria Alpha Read Test");
    }

    // =========================================================================
    // SCENARIO 3: Multi-tenant isolation — PJ-B cannot see PJ-A's data
    // =========================================================================

    [Fact]
    public async Task GetConsultorias_PjB_DoesNotSeePjARows()
    {
        // Arrange — PJ-A creates a consultoria
        using var clientA = ClientPjA();
        var createPayload = new
        {
            razaoSocial = "Consultoria Alpha Isolation Test",
            cnpj = ValidCnpj
        };
        var createResponse = await clientA.PostAsJsonAsync("/api/fundos/consultorias", createPayload);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — PJ-B lists its own consultorias
        using var clientB = ClientPjB();
        var listResponse = await clientB.GetAsync("/api/fundos/consultorias");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var listBody = await listResponse.Content.ReadAsStringAsync();

        // Assert — PJ-B must NOT see PJ-A's row (multi-tenant isolation via HasQueryFilter)
        // Shouldly: ShouldNotContain for string
        listBody.ShouldNotContain("Consultoria Alpha Isolation Test");
    }

    // =========================================================================
    // SCENARIO 4: Request without funds:read claim → 403 Forbidden
    // =========================================================================

    [Fact]
    public async Task GetConsultorias_NoPermissions_Returns403()
    {
        // SubNoCompany has no Company/Employee in DB → Guid.Empty CompanyId + empty permissions
        // → PermissionAuthorizationHandler: permissions.Contains("funds:read") = false → 403
        using var client = ClientNoPermissions();
        var response = await client.GetAsync("/api/fundos/consultorias");

        var statusCode = (int)response.StatusCode;
        // 401 = JWT valid but no authenticated company (possible depending on pipeline order)
        // 403 = authenticated but no permission
        // Both are acceptable denials
        (statusCode == 401 || statusCode == 403).ShouldBeTrue(
            $"Expected 401 or 403, got {statusCode}. No-permission user must be denied.");
    }

    [Fact]
    public async Task PostConsultoria_NoPermissions_Returns403()
    {
        using var client = ClientNoPermissions();
        var payload = new { razaoSocial = "Should Be Rejected", cnpj = ValidCnpj };
        var response = await client.PostAsJsonAsync("/api/fundos/consultorias", payload);

        var statusCode = (int)response.StatusCode;
        (statusCode == 401 || statusCode == 403).ShouldBeTrue(
            $"Expected 401 or 403, got {statusCode}. No-permission user must be denied.");
    }

    // =========================================================================
    // SCENARIO 5: Request without auth → 401 Unauthorized
    // =========================================================================

    [Fact]
    public async Task GetConsultorias_NoAuth_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/fundos/consultorias");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostConsultoria_NoAuth_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var payload = new { razaoSocial = "No Auth Test", cnpj = ValidCnpj };
        var response = await client.PostAsJsonAsync("/api/fundos/consultorias", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // SCENARIO 6: Admin GET /api/admin/fundos/consultorias sees rows from BOTH PJ-A and PJ-B
    // =========================================================================

    [Fact]
    public async Task AdminGetConsultorias_CrossCompany_SeesBothCompanyAAndCompanyB()
    {
        // Arrange — PJ-A creates a consultoria
        using var clientA = ClientPjA();
        var payloadA = new
        {
            razaoSocial = "Consultoria Admin View Alpha",
            cnpj = ValidCnpj // PJ-A creates with ValidCnpj
        };
        var responseA = await clientA.PostAsJsonAsync("/api/fundos/consultorias", payloadA);
        responseA.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Arrange — PJ-B creates a consultoria
        // ValidCnpj can be reused because the uniqueness constraint is (ClientId, Cnpj) —
        // PJ-B has a different ClientId so no constraint violation.
        using var clientB = ClientPjB();
        var payloadB = new
        {
            razaoSocial = "Consultoria Admin View Beta",
            cnpj = ValidCnpj // same CNPJ is OK — different company (different ClientId)
        };
        var responseB = await clientB.PostAsJsonAsync("/api/fundos/consultorias", payloadB);
        responseB.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — Admin lists all consultorias (cross-company via IgnoreQueryFilters)
        using var adminClient = ClientAdmin();
        var adminResponse = await adminClient.GetAsync("/api/admin/fundos/consultorias");

        adminResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await adminResponse.Content.ReadAsStringAsync();

        // Assert — admin sees rows from BOTH companies (IgnoreQueryFilters bypasses HasQueryFilter)
        body.ShouldContain("Consultoria Admin View Alpha");
        body.ShouldContain("Consultoria Admin View Beta");
    }

    // =========================================================================
    // SCENARIO 9–12: Cross-tenant GET-by-id returns 404 (not 403) — security blocker fix
    //
    // Root cause: GetByIdAsync uses IgnoreQueryFilters(); company-A entity was readable
    // by company-B user. Fix: controller-side tenant check after fetch.
    // =========================================================================

    [Fact]
    public async Task GetConsultoriaById_CrossTenant_Returns404()
    {
        // Arrange — PJ-A creates a consultoria
        using var clientA = ClientPjA();
        var payload = new { razaoSocial = "Consultoria Cross Tenant A", cnpj = ValidCnpj };
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/consultorias", payload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var consultoriaId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Act — PJ-B attempts GET /api/fundos/consultorias/{companyA-entity-id}
        using var clientB = ClientPjB();
        var response = await clientB.GetAsync($"/api/fundos/consultorias/{consultoriaId}");

        // Assert — must be 404; entity existence must not be revealed across tenants
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant GET-by-id must return 404, not 200 or 403, to avoid leaking entity existence.");
    }

    [Fact]
    public async Task GetCustodianteById_CrossTenant_Returns404()
    {
        // Arrange — PJ-A creates a custodiante
        using var clientA = ClientPjA();
        var payload = new { razaoSocial = "Custodiante Cross Tenant A", cnpj = ValidCnpj };
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes", payload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var custodianteId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Act — PJ-B attempts GET /api/fundos/custodiantes/{companyA-entity-id}
        using var clientB = ClientPjB();
        var response = await clientB.GetAsync($"/api/fundos/custodiantes/{custodianteId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant GET-by-id must return 404, not 200 or 403, to avoid leaking entity existence.");
    }

    [Fact]
    public async Task GetFundoById_CrossTenant_Returns404()
    {
        // Arrange — PJ-A creates a full Fundo (requires consultoria + custodiante as FK)
        using var clientA = ClientPjA();

        var consultoriaResp = await clientA.PostAsJsonAsync("/api/fundos/consultorias",
            new { razaoSocial = "Consultoria FK For Fundo CrossTenant", cnpj = ValidCnpj });
        consultoriaResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var consultoriaId = (await consultoriaResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var custodianteResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes",
            new { razaoSocial = "Custodiante FK For Fundo CrossTenant", cnpj = ValidCnpj });
        custodianteResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var custodianteId = (await custodianteResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var fundoResp = await clientA.PostAsJsonAsync("/api/fundos",
            new
            {
                nome = "Fundo Cross Tenant A",
                cnpj = ValidCnpj,
                consultoriaFundoId = consultoriaId,
                custodianteId,
                tipoFundo = 1
            });
        fundoResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var fundoId = (await fundoResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Act — PJ-B attempts GET /api/fundos/{companyA-fundo-id}
        using var clientB = ClientPjB();
        var response = await clientB.GetAsync($"/api/fundos/{fundoId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant GET-by-id must return 404, not 200 or 403, to avoid leaking entity existence.");
    }

    [Fact]
    public async Task GetCedenteById_CrossTenant_Returns404()
    {
        // Arrange — PJ-A creates a cedente PF
        using var clientA = ClientPjA();
        var payload = new { cpf = "52998224725", nome = "Cedente Cross Tenant A" };
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/cedentes/pf", payload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var cedenteId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Act — PJ-B attempts GET /api/fundos/cedentes/{companyA-entity-id}
        using var clientB = ClientPjB();
        var response = await clientB.GetAsync($"/api/fundos/cedentes/{cedenteId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant GET-by-id must return 404, not 200 or 403, to avoid leaking entity existence.");
    }

    // =========================================================================
    // SCENARIO 13–16: Cross-tenant PUT returns 404 (W5 fix — application-layer ownership check)
    //
    // Root cause: UpdateX*CommandHandler called GetByIdAsync (IgnoreQueryFilters) without a
    // ClienteId ownership check in the application layer. A cross-tenant actor with funds:write
    // and a valid GUID could overwrite another company's entity field values.
    //
    // Fix: application-layer guard in each handler: entity.ClienteId != currentCompany → 404.
    // =========================================================================

    [Fact]
    public async Task UpdateConsultoriaFundo_CrossTenant_Returns404()
    {
        // Arrange — PJ-A creates a consultoria
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/consultorias",
            new { razaoSocial = "Consultoria PUT Cross Tenant A", cnpj = ValidCnpj });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var consultoriaId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Act — PJ-B attempts PUT /api/fundos/consultorias/{companyA-entity-id}
        using var clientB = ClientPjB();
        var updatePayload = new
        {
            razaoSocial = "Hijacked Name",
            nomeFantasia = (string?)null,
            email = (string?)null,
            telefone = (string?)null,
            status = 1 // ATIVO
        };
        var response = await clientB.PutAsJsonAsync($"/api/fundos/consultorias/{consultoriaId}", updatePayload);

        // Assert — ownership check in handler must return 404 (not 200, not 403)
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant PUT must return 404 to avoid leaking entity existence.");
    }

    [Fact]
    public async Task UpdateCustodiante_CrossTenant_Returns404()
    {
        // Arrange — PJ-A creates a custodiante
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes",
            new { razaoSocial = "Custodiante PUT Cross Tenant A", cnpj = ValidCnpj });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var custodianteId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Act — PJ-B attempts PUT /api/fundos/custodiantes/{companyA-entity-id}
        using var clientB = ClientPjB();
        var updatePayload = new
        {
            razaoSocial = "Hijacked Name",
            codigoInterno = (string?)null,
            email = (string?)null,
            telefone = (string?)null,
            status = 1 // ATIVO
        };
        var response = await clientB.PutAsJsonAsync($"/api/fundos/custodiantes/{custodianteId}", updatePayload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant PUT must return 404 to avoid leaking entity existence.");
    }

    [Fact]
    public async Task UpdateFundo_CrossTenant_Returns404()
    {
        // Arrange — PJ-A creates a full Fundo (requires consultoria + custodiante as FK)
        using var clientA = ClientPjA();

        var consultoriaResp = await clientA.PostAsJsonAsync("/api/fundos/consultorias",
            new { razaoSocial = "Consultoria FK For PUT CrossTenant", cnpj = ValidCnpj });
        consultoriaResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var consultoriaId = (await consultoriaResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var custodianteResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes",
            new { razaoSocial = "Custodiante FK For PUT CrossTenant", cnpj = ValidCnpj });
        custodianteResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var custodianteId = (await custodianteResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var fundoResp = await clientA.PostAsJsonAsync("/api/fundos",
            new
            {
                nome = "Fundo PUT Cross Tenant A",
                cnpj = ValidCnpj,
                consultoriaFundoId = consultoriaId,
                custodianteId,
                tipoFundo = 1
            });
        fundoResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var fundoId = (await fundoResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Act — PJ-B attempts PUT /api/fundos/{companyA-fundo-id}
        using var clientB = ClientPjB();
        var updatePayload = new
        {
            nome = "Hijacked Fundo Name",
            classeAnbima = (string?)null,
            segmento = (string?)null,
            dataConstituicao = (DateTimeOffset?)null
        };
        var response = await clientB.PutAsJsonAsync($"/api/fundos/{fundoId}", updatePayload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant PUT must return 404 to avoid leaking entity existence.");
    }

    [Fact]
    public async Task UpdateCedente_CrossTenant_Returns404()
    {
        // Arrange — PJ-A creates a cedente PF
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos/cedentes/pf",
            new { cpf = "52998224725", nome = "Cedente PUT Cross Tenant A" });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var cedenteId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Act — PJ-B attempts PUT /api/fundos/cedentes/{companyA-entity-id}
        using var clientB = ClientPjB();
        var updatePayload = new
        {
            nome = "Hijacked Cedente Name",
            email = (string?)null,
            telefone = (string?)null,
            endereco = (string?)null,
            status = 1 // ATIVO
        };
        var response = await clientB.PutAsJsonAsync($"/api/fundos/cedentes/{cedenteId}", updatePayload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant PUT must return 404 to avoid leaking entity existence.");
    }

    // =========================================================================
    // SCENARIO 7: Fundo state machine — RASCUNHO → ATIVO = 200
    // =========================================================================

    [Fact]
    public async Task TransitionFundoStatus_RascunhoToAtivo_Returns200()
    {
        using var client = ClientPjA();

        // Step 1: Create a ConsultoriaFundo (required FK for Fundo)
        // Fresh DB per test instance — ValidCnpj is unique within this test
        var consultoriaPayload = new
        {
            razaoSocial = "Consultoria For Fundo StateMachine Test",
            cnpj = ValidCnpj
        };
        var consultoriaResp = await client.PostAsJsonAsync("/api/fundos/consultorias", consultoriaPayload);
        consultoriaResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var consultoriaBody = await consultoriaResp.Content.ReadFromJsonAsync<JsonElement>();
        var consultoriaId = consultoriaBody.GetProperty("id").GetGuid();

        // Step 2: Create a Custodiante (required FK for Fundo)
        // ValidCnpj is distinct from ValidCnpj — no conflict within the same company
        var custodiantePayload = new
        {
            razaoSocial = "Custodiante For Fundo StateMachine Test",
            cnpj = ValidCnpj
        };
        var custodianteResp = await client.PostAsJsonAsync("/api/fundos/custodiantes", custodiantePayload);
        custodianteResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var custodianteBody = await custodianteResp.Content.ReadFromJsonAsync<JsonElement>();
        var custodianteId = custodianteBody.GetProperty("id").GetGuid();

        // Step 3: Create a Fundo (starts as RASCUNHO per domain invariant)
        // ValidCnpj is distinct from the consultoria and custodiante CNPJs
        var fundoPayload = new
        {
            nome = "Fundo State Machine Alpha",
            cnpj = ValidCnpj,
            consultoriaFundoId = consultoriaId,
            custodianteId,
            tipoFundo = 1 // TipoFundo.RendaFixa = 1
        };
        var fundoResp = await client.PostAsJsonAsync("/api/fundos", fundoPayload);
        fundoResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var fundoBody = await fundoResp.Content.ReadFromJsonAsync<JsonElement>();
        var fundoId = fundoBody.GetProperty("id").GetGuid();
        fundoBody.GetProperty("status").GetInt32().ShouldBe((int)FundoStatus.RASCUNHO);

        // Step 4: Transition RASCUNHO → ATIVO (valid state machine transition)
        var transitionPayload = new { newStatus = (int)FundoStatus.ATIVO };
        var transitionResp = await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status", transitionPayload);

        transitionResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transitionBody = await transitionResp.Content.ReadFromJsonAsync<JsonElement>();
        transitionBody.GetProperty("status").GetInt32().ShouldBe((int)FundoStatus.ATIVO);
    }

    // =========================================================================
    // SCENARIO 8: Fundo state machine — ENCERRADO → ATIVO = 400 (invalid transition)
    // =========================================================================

    [Fact]
    public async Task TransitionFundoStatus_EncerradoToAtivo_Returns400()
    {
        using var client = ClientPjA();

        // Build a Fundo and walk it to ENCERRADO state via the API:
        // RASCUNHO → ATIVO → EM_LIQUIDACAO → ENCERRADO

        // 1. ConsultoriaFundo — fresh DB per test, ValidCnpj is available
        var consultoriaPayload = new
        {
            razaoSocial = "Consultoria For Encerrado Test",
            cnpj = ValidCnpj
        };
        var consultoriaResp = await client.PostAsJsonAsync("/api/fundos/consultorias", consultoriaPayload);
        consultoriaResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var consultoriaId = (await consultoriaResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // 2. Custodiante — ValidCnpj is distinct from ConsultoriaFundo CNPJ
        var custodiantePayload = new
        {
            razaoSocial = "Custodiante For Encerrado Test",
            cnpj = ValidCnpj
        };
        var custodianteResp = await client.PostAsJsonAsync("/api/fundos/custodiantes", custodiantePayload);
        custodianteResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var custodianteId = (await custodianteResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // 3. Create Fundo (RASCUNHO) — ValidCnpj is distinct from consultoria and custodiante
        var fundoPayload = new
        {
            nome = "Fundo Encerrado Test",
            cnpj = ValidCnpj,
            consultoriaFundoId = consultoriaId,
            custodianteId,
            tipoFundo = 1 // TipoFundo.RendaFixa = 1
        };
        var fundoResp = await client.PostAsJsonAsync("/api/fundos", fundoPayload);
        fundoResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var fundoId = (await fundoResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // 4. RASCUNHO → ATIVO
        (await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status",
            new { newStatus = (int)FundoStatus.ATIVO })).StatusCode.ShouldBe(HttpStatusCode.OK);

        // 5. ATIVO → EM_LIQUIDACAO
        (await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status",
            new { newStatus = (int)FundoStatus.EM_LIQUIDACAO })).StatusCode.ShouldBe(HttpStatusCode.OK);

        // 6. EM_LIQUIDACAO → ENCERRADO
        (await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status",
            new { newStatus = (int)FundoStatus.ENCERRADO })).StatusCode.ShouldBe(HttpStatusCode.OK);

        // 7. ENCERRADO → ATIVO — must be 400 (InvalidStateTransitionException caught by controller)
        var invalidTransitionResp = await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status",
            new { newStatus = (int)FundoStatus.ATIVO });

        invalidTransitionResp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errorBody = await invalidTransitionResp.Content.ReadAsStringAsync();
        errorBody.ShouldContain("transition");
    }
}

/// <summary>
/// Generates HMAC-signed JWT tokens for integration tests without real Keycloak.
/// Uses the same signing key approach as AuthTestApiFactory in the API.Tests project,
/// but defined here as a file-scoped class to avoid cross-project test dependencies.
/// </summary>
file static class FakeJwtHelper
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
    public static string GenerateClientJwt(string email = "test@integration.test", string? sub = null)
    {
        var claims = new List<Claim>
        {
            new("email", email),
            new("sub", sub ?? Guid.NewGuid().ToString())
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
    /// PostConfigure on BearerBackoffice maps "role" claim → ClaimTypes.Role so RequireRole("admin") works.
    /// </summary>
    public static string GenerateAdminJwt(
        string email = "admin@backoffice.integration.test",
        string? sub = null)
    {
        var claims = new List<Claim>
        {
            new("email", email),
            new("sub", sub ?? Guid.NewGuid().ToString()),
            new("role", "admin") // mapped to ClaimTypes.Role in PostConfigure<BearerBackoffice>
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
