using Shouldly;

namespace Onboarding.API.Tests.Registration;

/// <summary>
/// Unit/integration stubs for IdempotencyFilter (Plan 04).
/// Tests the filter behavior independently of the full registration flow.
/// </summary>
public class IdempotencyFilterTests
{
    // REG-08: Filter only caches 2xx responses — a 422 must NOT be cached
    [Fact]
    public void Filter_422Response_IsNotCached()
    {
        // Stub — IdempotencyFilter not yet implemented (Plan 04)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 04 (REG-08 — no 4xx caching)");
    }

    // REG-08: Filter returns cached 201 body on second call with same key
    [Fact]
    public void Filter_SameKey_ReturnsCachedResponse()
    {
        // Stub — IdempotencyFilter not yet implemented (Plan 04)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 04 (REG-08 — cache hit)");
    }

    // REG-08: Filter parses Idempotency-Key as GUID; non-GUID header is ignored
    [Fact]
    public void Filter_NonGuidKey_PassesThrough()
    {
        // Stub — IdempotencyFilter not yet implemented (Plan 04)
        true.ShouldBeFalse("not yet implemented — Phase 5 Plan 04 (REG-08 — non-GUID passthrough)");
    }
}
