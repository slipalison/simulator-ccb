using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Onboarding.API.Tests.Registration;

/// <summary>
/// Test factory that configures the API for integration tests without real infrastructure.
/// Mirrors the approach from HealthCheckEndpointTests — provides fake connection strings
/// and removes real health check registrations so the TestServer can start.
/// </summary>
internal sealed class RegistrationTestApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:AppDb",
            "Host=localhost;Port=5432;Database=test;Username=test;Password=test");
        builder.UseSetting("Keycloak:RealmUrl",
            "http://localhost:8180/realms/onboarding");

        builder.ConfigureTestServices(services =>
        {
            // Remove real health checks so TestServer can start without real infrastructure
            var configureOptionsType = typeof(IConfigureOptions<HealthCheckServiceOptions>);
            var toRemove = services
                .Where(d => d.ServiceType == configureOptionsType)
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            services.AddHealthChecks()
                .AddCheck("stub-healthy", () => HealthCheckResult.Healthy("stub-ok"), ["ready"]);
        });
    }
}

/// <summary>
/// Integration stubs for RegistrationController (Phase 5).
/// All tests fail until Plan 02 (controller) and Plan 03 (infrastructure) are complete.
/// WebApplicationFactory spins up the real API with TestServer — no mocks at HTTP level.
/// </summary>
public class RegistrationControllerTests : IAsyncLifetime
{
    private RegistrationTestApiFactory? _factory;
    private HttpClient? _client;

    public Task InitializeAsync()
    {
        _factory = new RegistrationTestApiFactory();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
    }

    // REG-03: Valid PF payload → 201
    [Fact]
    public async Task PostPf_ValidCpf_Returns201()
    {
        // Stub — RegistrationController not yet implemented (Plan 02)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 02 (REG-03)");
        await Task.CompletedTask;
    }

    // REG-04: Valid PJ payload with CNPJ → 201
    [Fact]
    public async Task PostPj_ValidCnpj_Returns201()
    {
        // Stub — RegistrationController not yet implemented (Plan 02)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 02 (REG-04)");
        await Task.CompletedTask;
    }

    // REG-03: Invalid CPF check digit → 422
    [Fact]
    public async Task PostPf_InvalidCpfCheckDigit_Returns422()
    {
        // Stub — RegistrationController not yet implemented (Plan 02)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 02 (REG-03)");
        await Task.CompletedTask;
    }

    // REG-04: Invalid CNPJ check digit → 422
    [Fact]
    public async Task PostPj_InvalidCnpjCheckDigit_Returns422()
    {
        // Stub — RegistrationController not yet implemented (Plan 02)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 02 (REG-04)");
        await Task.CompletedTask;
    }

    // REG-05: Duplicate CPF → 409
    [Fact]
    public async Task PostPf_DuplicateCpf_Returns409()
    {
        // Stub — duplicate detection requires DB (Plan 03)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 03 (REG-05)");
        await Task.CompletedTask;
    }

    // REG-05: Duplicate email → 409
    [Fact]
    public async Task PostPf_DuplicateEmail_Returns409()
    {
        // Stub — duplicate detection requires DB (Plan 03)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 03 (REG-05)");
        await Task.CompletedTask;
    }

    // SEC-08: 409 response body must not mention which field caused conflict
    [Fact]
    public async Task PostPf_DuplicateCpf_ResponseBodyDoesNotLeakFieldName()
    {
        // Stub — SEC-08 verification (Plan 03)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 03 (SEC-08)");
        await Task.CompletedTask;
    }

    // SEC-08: 422 response body must not leak domain exception messages
    [Fact]
    public async Task PostPf_InvalidCpf_ResponseBodyIsGeneric()
    {
        // Stub — SEC-08: domain ex.Message must not appear in response (Plan 02)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 02 (SEC-08)");
        await Task.CompletedTask;
    }

    // BACK-05: Endpoint exists at POST /api/registration (not Minimal API)
    [Fact]
    public async Task PostRegistration_EndpointExists_NotMinimalApi()
    {
        // Stub — RegistrationController with [ApiController] attribute (Plan 02)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 02 (BACK-05)");
        await Task.CompletedTask;
    }

    // REG-08: Idempotency key → second call returns cached 201
    [Fact]
    public async Task PostPf_SameIdempotencyKey_SecondCallReturnsCached201()
    {
        // Stub — IdempotencyFilter (Plan 04)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 04 (REG-08)");
        await Task.CompletedTask;
    }

    // REG-08: Missing idempotency key → request proceeds normally
    [Fact]
    public async Task PostPf_NoIdempotencyKey_ProceedsNormally()
    {
        // Stub — IdempotencyFilter (Plan 04)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 04 (REG-08)");
        await Task.CompletedTask;
    }
}
