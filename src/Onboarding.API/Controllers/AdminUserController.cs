using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Admin.Queries;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;

namespace Onboarding.API.Controllers;

/// <summary>
/// Admin management endpoints — requires admin role.
/// Operates on Company/Employee (Phase 37 — D-19).
/// Phase 35 admin management (administrators) kept as-is.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(AuthenticationSchemes = "BearerBackoffice", Roles = "admin")]
public sealed class AdminUserController : ControllerBase
{
    // Company/Employee handlers (Phase 37 — D-19)
    private readonly IQueryHandler<GetPaginatedCompaniesQuery, PaginatedResult<CompanySummaryDto>> _paginatedCompaniesHandler;
    private readonly IQueryHandler<GetCompanyDetailsQuery, CompanySummaryDto> _companyDetailsHandler;
    private readonly IQueryHandler<GetPaginatedEmployeesQuery, PaginatedResult<EmployeeSummaryDto>> _paginatedEmployeesHandler;
    private readonly IQueryHandler<GetEmployeeDetailsQuery, EmployeeSummaryDto> _employeeDetailsHandler;
    private readonly ICommandHandler<UpdateCompanyCommand, Unit> _updateCompanyHandler;
    private readonly ICommandHandler<DeleteEmployeeCommand, Unit> _deleteEmployeeHandler;
    private readonly ICommandHandler<BlockEmployeeCommand, Unit> _blockEmployeeHandler;
    private readonly ICommandHandler<UnblockEmployeeCommand, Unit> _unblockEmployeeHandler;

    // Phase 29 — Admin management
    private readonly ICommandHandler<CreateAdminCommand, CreateAdminResult> _createAdminHandler;
    private readonly ICommandHandler<ForcePasswordChangeCommand, Unit> _forcePasswordChangeHandler;
    private readonly IQueryHandler<GetAuditLogQuery, PaginatedResult<AdminAuditLogDto>> _auditLogQueryHandler;
    private readonly IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>> _administratorsHandler;

    // Phase 35 — Admin management
    private readonly IQueryHandler<GetPaginatedAdministratorsQuery, PaginatedResult<AdminUserDto>> _paginatedAdministratorsHandler;
    private readonly ICommandHandler<UpdateAdministratorCommand, Unit> _updateAdministratorHandler;
    private readonly ICommandHandler<ResetAdministratorPasswordCommand, ResetAdministratorPasswordResult> _resetAdminPasswordHandler;
    private readonly ICommandHandler<ToggleAdministratorStatusCommand, Unit> _toggleAdminStatusHandler;

    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IValidator<CreateAdminCommand> _createAdminValidator;
    private readonly IValidator<ForcePasswordChangeCommand> _forcePasswordChangeValidator;
    private readonly IValidator<UpdateAdministratorCommand> _updateAdministratorValidator;
    private readonly IValidator<ResetAdministratorPasswordCommand> _resetAdminPasswordValidator;
    private readonly IValidator<ToggleAdministratorStatusCommand> _toggleAdminStatusValidator;
    private readonly ILogger<AdminUserController> _logger;

    public AdminUserController(
        // Company/Employee handlers (Phase 37)
        IQueryHandler<GetPaginatedCompaniesQuery, PaginatedResult<CompanySummaryDto>> paginatedCompaniesHandler,
        IQueryHandler<GetCompanyDetailsQuery, CompanySummaryDto> companyDetailsHandler,
        IQueryHandler<GetPaginatedEmployeesQuery, PaginatedResult<EmployeeSummaryDto>> paginatedEmployeesHandler,
        IQueryHandler<GetEmployeeDetailsQuery, EmployeeSummaryDto> employeeDetailsHandler,
        ICommandHandler<UpdateCompanyCommand, Unit> updateCompanyHandler,
        ICommandHandler<DeleteEmployeeCommand, Unit> deleteEmployeeHandler,
        ICommandHandler<BlockEmployeeCommand, Unit> blockEmployeeHandler,
        ICommandHandler<UnblockEmployeeCommand, Unit> unblockEmployeeHandler,
        // Phase 29 — Admin management
        ICommandHandler<CreateAdminCommand, CreateAdminResult> createAdminHandler,
        ICommandHandler<ForcePasswordChangeCommand, Unit> forcePasswordChangeHandler,
        IQueryHandler<GetAuditLogQuery, PaginatedResult<AdminAuditLogDto>> auditLogQueryHandler,
        IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>> administratorsHandler,
        // Phase 35 — Admin management
        IQueryHandler<GetPaginatedAdministratorsQuery, PaginatedResult<AdminUserDto>> paginatedAdministratorsHandler,
        ICommandHandler<UpdateAdministratorCommand, Unit> updateAdministratorHandler,
        ICommandHandler<ResetAdministratorPasswordCommand, ResetAdministratorPasswordResult> resetAdminPasswordHandler,
        ICommandHandler<ToggleAdministratorStatusCommand, Unit> toggleAdminStatusHandler,
        IKeycloakUserService keycloakUserService,
        IValidator<CreateAdminCommand> createAdminValidator,
        IValidator<ForcePasswordChangeCommand> forcePasswordChangeValidator,
        IValidator<UpdateAdministratorCommand> updateAdministratorValidator,
        IValidator<ResetAdministratorPasswordCommand> resetAdminPasswordValidator,
        IValidator<ToggleAdministratorStatusCommand> toggleAdminStatusValidator,
        ILogger<AdminUserController> logger)
    {
        _paginatedCompaniesHandler = paginatedCompaniesHandler;
        _companyDetailsHandler = companyDetailsHandler;
        _paginatedEmployeesHandler = paginatedEmployeesHandler;
        _employeeDetailsHandler = employeeDetailsHandler;
        _updateCompanyHandler = updateCompanyHandler;
        _deleteEmployeeHandler = deleteEmployeeHandler;
        _blockEmployeeHandler = blockEmployeeHandler;
        _unblockEmployeeHandler = unblockEmployeeHandler;
        _createAdminHandler = createAdminHandler;
        _forcePasswordChangeHandler = forcePasswordChangeHandler;
        _auditLogQueryHandler = auditLogQueryHandler;
        _administratorsHandler = administratorsHandler;
        _paginatedAdministratorsHandler = paginatedAdministratorsHandler;
        _updateAdministratorHandler = updateAdministratorHandler;
        _resetAdminPasswordHandler = resetAdminPasswordHandler;
        _toggleAdminStatusHandler = toggleAdminStatusHandler;
        _keycloakUserService = keycloakUserService;
        _createAdminValidator = createAdminValidator;
        _forcePasswordChangeValidator = forcePasswordChangeValidator;
        _updateAdministratorValidator = updateAdministratorValidator;
        _resetAdminPasswordValidator = resetAdminPasswordValidator;
        _toggleAdminStatusValidator = toggleAdminStatusValidator;
        _logger = logger;
    }

    // ─── Company endpoints ────────────────────────────────────────────

    /// <summary>GET /api/admin/companies — Paginated list of companies with search and status filters.</summary>
    [HttpGet("companies")]
    [ProducesResponseType(typeof(PaginatedResult<CompanySummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompanies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var query = new GetPaginatedCompaniesQuery(page, pageSize, search, status);
        var result = await _paginatedCompaniesHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/admin/companies/{id} — Detailed company data.</summary>
    [HttpGet("companies/{id:guid}")]
    [ProducesResponseType(typeof(CompanySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompanyDetails(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        var query = new GetCompanyDetailsQuery(id);
        var result = await _companyDetailsHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>PUT /api/admin/companies/{id} — Update company data.</summary>
    [HttpPut("companies/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCompany(
        [FromRoute] Guid id,
        [FromBody] UpdateCompanyRequest request,
        CancellationToken ct = default)
    {
        var command = new UpdateCompanyCommand(id, request.RazaoSocial ?? string.Empty, request.Email ?? string.Empty, request.Phone ?? string.Empty);
        await _updateCompanyHandler.HandleAsync(command, ct);
        return NoContent();
    }

    // ─── Employee endpoints ────────────────────────────────────────────

    /// <summary>GET /api/admin/employees — Paginated list of employees with search, status and company filters.</summary>
    [HttpGet("employees")]
    [ProducesResponseType(typeof(PaginatedResult<EmployeeSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? companyId = null,
        CancellationToken ct = default)
    {
        var query = new GetPaginatedEmployeesQuery(page, pageSize, search, status, companyId);
        var result = await _paginatedEmployeesHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/admin/employees/{id} — Detailed employee data.</summary>
    [HttpGet("employees/{id:guid}")]
    [ProducesResponseType(typeof(EmployeeSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeDetails(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        var query = new GetEmployeeDetailsQuery(id);
        var result = await _employeeDetailsHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>POST /api/admin/employees/{id}/block — Block employee account.</summary>
    [HttpPost("employees/{id:guid}/block")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BlockEmployee(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        var auditContext = GetAuditContext();
        var command = new BlockEmployeeCommand(id, auditContext.Sub);
        await _blockEmployeeHandler.HandleAsync(command, ct);
        return NoContent();
    }

    /// <summary>POST /api/admin/employees/{id}/unblock — Unblock employee account.</summary>
    [HttpPost("employees/{id:guid}/unblock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnblockEmployee(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        var auditContext = GetAuditContext();
        var command = new UnblockEmployeeCommand(id, auditContext.Sub);
        await _unblockEmployeeHandler.HandleAsync(command, ct);
        return NoContent();
    }

    /// <summary>DELETE /api/admin/employees/{id} — LGPD-compliant employee deletion.</summary>
    [HttpDelete("employees/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmployee(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        var auditContext = GetAuditContext();
        var command = new DeleteEmployeeCommand(id, auditContext.Sub);
        await _deleteEmployeeHandler.HandleAsync(command, ct);
        return NoContent();
    }

    // ─── Phase 29/35 — Admin management endpoints (unchanged) ──────────

    /// <summary>POST /api/admin/administrators — Create a new admin user with a temporary password.</summary>
    [HttpPost("administrators")]
    [ProducesResponseType(typeof(CreateAdminResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAdmin(
        [FromBody] CreateAdminRequest request,
        CancellationToken ct = default)
    {
        var auditContext = GetAuditContextSafe();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var command = new CreateAdminCommand(request.FullName, request.Email, auditContext.Sub, auditContext.Email, ipAddress);

        var validation = await _createAdminValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return UnprocessableEntity(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        try
        {
            var result = await _createAdminHandler.HandleAsync(command, ct);
            return CreatedAtAction(nameof(CreateAdmin), new { id = result.AdminId }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message
            });
        }
    }

    /// <summary>GET /api/admin/administrators — Lista todos os administradores via Keycloak (sem paginação).</summary>
    [HttpGet("administrators")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAdministrators(CancellationToken ct = default)
    {
        try
        {
            var result = await _administratorsHandler.HandleAsync(new GetAdministratorsQuery(), ct);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Keycloak unavailable when listing administrators");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Service Unavailable",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = "Unable to retrieve administrators from identity provider."
            });
        }
    }

    /// <summary>GET /api/admin/administrators/paginated — Paginated+filtered list of administrators (MGMT-01, MGMT-02).</summary>
    [HttpGet("administrators/paginated")]
    [ProducesResponseType(typeof(PaginatedResult<AdminUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAdministratorsPaginated(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? name = null,
        [FromQuery] string? email = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = new GetPaginatedAdministratorsQuery(page, pageSize, name, email, status);
            var result = await _paginatedAdministratorsHandler.HandleAsync(query, ct);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Keycloak unavailable when listing administrators");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Service Unavailable",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = "Unable to retrieve administrators from identity provider."
            });
        }
    }

    /// <summary>PUT /api/admin/administrators/{id} — Edit admin name and email (MGMT-03, SEC-01, SEC-04, AUD-04).</summary>
    [HttpPut("administrators/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateAdministrator(
        [FromRoute] string id,
        [FromBody] UpdateAdministratorRequest request,
        CancellationToken ct = default)
    {
        var audit = GetAuditContextSafe();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new UpdateAdministratorCommand(id, request.FullName ?? string.Empty, request.Email ?? string.Empty, audit.Sub, audit.Email, ip);

        var validation = await _updateAdministratorValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            await _updateAdministratorHandler.HandleAsync(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Problem("Administrator not found.", statusCode: 404));
        }
        catch (ArgumentException ex)
        {
            return Conflict(Problem(ex.Message, statusCode: 409));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Problem(ex.Message, statusCode: 400));
        }
    }

    /// <summary>POST /api/admin/administrators/{id}/reset-password — Reset password and return one-time temp password (MGMT-04, SEC-01, SEC-03, AUD-05).</summary>
    [HttpPost("administrators/{id:guid}/reset-password")]
    [ProducesResponseType(typeof(ResetAdministratorPasswordResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetAdministratorPassword(
        [FromRoute] string id,
        [FromBody] ResetAdministratorPasswordRequest request,
        CancellationToken ct = default)
    {
        var audit = GetAuditContextSafe();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new ResetAdministratorPasswordCommand(id, request.TargetUserName ?? string.Empty, audit.Sub, audit.Email, ip);

        var validation = await _resetAdminPasswordValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            var result = await _resetAdminPasswordHandler.HandleAsync(command, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Problem("Administrator not found.", statusCode: 404));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Problem(ex.Message, statusCode: 400));
        }
    }

    /// <summary>POST /api/admin/administrators/{id}/toggle-status — Disable or reactivate admin (MGMT-05, MGMT-06, SEC-01, SEC-05, AUD-06).</summary>
    [HttpPost("administrators/{id:guid}/toggle-status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ToggleAdministratorStatus(
        [FromRoute] string id,
        [FromBody] ToggleAdministratorStatusRequest request,
        CancellationToken ct = default)
    {
        var audit = GetAuditContextSafe();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new ToggleAdministratorStatusCommand(
            id, request.TargetUserName ?? string.Empty,
            request.Activate, request.Reason,
            audit.Sub, audit.Email, ip);

        var validation = await _toggleAdminStatusValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(ToValidationProblem(validation));

        try
        {
            await _toggleAdminStatusHandler.HandleAsync(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Problem("Administrator not found.", statusCode: 404));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Problem(ex.Message, statusCode: 400));
        }
    }

    /// <summary>PUT /api/admin/me/password — Force password change for current admin (first login).</summary>
    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ForcePasswordChange(
        [FromBody] ForcePasswordChangeRequest request,
        CancellationToken ct = default)
    {
        var adminEmail = HttpContext.Items["AdminEmail"] as string
            ?? User.FindFirst("email")?.Value
            ?? User.FindFirst("preferred_username")?.Value
            ?? throw new InvalidOperationException("Missing admin email context.");
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var keycloakUser = await _keycloakUserService.GetUserByEmailAsync("backoffice", adminEmail, ct)
            ?? throw new InvalidOperationException($"Keycloak user not found for email: {adminEmail}");
        var keycloakUserId = keycloakUser.Id;

        var command = new ForcePasswordChangeCommand(keycloakUserId, adminEmail, request.NewPassword, ipAddress);

        var validation = await _forcePasswordChangeValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return UnprocessableEntity(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        await _forcePasswordChangeHandler.HandleAsync(command, ct);
        return NoContent();
    }

    /// <summary>POST /api/admin/me/complete-first-login — Clears isFirstLogin flag after admin finished first login + password change.</summary>
    [HttpPost("me/complete-first-login")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteFirstLogin(CancellationToken ct = default)
    {
        var adminEmail = HttpContext.Items["AdminEmail"] as string
            ?? User.FindFirst("email")?.Value
            ?? User.FindFirst("preferred_username")?.Value
            ?? throw new InvalidOperationException("Missing admin email context.");

        var keycloakUser = await _keycloakUserService.GetUserByEmailAsync("backoffice", adminEmail, ct)
            ?? throw new InvalidOperationException($"Keycloak user not found for email: {adminEmail}");

        await _keycloakUserService.ClearFirstLoginFlagAsync("backoffice", keycloakUser.Id, ct);
        _logger.LogInformation("Admin {AdminEmail} completed first login; isFirstLogin flag cleared.", adminEmail);
        return NoContent();
    }

    /// <summary>GET /api/admin/audit-log — Paginated audit log with filters.</summary>
    [HttpGet("audit-log")]
    [ProducesResponseType(typeof(PaginatedResult<AdminAuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null,
        [FromQuery] string? actionType = null,
        [FromQuery] string? adminUserName = null,
        CancellationToken ct = default)
    {
        ActionType? parsedActionType = null;
        if (!string.IsNullOrWhiteSpace(actionType) && Enum.TryParse<ActionType>(actionType, out var parsed))
            parsedActionType = parsed;

        var query = new GetAuditLogQuery(page, pageSize, startDate, endDate, parsedActionType, adminUserName);
        var result = await _auditLogQueryHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Extracts admin identity from JWT claims for audit logging. Throws if claims missing.</summary>
    private (string Sub, string Email) GetAuditContext()
    {
        var sub = User.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("Missing 'sub' claim.");
        var email = User.FindFirst("email")?.Value
            ?? throw new InvalidOperationException("Missing 'email' claim.");
        return (sub, email);
    }

    /// <summary>Extracts admin identity from JWT claims for audit logging. Falls back to cookie if claims missing.</summary>
    private (string Sub, string Email) GetAuditContextSafe()
    {
        var email = HttpContext.Items["AdminEmail"] as string
            ?? User.FindFirst("email")?.Value
            ?? "unknown";
        var sub = User.FindFirst("sub")?.Value
            ?? User.FindFirst("preferred_username")?.Value
            ?? email;
        return (sub, email);
    }

    /// <summary>Converts a FluentValidation result into a ValidationProblemDetails response body.</summary>
    private static ValidationProblemDetails ToValidationProblem(FluentValidation.Results.ValidationResult result)
        => new(result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
}

/// <summary>PUT request body for updating a company (Phase 37 — D-19).</summary>
public sealed record UpdateCompanyRequest(string? RazaoSocial, string? Email, string? Phone);

/// <summary>POST request body for creating a new admin user.</summary>
public sealed record CreateAdminRequest(string FullName, string Email);

/// <summary>PUT request body for forcing password change.</summary>
public sealed record ForcePasswordChangeRequest(string NewPassword);

/// <summary>PUT request body for editing an administrator (Phase 35 — MGMT-03).</summary>
public sealed record UpdateAdministratorRequest(string? FullName, string? Email);

/// <summary>POST request body for resetting an administrator's password (Phase 35 — MGMT-04).</summary>
public sealed record ResetAdministratorPasswordRequest(string? TargetUserName);

/// <summary>POST request body for enabling or disabling an administrator (Phase 35 — MGMT-05, MGMT-06).</summary>
public sealed record ToggleAdministratorStatusRequest(bool Activate, string? TargetUserName, string? Reason);