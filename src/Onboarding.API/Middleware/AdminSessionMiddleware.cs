using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Onboarding.Application.Common;
using Onboarding.Domain.Exceptions;
using Onboarding.Infrastructure.Keycloak;

namespace Onboarding.API.Middleware;

/// <summary>
/// Admin session middleware — converts the httpOnly refresh token cookie into a JWT access token
/// so that [Authorize(Roles = "admin")] works on admin endpoints.
///
/// Flow:
/// 1. Reads the adminRefreshToken cookie from the request
/// 2. Exchanges it for a new access + refresh token pair via Keycloak
/// 3. Sets the Authorization header with the new access token (JWT Bearer)
/// 4. Updates the refresh token cookie with the new value
///
/// This runs BEFORE UseAuthentication so JWT Bearer auth sees a valid token.
/// </summary>
public static class AdminSessionMiddleware
{
    private const string AdminCookieName = "adminRefreshToken";

    public static IApplicationBuilder UseAdminSession(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("AdminSessionMiddleware");

            // Skip auth endpoints — they handle cookies themselves
            if (path.StartsWithSegments("/api/admin/auth"))
            {
                await next();
                return;
            }

            // Only process admin API paths
            if (!path.StartsWithSegments("/api/admin"))
            {
                logger.LogDebug("AdminSessionMiddleware: skipped (not admin path): {Path}", path);
                await next();
                return;
            }

            // Read refresh token from cookie
            if (!context.Request.Cookies.TryGetValue(AdminCookieName, out var refreshToken) ||
                string.IsNullOrEmpty(refreshToken))
            {
                logger.LogWarning("AdminSessionMiddleware: no cookie found. Cookie header: {CookieHeader}",
                    context.Request.Headers.Cookie.ToString());
                // No cookie — let downstream auth handle the 401
                await next();
                return;
            }

            logger.LogInformation("AdminSessionMiddleware: found refresh token, exchanging...");

            // Exchange refresh token for access + refresh tokens
            var tokenService = context.RequestServices.GetRequiredService<IKeycloakTokenService>();

            try
            {
                var tokens = await tokenService.RefreshTokenAsync(refreshToken, context.RequestAborted);
                logger.LogInformation("AdminSessionMiddleware: exchanged refresh token for access token. Scope: {Scope}", tokens.Scope);

                // Decode access token to verify realm_access.roles is present
                try
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(tokens.AccessToken);
                    var realmAccess = jwt.Payload.ContainsKey("realm_access")
                        ? jwt.Payload["realm_access"]?.ToString()
                        : "MISSING";
                    logger.LogInformation("AdminSessionMiddleware: realm_access in access token: {RealmAccess}", realmAccess);
                }
                catch { /* ignore decode errors in middleware */ }

                // Set Authorization header with the new access token so JWT Bearer auth works
                context.Request.Headers.Authorization = $"Bearer {tokens.AccessToken}";
                logger.LogInformation("AdminSessionMiddleware: set Authorization header");

                // Update the refresh token cookie with the new value
                // Note: CookieSettings.Secure is read at login time; here we use default dev settings
                context.Response.OnStarting(() =>
                {
                    context.Response.Cookies.Append(AdminCookieName, tokens.RefreshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false, // Development — set true in production via config
                        SameSite = SameSiteMode.Strict,
                        Path = "/api/admin",
                        Expires = DateTimeOffset.UtcNow.AddSeconds(tokens.RefreshExpiresIn),
                    });
                    return Task.CompletedTask;
                });
            }
            catch (KeycloakAuthException ex)
            {
                // Token expired/invalid — clear cookie and let downstream return 401
                logger.LogWarning(ex, "AdminSessionMiddleware: Keycloak auth exception");
                context.Response.Cookies.Delete(AdminCookieName, new CookieOptions { Path = "/api/admin" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AdminSessionMiddleware: UNEXPECTED exception");
            }

            await next();
        });
    }
}
