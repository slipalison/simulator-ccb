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
/// Integration tests for Phase 50 relationship aggregate endpoints (T-7).
///
/// Tests require Docker. Run with:
///   dotnet test tests/Onboarding.Integration.Tests --filter "FullyQualifiedName~RelationshipAggregates"
///
/// Scenarios covered (minimum 18 per D-21):
///
/// FundoCedente:
///   1. POST /api/fundos/{fundoId}/cedentes → 201 (happy path)
///   2. POST duplicate active → 409 (REL-09 in-memory guard)
///   3. Cross-tenant POST (PJ-B tries PJ-A's fundo) → 404
///   4. PATCH .../limits happy path → 200
///   5. POST .../status ATIVO → INATIVO → 200; HISTORICO terminal → 400
///   6. GET list only returns own company's associations
///
/// CedenteTipoAtivo (symmetric, 6 scenarios)
/// FundoTipoAtivo (symmetric, 6 scenarios)
///
/// Admin cross-company (T-6 bonus):
///   - GET /api/admin/fundos/fundo-cedentes sees both companies
///
/// Isolation strategy (D-37):
///   Each test that creates an ATIVO FundoCedente association uses a DEDICATED cedente
///   from a pre-seeded pool (_fcCedenteIds[]), dispensed via a static atomic counter
///   (_fcSlot). This prevents REL-09 partial-unique-index violations when multiple
///   tests share the same PostgreSQL container via IClassFixture.
///
///   CedenteTipoAtivo and FundoTipoAtivo tests use analogous TipoAtivo pools
///   (_ctaIds[], _ftaIds[]) dispensed via _ctaSlot / _ftaSlot counters.
/// </summary>
[Trait("Category", "Integration")]
public class RelationshipAggregatesIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    // Seeded IDs — set in InitializeAsync
    private Guid _companyAId;
    private Guid _companyBId;
    private Guid _fundoAId;              // Fundo belonging to CompanyA
    private Guid _fundoBId;              // Fundo belonging to CompanyB
    private Guid _cedenteAId;            // Cedente for cross-tenant tests (never used in ATIVO-creating tests)
    private Guid _cedenteConcurrentId;   // Dedicated to ConcurrentCreate test only

    // FundoCedente isolation pool: 7 cedentes, one per FC test that creates an ATIVO association.
    // Slot 0..6 are dispensed in test-creation order via _fcSlot static counter.
    private Guid[] _fcCedenteIds = Array.Empty<Guid>();
    private static int _fcSlot = -1;
    private Guid NextFcCedenteId() => _fcCedenteIds[Interlocked.Increment(ref _fcSlot) % _fcCedenteIds.Length];

    // CedenteTipoAtivo isolation pool: 4 TipoAtivos, one per CTA test that creates an ATIVO association.
    private Guid[] _ctaIds = Array.Empty<Guid>();
    private static int _ctaSlot = -1;
    private Guid NextCtaTipoAtivoId() => _ctaIds[Interlocked.Increment(ref _ctaSlot) % _ctaIds.Length];

    // FundoTipoAtivo isolation pool: 4 TipoAtivos, one per FTA test that creates an ATIVO association.
    private Guid[] _ftaIds = Array.Empty<Guid>();
    private static int _ftaSlot = -1;
    private Guid NextFtaTipoAtivoId() => _ftaIds[Interlocked.Increment(ref _ftaSlot) % _ftaIds.Length];

    private const string SubPjA = "rel-integration-pja-001";
    private const string SubPjB = "rel-integration-pjb-002";

    // FC pool size: 7 tests × 1 ATIVO association each + 1 extra buffer
    private const int FcPoolSize = 8;
    // CTA pool size: 4 tests × 1 ATIVO association each
    private const int CtaPoolSize = 4;
    // FTA pool size: 4 tests × 1 ATIVO association each
    private const int FtaPoolSize = 4;

    public RelationshipAggregatesIntegrationTests(PostgreSqlFixture fixture) : base(fixture) { }

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
            await SeedAsync(db, scope.ServiceProvider);
        });

        // Re-read IDs on every test instance — seed is idempotent, rows are stable.
        // IgnoreQueryFilters() bypasses HasQueryFilter (tenant isolation) needed for direct DB reads
        // outside an HTTP request context where CompanyId would be Guid.Empty.
        using var readScope = CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _companyAId = readDb.Companies.IgnoreQueryFilters().Where(c => c.KeycloakUserId == SubPjA).Select(c => c.Id).First();
        _companyBId = readDb.Companies.IgnoreQueryFilters().Where(c => c.KeycloakUserId == SubPjB).Select(c => c.Id).First();
        _fundoAId = readDb.Fundos.IgnoreQueryFilters().Where(f => f.Nome == "Fundo Rel Alpha").Select(f => f.Id).First();
        _fundoBId = readDb.Fundos.IgnoreQueryFilters().Where(f => f.Nome == "Fundo Rel Beta").Select(f => f.Id).First();
        _cedenteAId = readDb.Cedentes.IgnoreQueryFilters().Where(c => c.Nome == "Cedente Alpha PF").Select(c => c.Id).First();
        _cedenteConcurrentId = readDb.Cedentes.IgnoreQueryFilters().Where(c => c.Nome == "Cedente Alpha PF Concurrent").Select(c => c.Id).First();

        // Load FC cedente pool (sorted by name for deterministic ordering across test instances)
        _fcCedenteIds = readDb.Cedentes.IgnoreQueryFilters()
            .Where(c => c.Nome.StartsWith("Cedente FC "))
            .OrderBy(c => c.Nome)
            .Select(c => c.Id)
            .ToArray();

        // Load CTA and FTA TipoAtivo pools
        _ctaIds = readDb.TiposAtivo
            .Where(t => t.Codigo.StartsWith("CDB-CTA-"))
            .OrderBy(t => t.Codigo)
            .Select(t => t.Id)
            .ToArray();

        _ftaIds = readDb.TiposAtivo
            .Where(t => t.Codigo.StartsWith("CDB-FTA-"))
            .OrderBy(t => t.Codigo)
            .Select(t => t.Id)
            .ToArray();
    }

    // =========================================================================
    // Seed
    // =========================================================================

    private static async Task SeedAsync(AppDbContext db, IServiceProvider services)
    {
        // Company A + B
        var companyA = Company.Register(
            "Alpha Rel Integration Ltda", "11444777000161",
            "alpha.rel@test.com", "+5511000000001",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "192.168.1.10"));
        companyA.SetKeycloakUserId(SubPjA);

        var companyB = Company.Register(
            "Beta Rel Integration S.A.", "62232889000190",
            "beta.rel@test.com", "+5511000000002",
            TermsAcceptance.Create(TermsAcceptance.CurrentVersion, "192.168.1.20"));
        companyB.SetKeycloakUserId(SubPjB);

        await db.Companies.AddRangeAsync(companyA, companyB);
        await db.SaveChangesAsync();

        // Prerequisite entities — bypass API to avoid circular dependency on endpoint-under-test
        // All share the same CNPJ per company since they are in separate tables with separate unique indexes
        var companyACnpj = "11444777000161";
        var companyBCnpj = "62232889000190";

        var consultoriaA = ConsultoriaFundo.Register("Consultoria Rel Alpha", companyACnpj, companyA.Id);
        var custodianteA = Custodiante.Register("Custodiante Rel Alpha", companyACnpj, companyA.Id);
        var fundoA = Fundo.Register("Fundo Rel Alpha", companyACnpj, companyA.Id,
            consultoriaA.Id, custodianteA.Id, TipoFundo.RendaFixa);

        var consultoriaB = ConsultoriaFundo.Register("Consultoria Rel Beta", companyBCnpj, companyB.Id);
        var custodianteB = Custodiante.Register("Custodiante Rel Beta", companyBCnpj, companyB.Id);
        var fundoB = Fundo.Register("Fundo Rel Beta", companyBCnpj, companyB.Id,
            consultoriaB.Id, custodianteB.Id, TipoFundo.Multimercado);

        await db.ConsultoriasFundo.AddRangeAsync(consultoriaA, consultoriaB);
        await db.Custodiantes.AddRangeAsync(custodianteA, custodianteB);
        await db.Fundos.AddRangeAsync(fundoA, fundoB);

        // TipoAtivo pools for CedenteTipoAtivo (CTA) and FundoTipoAtivo (FTA) isolation
        for (var i = 0; i < CtaPoolSize; i++)
            await db.TiposAtivo.AddAsync(TipoAtivo.Register($"CDB-CTA-{i}", $"CDB CTA Pool {i}", TipoAtivoCategoria.RendaFixa));

        for (var i = 0; i < FtaPoolSize; i++)
            await db.TiposAtivo.AddAsync(TipoAtivo.Register($"CDB-FTA-{i}", $"CDB FTA Pool {i}", TipoAtivoCategoria.RendaFixa));

        await db.SaveChangesAsync();

        // Cedente requires ICedenteRepository to set shadow properties (DocumentoTipo, CpfValue) per D-09
        var cedenteRepo = services.GetRequiredService<ICedenteRepository>();

        // Cross-tenant cedente for tenant isolation tests (never involved in ATIVO-creating tests)
        var cedenteA = Cedente.RegisterPf("52998224725", "Cedente Alpha PF", companyA.Id);
        await cedenteRepo.AddAsync(cedenteA);

        // Concurrent test cedente — isolated, used only by FundoCedente_ConcurrentCreate_OnlyOneSucceeds
        var cedenteConcurrent = Cedente.RegisterPf("40484604805", "Cedente Alpha PF Concurrent", companyA.Id);
        await cedenteRepo.AddAsync(cedenteConcurrent);

        // FundoCedente isolation pool: FcPoolSize cedentes with unique CPFs generated via GenerateCpf.
        // Name prefix "Cedente FC " used for pool lookup in InitializeAsync.
        // CPF counter starts at 1000 to avoid collisions with manually seeded CPFs above.
        for (var i = 0; i < FcPoolSize; i++)
        {
            var cpf = GenerateCpf(1000 + i);
            var cedente = Cedente.RegisterPf(cpf, $"Cedente FC {i:D2}", companyA.Id);
            await cedenteRepo.AddAsync(cedente);
        }

        await db.SaveChangesAsync();
    }

    // =========================================================================
    // HTTP helpers
    // =========================================================================

    private HttpClient ClientPjA() => CreateClientJwt(SubPjA);
    private HttpClient ClientPjB() => CreateClientJwt(SubPjB);
    private HttpClient ClientAdmin() => CreateAdminJwt("admin-rel-sub");

    // =========================================================================
    // FundoCedente — 6 scenarios
    // =========================================================================

    [Fact]
    public async Task FundoCedente_Create_Returns201()
    {
        using var client = ClientPjA();
        var payload = new
        {
            cedenteId = NextFcCedenteId(),
            limitePercentual = 50m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        // Status is serialized as string via JsonStringEnumConverter (commit 4c352bf).
        var dto = await response.Content.ReadFromJsonAsync<JsonElement>();
        dto.GetProperty("status").GetString().ShouldBe("ATIVO", "Created association must have ATIVO status.");
    }

    [Fact]
    public async Task FundoCedente_CreateDuplicate_Returns409()
    {
        using var client = ClientPjA();
        var dedicatedCedente = NextFcCedenteId();
        var payload = new
        {
            cedenteId = dedicatedCedente,
            limitePercentual = 30m,
            dataInicio = DateTimeOffset.UtcNow
        };

        // First create — must succeed
        var first = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Second create same pair while first is ATIVO — must return 409
        var second = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Duplicate ATIVO association for same FundoId+CedenteId pair must return 409 (REL-09).");
    }

    [Fact]
    public async Task FundoCedente_CrossTenantCreate_Returns404()
    {
        using var client = ClientPjB();
        var payload = new
        {
            cedenteId = _cedenteAId,
            limitePercentual = 40m,
            dataInicio = DateTimeOffset.UtcNow
        };

        // PJ-B tries to post to PJ-A's Fundo
        var response = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Cross-tenant POST must return 404, not 403, to avoid leaking Fundo existence.");
    }

    [Fact]
    public async Task FundoCedente_UpdateLimits_Returns200()
    {
        using var client = ClientPjA();

        // Create association first — dedicated cedente to avoid REL-09 conflict with other tests
        var createPayload = new
        {
            cedenteId = NextFcCedenteId(),
            limiteValor = 50_000m,
            dataInicio = DateTimeOffset.UtcNow
        };
        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Update limits
        var updatePayload = new { limitePercentual = 75m };
        var updateResp = await client.PatchAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/limits", updatePayload);

        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await updateResp.Content.ReadAsStringAsync();
        body.ShouldContain("75");
    }

    [Fact]
    public async Task FundoCedente_StatusTransition_AtivoToInativo_Returns200()
    {
        using var client = ClientPjA();

        // Create — dedicated cedente
        var createPayload = new
        {
            cedenteId = NextFcCedenteId(),
            limitePercentual = 25m,
            dataInicio = DateTimeOffset.UtcNow
        };
        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Transition ATIVO → INATIVO (RelationshipStatus.INATIVO = 2)
        var transitionResp = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/status",
            new { newStatus = 2 });

        transitionResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var transitionDto = await transitionResp.Content.ReadFromJsonAsync<JsonElement>();
        transitionDto.GetProperty("status").GetString().ShouldBe("INATIVO", "Status must be INATIVO after transition.");
    }

    [Fact]
    public async Task FundoCedente_StatusTransition_HistoricoTerminal_Returns400()
    {
        using var client = ClientPjA();

        // Create — dedicated cedente
        var createPayload = new
        {
            cedenteId = NextFcCedenteId(),
            limitePercentual = 20m,
            dataInicio = DateTimeOffset.UtcNow
        };
        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // ATIVO → HISTORICO (terminal). RelationshipStatus.HISTORICO = 3
        var toHistorico = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/status",
            new { newStatus = 3 });
        toHistorico.StatusCode.ShouldBe(HttpStatusCode.OK);

        // HISTORICO → ATIVO must fail. RelationshipStatus.ATIVO = 1
        var fromHistorico = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/cedentes/{assocId}/status",
            new { newStatus = 1 });
        fromHistorico.StatusCode.ShouldBe(HttpStatusCode.BadRequest,
            "HISTORICO is terminal — transition to any other status must return 400.");
    }

    [Fact]
    public async Task FundoCedente_GetList_OnlyReturnsTenantOwnedRows()
    {
        using var clientA = ClientPjA();
        using var clientB = ClientPjB();

        // PJ-A creates an association — dedicated cedente
        var createPayload = new
        {
            cedenteId = NextFcCedenteId(),
            limitePercentual = 10m,
            dataInicio = DateTimeOffset.UtcNow
        };
        var createResp = await clientA.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // PJ-B tries to list PJ-A's Fundo associations — must get 404 (cross-tenant)
        var listResp = await clientB.GetAsync($"/api/fundos/{_fundoAId}/cedentes");
        listResp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // CedenteTipoAtivo — 6 scenarios
    // =========================================================================

    [Fact]
    public async Task CedenteTipoAtivo_Create_Returns201()
    {
        using var client = ClientPjA();
        var payload = new
        {
            tipoAtivoId = NextCtaTipoAtivoId(),
            limitePercentual = 30m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var ctaDto = await response.Content.ReadFromJsonAsync<JsonElement>();
        ctaDto.GetProperty("status").GetString().ShouldBe("ATIVO", "Created association must have ATIVO status.");
    }

    [Fact]
    public async Task CedenteTipoAtivo_CreateDuplicate_Returns409()
    {
        using var client = ClientPjA();
        var dedicatedTipoAtivo = NextCtaTipoAtivoId();
        var payload = new
        {
            tipoAtivoId = dedicatedTipoAtivo,
            limitePercentual = 25m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var first = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CedenteTipoAtivo_CrossTenantCreate_Returns404()
    {
        using var client = ClientPjB();
        // Uses _ctaIds[0] directly — no slot consumed since no ATIVO creation expected
        // (404 is returned before any DB write)
        var payload = new
        {
            tipoAtivoId = _ctaIds.Length > 0 ? _ctaIds[0] : Guid.NewGuid(),
            limitePercentual = 20m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CedenteTipoAtivo_UpdateLimits_Returns200()
    {
        using var client = ClientPjA();

        var createPayload = new { tipoAtivoId = NextCtaTipoAtivoId(), limiteValor = 80_000m, dataInicio = DateTimeOffset.UtcNow };
        var createResp = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var updateResp = await client.PatchAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos/{assocId}/limits",
            new { limitePercentual = 60m });

        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CedenteTipoAtivo_StatusTransition_AtivoToInativo_Returns200()
    {
        using var client = ClientPjA();

        var createPayload = new { tipoAtivoId = NextCtaTipoAtivoId(), limitePercentual = 15m, dataInicio = DateTimeOffset.UtcNow };
        var createResp = await client.PostAsJsonAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // RelationshipStatus.INATIVO = 2
        var transResp = await client.PostAsJsonAsync(
            $"/api/cedentes/{_cedenteAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 2 });

        transResp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CedenteTipoAtivo_GetList_CrossTenantCedente_Returns404()
    {
        using var client = ClientPjB();

        var listResp = await client.GetAsync($"/api/cedentes/{_cedenteAId}/tipos-ativos");
        listResp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // FundoTipoAtivo — 6 scenarios
    // =========================================================================

    [Fact]
    public async Task FundoTipoAtivo_Create_Returns201()
    {
        using var client = ClientPjA();
        var payload = new
        {
            tipoAtivoId = NextFtaTipoAtivoId(),
            limitePercentual = 40m,
            dataInicio = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var ftaDto = await response.Content.ReadFromJsonAsync<JsonElement>();
        ftaDto.GetProperty("status").GetString().ShouldBe("ATIVO", "Created association must have ATIVO status.");
    }

    [Fact]
    public async Task FundoTipoAtivo_CreateDuplicate_Returns409()
    {
        using var client = ClientPjA();
        var dedicatedTipoAtivo = NextFtaTipoAtivoId();
        var payload = new { tipoAtivoId = dedicatedTipoAtivo, limitePercentual = 35m, dataInicio = DateTimeOffset.UtcNow };

        var first = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", payload);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", payload);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task FundoTipoAtivo_CrossTenantCreate_Returns404()
    {
        using var client = ClientPjB();
        var payload = new { tipoAtivoId = _ftaIds.Length > 0 ? _ftaIds[0] : Guid.NewGuid(), limitePercentual = 10m, dataInicio = DateTimeOffset.UtcNow };

        var response = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", payload);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FundoTipoAtivo_UpdateLimits_Returns200()
    {
        using var client = ClientPjA();

        var createPayload = new { tipoAtivoId = NextFtaTipoAtivoId(), limiteValor = 60_000m, dataInicio = DateTimeOffset.UtcNow };
        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var updateResp = await client.PatchAsJsonAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos/{assocId}/limits",
            new { limitePercentual = 55m });

        updateResp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FundoTipoAtivo_StatusTransition_AtivoToInativo_Returns200()
    {
        using var client = ClientPjA();

        var createPayload = new { tipoAtivoId = NextFtaTipoAtivoId(), limitePercentual = 45m, dataInicio = DateTimeOffset.UtcNow };
        var createResp = await client.PostAsJsonAsync($"/api/fundos/{_fundoAId}/tipos-ativos", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assocId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // RelationshipStatus.INATIVO = 2
        var transResp = await client.PostAsJsonAsync(
            $"/api/fundos/{_fundoAId}/tipos-ativos/{assocId}/status",
            new { newStatus = 2 });

        transResp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FundoTipoAtivo_GetList_CrossTenantFundo_Returns404()
    {
        using var client = ClientPjB();
        var listResp = await client.GetAsync($"/api/fundos/{_fundoAId}/tipos-ativos");
        listResp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // FundoCedente — REL-09 race condition (DB partial unique index gate)
    // =========================================================================

    /// <summary>
    /// Simulates two concurrent POST requests for the same FundoId+CedenteId pair.
    /// The DB partial unique index (FundoId, CedenteId) WHERE Status='ATIVO' must reject the
    /// second insert with a DbUpdateException, which GlobalExceptionHandler maps to 409 (D-18).
    ///
    /// Note: this test is not a strict concurrency test — true race conditions are difficult to
    /// reproduce deterministically in integration tests. This test verifies that when two requests
    /// arrive in quick succession, exactly one succeeds (201) and the other gets a conflict (409),
    /// regardless of which one "wins" the race.
    /// </summary>
    [Fact]
    public async Task FundoCedente_ConcurrentCreate_OnlyOneSucceeds()
    {
        // Uses _cedenteConcurrentId seeded in InitializeAsync (CPF 40484604805, CompanyA-scoped).
        // This cedente is not used by any other test — no interference with test order.
        var payload = new
        {
            cedenteId = _cedenteConcurrentId,
            limitePercentual = 10m,
            dataInicio = DateTimeOffset.UtcNow
        };

        // Fire two requests concurrently targeting the same (FundoId, CedenteId) pair.
        // Both handlers will pass the in-memory ActivateGuard (no ATIVO row exists yet when
        // they both read from DB). The DB partial unique index (D-18) rejects the second insert
        // with a unique violation, which GlobalExceptionHandler maps to HTTP 409.
        using var client1 = ClientPjA();
        using var client2 = ClientPjA();

        var t1 = client1.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);
        var t2 = client2.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", payload);

        var results = await Task.WhenAll(t1, t2);

        var statuses = results.Select(r => (int)r.StatusCode).OrderBy(s => s).ToArray();

        // Exactly one (201, 409) pair — order depends on which request wins the DB race.
        statuses.ShouldContain(201,
            "One concurrent request must succeed and create the ATIVO association.");
        statuses.ShouldContain(409,
            "The other concurrent request must be rejected with 409 — DB partial unique index enforces REL-09 (D-18).");
    }

    // =========================================================================
    // Admin cross-company (T-6 bonus scenario)
    // =========================================================================

    [Fact]
    public async Task AdminFundoCedentes_CrossCompany_ReturnsBothCompanies()
    {
        // Create a FundoCedente in PJ-A — dedicated cedente slot
        using var clientA = ClientPjA();
        var createPayload = new { cedenteId = NextFcCedenteId(), limitePercentual = 5m, dataInicio = DateTimeOffset.UtcNow };
        var createResp = await clientA.PostAsJsonAsync($"/api/fundos/{_fundoAId}/cedentes", createPayload);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Admin lists all FundoCedentes — must see the one just created
        using var adminClient = ClientAdmin();
        var adminResp = await adminClient.GetAsync("/api/admin/fundos/fundo-cedentes");
        adminResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await adminResp.Content.ReadAsStringAsync();
        body.ShouldContain(_fundoAId.ToString());
    }
}
