using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Onboarding.API.Controllers;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Queries.Admin;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Shouldly;

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// Unit tests for AdminFundosController GET-by-id endpoints (Phase 51, D-8 fix).
/// Phase 55 refactor: uses IQueryDispatcher (D-62, was 11 ctor deps).
/// </summary>
public sealed class AdminFundosControllerByIdTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid EntityId = Guid.NewGuid();
    private const string EmpresaNome = "Empresa Teste Ltda";

    private readonly IQueryDispatcher _queries = Substitute.For<IQueryDispatcher>();
    private readonly AdminFundosController _sut;

    public AdminFundosControllerByIdTests()
    {
        _sut = new AdminFundosController(_queries)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    // =========================================================================
    // GET /api/admin/fundos/{id}
    // =========================================================================

    [Fact]
    public async Task GetFundoById_HandlerReturnsDto_Returns200WithDto()
    {
        var dto = new AdminFundoDto(
            EntityId, CompanyId, EmpresaNome, "Fundo Alpha", "11444777000161",
            Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa,
            null, null, null, FundoStatus.RASCUNHO, DateTimeOffset.UtcNow);
        _queries.Query<AdminFundoDto?>(Arg.Any<GetAdminFundoByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.GetFundoById(EntityId);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task GetFundoById_HandlerReturnsNull_Returns404()
    {
        _queries.Query<AdminFundoDto?>(Arg.Any<GetAdminFundoByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((AdminFundoDto?)null);

        var result = await _sut.GetFundoById(Guid.NewGuid());

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetFundoById_PassesCorrectIdToDispatcher()
    {
        var id = Guid.NewGuid();
        GetAdminFundoByIdQuery? captured = null;
        _queries.Query<AdminFundoDto?>(Arg.Do<object>(q => captured = q as GetAdminFundoByIdQuery), Arg.Any<CancellationToken>())
            .Returns((AdminFundoDto?)null);

        await _sut.GetFundoById(id);

        captured.ShouldNotBeNull();
        captured!.Id.ShouldBe(id);
    }

    // =========================================================================
    // GET /api/admin/fundos/consultorias/{id}
    // =========================================================================

    [Fact]
    public async Task GetConsultoriaById_HandlerReturnsDto_Returns200WithDto()
    {
        var dto = new AdminConsultoriaFundoDto(
            EntityId, CompanyId, EmpresaNome,
            "Consultoria Beta Ltda", null, "11444777000161",
            null, null, ConsultoriaFundoStatus.ATIVO, DateTimeOffset.UtcNow);
        _queries.Query<AdminConsultoriaFundoDto?>(Arg.Any<GetAdminConsultoriaFundoByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.GetConsultoriaById(EntityId);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task GetConsultoriaById_HandlerReturnsNull_Returns404()
    {
        _queries.Query<AdminConsultoriaFundoDto?>(Arg.Any<GetAdminConsultoriaFundoByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((AdminConsultoriaFundoDto?)null);

        var result = await _sut.GetConsultoriaById(Guid.NewGuid());

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetConsultoriaById_PassesCorrectIdToDispatcher()
    {
        var id = Guid.NewGuid();
        GetAdminConsultoriaFundoByIdQuery? captured = null;
        _queries.Query<AdminConsultoriaFundoDto?>(Arg.Do<object>(q => captured = q as GetAdminConsultoriaFundoByIdQuery), Arg.Any<CancellationToken>())
            .Returns((AdminConsultoriaFundoDto?)null);

        await _sut.GetConsultoriaById(id);

        captured.ShouldNotBeNull();
        captured!.Id.ShouldBe(id);
    }

    // =========================================================================
    // GET /api/admin/fundos/custodiantes/{id}
    // =========================================================================

    [Fact]
    public async Task GetCustodianteById_HandlerReturnsDto_Returns200WithDto()
    {
        var dto = new AdminCustodianteDto(
            EntityId, CompanyId, EmpresaNome,
            "Custodiante Gamma", null, "11444777000161",
            null, null, CustodianteStatus.ATIVO, DateTimeOffset.UtcNow);
        _queries.Query<AdminCustodianteDto?>(Arg.Any<GetAdminCustodianteByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.GetCustodianteById(EntityId);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task GetCustodianteById_HandlerReturnsNull_Returns404()
    {
        _queries.Query<AdminCustodianteDto?>(Arg.Any<GetAdminCustodianteByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((AdminCustodianteDto?)null);

        var result = await _sut.GetCustodianteById(Guid.NewGuid());

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCustodianteById_PassesCorrectIdToDispatcher()
    {
        var id = Guid.NewGuid();
        GetAdminCustodianteByIdQuery? captured = null;
        _queries.Query<AdminCustodianteDto?>(Arg.Do<object>(q => captured = q as GetAdminCustodianteByIdQuery), Arg.Any<CancellationToken>())
            .Returns((AdminCustodianteDto?)null);

        await _sut.GetCustodianteById(id);

        captured.ShouldNotBeNull();
        captured!.Id.ShouldBe(id);
    }

    // =========================================================================
    // GET /api/admin/fundos/cedentes/{id}
    // =========================================================================

    [Fact]
    public async Task GetCedenteById_HandlerReturnsDto_Returns200WithDto()
    {
        var dto = new AdminCedenteDto(
            EntityId, CompanyId, EmpresaNome,
            "52998224725", "Cedente Delta",
            null, null, null,
            CedenteTipo.PF, CedenteStatus.ATIVO, DateTimeOffset.UtcNow);
        _queries.Query<AdminCedenteDto?>(Arg.Any<GetAdminCedenteByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.GetCedenteById(EntityId);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task GetCedenteById_HandlerReturnsNull_Returns404()
    {
        _queries.Query<AdminCedenteDto?>(Arg.Any<GetAdminCedenteByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((AdminCedenteDto?)null);

        var result = await _sut.GetCedenteById(Guid.NewGuid());

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCedenteById_PassesCorrectIdToDispatcher()
    {
        var id = Guid.NewGuid();
        GetAdminCedenteByIdQuery? captured = null;
        _queries.Query<AdminCedenteDto?>(Arg.Do<object>(q => captured = q as GetAdminCedenteByIdQuery), Arg.Any<CancellationToken>())
            .Returns((AdminCedenteDto?)null);

        await _sut.GetCedenteById(id);

        captured.ShouldNotBeNull();
        captured!.Id.ShouldBe(id);
    }
}
