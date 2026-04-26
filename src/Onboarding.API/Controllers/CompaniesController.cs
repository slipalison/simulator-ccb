using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.Application.Common;
using Onboarding.Application.Companies.Commands;
using Onboarding.Application.Companies.DTOs;
using Onboarding.Application.Companies.Queries;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;

namespace Onboarding.API.Controllers;

/// <summary>
/// Company endpoints.
/// GET /api/companies/me — AUTH-03: protected route returns profile of authenticated company.
/// POST /api/companies/registration — REG-01: PJ company registration with Keycloak user creation.
/// POST /api/companies/{companyId}/employees — REG-03: PJ registers employee (PF) with temp password.
/// GET /api/companies/{companyId}/employees — MGMT-02: PJ lists employees with pagination/filters.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class CompaniesController : ControllerBase
{
    private readonly ICompanyRepository _repository;
    private readonly ICommandHandler<RegisterCompanyCommand, RegisterCompanyResult> _registerHandler;
    private readonly IValidator<RegisterCompanyCommand> _registerValidator;
    private readonly ICommandHandler<RegisterEmployeeCommand, RegisterEmployeeResult> _registerEmployeeHandler;
    private readonly IValidator<RegisterEmployeeCommand> _registerEmployeeValidator;
    private readonly IQueryHandler<GetCompanyEmployeesQuery, PaginatedResult<EmployeeListItemDto>> _getEmployeesHandler;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(
        ICompanyRepository repository,
        ICommandHandler<RegisterCompanyCommand, RegisterCompanyResult> registerHandler,
        IValidator<RegisterCompanyCommand> registerValidator,
        ICommandHandler<RegisterEmployeeCommand, RegisterEmployeeResult> registerEmployeeHandler,
        IValidator<RegisterEmployeeCommand> registerEmployeeValidator,
        IQueryHandler<GetCompanyEmployeesQuery, PaginatedResult<EmployeeListItemDto>> getEmployeesHandler,
        ICurrentCompanyService currentCompanyService,
        ILogger<CompaniesController> logger)
    {
        _repository = repository;
        _registerHandler = registerHandler;
        _registerValidator = registerValidator;
        _registerEmployeeHandler = registerEmployeeHandler;
        _registerEmployeeValidator = registerEmployeeValidator;
        _getEmployeesHandler = getEmployeesHandler;
        _currentCompanyService = currentCompanyService;
        _logger = logger;
    }

    /// <summary>GET /api/companies/me — returns the authenticated company's profile.</summary>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "BearerClient")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        // Primary: use "sub" (Keycloak user ID) — opaque, stable, non-personal identifier
        var keycloakSub = User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(keycloakSub))
        {
            var company = await _repository.GetByKeycloakSubAsync(keycloakSub, ct);
            if (company is not null) return Ok(MapToDto(company));

            _logger.LogWarning("Authenticated user with sub {Sub} not found in database", keycloakSub);
            return NotFound();
        }

        _logger.LogWarning("Authenticated request missing 'sub' claim in JWT");
        return Unauthorized();
    }

    /// <summary>POST /api/companies/registration — Register a new PJ company (REG-01).</summary>
    [HttpPost("registration")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterCompany(
        [FromBody] RegisterCompanyRequest? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Request body is required."
            });

        // Extract IP address from connection + X-Forwarded-For
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            ipAddress = forwardedFor.Split(',')[0].Trim();

        // Map request to command
        var command = new RegisterCompanyCommand(
            RazaoSocial: request.RazaoSocial ?? string.Empty,
            Cnpj: request.Cnpj ?? string.Empty,
            Email: request.Email ?? string.Empty,
            Phone: request.Phone ?? string.Empty,
            Password: request.Password ?? string.Empty,
            TermsAccepted: request.TermsAccepted ?? false,
            TermsVersion: TermsAcceptance.CurrentVersion,
            IpAddress: ipAddress);

        // Validate
        var validation = await _registerValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return UnprocessableEntity(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        try
        {
            var result = await _registerHandler.HandleAsync(command, ct);
            return CreatedAtAction(nameof(GetMe), null, result);
        }
        catch (DuplicateCompanyException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message
            });
        }
        catch (DuplicateKeycloakUserException ex)
        {
            _logger.LogWarning(ex, "Duplicate Keycloak user during registration for {Email}", command.Email);
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = "A user with this email already exists."
            });
        }
    }

    /// <summary>POST /api/companies/{companyId}/employees — Register employee (PF) for company (REG-03, MGMT-01).</summary>
    [HttpPost("{companyId:guid}/employees")]
    [Authorize(AuthenticationSchemes = "BearerClient")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterEmployee(
        Guid companyId, [FromBody] RegisterEmployeeRequest? request, CancellationToken ct)
    {
        // Company isolation: verify route companyId matches JWT companyId (T-38-07)
        if (companyId != _currentCompanyService.CompanyId)
            return Forbid();

        if (request is null)
            return BadRequest(new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Request body is required."
            });

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            ipAddress = forwardedFor.Split(',')[0].Trim();

        var command = new RegisterEmployeeCommand(
            CompanyId: companyId,
            Nome: request.Nome ?? string.Empty,
            Cpf: request.Cpf ?? string.Empty,
            Email: request.Email ?? string.Empty,
            Phone: request.Phone ?? string.Empty,
            AccessGroupId: request.AccessGroupId,
            ActorSub: actorSub,
            ActorEmail: actorEmail,
            IpAddress: ipAddress);

        var validation = await _registerEmployeeValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return UnprocessableEntity(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        try
        {
            var result = await _registerEmployeeHandler.HandleAsync(command, ct);
            return CreatedAtAction(nameof(GetMe), new { companyId }, result);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
        catch (DuplicateKeycloakUserException ex)
        {
            _logger.LogWarning(ex, "Duplicate Keycloak user during employee registration for {Email}", command.Email);
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = "A user with this email already exists."
            });
        }
    }

    /// <summary>GET /api/companies/{companyId}/employees — Paginated employee listing (MGMT-02).</summary>
    [HttpGet("{companyId:guid}/employees")]
    [Authorize(AuthenticationSchemes = "BearerClient")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEmployees(
        Guid companyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] string? status = null, CancellationToken ct = default)
    {
        // Company isolation (T-38-07)
        if (companyId != _currentCompanyService.CompanyId)
            return Forbid();

        var query = new GetCompanyEmployeesQuery(companyId, page, pageSize, search, status);
        var result = await _getEmployeesHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    private static CompanyProfileDto MapToDto(Company company) => new(
        Id: company.Id,
        RazaoSocial: company.RazaoSocial,
        Email: company.Email.Value,
        Phone: company.Phone.Value,
        Cnpj: company.Cnpj?.Value);
}

/// <summary>Read-only profile DTO — returned by GET /api/companies/me.</summary>
public sealed record CompanyProfileDto(
    Guid Id,
    string RazaoSocial,
    string Email,
    string Phone,
    string? Cnpj);

/// <summary>Request DTO for employee registration.</summary>
public sealed record RegisterEmployeeRequest(
    string? Nome,
    string? Cpf,
    string? Email,
    string? Phone,
    Guid? AccessGroupId);