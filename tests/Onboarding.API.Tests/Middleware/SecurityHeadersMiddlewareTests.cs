using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.API.Middleware;
using Shouldly;

namespace Onboarding.API.Tests.Middleware;

/// <summary>
/// Unit tests for SecurityHeadersMiddleware.
///
/// Two classes:
/// 1. SecurityHeadersMiddlewareBlockingTests — tests the Sec-Fetch blocking paths
///    (cross-origin 403, same-site+document 403). These paths call Response.WriteAsync
///    directly (before next), so OnStarting callbacks are triggered within the pipeline.
///
/// 2. SecurityHeadersMiddlewareHeaderTests — tests the security response headers
///    set via Response.OnStarting. Uses a custom IHttpResponseFeature shim that fires
///    callbacks when StartAsync is called explicitly, allowing DefaultHttpContext to be
///    used without a full Kestrel server.
/// </summary>

// ── Custom HttpResponseFeature that fires OnStarting callbacks on StartAsync ───────

internal sealed class FireableResponseFeature : IHttpResponseFeature
{
    private readonly List<(Func<object, Task> Callback, object State)> _onStarting = new();

    public int StatusCode { get; set; } = 200;
    public string? ReasonPhrase { get; set; }
    public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
    public Stream Body { get; set; } = Stream.Null;
    public bool HasStarted { get; private set; }

    public void OnStarting(Func<object, Task> callback, object state)
        => _onStarting.Add((callback, state));

    public void OnCompleted(Func<object, Task> callback, object state) { }

    public async Task FireOnStartingAsync()
    {
        foreach (var (cb, state) in _onStarting)
            await cb(state).ConfigureAwait(false);
        HasStarted = true;
    }
}

// ── Blocking path tests (Sec-Fetch validation) ────────────────────────────────────

/// <summary>
/// Tests for the Sec-Fetch-* validation blocking paths in SecurityHeadersMiddleware.
/// These paths write directly to the response (not via OnStarting), so they work
/// with DefaultHttpContext without a full HTTP server.
/// </summary>
public class SecurityHeadersMiddlewareBlockingTests
{
    private static IServiceProvider BuildServices(string[]? allowedOrigins = null)
    {
        var configData = new Dictionary<string, string?>();
        if (allowedOrigins is { Length: > 0 })
        {
            for (var i = 0; i < allowedOrigins.Length; i++)
                configData[$"SecurityHeaders:AllowedOrigins:{i}"] = allowedOrigins[i];
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateContext(
        string path,
        string? secFetchSite = null,
        string? secFetchDest = null,
        string? secFetchMode = null,
        string? secFetchUser = null,
        string? origin = null,
        IServiceProvider? services = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Request.Method = "GET";
        ctx.RequestServices = services ?? BuildServices();

        var bodyStream = new MemoryStream();
        ctx.Response.Body = bodyStream;

        if (secFetchSite is not null) ctx.Request.Headers["Sec-Fetch-Site"] = secFetchSite;
        if (secFetchDest is not null) ctx.Request.Headers["Sec-Fetch-Dest"] = secFetchDest;
        if (secFetchMode is not null) ctx.Request.Headers["Sec-Fetch-Mode"] = secFetchMode;
        if (secFetchUser is not null) ctx.Request.Headers["Sec-Fetch-User"] = secFetchUser;
        if (origin is not null) ctx.Request.Headers["Origin"] = origin;

        return ctx;
    }

    private static async Task RunAsync(DefaultHttpContext ctx)
    {
        var appBuilder = new ApplicationBuilder(ctx.RequestServices);
        appBuilder.UseSecurityHeaders();
        appBuilder.Run(async c => await c.Response.WriteAsync("ok"));
        await appBuilder.Build()(ctx);
    }

    // ── cross-origin → 403 ────────────────────────────────────────────────────────

    [Fact]
    public async Task CrossOriginRequest_FromDisallowedOrigin_Returns403()
    {
        var services = BuildServices(new[] { "https://allowed.example.com" });
        var ctx = CreateContext(
            "/api/fundos",
            secFetchSite: "cross-origin",
            origin: "https://evil.attacker.com",
            services: services);

        await RunAsync(ctx);

        ctx.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task CrossOriginRequest_FromAllowedOrigin_Passthrough()
    {
        var services = BuildServices(new[] { "https://frontend.example.com" });
        var ctx = CreateContext(
            "/api/fundos",
            secFetchSite: "cross-origin",
            origin: "https://frontend.example.com",
            services: services);

        await RunAsync(ctx);

        ctx.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CrossOriginRequest_FromAllowedOrigin_CaseInsensitive()
    {
        var services = BuildServices(new[] { "https://frontend.example.com" });
        var ctx = CreateContext(
            "/api/fundos",
            secFetchSite: "cross-origin",
            origin: "HTTPS://FRONTEND.EXAMPLE.COM",
            services: services);

        await RunAsync(ctx);

        ctx.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task CrossOriginRequest_EmptyAllowedOrigins_Returns403()
    {
        var services = BuildServices(allowedOrigins: null);
        var ctx = CreateContext(
            "/api/fundos",
            secFetchSite: "cross-origin",
            origin: "https://any.origin.com",
            services: services);

        await RunAsync(ctx);

        ctx.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task CrossOriginRequest_Returns403_WithJsonBody()
    {
        var services = BuildServices(new[] { "https://allowed.com" });
        var ctx = CreateContext(
            "/api/test",
            secFetchSite: "cross-origin",
            origin: "https://evil.com",
            services: services);

        await RunAsync(ctx);

        ctx.Response.Body.Position = 0;
        var body = new StreamReader(ctx.Response.Body).ReadToEnd();
        body.ShouldContain("Forbidden");
        ctx.Response.ContentType!.ShouldContain("application/json");
    }

    // ── same-site + document → 403 ────────────────────────────────────────────────

    [Fact]
    public async Task SameSite_Document_Returns403()
    {
        var ctx = CreateContext(
            "/api/fundos",
            secFetchSite: "same-site",
            secFetchDest: "document");

        await RunAsync(ctx);

        ctx.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task SameSite_Document_Returns403_WithJsonBody()
    {
        var ctx = CreateContext(
            "/api/test",
            secFetchSite: "same-site",
            secFetchDest: "document");

        await RunAsync(ctx);

        ctx.Response.Body.Position = 0;
        var body = new StreamReader(ctx.Response.Body).ReadToEnd();
        body.ShouldContain("Forbidden");
        ctx.Response.ContentType!.ShouldContain("application/json");
    }

    [Fact]
    public async Task SameSite_NonDocumentDest_Passthrough()
    {
        var ctx = CreateContext(
            "/api/fundos",
            secFetchSite: "same-site",
            secFetchDest: "fetch");

        await RunAsync(ctx);

        ctx.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task SameOrigin_AnyDest_Passthrough()
    {
        // same-origin is always safe — code only blocks "cross-origin" and "same-site"+"document"
        var ctx = CreateContext(
            "/api/fundos",
            secFetchSite: "same-origin",
            secFetchDest: "document");

        await RunAsync(ctx);

        ctx.Response.StatusCode.ShouldBe(200);
    }

    // ── No Sec-Fetch headers → passthrough ────────────────────────────────────────

    [Fact]
    public async Task NoSecFetchHeaders_ApiPath_Passthrough()
    {
        var ctx = CreateContext("/api/fundos");

        await RunAsync(ctx);

        ctx.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task NoSecFetchHeaders_NonApiPath_Passthrough()
    {
        var ctx = CreateContext("/healthz/live");

        await RunAsync(ctx);

        ctx.Response.StatusCode.ShouldBe(200);
    }

    // ── Non-API paths skip Sec-Fetch validation entirely ─────────────────────────

    [Fact]
    public async Task NonApiPath_CrossOriginRequest_Passthrough_NoSecFetchValidation()
    {
        var ctx = CreateContext(
            "/robots.txt",
            secFetchSite: "cross-origin",
            origin: "https://attacker.com");

        await RunAsync(ctx);

        ctx.Response.StatusCode.ShouldBe(200);
    }

    // ── hasAnySecFetch: one header is enough to trigger validation ─────────────────

    [Fact]
    public async Task OnlySecFetchModePresent_TriggersValidation_NotBlocked_WhenSafeValues()
    {
        // Sec-Fetch-Mode only (navigate), no Sec-Fetch-Site → site="" → not "cross-origin" → passes
        var ctx = CreateContext(
            "/api/fundos",
            secFetchMode: "navigate");

        await RunAsync(ctx);

        ctx.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task OnlySecFetchUserPresent_TriggersValidation_NotBlocked()
    {
        var ctx = CreateContext(
            "/api/fundos",
            secFetchUser: "?1");

        await RunAsync(ctx);

        ctx.Response.StatusCode.ShouldBe(200);
    }
}

// ── Security response header tests (OnStarting callback path) ─────────────────────

/// <summary>
/// Tests for security response headers set via Response.OnStarting.
/// Uses a custom FireableResponseFeature to fire the callbacks explicitly after the
/// pipeline runs, simulating what Kestrel does when the response starts.
/// </summary>
public class SecurityHeadersMiddlewareHeaderTests
{
    private static IServiceProvider BuildServices()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static async Task<DefaultHttpContext> RunWithHeaderCapture(string path)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Request.Method = "GET";
        ctx.RequestServices = BuildServices();

        // Install a custom feature that captures OnStarting callbacks and fires them
        var responseFeature = new FireableResponseFeature();
        ctx.Features.Set<IHttpResponseFeature>(responseFeature);

        var appBuilder = new ApplicationBuilder(ctx.RequestServices);
        appBuilder.UseSecurityHeaders();
        appBuilder.Run(_ => Task.CompletedTask);
        await appBuilder.Build()(ctx);

        // Fire the OnStarting callbacks (simulates Kestrel flushing response headers)
        await responseFeature.FireOnStartingAsync();

        // Merge feature headers into DefaultHttpContext.Response.Headers for assertion
        foreach (var (key, value) in responseFeature.Headers)
            ctx.Response.Headers[key] = value;

        return ctx;
    }

    [Fact]
    public async Task ApiPath_SetsCacheControlNoStore()
    {
        var ctx = await RunWithHeaderCapture("/api/companies/me");

        ctx.Response.Headers.CacheControl.ToString().ShouldContain("no-store");
    }

    [Fact]
    public async Task ApiPath_SetsPragmaNoCache()
    {
        var ctx = await RunWithHeaderCapture("/api/companies/me");

        ctx.Response.Headers.Pragma.ToString().ShouldBe("no-cache");
    }

    [Fact]
    public async Task ApiPath_SetsExpiresZero()
    {
        var ctx = await RunWithHeaderCapture("/api/companies/me");

        ctx.Response.Headers.Expires.ToString().ShouldBe("0");
    }

    [Fact]
    public async Task ApiPath_SetsXContentTypeOptions_NoSniff()
    {
        var ctx = await RunWithHeaderCapture("/api/companies/me");

        ctx.Response.Headers.XContentTypeOptions.ToString().ShouldBe("nosniff");
    }

    [Fact]
    public async Task ApiPath_SetsXFrameOptions_Deny()
    {
        var ctx = await RunWithHeaderCapture("/api/companies/me");

        ctx.Response.Headers.XFrameOptions.ToString().ShouldBe("DENY");
    }

    [Fact]
    public async Task ApiPath_SetsReferrerPolicy()
    {
        var ctx = await RunWithHeaderCapture("/api/companies/me");

        ctx.Response.Headers["Referrer-Policy"].ToString()
            .ShouldBe("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task NonApiPath_AlsoSetsCacheControlNoStore()
    {
        // Cache-Control applies to ALL paths, not just /api/*
        var ctx = await RunWithHeaderCapture("/healthz/live");

        ctx.Response.Headers.CacheControl.ToString().ShouldContain("no-store");
    }

    [Fact]
    public async Task XContentTypeOptions_NotOverwritten_WhenAlreadySet()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/api/test";
        ctx.RequestServices = BuildServices();

        var responseFeature = new FireableResponseFeature();
        // Pre-set the header in the feature to simulate upstream middleware having set it
        responseFeature.Headers["X-Content-Type-Options"] = "upstream-value";
        ctx.Features.Set<IHttpResponseFeature>(responseFeature);

        var appBuilder = new ApplicationBuilder(ctx.RequestServices);
        appBuilder.UseSecurityHeaders();
        appBuilder.Run(_ => Task.CompletedTask);
        await appBuilder.Build()(ctx);
        await responseFeature.FireOnStartingAsync();

        // The middleware checks ContainsKey before writing: upstream value must be preserved
        // (Note: ctx.Response.Headers and responseFeature.Headers are different dictionaries
        //  because DefaultHttpContext has its own IHttpResponseFeature. We check the feature.)
        responseFeature.Headers["X-Content-Type-Options"].ToString().ShouldBe("upstream-value");
    }
}
