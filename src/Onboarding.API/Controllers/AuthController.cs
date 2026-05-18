using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Keycloak;

namespace Onboarding.API.Controllers;

/// <summary>
/// Authentication endpoints — public (no [Authorize]).
/// POST /api/auth/login — D-01: ROPC token exchange via IKeycloakTokenService
/// POST /api/auth/refresh — D-02: token refresh via IKeycloakTokenService
/// GET  /api/auth/me     — session validation + resolved permissions (frontend-client-fundos)
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
    private readonly ICompanyRepository _companyRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAccessGroupRepository _accessGroupRepository;

    // Handler reads JWT without signature validation — token was issued moments ago by Keycloak
    // via the refresh flow and is not user-supplied. This is intentional (no extra round-trip to JWKS).
    private static readonly JwtSecurityTokenHandler _jwtHandler = new();

    public AuthController(
        ICommandHandler<LoginCommand, TokenResponse> loginHandler,
        ICommandHandler<RefreshTokenCommand, TokenResponse> refreshHandler,
        IValidator<LoginCommand> loginValidator,
        IValidator<RefreshTokenCommand> refreshValidator,
        ILogger<AuthController> logger,
        ICompanyRepository companyRepository,
        IEmployeeRepository employeeRepository,
        IAccessGroupRepository accessGroupRepository)
    {
        _loginHandler = loginHandler;
        _refreshHandler = refreshHandler;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
        _logger = logger;
        _companyRepository = companyRepository;
        _employeeRepository = employeeRepository;
        _accessGroupRepository = accessGroupRepository;
    }

    /// <summary>
    /// Resolves the flat permissions list for a Keycloak subject by mirroring the
    /// ClientClaimsMiddleware lookup: Company owner → all permissions; Employee → AccessGroup permissions.
    /// Token is decoded without signature validation because it was just issued by Keycloak.
    /// </summary>
    private async Task<IReadOnlyList<string>> ResolvePermissionsFromAccessTokenAsync(
        string accessToken, CancellationToken ct)
    {
        if (!_jwtHandler.CanReadToken(accessToken))
            return Array.Empty<string>();

        var jwt = _jwtHandler.ReadJwtToken(accessToken);
        var sub = jwt.Subject;

        if (string.IsNullOrEmpty(sub))
            return Array.Empty<string>();

        // Mirror ClientClaimsMiddleware: Company owner → all permissions
        var company = await _companyRepository.GetByKeycloakSubAsync(sub, ct).ConfigureAwait(false);
        if (company != null)
            return Permissions.All;

        // Employee → AccessGroup permissions
        var employee = await _employeeRepository.GetByKeycloakSubAsync(sub, ct).ConfigureAwait(false);
        if (employee != null)
        {
            var accessGroup = await _accessGroupRepository.GetByIdAsync(employee.AccessGroupId, ct).ConfigureAwait(false);
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
    /// </remarks>
    [HttpGet("me")]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
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
                Path = "/api"
            });

            // Resolve permissions from the freshly issued access token (sub → DB lookup)
            var permissions = await ResolvePermissionsFromAccessTokenAsync(tokens.AccessToken, ct);

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
