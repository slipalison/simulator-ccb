using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
/// Integration tests for FundoTipoAtivo relationship aggregate endpoints (T-4).
///
/// Tests require Docker. Run with:
///   dotnet test tests/Onboarding.Integration.Tests --filter "FullyQualifiedName~FundoTipoAtivoAssociation"
///
/// Scenarios (T-4 DoD):
///   1.  Duplicate ATIVO same pair → 409 (REL-09 analog for FundoTipoAtivo)
///   2.  Status transition ATIVO → INATIVO → 200
///   3.  Status transition INATIVO → ATIVO reactivation → 200
///   4.  Status transition any → HISTORICO terminal + HISTORICO → any → 400
///   5.  Audit row per D-22 (entityType="FundoTipoAtivo")
///   6.  Multi-tenant: PJ-B request PJ-A FundoTipoAtivo → 404
///   7.  Date window D-20: dataFim before dataInicio → 422
///   8.  Date window D-20: dataFim null accepted → 201
///   9.  LimiteExposicao D-18: percentual > 100 → 422
///   10. LimiteExposicao D-18: both limites null → 422
///   11. Unauthenticated POST → 401
///   12. GET by id round-trip
///   13. Allowed transitions when INATIVO contains ATIVO + HISTORICO
///
/// Isolation strategy (D-37):
///   TipoAtivo pool seeded once per fixture lifetime (EnsureSeedAsync).
///   Each ATIVO-creating test consumes one slot via _ftaSlot counter (Interlocked.Increment)
///   to prevent (FundoId, TipoAtivoId) partial-unique-index violations.
/// </summary>
[Trait("Category", "Integration")]
public sealed class FundoTipoAtivoAssociationIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    // =========================================================================
    // Seeded IDs
    // =========================================================================

    private Guid _companyAId;
    private Guid _companyBId;
    private Guid _fundoAId;   // owned by CompanyA
    private Guid _fundoBId;   // owned by CompanyB (for cross-tenant tests)

    // TipoAtivo isolation pool: pool of TipoAtivos, one per ATIVO-creating test.
    private Guid[] _ftaTipoAtivoIds = Array.Empty<Guid>();
    private static int _ftaSlot = -1;
    private Guid NextFtaTipoAtivo() => _ftaTipoAtivoIds[Interlocked.Increment(ref _ftaSlot) % _ftaTipoAtivoIds.Length];

    private const string SubPjA = "fta-pja-sub-001";
    private const string SubPjB = "fta-pjb-sub-002";
    private const int FtaPoolSize = 16; // 16 slots — one per ATIVO-creating test + 2 buffer

    public FundoTipoAtivoAssociationIntegrationTests(PostgreSqlFixture fixture) : base(fixture) { }

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
            .Where(f => f.Nome == "FTA Fundo Alpha").Select(f => f.Id).First();
        _fundoBId = readDb.Fundos.IgnoreQueryFilters()
            .Where(f => f.Nome == "FTA Fundo Beta").Select(f => f.Id).First();

        _ftaTipoAtivoIds = readDb.TiposAtivo
            .Where(t => t.Codigo.StartsWith("FTA-POOL-"))
            .OrderBy(t => t.Codigo)
            .Select(t => t.Id)
            .ToArray();
    }

    // =========================================================================
    // Seed
    // =========================================================================

    private static async Task SeedAsync(AppDbContext db, IServiceProvider services)
    {
        var companyA = Company.Register(
            "FTA Alpha Ltda", "88666444000101",
            "fta.alpha@test.com", "+5511000000031",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.2.1"));
        companyA.SetKeycloakUserId(SubPjA);

        var companyB = Company.Register(
            "FTA Beta S.A.", "99777555000101",
            "fta.beta@test.com", "+5511000000032",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.2.2"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();

        var cnpjA = "88666444000101";
        var cnpjB = "99777555000101";

        var consultoriaA = ConsultoriaFundo.Register("FTA Consultoria Alpha", cnpjA, companyA.Id);
        var custodianteA = Custodiante.Register("FTA Custodiante Alpha", cnpjA, companyA.Id);
        var fundoA = Fundo.Register("FTA Fundo Alpha", cnpjA, companyA.Id,
            consultoriaA.Id, custodianteA.Id, TipoFundo.RendaFixa);

        var consultoriaB = ConsultoriaFundo.Register("FTA Consultoria Beta", cnpjB, companyB.Id);
        var custodianteB = Custodiante.Register("FTA Custodiante Beta", cnpjB, companyB.Id);
        var fundoB = Fundo.Register("FTA Fundo Beta", cnpjB, companyB.Id,
            consultoriaB.Id, custodianteB.Id, TipoFundo.Multimercado);

        await db.ConsultoriasFundo.AddRangeAsync(consultoriaA, consultoriaB);
        await db.Custodiantes.AddRangeAsync(custodianteA, custodianteB);
        await db.Fundos.AddRangeAsync(fundoA, fundoB);

        // TipoAtivo isolation pool (global entity — no ClientId)
        for (var i = 0; i < FtaPoolSize; i++)
        {
            await db.TiposAtivo.AddAsync(
                TipoAtivo.Register($"FTA-POOL-{i:D2}", $"FTA Pool TipoAtivo {i}", TipoAtivoCategoria.RendaFixa));
        }

        await db.SaveChangesAsync();
    }

    // =========================================================================
    // HTTP helpers
    // =========================================================================

    private HttpClient ClientPjA() => CreateClientJwt(SubPjA, "fta.pja@test.com");
    private HttpClient ClientPjB() => CreateClientJwt(SubPjB, "fta.pjb@test.com");
    private HttpClient ClientAdmin() => CreateAdminJwt("fta-admin-sub");

    // =========================================================================
    // Duplicate ATIVO → 409
    // =========================================================================

    [Fact]
    public async Task CreateFundoTipoAtivo_DuplicateAtivo_Returns409()
    {
        using var client = ClientPjA();
        var dedicatedTipoAtivo = NextFtaTipoAtivo();
        var payload = new
        {
            tipoAtivoId = dedicatedTipoAtivo,
            limitePercentual = 40m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var first = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", payload);
        first.StatusCode.ShouldBe(HttpStatusCode.Created,
            "First ATIVO FundoTipoAtivo association must succeed.");

        var second = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", payload);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Duplicate ATIVO association for same (FundoId, TipoAtivoId) must return 409.");
    }

    // =========================================================================
    // Status transitions — full state-machine coverage
    // =========================================================================

    [Fact]
    public async Task FundoTipoAtivo_StatusTransition_AtivoToInativo_Returns200()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", new
        {
            tipoAtivoId = NextFtaTipoAtivo(),
            limitePercentual = 35m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var transResp = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 2 }); // INATIVO

        transResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await transResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString().ShouldBe("INATIVO");
    }

    [Fact]
    public async Task FundoTipoAtivo_StatusTransition_InativoToAtivo_Returns200()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", new
        {
            tipoAtivoId = NextFtaTipoAtivo(),
            limitePercentual = 20m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // ATIVO → INATIVO
        await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 2 });

        // INATIVO → ATIVO (reactivation)
        var reactivateResp = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 1 }); // ATIVO

        reactivateResp.StatusCode.ShouldBe(HttpStatusCode.OK,
            "INATIVO→ATIVO reactivation must succeed (D-22).");
        (await reactivateResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString().ShouldBe("ATIVO");
    }

    [Fact]
    public async Task FundoTipoAtivo_StatusTransition_HistoricoIsTerminal_Returns400()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", new
        {
            tipoAtivoId = NextFtaTipoAtivo(),
            limitePercentual = 10m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // ATIVO → HISTORICO directly (valid — any → HISTORICO allowed per D-22)
        var toHistorico = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 3 }); // HISTORICO
        toHistorico.StatusCode.ShouldBe(HttpStatusCode.OK,
            "ATIVO→HISTORICO direct transition must succeed (D-22).");
        (await toHistorico.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString().ShouldBe("HISTORICO");

        // HISTORICO → INATIVO must fail (terminal state)
        var fromHistorico = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 2 }); // INATIVO
        fromHistorico.StatusCode.ShouldBe(HttpStatusCode.BadRequest,
            "HISTORICO is terminal — any transition from it must return 400 (D-22).");
    }

    // =========================================================================
    // Audit row per D-22 (entityType="FundoTipoAtivo")
    // =========================================================================

    [Fact]
    public async Task FundoTipoAtivo_StatusTransition_WritesAuditRowWithEntityTypeFundoTipoAtivo()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", new
        {
            tipoAtivoId = NextFtaTipoAtivo(),
            limiteValor = 80_000m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Trigger status transition to produce audit row (D-22)
        var transResp = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 2 }); // ATIVO → INATIVO
        transResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert audit row via admin endpoint
        using var adminClient = ClientAdmin();
        var auditResp = await adminClient.GetAsync(
            $"/api/admin/audit-log?entityType=FundoTipoAtivo&entityId={assocId}");
        auditResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var auditBody = await auditResp.Content.ReadAsStringAsync();
        var auditResult = JsonSerializer.Deserialize<AuditPageDto>(auditBody, JsonCaseInsensitive);

        auditResult.ShouldNotBeNull();
        auditResult.TotalCount.ShouldBeGreaterThanOrEqualTo(1,
            "At least one audit row must be written for FundoTipoAtivo status transition (D-22).");
        auditResult.Items.ShouldNotBeNull();
        auditResult.Items!.ShouldAllBe(r => r.EntityType == "FundoTipoAtivo",
            "Audit row entityType must be exactly 'FundoTipoAtivo'.");
        auditResult.Items!.ShouldAllBe(r => r.EntityId == assocId,
            "Audit row entityId must match the association id.");
    }

    // =========================================================================
    // Multi-tenant cross-probe — PJ-B → PJ-A FundoTipoAtivo → 404
    // =========================================================================

    [Fact]
    public async Task FundoTipoAtivo_CrossTenant_PostToOtherTenantFundo_Returns404()
    {
        using var client = ClientPjB();
        var payload = new
        {
            tipoAtivoId = _ftaTipoAtivoIds.Length > 0 ? _ftaTipoAtivoIds[0] : Guid.NewGuid(),
            limitePercentual = 30m,
            dataInicio = DateTimeOffset.UtcNow
        };

        // PJ-B tries to post to PJ-A's Fundo
        var response = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "PJ-B POST to PJ-A Fundo must return 404 (no existence leak, D-5).");
    }

    [Fact]
    public async Task FundoTipoAtivo_CrossTenant_GetListFromOtherTenantFundo_Returns404()
    {
        using var client = ClientPjB();

        var response = await client.GetAsync($"/api/fundos/{_fundoAId}/tipos-ativos");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "PJ-B GET list on PJ-A Fundo must return 404 (D-5).");
    }

    // =========================================================================
    // Date window D-20 invariants
    // =========================================================================

    [Fact]
    public async Task FundoTipoAtivo_Create_DataFimBeforeDataInicio_Returns422()
    {
        using var client = ClientPjA();
        var now = DateTimeOffset.UtcNow;
        var payload = new
        {
            tipoAtivoId = NextFtaTipoAtivo(),
            limitePercentual = 20m,
            dataInicio = now,
            dataFim = now.AddHours(-1) // invalid: dataFim < dataInicio
        };

        var response = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity,
            "DataFim < DataInicio must be rejected with 422 (D-20 JanelaVigencia invariant).");
    }

    [Fact]
    public async Task FundoTipoAtivo_Create_DataFimNull_Returns201()
    {
        using var client = ClientPjA();
        var payload = new
        {
            tipoAtivoId = NextFtaTipoAtivo(),
            limitePercentual = 60m,
            dataInicio = DateTimeOffset.UtcNow
            // dataFim omitted — null = infinite validity
        };

        var response = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created,
            "Null dataFim (infinite window) must be accepted per D-20.");
        var dto = await response.Content.ReadFromJsonAsync<JsonElement>();
        dto.GetProperty("dataFim").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    // =========================================================================
    // LimiteExposicao D-18 invariants
    // =========================================================================

    [Fact]
    public async Task FundoTipoAtivo_Create_LimitePercentualOver100_Returns422()
    {
        using var client = ClientPjA();
        var payload = new
        {
            tipoAtivoId = NextFtaTipoAtivo(),
            limitePercentual = 150m, // > 100 — invalid
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity,
            "LimitePercentual > 100 must be rejected with 422 (D-18).");
    }

    [Fact]
    public async Task FundoTipoAtivo_Create_BothLimitesNull_Returns422()
    {
        using var client = ClientPjA();
        var payload = new
        {
            tipoAtivoId = NextFtaTipoAtivo(),
            limitePercentual = (decimal?)null,
            limiteValor = (decimal?)null,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity,
            "Both limits null must be rejected with 422 (D-18).");
    }

    // =========================================================================
    // Security: unauthenticated → 401
    // =========================================================================

    [Fact]
    public async Task FundoTipoAtivo_Post_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var payload = new
        {
            tipoAtivoId = _ftaTipoAtivoIds.Length > 0 ? _ftaTipoAtivoIds[0] : Guid.NewGuid(),
            limitePercentual = 10m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // GET by id round-trip
    // =========================================================================

    [Fact]
    public async Task FundoTipoAtivo_GetById_ReturnsCorrectAssociation()
    {
        using var client = ClientPjA();
        var tipoAtivoId = NextFtaTipoAtivo();

        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", new
        {
            tipoAtivoId,
            limiteValor = 120_000m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var getResp = await client.GetAsync($"/api/fundos/{_fundoAId}/tipos-ativos/{assocId}");
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dto = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        dto.GetProperty("id").GetGuid().ShouldBe(assocId);
        dto.GetProperty("tipoAtivoId").GetGuid().ShouldBe(tipoAtivoId);
        dto.GetProperty("status").GetString().ShouldBe("ATIVO");
        dto.GetProperty("limiteValor").GetDecimal().ShouldBe(120_000m);
    }

    // =========================================================================
    // Allowed transitions endpoint (D-25) — from INATIVO state
    // =========================================================================

    [Fact]
    public async Task FundoTipoAtivo_AllowedTransitions_WhenInativo_ContainsAtivoAndHistorico()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", new
        {
            tipoAtivoId = NextFtaTipoAtivo(),
            limitePercentual = 15m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Transition to INATIVO to test transitions from that state
        await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 2 }); // INATIVO

        var transitionsResp = await client.GetAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos/{assocId}/allowed-transitions");
        transitionsResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var transitions = await transitionsResp.Content.ReadFromJsonAsync<string[]>();
        transitions.ShouldNotBeNull();
        transitions.ShouldContain("ATIVO",
            "INATIVO association must allow ATIVO reactivation (D-22).");
        transitions.ShouldContain("HISTORICO",
            "INATIVO association must allow HISTORICO terminal (D-22).");
        transitions.ShouldNotContain("INATIVO",
            "INATIVO → INATIVO is not a valid self-transition.");
    }

    // =========================================================================
    // GET list happy-path — drives GetFundoTiposAtivosQueryHandler (B5-iter5 coverage fix)
    // =========================================================================

    [Fact]
    public async Task FundoTipoAtivo_GetList_AuthenticatedPjA_Returns200()
    {
        using var client = ClientPjA();
        var response = await client.GetAsync($"/api/fundos/{_fundoAId}/tipos-ativos");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("totalCount").GetInt32().ShouldBeGreaterThanOrEqualTo(0);
    }

    // =========================================================================
    // JSON DTO types — self-contained
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
