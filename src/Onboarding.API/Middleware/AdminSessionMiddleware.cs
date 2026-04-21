using Microsoft.AspNetCore.Http;

namespace Onboarding.API.Middleware;

/// <summary>
/// Admin session middleware — converts the httpOnly backoffice_access_token cookie
/// (JWT issued by Keycloak via Auth Code Flow + PKCE) into an Authorization: Bearer header
/// so [Authorize(Roles = "admin")] works on /api/admin/* endpoints.
///
/// The cookie is set by the Vinxi auth-server (frontend/backoffice/auth-server.ts) after
/// /auth/callback. Access token rotation is the Vinxi server's responsibility via /auth/refresh —
/// this middleware does NOT call Keycloak and does NOT manipulate response cookies.
///
/// Runs BEFORE UseAuthentication so JwtBearer sees the populated header.
/// </summary>
public static class AdminSessionMiddleware
{
    private const string AccessTokenCookieName = "backoffice_access_token";

    public static IApplicationBuilder UseAdminSession(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var path = context.Request.Path;

            if (!path.StartsWithSegments("/api/admin"))
            {
                await next();
                return;
            }

            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("AdminSessionMiddleware");

            if (!context.Request.Cookies.TryGetValue(AccessTokenCookieName, out var accessToken)
                || string.IsNullOrEmpty(accessToken))
            {
                logger.LogDebug("AdminSessionMiddleware: no {Cookie} cookie on {Path}",
                    AccessTokenCookieName, path);
                await next();
                return;
            }

            context.Request.Headers.Authorization = $"Bearer {accessToken}";
            logger.LogInformation("AdminSessionMiddleware: forwarded access token from cookie on {Path}",
                path);

            await next();
        });
    }
}
