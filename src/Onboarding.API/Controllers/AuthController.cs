using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Exceptions;
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

            // Set refresh token as httpOnly cookie (secure against XSS)
            Response.Cookies.Append("refreshToken", tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // true in production (HTTPS)
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddSeconds(tokens.RefreshExpiresIn),
                Path = "/api/auth/refresh" // Only sent to refresh endpoint
            });

            // Return access token only (refresh token in cookie)
            return Ok(new
            {
                tokens.AccessToken,
                tokens.ExpiresIn,
                tokens.TokenType,
                tokens.Scope
            });
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
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        CancellationToken ct)
    {
        // Read refresh token from httpOnly cookie
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "No refresh token available."
            });
        }

        var command = new RefreshTokenCommand(refreshToken);

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

            // Update refresh token cookie with new token
            Response.Cookies.Append("refreshToken", tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // true in production (HTTPS)
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddSeconds(tokens.RefreshExpiresIn),
                Path = "/api/auth/refresh"
            });

            // Return access token only
            return Ok(new
            {
                tokens.AccessToken,
                tokens.ExpiresIn,
                tokens.TokenType,
                tokens.Scope
            });
        }
        catch (KeycloakAuthException ex)
        {
            // D-13 + SEC-08: generic error for invalid/expired refresh token
            _logger.LogWarning(ex, "Refresh token exchange failed");

            // Clear invalid cookie
            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth/refresh" });

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Invalid or expired refresh token."
            });
        }
    }

    /// <summary>POST /api/auth/logout — clear session and refresh token cookie.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        // Clear refresh token cookie
        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth/refresh" });
        return NoContent();
    }

    /// <summary>GET /api/auth/me — validate session and return user info.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(
        CancellationToken ct)
    {
        // Read refresh token from cookie to validate session
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "No session found."
            });
        }

        // Try to refresh tokens to validate the session
        try
        {
            var command = new RefreshTokenCommand(refreshToken);
            var tokens = await _refreshHandler.HandleAsync(command, ct);

            // Update cookie with new tokens
            Response.Cookies.Append("refreshToken", tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddSeconds(tokens.RefreshExpiresIn),
                Path = "/api/auth/refresh"
            });

            // Return user info (decoded from access token)
            return Ok(new
            {
                tokens.AccessToken,
                tokens.ExpiresIn,
                tokens.TokenType,
                tokens.Scope
            });
        }
        catch (KeycloakAuthException)
        {
            // Session invalid — clear cookie
            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/auth/refresh" });

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Session expired."
            });
        }
    }

    /// <summary>POST /api/auth/forgot-password — UX-05: initiate password reset flow.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        [FromServices] ICommandHandler<ForgotPasswordCommand, Unit> handler,
        [FromServices] IValidator<ForgotPasswordCommand> validator,
        CancellationToken ct)
    {
        var command = new ForgotPasswordCommand(request.Email ?? string.Empty);

        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        try
        {
            await handler.HandleAsync(command, ct);
            // Always returns 200 (no info disclosure)
            return Ok(new { message = "Se o email existir, voce recebera um link de recuperacao." });
        }
        catch (RateLimitExceededException ex)
        {
            return StatusCode(429, new ProblemDetails
            {
                Title = "Rate limit exceeded",
                Status = StatusCodes.Status429TooManyRequests,
                Detail = ex.Message
            });
        }
    }

    /// <summary>POST /api/auth/reset-password — UX-05: reset password with token.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        [FromServices] ICommandHandler<ResetPasswordCommand, Unit> handler,
        [FromServices] IValidator<ResetPasswordCommand> validator,
        CancellationToken ct)
    {
        var command = new ResetPasswordCommand(
            request.Token ?? string.Empty,
            request.NewPassword ?? string.Empty);

        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return UnprocessableEntity(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        try
        {
            await handler.HandleAsync(command, ct);
            return Ok(new { message = "Senha alterada com sucesso." });
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
    }
}

// HTTP request DTOs for AuthController — nullable to allow FluentValidation to handle missing fields
// (non-nullable would cause 400 from model binding before FluentValidation runs)
public sealed record LoginRequest(string? Email, string? Password);
public sealed record RefreshTokenRequest(string? RefreshToken);
public sealed record ForgotPasswordRequest(string? Email);
public sealed record ResetPasswordRequest(string? Token, string? NewPassword);
