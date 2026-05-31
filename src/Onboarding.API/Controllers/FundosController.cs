using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.API.Extensions;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries;
using Onboarding.Application.Fundos.Queries.GetFundoAllowedTransitions;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.API.Controllers;

/// <summary>
/// Fundos module endpoints — company-scoped (ConsultoriaFundo, Custodiante, Fundo, Cedente) and global (TipoAtivo).
/// POST   /api/fundos/consultorias              CAD-01: register ConsultoriaFundo
/// GET    /api/fundos/consultorias              CAD-02: list ConsultoriaFundo (paginated)
/// GET    /api/fundos/consultorias/{id}         CAD-02b: get ConsultoriaFundo by id
/// PUT    /api/fundos/consultorias/{id}         CAD-03: update ConsultoriaFundo
/// POST   /api/fundos/custodiantes              CAD-05: register Custodiante
/// GET    /api/fundos/custodiantes              CAD-06: list Custodiante (paginated)
/// GET    /api/fundos/custodiantes/{id}         CAD-06b: get Custodiante by id
/// PUT    /api/fundos/custodiantes/{id}         CAD-07: update Custodiante
/// POST   /api/fundos/tipos-ativo               CAD-19: create TipoAtivo (global)
/// GET    /api/fundos/tipos-ativo               CAD-20: list TipoAtivo (paginated, global)
/// GET    /api/fundos/tipos-ativo/{id}          CAD-20b: get TipoAtivo by id (global)
/// PUT    /api/fundos/tipos-ativo/{id}          CAD-21: update TipoAtivo (global)
/// POST   /api/fundos                           CAD-09: register Fundo (RASCUNHO)
/// GET    /api/fundos                           CAD-10: list Fundo (paginated)
/// GET    /api/fundos/{id}                      CAD-10b: get Fundo by id
/// PUT    /api/fundos/{id}                      CAD-11: update Fundo
/// POST   /api/fundos/{id}/status               CAD-13: transition Fundo status (D-9)
/// POST   /api/fundos/cedentes/pf               CAD-14: register Cedente PF
/// POST   /api/fundos/cedentes/pj               CAD-15: register Cedente PJ
/// GET    /api/fundos/cedentes                  CAD-16: list Cedente (paginated)
/// GET    /api/fundos/cedentes/{id}             CAD-16b: get Cedente by id
/// PUT    /api/fundos/cedentes/{id}             CAD-17: update Cedente
/// </summary>
[ApiController]
[Route("api/fundos")]
[Authorize(AuthenticationSchemes = "BearerClient")]
public sealed class FundosController : ControllerBase
{
    // D-60/D-61/D-62: 4 ctor deps (was 37). Repos used only for GetById tenant guards
    // are injected via [FromServices] on the specific action methods.
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;
    private readonly IValidationRunner _validation;
    private readonly ICurrentCompanyService _currentCompanyService;

    public FundosController(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        IValidationRunner validation,
        ICurrentCompanyService currentCompanyService)
    {
        _commands = commands;
        _queries = queries;
        _validation = validation;
        _currentCompanyService = currentCompanyService;
    }

    // =========================================================================
    // ConsultoriaFundo — company-scoped (D-5, HasQueryFilter)
    // =========================================================================

    /// <summary>POST /api/fundos/consultorias — Register a new ConsultoriaFundo (CAD-01).</summary>
    [HttpPost("consultorias")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(ConsultoriaFundoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterConsultoria(
        [FromBody] RegisterConsultoriaFundoRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        var (actorSub, actorEmail) = GetActorContext();

        var command = new RegisterConsultoriaFundoCommand(
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            Cnpj: request.Cnpj ?? string.Empty,
            NomeFantasia: string.IsNullOrWhiteSpace(request.NomeFantasia) ? null : request.NomeFantasia,
            Email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Telefone: string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _commands.Send<ConsultoriaFundoDto>(command, ct);
            return CreatedAtAction(nameof(GetConsultoriaById), new { id = result.Id }, result);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Status = 409, Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = ex.Message });
        }
    }

    /// <summary>GET /api/fundos/consultorias — Paginated ConsultoriaFundo listing (CAD-02).</summary>
    [HttpGet("consultorias")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(PaginatedResult<ConsultoriaFundoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListConsultorias(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = new ListConsultoriaFundoQuery(page, pageSize, search);
        var result = await _queries.Query<PaginatedResult<ConsultoriaFundoDto>>(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/consultorias/{id} — Get ConsultoriaFundo by id (CAD-02b).</summary>
    [HttpGet("consultorias/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(ConsultoriaFundoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsultoriaById(
        Guid id,
        [FromServices] IConsultoriaFundoRepository consultoriaRepository,
        CancellationToken ct)
    {
        var consultoria = await consultoriaRepository.GetByIdAsync(id, ct);
        // Security: IgnoreQueryFilters in repo returns cross-tenant rows; enforce tenant boundary
        // here. Return NotFound (not Forbid) — do not leak entity existence to other tenants.
        if (consultoria is null || consultoria.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        return Ok(new ConsultoriaFundoDto(
            consultoria.Id,
            consultoria.RazaoSocial,
            consultoria.NomeFantasia,
            consultoria.Cnpj.Value,
            consultoria.Email?.Value,
            consultoria.Telefone?.Value,
            consultoria.Status,
            consultoria.CreatedAt));
    }

    /// <summary>PUT /api/fundos/consultorias/{id} — Update ConsultoriaFundo (CAD-03).</summary>
    [HttpPut("consultorias/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(ConsultoriaFundoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateConsultoria(
        Guid id, [FromBody] UpdateConsultoriaFundoRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        var (actorSub, actorEmail) = GetActorContext();

        var command = new UpdateConsultoriaFundoCommand(
            Id: id,
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            NomeFantasia: string.IsNullOrWhiteSpace(request.NomeFantasia) ? null : request.NomeFantasia,
            Email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Telefone: string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone,
            Status: request.Status,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _commands.Send<ConsultoriaFundoDto>(command, ct);
            return Ok(result);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Status = 409, Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = ex.Message });
        }
    }

    // =========================================================================
    // Custodiante — company-scoped (D-5, HasQueryFilter)
    // =========================================================================

    /// <summary>POST /api/fundos/custodiantes — Register a new Custodiante (CAD-05).</summary>
    [HttpPost("custodiantes")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(CustodianteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterCustodiante(
        [FromBody] RegisterCustodianteRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        var (actorSub, actorEmail) = GetActorContext();

        var command = new RegisterCustodianteCommand(
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            Cnpj: request.Cnpj ?? string.Empty,
            CodigoInterno: string.IsNullOrWhiteSpace(request.CodigoInterno) ? null : request.CodigoInterno,
            Email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Telefone: string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _commands.Send<CustodianteDto>(command, ct);
            return CreatedAtAction(nameof(GetCustodianteById), new { id = result.Id }, result);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Status = 409, Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = ex.Message });
        }
    }

    /// <summary>GET /api/fundos/custodiantes — Paginated Custodiante listing (CAD-06).</summary>
    [HttpGet("custodiantes")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(PaginatedResult<CustodianteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListCustodiantes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = new ListCustodianteQuery(page, pageSize, search);
        var result = await _queries.Query<PaginatedResult<CustodianteDto>>(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/custodiantes/{id} — Get Custodiante by id (CAD-06b).</summary>
    [HttpGet("custodiantes/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(CustodianteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustodianteById(
        Guid id,
        [FromServices] ICustodianteRepository custodianteRepository,
        CancellationToken ct)
    {
        var custodiante = await custodianteRepository.GetByIdAsync(id, ct);
        // Security: IgnoreQueryFilters in repo returns cross-tenant rows; enforce tenant boundary
        // here. Return NotFound (not Forbid) — do not leak entity existence to other tenants.
        if (custodiante is null || custodiante.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        return Ok(new CustodianteDto(
            custodiante.Id,
            custodiante.RazaoSocial,
            custodiante.CodigoInterno,
            custodiante.Cnpj.Value,
            custodiante.Email?.Value,
            custodiante.Telefone?.Value,
            custodiante.Status,
            custodiante.CreatedAt));
    }

    /// <summary>PUT /api/fundos/custodiantes/{id} — Update Custodiante (CAD-07).</summary>
    [HttpPut("custodiantes/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(CustodianteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateCustodiante(
        Guid id, [FromBody] UpdateCustodianteRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        var (actorSub, actorEmail) = GetActorContext();

        var command = new UpdateCustodianteCommand(
            Id: id,
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            CodigoInterno: string.IsNullOrWhiteSpace(request.CodigoInterno) ? null : request.CodigoInterno,
            Email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Telefone: string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone,
            Status: request.Status,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _commands.Send<CustodianteDto>(command, ct);
            return Ok(result);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Status = 409, Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = ex.Message });
        }
    }

    // =========================================================================
    // TipoAtivo — global scope (D-5/TEN-03: no HasQueryFilter, no CompanyId)
    // =========================================================================

    /// <summary>POST /api/fundos/tipos-ativo — Create TipoAtivo in global catalog (CAD-19).</summary>
    [HttpPost("tipos-ativo")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(TipoAtivoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateTipoAtivo(
        [FromBody] CreateTipoAtivoRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        // TipoAtivo is global — actor captured from JWT only (no ICurrentCompanyService for company scope)
        var (actorSub, actorEmail) = GetActorContext();

        var command = new CreateTipoAtivoCommand(
            Codigo: request.Codigo ?? string.Empty,
            Descricao: request.Descricao ?? string.Empty,
            Categoria: request.Categoria,
            Subcategoria: request.Subcategoria,
            OrdemExibicao: request.OrdemExibicao,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _commands.Send<TipoAtivoDto>(command, ct);
            return CreatedAtAction(nameof(GetTipoAtivoById), new { id = result.Id }, result);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Status = 409, Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = ex.Message });
        }
    }

    /// <summary>GET /api/fundos/tipos-ativo — Paginated TipoAtivo listing (global, CAD-20).</summary>
    [HttpGet("tipos-ativo")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(PaginatedResult<TipoAtivoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListTiposAtivo(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = new ListTipoAtivoQuery(page, pageSize, search);
        var result = await _queries.Query<PaginatedResult<TipoAtivoDto>>(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/tipos-ativo/{id} — Get TipoAtivo by id (global, CAD-20b).</summary>
    [HttpGet("tipos-ativo/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(TipoAtivoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTipoAtivoById(
        Guid id,
        [FromServices] ITipoAtivoRepository tipoAtivoRepository,
        CancellationToken ct)
    {
        var tipoAtivo = await tipoAtivoRepository.GetByIdAsync(id, ct);
        if (tipoAtivo is null)
            return NotFound();

        return Ok(new TipoAtivoDto(
            tipoAtivo.Id,
            tipoAtivo.Codigo,
            tipoAtivo.Descricao,
            tipoAtivo.Categoria,
            tipoAtivo.Subcategoria,
            tipoAtivo.Status,
            tipoAtivo.OrdemExibicao));
    }

    /// <summary>PUT /api/fundos/tipos-ativo/{id} — Update TipoAtivo (global, CAD-21).</summary>
    [HttpPut("tipos-ativo/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(TipoAtivoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateTipoAtivo(
        Guid id, [FromBody] UpdateTipoAtivoRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        var (actorSub, actorEmail) = GetActorContext();

        var command = new UpdateTipoAtivoCommand(
            Id: id,
            Descricao: request.Descricao ?? string.Empty,
            Subcategoria: request.Subcategoria,
            Status: request.Status,
            OrdemExibicao: request.OrdemExibicao,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _commands.Send<TipoAtivoDto>(command, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = ex.Message });
        }
    }

    // =========================================================================
    // Fundo — company-scoped (D-5, HasQueryFilter)
    // =========================================================================

    /// <summary>POST /api/fundos — Register a new Fundo (CAD-09). Status starts as RASCUNHO.</summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(FundoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterFundo(
        [FromBody] RegisterFundoRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        var (actorSub, actorEmail) = GetActorContext();

        var command = new RegisterFundoCommand(
            Nome: request.Nome ?? string.Empty,
            Cnpj: request.Cnpj ?? string.Empty,
            ConsultoriaFundoId: request.ConsultoriaFundoId,
            CustodianteId: request.CustodianteId,
            TipoFundo: request.TipoFundo,
            ClasseAnbima: request.ClasseAnbima,
            Segmento: request.Segmento,
            DataConstituicao: request.DataConstituicao,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _commands.Send<FundoDto>(command, ct);
            return CreatedAtAction(nameof(GetFundoById), new { id = result.Id }, result);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Status = 409, Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = ex.Message });
        }
    }

    /// <summary>GET /api/fundos — Paginated Fundo listing (CAD-10).</summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(PaginatedResult<FundoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListFundos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = new ListFundoQuery(page, pageSize, search);
        var result = await _queries.Query<PaginatedResult<FundoDto>>(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/{id} — Get Fundo by id (CAD-10b).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(FundoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFundoById(
        Guid id,
        [FromServices] IFundoRepository fundoRepository,
        CancellationToken ct)
    {
        var fundo = await fundoRepository.GetByIdAsync(id, ct);
        // Security: IgnoreQueryFilters in repo returns cross-tenant rows; enforce tenant boundary
        // here. Return NotFound (not Forbid) — do not leak entity existence to other tenants.
        if (fundo is null || fundo.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        return Ok(new FundoDto(
            fundo.Id,
            fundo.Nome,
            fundo.Cnpj.Value,
            fundo.ConsultoriaFundoId,
            fundo.CustodianteId,
            fundo.TipoFundo,
            fundo.ClasseAnbima,
            fundo.Segmento,
            fundo.DataConstituicao,
            fundo.Status,
            fundo.CreatedAt));
    }

    /// <summary>PUT /api/fundos/{id} — Update Fundo mutable data (CAD-11). Status transition is separate.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(FundoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateFundo(
        Guid id, [FromBody] UpdateFundoRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        var (actorSub, actorEmail) = GetActorContext();

        var command = new UpdateFundoCommand(
            Id: id,
            Nome: request.Nome ?? string.Empty,
            ClasseAnbima: request.ClasseAnbima,
            Segmento: request.Segmento,
            DataConstituicao: request.DataConstituicao,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _commands.Send<FundoDto>(command, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/fundos/{id}/status — Transition Fundo status (CAD-13, D-9).
    /// Body: { NewStatus } only. Invalid transitions → 400 BadRequest with from/to detail.
    /// </summary>
    [HttpPost("{id:guid}/status")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(FundoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransitionFundoStatus(
        Guid id,
        [FromBody] TransitionFundoStatusRequest? request,
        [FromServices] IFundoRepository fundoRepository,
        CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        // Security: IgnoreQueryFilters in repo returns cross-tenant rows; enforce tenant boundary
        // here. Return NotFound (not Forbid) — do not leak entity existence to other tenants.
        var fundo = await fundoRepository.GetByIdAsync(id, ct);
        if (fundo is null || fundo.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        var (actorSub, actorEmail) = GetActorContext();

        var command = new TransitionFundoStatusCommand(
            FundoId: id,
            NewStatus: request.NewStatus,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _commands.Send<FundoDto>(command, ct);
            return Ok(result);
        }
        catch (InvalidStateTransitionException ex)
        {
            // Explicit 400 with from/to detail per acceptance criteria
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid status transition",
                Status = 400,
                Detail = $"Cannot transition Fundo from {ex.From} to {ex.To}. {ex.Message}"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/fundos/{id}/allowed-transitions — Returns valid next statuses for a Fundo (D-25).
    /// 200 OK with string[]; 404 if not found or cross-tenant.
    /// </summary>
    [HttpGet("{id:guid}/allowed-transitions")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFundoAllowedTransitions(Guid id, CancellationToken ct)
    {
        var query = new GetFundoAllowedTransitionsQuery(id);
        var result = await _queries.Query<IReadOnlyList<string>?>(query, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    // =========================================================================
    // Cedente — company-scoped (D-5, D-10: composite unique (ClientId, Cpf/Cnpj))
    // =========================================================================

    /// <summary>POST /api/fundos/cedentes/pf — Register a new Cedente PF with CPF (CAD-14).</summary>
    [HttpPost("cedentes/pf")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(CedenteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterCedentePf(
        [FromBody] RegisterCedentePfRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        var (actorSub, actorEmail) = GetActorContext();

        var command = new RegisterCedentePfCommand(
            Cpf: request.Cpf ?? string.Empty,
            Nome: request.Nome ?? string.Empty,
            Email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Telefone: string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone,
            Endereco: request.Endereco,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _commands.Send<CedenteDto>(command, ct);
            return CreatedAtAction(nameof(GetCedenteById), new { id = result.Id }, result);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Status = 409, Detail = ex.Message });
        }
    }

    /// <summary>POST /api/fundos/cedentes/pj — Register a new Cedente PJ with CNPJ (CAD-15).</summary>
    [HttpPost("cedentes/pj")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(CedenteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterCedentePj(
        [FromBody] RegisterCedentePjRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        var (actorSub, actorEmail) = GetActorContext();

        var command = new RegisterCedentePjCommand(
            Cnpj: request.Cnpj ?? string.Empty,
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            Email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Telefone: string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone,
            Endereco: request.Endereco,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _commands.Send<CedenteDto>(command, ct);
            return CreatedAtAction(nameof(GetCedenteById), new { id = result.Id }, result);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Status = 409, Detail = ex.Message });
        }
    }

    /// <summary>GET /api/fundos/cedentes — Paginated Cedente listing (CAD-16).</summary>
    [HttpGet("cedentes")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(PaginatedResult<CedenteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListCedentes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = new ListCedenteQuery(page, pageSize, search);
        var result = await _queries.Query<PaginatedResult<CedenteDto>>(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/cedentes/{id} — Get Cedente by id (CAD-16b).</summary>
    [HttpGet("cedentes/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(CedenteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCedenteById(
        Guid id,
        [FromServices] ICedenteRepository cedenteRepository,
        CancellationToken ct)
    {
        var cedente = await cedenteRepository.GetByIdAsync(id, ct);
        // Security: IgnoreQueryFilters in repo returns cross-tenant rows; enforce tenant boundary
        // here. Return NotFound (not Forbid) — do not leak entity existence to other tenants.
        if (cedente is null || cedente.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        return Ok(new CedenteDto(
            cedente.Id,
            cedente.Documento.Match(
                pf => pf.Cpf.Value,
                pj => pj.Cnpj.Value),
            cedente.Nome,
            cedente.Email?.Value,
            cedente.Telefone?.Value,
            cedente.Endereco,
            cedente.Documento.IsPf ? CedenteTipo.PF : CedenteTipo.PJ,
            cedente.Status,
            cedente.CreatedAt));
    }

    /// <summary>PUT /api/fundos/cedentes/{id} — Update Cedente data (CAD-17).</summary>
    [HttpPut("cedentes/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(CedenteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateCedente(
        Guid id, [FromBody] UpdateCedenteRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        var (actorSub, actorEmail) = GetActorContext();

        var command = new UpdateCedenteCommand(
            Id: id,
            Nome: request.Nome ?? string.Empty,
            Email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Telefone: string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone,
            Endereco: request.Endereco,
            Status: request.Status,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _commands.Send<CedenteDto>(command, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = ex.Message });
        }
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>Extracts ActorSub and ActorEmail from the authenticated JWT for audit trail.</summary>
    private (string ActorSub, string ActorEmail) GetActorContext()
        => (User.FindFirst("sub")?.Value ?? string.Empty,
            User.FindFirst("email")?.Value ?? string.Empty);

    /// <summary>
    /// Converts FluentValidation results to 422 ValidationProblemDetails.
    /// Delegates to shared <see cref="ValidationExtensions.ToValidationProblem"/> (DRY-01).
    /// </summary>
    private static ValidationProblemDetails ToValidationProblem(FluentValidation.Results.ValidationResult result)
        => result.ToValidationProblem();
}

// =========================================================================
// Request DTOs — defined here to keep the controller file self-contained
// =========================================================================

/// <summary>Request DTO for POST /api/fundos/consultorias.</summary>
public sealed record RegisterConsultoriaFundoRequest(
    string? RazaoSocial,
    string? Cnpj,
    string? NomeFantasia,
    string? Email,
    string? Telefone);

/// <summary>Request DTO for PUT /api/fundos/consultorias/{id}.</summary>
public sealed record UpdateConsultoriaFundoRequest(
    string? RazaoSocial,
    string? NomeFantasia,
    string? Email,
    string? Telefone,
    ConsultoriaFundoStatus Status);

/// <summary>Request DTO for POST /api/fundos/custodiantes.</summary>
public sealed record RegisterCustodianteRequest(
    string? RazaoSocial,
    string? Cnpj,
    string? CodigoInterno,
    string? Email,
    string? Telefone);

/// <summary>Request DTO for PUT /api/fundos/custodiantes/{id}.</summary>
public sealed record UpdateCustodianteRequest(
    string? RazaoSocial,
    string? CodigoInterno,
    string? Email,
    string? Telefone,
    CustodianteStatus Status);

/// <summary>Request DTO for POST /api/fundos/tipos-ativo.</summary>
public sealed record CreateTipoAtivoRequest(
    string? Codigo,
    string? Descricao,
    TipoAtivoCategoria Categoria,
    string? Subcategoria,
    int OrdemExibicao = 0);

/// <summary>Request DTO for PUT /api/fundos/tipos-ativo/{id}.</summary>
public sealed record UpdateTipoAtivoRequest(
    string? Descricao,
    string? Subcategoria,
    TipoAtivoStatus Status,
    int OrdemExibicao);

/// <summary>Request DTO for POST /api/fundos.</summary>
public sealed record RegisterFundoRequest(
    string? Nome,
    string? Cnpj,
    Guid ConsultoriaFundoId,
    Guid CustodianteId,
    TipoFundo TipoFundo,
    string? ClasseAnbima,
    string? Segmento,
    DateTimeOffset? DataConstituicao);

/// <summary>Request DTO for PUT /api/fundos/{id}.</summary>
public sealed record UpdateFundoRequest(
    string? Nome,
    string? ClasseAnbima,
    string? Segmento,
    DateTimeOffset? DataConstituicao);

/// <summary>Request DTO for POST /api/fundos/{id}/status (D-9 — minimal body).</summary>
public sealed record TransitionFundoStatusRequest(FundoStatus NewStatus);

/// <summary>Request DTO for POST /api/fundos/cedentes/pf.</summary>
public sealed record RegisterCedentePfRequest(
    string? Cpf,
    string? Nome,
    string? Email,
    string? Telefone,
    string? Endereco);

/// <summary>Request DTO for POST /api/fundos/cedentes/pj.</summary>
public sealed record RegisterCedentePjRequest(
    string? Cnpj,
    string? RazaoSocial,
    string? Email,
    string? Telefone,
    string? Endereco);

/// <summary>Request DTO for PUT /api/fundos/cedentes/{id}.</summary>
public sealed record UpdateCedenteRequest(
    string? Nome,
    string? Email,
    string? Telefone,
    string? Endereco,
    CedenteStatus Status);
