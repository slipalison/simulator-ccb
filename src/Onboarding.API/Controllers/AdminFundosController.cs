using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Queries.Admin;

namespace Onboarding.API.Controllers;

/// <summary>
/// Cross-company read-only admin endpoints for the Fundos module (D-8).
/// Requires BearerBackoffice authentication + CrossCompanyAccess policy.
///
/// GET /api/admin/fundos                      — paginated list of all Fundos across companies
/// GET /api/admin/fundos/{id}                 — single Fundo by Id (cross-company)
/// GET /api/admin/fundos/consultorias         — paginated list of all ConsultoriaFundo across companies
/// GET /api/admin/fundos/consultorias/{id}    — single ConsultoriaFundo by Id (cross-company)
/// GET /api/admin/fundos/custodiantes         — paginated list of all Custodiante across companies
/// GET /api/admin/fundos/custodiantes/{id}    — single Custodiante by Id (cross-company)
/// GET /api/admin/fundos/cedentes             — paginated list of all Cedente across companies
/// GET /api/admin/fundos/cedentes/{id}        — single Cedente by Id (cross-company)
/// GET /api/admin/fundos/fundo-cedentes       — paginated list of all FundoCedente associations
/// GET /api/admin/fundos/fundo-tipos-ativos   — paginated list of all FundoTipoAtivo associations
/// GET /api/admin/fundos/cedente-tipos-ativos — paginated list of all CedenteTipoAtivo associations
///
/// D-62: 1 ctor dep (IQueryDispatcher) — was 11. All query-only, no mutations.
/// </summary>
[ApiController]
[Route("api/admin/fundos")]
[Authorize(AuthenticationSchemes = "BearerBackoffice", Policy = PermissionPolicies.CrossCompanyAccess)]
public sealed class AdminFundosController : ControllerBase
{
    private readonly IQueryDispatcher _queries;

    public AdminFundosController(IQueryDispatcher queries)
    {
        _queries = queries;
    }

    /// <summary>
    /// GET /api/admin/fundos/{id} — Single Fundo by Id, cross-company (D-8 fix).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminFundoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFundoById(Guid id, CancellationToken ct = default)
    {
        var result = await _queries.Query<AdminFundoDto?>(new GetAdminFundoByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/consultorias/{id} — Single ConsultoriaFundo by Id, cross-company.
    /// </summary>
    [HttpGet("consultorias/{id:guid}")]
    [ProducesResponseType(typeof(AdminConsultoriaFundoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsultoriaById(Guid id, CancellationToken ct = default)
    {
        var result = await _queries.Query<AdminConsultoriaFundoDto?>(new GetAdminConsultoriaFundoByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/custodiantes/{id} — Single Custodiante by Id, cross-company.
    /// </summary>
    [HttpGet("custodiantes/{id:guid}")]
    [ProducesResponseType(typeof(AdminCustodianteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustodianteById(Guid id, CancellationToken ct = default)
    {
        var result = await _queries.Query<AdminCustodianteDto?>(new GetAdminCustodianteByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/cedentes/{id} — Single Cedente by Id, cross-company.
    /// </summary>
    [HttpGet("cedentes/{id:guid}")]
    [ProducesResponseType(typeof(AdminCedenteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCedenteById(Guid id, CancellationToken ct = default)
    {
        var result = await _queries.Query<AdminCedenteDto?>(new GetAdminCedenteByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos — Paginated cross-company list of all Fundos.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<AdminFundoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListFundos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? companyId = null,
        CancellationToken ct = default)
    {
        var query = new ListAdminFundoQuery(page, pageSize, search, companyId);
        var result = await _queries.Query<PaginatedResult<AdminFundoDto>>(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/consultorias — Paginated cross-company list of all ConsultoriaFundo.
    /// </summary>
    [HttpGet("consultorias")]
    [ProducesResponseType(typeof(PaginatedResult<AdminConsultoriaFundoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListConsultorias(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? companyId = null,
        CancellationToken ct = default)
    {
        var query = new ListAdminConsultoriaQuery(page, pageSize, search, companyId);
        var result = await _queries.Query<PaginatedResult<AdminConsultoriaFundoDto>>(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/custodiantes — Paginated cross-company list of all Custodiante.
    /// </summary>
    [HttpGet("custodiantes")]
    [ProducesResponseType(typeof(PaginatedResult<AdminCustodianteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCustodiantes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? companyId = null,
        CancellationToken ct = default)
    {
        var query = new ListAdminCustodianteQuery(page, pageSize, search, companyId);
        var result = await _queries.Query<PaginatedResult<AdminCustodianteDto>>(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/cedentes — Paginated cross-company list of all Cedente.
    /// </summary>
    [HttpGet("cedentes")]
    [ProducesResponseType(typeof(PaginatedResult<AdminCedenteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCedentes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? companyId = null,
        CancellationToken ct = default)
    {
        var query = new ListAdminCedenteQuery(page, pageSize, search, companyId);
        var result = await _queries.Query<PaginatedResult<AdminCedenteDto>>(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/fundo-cedentes — Paginated cross-company FundoCedente associations.
    /// </summary>
    [HttpGet("fundo-cedentes")]
    [ProducesResponseType(typeof(PaginatedResult<AdminRelFundoCedenteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListFundoCedentes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? companyId = null,
        CancellationToken ct = default)
    {
        var query = new ListAdminFundoCedenteQuery(page, pageSize, companyId);
        var result = await _queries.Query<PaginatedResult<AdminRelFundoCedenteDto>>(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/fundo-tipos-ativos — Paginated cross-company FundoTipoAtivo associations.
    /// </summary>
    [HttpGet("fundo-tipos-ativos")]
    [ProducesResponseType(typeof(PaginatedResult<AdminRelFundoTipoAtivoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListFundoTiposAtivos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? companyId = null,
        CancellationToken ct = default)
    {
        var query = new ListAdminFundoTipoAtivoQuery(page, pageSize, companyId);
        var result = await _queries.Query<PaginatedResult<AdminRelFundoTipoAtivoDto>>(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/cedente-tipos-ativos — Paginated cross-company CedenteTipoAtivo associations.
    /// </summary>
    [HttpGet("cedente-tipos-ativos")]
    [ProducesResponseType(typeof(PaginatedResult<AdminRelCedenteTipoAtivoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCedenteTiposAtivos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? companyId = null,
        CancellationToken ct = default)
    {
        var query = new ListAdminCedenteTipoAtivoQuery(page, pageSize, companyId);
        var result = await _queries.Query<PaginatedResult<AdminRelCedenteTipoAtivoDto>>(query, ct);
        return Ok(result);
    }
}
