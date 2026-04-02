---
phase: 03-backend-domain-layer
plan: "02"
subsystem: api
tags: [dotnet, csharp, cqrs, command-handler, application-layer, di, tdd, nsubstitute, shouldly]

# Dependency graph
requires:
  - phase: 03-backend-domain-layer
    plan: "01"
    provides: "Client aggregate, IClientRepository interface, value objects (Cpf, Cnpj, Email, PhoneNumber)"
provides:
  - "ICommandHandler<TCommand,TResult> and IQueryHandler<TQuery,TResult> CQRS interfaces"
  - "Unit readonly struct for void commands"
  - "RegisterClientCommand sealed record with Password field (forwarded to Keycloak in Phase 5)"
  - "RegisterClientResult DTO"
  - "RegisterClientCommandHandler dispatching to PF/PJ factory based on Cpf presence"
  - "AddApplication() DI extension registering handler as Scoped"
  - "5 handler unit tests (PF path, PJ path, invalid CPF, null Nome, Password-not-in-domain)"
affects:
  - 04-infrastructure-layer
  - 05-registration-api
  - 06-integration-tests

# Tech tracking
tech-stack:
  added:
    - "Microsoft.Extensions.DependencyInjection.Abstractions 10.0.5 (MIT) — IServiceCollection for AddApplication()"
  patterns:
    - "Manual CQRS via DI: ICommandHandler<TCommand,TResult> injected directly — no MediatR"
    - "Application DI extension: AddApplication() static class registers all handlers as Scoped"
    - "Password flows through command but is intentionally excluded from domain model"

key-files:
  created:
    - src/Onboarding.Application/Common/ICommandHandler.cs
    - src/Onboarding.Application/Common/IQueryHandler.cs
    - src/Onboarding.Application/Common/Unit.cs
    - src/Onboarding.Application/Clients/Commands/RegisterClientCommand.cs
    - src/Onboarding.Application/Clients/Commands/RegisterClientCommandHandler.cs
    - src/Onboarding.Application/Clients/DTOs/RegisterClientResult.cs
    - src/Onboarding.Application/DependencyInjection.cs
    - tests/Onboarding.Domain.Tests/Application/Commands/RegisterClientCommandHandlerTests.cs
  modified:
    - src/Onboarding.Application/Onboarding.Application.csproj
    - tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj

key-decisions:
  - "No MediatR — ICommandHandler<TCommand,TResult> interface used directly for CQRS; handlers injected via built-in .NET DI"
  - "Password included in RegisterClientCommand but intentionally absent from Client aggregate — Keycloak owns credentials, handler has TODO for Phase 5 IKeycloakUserService call"
  - "Microsoft.Extensions.DependencyInjection.Abstractions added to Application project (MIT, standard .NET SDK package) — needed for IServiceCollection in AddApplication()"
  - "Application.csproj references only Onboarding.Domain — no Infrastructure or persistence packages (layer boundary maintained)"

patterns-established:
  - "CQRS handler: sealed class implementing ICommandHandler<TCommand,TResult>, constructor injection of repository"
  - "DI extension: public static ApplicationServiceExtensions class with AddApplication() wiring all handlers as AddScoped"
  - "Handler unit test: NSubstitute mock of IClientRepository, verify AddAsync called with correct Client type"

requirements-completed:
  - BACK-04
  - BACK-06

# Metrics
duration: 2min
completed: 2026-04-02
---

# Phase 03 Plan 02: CQRS Application Layer Summary

**ICommandHandler interface + RegisterClientCommandHandler dispatching PF/PJ via Client factories, DI wiring via AddApplication(), and 5 unit tests using NSubstitute — no MediatR, Application references only Domain**

## Performance

- **Duration:** 2 min
- **Started:** 2026-04-02T21:17:55Z
- **Completed:** 2026-04-02T21:19:56Z
- **Tasks:** 2 (TDD: RED + GREEN)
- **Files modified:** 10

## Accomplishments

- CQRS interface contracts (ICommandHandler, IQueryHandler, Unit) added to Application layer with zero third-party mediator
- RegisterClientCommandHandler dispatches to Client.RegisterPessoaFisica or RegisterPessoaJuridica based on Cpf presence; Password intentionally excluded from domain with TODO for Phase 5 Keycloak forwarding
- AddApplication() DI extension registers handler as Scoped; Application.csproj references only Domain (layer boundary preserved)
- 38 total tests all green (33 domain + 5 new handler tests)

## Task Commits

1. **Task 1: Write failing handler tests and CQRS interface stubs (RED phase)** - `0cbc313` (test)
2. **Task 2: Implement RegisterClientCommandHandler and DI wiring (GREEN phase)** - `1aa7371` (feat)

## Files Created/Modified

- `src/Onboarding.Application/Common/ICommandHandler.cs` — Generic CQRS command handler interface with HandleAsync
- `src/Onboarding.Application/Common/IQueryHandler.cs` — Generic CQRS query handler interface with HandleAsync
- `src/Onboarding.Application/Common/Unit.cs` — readonly struct for void commands (Unit.Value)
- `src/Onboarding.Application/Clients/Commands/RegisterClientCommand.cs` — sealed record with Nome, Cpf?, Cnpj?, RazaoSocial?, Email, Phone, Password (with comment: Password not stored in domain)
- `src/Onboarding.Application/Clients/Commands/RegisterClientCommandHandler.cs` — implements ICommandHandler<RegisterClientCommand, Guid>; PF/PJ dispatch; TODO Phase 5 comment for Keycloak forwarding
- `src/Onboarding.Application/Clients/DTOs/RegisterClientResult.cs` — sealed record RegisterClientResult(Guid ClientId)
- `src/Onboarding.Application/DependencyInjection.cs` — ApplicationServiceExtensions.AddApplication() registers handler as Scoped
- `src/Onboarding.Application/Onboarding.Application.csproj` — added Microsoft.Extensions.DependencyInjection.Abstractions 10.0.5 (MIT)
- `tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj` — added ProjectReference to Onboarding.Application
- `tests/Onboarding.Domain.Tests/Application/Commands/RegisterClientCommandHandlerTests.cs` — 5 tests: PF path, PJ path, invalid CPF, null Nome, Password-not-in-domain reflection test

## Decisions Made

- **No MediatR**: ICommandHandler<TCommand,TResult> used directly — MediatR is no longer open source (commercial license). Manual DI is simpler and keeps CLAUDE.md compliance.
- **Password in command, not domain**: RegisterClientCommand carries Password but Client aggregate has no Password property. Phase 5 handler will forward to IKeycloakUserService.CreateUserAsync. A TODO comment marks the injection point.
- **Microsoft.Extensions.DependencyInjection.Abstractions**: Added to Application project (MIT license, Microsoft-maintained). Required for IServiceCollection type in AddApplication(). Compliant with OSS-only library rule.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added Microsoft.Extensions.DependencyInjection.Abstractions package**
- **Found during:** Task 2 (DependencyInjection.cs implementation)
- **Issue:** DependencyInjection.cs uses IServiceCollection which is not available in a bare net10.0 class library without the abstractions package
- **Fix:** `dotnet add src/Onboarding.Application/ package Microsoft.Extensions.DependencyInjection.Abstractions` — resolves to 10.0.5 (MIT)
- **Files modified:** `src/Onboarding.Application/Onboarding.Application.csproj`
- **Verification:** `dotnet build src/Onboarding.Application/` exits 0 with 0 warnings
- **Committed in:** `1aa7371` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 3 — blocking missing package)
**Impact on plan:** Package is MIT-licensed and a standard .NET SDK component. No scope creep. Layer boundary maintained (Application still references only Domain + DI Abstractions; no Infrastructure reference).

## Issues Encountered

None — TDD RED→GREEN cycle proceeded exactly as planned. RED confirmed missing handler type (CS0246). GREEN resolved with all 38 tests passing.

## User Setup Required

None — no external service configuration required. Application layer is pure C# + .NET SDK packages.

## Next Phase Readiness

- Application layer complete with CQRS contracts and RegisterClient handler — ready for Plan 04 (Infrastructure: EF Core + repository implementation)
- AddApplication() DI extension ready to be called from API's Program.cs in Phase 5
- ICommandHandler<RegisterClientCommand, Guid> ready for injection into the Registration controller
- No blockers for Plan 04

## Self-Check: PASSED

- FOUND: src/Onboarding.Application/Common/ICommandHandler.cs
- FOUND: src/Onboarding.Application/Common/IQueryHandler.cs
- FOUND: src/Onboarding.Application/Common/Unit.cs
- FOUND: src/Onboarding.Application/Clients/Commands/RegisterClientCommand.cs
- FOUND: src/Onboarding.Application/Clients/Commands/RegisterClientCommandHandler.cs
- FOUND: src/Onboarding.Application/Clients/DTOs/RegisterClientResult.cs
- FOUND: src/Onboarding.Application/DependencyInjection.cs
- FOUND: tests/Onboarding.Domain.Tests/Application/Commands/RegisterClientCommandHandlerTests.cs
- Commits verified: 0cbc313 (RED), 1aa7371 (GREEN)
- Tests: 38/38 passed, 0 failed, 0 skipped

---
*Phase: 03-backend-domain-layer*
*Completed: 2026-04-02*
