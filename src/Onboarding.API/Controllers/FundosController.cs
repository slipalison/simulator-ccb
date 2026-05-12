using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.API.Controllers;

/// <summary>
/// Fund entity endpoints — ConsultoriaFundo, Custodiante, TipoAtivo.
/// ConsultoriaFundo/Custodiante are company-scoped (D-01).
/// TipoAtivo is global (D-03/TEN-03) — no company filter.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "BearerClient")]
public sealed class FundosController : ControllerBase
{
    // ConsultoriaFundo
    private readonly ICommandHandler<RegisterConsultoriaFundoCommand, ConsultoriaFundoDto> _registerConsultoriaHandler;
    private readonly IValidator<RegisterConsultoriaFundoCommand> _registerConsultoriaValidator;
    private readonly ICommandHandler<UpdateConsultoriaFundoCommand, Unit> _updateConsultoriaHandler;
    private readonly IValidator<UpdateConsultoriaFundoCommand> _updateConsultoriaValidator;
    private readonly IQueryHandler<ListConsultoriaFundoQuery, PaginatedResult<ConsultoriaFundoDto>> _listConsultoriaHandler;
    private readonly IConsultoriaFundoRepository _consultoriaRepository;

    // Custodiante
    private readonly ICommandHandler<RegisterCustodianteCommand, CustodianteDto> _registerCustodianteHandler;
    private readonly IValidator<RegisterCustodianteCommand> _registerCustodianteValidator;
    private readonly ICommandHandler<UpdateCustodianteCommand, Unit> _updateCustodianteHandler;
    private readonly IValidator<UpdateCustodianteCommand> _updateCustodianteValidator;
    private readonly IQueryHandler<ListCustodianteQuery, PaginatedResult<CustodianteDto>> _listCustodianteHandler;
    private readonly ICustodianteRepository _custodianteRepository;

    // TipoAtivo (global — no ICurrentCompanyService)
    private readonly ICommandHandler<CreateTipoAtivoCommand, TipoAtivoDto> _createTipoAtivoHandler;
    private readonly IValidator<CreateTipoAtivoCommand> _createTipoAtivoValidator;
    private readonly ICommandHandler<UpdateTipoAtivoCommand, Unit> _updateTipoAtivoHandler;
    private readonly IValidator<UpdateTipoAtivoCommand> _updateTipoAtivoValidator;
    private readonly IQueryHandler<ListTipoAtivoQuery, PaginatedResult<TipoAtivoDto>> _listTipoAtivoHandler;
    private readonly ITipoAtivoRepository _tipoAtivoRepository;

    // Shared
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly ILogger<FundosController> _logger;

    public FundosController(
        // ConsultoriaFundo
        ICommandHandler<RegisterConsultoriaFundoCommand, ConsultoriaFundoDto> registerConsultoriaHandler,
        IValidator<RegisterConsultoriaFundoCommand> registerConsultoriaValidator,
        ICommandHandler<UpdateConsultoriaFundoCommand, Unit> updateConsultoriaHandler,
        IValidator<UpdateConsultoriaFundoCommand> updateConsultoriaValidator,
        IQueryHandler<ListConsultoriaFundoQuery, PaginatedResult<ConsultoriaFundoDto>> listConsultoriaHandler,
        IConsultoriaFundoRepository consultoriaRepository,
        // Custodiante
        ICommandHandler<RegisterCustodianteCommand, CustodianteDto> registerCustodianteHandler,
        IValidator<RegisterCustodianteCommand> registerCustodianteValidator,
        ICommandHandler<UpdateCustodianteCommand, Unit> updateCustodianteHandler,
        IValidator<UpdateCustodianteCommand> updateCustodianteValidator,
        IQueryHandler<ListCustodianteQuery, PaginatedResult<CustodianteDto>> listCustodianteHandler,
        ICustodianteRepository custodianteRepository,
        // TipoAtivo
        ICommandHandler<CreateTipoAtivoCommand, TipoAtivoDto> createTipoAtivoHandler,
        IValidator<CreateTipoAtivoCommand> createTipoAtivoValidator,
        ICommandHandler<UpdateTipoAtivoCommand, Unit> updateTipoAtivoHandler,
        IValidator<UpdateTipoAtivoCommand> updateTipoAtivoValidator,
        IQueryHandler<ListTipoAtivoQuery, PaginatedResult<TipoAtivoDto>> listTipoAtivoHandler,
        ITipoAtivoRepository tipoAtivoRepository,
        // Shared
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
        _currentCompanyService = currentCompanyService;
        _logger = logger;
    }

    // ─── ConsultoriaFundo endpoints ────────────────────────────────────

    /// <summary>POST /api/fundos/consultorias — Register ConsultoriaFundo (CAD-01).</summary>
    [HttpPost("consultorias")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(ConsultoriaFundoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterConsultoriaFundo(
        [FromBody] RegisterConsultoriaFundoRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Request body is required."
            });

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new RegisterConsultoriaFundoCommand(
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            Cnpj: request.Cnpj ?? string.Empty,
            NomeFantasia: request.NomeFantasia,
            Email: request.Email,
            Telefone: request.Telefone,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _registerConsultoriaValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _registerConsultoriaHandler.HandleAsync(command, ct);
            return CreatedAtAction(nameof(GetConsultoriaFundo), new { id = result.Id }, result);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>GET /api/fundos/consultorias — List ConsultoriaFundo (paginated, company-scoped).</summary>
    [HttpGet("consultorias")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(PaginatedResult<ConsultoriaFundoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListConsultoriaFundo(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var query = new ListConsultoriaFundoQuery(page, pageSize, search);
        var result = await _listConsultoriaHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/consultorias/{id} — Get ConsultoriaFundo by ID.</summary>
    [HttpGet("consultorias/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(ConsultoriaFundoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsultoriaFundo(Guid id, CancellationToken ct)
    {
        var entity = await _consultoriaRepository.GetByIdAsync(id, ct);
        if (entity is null)
            return NotFound();

        var dto = new ConsultoriaFundoDto(
            entity.Id,
            entity.RazaoSocial,
            entity.NomeFantasia,
            entity.Cnpj.Value,
            entity.Email?.Value,
            entity.Telefone?.Value,
            entity.Status,
            entity.CreatedAt);

        return Ok(dto);
    }

    /// <summary>PUT /api/fundos/consultorias/{id} — Update ConsultoriaFundo (CAD-03).</summary>
    [HttpPut("consultorias/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(ConsultoriaFundoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateConsultoriaFundo(
        Guid id, [FromBody] UpdateConsultoriaFundoRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Request body is required."
            });

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new UpdateConsultoriaFundoCommand(
            Id: id,
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            NomeFantasia: request.NomeFantasia,
            Email: request.Email,
            Telefone: request.Telefone,
            Status: request.Status,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _updateConsultoriaValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            await _updateConsultoriaHandler.HandleAsync(command, ct);

            // Return updated entity
            var entity = await _consultoriaRepository.GetByIdAsync(id, ct);
            if (entity is null)
                return NotFound();

            var dto = new ConsultoriaFundoDto(
                entity.Id, entity.RazaoSocial, entity.NomeFantasia,
                entity.Cnpj.Value, entity.Email?.Value, entity.Telefone?.Value,
                entity.Status, entity.CreatedAt);

            return Ok(dto);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // ─── Custodiante endpoints ─────────────────────────────────────────

    /// <summary>POST /api/fundos/custodiantes — Register Custodiante (CAD-05).</summary>
    [HttpPost("custodiantes")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(CustodianteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterCustodiante(
        [FromBody] RegisterCustodianteRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Request body is required."
            });

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new RegisterCustodianteCommand(
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            Cnpj: request.Cnpj ?? string.Empty,
            CodigoInterno: request.CodigoInterno,
            Email: request.Email,
            Telefone: request.Telefone,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _registerCustodianteValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _registerCustodianteHandler.HandleAsync(command, ct);
            return CreatedAtAction(nameof(GetCustodiante), new { id = result.Id }, result);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>GET /api/fundos/custodiantes — List Custodiante (paginated, company-scoped).</summary>
    [HttpGet("custodiantes")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(PaginatedResult<CustodianteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCustodiante(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var query = new ListCustodianteQuery(page, pageSize, search);
        var result = await _listCustodianteHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/custodiantes/{id} — Get Custodiante by ID.</summary>
    [HttpGet("custodiantes/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(CustodianteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustodiante(Guid id, CancellationToken ct)
    {
        var entity = await _custodianteRepository.GetByIdAsync(id, ct);
        if (entity is null)
            return NotFound();

        var dto = new CustodianteDto(
            entity.Id,
            entity.RazaoSocial,
            entity.CodigoInterno,
            entity.Cnpj.Value,
            entity.Email?.Value,
            entity.Telefone?.Value,
            entity.Status,
            entity.CreatedAt);

        return Ok(dto);
    }

    /// <summary>PUT /api/fundos/custodiantes/{id} — Update Custodiante (CAD-07).</summary>
    [HttpPut("custodiantes/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(CustodianteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateCustodiante(
        Guid id, [FromBody] UpdateCustodianteRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Request body is required."
            });

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;

        var command = new UpdateCustodianteCommand(
            Id: id,
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            CodigoInterno: request.CodigoInterno,
            Email: request.Email,
            Telefone: request.Telefone,
            Status: request.Status,
            ActorSub: actorSub,
            ActorEmail: actorEmail);

        var validation = await _updateCustodianteValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            await _updateCustodianteHandler.HandleAsync(command, ct);

            // Return updated entity
            var entity = await _custodianteRepository.GetByIdAsync(id, ct);
            if (entity is null)
                return NotFound();

            var dto = new CustodianteDto(
                entity.Id, entity.RazaoSocial, entity.CodigoInterno,
                entity.Cnpj.Value, entity.Email?.Value, entity.Telefone?.Value,
                entity.Status, entity.CreatedAt);

            return Ok(dto);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // ─── TipoAtivo endpoints (global entity — no company scope per D-03) ──

    /// <summary>POST /api/fundos/tipos-ativo — Create TipoAtivo (CAD-19). Global entity per D-03.</summary>
    [HttpPost("tipos-ativo")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(TipoAtivoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateTipoAtivo(
        [FromBody] CreateTipoAtivoRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Request body is required."
            });

        // TipoAtivo is global (D-03) — actor info from JWT claims directly, no ICurrentCompanyService
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
            return CreatedAtAction(nameof(GetTipoAtivo), new { id = result.Id }, result);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>GET /api/fundos/tipos-ativo — List TipoAtivo (paginated, global scope per D-03).</summary>
    [HttpGet("tipos-ativo")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(PaginatedResult<TipoAtivoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTipoAtivo(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var query = new ListTipoAtivoQuery(page, pageSize, search);
        var result = await _listTipoAtivoHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/fundos/tipos-ativo/{id} — Get TipoAtivo by ID.</summary>
    [HttpGet("tipos-ativo/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
    [ProducesResponseType(typeof(TipoAtivoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTipoAtivo(Guid id, CancellationToken ct)
    {
        var entity = await _tipoAtivoRepository.GetByIdAsync(id, ct);
        if (entity is null)
            return NotFound();

        var dto = new TipoAtivoDto(
            entity.Id,
            entity.Codigo,
            entity.Descricao,
            entity.Categoria,
            entity.Subcategoria,
            entity.Status,
            entity.OrdemExibicao);

        return Ok(dto);
    }

    /// <summary>PUT /api/fundos/tipos-ativo/{id} — Update TipoAtivo (CAD-21). Global entity per D-03.</summary>
    [HttpPut("tipos-ativo/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundWrite)]
    [ProducesResponseType(typeof(TipoAtivoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateTipoAtivo(
        Guid id, [FromBody] UpdateTipoAtivoRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Request body is required."
            });

        // TipoAtivo is global (D-03) — actor info from JWT claims directly
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
            await _updateTipoAtivoHandler.HandleAsync(command, ct);

            // Return updated entity
            var entity = await _tipoAtivoRepository.GetByIdAsync(id, ct);
            if (entity is null)
                return NotFound();

            var dto = new TipoAtivoDto(
                entity.Id, entity.Codigo, entity.Descricao,
                entity.Categoria, entity.Subcategoria,
                entity.Status, entity.OrdemExibicao);

            return Ok(dto);
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // ─── Helper ────────────────────────────────────────────────────────

    /// <summary>Converts a FluentValidation result into a ValidationProblemDetails response body.</summary>
    private static ValidationProblemDetails ToValidationProblem(FluentValidation.Results.ValidationResult result)
        => new(result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
}

// ─── Request DTOs ──────────────────────────────────────────────────────

/// <summary>Request body for registering a ConsultoriaFundo.</summary>
public sealed record RegisterConsultoriaFundoRequest(
    string? RazaoSocial,
    string? Cnpj,
    string? NomeFantasia,
    string? Email,
    string? Telefone);

/// <summary>Request body for updating a ConsultoriaFundo.</summary>
public sealed record UpdateConsultoriaFundoRequest(
    string? RazaoSocial,
    string? NomeFantasia,
    string? Email,
    string? Telefone,
    ConsultoriaFundoStatus Status);

/// <summary>Request body for registering a Custodiante.</summary>
public sealed record RegisterCustodianteRequest(
    string? RazaoSocial,
    string? Cnpj,
    string? CodigoInterno,
    string? Email,
    string? Telefone);

/// <summary>Request body for updating a Custodiante.</summary>
public sealed record UpdateCustodianteRequest(
    string? RazaoSocial,
    string? CodigoInterno,
    string? Email,
    string? Telefone,
    CustodianteStatus Status);

/// <summary>Request body for creating a TipoAtivo.</summary>
public sealed record CreateTipoAtivoRequest(
    string? Codigo,
    string? Descricao,
    TipoAtivoCategoria Categoria,
    string? Subcategoria,
    int OrdemExibicao = 0);

/// <summary>Request body for updating a TipoAtivo.</summary>
public sealed record UpdateTipoAtivoRequest(
    string? Descricao,
    string? Subcategoria,
    TipoAtivoStatus Status,
    int OrdemExibicao);