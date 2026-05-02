using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Onboarding.API.Middleware;

/// <summary>
/// Adds security response headers and validates Fetch Metadata request headers (Sec-Fetch-*).
///
/// Resolves ZAP alerts:
///   - 90005: Sec-Fetch-Dest/Mode/Site/User headers missing → we now validate them
///   - 10049: Storable and Cacheable Content → we now send Cache-Control: no-store
/// </summary>
public static class SecurityHeadersMiddleware
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value;

            // Skip non-API paths (health checks, robots.txt, sitemap.xml, etc.)
            var isApiPath = path != null && path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);

            if (isApiPath)
            {
                // ── Sec-Fetch-* validation (ZAP 90005) ──────────────────────────
                // Browsers send Sec-Fetch-* headers on all navigation and fetch requests.
                // Non-browser clients (curl, Postman, service-to-service) don't send them,
                // so we can't reject missing headers outright — but we CAN reject requests
                // that carry Sec-Fetch headers with values that indicate CSRF attacks.
                //
                // Strategy: if Sec-Fetch headers are present, validate them. If absent, allow
                // (service-to-service calls from the backend or Postman won't have them).
                var hasAnySecFetch = context.Request.Headers.ContainsKey("Sec-Fetch-Dest")
                    || context.Request.Headers.ContainsKey("Sec-Fetch-Mode")
                    || context.Request.Headers.ContainsKey("Sec-Fetch-Site")
                    || context.Request.Headers.ContainsKey("Sec-Fetch-User");

                if (hasAnySecFetch)
                {
                    var site = context.Request.Headers["Sec-Fetch-Site"].ToString();
                    var dest = context.Request.Headers["Sec-Fetch-Dest"].ToString();

                    // Load allowed origins from configuration (SecurityHeaders:AllowedOrigins)
                    var config = context.RequestServices.GetRequiredService<IConfiguration>();
                    var allowedOrigins = config.GetSection("SecurityHeaders:AllowedOrigins")
                        .Get<string[]>() ?? [];

                    // Block cross-origin requests that don't come from our allowed frontend origins.
                    // Our CORS policy already handles preflight, but Sec-Fetch-Site: cross-origin
                    // appears on simple requests too. We must allow our own frontends through.
                    if (site == "cross-origin")
                    {
                        var origin = context.Request.Headers["Origin"].ToString();
                        if (!allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync(
                                """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.4","title":"Forbidden","status":403,"detail":"Cross-origin requests are not permitted."}"""
                            );
                            return;
                        }
                    }

                    // Block same-site embed navigations (iframe, object) to API endpoints
                    if (site == "same-site" && dest == "document")
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(
                            """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.4","title":"Forbidden","status":403,"detail":"Embedded cross-site navigation is not permitted."}"""
                        );
                        return;
                    }
                }
            }

            await next(context);

            // ── Response headers (applied to ALL responses) ─────────────────────
            // Cache-Control: no-store — prevents sensitive API responses from being cached (ZAP 10049)
            // Applies to all paths including /healthz, /robots.txt, /sitemap.xml
            context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers.Expires = "0";

            // X-Content-Type-Options: prevents MIME-sniffing
            if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
            {
                context.Response.Headers.XContentTypeOptions = "nosniff";
            }

            // X-Frame-Options: prevents clickjacking via iframe
            if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
            {
                context.Response.Headers.XFrameOptions = "DENY";
            }

            // Referrer-Policy: limit referrer info
            if (!context.Response.Headers.ContainsKey("Referrer-Policy"))
            {
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            }
        });
    }
}