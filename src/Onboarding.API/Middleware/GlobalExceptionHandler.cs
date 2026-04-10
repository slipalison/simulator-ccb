using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Onboarding.API.Middleware;

/// <summary>
/// Global exception handler — prevents stack trace exposure and returns standardized error responses.
/// Logs the full exception (including stack trace) via ILogger but returns a sanitized ProblemDetails to the client.
/// </summary>
public static class GlobalExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(appError =>
        {
            appError.Run(async context =>
            {
                context.Response.ContentType = "application/json";

                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (contextFeature == null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await WriteError(context, "An unexpected error occurred.");
                    return;
                }

                var exception = contextFeature.Error;
                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GlobalExceptionHandler");

                // Log the FULL exception (including stack trace) — only visible in server logs
                logger.LogError(exception, "Unhandled exception processing request {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                // Map known exception types to user-friendly responses
                context.Response.StatusCode = exception switch
                {
                    DbUpdateException => (int)HttpStatusCode.Conflict,
                    PostgresException pgEx when pgEx.SqlState == "23505" => (int)HttpStatusCode.Conflict, // unique violation
                    PostgresException pgEx when pgEx.SqlState == "23503" => (int)HttpStatusCode.BadRequest, // foreign key
                    KeyNotFoundException => (int)HttpStatusCode.NotFound,
                    UnauthorizedAccessException => (int)HttpStatusCode.Forbidden,
                    _ => (int)HttpStatusCode.InternalServerError,
                };

                var title = context.Response.StatusCode switch
                {
                    (int)HttpStatusCode.Conflict => "Resource conflict",
                    (int)HttpStatusCode.NotFound => "Resource not found",
                    (int)HttpStatusCode.Forbidden => "Access denied",
                    (int)HttpStatusCode.BadRequest => "Bad request",
                    _ => "Internal server error",
                };

                // NEVER expose stack trace to client
                var detail = context.Response.StatusCode == (int)HttpStatusCode.InternalServerError
                    ? "An unexpected error occurred. Please try again later."
                    : exception.Message;

                await WriteError(context, detail, title, context.Response.StatusCode);
            });
        });
    }

    private static async Task WriteError(HttpContext context, string detail, string? title = null, int? status = null)
    {
        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            title = title ?? "Error",
            status = status ?? context.Response.StatusCode,
            detail = detail,
            instance = $"{context.Request.Path}{context.Request.QueryString}"
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
