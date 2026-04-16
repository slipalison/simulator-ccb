using Microsoft.AspNetCore.Http;

namespace Onboarding.API.Middleware;

/// <summary>
/// Client session middleware — reads the client_access_token httpOnly cookie
/// and sets the Authorization: Bearer header so downstream [Authorize] works.
/// This enables ACF-authenticated clients to access API endpoints without
/// manually managing tokens in JavaScript.
/// </summary>
public sealed class ClientSessionMiddleware
{
    private const string ClientCookieName = "client_access_token";
    private readonly RequestDelegate _next;
    private readonly ILogger<ClientSessionMiddleware> _logger;

    public ClientSessionMiddleware(RequestDelegate next, ILogger<ClientSessionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth endpoints — they handle auth themselves
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Read access token from cookie
        if (context.Request.Cookies.TryGetValue(ClientCookieName, out var accessToken) &&
            !string.IsNullOrEmpty(accessToken))
        {
            // Set Authorization header so downstream [Authorize] works
            context.Request.Headers.Authorization = $"Bearer {accessToken}";
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method for adding ClientSessionMiddleware to the pipeline.
/// </summary>
public static class ClientSessionMiddlewareExtensions
{
    public static IApplicationBuilder UseClientSession(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ClientSessionMiddleware>();
    }
}
