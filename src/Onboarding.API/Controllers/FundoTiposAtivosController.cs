using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.API.Extensions;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateFundoTipoAtivo;
using Onboarding.Application.Fundos.Commands.TransitionFundoTipoAtivoStatus;
using Onboarding.Application.Fundos.Commands.UpdateFundoTipoAtivoLimite;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries.GetFundoTiposAtivos;
using Onboarding.Application.Fundos.Queries.GetFundoTipoAtivoAllowedTransitions;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.API.Controllers;

/// <summary>
/// FundoTipoAtivo relationship endpoints — tenant-scoped via parent Fundo.ClienteId.
/// POST   /api/fundos/{fundoId}/tipos-ativos                  — create association
/// GET    /api/fundos/{fundoId}/tipos-ativos                  — paginated list
/// GET    /api/fundos/{fundoId}/tipos-ativos/{id}             — get by id
/// PATCH  /api/fundos/{fundoId}/tipos-ativos/{id}/limits      — update exposure limits
/// POST   /api/fundos/{fundoId}/tipos-ativos/{id}/status      — transition status (D-22)
/// </summary>
[ApiController]
[Route("api/fundos/{fundoId:guid}/tipos-ativos")]
[Authorize(AuthenticationSchemes = "BearerClient")]
public sealed class FundoTiposAtivosController : ControllerBase
{
    private readonly ICommandHandler<CreateFundoTipoAtivoCommand, RelFundoTipoAtivoDto> _createHandler;
    private readonly ICommandHandler<UpdateFundoTipoAtivoLimiteCommand, RelFundoTipoAtivoDto> _updateLimiteHandler;
    private readonly ICommandHandler<TransitionFundoTipoAtivoStatusCommand, RelFundoTipoAtivoDto> _transitionHandler;
    private readonly IQueryHandler<GetFundoTiposAtivosQuery, PaginatedResult<RelFundoTipoAtivoDto>> _listHandler;
    private readonly IQueryHandler<GetFundoTipoAtivoAllowedTransitionsQuery, IReadOnlyList<string>?> _allowedTransitionsHandler;
    private readonly IFundoTipoAtivoAggregateRepository _repository;
    private readonly IFundoRepository _fundoRepository;
    private readonly ICurrentCompanyService _currentCompanyService;

    public FundoTiposAtivosController(
        ICommandHandler<CreateFundoTipoAtivoCommand, RelFundoTipoAtivoDto> createHandler,
        ICommandHandler<UpdateFundoTipoAtivoLimiteCommand, RelFundoTipoAtivoDto> updateLimiteHandler,
        ICommandHandler<TransitionFundoTipoAtivoStatusCommand, RelFundoTipoAtivoDto> transitionHandler,
        IQueryHandler<GetFundoTiposAtivosQuery, PaginatedResult<RelFundoTipoAtivoDto>> listHandler,
        IQueryHandler<GetFundoTipoAtivoAllowedTransitionsQuery, IReadOnlyList<string>?> allowedTransitionsHandler,
        IFundoTipoAtivoAggregateRepository repository,
        IFundoRepository fundoRepository,
        ICurrentCompanyService currentCompanyService)
    {
        _createHandler = createHandler;
        _updateLimiteHandler = updateLimiteHandler;
        _transitionHandler = transitionHandler;
        _listHandler = listHandler;
        _allowedTransitionsHandler = allowedTransitionsHandler;
        _repository = repository;
        _fundoRepository = fundoRepository;
        _currentCompanyService = currentCompanyService;
    }

    /// <summary>POST /api/fundos/{fundoId}/tipos-ativos — Create FundoTipoAtivo association.</summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(RelFundoTipoAtivoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateFundoTipoAtivo(
        Guid fundoId,
        [FromBody] CreateFundoTipoAtivoRequest? request,
        CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        // Security: tenant guard — caller must own this Fundo
        var fundo = await _fundoRepository.GetByIdAsync(fundoId, ct);
        if (fundo is null || fundo.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new CreateFundoTipoAtivoCommand(
            FundoId: fundoId,
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
            return CreatedAtAction(nameof(GetFundoTipoAtivoById),
                new { fundoId, id = result.Id }, result);
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

    /// <summary>GET /api/fundos/{fundoId}/tipos-ativos — Paginated list of FundoTipoAtivo associations.</summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(PaginatedResult<RelFundoTipoAtivoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListFundoTiposAtivos(
        Guid fundoId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Security: tenant guard — caller must own this Fundo
        var fundo = await _fundoRepository.GetByIdAsync(fundoId, ct);
        if (fundo is null || fundo.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        var query = new GetFundoTiposAtivosQuery(fundoId, page, pageSize);
        var result = await _listHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/{fundoId}/tipos-ativos/{id} — Get FundoTipoAtivo by id.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(RelFundoTipoAtivoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFundoTipoAtivoById(
        Guid fundoId, Guid id, CancellationToken ct)
    {
        // Security: tenant guard — caller must own this Fundo
        var fundo = await _fundoRepository.GetByIdAsync(fundoId, ct);
        if (fundo is null || fundo.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        var association = await _repository.GetByIdAsync(id, ct);
        if (association is null || association.FundoId != fundoId)
            return NotFound();

        return Ok(ToDto(association));
    }

    /// <summary>PATCH /api/fundos/{fundoId}/tipos-ativos/{id}/limits — Update exposure limits.</summary>
    [HttpPatch("{id:guid}/limits")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(RelFundoTipoAtivoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateFundoTipoAtivoLimits(
        Guid fundoId, Guid id,
        [FromBody] UpdateRelationshipLimitsRequest? request,
        CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        // Security: tenant guard — caller must own this Fundo
        var fundo = await _fundoRepository.GetByIdAsync(fundoId, ct);
        if (fundo is null || fundo.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new UpdateFundoTipoAtivoLimiteCommand(
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

    /// <summary>POST /api/fundos/{fundoId}/tipos-ativos/{id}/status — Transition FundoTipoAtivo status (D-22).</summary>
    [HttpPost("{id:guid}/status")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(RelFundoTipoAtivoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransitionFundoTipoAtivoStatus(
        Guid fundoId, Guid id,
        [FromBody] TransitionRelationshipStatusRequest? request,
        CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails { Title = "Bad request", Status = 400, Detail = "Request body is required." });

        // Security: tenant guard — caller must own this Fundo
        var fundo = await _fundoRepository.GetByIdAsync(fundoId, ct);
        if (fundo is null || fundo.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new TransitionFundoTipoAtivoStatusCommand(
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
    /// GET /api/fundos/{fundoId}/tipos-ativos/{id}/allowed-transitions — Returns valid next statuses (D-25).
    /// 200 OK with string[]; 404 if not found or cross-tenant.
    /// </summary>
    [HttpGet("{id:guid}/allowed-transitions")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFundoTipoAtivoAllowedTransitions(
        Guid fundoId, Guid id, CancellationToken ct)
    {
        var query = new GetFundoTipoAtivoAllowedTransitionsQuery(fundoId, id);
        var result = await _allowedTransitionsHandler.HandleAsync(query, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    private static RelFundoTipoAtivoDto ToDto(FundoTipoAtivoAggregate a) => new(
        a.Id, a.FundoId, a.TipoAtivoId,
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

/// <summary>Request DTO for POST /api/fundos/{fundoId}/tipos-ativos.</summary>
public sealed record CreateFundoTipoAtivoRequest(
    Guid TipoAtivoId,
    decimal? LimitePercentual,
    decimal? LimiteValor,
    DateTimeOffset DataInicio,
    DateTimeOffset? DataFim);
