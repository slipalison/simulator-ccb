using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Onboarding.API.Filters;

/// <summary>
/// Action filter that provides idempotency for POST endpoints (REG-08).
/// Usage: decorate a controller action with [Idempotent].
///
/// Behavior:
/// - Reads "Idempotency-Key" header (optional GUID string).
/// - If absent or not a valid GUID: request proceeds normally (key is optional).
/// - If present and found in cache: returns cached 201 response without re-executing the action.
/// - If present and not cached: executes the action; caches 2xx responses for 60 minutes.
/// - 4xx and 5xx responses are never cached (prevents caching transient errors).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    private const int CacheTtlMinutes = 60;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // No Idempotency-Key header → passthrough
        if (!context.HttpContext.Request.Headers.TryGetValue(
                "Idempotency-Key", out var keyValue))
        {
            await next();
            return;
        }

        // Non-GUID key → passthrough (REG-08: non-GUID keys are silently ignored)
        if (!Guid.TryParse(keyValue, out var idempotencyKey))
        {
            await next();
            return;
        }

        var cache = context.HttpContext.RequestServices
            .GetRequiredService<IDistributedCache>();

        var cacheKey = $"idem:{idempotencyKey}";
        var cached = await cache.GetStringAsync(cacheKey);

        if (cached is not null)
        {
            // Cache hit — return stored response without executing handler (REG-08)
            var stored = JsonSerializer.Deserialize<IdempotentResponse>(cached)!;
            context.Result = new ObjectResult(stored.Value) { StatusCode = stored.StatusCode };
            return;
        }

        // Cache miss — execute the action
        var executed = await next();

        // Cache only 2xx responses (4xx/5xx must not be cached — transient errors or validation failures)
        if (executed.Result is ObjectResult { StatusCode: >= 200 and < 300 } result)
        {
            var response = new IdempotentResponse(result.StatusCode ?? 200, result.Value);
            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheTtlMinutes)
                });
        }
    }
}

/// <summary>
/// Serialization record for cached idempotent responses.
/// </summary>
internal sealed record IdempotentResponse(int StatusCode, object? Value);
