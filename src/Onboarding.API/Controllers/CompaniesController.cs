using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.Application.Common;
using Onboarding.Application.Companies.Commands;
using Onboarding.Application.Companies.DTOs;
using Onboarding.Application.Companies.Queries;
using Onboarding.API.Security;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
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
    private readonly ICommandHandler<ToggleEmployeeStatusCommand, Unit> _toggleStatusHandler;
    private readonly ICommandHandler<ResetEmployeePasswordCommand, ResetEmployeePasswordResult> _resetPasswordHandler;
    private readonly ICommandHandler<UpdateEmployeeCommand, Unit> _updateEmployeeHandler;
    private readonly ICommandHandler<DeleteEmployeeCommand, Unit> _deleteEmployeeHandler;
    private readonly ICommandHandler<ChangeEmployeeAccessGroupCommand, Unit> _changeAccessGroupHandler;
    private readonly ICommandHandler<CreateAccessGroupCommand, AccessGroupDto> _createAccessGroupHandler;
    private readonly ICommandHandler<UpdateAccessGroupCommand, AccessGroupDto> _updateAccessGroupHandler;
    private readonly ICommandHandler<DeleteAccessGroupCommand, Unit> _deleteAccessGroupHandler;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(
        ICompanyRepository repository,
        ICommandHandler<RegisterCompanyCommand, RegisterCompanyResult> registerHandler,
        IValidator<RegisterCompanyCommand> registerValidator,
        ICommandHandler<RegisterEmployeeCommand, RegisterEmployeeResult> registerEmployeeHandler,
        IValidator<RegisterEmployeeCommand> registerEmployeeValidator,
        IQueryHandler<GetCompanyEmployeesQuery, PaginatedResult<EmployeeListItemDto>> getEmployeesHandler,
        ICommandHandler<ToggleEmployeeStatusCommand, Unit> toggleStatusHandler,
        ICommandHandler<ResetEmployeePasswordCommand, ResetEmployeePasswordResult> resetPasswordHandler,
        ICommandHandler<UpdateEmployeeCommand, Unit> updateEmployeeHandler,
        ICommandHandler<DeleteEmployeeCommand, Unit> deleteEmployeeHandler,
        ICommandHandler<ChangeEmployeeAccessGroupCommand, Unit> changeAccessGroupHandler,
        ICommandHandler<CreateAccessGroupCommand, AccessGroupDto> createAccessGroupHandler,
        ICommandHandler<UpdateAccessGroupCommand, AccessGroupDto> updateAccessGroupHandler,
        ICommandHandler<DeleteAccessGroupCommand, Unit> deleteAccessGroupHandler,
        ICurrentCompanyService currentCompanyService,
        IAccessGroupRepository accessGroupRepository,
        ILogger<CompaniesController> logger)
    {
        _repository = repository;
        _registerHandler = registerHandler;
        _registerValidator = registerValidator;
        _registerEmployeeHandler = registerEmployeeHandler;
        _registerEmployeeValidator = registerEmployeeValidator;
        _getEmployeesHandler = getEmployeesHandler;
        _toggleStatusHandler = toggleStatusHandler;
        _resetPasswordHandler = resetPasswordHandler;
        _updateEmployeeHandler = updateEmployeeHandler;
        _deleteEmployeeHandler = deleteEmployeeHandler;
        _changeAccessGroupHandler = changeAccessGroupHandler;
        _createAccessGroupHandler = createAccessGroupHandler;
        _updateAccessGroupHandler = updateAccessGroupHandler;
        _deleteAccessGroupHandler = deleteAccessGroupHandler;
        _currentCompanyService = currentCompanyService;
        _accessGroupRepository = accessGroupRepository;
        _logger = logger;
    }

    /// <summary>GET /api/companies/me — returns the authenticated company's profile.</summary>
    /// <remarks>
    /// Works for both PJ owners (looked up by keycloakSub) and PF employees
    /// (resolved via ICurrentCompanyService set by ClientClaimsMiddleware).
    /// </remarks>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "BearerClient")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var keycloakSub = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(keycloakSub))
        {
            _logger.LogWarning("Authenticated request missing 'sub' claim in JWT");
            return Unauthorized();
        }

        // Try PJ owner lookup first (sub matches Company.KeycloakUserId)
        var company = await _repository.GetByKeycloakSubAsync(keycloakSub, ct);
        if (company is not null) return Ok(MapToDto(company));

        // PF employee: ClientClaimsMiddleware already resolved CompanyId from employee lookup
        var companyId = _currentCompanyService.CompanyId;
        if (companyId != Guid.Empty)
        {
            company = await _repository.GetByIdAsync(companyId, ct);
            if (company is not null) return Ok(MapToDto(company));
        }

        _logger.LogWarning("Authenticated user with sub {Sub} not found in database", keycloakSub);
        return NotFound();
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
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.EmployeeWrite)]
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
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.EmployeeRead)]
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

    /// <summary>POST /api/companies/{companyId}/employees/{id:guid}/toggle-status — Block/unblock employee (MGMT-03).</summary>
    [HttpPost("{companyId:guid}/employees/{id:guid}/toggle-status")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.EmployeeWrite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleEmployeeStatus(
        Guid companyId, Guid id, [FromBody] ToggleStatusRequest request, CancellationToken ct)
    {
        if (companyId != _currentCompanyService.CompanyId)
            return Forbid();

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            ipAddress = forwardedFor.Split(',')[0].Trim();

        var command = new ToggleEmployeeStatusCommand(id, companyId, request.Activate, actorSub, actorEmail, ipAddress);

        try
        {
            await _toggleStatusHandler.HandleAsync(command, ct);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>POST /api/companies/{companyId}/employees/{id:guid}/reset-password — Reset employee password (MGMT-04).</summary>
    [HttpPost("{companyId:guid}/employees/{id:guid}/reset-password")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.EmployeeWrite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetEmployeePassword(
        Guid companyId, Guid id, CancellationToken ct)
    {
        if (companyId != _currentCompanyService.CompanyId)
            return Forbid();

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            ipAddress = forwardedFor.Split(',')[0].Trim();

        var command = new ResetEmployeePasswordCommand(id, companyId, actorSub, actorEmail, ipAddress);

        try
        {
            var result = await _resetPasswordHandler.HandleAsync(command, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>PUT /api/companies/{companyId}/employees/{id:guid} — Update employee data (MGMT-05).</summary>
    [HttpPut("{companyId:guid}/employees/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.EmployeeWrite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmployee(
        Guid companyId, Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct)
    {
        if (companyId != _currentCompanyService.CompanyId)
            return Forbid();

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            ipAddress = forwardedFor.Split(',')[0].Trim();

        var command = new UpdateEmployeeCommand(id, companyId, request.Nome ?? string.Empty, request.Email ?? string.Empty, request.Phone ?? string.Empty, actorSub, actorEmail, ipAddress);

        try
        {
            await _updateEmployeeHandler.HandleAsync(command, ct);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>DELETE /api/companies/{companyId}/employees/{id:guid} — LGPD delete employee (MGMT-05).</summary>
    [HttpDelete("{companyId:guid}/employees/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.EmployeeDelete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmployee(
        Guid companyId, Guid id, CancellationToken ct)
    {
        if (companyId != _currentCompanyService.CompanyId)
            return Forbid();

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            ipAddress = forwardedFor.Split(',')[0].Trim();

        var command = new DeleteEmployeeCommand(id, companyId, actorSub, actorEmail, ipAddress);

        try
        {
            await _deleteEmployeeHandler.HandleAsync(command, ct);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>PUT /api/companies/{companyId}/employees/{id:guid}/access-group — Change access group (T-38-11).</summary>
    [HttpPut("{companyId:guid}/employees/{id:guid}/access-group")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.AccessGroupsManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeEmployeeAccessGroup(
        Guid companyId, Guid id, [FromBody] ChangeAccessGroupRequest request, CancellationToken ct)
    {
        if (companyId != _currentCompanyService.CompanyId)
            return Forbid();

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            ipAddress = forwardedFor.Split(',')[0].Trim();

        var command = new ChangeEmployeeAccessGroupCommand(id, companyId, request.AccessGroupId, actorSub, actorEmail, ipAddress);

        try
        {
            await _changeAccessGroupHandler.HandleAsync(command, ct);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>GET /api/companies/{companyId}/access-groups — List access groups for the company (PERM-04).</summary>
    [HttpGet("{companyId:guid}/access-groups")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.EmployeeRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAccessGroups(Guid companyId, CancellationToken ct)
    {
        if (companyId != _currentCompanyService.CompanyId)
            return Forbid();

        var groups = await _accessGroupRepository.GetByCompanyIdAsync(companyId, ct);
        var dtos = groups.Select(g => new AccessGroupDto(g.Id, g.Name, (IReadOnlyList<string>)g.Permissions, g.IsDefault)).ToList();
        return Ok(dtos);
    }

    /// <summary>POST /api/companies/{companyId}/access-groups — Create a custom access group (PERM-06).</summary>
    [HttpPost("{companyId:guid}/access-groups")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.AccessGroupsManage)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAccessGroup(Guid companyId, [FromBody] CreateAccessGroupRequest request, CancellationToken ct)
    {
        if (companyId != _currentCompanyService.CompanyId)
            return Forbid();

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var command = new CreateAccessGroupCommand(companyId, request.Name, request.Permissions, actorSub, actorEmail, ipAddress);

        try
        {
            var result = await _createAccessGroupHandler.HandleAsync(command, ct);
            return CreatedAtAction(nameof(GetAccessGroups), new { companyId }, result);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { title = "Bad request", status = 400, detail = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>PUT /api/companies/{companyId}/access-groups/{id} — Update a custom access group (PERM-06).</summary>
    [HttpPut("{companyId:guid}/access-groups/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.AccessGroupsManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAccessGroup(Guid companyId, Guid id, [FromBody] UpdateAccessGroupRequest request, CancellationToken ct)
    {
        if (companyId != _currentCompanyService.CompanyId)
            return Forbid();

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var command = new UpdateAccessGroupCommand(companyId, id, request.Name, request.Permissions, actorSub, actorEmail, ipAddress);

        try
        {
            var result = await _updateAccessGroupHandler.HandleAsync(command, ct);
            return Ok(result);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { title = "Bad request", status = 400, detail = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>DELETE /api/companies/{companyId}/access-groups/{id} — Delete a custom access group (PERM-06).</summary>
    [HttpDelete("{companyId:guid}/access-groups/{id:guid}")]
    [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.AccessGroupsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccessGroup(Guid companyId, Guid id, CancellationToken ct)
    {
        if (companyId != _currentCompanyService.CompanyId)
            return Forbid();

        var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;
        var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var command = new DeleteAccessGroupCommand(companyId, id, actorSub, actorEmail, ipAddress);

        try
        {
            await _deleteAccessGroupHandler.HandleAsync(command, ct);
            return NoContent();
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { title = "Bad request", status = 400, detail = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
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

/// <summary>Request DTO for toggle employee status.</summary>
public sealed record ToggleStatusRequest(bool Activate);

/// <summary>Request DTO for updating employee data.</summary>
public sealed record UpdateEmployeeRequest(string? Nome, string? Email, string? Phone);

/// <summary>Request DTO for changing employee access group.</summary>
public sealed record ChangeAccessGroupRequest(Guid AccessGroupId);

/// <summary>Request DTO for creating an access group.</summary>
public sealed record CreateAccessGroupRequest(string Name, IReadOnlyList<string> Permissions);

/// <summary>Request DTO for updating an access group.</summary>
public sealed record UpdateAccessGroupRequest(string? Name, IReadOnlyList<string>? Permissions);