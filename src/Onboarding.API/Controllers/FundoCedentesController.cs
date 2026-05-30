using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.API.Extensions;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateFundoCedente;
using Onboarding.Application.Fundos.Commands.TransitionFundoCedenteStatus;
using Onboarding.Application.Fundos.Commands.UpdateFundoCedenteLimite;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries.GetFundoCedentes;
using Onboarding.Application.Fundos.Queries.GetFundoCedenteAllowedTransitions;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.API.Controllers;

/// <summary>
/// FundoCedente relationship endpoints — tenant-scoped via parent Fundo.ClienteId.
/// POST   /api/fundos/{fundoId}/cedentes                  — REL-01: create association
/// GET    /api/fundos/{fundoId}/cedentes                  — REL-02: paginated list
/// GET    /api/fundos/{fundoId}/cedentes/{id}             — REL-02b: get by id
/// PATCH  /api/fundos/{fundoId}/cedentes/{id}/limits      — REL-03: update exposure limits
/// POST   /api/fundos/{fundoId}/cedentes/{id}/status      — REL-04: transition status (D-22)
/// </summary>
[ApiController]
[Route("api/fundos/{fundoId:guid}/cedentes")]
[Authorize(AuthenticationSchemes = "BearerClient")]
public sealed class FundoCedentesController : ControllerBase
{
    private readonly ICommandHandler<CreateFundoCedenteCommand, RelFundoCedenteDto> _createHandler;
    private readonly ICommandHandler<UpdateFundoCedenteLimiteCommand, RelFundoCedenteDto> _updateLimiteHandler;
    private readonly ICommandHandler<TransitionFundoCedenteStatusCommand, RelFundoCedenteDto> _transitionHandler;
    private readonly IQueryHandler<GetFundoCedentesQuery, PaginatedResult<RelFundoCedenteDto>> _listHandler;
    private readonly IQueryHandler<GetFundoCedenteAllowedTransitionsQuery, IReadOnlyList<string>?> _allowedTransitionsHandler;
    private readonly IFundoCedenteAggregateRepository _repository;
    private readonly IFundoRepository _fundoRepository;
    private readonly ICurrentCompanyService _currentCompanyService;

    public FundoCedentesController(
        ICommandHandler<CreateFundoCedenteCommand, RelFundoCedenteDto> createHandler,
        ICommandHandler<UpdateFundoCedenteLimiteCommand, RelFundoCedenteDto> updateLimiteHandler,
        ICommandHandler<TransitionFundoCedenteStatusCommand, RelFundoCedenteDto> transitionHandler,
        IQueryHandler<GetFundoCedentesQuery, PaginatedResult<RelFundoCedenteDto>> listHandler,
        IQueryHandler<GetFundoCedenteAllowedTransitionsQuery, IReadOnlyList<string>?> allowedTransitionsHandler,
        IFundoCedenteAggregateRepository repository,
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

    /// <summary>POST /api/fundos/{fundoId}/cedentes — Create FundoCedente association (REL-01).</summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(RelFundoCedenteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateFundoCedente(
        Guid fundoId,
        [FromBody] CreateFundoCedenteRequest? request,
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

        var command = new CreateFundoCedenteCommand(
            FundoId: fundoId,
            CedenteId: request.CedenteId,
            LimitePercentual: request.LimitePercentual,
            LimiteValor: request.LimiteValor,
            DataInicio: request.DataInicio,
            DataFim: request.DataFim,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        try
        {
            var result = await _createHandler.HandleAsync(command, ct);
            return CreatedAtAction(nameof(GetFundoCedenteById),
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

    /// <summary>GET /api/fundos/{fundoId}/cedentes — Paginated list of FundoCedente associations (REL-02).</summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(PaginatedResult<RelFundoCedenteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListFundoCedentes(
        Guid fundoId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Security: tenant guard — caller must own this Fundo
        var fundo = await _fundoRepository.GetByIdAsync(fundoId, ct);
        if (fundo is null || fundo.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        var query = new GetFundoCedentesQuery(fundoId, page, pageSize);
        var result = await _listHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/{fundoId}/cedentes/{id} — Get FundoCedente by id (REL-02b).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(RelFundoCedenteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFundoCedenteById(
        Guid fundoId, Guid id, CancellationToken ct)
    {
        // Security: tenant guard — caller must own this Fundo
        var fundo = await _fundoRepository.GetByIdAsync(fundoId, ct);
        if (fundo is null || fundo.ClienteId != _currentCompanyService.CompanyId)
            return NotFound();

        var association = await _repository.GetByIdAsync(id, ct);
        // Also verify the association belongs to this Fundo (prevents cross-fundo enumeration)
        if (association is null || association.FundoId != fundoId)
            return NotFound();

        return Ok(ToDto(association));
    }

    /// <summary>PATCH /api/fundos/{fundoId}/cedentes/{id}/limits — Update exposure limits (REL-03).</summary>
    [HttpPatch("{id:guid}/limits")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(RelFundoCedenteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateFundoCedenteLimits(
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

        var command = new UpdateFundoCedenteLimiteCommand(
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

    /// <summary>POST /api/fundos/{fundoId}/cedentes/{id}/status — Transition FundoCedente status (REL-04, D-22).</summary>
    [HttpPost("{id:guid}/status")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(RelFundoCedenteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransitionFundoCedenteStatus(
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

        var command = new TransitionFundoCedenteStatusCommand(
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
    /// GET /api/fundos/{fundoId}/cedentes/{id}/allowed-transitions — Returns valid next statuses (D-25).
    /// 200 OK with string[]; 404 if not found or cross-tenant.
    /// </summary>
    [HttpGet("{id:guid}/allowed-transitions")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFundoCedenteAllowedTransitions(
        Guid fundoId, Guid id, CancellationToken ct)
    {
        var query = new GetFundoCedenteAllowedTransitionsQuery(fundoId, id);
        var result = await _allowedTransitionsHandler.HandleAsync(query, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    private static RelFundoCedenteDto ToDto(FundoCedenteAggregate a) => new(
        a.Id, a.FundoId, a.CedenteId,
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
// Request DTOs — self-contained with the controller
// =========================================================================

/// <summary>Request DTO for POST /api/fundos/{fundoId}/cedentes.</summary>
public sealed record CreateFundoCedenteRequest(
    Guid CedenteId,
    decimal? LimitePercentual,
    decimal? LimiteValor,
    DateTimeOffset DataInicio,
    DateTimeOffset? DataFim);

/// <summary>Shared request DTO for PATCH .../limits on relationship aggregates.</summary>
public sealed record UpdateRelationshipLimitsRequest(
    decimal? LimitePercentual,
    decimal? LimiteValor);

/// <summary>Shared request DTO for POST .../status on relationship aggregates (D-22).</summary>
public sealed record TransitionRelationshipStatusRequest(RelationshipStatus NewStatus);
