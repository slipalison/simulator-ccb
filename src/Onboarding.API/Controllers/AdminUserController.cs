using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Admin.Queries;
using Onboarding.Application.Common;

namespace Onboarding.API.Controllers;

/// <summary>
/// Admin user management endpoints — requires admin role.
/// All endpoints are protected by [Authorize(Roles = "admin")].
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "admin")]
public sealed class AdminUserController : ControllerBase
{
    private readonly IQueryHandler<GetPaginatedUsersQuery, PaginatedResult<UserSummaryDto>> _paginatedHandler;
    private readonly IQueryHandler<GetUserDetailsQuery, UserDetailDto> _detailsHandler;
    private readonly ICommandHandler<UpdateUserCommand, Unit> _updateHandler;
    private readonly ICommandHandler<BlockUserCommand, Unit> _blockHandler;
    private readonly ICommandHandler<UnblockUserCommand, Unit> _unblockHandler;
    private readonly ICommandHandler<DeleteUserCommand, Unit> _deleteHandler;
    private readonly IValidator<UpdateUserCommand> _updateValidator;
    private readonly IValidator<DeleteUserCommand> _deleteValidator;
    private readonly ILogger<AdminUserController> _logger;

    public AdminUserController(
        IQueryHandler<GetPaginatedUsersQuery, PaginatedResult<UserSummaryDto>> paginatedHandler,
        IQueryHandler<GetUserDetailsQuery, UserDetailDto> detailsHandler,
        ICommandHandler<UpdateUserCommand, Unit> updateHandler,
        ICommandHandler<BlockUserCommand, Unit> blockHandler,
        ICommandHandler<UnblockUserCommand, Unit> unblockHandler,
        ICommandHandler<DeleteUserCommand, Unit> deleteHandler,
        IValidator<UpdateUserCommand> updateValidator,
        IValidator<DeleteUserCommand> deleteValidator,
        ILogger<AdminUserController> logger)
    {
        _paginatedHandler = paginatedHandler;
        _detailsHandler = detailsHandler;
        _updateHandler = updateHandler;
        _blockHandler = blockHandler;
        _unblockHandler = unblockHandler;
        _deleteHandler = deleteHandler;
        _updateValidator = updateValidator;
        _deleteValidator = deleteValidator;
        _logger = logger;
    }

    /// <summary>GET /api/admin/users — Paginated list of users with search and status filters.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(PaginatedResult<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var query = new GetPaginatedUsersQuery(page, pageSize, search, status);
        var result = await _paginatedHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>GET /api/admin/users/{id} — Detailed user data including Keycloak status.</summary>
    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserDetails(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        try
        {
            var query = new GetUserDetailsQuery(id);
            var result = await _detailsHandler.HandleAsync(query, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found",
                Status = StatusCodes.Status404NotFound,
                Detail = "The requested user does not exist."
            });
        }
    }

    /// <summary>PUT /api/admin/users/{id} — Update user data.</summary>
    [HttpPut("users/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateUser(
        [FromRoute] Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct = default)
    {
        var auditContext = GetAuditContext();

        var command = new UpdateUserCommand(
            id,
            request.Name ?? string.Empty,
            request.RazaoSocial,
            request.Email ?? string.Empty,
            request.Phone ?? string.Empty,
            auditContext.Sub,
            auditContext.Email);

        var validation = await _updateValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return UnprocessableEntity(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        try
        {
            await _updateHandler.HandleAsync(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found",
                Status = StatusCodes.Status404NotFound,
                Detail = "The requested user does not exist."
            });
        }
        catch (ArgumentException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message
            });
        }
    }

    /// <summary>POST /api/admin/users/{id}/block — Block user account.</summary>
    [HttpPost("users/{id:guid}/block")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> BlockUser(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        var auditContext = GetAuditContext();
        var command = new BlockUserCommand(id, auditContext.Sub, auditContext.Email);

        try
        {
            await _blockHandler.HandleAsync(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found",
                Status = StatusCodes.Status404NotFound,
                Detail = "The requested user does not exist."
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to block user {UserId}", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Service unavailable",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = "Unable to process this request. Please try again later."
            });
        }
    }

    /// <summary>POST /api/admin/users/{id}/unblock — Unblock user account.</summary>
    [HttpPost("users/{id:guid}/unblock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UnblockUser(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        var auditContext = GetAuditContext();
        var command = new UnblockUserCommand(id, auditContext.Sub, auditContext.Email);

        try
        {
            await _unblockHandler.HandleAsync(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found",
                Status = StatusCodes.Status404NotFound,
                Detail = "The requested user does not exist."
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to unblock user {UserId}", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Service unavailable",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = "Unable to process this request. Please try again later."
            });
        }
    }

    /// <summary>DELETE /api/admin/users/{id} — LGPD-compliant user deletion.</summary>
    [HttpDelete("users/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUser(
        [FromRoute] Guid id,
        [FromBody] DeleteUserRequest request,
        CancellationToken ct = default)
    {
        var auditContext = GetAuditContext();
        var command = new DeleteUserCommand(
            id,
            request.ConfirmEmail ?? string.Empty,
            auditContext.Sub,
            auditContext.Email);

        var validation = await _deleteValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        try
        {
            await _deleteHandler.HandleAsync(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found",
                Status = StatusCodes.Status404NotFound,
                Detail = "The requested user does not exist."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message
            });
        }
    }

    /// <summary>Extracts admin identity from JWT claims for audit logging.</summary>
    private (string Sub, string Email) GetAuditContext()
    {
        var sub = User.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("Missing 'sub' claim.");
        var email = User.FindFirst("email")?.Value
            ?? throw new InvalidOperationException("Missing 'email' claim.");
        return (sub, email);
    }
}

/// <summary>DELETE request body for LGPD user deletion.</summary>
public sealed record DeleteUserRequest(string? ConfirmEmail);
