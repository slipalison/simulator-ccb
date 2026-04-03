using Shouldly;

namespace Onboarding.API.Tests.HealthChecks;

[Trait("Category", "HealthCheck")]
public class HealthCheckEndpointTests
{
    // OBS-05: /healthz/live returns 200 without checking dependencies
    [Fact]
    public async Task LiveEndpoint_ShouldReturn200_WithoutCheckingDependencies()
    {
        await Task.CompletedTask;
        true.ShouldBeFalse("Production code not yet implemented — plan 04-02 wires health checks");
    }

    // OBS-05: /healthz/ready returns 200 when all checks pass
    [Fact]
    public async Task ReadyEndpoint_ShouldReturn200_WhenAllChecksPass()
    {
        await Task.CompletedTask;
        true.ShouldBeFalse("Production code not yet implemented — plan 04-02 wires health checks");
    }

    // OBS-05: /healthz/ready returns 503 when a dependency check fails
    [Fact]
    public async Task ReadyEndpoint_ShouldReturn503_WhenDependencyCheckFails()
    {
        await Task.CompletedTask;
        true.ShouldBeFalse("Production code not yet implemented — plan 04-02 wires health checks");
    }

    // OBS-05: /healthz/ready response is JSON with per-check details
    [Fact]
    public async Task ReadyEndpoint_ResponseBody_ShouldBeJsonWithCheckDetails()
    {
        await Task.CompletedTask;
        true.ShouldBeFalse("Production code not yet implemented — plan 04-02 wires health checks");
    }

    // D-26: Docker compose healthcheck uses /healthz/live (fast, no I/O)
    [Fact]
    public void LiveEndpoint_ShouldNotExecuteAnyHealthCheckPredicate()
    {
        true.ShouldBeFalse("Production code not yet implemented — plan 04-02 wires health checks");
    }
}
