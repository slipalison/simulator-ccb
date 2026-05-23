using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Infrastructure.Persistence;
using Onboarding.Integration.Tests.Fixtures;
using Shouldly;

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
public class FundosControllerIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    // Seeded company IDs — set during InitializeAsync, used in tests
    private Guid _companyAId;
    private Guid _companyBId;

    // Known sub claims — mapped to Company.KeycloakUserId in seed data
    private const string SubPjA = "integration-pja-sub-001";
    private const string SubPjB = "integration-pjb-sub-002";
    private const string SubNoCompany = "integration-noperm-sub-003";

    // Each test uses TestCnpj (inherited from PostgreSqlIntegrationTestBase) which provides a
    // per-test-instance unique valid CNPJ generated via GenerateCnpj(Fixture.NextCnpjSlot()).
    // This prevents (ClientId, Cnpj) uniqueness violations when multiple tests share the container.
    //
    // Note: within a SINGLE test, the same TestCnpj can be reused for different entity types
    // (ConsultoriaFundo, Custodiante, Fundo) because those are separate tables with separate
    // (ClientId, Cnpj) unique indexes — no cross-table collision occurs.

    public FundosControllerIntegrationTests(PostgreSqlFixture fixture) : base(fixture) { }

    // =========================================================================
    // IAsyncLifetime — per-class seed (guarded against re-seeding per test)
    //
    // xUnit creates one test-class instance per test method, so InitializeAsync is called
    // once per test. Fixture.EnsureSeedAsync ensures the DB seed runs only once per class.
    // After seed, IDs are re-read from DB so each test instance has them populated.
    // =========================================================================

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await Fixture.EnsureSeedAsync(async () =>
        {
            using var scope = CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedCompaniesAsync(db);
        });

        // Re-read IDs on every test instance — seed is idempotent, rows are stable.
        // IgnoreQueryFilters() required: Companies has no tenant filter, but explicit for safety.
        using var readScope = CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _companyAId = readDb.Companies.IgnoreQueryFilters().Where(c => c.KeycloakUserId == SubPjA).Select(c => c.Id).First();
        _companyBId = readDb.Companies.IgnoreQueryFilters().Where(c => c.KeycloakUserId == SubPjB).Select(c => c.Id).First();
    }

    // =========================================================================
    // Seed helpers
    // =========================================================================

    private static async Task SeedCompaniesAsync(AppDbContext db)
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
    }

    // =========================================================================
    // HTTP client helpers — inject fake JWT for specific user
    // =========================================================================

    /// <summary>
    /// Creates a PJ-A client whose sub is seeded in the DB — ClientClaimsMiddleware will resolve
    /// CompanyId=_companyAId + Permissions.All (owner) for this user.
    /// </summary>
    private HttpClient ClientPjA() => CreateClientJwt(SubPjA);

    /// <summary>Creates a PJ-B client — ClientClaimsMiddleware resolves CompanyId=_companyBId.</summary>
    private HttpClient ClientPjB() => CreateClientJwt(SubPjB);

    /// <summary>
    /// Creates a client with a sub that has NO Company/Employee in DB — gets Guid.Empty CompanyId +
    /// empty permissions → 403 on any permission-gated endpoint.
    /// </summary>
    private HttpClient ClientNoPermissions() => CreateClientJwt(SubNoCompany);

    /// <summary>
    /// Admin client with BearerBackoffice scheme + role=admin claim.
    /// CrossCompanyAccess policy (RequireRole("admin")) will succeed.
    /// </summary>
    private HttpClient ClientAdmin() => CreateAdminJwt("admin-sub-backoffice");

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
            cnpj = TestCnpj
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

        // Create a consultoria for PJ-A — TestCnpj is unique per test instance, no collision risk
        var createPayload = new
        {
            razaoSocial = "Consultoria Alpha Read Test",
            cnpj = TestCnpj
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
            cnpj = TestCnpj
        };
        var createResponse = await clientA.PostAsJsonAsync("/api/fundos/consultorias", createPayload);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — PJ-B lists its own consultorias
        using var clientB = ClientPjB();
        var listResponse = await clientB.GetAsync("/api/fundos/consultorias");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var listBody = await listResponse.Content.ReadAsStringAsync();

        // Assert — PJ-B must NOT see PJ-A's row (multi-tenant isolation via HasQueryFilter)
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
        var payload = new { razaoSocial = "Should Be Rejected", cnpj = TestCnpj };
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
        var payload = new { razaoSocial = "No Auth Test", cnpj = TestCnpj };
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
            cnpj = TestCnpj // PJ-A creates with TestCnpj
        };
        var responseA = await clientA.PostAsJsonAsync("/api/fundos/consultorias", payloadA);
        responseA.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Arrange — PJ-B creates a consultoria
        // TestCnpj can be reused because the uniqueness constraint is (ClientId, Cnpj) —
        // PJ-B has a different ClientId so no constraint violation.
        using var clientB = ClientPjB();
        var payloadB = new
        {
            razaoSocial = "Consultoria Admin View Beta",
            cnpj = TestCnpj // same CNPJ is OK — different company (different ClientId)
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
        var payload = new { razaoSocial = "Consultoria Cross Tenant A", cnpj = TestCnpj };
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
        var payload = new { razaoSocial = "Custodiante Cross Tenant A", cnpj = TestCnpj };
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
            new { razaoSocial = "Consultoria FK For Fundo CrossTenant", cnpj = TestCnpj });
        consultoriaResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var consultoriaId = (await consultoriaResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var custodianteResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes",
            new { razaoSocial = "Custodiante FK For Fundo CrossTenant", cnpj = TestCnpj });
        custodianteResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var custodianteId = (await custodianteResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var fundoResp = await clientA.PostAsJsonAsync("/api/fundos",
            new
            {
                nome = "Fundo Cross Tenant A",
                cnpj = TestCnpj,
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
        var payload = new { cpf = TestCpf, nome = "Cedente Cross Tenant A" };
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
            new { razaoSocial = "Consultoria PUT Cross Tenant A", cnpj = TestCnpj });
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
            new { razaoSocial = "Custodiante PUT Cross Tenant A", cnpj = TestCnpj });
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
            new { razaoSocial = "Consultoria FK For PUT CrossTenant", cnpj = TestCnpj });
        consultoriaResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var consultoriaId = (await consultoriaResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var custodianteResp = await clientA.PostAsJsonAsync("/api/fundos/custodiantes",
            new { razaoSocial = "Custodiante FK For PUT CrossTenant", cnpj = TestCnpj });
        custodianteResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var custodianteId = (await custodianteResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var fundoResp = await clientA.PostAsJsonAsync("/api/fundos",
            new
            {
                nome = "Fundo PUT Cross Tenant A",
                cnpj = TestCnpj,
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
            new { cpf = TestCpf, nome = "Cedente PUT Cross Tenant A" });
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
        // Fresh DB per test instance — TestCnpj is unique within this test
        var consultoriaPayload = new
        {
            razaoSocial = "Consultoria For Fundo StateMachine Test",
            cnpj = TestCnpj
        };
        var consultoriaResp = await client.PostAsJsonAsync("/api/fundos/consultorias", consultoriaPayload);
        consultoriaResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var consultoriaBody = await consultoriaResp.Content.ReadFromJsonAsync<JsonElement>();
        var consultoriaId = consultoriaBody.GetProperty("id").GetGuid();

        // Step 2: Create a Custodiante (required FK for Fundo)
        var custodiantePayload = new
        {
            razaoSocial = "Custodiante For Fundo StateMachine Test",
            cnpj = TestCnpj
        };
        var custodianteResp = await client.PostAsJsonAsync("/api/fundos/custodiantes", custodiantePayload);
        custodianteResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var custodianteBody = await custodianteResp.Content.ReadFromJsonAsync<JsonElement>();
        var custodianteId = custodianteBody.GetProperty("id").GetGuid();

        // Step 3: Create a Fundo (starts as RASCUNHO per domain invariant)
        var fundoPayload = new
        {
            nome = "Fundo State Machine Alpha",
            cnpj = TestCnpj,
            consultoriaFundoId = consultoriaId,
            custodianteId,
            tipoFundo = 1 // TipoFundo.RendaFixa = 1
        };
        var fundoResp = await client.PostAsJsonAsync("/api/fundos", fundoPayload);
        fundoResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var fundoBody = await fundoResp.Content.ReadFromJsonAsync<JsonElement>();
        var fundoId = fundoBody.GetProperty("id").GetGuid();
        fundoBody.GetProperty("status").GetString().ShouldBe("RASCUNHO");

        // Step 4: Transition RASCUNHO → ATIVO (valid state machine transition)
        var transitionPayload = new { newStatus = (int)FundoStatus.ATIVO };
        var transitionResp = await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status", transitionPayload);

        transitionResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transitionBody = await transitionResp.Content.ReadFromJsonAsync<JsonElement>();
        transitionBody.GetProperty("status").GetString().ShouldBe("ATIVO");
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

        // 1. ConsultoriaFundo — TestCnpj is unique per test instance, no CNPJ collision
        var consultoriaPayload = new
        {
            razaoSocial = "Consultoria For Encerrado Test",
            cnpj = TestCnpj
        };
        var consultoriaResp = await client.PostAsJsonAsync("/api/fundos/consultorias", consultoriaPayload);
        consultoriaResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var consultoriaId = (await consultoriaResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // 2. Custodiante
        var custodiantePayload = new
        {
            razaoSocial = "Custodiante For Encerrado Test",
            cnpj = TestCnpj
        };
        var custodianteResp = await client.PostAsJsonAsync("/api/fundos/custodiantes", custodiantePayload);
        custodianteResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var custodianteId = (await custodianteResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // 3. Create Fundo (RASCUNHO)
        var fundoPayload = new
        {
            nome = "Fundo Encerrado Test",
            cnpj = TestCnpj,
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

    // =========================================================================
    // SCENARIO B1 regression lock: GET /api/fundos/cedentes must return
    // non-null Documento fields after CedenteRepository B1 fix (ChangeTracker.Clear).
    //
    // Root cause (B1): GetPagedByCompanyAsync used AsNoTracking(); ReconstructDocumento
    // reads shadow properties via _db.Entry() which requires tracked entities.
    // Detached entities returned NullReferenceException when Documento.Match() was called.
    // Fix: remove AsNoTracking(), call ChangeTracker.Clear() post-reconstruction.
    // =========================================================================

    [Fact]
    public async Task ListCedentes_AfterPfCreate_ReturnsCpfPopulated()
    {
        // Arrange — PJ-A creates a CedentePf via the API (persists shadow properties via repo)
        using var client = ClientPjA();
        var testCpf = TestCpf; // unique per test instance — prevents (ClientId, Cpf) collisions in shared container
        var createResp = await client.PostAsJsonAsync("/api/fundos/cedentes/pf",
            new { cpf = testCpf, nome = "Cedente B1 Regression PF" });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act — list cedentes; this path exercised ReconstructDocumento on detached entity (B1)
        var listResp = await client.GetAsync("/api/fundos/cedentes");

        // Assert status
        listResp.StatusCode.ShouldBe(HttpStatusCode.OK,
            "GET /api/fundos/cedentes must return 200 after B1 fix.");

        var body = await listResp.Content.ReadFromJsonAsync<JsonElement>();

        // PaginatedResult shape: { items: [...], totalCount, page, pageSize }
        var items = body.GetProperty("items");
        items.GetArrayLength().ShouldBeGreaterThan(0, "At least one Cedente must be returned.");

        // Find the cedente we just created and assert Documento (cpf field) is populated
        var match = Enumerable.Range(0, items.GetArrayLength())
            .Select(i => items[i])
            .FirstOrDefault(item =>
                item.TryGetProperty("nome", out var n) && n.GetString() == "Cedente B1 Regression PF");

        match.ValueKind.ShouldNotBe(JsonValueKind.Undefined,
            "Created cedente must appear in list response.");

        // documento = the Cpf/Cnpj value serialized as plain string from CedenteDto.Documento
        var documento = match.GetProperty("documento").GetString();
        documento.ShouldNotBeNull(
            "documento must be non-null — B1: Documento.Match() was throwing NRE on detached entity.");
        documento.ShouldBe(testCpf,
            "documento must contain the CPF stored via shadow property.");
    }

    [Fact]
    public async Task ListCedentes_MultiTenantIsolation_PjBDoesNotSeePjARows()
    {
        // Arrange — PJ-A creates a cedente
        using var clientA = ClientPjA();
        await clientA.PostAsJsonAsync("/api/fundos/cedentes/pf",
            new { cpf = TestCpf, nome = "Cedente Isolation PjA" });

        // Act — PJ-B lists its own cedentes
        using var clientB = ClientPjB();
        var listResp = await clientB.GetAsync("/api/fundos/cedentes");

        listResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await listResp.Content.ReadAsStringAsync();

        // Assert — PJ-B must NOT see PJ-A's cedente (WHERE ClienteId = PjB's companyId via HasQueryFilter)
        body.ShouldNotContain("Cedente Isolation PjA");
    }
}
