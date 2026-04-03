using Shouldly;

namespace Onboarding.API.Tests.Observability;

[Trait("Category", "Observability")]
public class TracePropagationTests
{
    // OBS-04: outbound HttpClient calls carry W3C traceparent header
    [Fact]
    public async Task HttpClient_ShouldPropagateW3CTraceparent_OnOutboundCalls()
    {
        await Task.CompletedTask;
        true.ShouldBeFalse("Production code not yet implemented — plan 04-01 wires AddHttpClientInstrumentation");
    }

    // OBS-04: TraceId in log entry matches the active trace
    [Fact]
    public async Task LogEntry_TraceId_ShouldMatchActiveOtelTrace()
    {
        await Task.CompletedTask;
        true.ShouldBeFalse("Production code not yet implemented — plan 04-01 wires Serilog.Enrichers.Span");
    }
}
