using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Common;
using Onboarding.API.Configuration;
using Onboarding.Domain.Exceptions;
using Onboarding.Infrastructure.Keycloak;

namespace Onboarding.API.Controllers;

/// <summary>
/// Admin authentication endpoints — cookie-based session management for backoffice admin.
/// ADMIN-06: httpOnly cookies, no JWT in browser storage.
/// </summary>
[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController : ControllerBase
{
    private const string AdminCookieName = "adminRefreshToken";
    private const string AdminCookiePath = "/api/admin";

    private readonly ICommandHandler<AdminLoginCommand, AdminSessionResponse> _loginHandler;
    private readonly IKeycloakTokenService _tokenService;
    private readonly IValidator<AdminLoginCommand> _validator;
    private readonly IOptions<CookieSettings> _cookieSettings;
    private readonly ILogger<AdminAuthController> _logger;

    public AdminAuthController(
        ICommandHandler<AdminLoginCommand, AdminSessionResponse> loginHandler,
        IKeycloakTokenService tokenService,
        IValidator<AdminLoginCommand> validator,
        IOptions<CookieSettings> cookieSettings,
        ILogger<AdminAuthController> logger)
    {
        _loginHandler = loginHandler;
        _tokenService = tokenService;
        _validator = validator;
        _cookieSettings = cookieSettings;
        _logger = logger;
    }

    /// <summary>POST /api/admin/auth/login — Admin login via ROPC + role validation.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(
        [FromBody] AdminLoginRequest request,
        CancellationToken ct)
    {
        var command = new AdminLoginCommand(request.Email ?? string.Empty, request.Password ?? string.Empty);

        var validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return UnprocessableEntity(new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        try
        {
            var session = await _loginHandler.HandleAsync(command, ct);

            // Set refresh token as httpOnly cookie
            Response.Cookies.Append(AdminCookieName, session.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = _cookieSettings.Value.Secure,
                SameSite = SameSiteMode.Strict,
                Path = AdminCookiePath,
                Expires = DateTimeOffset.UtcNow.AddSeconds(session.RefreshExpiresIn),
            });

            // Return admin info only — NO tokens in response body
            return Ok(new
            {
                adminName = session.AdminName,
                adminEmail = session.AdminEmail,
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            // Non-admin user attempting login
            _logger.LogWarning("Non-admin user attempted login: {Email}", request.Email);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Access denied",
                Status = StatusCodes.Status403Forbidden,
                Detail = ex.Message
            });
        }
        catch (KeycloakAuthException ex)
        {
            _logger.LogWarning(ex, "Admin login attempt failed for email {Email}", request.Email);
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Invalid credentials."
            });
        }
    }

    /// <summary>POST /api/admin/auth/logout — Clear admin session cookie.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AdminCookieName, new CookieOptions { Path = AdminCookiePath });
        return NoContent();
    }

    /// <summary>GET /api/admin/auth/me — Validate session and return admin info.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(AdminCookieName, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "No session found."
            });
        }

        try
        {
            var tokens = await _tokenService.RefreshTokenAsync(refreshToken, ct);

            // Decode access token to extract admin info
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(tokens.AccessToken);

            var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty;
            var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                ?? jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                ?? email;

            // Update cookie with new refresh token
            Response.Cookies.Append(AdminCookieName, tokens.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = _cookieSettings.Value.Secure,
                SameSite = SameSiteMode.Strict,
                Path = AdminCookiePath,
                Expires = DateTimeOffset.UtcNow.AddSeconds(tokens.RefreshExpiresIn),
            });

            return Ok(new
            {
                adminName = name,
                adminEmail = email,
            });
        }
        catch (KeycloakAuthException)
        {
            // Session invalid — clear cookie
            Response.Cookies.Delete(AdminCookieName, new CookieOptions { Path = AdminCookiePath });

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Session expired."
            });
        }
    }
}

/// <summary>HTTP request body for admin login — nullable for FluentValidation handling.</summary>
public sealed record AdminLoginRequest(string? Email, string? Password);
