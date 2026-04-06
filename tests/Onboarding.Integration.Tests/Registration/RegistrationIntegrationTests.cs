using Shouldly;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;

namespace Onboarding.Integration.Tests.Registration;

/// <summary>
/// End-to-end integration stubs using real Keycloak + PostgreSQL containers.
/// These tests require Docker and are slow (~90s). Run with: dotnet test tests/Onboarding.Integration.Tests/
///
/// All stubs fail until Plan 04 (Keycloak integration) is complete.
/// </summary>
[Trait("Category", "Integration")]
public class RegistrationIntegrationTests : IAsyncLifetime
{
    private readonly KeycloakContainer _keycloak = new KeycloakBuilder()
        .WithImage("quay.io/keycloak/keycloak:26.1")
        .Build();

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        // Stub — containers not started until Plan 04 wires the real test
        // Skipping container startup here to keep Wave 0 fast
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _keycloak.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // REG-06: POST valid PF to real API → user appears in Keycloak realm
    [Fact]
    public async Task PostPf_ValidPayload_CreatesUserInKeycloak()
    {
        // Stub — full Keycloak integration (Plan 04)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 04 (REG-06 Testcontainers end-to-end)");
        await Task.CompletedTask;
    }

    // REG-06: Keycloak failure → no record in app_db (compensation)
    [Fact]
    public async Task PostPf_KeycloakDown_NoOrphanedRowInAppDb()
    {
        // Stub — compensation path end-to-end (Plan 04)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 04 (REG-06 compensation end-to-end)");
        await Task.CompletedTask;
    }
}
