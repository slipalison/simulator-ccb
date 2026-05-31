using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Onboarding.API.Extensions;
using Onboarding.API.Security;
using Onboarding.Application.Auth.Commands;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Keycloak;

namespace Onboarding.API.Controllers;

/// <summary>
/// Authentication endpoints — public (no [Authorize]).
/// POST /api/auth/login — D-01: ROPC token exchange via IKeycloakTokenService
/// POST /api/auth/refresh — D-02: token refresh via IKeycloakTokenService
/// GET  /api/auth/me     — session validation + resolved permissions (frontend-client-fundos)
///
/// D-62: 3 ctor deps (was 8): ICommandDispatcher + IValidationRunner + ILogger.
/// Repos used only in GetMe (permission resolution) moved to [FromServices].
/// ForgotPassword and ResetPassword already used [FromServices] (unchanged).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IValidationRunner _validation;
    private readonly ILogger<AuthController> _logger;

    // Handler reads JWT without signature validation — token was issued moments ago by Keycloak
    // via the refresh flow and is not user-supplied. This is intentional (no extra round-trip to JWKS).
    private static readonly JwtSecurityTokenHandler _jwtHandler = new();

    public AuthController(
        ICommandDispatcher commands,
        IValidationRunner validation,
        ILogger<AuthController> logger)
    {
        _commands = commands;
        _validation = validation;
        _logger = logger;
    }

    /// <summary>
    /// Resolves the flat permissions list for a Keycloak subject by mirroring the
    /// ClientClaimsMiddleware lookup: Company owner → all permissions; Employee → AccessGroup permissions.
    /// Token is decoded without signature validation because it was just issued by Keycloak.
    /// </summary>
    private static async Task<IReadOnlyList<string>> ResolvePermissionsFromAccessTokenAsync(
        string accessToken,
        ICompanyRepository companyRepository,
        IEmployeeRepository employeeRepository,
        IAccessGroupRepository accessGroupRepository,
        CancellationToken ct)
    {
        if (!_jwtHandler.CanReadToken(accessToken))
            return Array.Empty<string>();

        var jwt = _jwtHandler.ReadJwtToken(accessToken);
        var sub = jwt.Subject;

        if (string.IsNullOrEmpty(sub))
            return Array.Empty<string>();

        // Mirror ClientClaimsMiddleware: Company owner → all permissions
        var company = await companyRepository.GetByKeycloakSubAsync(sub, ct).ConfigureAwait(false);
        if (company != null)
            return Permissions.All;

        // Employee → AccessGroup permissions
        var employee = await employeeRepository.GetByKeycloakSubAsync(sub, ct).ConfigureAwait(false);
        if (employee != null)
        {
            var accessGroup = await accessGroupRepository.GetByIdAsync(employee.AccessGroupId, ct).ConfigureAwait(false);
            return accessGroup?.Permissions.ToArray() ?? Array.Empty<string>();
        }

        return Array.Empty<string>();
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

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(validation.ToValidationProblem());

        try
        {
            var tokens = await _commands.Send<TokenResponse>(command, ct);

            // Set refresh token as httpOnly cookie (secure against XSS)
            Response.Cookies.Append("refreshToken", tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // true in production (HTTPS)
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddSeconds(tokens.RefreshExpiresIn),
                Path = "/api" // Available to all /api endpoints
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
            _logger.LogWarning(ex, "Login attempt failed for email {Email}",
                Observability.SensitiveDataDestructuringPolicy.MaskEmail(request.Email ?? string.Empty));
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

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(validation.ToValidationProblem());

        try
        {
            var tokens = await _commands.Send<TokenResponse>(command, ct);

            // Update refresh token cookie with new token
            Response.Cookies.Append("refreshToken", tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // true in production (HTTPS)
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddSeconds(tokens.RefreshExpiresIn),
                Path = "/api"
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
            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api" });

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
        Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api" });
        return NoContent();
    }

    /// <summary>GET /api/auth/me — validate session, return user info + resolved permissions.</summary>
    /// <remarks>
    /// Explicit allow-anonymous: this endpoint validates the session via the httpOnly
    /// refresh token cookie — there is no Authorization header to authenticate against.
    /// Permissions are resolved from the freshly issued access token (sub → DB lookup),
    /// mirroring the ClientClaimsMiddleware logic. Frontend uses the flat permissions[]
    /// array to gate menu items such as the Fundos NavGroup (frontend-client-fundos, iter 6).
    /// Repos injected via [FromServices] — only used in this endpoint (SOLID-04 deferred D-63).
    /// </remarks>
    [HttpGet("me")]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(
        [FromServices] ICompanyRepository companyRepository,
        [FromServices] IEmployeeRepository employeeRepository,
        [FromServices] IAccessGroupRepository accessGroupRepository,
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
            var tokens = await _commands.Send<TokenResponse>(command, ct);

            // Update cookie with new tokens
            Response.Cookies.Append("refreshToken", tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddSeconds(tokens.RefreshExpiresIn),
                Path = "/api"
            });

            // Resolve permissions from the freshly issued access token (sub → DB lookup)
            var permissions = await ResolvePermissionsFromAccessTokenAsync(
                tokens.AccessToken, companyRepository, employeeRepository, accessGroupRepository, ct);

            return Ok(new MeResponse(
                tokens.AccessToken,
                tokens.ExpiresIn,
                tokens.TokenType,
                tokens.Scope,
                permissions));
        }
        catch (KeycloakAuthException)
        {
            // Session invalid — clear cookie
            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api" });

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
        CancellationToken ct)
    {
        var command = new ForgotPasswordCommand(request.Email ?? string.Empty);

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return BadRequest(validation.ToValidationProblem());

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
        CancellationToken ct)
    {
        var command = new ResetPasswordCommand(
            request.Token ?? string.Empty,
            request.NewPassword ?? string.Empty);

        var validation = await _validation.Validate(command, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(validation.ToValidationProblem());

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
