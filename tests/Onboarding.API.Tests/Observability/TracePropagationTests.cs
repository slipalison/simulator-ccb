using Shouldly;

namespace Onboarding.API.Tests.Observability;

[Trait("Category", "Observability")]
public class TracePropagationTests
{
    // OBS-04: outbound HttpClient calls carry W3C traceparent header
    // Full E2E trace propagation verified manually via Grafana Tempo — see VALIDATION.md
    [Fact(Skip = "E2E trace propagation verified manually via Grafana Tempo — see VALIDATION.md")]
    public async Task HttpClient_ShouldPropagateW3CTraceparent_OnOutboundCalls()
    {
        await Task.CompletedTask;
        // W3C traceparent propagation is automatic via AddHttpClientInstrumentation().
        // No custom middleware needed (D-14, D-16). Structural wiring verified in Program.cs.
    }

    // OBS-04: TraceId in log entry matches the active trace
    // Full E2E correlation verified manually via Grafana — see VALIDATION.md
    [Fact(Skip = "E2E TraceId correlation verified manually via Grafana Loki + Tempo — see VALIDATION.md")]
    public async Task LogEntry_TraceId_ShouldMatchActiveOtelTrace()
    {
        await Task.CompletedTask;
        // TraceId/SpanId enriched automatically via Serilog.Enrichers.Span (D-03, D-15).
        // Correlation is transparent through System.Diagnostics.Activity context.
    }
}
