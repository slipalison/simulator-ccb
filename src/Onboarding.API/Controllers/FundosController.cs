using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries;
using Onboarding.Application.Fundos.Queries.GetFundoAllowedTransitions;
using Onboarding.API.Security;
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
    // ConsultoriaFundo
    private readonly ICommandHandler<RegisterConsultoriaFundoCommand, ConsultoriaFundoDto> _registerConsultoriaHandler;
    private readonly IValidator<RegisterConsultoriaFundoCommand> _registerConsultoriaValidator;
    private readonly ICommandHandler<UpdateConsultoriaFundoCommand, ConsultoriaFundoDto> _updateConsultoriaHandler;
    private readonly IValidator<UpdateConsultoriaFundoCommand> _updateConsultoriaValidator;
    private readonly IQueryHandler<ListConsultoriaFundoQuery, PaginatedResult<ConsultoriaFundoDto>> _listConsultoriaHandler;
    private readonly IConsultoriaFundoRepository _consultoriaRepository;

    // Custodiante
    private readonly ICommandHandler<RegisterCustodianteCommand, CustodianteDto> _registerCustodianteHandler;
    private readonly IValidator<RegisterCustodianteCommand> _registerCustodianteValidator;
    private readonly ICommandHandler<UpdateCustodianteCommand, CustodianteDto> _updateCustodianteHandler;
    private readonly IValidator<UpdateCustodianteCommand> _updateCustodianteValidator;
    private readonly IQueryHandler<ListCustodianteQuery, PaginatedResult<CustodianteDto>> _listCustodianteHandler;
    private readonly ICustodianteRepository _custodianteRepository;

    // TipoAtivo (global — no company scope per D-5/TEN-03)
    private readonly ICommandHandler<CreateTipoAtivoCommand, TipoAtivoDto> _createTipoAtivoHandler;
    private readonly IValidator<CreateTipoAtivoCommand> _createTipoAtivoValidator;
    private readonly ICommandHandler<UpdateTipoAtivoCommand, TipoAtivoDto> _updateTipoAtivoHandler;
    private readonly IValidator<UpdateTipoAtivoCommand> _updateTipoAtivoValidator;
    private readonly IQueryHandler<ListTipoAtivoQuery, PaginatedResult<TipoAtivoDto>> _listTipoAtivoHandler;
    private readonly ITipoAtivoRepository _tipoAtivoRepository;

    // Fundo — company-scoped (D-5, HasQueryFilter)
    private readonly ICommandHandler<RegisterFundoCommand, FundoDto> _registerFundoHandler;
    private readonly IValidator<RegisterFundoCommand> _registerFundoValidator;
    private readonly ICommandHandler<UpdateFundoCommand, FundoDto> _updateFundoHandler;
    private readonly IValidator<UpdateFundoCommand> _updateFundoValidator;
    private readonly ICommandHandler<TransitionFundoStatusCommand, FundoDto> _transitionFundoStatusHandler;
    private readonly IValidator<TransitionFundoStatusCommand> _transitionFundoStatusValidator;
    private readonly IQueryHandler<ListFundoQuery, PaginatedResult<FundoDto>> _listFundoHandler;
    private readonly IFundoRepository _fundoRepository;

    // Cedente — company-scoped (D-5, D-10: composite unique index (ClientId, Cpf/Cnpj))
    private readonly ICommandHandler<RegisterCedentePfCommand, CedenteDto> _registerCedentePfHandler;
    private readonly IValidator<RegisterCedentePfCommand> _registerCedentePfValidator;
    private readonly ICommandHandler<RegisterCedentePjCommand, CedenteDto> _registerCedentePjHandler;
    private readonly IValidator<RegisterCedentePjCommand> _registerCedentePjValidator;
    private readonly ICommandHandler<UpdateCedenteCommand, CedenteDto> _updateCedenteHandler;
    private readonly IValidator<UpdateCedenteCommand> _updateCedenteValidator;
    private readonly IQueryHandler<ListCedenteQuery, PaginatedResult<CedenteDto>> _listCedenteHandler;
    private readonly ICedenteRepository _cedenteRepository;

    // Allowed transitions query (D-25)
    private readonly IQueryHandler<GetFundoAllowedTransitionsQuery, IReadOnlyList<string>?> _allowedTransitionsHandler;

    // Cross-cutting
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly ILogger<FundosController> _logger;

    public FundosController(
        ICommandHandler<RegisterConsultoriaFundoCommand, ConsultoriaFundoDto> registerConsultoriaHandler,
        IValidator<RegisterConsultoriaFundoCommand> registerConsultoriaValidator,
        ICommandHandler<UpdateConsultoriaFundoCommand, ConsultoriaFundoDto> updateConsultoriaHandler,
        IValidator<UpdateConsultoriaFundoCommand> updateConsultoriaValidator,
        IQueryHandler<ListConsultoriaFundoQuery, PaginatedResult<ConsultoriaFundoDto>> listConsultoriaHandler,
        IConsultoriaFundoRepository consultoriaRepository,
        ICommandHandler<RegisterCustodianteCommand, CustodianteDto> registerCustodianteHandler,
        IValidator<RegisterCustodianteCommand> registerCustodianteValidator,
        ICommandHandler<UpdateCustodianteCommand, CustodianteDto> updateCustodianteHandler,
        IValidator<UpdateCustodianteCommand> updateCustodianteValidator,
        IQueryHandler<ListCustodianteQuery, PaginatedResult<CustodianteDto>> listCustodianteHandler,
        ICustodianteRepository custodianteRepository,
        ICommandHandler<CreateTipoAtivoCommand, TipoAtivoDto> createTipoAtivoHandler,
        IValidator<CreateTipoAtivoCommand> createTipoAtivoValidator,
        ICommandHandler<UpdateTipoAtivoCommand, TipoAtivoDto> updateTipoAtivoHandler,
        IValidator<UpdateTipoAtivoCommand> updateTipoAtivoValidator,
        IQueryHandler<ListTipoAtivoQuery, PaginatedResult<TipoAtivoDto>> listTipoAtivoHandler,
        ITipoAtivoRepository tipoAtivoRepository,
        ICommandHandler<RegisterFundoCommand, FundoDto> registerFundoHandler,
        IValidator<RegisterFundoCommand> registerFundoValidator,
        ICommandHandler<UpdateFundoCommand, FundoDto> updateFundoHandler,
        IValidator<UpdateFundoCommand> updateFundoValidator,
        ICommandHandler<TransitionFundoStatusCommand, FundoDto> transitionFundoStatusHandler,
        IValidator<TransitionFundoStatusCommand> transitionFundoStatusValidator,
        IQueryHandler<ListFundoQuery, PaginatedResult<FundoDto>> listFundoHandler,
        IFundoRepository fundoRepository,
        ICommandHandler<RegisterCedentePfCommand, CedenteDto> registerCedentePfHandler,
        IValidator<RegisterCedentePfCommand> registerCedentePfValidator,
        ICommandHandler<RegisterCedentePjCommand, CedenteDto> registerCedentePjHandler,
        IValidator<RegisterCedentePjCommand> registerCedentePjValidator,
        ICommandHandler<UpdateCedenteCommand, CedenteDto> updateCedenteHandler,
        IValidator<UpdateCedenteCommand> updateCedenteValidator,
        IQueryHandler<ListCedenteQuery, PaginatedResult<CedenteDto>> listCedenteHandler,
        ICedenteRepository cedenteRepository,
        IQueryHandler<GetFundoAllowedTransitionsQuery, IReadOnlyList<string>?> allowedTransitionsHandler,
        ICurrentCompanyService currentCompanyService,
        ILogger<FundosController> logger)
    {
        _registerConsultoriaHandler = registerConsultoriaHandler;
        _registerConsultoriaValidator = registerConsultoriaValidator;
        _updateConsultoriaHandler = updateConsultoriaHandler;
        _updateConsultoriaValidator = updateConsultoriaValidator;
        _listConsultoriaHandler = listConsultoriaHandler;
        _consultoriaRepository = consultoriaRepository;

        _registerCustodianteHandler = registerCustodianteHandler;
        _registerCustodianteValidator = registerCustodianteValidator;
        _updateCustodianteHandler = updateCustodianteHandler;
        _updateCustodianteValidator = updateCustodianteValidator;
        _listCustodianteHandler = listCustodianteHandler;
        _custodianteRepository = custodianteRepository;

        _createTipoAtivoHandler = createTipoAtivoHandler;
        _createTipoAtivoValidator = createTipoAtivoValidator;
        _updateTipoAtivoHandler = updateTipoAtivoHandler;
        _updateTipoAtivoValidator = updateTipoAtivoValidator;
        _listTipoAtivoHandler = listTipoAtivoHandler;
        _tipoAtivoRepository = tipoAtivoRepository;

        _registerFundoHandler = registerFundoHandler;
        _registerFundoValidator = registerFundoValidator;
        _updateFundoHandler = updateFundoHandler;
        _updateFundoValidator = updateFundoValidator;
        _transitionFundoStatusHandler = transitionFundoStatusHandler;
        _transitionFundoStatusValidator = transitionFundoStatusValidator;
        _listFundoHandler = listFundoHandler;
        _fundoRepository = fundoRepository;

        _registerCedentePfHandler = registerCedentePfHandler;
        _registerCedentePfValidator = registerCedentePfValidator;
        _registerCedentePjHandler = registerCedentePjHandler;
        _registerCedentePjValidator = registerCedentePjValidator;
        _updateCedenteHandler = updateCedenteHandler;
        _updateCedenteValidator = updateCedenteValidator;
        _listCedenteHandler = listCedenteHandler;
        _cedenteRepository = cedenteRepository;

        _allowedTransitionsHandler = allowedTransitionsHandler;
        _currentCompanyService = currentCompanyService;
        _logger = logger;
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

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new RegisterConsultoriaFundoCommand(
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            Cnpj: request.Cnpj ?? string.Empty,
            NomeFantasia: string.IsNullOrWhiteSpace(request.NomeFantasia) ? null : request.NomeFantasia,
            Email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Telefone: string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _registerConsultoriaValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _registerConsultoriaHandler.HandleAsync(command, ct);
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
        var result = await _listConsultoriaHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/consultorias/{id} — Get ConsultoriaFundo by id (CAD-02b).</summary>
    [HttpGet("consultorias/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(ConsultoriaFundoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsultoriaById(Guid id, CancellationToken ct)
    {
        var consultoria = await _consultoriaRepository.GetByIdAsync(id, ct);
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

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new UpdateConsultoriaFundoCommand(
            Id: id,
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            NomeFantasia: string.IsNullOrWhiteSpace(request.NomeFantasia) ? null : request.NomeFantasia,
            Email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Telefone: string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone,
            Status: request.Status,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _updateConsultoriaValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _updateConsultoriaHandler.HandleAsync(command, ct);
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

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new RegisterCustodianteCommand(
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            Cnpj: request.Cnpj ?? string.Empty,
            CodigoInterno: string.IsNullOrWhiteSpace(request.CodigoInterno) ? null : request.CodigoInterno,
            Email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Telefone: string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _registerCustodianteValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _registerCustodianteHandler.HandleAsync(command, ct);
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
        var result = await _listCustodianteHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/custodiantes/{id} — Get Custodiante by id (CAD-06b).</summary>
    [HttpGet("custodiantes/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(CustodianteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustodianteById(Guid id, CancellationToken ct)
    {
        var custodiante = await _custodianteRepository.GetByIdAsync(id, ct);
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

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new UpdateCustodianteCommand(
            Id: id,
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            CodigoInterno: string.IsNullOrWhiteSpace(request.CodigoInterno) ? null : request.CodigoInterno,
            Email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Telefone: string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone,
            Status: request.Status,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _updateCustodianteValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _updateCustodianteHandler.HandleAsync(command, ct);
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
        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new CreateTipoAtivoCommand(
            Codigo: request.Codigo ?? string.Empty,
            Descricao: request.Descricao ?? string.Empty,
            Categoria: request.Categoria,
            Subcategoria: request.Subcategoria,
            OrdemExibicao: request.OrdemExibicao,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _createTipoAtivoValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _createTipoAtivoHandler.HandleAsync(command, ct);
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
        var result = await _listTipoAtivoHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/tipos-ativo/{id} — Get TipoAtivo by id (global, CAD-20b).</summary>
    [HttpGet("tipos-ativo/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(TipoAtivoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTipoAtivoById(Guid id, CancellationToken ct)
    {
        var tipoAtivo = await _tipoAtivoRepository.GetByIdAsync(id, ct);
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

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new UpdateTipoAtivoCommand(
            Id: id,
            Descricao: request.Descricao ?? string.Empty,
            Subcategoria: request.Subcategoria,
            Status: request.Status,
            OrdemExibicao: request.OrdemExibicao,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _updateTipoAtivoValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _updateTipoAtivoHandler.HandleAsync(command, ct);
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

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

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

        var validation = await _registerFundoValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _registerFundoHandler.HandleAsync(command, ct);
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
        var result = await _listFundoHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/{id} — Get Fundo by id (CAD-10b).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(FundoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFundoById(Guid id, CancellationToken ct)
    {
        var fundo = await _fundoRepository.GetByIdAsync(id, ct);
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

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new UpdateFundoCommand(
            Id: id,
            Nome: request.Nome ?? string.Empty,
            ClasseAnbima: request.ClasseAnbima,
            Segmento: request.Segmento,
            DataConstituicao: request.DataConstituicao,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _updateFundoValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _updateFundoHandler.HandleAsync(command, ct);
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
        Guid id, [FromBody] TransitionFundoStatusRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new TransitionFundoStatusCommand(
            FundoId: id,
            NewStatus: request.NewStatus,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _transitionFundoStatusValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _transitionFundoStatusHandler.HandleAsync(command, ct);
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
        var result = await _allowedTransitionsHandler.HandleAsync(query, ct);
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

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new RegisterCedentePfCommand(
            Cpf: request.Cpf ?? string.Empty,
            Nome: request.Nome ?? string.Empty,
            Email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Telefone: string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone,
            Endereco: request.Endereco,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _registerCedentePfValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _registerCedentePfHandler.HandleAsync(command, ct);
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

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new RegisterCedentePjCommand(
            Cnpj: request.Cnpj ?? string.Empty,
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            Email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Telefone: string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone,
            Endereco: request.Endereco,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _registerCedentePjValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _registerCedentePjHandler.HandleAsync(command, ct);
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
        var result = await _listCedenteHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/cedentes/{id} — Get Cedente by id (CAD-16b).</summary>
    [HttpGet("cedentes/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(CedenteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCedenteById(Guid id, CancellationToken ct)
    {
        var cedente = await _cedenteRepository.GetByIdAsync(id, ct);
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

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new UpdateCedenteCommand(
            Id: id,
            Nome: request.Nome ?? string.Empty,
            Email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
            Telefone: string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone,
            Endereco: request.Endereco,
            Status: request.Status,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _updateCedenteValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _updateCedenteHandler.HandleAsync(command, ct);
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

    /// <summary>
    /// Converts FluentValidation results to 422 ValidationProblemDetails — matches pattern used across all controllers.
    /// </summary>
    private static ValidationProblemDetails ToValidationProblem(FluentValidation.Results.ValidationResult result)
        => new(result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
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
