---
phase: 04-observability
plan: "00"
subsystem: testing
tags: [xunit, dotnet, observability, opentelemetry, serilog, health-checks, tdd, test-scaffold]

# Dependency graph
requires:
  - phase: 03-backend-domain-layer
    provides: Onboarding.API project and solution structure this test project references
provides:
  - xUnit test project Onboarding.API.Tests targeting net10.0 with MVC Testing
  - 16 failing stub tests covering SEC-09, OBS-01, OBS-04, OBS-05 behaviors
  - Automated --filter verify commands for Wave 1 plans 04-01 and 04-02
affects:
  - 04-01 (Serilog + OpenTelemetry wiring — uses --filter Category=Observability,Security)
  - 04-02 (health checks implementation — uses --filter Category=HealthCheck)

# Tech tracking
tech-stack:
  added:
    - Microsoft.AspNetCore.Mvc.Testing 10.0.0
  patterns:
    - "[Trait(\"Category\", \"...\")] on test classes for dotnet test --filter targeting"
    - "Stub RED tests use true.ShouldBeFalse(\"...\") — xUnit 2.9.3 lacks Assert.Fail"

key-files:
  created:
    - tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj
    - tests/Onboarding.API.Tests/Observability/SensitiveDataDestructuringPolicyTests.cs
    - tests/Onboarding.API.Tests/HealthChecks/HealthCheckEndpointTests.cs
    - tests/Onboarding.API.Tests/Observability/TracePropagationTests.cs
  modified:
    - Onboarding.slnx

key-decisions:
  - "xUnit 2.9.3 has no Assert.Fail — use true.ShouldBeFalse(message) for RED stubs"
  - "Single test project Onboarding.API.Tests covers all observability categories (Observability, Security, HealthCheck)"

patterns-established:
  - "Pattern: RED stub tests use Shouldly ShouldBeFalse with descriptive message indicating which future plan implements the feature"

requirements-completed: [OBS-01, OBS-04, OBS-05, SEC-09]

# Metrics
duration: 2min
completed: 2026-04-03
---

# Phase 4 Plan 00: Observability Test Scaffold Summary

**xUnit test project with 16 failing stubs covering masking (SEC-09), trace propagation (OBS-04), and health checks (OBS-05) — Wave 0 scaffold enabling automated verify commands for all Phase 4 plans**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-04-03T20:24:00Z
- **Completed:** 2026-04-03T20:25:52Z
- **Tasks:** 2 completed
- **Files modified:** 5

## Accomplishments

- Created Onboarding.API.Tests xUnit project targeting net10.0 with Microsoft.AspNetCore.Mvc.Testing
- Wrote 9 failing stubs for SensitiveDataDestructuringPolicy (SEC-09, OBS-01) across security masking scenarios (password, token, secret, CPF, email, authorization header)
- Wrote 5 failing stubs for health check endpoints (OBS-05): live, ready, 503 on failure, JSON response, Docker Compose fast-path
- Wrote 2 failing stubs for W3C trace propagation (OBS-04): traceparent header, TraceId-log correlation
- All 16 tests compile cleanly and fail (RED state confirmed), unblocking Wave 1 --filter verify commands

## Task Commits

1. **Task 1: Create Onboarding.API.Tests project and add to solution** - `41cac2e` (feat)
2. **Task 2: Write failing stub test files for all observability behaviors** - `7f16e43` (test)

## Files Created/Modified

- `tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj` - xUnit project with MVC Testing, NSubstitute, Shouldly, ProjectReference to Onboarding.API
- `tests/Onboarding.API.Tests/Observability/SensitiveDataDestructuringPolicyTests.cs` - 9 stubs tagged [Category=Observability] and [Category=Security]
- `tests/Onboarding.API.Tests/HealthChecks/HealthCheckEndpointTests.cs` - 5 stubs tagged [Category=HealthCheck]
- `tests/Onboarding.API.Tests/Observability/TracePropagationTests.cs` - 2 stubs tagged [Category=Observability]
- `Onboarding.slnx` - Added new test project under /tests/ folder

## Decisions Made

- xUnit 2.9.3 does not have `Assert.Fail` — used `true.ShouldBeFalse("message")` as the RED stub pattern throughout, consistent with the Shouldly library already in use
- Single test project covers all three observability categories rather than splitting by category — simpler to maintain given the small test count per category

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Test scaffold complete: Wave 1 plans (04-01 and 04-02) can immediately reference `dotnet test tests/Onboarding.API.Tests/ --filter "Category=Observability"`, `--filter "Category=Security"`, and `--filter "Category=HealthCheck"` as automated verify commands
- Plan 04-01 will implement SensitiveDataDestructuringPolicy and OpenTelemetry wiring, turning the RED stubs GREEN
- Plan 04-02 will implement /healthz/live and /healthz/ready endpoints, turning the health check stubs GREEN

---
*Phase: 04-observability*
*Completed: 2026-04-03*

## Self-Check: PASSED

- FOUND: tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj
- FOUND: tests/Onboarding.API.Tests/Observability/SensitiveDataDestructuringPolicyTests.cs
- FOUND: tests/Onboarding.API.Tests/HealthChecks/HealthCheckEndpointTests.cs
- FOUND: tests/Onboarding.API.Tests/Observability/TracePropagationTests.cs
- FOUND: .planning/phases/04-observability/04-00-SUMMARY.md
- Commit 41cac2e verified (Task 1)
- Commit 7f16e43 verified (Task 2)
