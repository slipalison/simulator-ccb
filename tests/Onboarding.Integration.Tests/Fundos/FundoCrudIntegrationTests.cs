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

namespace Onboarding.Integration.Tests.Fundos;

/// <summary>
/// Integration tests for Fundo CRUD + full state-machine transitions + audit row assertions.
///
/// DoD T-2 coverage:
///   - POST /api/fundos → 201 (RASCUNHO)
///   - GET /api/fundos/{id} → 200
///   - GET /api/fundos → 200 paginated
///   - PUT /api/fundos/{id} → 200
///   - State machine: RASCUNHO→ATIVO→SUSPENSO→ATIVO→EM_LIQUIDACAO→ENCERRADO
///   - State machine: ENCERRADO→ATIVO → 400 (invalid terminal transition)
///   - State machine: ATIVO→SUSPENSO each step → AdminAuditLog row inserted with entityType="Fundo"+ActorSub
///   - Multi-tenant: PJ-B request Fundo of PJ-A → 404
///   - Validation: missing required field → 422 ProblemDetails errors[]
///   - Duplicate CNPJ within same company → 409
///
/// Requires Docker (Testcontainers PostgreSQL). Tag: [Category=Integration]
/// </summary>
[Trait("Category", "Integration")]
public sealed class FundoCrudIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    private Guid _companyAId;
    private Guid _companyBId;
    private Guid _consultoriaAId;
    private Guid _custodianteAId;

    private const string SubPjA = "fundo-crud-pja-sub-001";
    private const string SubPjB = "fundo-crud-pjb-sub-002";

    public FundoCrudIntegrationTests(PostgreSqlFixture fixture) : base(fixture) { }

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
            await SeedAsync(db);
        });

        // Re-read seeded IDs on every test instance — seed is idempotent.
        using var readScope = CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _companyAId = readDb.Companies.IgnoreQueryFilters()
            .Where(c => c.KeycloakUserId == SubPjA).Select(c => c.Id).First();
        _companyBId = readDb.Companies.IgnoreQueryFilters()
            .Where(c => c.KeycloakUserId == SubPjB).Select(c => c.Id).First();
        _consultoriaAId = readDb.ConsultoriasFundo.IgnoreQueryFilters()
            .Where(c => c.RazaoSocial == "Consultoria Fundo CRUD Alpha").Select(c => c.Id).First();
        _custodianteAId = readDb.Custodiantes.IgnoreQueryFilters()
            .Where(c => c.RazaoSocial == "Custodiante Fundo CRUD Alpha").Select(c => c.Id).First();
    }

    private static async Task SeedAsync(AppDbContext db)
    {
        var companyA = Company.Register(
            "Alpha Fundo CRUD Ltda", "11444777000161",
            "alpha.fundo.crud@test.com", "+5511000000010",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "192.168.1.30"));
        companyA.SetKeycloakUserId(SubPjA);

        var companyB = Company.Register(
            "Beta Fundo CRUD S.A.", "62232889000190",
            "beta.fundo.crud@test.com", "+5511000000011",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "192.168.1.31"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();

        // Prerequisite entities for Fundo creation: ConsultoriaFundo + Custodiante
        // Same CNPJ as companyA is OK — different tables, separate unique indexes
        var consultoria = ConsultoriaFundo.Register(
            "Consultoria Fundo CRUD Alpha", "11444777000161", companyA.Id);
        var custodiante = Custodiante.Register(
            "Custodiante Fundo CRUD Alpha", "11444777000161", companyA.Id);

        await db.ConsultoriasFundo.AddAsync(consultoria);
        await db.Custodiantes.AddAsync(custodiante);
        await db.SaveChangesAsync();
    }

    // =========================================================================
    // HTTP client helpers
    // =========================================================================

    private HttpClient ClientPjA() => CreateClientJwt(SubPjA);
    private HttpClient ClientPjB() => CreateClientJwt(SubPjB);

    // =========================================================================
    // SCENARIO: POST /api/fundos → 201 (happy path)
    // =========================================================================

    [Fact]
    public async Task RegisterFundo_HappyPath_Returns201WithRascunho()
    {
        using var client = ClientPjA();
        var response = await client.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo Happy {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1  // TipoFundo.RendaFixa
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().ShouldBe("RASCUNHO");
        body.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
    }

    // =========================================================================
    // SCENARIO: GET /api/fundos/{id} → 200
    // =========================================================================

    [Fact]
    public async Task GetFundoById_OwnedByPjA_Returns200()
    {
        using var client = ClientPjA();

        // Create first
        var createResp = await client.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo Get {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var fundoId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Get by id
        var getResp = await client.GetAsync($"/api/fundos/{fundoId}");
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().ShouldBe(fundoId);
        body.GetProperty("status").GetString().ShouldBe("RASCUNHO");
    }

    // =========================================================================
    // SCENARIO: GET /api/fundos → 200 paginated list
    // =========================================================================

    [Fact]
    public async Task ListFundos_PjA_Returns200Paginated()
    {
        using var client = ClientPjA();

        // Create a fundo to ensure there is at least one row for PJ-A
        var createResp = await client.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo List {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 2  // TipoFundo.Multimercado
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // List
        var listResp = await client.GetAsync("/api/fundos?page=1&pageSize=20");
        listResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("items", out _).ShouldBeTrue("Response must have 'items' property (paginated result).");
        body.TryGetProperty("totalCount", out var totalCount).ShouldBeTrue();
        totalCount.GetInt32().ShouldBeGreaterThanOrEqualTo(1);
    }

    // =========================================================================
    // SCENARIO: PUT /api/fundos/{id} → 200
    // =========================================================================

    [Fact]
    public async Task UpdateFundo_HappyPath_Returns200WithUpdatedName()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo Update Before {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var fundoId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var updateResp = await client.PutAsJsonAsync($"/api/fundos/{fundoId}", new
        {
            nome = $"Fundo Update After {TestId}",
            classeAnbima = (string?)null,
            segmento = (string?)null,
            dataConstituicao = (DateTimeOffset?)null
        });

        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await updateResp.Content.ReadFromJsonAsync<JsonElement>();
        (body.GetProperty("nome").GetString() ?? "").ShouldContain("After");
    }

    // =========================================================================
    // SCENARIO: State machine full path RASCUNHO→ATIVO→SUSPENSO→ATIVO→EM_LIQUIDACAO→ENCERRADO
    // =========================================================================

    [Fact]
    public async Task StateMachine_FullValidPath_EachTransitionReturns200()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo StateMachine {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var fundoId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // RASCUNHO → ATIVO
        var r1 = await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status",
            new { newStatus = (int)FundoStatus.ATIVO });
        r1.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await r1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().ShouldBe("ATIVO");

        // ATIVO → SUSPENSO
        var r2 = await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status",
            new { newStatus = (int)FundoStatus.SUSPENSO });
        r2.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await r2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().ShouldBe("SUSPENSO");

        // SUSPENSO → ATIVO
        var r3 = await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status",
            new { newStatus = (int)FundoStatus.ATIVO });
        r3.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await r3.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().ShouldBe("ATIVO");

        // ATIVO → EM_LIQUIDACAO
        var r4 = await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status",
            new { newStatus = (int)FundoStatus.EM_LIQUIDACAO });
        r4.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await r4.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().ShouldBe("EM_LIQUIDACAO");

        // EM_LIQUIDACAO → ENCERRADO
        var r5 = await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status",
            new { newStatus = (int)FundoStatus.ENCERRADO });
        r5.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await r5.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().ShouldBe("ENCERRADO");
    }

    // =========================================================================
    // SCENARIO: Invalid state transition → 400
    // =========================================================================

    [Fact]
    public async Task StateMachine_InvalidTransitionEncerradoToAtivo_Returns400()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo InvalidTransition {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });
        var fundoId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Walk to ENCERRADO
        await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status", new { newStatus = (int)FundoStatus.ATIVO });
        await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status", new { newStatus = (int)FundoStatus.EM_LIQUIDACAO });
        await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status", new { newStatus = (int)FundoStatus.ENCERRADO });

        // ENCERRADO → ATIVO must fail
        var invalidResp = await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status",
            new { newStatus = (int)FundoStatus.ATIVO });

        invalidResp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errorBody = await invalidResp.Content.ReadAsStringAsync();
        errorBody.ShouldContain("transition");
    }

    // =========================================================================
    // SCENARIO: Invalid transition RASCUNHO→ENCERRADO (non-adjacent) → 400
    // =========================================================================

    [Fact]
    public async Task StateMachine_InvalidTransitionRascunhoToEncerrado_Returns400()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo InvalidRascunhoToEncerrado {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });
        var fundoId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resp = await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status",
            new { newStatus = (int)FundoStatus.ENCERRADO });

        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // SCENARIO: AdminAuditLog row inserted after state machine transition
    //   entityType="Fundo", entityId=fundoId, ActorSub matches JWT sub
    // =========================================================================

    [Fact]
    public async Task StateMachine_Transition_InsertsAuditLogRowWithCorrectEntityType()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo AuditLog {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var fundoId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Perform RASCUNHO → ATIVO transition
        var transResp = await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status",
            new { newStatus = (int)FundoStatus.ATIVO });
        transResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert audit log row via DB — IgnoreQueryFilters not needed: AdminAuditLogs has no tenant filter
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditRow = await db.AdminAuditLogs
            .Where(a => a.EntityType == "Fundo" && a.EntityId == fundoId)
            .FirstOrDefaultAsync();

        auditRow.ShouldNotBeNull("AdminAuditLog row must be inserted on state machine transition.");
        auditRow.EntityType.ShouldBe("Fundo");
        auditRow.EntityId.ShouldBe(fundoId);

        // AuditService.RecordAsync stores actorEmail as AdminUserName (see AuditService.cs line 30).
        // The test JWT is generated with email="test@integration.test" by CreateClientJwt default.
        auditRow.AdminUserName.ShouldBe("test@integration.test",
            "ActorEmail from JWT must be stored as AdminUserName in the audit log (AuditService pattern).");
    }

    // =========================================================================
    // SCENARIO: Multiple transitions → multiple audit rows
    // =========================================================================

    [Fact]
    public async Task StateMachine_MultipleTransitions_InsertsAuditRowPerTransition()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo MultiAudit {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });
        var fundoId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Two valid transitions
        await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status", new { newStatus = (int)FundoStatus.ATIVO });
        await client.PostAsJsonAsync($"/api/fundos/{fundoId}/status", new { newStatus = (int)FundoStatus.SUSPENSO });

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditCount = await db.AdminAuditLogs
            .Where(a => a.EntityType == "Fundo" && a.EntityId == fundoId)
            .CountAsync();

        auditCount.ShouldBe(2, "One audit row must be inserted per status transition.");
    }

    // =========================================================================
    // SCENARIO: GET /api/fundos/{id} cross-tenant → 404
    // =========================================================================

    [Fact]
    public async Task GetFundoById_CrossTenant_Returns404()
    {
        // PJ-A creates a fundo
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo CrossTenant Get {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var fundoId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // PJ-B attempts to read PJ-A's fundo
        using var clientB = ClientPjB();
        var resp = await clientB.GetAsync($"/api/fundos/{fundoId}");

        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant GET-by-id must return 404, not 200 or 403, to avoid leaking entity existence.");
    }

    // =========================================================================
    // SCENARIO: PUT /api/fundos/{id} cross-tenant → 404
    // =========================================================================

    [Fact]
    public async Task UpdateFundo_CrossTenant_Returns404()
    {
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo CrossTenant Put {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });
        var fundoId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var clientB = ClientPjB();
        var resp = await clientB.PutAsJsonAsync($"/api/fundos/{fundoId}", new
        {
            nome = "Hijacked Fundo",
            classeAnbima = (string?)null,
            segmento = (string?)null,
            dataConstituicao = (DateTimeOffset?)null
        });

        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant PUT must return 404, not 200 or 403.");
    }

    // =========================================================================
    // SCENARIO: State machine cross-tenant → 404
    // =========================================================================

    [Fact]
    public async Task StateMachine_CrossTenantTransition_Returns404()
    {
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo CrossTenant Status {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });
        var fundoId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var clientB = ClientPjB();
        var resp = await clientB.PostAsJsonAsync($"/api/fundos/{fundoId}/status",
            new { newStatus = (int)FundoStatus.ATIVO });

        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant status transition must return 404 to avoid leaking entity existence.");
    }

    // =========================================================================
    // SCENARIO: POST /api/fundos with missing required field → 422
    // =========================================================================

    [Fact]
    public async Task RegisterFundo_MissingNome_Returns422WithProblemDetails()
    {
        using var client = ClientPjA();
        var response = await client.PostAsJsonAsync("/api/fundos", new
        {
            // nome intentionally missing
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity,
            "Missing required 'nome' field must return 422 UnprocessableEntity.");
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("error", Case.Insensitive,
            "Response must include validation errors in ProblemDetails shape.");
    }

    // =========================================================================
    // SCENARIO: Duplicate CNPJ within same company → 409
    // =========================================================================

    [Fact]
    public async Task RegisterFundo_DuplicateCnpj_Returns409()
    {
        using var client = ClientPjA();
        var sharedCnpj = TestCnpj;

        // First create
        var first = await client.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo Dup First {TestId}",
            cnpj = sharedCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Second create same CNPJ same company
        var second = await client.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo Dup Second {TestId}",
            cnpj = sharedCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 2
        });
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Duplicate CNPJ within same company must return 409 (D-10 composite unique index).");
    }

    // =========================================================================
    // SCENARIO: No auth → 401
    // =========================================================================

    [Fact]
    public async Task RegisterFundo_NoAuth_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/fundos", new
        {
            nome = "No Auth Fundo",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFundoList_NoAuth_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/fundos");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // SCENARIO: Multi-tenant list isolation — PJ-B cannot see PJ-A's fundos
    // =========================================================================

    [Fact]
    public async Task ListFundos_PjB_DoesNotSeePjAFundos()
    {
        using var clientA = ClientPjA();
        var createResp = await clientA.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo Isolation {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        using var clientB = ClientPjB();
        var listResp = await clientB.GetAsync("/api/fundos");
        listResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await listResp.Content.ReadAsStringAsync();
        // PJ-B must not see PJ-A's fundos via HasQueryFilter multi-tenant isolation
        body.ShouldNotContain($"Fundo Isolation {TestId}");
    }

    // =========================================================================
    // SCENARIO: GET /api/fundos/{id}/allowed-transitions → 200 with array (D-25)
    // =========================================================================

    [Fact]
    public async Task GetAllowedTransitions_FundoInRascunho_ReturnsAtivoOnly()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync("/api/fundos", new
        {
            nome = $"Fundo AllowedTransitions {TestId}",
            cnpj = TestCnpj,
            consultoriaFundoId = _consultoriaAId,
            custodianteId = _custodianteAId,
            tipoFundo = 1
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var fundoId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resp = await client.GetAsync($"/api/fundos/{fundoId}/allowed-transitions");
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transitions = await resp.Content.ReadFromJsonAsync<string[]>();
        transitions.ShouldNotBeNull();
        transitions.ShouldContain("ATIVO", "RASCUNHO can only transition to ATIVO per D-02.");
        transitions.Length.ShouldBe(1, "RASCUNHO has exactly one allowed next state.");
    }
}
