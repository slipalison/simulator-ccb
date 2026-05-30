using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.API.Extensions;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateCedenteTipoAtivo;
using Onboarding.Application.Fundos.Commands.TransitionCedenteTipoAtivoStatus;
using Onboarding.Application.Fundos.Commands.UpdateCedenteTipoAtivoLimite;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries.GetCedenteTiposAtivos;
using Onboarding.Application.Fundos.Queries.GetCedenteTipoAtivoAllowedTransitions;
using Onboarding.Domain.Aggregates.CedenteTipoAtivoAggregate;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.API.Controllers;

/// <summary>
/// CedenteTipoAtivo relationship endpoints — tenant-scoped via parent Cedente.ClienteId.
/// POST   /api/cedentes/{cedenteId}/tipos-ativos                  — create association
/// GET    /api/cedentes/{cedenteId}/tipos-ativos                  — paginated list
/// GET    /api/cedentes/{cedenteId}/tipos-ativos/{id}             — get by id
/// PATCH  /api/cedentes/{cedenteId}/tipos-ativos/{id}/limits      — update exposure limits
/// POST   /api/cedentes/{cedenteId}/tipos-ativos/{id}/status      — transition status (D-22)
/// </summary>
[ApiController]
[Route("api/cedentes/{cedenteId:guid}/tipos-ativos")]
[Authorize(AuthenticationSchemes = "BearerClient")]
public sealed class CedenteTiposAtivosController : ControllerBase
{
    private readonly ICommandHandler<CreateCedenteTipoAtivoCommand, RelCedenteTipoAtivoDto> _createHandler;
    private readonly ICommandHandler<UpdateCedenteTipoAtivoLimiteCommand, RelCedenteTipoAtivoDto> _updateLimiteHandler;
    private readonly ICommandHandler<TransitionCedenteTipoAtivoStatusCommand, RelCedenteTipoAtivoDto> _transitionHandler;
    private readonly IQueryHandler<GetCedenteTiposAtivosQuery, PaginatedResult<RelCedenteTipoAtivoDto>> _listHandler;
    private readonly IQueryHandler<GetCedenteTipoAtivoAllowedTransitionsQuery, IReadOnlyList<string>?> _allowedTransitionsHandler;
    private readonly ICedenteTipoAtivoAggregateRepository _repository;
    private readonly ICedenteRepository _cedenteRepository;
    private readonly ICurrentCompanyService _currentCompanyService;

    public CedenteTiposAtivosController(
        ICommandHandler<CreateCedenteTipoAtivoCommand, RelCedenteTipoAtivoDto> createHandler,
        ICommandHandler<UpdateCedenteTipoAtivoLimiteCommand, RelCedenteTipoAtivoDto> updateLimiteHandler,
        ICommandHandler<TransitionCedenteTipoAtivoStatusCommand, RelCedenteTipoAtivoDto> transitionHandler,
        IQueryHandler<GetCedenteTiposAtivosQuery, PaginatedResult<RelCedenteTipoAtivoDto>> listHandler,
        IQueryHandler<GetCedenteTipoAtivoAllowedTransitionsQuery, IReadOnlyList<string>?> allowedTransitionsHandler,
        ICedenteTipoAtivoAggregateRepository repository,
        ICedenteRepository cedenteRepository,
        ICurrentCompanyService currentCompanyService)
    {
        _createHandler = createHandler;
        _updateLimiteHandler = updateLimiteHandler;
        _transitionHandler = transitionHandler;
        _listHandler = listHandler;
        _allowedTransitionsHandler = allowedTransitionsHandler;
        _repository = repository;
        _cedenteRepository = cedenteRepository;
        _currentCompanyService = currentCompanyService;
    }

    /// <summary>POST /api/cedentes/{cedenteId}/tipos-ativos — Create CedenteTipoAtivo association.</summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(RelCedenteTipoAtivoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateCedenteTipoAtivo(
        Guid cedenteId,
        [FromBody] CreateCedenteTipoAtivoRequest? request,
        CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        // Security: tenant guard — caller must own this Cedente
        var cedente = await _cedenteRepository.GetByIdAsync(cedenteId, ct);
        if (cedente is null || cedente.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new CreateCedenteTipoAtivoCommand(
            CedenteId: cedenteId,
            TipoAtivoId: request.TipoAtivoId,
            LimitePercentual: request.LimitePercentual,
            LimiteValor: request.LimiteValor,
            DataInicio: request.DataInicio,
            DataFim: request.DataFim,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        try
        {
            var result = await _createHandler.HandleAsync(command, ct);
            return CreatedAtAction(nameof(GetCedenteTipoAtivoById),
                new { cedenteId, id = result.Id }, result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return UnprocessableEntity(ToValidationProblem(ex));
        }
        catch (DuplicateActiveAssociationException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Status = 409, Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = ex.Message });
        }
    }

    /// <summary>GET /api/cedentes/{cedenteId}/tipos-ativos — Paginated list of CedenteTipoAtivo associations.</summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(PaginatedResult<RelCedenteTipoAtivoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListCedenteTiposAtivos(
        Guid cedenteId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Security: tenant guard — caller must own this Cedente
        var cedente = await _cedenteRepository.GetByIdAsync(cedenteId, ct);
        if (cedente is null || cedente.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        var query = new GetCedenteTiposAtivosQuery(cedenteId, page, pageSize);
        var result = await _listHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/cedentes/{cedenteId}/tipos-ativos/{id} — Get CedenteTipoAtivo by id.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(RelCedenteTipoAtivoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCedenteTipoAtivoById(
        Guid cedenteId, Guid id, CancellationToken ct)
    {
        // Security: tenant guard — caller must own this Cedente
        var cedente = await _cedenteRepository.GetByIdAsync(cedenteId, ct);
        if (cedente is null || cedente.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        var association = await _repository.GetByIdAsync(id, ct);
        if (association is null || association.CedenteId != cedenteId)
            return NotFound();

        return Ok(ToDto(association));
    }

    /// <summary>PATCH /api/cedentes/{cedenteId}/tipos-ativos/{id}/limits — Update exposure limits.</summary>
    [HttpPatch("{id:guid}/limits")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(RelCedenteTipoAtivoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateCedenteTipoAtivoLimits(
        Guid cedenteId, Guid id,
        [FromBody] UpdateRelationshipLimitsRequest? request,
        CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        // Security: tenant guard — caller must own this Cedente
        var cedente = await _cedenteRepository.GetByIdAsync(cedenteId, ct);
        if (cedente is null || cedente.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new UpdateCedenteTipoAtivoLimiteCommand(
            AssociationId: id,
            LimitePercentual: request.LimitePercentual,
            LimiteValor: request.LimiteValor,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        try
        {
            var result = await _updateLimiteHandler.HandleAsync(command, ct);
            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return UnprocessableEntity(ToValidationProblem(ex));
        }
        catch (InvalidStateTransitionException ex)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid operation", Status = 400, Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = ex.Message });
        }
    }

    /// <summary>POST /api/cedentes/{cedenteId}/tipos-ativos/{id}/status — Transition CedenteTipoAtivo status (D-22).</summary>
    [HttpPost("{id:guid}/status")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(RelCedenteTipoAtivoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransitionCedenteTipoAtivoStatus(
        Guid cedenteId, Guid id,
        [FromBody] TransitionRelationshipStatusRequest? request,
        CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        // Security: tenant guard — caller must own this Cedente
        var cedente = await _cedenteRepository.GetByIdAsync(cedenteId, ct);
        if (cedente is null || cedente.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new TransitionCedenteTipoAtivoStatusCommand(
            AssociationId: id,
            NewStatus: request.NewStatus,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        try
        {
            var result = await _transitionHandler.HandleAsync(command, ct);
            return Ok(result);
        }
        catch (InvalidStateTransitionException ex)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid status transition", Status = 400, Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not found", Status = 404, Detail = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/cedentes/{cedenteId}/tipos-ativos/{id}/allowed-transitions — Returns valid next statuses (D-25).
    /// 200 OK with string[]; 404 if not found or cross-tenant.
    /// </summary>
    [HttpGet("{id:guid}/allowed-transitions")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCedenteTipoAtivoAllowedTransitions(
        Guid cedenteId, Guid id, CancellationToken ct)
    {
        var query = new GetCedenteTipoAtivoAllowedTransitionsQuery(cedenteId, id);
        var result = await _allowedTransitionsHandler.HandleAsync(query, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    private static RelCedenteTipoAtivoDto ToDto(CedenteTipoAtivoAggregate a) => new(
        a.Id, a.CedenteId, a.TipoAtivoId,
        a.Limite.Percentual, a.Limite.Valor,
        a.Janela.DataInicio, a.Janela.DataFim,
        a.Status, a.CreatedAt);

    /// <summary>
    /// Converts a <see cref="FluentValidation.ValidationException"/> to 422 ValidationProblemDetails.
    /// Delegates to shared <see cref="ValidationExtensions.ToValidationProblem"/> (DRY-01).
    /// </summary>
    private static ValidationProblemDetails ToValidationProblem(FluentValidation.ValidationException ex)
        => ex.ToValidationProblem();
}

// =========================================================================
// Request DTOs
// =========================================================================

/// <summary>Request DTO for POST /api/cedentes/{cedenteId}/tipos-ativos.</summary>
public sealed record CreateCedenteTipoAtivoRequest(
    Guid TipoAtivoId,
    decimal? LimitePercentual,
    decimal? LimiteValor,
    DateTimeOffset DataInicio,
    DateTimeOffset? DataFim);
