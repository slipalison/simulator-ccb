using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;
using Onboarding.Infrastructure.Keycloak;

namespace Onboarding.API.Controllers;

/// <summary>
/// Authentication endpoints — public (no [Authorize]).
/// POST /api/auth/login — D-01: ROPC token exchange via IKeycloakTokenService
/// POST /api/auth/refresh — D-02: token refresh via IKeycloakTokenService
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly ICommandHandler<LoginCommand, TokenResponse> _loginHandler;
    private readonly ICommandHandler<RefreshTokenCommand, TokenResponse> _refreshHandler;
    private readonly IValidator<LoginCommand> _loginValidator;
    private readonly IValidator<RefreshTokenCommand> _refreshValidator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ICommandHandler<LoginCommand, TokenResponse> loginHandler,
        ICommandHandler<RefreshTokenCommand, TokenResponse> refreshHandler,
        IValidator<LoginCommand> loginValidator,
        IValidator<RefreshTokenCommand> refreshValidator,
        ILogger<AuthController> logger)
    {
        _loginHandler = loginHandler;
        _refreshHandler = refreshHandler;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
        _logger = logger;
    }

    /// <summary>POST /api/auth/login — AUTH-02: exchange credentials for JWT token pair.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var command = new LoginCommand(request.Email ?? string.Empty, request.Password ?? string.Empty);

        var validation = await _loginValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return UnprocessableEntity(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        try
        {
            var tokens = await _loginHandler.HandleAsync(command, ct);
            return Ok(tokens);
        }
        catch (KeycloakAuthException ex)
        {
            // D-13 + SEC-08: generic error — do not reveal if email exists or not
            _logger.LogWarning(ex, "Login attempt failed for email {Email}", request.Email);
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Invalid credentials."
            });
        }
    }

    /// <summary>POST /api/auth/refresh — AUTH-04: exchange refresh token for new token pair.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        var command = new RefreshTokenCommand(request.RefreshToken ?? string.Empty);

        var validation = await _refreshValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return UnprocessableEntity(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        try
        {
            var tokens = await _refreshHandler.HandleAsync(command, ct);
            return Ok(tokens);
        }
        catch (KeycloakAuthException ex)
        {
            // D-13 + SEC-08: generic error for invalid/expired refresh token
            _logger.LogWarning(ex, "Refresh token exchange failed");
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Invalid or expired refresh token."
            });
        }
    }
}

// HTTP request DTOs for AuthController — nullable to allow FluentValidation to handle missing fields
// (non-nullable would cause 400 from model binding before FluentValidation runs)
public sealed record LoginRequest(string? Email, string? Password);
public sealed record RefreshTokenRequest(string? RefreshToken);
