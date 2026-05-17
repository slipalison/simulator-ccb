using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Queries.Admin;

namespace Onboarding.API.Controllers;

/// <summary>
/// Cross-company read-only admin endpoints for the Fundos module (D-8).
/// Requires BearerBackoffice authentication + CrossCompanyAccess policy.
/// Scope is strictly read-only list operations — NO detail-by-id, NO mutations, NO admin overrides.
///
/// GET /api/admin/fundos                      — paginated list of all Fundos across companies
/// GET /api/admin/fundos/consultorias         — paginated list of all ConsultoriaFundo across companies
/// GET /api/admin/fundos/custodiantes         — paginated list of all Custodiante across companies
/// GET /api/admin/fundos/cedentes             — paginated list of all Cedente across companies
/// GET /api/admin/fundos/fundo-cedentes       — paginated list of all FundoCedente associations
/// GET /api/admin/fundos/fundo-tipos-ativos   — paginated list of all FundoTipoAtivo associations
/// GET /api/admin/fundos/cedente-tipos-ativos — paginated list of all CedenteTipoAtivo associations
/// </summary>
[ApiController]
[Route("api/admin/fundos")]
[Authorize(AuthenticationSchemes = "BearerBackoffice", Policy = PermissionPolicies.CrossCompanyAccess)]
public sealed class AdminFundosController : ControllerBase
{
    private readonly IQueryHandler<ListAdminFundoQuery, PaginatedResult<AdminFundoDto>> _listFundoHandler;
    private readonly IQueryHandler<ListAdminConsultoriaQuery, PaginatedResult<AdminConsultoriaFundoDto>> _listConsultoriaHandler;
    private readonly IQueryHandler<ListAdminCustodianteQuery, PaginatedResult<AdminCustodianteDto>> _listCustodianteHandler;
    private readonly IQueryHandler<ListAdminCedenteQuery, PaginatedResult<AdminCedenteDto>> _listCedenteHandler;

    // Phase 50 — relationship aggregate admin queries (D-8, D-21)
    private readonly IQueryHandler<ListAdminFundoCedenteQuery, PaginatedResult<AdminRelFundoCedenteDto>> _listFundoCedenteHandler;
    private readonly IQueryHandler<ListAdminFundoTipoAtivoQuery, PaginatedResult<AdminRelFundoTipoAtivoDto>> _listFundoTipoAtivoHandler;
    private readonly IQueryHandler<ListAdminCedenteTipoAtivoQuery, PaginatedResult<AdminRelCedenteTipoAtivoDto>> _listCedenteTipoAtivoHandler;

    public AdminFundosController(
        IQueryHandler<ListAdminFundoQuery, PaginatedResult<AdminFundoDto>> listFundoHandler,
        IQueryHandler<ListAdminConsultoriaQuery, PaginatedResult<AdminConsultoriaFundoDto>> listConsultoriaHandler,
        IQueryHandler<ListAdminCustodianteQuery, PaginatedResult<AdminCustodianteDto>> listCustodianteHandler,
        IQueryHandler<ListAdminCedenteQuery, PaginatedResult<AdminCedenteDto>> listCedenteHandler,
        IQueryHandler<ListAdminFundoCedenteQuery, PaginatedResult<AdminRelFundoCedenteDto>> listFundoCedenteHandler,
        IQueryHandler<ListAdminFundoTipoAtivoQuery, PaginatedResult<AdminRelFundoTipoAtivoDto>> listFundoTipoAtivoHandler,
        IQueryHandler<ListAdminCedenteTipoAtivoQuery, PaginatedResult<AdminRelCedenteTipoAtivoDto>> listCedenteTipoAtivoHandler)
    {
        _listFundoHandler = listFundoHandler;
        _listConsultoriaHandler = listConsultoriaHandler;
        _listCustodianteHandler = listCustodianteHandler;
        _listCedenteHandler = listCedenteHandler;
        _listFundoCedenteHandler = listFundoCedenteHandler;
        _listFundoTipoAtivoHandler = listFundoTipoAtivoHandler;
        _listCedenteTipoAtivoHandler = listCedenteTipoAtivoHandler;
    }

    /// <summary>
    /// GET /api/admin/fundos — Paginated cross-company list of all Fundos.
    /// Each item includes ClienteId + EmpresaNome (from Company join) for tenant identification.
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
        var result = await _listFundoHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/consultorias — Paginated cross-company list of all ConsultoriaFundo.
    /// Each item includes ClienteId + EmpresaNome (from Company join) for tenant identification.
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
        var result = await _listConsultoriaHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/custodiantes — Paginated cross-company list of all Custodiante.
    /// Each item includes ClienteId + EmpresaNome (from Company join) for tenant identification.
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
        var result = await _listCustodianteHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/cedentes — Paginated cross-company list of all Cedente.
    /// Each item includes ClienteId + EmpresaNome (from Company join) for tenant identification.
    /// Documento field contains CPF (PF) or CNPJ (PJ) string; CedenteTipo indicates which.
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
        var result = await _listCedenteHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/fundo-cedentes — Paginated cross-company list of all FundoCedente associations (D-21).
    /// Each item includes ClienteId + EmpresaNome from Fundo→Company join.
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
        var result = await _listFundoCedenteHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/fundo-tipos-ativos — Paginated cross-company list of all FundoTipoAtivo associations (D-21).
    /// Each item includes ClienteId + EmpresaNome from Fundo→Company join.
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
        var result = await _listFundoTipoAtivoHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/admin/fundos/cedente-tipos-ativos — Paginated cross-company list of all CedenteTipoAtivo associations (D-21).
    /// Each item includes ClienteId + EmpresaNome from Cedente→Company join.
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
        var result = await _listCedenteTipoAtivoHandler.HandleAsync(query, ct);
        return Ok(result);
    }
}
