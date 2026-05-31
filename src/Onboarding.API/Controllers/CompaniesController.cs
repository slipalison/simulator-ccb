using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.API.Extensions;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Application.Companies.Commands;
using Onboarding.Application.Companies.DTOs;
using Onboarding.Application.Companies.Queries;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using CompaniesDeleteEmployeeCommand = Onboarding.Application.Companies.Commands.DeleteEmployeeCommand;

namespace Onboarding.API.Controllers;

/// <summary>
/// Company endpoints.
/// GET /api/companies/me — AUTH-03: protected route returns profile of authenticated company.
/// POST /api/companies/registration — REG-01: PJ company registration with Keycloak user creation.
/// POST /api/companies/{companyId}/employees — REG-03: PJ registers employee (PF) with temp password.
/// GET /api/companies/{companyId}/employees — MGMT-02: PJ lists employees with pagination/filters.
/// D-62: 5 ctor deps (was 17): ICommandDispatcher + IQueryDispatcher + IValidationRunner
///       + ICurrentCompanyService + ILogger. Repos used only in specific actions are [FromServices].
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class CompaniesController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;
    private readonly IValidationRunner _validation;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        IValidationRunner validation,
        ICurrentCompanyService currentCompanyService,
        ILogger<CompaniesController> logger)
    {
        _commands = commands;
        _queries = queries;
        _validation = validation;
        _currentCompanyService = currentCompanyService;
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
    public async Task<IActionResult> GetMe(
        [FromServices] ICompanyRepository repository,
        CancellationToken ct)
    {
        var keycloakSub = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(keycloakSub))
        {
            _logger.LogWarning("Authenticated request missing 'sub' claim in JWT");
            return Unauthorized();
        }

        // Try PJ owner lookup first (sub matches Company.KeycloakUserId)
        var company = await repository.GetByKeycloakSubAsync(keycloakSub, ct);
        if (company is not null) return Ok(MapToDto(company));

        // PF employee: ClientClaimsMiddleware already resolved CompanyId from employee lookup
        var companyId = _currentCompanyService.CompanyId;
        if (companyId != Guid.Empty)
        {
            company = await repository.GetByIdAsync(companyId, ct);
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

        var ipAddress = ResolveClientIpAddress();

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
        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(validation.ToValidationProblem());

        try
        {
            var result = await _commands.Send<RegisterCompanyResult>(command, ct);
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
            _logger.LogWarning(ex, "Duplicate Keycloak user during registration for {Email}",
                Onboarding.API.Observability.SensitiveDataDestructuringPolicy.MaskEmail(command.Email ?? string.Empty));
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

        var (actorSub, actorEmail) = GetActorContext();
        var ipAddress = ResolveClientIpAddress();

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

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(validation.ToValidationProblem());

        try
        {
            var result = await _commands.Send<RegisterEmployeeResult>(command, ct);
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
            _logger.LogWarning(ex, "Duplicate Keycloak user during employee registration for {Email}",
                Onboarding.API.Observability.SensitiveDataDestructuringPolicy.MaskEmail(command.Email ?? string.Empty));
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
        var result = await _queries.Query<PaginatedResult<EmployeeListItemDto>>(query, ct);
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

        var (actorSub, actorEmail) = GetActorContext();
        var ipAddress = ResolveClientIpAddress();

        var command = new ToggleEmployeeStatusCommand(id, companyId, request.Activate, actorSub, actorEmail, ipAddress);

        try
        {
            await _commands.Send<Unit>(command, ct);
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

        var (actorSub, actorEmail) = GetActorContext();
        var ipAddress = ResolveClientIpAddress();

        var command = new ResetEmployeePasswordCommand(id, companyId, actorSub, actorEmail, ipAddress);

        try
        {
            var result = await _commands.Send<ResetEmployeePasswordResult>(command, ct);
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

        var (actorSub, actorEmail) = GetActorContext();
        var ipAddress = ResolveClientIpAddress();

        var command = new UpdateEmployeeCommand(id, companyId, request.Nome ?? string.Empty, request.Email ?? string.Empty, request.Phone ?? string.Empty, actorSub, actorEmail, ipAddress);

        try
        {
            await _commands.Send<Unit>(command, ct);
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

        var (actorSub, actorEmail) = GetActorContext();
        var ipAddress = ResolveClientIpAddress();

        var command = new CompaniesDeleteEmployeeCommand(id, companyId, actorSub, actorEmail, ipAddress);

        try
        {
            await _commands.Send<Unit>(command, ct);
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

        var (actorSub, actorEmail) = GetActorContext();
        var ipAddress = ResolveClientIpAddress();

        var command = new ChangeEmployeeAccessGroupCommand(id, companyId, request.AccessGroupId, actorSub, actorEmail, ipAddress);

        try
        {
            await _commands.Send<Unit>(command, ct);
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
    public async Task<IActionResult> GetAccessGroups(
        Guid companyId,
        [FromServices] IAccessGroupRepository accessGroupRepository,
        CancellationToken ct)
    {
        if (companyId != _currentCompanyService.CompanyId)
            return Forbid();

        var groups = await accessGroupRepository.GetByCompanyIdAsync(companyId, ct);
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

        var (actorSub, actorEmail) = GetActorContext();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var command = new CreateAccessGroupCommand(companyId, request.Name, request.Permissions, actorSub, actorEmail, ipAddress);

        try
        {
            var result = await _commands.Send<AccessGroupDto>(command, ct);
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

        var (actorSub, actorEmail) = GetActorContext();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var command = new UpdateAccessGroupCommand(companyId, id, request.Name, request.Permissions, actorSub, actorEmail, ipAddress);

        try
        {
            var result = await _commands.Send<AccessGroupDto>(command, ct);
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

        var (actorSub, actorEmail) = GetActorContext();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var command = new DeleteAccessGroupCommand(companyId, id, actorSub, actorEmail, ipAddress);

        try
        {
            await _commands.Send<Unit>(command, ct);
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

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>Extracts ActorSub and ActorEmail from the authenticated JWT for audit trail (DRY-02).</summary>
    private (string ActorSub, string ActorEmail) GetActorContext()
        => (User.FindFirst("sub")?.Value ?? string.Empty,
            User.FindFirst("email")?.Value ?? string.Empty);

    /// <summary>
    /// Resolves the caller IP address, preferring the first value from X-Forwarded-For when present.
    /// </summary>
    private string? ResolveClientIpAddress()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();
        return HttpContext.Connection.RemoteIpAddress?.ToString();
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
