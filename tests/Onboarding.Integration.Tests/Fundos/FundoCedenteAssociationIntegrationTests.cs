using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Persistence;
using Onboarding.Integration.Tests.Fixtures;
using Shouldly;

namespace Onboarding.Integration.Tests.Fundos;

/// <summary>
/// Integration tests for FundoCedente relationship aggregate endpoints (T-4).
///
/// Tests require Docker. Run with:
///   dotnet test tests/Onboarding.Integration.Tests --filter "FullyQualifiedName~FundoCedenteAssociation"
///
/// Scenarios (T-4 DoD):
///   1.  REL-09: duplicate ATIVO same pair → 409 (domain + DB partial unique index)
///   2.  Status transition ATIVO → INATIVO → 200
///   3.  Status transition INATIVO → ATIVO → 200
///   4.  Status transition INATIVO → HISTORICO terminal → 200, then any → 400
///   5.  Audit row asserted after status transition (D-22: entityType="FundoCedente")
///   6.  Multi-tenant: PJ-B request PJ-A FundoCedente → 404
///   7.  Date window D-20: dataFim before dataInicio → 422
///   8.  Date window D-20: dataFim null (infinite) → 201 accepted
///   9.  LimiteExposicao D-18: limitePercentual > 100 → 422
///   10. LimiteExposicao D-18: both limitePercentual null + limiteValor null → 422
///   11. Unauthenticated POST → 401
///   12. GET by id returns correct data
///   13. Allowed transitions endpoint returns INATIVO when ATIVO
///   14. REL-09 race: concurrent inserts — exactly one 201, one 409
///
/// Isolation strategy (D-37 / T-1 patterns):
///   Entity pools seeded once per fixture lifetime (EnsureSeedAsync).
///   Each ATIVO-creating test consumes one cedente from _fcCedenteIds[] via _fcSlot counter
///   (Interlocked.Increment) to prevent REL-09 partial-unique-index violations.
/// </summary>
[Trait("Category", "Integration")]
public sealed class FundoCedenteAssociationIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    // =========================================================================
    // Seeded IDs — populated in InitializeAsync after EnsureSeedAsync
    // =========================================================================

    private Guid _companyAId;
    private Guid _companyBId;
    private Guid _fundoAId;
    private Guid _cedenteBaseId;       // for cross-tenant + audit tests (shared read-only use)
    private Guid _cedenteConcurrentId; // isolated to concurrent REL-09 race test only

    // FundoCedente isolation pool: 12 cedentes, one per test that creates an ATIVO association.
    // Slot counter is static so it is class-wide across all xUnit test instances of this class.
    private Guid[] _fcCedenteIds = Array.Empty<Guid>();
    private static int _fcSlot = -1;
    private Guid NextFcCedente() => _fcCedenteIds[Interlocked.Increment(ref _fcSlot) % _fcCedenteIds.Length];

    private const string SubPjA = "fca-pja-sub-001";
    private const string SubPjB = "fca-pjb-sub-002";
    private const int FcPoolSize = 14; // 14 slots — one per ATIVO-creating test + 2 buffer

    public FundoCedenteAssociationIntegrationTests(PostgreSqlFixture fixture) : base(fixture) { }

    // =========================================================================
    // IAsyncLifetime
    // =========================================================================

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await Fixture.EnsureSeedAsync(async () =>
        {
            using var scope = CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedAsync(db, scope.ServiceProvider);
        });

        using var readScope = CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();

        _companyAId = readDb.Companies.IgnoreQueryFilters()
            .Where(c => c.KeycloakUserId == SubPjA).Select(c => c.Id).First();
        _companyBId = readDb.Companies.IgnoreQueryFilters()
            .Where(c => c.KeycloakUserId == SubPjB).Select(c => c.Id).First();
        _fundoAId = readDb.Fundos.IgnoreQueryFilters()
            .Where(f => f.Nome == "FCA Fundo Alpha").Select(f => f.Id).First();
        _cedenteBaseId = readDb.Cedentes.IgnoreQueryFilters()
            .Where(c => c.Nome == "FCA Cedente Base").Select(c => c.Id).First();
        _cedenteConcurrentId = readDb.Cedentes.IgnoreQueryFilters()
            .Where(c => c.Nome == "FCA Cedente Concurrent").Select(c => c.Id).First();

        _fcCedenteIds = readDb.Cedentes.IgnoreQueryFilters()
            .Where(c => c.Nome.StartsWith("FCA FC Pool "))
            .OrderBy(c => c.Nome)
            .Select(c => c.Id)
            .ToArray();
    }

    // =========================================================================
    // Seed
    // =========================================================================

    private static async Task SeedAsync(AppDbContext db, IServiceProvider services)
    {
        var companyA = Company.Register(
            "FCA Alpha Ltda", "11222333000181",
            "fca.alpha@test.com", "+5511000000011",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.0.1"));
        companyA.SetKeycloakUserId(SubPjA);

        var companyB = Company.Register(
            "FCA Beta S.A.", "55333111000101",
            "fca.beta@test.com", "+5511000000012",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.0.2"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();

        var cnpjA = "11222333000181";
        var consultoriaA = ConsultoriaFundo.Register("FCA Consultoria Alpha", cnpjA, companyA.Id);
        var custodianteA = Custodiante.Register("FCA Custodiante Alpha", cnpjA, companyA.Id);
        var fundoA = Fundo.Register("FCA Fundo Alpha", cnpjA, companyA.Id,
            consultoriaA.Id, custodianteA.Id, TipoFundo.RendaFixa);

        await db.ConsultoriasFundo.AddAsync(consultoriaA);
        await db.Custodiantes.AddAsync(custodianteA);
        await db.Fundos.AddAsync(fundoA);
        await db.SaveChangesAsync();

        var cedenteRepo = services.GetRequiredService<ICedenteRepository>();

        // Base cedente — used for cross-tenant probe and audit tests (no ATIVO association seeded)
        var cedenteBase = Cedente.RegisterPf(GenerateCpf(9001), "FCA Cedente Base", companyA.Id);
        await cedenteRepo.AddAsync(cedenteBase);

        // Concurrent REL-09 race cedente — isolated to concurrent test only
        var cedenteConcurrent = Cedente.RegisterPf(GenerateCpf(9002), "FCA Cedente Concurrent", companyA.Id);
        await cedenteRepo.AddAsync(cedenteConcurrent);

        // Isolation pool — FcPoolSize cedentes with deterministic unique CPFs
        for (var i = 0; i < FcPoolSize; i++)
        {
            var cpf = GenerateCpf(3000 + i);
            var cedente = Cedente.RegisterPf(cpf, $"FCA FC Pool {i:D2}", companyA.Id);
            await cedenteRepo.AddAsync(cedente);
        }

        await db.SaveChangesAsync();
    }

    // =========================================================================
    // HTTP helpers
    // =========================================================================

    private HttpClient ClientPjA() => CreateClientJwt(SubPjA, "fca.pja@test.com");
    private HttpClient ClientPjB() => CreateClientJwt(SubPjB, "fca.pjb@test.com");
    private HttpClient ClientAdmin() => CreateAdminJwt("fca-admin-sub");

    // =========================================================================
    // T-4 DoD: REL-09 duplicate ATIVO same pair → 409
    // =========================================================================

    [Fact]
    public async Task CreateFundoCedente_DuplicateAtivo_Returns409()
    {
        using var client = ClientPjA();
        var dedicatedCedente = NextFcCedente();
        var payload = new
        {
            cedenteId = dedicatedCedente,
            limitePercentual = 30m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var first = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);
        first.StatusCode.ShouldBe(HttpStatusCode.Created,
            "First creation of ATIVO FundoCedente must succeed.");

        var second = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Duplicate ATIVO association for same (FundoId, CedenteId) must return 409 (REL-09, D-18).");
    }

    // =========================================================================
    // T-4 DoD: Status transitions — ATIVO ↔ INATIVO + INATIVO → HISTORICO terminal
    // =========================================================================

    [Fact]
    public async Task FundoCedente_StatusTransition_AtivoToInativo_Returns200WithInativoStatus()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", new
        {
            cedenteId = NextFcCedente(),
            limitePercentual = 40m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var transResp = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/status",
            new { newStatus = 2 }); // RelationshipStatus.INATIVO = 2

        transResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await transResp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().ShouldBe("INATIVO",
            "Status must be INATIVO after ATIVO→INATIVO transition.");
    }

    [Fact]
    public async Task FundoCedente_StatusTransition_InativoToAtivo_Returns200WithAtivoStatus()
    {
        using var client = ClientPjA();

        // Create association — ATIVO
        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", new
        {
            cedenteId = NextFcCedente(),
            limitePercentual = 25m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // ATIVO → INATIVO
        var toInativo = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/status",
            new { newStatus = 2 }); // INATIVO
        toInativo.StatusCode.ShouldBe(HttpStatusCode.OK);

        // INATIVO → ATIVO (reactivation)
        var toAtivo = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/status",
            new { newStatus = 1 }); // ATIVO

        toAtivo.StatusCode.ShouldBe(HttpStatusCode.OK,
            "INATIVO→ATIVO reactivation must succeed (not terminal direction).");
        var body = await toAtivo.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().ShouldBe("ATIVO",
            "Status must be ATIVO after INATIVO→ATIVO reactivation.");
    }

    [Fact]
    public async Task FundoCedente_StatusTransition_InativoToHistorico_Returns200_ThenHistoricoTerminalReturns400()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", new
        {
            cedenteId = NextFcCedente(),
            limitePercentual = 15m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // ATIVO → INATIVO
        var toInativo = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/status",
            new { newStatus = 2 });
        toInativo.StatusCode.ShouldBe(HttpStatusCode.OK);

        // INATIVO → HISTORICO (terminal)
        var toHistorico = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/status",
            new { newStatus = 3 }); // HISTORICO
        toHistorico.StatusCode.ShouldBe(HttpStatusCode.OK,
            "INATIVO→HISTORICO transition must succeed.");
        (await toHistorico.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString().ShouldBe("HISTORICO");

        // HISTORICO → ATIVO must fail — HISTORICO is terminal (D-22)
        var fromHistorico = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/status",
            new { newStatus = 1 }); // ATIVO
        fromHistorico.StatusCode.ShouldBe(HttpStatusCode.BadRequest,
            "Transition from terminal HISTORICO must return 400.");
    }

    // =========================================================================
    // T-4 DoD: Audit row asserted per D-22 (entityType="FundoCedente")
    // =========================================================================

    [Fact]
    public async Task FundoCedente_StatusTransition_WritesAuditRowWithCorrectEntityType()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", new
        {
            cedenteId = NextFcCedente(),
            limitePercentual = 50m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Trigger status transition — this must produce an audit log row (D-22)
        var transResp = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/status",
            new { newStatus = 2 }); // ATIVO → INATIVO
        transResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert audit row written directly via admin endpoint
        using var adminClient = ClientAdmin();
        var auditResp = await adminClient.GetAsync(
            $"/api/admin/audit-log?entityType=FundoCedente&entityId={assocId}");
        auditResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var auditBody = await auditResp.Content.ReadAsStringAsync();
        var auditResult = JsonSerializer.Deserialize<AuditPageDto>(auditBody, JsonCaseInsensitive);

        auditResult.ShouldNotBeNull();
        auditResult.TotalCount.ShouldBeGreaterThanOrEqualTo(1,
            "At least one audit row with entityType=FundoCedente must be persisted after status transition (D-22).");
        auditResult.Items.ShouldNotBeNull();
        auditResult.Items!.ShouldAllBe(r => r.EntityType == "FundoCedente",
            "Audit row entityType must be exactly 'FundoCedente'.");
        auditResult.Items!.ShouldAllBe(r => r.EntityId == assocId,
            "Audit row entityId must match the association id.");
    }

    // =========================================================================
    // T-4 DoD: Multi-tenant cross-probe — PJ-B → PJ-A FundoCedente → 404
    // =========================================================================

    [Fact]
    public async Task FundoCedente_CrossTenant_PostToOtherTenantFundo_Returns404()
    {
        using var client = ClientPjB();
        var payload = new
        {
            cedenteId = _cedenteBaseId,
            limitePercentual = 20m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "PJ-B POST to PJ-A Fundo must return 404, not 403 (no existence leak, D-5).");
    }

    [Fact]
    public async Task FundoCedente_CrossTenant_GetListFromOtherTenantFundo_Returns404()
    {
        using var client = ClientPjB();

        var response = await client.GetAsync($"/api/fundos/{_fundoAId}/cedentes");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "PJ-B GET list on PJ-A Fundo must return 404 (D-5).");
    }

    // =========================================================================
    // T-4 DoD: Date window D-20 invariants
    // =========================================================================

    [Fact]
    public async Task FundoCedente_Create_DataFimBeforeDataInicio_Returns422()
    {
        using var client = ClientPjA();
        var now = DateTimeOffset.UtcNow;
        var payload = new
        {
            cedenteId = NextFcCedente(),
            limitePercentual = 30m,
            dataInicio = now,
            dataFim = now.AddDays(-1) // dataFim < dataInicio — invalid per D-20
        };

        var response = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);

        // 422 from FluentValidation (DataFim must be after DataInicio, JanelaVigencia.Create throws)
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity,
            "DataFim before DataInicio must be rejected with 422 (D-20 half-open window invariant).");
    }

    [Fact]
    public async Task FundoCedente_Create_DataFimNull_InfiniteWindow_Returns201()
    {
        using var client = ClientPjA();
        var payload = new
        {
            cedenteId = NextFcCedente(),
            limitePercentual = 25m,
            dataInicio = DateTimeOffset.UtcNow
            // dataFim omitted — null = infinite validity per D-20
        };

        var response = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created,
            "Null dataFim (infinite window) is valid per D-20 and must produce 201.");
        var dto = await response.Content.ReadFromJsonAsync<JsonElement>();
        dto.GetProperty("dataFim").ValueKind.ShouldBe(JsonValueKind.Null,
            "Response dataFim must be null for infinite-window association.");
    }

    // =========================================================================
    // T-4 DoD: LimiteExposicao D-18 invariants
    // =========================================================================

    [Fact]
    public async Task FundoCedente_Create_LimitePercentualOver100_Returns422()
    {
        using var client = ClientPjA();
        // Percentual 101 exceeds 100 — invalid per LimiteExposicao.Create validation
        var payload = new
        {
            cedenteId = NextFcCedente(),
            limitePercentual = 101m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity,
            "LimitePercentual > 100 must be rejected with 422 (D-18 LimiteExposicao invariant).");
    }

    [Fact]
    public async Task FundoCedente_Create_BothLimitesNull_Returns422()
    {
        using var client = ClientPjA();
        // Both limitePercentual and limiteValor null — invalid per LimiteExposicao.Create
        var payload = new
        {
            cedenteId = NextFcCedente(),
            limitePercentual = (decimal?)null,
            limiteValor = (decimal?)null,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity,
            "Both LimitePercentual and LimiteValor null must be rejected with 422 (D-18).");
    }

    // =========================================================================
    // Security: unauthenticated → 401
    // =========================================================================

    [Fact]
    public async Task FundoCedente_Post_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var payload = new { cedenteId = _cedenteBaseId, limitePercentual = 10m, dataInicio = DateTimeOffset.UtcNow };

        var response = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized,
            "Unauthenticated POST to FundoCedente endpoint must return 401.");
    }

    [Fact]
    public async Task FundoCedente_Get_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync($"/api/fundos/{_fundoAId}/cedentes");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // GET by id returns correct data (round-trip assertion)
    // =========================================================================

    [Fact]
    public async Task FundoCedente_GetById_ReturnsCorrectAssociation()
    {
        using var client = ClientPjA();
        var cedenteId = NextFcCedente();
        var dataInicio = DateTimeOffset.UtcNow;

        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", new
        {
            cedenteId,
            limiteValor = 75_000m,
            dataInicio
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var getResp = await client.GetAsync($"/api/fundos/{_fundoAId}/cedentes/{assocId}");
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dto = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        dto.GetProperty("id").GetGuid().ShouldBe(assocId);
        dto.GetProperty("cedenteId").GetGuid().ShouldBe(cedenteId);
        dto.GetProperty("status").GetString().ShouldBe("ATIVO");
        dto.GetProperty("limiteValor").GetDecimal().ShouldBe(75_000m);
    }

    // =========================================================================
    // Allowed transitions endpoint (D-25)
    // =========================================================================

    [Fact]
    public async Task FundoCedente_AllowedTransitions_WhenAtivo_ContainsInativoAndHistorico()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", new
        {
            cedenteId = NextFcCedente(),
            limitePercentual = 10m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var transitionsResp = await client.GetAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/allowed-transitions");
        transitionsResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var transitions = await transitionsResp.Content.ReadFromJsonAsync<string[]>();
        transitions.ShouldNotBeNull();
        transitions.ShouldContain("INATIVO",
            "ATIVO association must allow INATIVO transition (D-22).");
        transitions.ShouldContain("HISTORICO",
            "ATIVO association must allow HISTORICO transition (D-22).");
        transitions.ShouldNotContain("ATIVO",
            "ATIVO → ATIVO transition is invalid (same state).");
    }

    // =========================================================================
    // REL-09 race condition — concurrent inserts, only one succeeds (D-18)
    // =========================================================================

    [Fact]
    public async Task FundoCedente_ConcurrentCreate_SamePair_OnlyOneSucceeds()
    {
        // _cedenteConcurrentId is seeded and exclusively reserved for this test.
        // Two concurrent POSTs targeting the same (FundoId, CedenteId) pair.
        // Both may pass the in-memory ActivateGuard when they both see no ATIVO row.
        // The DB partial unique index (D-18) rejects the second insert with 409.
        var payload = new
        {
            cedenteId = _cedenteConcurrentId,
            limitePercentual = 10m,
            dataInicio = DateTimeOffset.UtcNow
        };

        using var client1 = ClientPjA();
        using var client2 = ClientPjA();

        var t1 = client1.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);
        var t2 = client2.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);

        var results = await Task.WhenAll(t1, t2);
        var statuses = results.Select(r => (int)r.StatusCode).OrderBy(s => s).ToArray();

        statuses.ShouldContain(201,
            "One concurrent request must succeed and create the ATIVO FundoCedente association.");
        statuses.ShouldContain(409,
            "The other concurrent request must be rejected with 409 — DB partial unique index enforces REL-09 (D-18).");
    }

    // =========================================================================
    // JSON DTO types — self-contained, no cross-project coupling
    // =========================================================================

    private static readonly JsonSerializerOptions JsonCaseInsensitive =
        new() { PropertyNameCaseInsensitive = true };

    private sealed class AuditPageDto
    {
        public AuditItemDto[]? Items { get; set; }
        public int TotalCount { get; set; }
    }

    private sealed class AuditItemDto
    {
        public Guid Id { get; set; }
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public string? ActionType { get; set; }
    }
}
