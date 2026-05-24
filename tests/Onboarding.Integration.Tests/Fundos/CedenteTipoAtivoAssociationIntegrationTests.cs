using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Persistence;
using Onboarding.Integration.Tests.Fixtures;
using Shouldly;

namespace Onboarding.Integration.Tests.Fundos;

/// <summary>
/// Integration tests for CedenteTipoAtivo relationship aggregate endpoints (T-4).
///
/// Tests require Docker. Run with:
///   dotnet test tests/Onboarding.Integration.Tests --filter "FullyQualifiedName~CedenteTipoAtivoAssociation"
///
/// Scenarios (T-4 DoD):
///   1.  Duplicate ATIVO same pair → 409 (REL-09 analog for CedenteTipoAtivo)
///   2.  Status transition ATIVO → INATIVO → 200
///   3.  Status transition INATIVO → ATIVO reactivation → 200
///   4.  Status transition INATIVO → HISTORICO terminal → 200
///   5.  HISTORICO → any → 400 (terminal enforcement)
///   6.  Audit row asserted per D-22 (entityType="CedenteTipoAtivo")
///   7.  Multi-tenant: PJ-B request PJ-A Cedente → 404
///   8.  Date window D-20: dataFim before dataInicio → 422
///   9.  Date window D-20: dataFim null accepted → 201
///   10. LimiteExposicao D-18: percentual > 100 → 422
///   11. LimiteExposicao D-18: both limites null → 422
///   12. Unauthenticated POST → 401
///   13. GET by id round-trip
///   14. Allowed transitions when ATIVO contains INATIVO + HISTORICO
///
/// Isolation strategy (D-37):
///   TipoAtivo pool seeded once — each ATIVO-creating test consumes one slot via _ctaSlot counter
///   (Interlocked.Increment). This prevents (CedenteId, TipoAtivoId) partial-unique-index conflicts.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CedenteTipoAtivoAssociationIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    // =========================================================================
    // Seeded IDs
    // =========================================================================

    private Guid _companyAId;
    private Guid _companyBId;
    private Guid _cedenteAId;           // owned by CompanyA
    private Guid _cedenteBId;           // owned by CompanyB (for cross-tenant test)

    // TipoAtivo isolation pool: pool of TipoAtivos, one per ATIVO-creating test.
    private Guid[] _ctaTipoAtivoIds = Array.Empty<Guid>();
    private static int _ctaSlot = -1;
    private Guid NextCtaTipoAtivo() => _ctaTipoAtivoIds[Interlocked.Increment(ref _ctaSlot) % _ctaTipoAtivoIds.Length];

    private const string SubPjA = "cta-pja-sub-001";
    private const string SubPjB = "cta-pjb-sub-002";
    private const int CtaPoolSize = 16; // 16 slots — one per ATIVO-creating test + 2 buffer

    public CedenteTipoAtivoAssociationIntegrationTests(PostgreSqlFixture fixture) : base(fixture) { }

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
        _cedenteAId = readDb.Cedentes.IgnoreQueryFilters()
            .Where(c => c.Nome == "CTA Cedente Alpha").Select(c => c.Id).First();
        _cedenteBId = readDb.Cedentes.IgnoreQueryFilters()
            .Where(c => c.Nome == "CTA Cedente Beta").Select(c => c.Id).First();

        _ctaTipoAtivoIds = readDb.TiposAtivo
            .Where(t => t.Codigo.StartsWith("CTA-POOL-"))
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
            "CTA Alpha Ltda", "66444222000101",
            "cta.alpha@test.com", "+5511000000021",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.1.1"));
        companyA.SetKeycloakUserId(SubPjA);

        var companyB = Company.Register(
            "CTA Beta S.A.", "77555333000101",
            "cta.beta@test.com", "+5511000000022",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "10.0.1.2"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();

        // TipoAtivo isolation pool (global entity — no ClientId)
        for (var i = 0; i < CtaPoolSize; i++)
        {
            await db.TiposAtivo.AddAsync(
                TipoAtivo.Register($"CTA-POOL-{i:D2}", $"CTA Pool TipoAtivo {i}", TipoAtivoCategoria.RendaFixa));
        }
        await db.SaveChangesAsync();

        var cedenteRepo = services.GetRequiredService<ICedenteRepository>();

        var cedenteA = Cedente.RegisterPf("59978867083", "CTA Cedente Alpha", companyA.Id);
        await cedenteRepo.AddAsync(cedenteA);

        var cedenteB = Cedente.RegisterPf("12345678909", "CTA Cedente Beta", companyB.Id);
        await cedenteRepo.AddAsync(cedenteB);

        await db.SaveChangesAsync();
    }

    // =========================================================================
    // HTTP helpers
    // =========================================================================

    private HttpClient ClientPjA() => CreateClientJwt(SubPjA, "cta.pja@test.com");
    private HttpClient ClientPjB() => CreateClientJwt(SubPjB, "cta.pjb@test.com");
    private HttpClient ClientAdmin() => CreateAdminJwt("cta-admin-sub");

    // =========================================================================
    // Duplicate ATIVO → 409
    // =========================================================================

    [Fact]
    public async Task CreateCedenteTipoAtivo_DuplicateAtivo_Returns409()
    {
        using var client = ClientPjA();
        var dedicatedTipoAtivo = NextCtaTipoAtivo();
        var payload = new
        {
            tipoAtivoId = dedicatedTipoAtivo,
            limitePercentual = 20m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var first = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);
        first.StatusCode.ShouldBe(HttpStatusCode.Created,
            "First ATIVO CedenteTipoAtivo association must succeed.");

        var second = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Duplicate ATIVO association for same (CedenteId, TipoAtivoId) must return 409.");
    }

    // =========================================================================
    // Status transitions — full state-machine coverage
    // =========================================================================

    [Fact]
    public async Task CedenteTipoAtivo_StatusTransition_AtivoToInativo_Returns200()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", new
        {
            tipoAtivoId = NextCtaTipoAtivo(),
            limitePercentual = 30m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var transResp = await client.PostAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 2 }); // INATIVO

        transResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await transResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString().ShouldBe("INATIVO");
    }

    [Fact]
    public async Task CedenteTipoAtivo_StatusTransition_InativoToAtivo_Returns200()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", new
        {
            tipoAtivoId = NextCtaTipoAtivo(),
            limitePercentual = 25m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // ATIVO → INATIVO
        await client.PostAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 2 });

        // INATIVO → ATIVO (reactivation)
        var reactivateResp = await client.PostAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 1 }); // ATIVO

        reactivateResp.StatusCode.ShouldBe(HttpStatusCode.OK,
            "INATIVO→ATIVO reactivation must succeed (D-22).");
        (await reactivateResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString().ShouldBe("ATIVO");
    }

    [Fact]
    public async Task CedenteTipoAtivo_StatusTransition_HistoricoIsTerminal_Returns400()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", new
        {
            tipoAtivoId = NextCtaTipoAtivo(),
            limitePercentual = 15m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // ATIVO → INATIVO
        await client.PostAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 2 });

        // INATIVO → HISTORICO (terminal)
        var toHistorico = await client.PostAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 3 });
        toHistorico.StatusCode.ShouldBe(HttpStatusCode.OK,
            "INATIVO→HISTORICO must succeed.");

        // HISTORICO → ATIVO must fail (terminal state)
        var fromHistorico = await client.PostAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 1 });
        fromHistorico.StatusCode.ShouldBe(HttpStatusCode.BadRequest,
            "Transition from terminal HISTORICO must return 400 (D-22).");
    }

    // =========================================================================
    // Audit row per D-22 (entityType="CedenteTipoAtivo")
    // =========================================================================

    [Fact]
    public async Task CedenteTipoAtivo_StatusTransition_WritesAuditRowWithEntityTypeCedenteTipoAtivo()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", new
        {
            tipoAtivoId = NextCtaTipoAtivo(),
            limiteValor = 100_000m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Trigger status transition to produce audit row (D-22)
        var transResp = await client.PostAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 2 }); // ATIVO → INATIVO
        transResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert audit row via admin endpoint
        using var adminClient = ClientAdmin();
        var auditResp = await adminClient.GetAsync(
            $"/api/admin/audit-log?entityType=CedenteTipoAtivo&entityId={assocId}");
        auditResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var auditBody = await auditResp.Content.ReadAsStringAsync();
        var auditResult = JsonSerializer.Deserialize<AuditPageDto>(auditBody, JsonCaseInsensitive);

        auditResult.ShouldNotBeNull();
        auditResult.TotalCount.ShouldBeGreaterThanOrEqualTo(1,
            "At least one audit row must be written for CedenteTipoAtivo status transition (D-22).");
        auditResult.Items.ShouldNotBeNull();
        auditResult.Items!.ShouldAllBe(r => r.EntityType == "CedenteTipoAtivo",
            "Audit row entityType must be exactly 'CedenteTipoAtivo'.");
        auditResult.Items!.ShouldAllBe(r => r.EntityId == assocId,
            "Audit row entityId must match the association id.");
    }

    // =========================================================================
    // Multi-tenant cross-probe — PJ-B → PJ-A Cedente → 404
    // =========================================================================

    [Fact]
    public async Task CedenteTipoAtivo_CrossTenant_PostToOtherTenantCedente_Returns404()
    {
        using var client = ClientPjB();
        var payload = new
        {
            tipoAtivoId = _ctaTipoAtivoIds.Length > 0 ? _ctaTipoAtivoIds[0] : Guid.NewGuid(),
            limitePercentual = 20m,
            dataInicio = DateTimeOffset.UtcNow
        };

        // PJ-B tries to post to PJ-A's cedente
        var response = await client.PostAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "PJ-B POST to PJ-A Cedente must return 404 (no existence leak, D-5).");
    }

    [Fact]
    public async Task CedenteTipoAtivo_CrossTenant_GetListFromOtherTenantCedente_Returns404()
    {
        using var client = ClientPjB();

        var response = await client.GetAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "PJ-B GET list on PJ-A Cedente must return 404 (D-5).");
    }

    // =========================================================================
    // Date window D-20 invariants
    // =========================================================================

    [Fact]
    public async Task CedenteTipoAtivo_Create_DataFimBeforeDataInicio_Returns422()
    {
        using var client = ClientPjA();
        var now = DateTimeOffset.UtcNow;
        var payload = new
        {
            tipoAtivoId = NextCtaTipoAtivo(),
            limitePercentual = 10m,
            dataInicio = now,
            dataFim = now.AddDays(-1) // invalid: dataFim < dataInicio
        };

        var response = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity,
            "DataFim < DataInicio must be rejected with 422 (D-20 JanelaVigencia invariant).");
    }

    [Fact]
    public async Task CedenteTipoAtivo_Create_DataFimNull_Returns201()
    {
        using var client = ClientPjA();
        var payload = new
        {
            tipoAtivoId = NextCtaTipoAtivo(),
            limitePercentual = 50m,
            dataInicio = DateTimeOffset.UtcNow
            // dataFim omitted — null = infinite validity
        };

        var response = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created,
            "Null dataFim (infinite window) must be accepted per D-20.");
        var dto = await response.Content.ReadFromJsonAsync<JsonElement>();
        dto.GetProperty("dataFim").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    // =========================================================================
    // LimiteExposicao D-18 invariants
    // =========================================================================

    [Fact]
    public async Task CedenteTipoAtivo_Create_LimitePercentualOver100_Returns422()
    {
        using var client = ClientPjA();
        var payload = new
        {
            tipoAtivoId = NextCtaTipoAtivo(),
            limitePercentual = 110m, // > 100 — invalid
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity,
            "LimitePercentual > 100 must be rejected with 422 (D-18).");
    }

    [Fact]
    public async Task CedenteTipoAtivo_Create_BothLimitesNull_Returns422()
    {
        using var client = ClientPjA();
        var payload = new
        {
            tipoAtivoId = NextCtaTipoAtivo(),
            limitePercentual = (decimal?)null,
            limiteValor = (decimal?)null,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity,
            "Both limits null must be rejected with 422 (D-18).");
    }

    // =========================================================================
    // Security: unauthenticated → 401
    // =========================================================================

    [Fact]
    public async Task CedenteTipoAtivo_Post_Unauthenticated_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var payload = new
        {
            tipoAtivoId = _ctaTipoAtivoIds.Length > 0 ? _ctaTipoAtivoIds[0] : Guid.NewGuid(),
            limitePercentual = 10m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // GET by id round-trip
    // =========================================================================

    [Fact]
    public async Task CedenteTipoAtivo_GetById_ReturnsCorrectAssociation()
    {
        using var client = ClientPjA();
        var tipoAtivoId = NextCtaTipoAtivo();

        var createResp = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", new
        {
            tipoAtivoId,
            limiteValor = 50_000m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var getResp = await client.GetAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos/{assocId}");
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dto = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        dto.GetProperty("id").GetGuid().ShouldBe(assocId);
        dto.GetProperty("tipoAtivoId").GetGuid().ShouldBe(tipoAtivoId);
        dto.GetProperty("status").GetString().ShouldBe("ATIVO");
        dto.GetProperty("limiteValor").GetDecimal().ShouldBe(50_000m);
    }

    // =========================================================================
    // Allowed transitions endpoint (D-25)
    // =========================================================================

    [Fact]
    public async Task CedenteTipoAtivo_AllowedTransitions_WhenAtivo_ContainsInativoAndHistorico()
    {
        using var client = ClientPjA();

        var createResp = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", new
        {
            tipoAtivoId = NextCtaTipoAtivo(),
            limitePercentual = 10m,
            dataInicio = DateTimeOffset.UtcNow
        });
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var transitionsResp = await client.GetAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos/{assocId}/allowed-transitions");
        transitionsResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var transitions = await transitionsResp.Content.ReadFromJsonAsync<string[]>();
        transitions.ShouldNotBeNull();
        transitions.ShouldContain("INATIVO",
            "ATIVO association must allow INATIVO transition (D-22).");
        transitions.ShouldContain("HISTORICO",
            "ATIVO association must allow HISTORICO transition (D-22).");
        transitions.ShouldNotContain("ATIVO",
            "ATIVO → ATIVO is not a valid transition.");
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
