using Microsoft.AspNetCore.Http;

namespace Onboarding.API.Middleware;

/// <summary>
/// Client session middleware — converts the httpOnly client_access_token cookie
/// (JWT issued by Keycloak via Auth Code Flow + PKCE) into an Authorization: Bearer header
/// so [Authorize] works on /api/clients/* endpoints.
///
/// The cookie is set by the Vinxi auth-server (frontend/client/auth-server.ts) after
/// /auth/callback. This middleware does NOT call Keycloak and does NOT manipulate response cookies.
///
/// Runs BEFORE UseAuthentication so JwtBearer sees the populated header.
///
/// Note: Only fires on non-admin routes that don't already have an Authorization header
/// (AdminSessionMiddleware handles /api/admin/* independently).
/// </summary>
public static class ClientSessionMiddleware
{
    private const string AccessTokenCookieName = "client_access_token";

    public static IApplicationBuilder UseClientSession(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var path = context.Request.Path;

            // Skip admin routes — those use backoffice_access_token via AdminSessionMiddleware
            if (path.StartsWithSegments("/api/admin"))
            {
                await next();
                return;
            }

            // Skip if Authorization header is already present (e.g. from a direct Bearer token)
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                await next();
                return;
            }

            // Only inject token for API routes that need authentication
            if (!path.StartsWithSegments("/api"))
            {
                await next();
                return;
            }

            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("ClientSessionMiddleware");

            if (!context.Request.Cookies.TryGetValue(AccessTokenCookieName, out var accessToken)
                || string.IsNullOrEmpty(accessToken))
            {
                logger.LogDebug("ClientSessionMiddleware: no {Cookie} cookie on {Path}",
                    AccessTokenCookieName, path);
                await next();
                return;
            }

            context.Request.Headers.Authorization = $"Bearer {accessToken}";
            logger.LogInformation("ClientSessionMiddleware: forwarded access token from cookie on {Path}",
                path);

            await next();
        });
    }
}
