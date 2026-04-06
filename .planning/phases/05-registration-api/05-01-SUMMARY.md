---
phase: 05-registration-api
plan: 01
subsystem: testing
tags: [xunit, shouldly, testcontainers, tdd, stubs]

requires:
  - phase: 04-observability
    provides: WebApplicationFactory pattern, test infrastructure setup

provides:
  - 14 failing stub tests in Onboarding.API.Tests covering REG-03, REG-04, REG-05, REG-06, REG-08, BACK-05, SEC-08
  - 4 failing stub tests extending RegisterClientCommandHandlerTests (REG-05, REG-06)
  - Onboarding.Integration.Tests project with Testcontainers.Keycloak + Testcontainers.PostgreSql
  - 2 failing end-to-end integration stubs for REG-06
affects: [05-02, 05-03, 05-04]

tech-stack:
  added: [Testcontainers.Keycloak 4.11.0, Testcontainers.PostgreSql 4.11.0]
  patterns: [RED stub pattern with ShouldBeFalse("not yet implemented — Phase 5 Plan XX"), RegistrationTestApiFactory mirrors HealthCheckEndpointTests approach]

key-files:
  created:
    - tests/Onboarding.API.Tests/Registration/RegistrationControllerTests.cs
    - tests/Onboarding.API.Tests/Registration/IdempotencyFilterTests.cs
    - tests/Onboarding.Integration.Tests/Onboarding.Integration.Tests.csproj
    - tests/Onboarding.Integration.Tests/Registration/RegistrationIntegrationTests.cs
  modified:
    - tests/Onboarding.Domain.Tests/Application/Commands/RegisterClientCommandHandlerTests.cs
    - Onboarding.slnx

key-decisions:
  - "RegistrationControllerTests uses RegistrationTestApiFactory (mirrors HealthCheckEndpointTests) to allow TestServer to start without real DB — stubs fail at ShouldBeFalse before any HTTP call"
  - "Integration.Tests containers not started in InitializeAsync stubs — keeps Wave 0 fast, containers wire up in Plan 04"
  - "Solution file is .slnx format (not .sln) — used XML edit instead of dotnet sln add"

patterns-established:
  - "RED stub pattern: true.ShouldBeFalse('not yet implemented — Phase 5 Plan XX (REQ-ID)') with requirement tag in message"
  - "RegistrationTestApiFactory: provides fake connection strings + removes real health checks so WebApplicationFactory TestServer can boot without infrastructure"

requirements-completed: [REG-03, REG-04, REG-05, REG-06, REG-08, BACK-05, SEC-08]

duration: 15min
completed: 2026-04-06
---

# Phase 05 Plan 01: Test Stubs (Wave 0 TDD)

**20 failing RED stubs established across 4 test files — every Phase 5 requirement has a named, tagged stub before any production code exists.**

## Performance

- **Duration:** ~15 min
- **Completed:** 2026-04-06
- **Tasks:** 2/2
- **Files modified:** 6

## Accomplishments

- Created `RegistrationControllerTests.cs` with 11 stubs covering REG-03 (2), REG-04 (2), REG-05 (2), SEC-08 (2), BACK-05 (1), REG-08 (2)
- Created `IdempotencyFilterTests.cs` with 3 stubs covering REG-08 filter behavior
- Extended `RegisterClientCommandHandlerTests.cs` with 4 stubs: duplicate CPF/email (REG-05) and Keycloak integration/compensation (REG-06)
- Created `Onboarding.Integration.Tests` project with Testcontainers.Keycloak 4.11.0 + Testcontainers.PostgreSql 4.11.0
- Created `RegistrationIntegrationTests.cs` with 2 end-to-end stubs for REG-06
- Added Integration.Tests to `Onboarding.slnx`

## Verification Results

- `dotnet test API.Tests --filter Registration`: **14 failed** (ShouldBeFalse) — ✓
- `dotnet test Domain.Tests --filter CommandHandler`: **5 passed, 4 failed** (ShouldBeFalse) — ✓
- `dotnet build Integration.Tests`: **Build succeeded, 0 errors** — ✓
- Existing tests (Observability + HealthCheck): **14 passed** — ✓

## Self-Check: PASSED
