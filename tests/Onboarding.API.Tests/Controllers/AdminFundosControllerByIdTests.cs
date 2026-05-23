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
/// Scenarios: handler returns dto → 200 OK; handler returns null → 404 NotFound.
/// Auth enforcement (401/403) is tested separately in integration tests.
/// </summary>
public sealed class AdminFundosControllerByIdTests
{
    // =========================================================================
    // Test fixtures
    // =========================================================================

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid EntityId = Guid.NewGuid();
    private const string EmpresaNome = "Empresa Teste Ltda";

    // Handler mocks for list endpoints (required by controller ctor)
    private readonly IQueryHandler<ListAdminFundoQuery, PaginatedResult<AdminFundoDto>> _listFundo =
        Substitute.For<IQueryHandler<ListAdminFundoQuery, PaginatedResult<AdminFundoDto>>>();
    private readonly IQueryHandler<ListAdminConsultoriaQuery, PaginatedResult<AdminConsultoriaFundoDto>> _listConsultoria =
        Substitute.For<IQueryHandler<ListAdminConsultoriaQuery, PaginatedResult<AdminConsultoriaFundoDto>>>();
    private readonly IQueryHandler<ListAdminCustodianteQuery, PaginatedResult<AdminCustodianteDto>> _listCustodiante =
        Substitute.For<IQueryHandler<ListAdminCustodianteQuery, PaginatedResult<AdminCustodianteDto>>>();
    private readonly IQueryHandler<ListAdminCedenteQuery, PaginatedResult<AdminCedenteDto>> _listCedente =
        Substitute.For<IQueryHandler<ListAdminCedenteQuery, PaginatedResult<AdminCedenteDto>>>();

    // Handler mocks for by-id endpoints (under test)
    private readonly IQueryHandler<GetAdminFundoByIdQuery, AdminFundoDto?> _getFundoById =
        Substitute.For<IQueryHandler<GetAdminFundoByIdQuery, AdminFundoDto?>>();
    private readonly IQueryHandler<GetAdminConsultoriaFundoByIdQuery, AdminConsultoriaFundoDto?> _getConsultoriaById =
        Substitute.For<IQueryHandler<GetAdminConsultoriaFundoByIdQuery, AdminConsultoriaFundoDto?>>();
    private readonly IQueryHandler<GetAdminCustodianteByIdQuery, AdminCustodianteDto?> _getCustodianteById =
        Substitute.For<IQueryHandler<GetAdminCustodianteByIdQuery, AdminCustodianteDto?>>();
    private readonly IQueryHandler<GetAdminCedenteByIdQuery, AdminCedenteDto?> _getCedenteById =
        Substitute.For<IQueryHandler<GetAdminCedenteByIdQuery, AdminCedenteDto?>>();

    // Relationship list mocks
    private readonly IQueryHandler<ListAdminFundoCedenteQuery, PaginatedResult<AdminRelFundoCedenteDto>> _listFundoCedente =
        Substitute.For<IQueryHandler<ListAdminFundoCedenteQuery, PaginatedResult<AdminRelFundoCedenteDto>>>();
    private readonly IQueryHandler<ListAdminFundoTipoAtivoQuery, PaginatedResult<AdminRelFundoTipoAtivoDto>> _listFundoTipoAtivo =
        Substitute.For<IQueryHandler<ListAdminFundoTipoAtivoQuery, PaginatedResult<AdminRelFundoTipoAtivoDto>>>();
    private readonly IQueryHandler<ListAdminCedenteTipoAtivoQuery, PaginatedResult<AdminRelCedenteTipoAtivoDto>> _listCedenteTipoAtivo =
        Substitute.For<IQueryHandler<ListAdminCedenteTipoAtivoQuery, PaginatedResult<AdminRelCedenteTipoAtivoDto>>>();

    private readonly AdminFundosController _sut;

    public AdminFundosControllerByIdTests()
    {
        _sut = new AdminFundosController(
            _listFundo, _listConsultoria, _listCustodiante, _listCedente,
            _getFundoById, _getConsultoriaById, _getCustodianteById, _getCedenteById,
            _listFundoCedente, _listFundoTipoAtivo, _listCedenteTipoAtivo)
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

        _getFundoById
            .HandleAsync(Arg.Is<GetAdminFundoByIdQuery>(q => q.Id == EntityId), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.GetFundoById(EntityId);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task GetFundoById_HandlerReturnsNull_Returns404()
    {
        _getFundoById
            .HandleAsync(Arg.Any<GetAdminFundoByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((AdminFundoDto?)null);

        var result = await _sut.GetFundoById(Guid.NewGuid());

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetFundoById_PassesCorrectIdToHandler()
    {
        var id = Guid.NewGuid();
        _getFundoById
            .HandleAsync(Arg.Any<GetAdminFundoByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((AdminFundoDto?)null);

        await _sut.GetFundoById(id);

        await _getFundoById.Received(1)
            .HandleAsync(Arg.Is<GetAdminFundoByIdQuery>(q => q.Id == id), Arg.Any<CancellationToken>());
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

        _getConsultoriaById
            .HandleAsync(Arg.Is<GetAdminConsultoriaFundoByIdQuery>(q => q.Id == EntityId), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.GetConsultoriaById(EntityId);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task GetConsultoriaById_HandlerReturnsNull_Returns404()
    {
        _getConsultoriaById
            .HandleAsync(Arg.Any<GetAdminConsultoriaFundoByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((AdminConsultoriaFundoDto?)null);

        var result = await _sut.GetConsultoriaById(Guid.NewGuid());

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetConsultoriaById_PassesCorrectIdToHandler()
    {
        var id = Guid.NewGuid();
        _getConsultoriaById
            .HandleAsync(Arg.Any<GetAdminConsultoriaFundoByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((AdminConsultoriaFundoDto?)null);

        await _sut.GetConsultoriaById(id);

        await _getConsultoriaById.Received(1)
            .HandleAsync(Arg.Is<GetAdminConsultoriaFundoByIdQuery>(q => q.Id == id), Arg.Any<CancellationToken>());
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

        _getCustodianteById
            .HandleAsync(Arg.Is<GetAdminCustodianteByIdQuery>(q => q.Id == EntityId), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.GetCustodianteById(EntityId);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task GetCustodianteById_HandlerReturnsNull_Returns404()
    {
        _getCustodianteById
            .HandleAsync(Arg.Any<GetAdminCustodianteByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((AdminCustodianteDto?)null);

        var result = await _sut.GetCustodianteById(Guid.NewGuid());

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCustodianteById_PassesCorrectIdToHandler()
    {
        var id = Guid.NewGuid();
        _getCustodianteById
            .HandleAsync(Arg.Any<GetAdminCustodianteByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((AdminCustodianteDto?)null);

        await _sut.GetCustodianteById(id);

        await _getCustodianteById.Received(1)
            .HandleAsync(Arg.Is<GetAdminCustodianteByIdQuery>(q => q.Id == id), Arg.Any<CancellationToken>());
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

        _getCedenteById
            .HandleAsync(Arg.Is<GetAdminCedenteByIdQuery>(q => q.Id == EntityId), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.GetCedenteById(EntityId);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task GetCedenteById_HandlerReturnsNull_Returns404()
    {
        _getCedenteById
            .HandleAsync(Arg.Any<GetAdminCedenteByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((AdminCedenteDto?)null);

        var result = await _sut.GetCedenteById(Guid.NewGuid());

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCedenteById_PassesCorrectIdToHandler()
    {
        var id = Guid.NewGuid();
        _getCedenteById
            .HandleAsync(Arg.Any<GetAdminCedenteByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((AdminCedenteDto?)null);

        await _sut.GetCedenteById(id);

        await _getCedenteById.Received(1)
            .HandleAsync(Arg.Is<GetAdminCedenteByIdQuery>(q => q.Id == id), Arg.Any<CancellationToken>());
    }
}
