# Phase 55 Review --- controller-di-reduction (iter 1)

**Verdict:** APPROVED_WITH_WARNINGS

---

## Gates

- **[G1 Multi-tenant isolation]** PASS --- No HasQueryFilter configurations modified. No new bare IgnoreQueryFilters call introduced. [FromServices] tenant-guard repos in GetById endpoints still enforce explicit ClienteId != _currentCompanyService.CompanyId checks (FundosController:151, :293, :572, :653; FundoCedentesController:72, :123; analogously FundoTiposAtivos, CedenteTiposAtivos). Isolation semantics unchanged.

- **[G2 Endpoint AuthZ + audit]** PASS --- All 9 controllers verified. Every Http-verb endpoint has class-level or per-action Authorize. Auth endpoints (Login, Refresh, Logout, GetMe, ForgotPassword, ResetPassword) are intentionally public for OIDC flow. All mutation commands capture ActorSub + ActorEmail via GetActorContext() or GetAuditContext().

- **[G3 Secret + raw SQL]** PASS --- No secrets in appsettings. No FromSqlRaw with string concat or interpolation in new code.

- **[G4 Telemetry]** PASS with pre-existing warnings --- No Console.Write*, no rogue ActivitySource/Meter in new code. Program.cs NOT modified by this phase. OTel, Serilog, instrumentation, OTLP exporter all present. Pre-existing gaps (PiiScrubber, TenantBaggageMiddleware, TelemetryCommandHandlerDecorator) predate phase 55; not under scope.

- **[G5 Performance hygiene]** PASS --- All list endpoints paginated. No unbounded returns. Dispatch classes perform no DB calls.

- **[G6 Index coverage]** N/A --- No new migrations in this phase.

- **[G7 Build]** PASS --- dotnet build --no-incremental: 0 warnings, 0 errors.

- **[G8 Lint]** PASS --- dotnet format --verify-no-changes: clean.

- **[G9 DDD/Design]** PASS --- Abstractions in Application.Common, impls in Infrastructure.Dispatch. No anemic aggregates, no public setters, no MediatR, no FluentAssertions. CommandDispatcher/QueryDispatcher are internal sealed. Reflection with ConcurrentDictionary type-cache (D-60). No cross-aggregate entity refs.

- **[G10 Tests]** PASS
  - Onboarding.Domain.Tests: 513 pass, 0 fail, 0 skip
  - Onboarding.API.Tests: 506 pass, 0 fail, 4 skip (pre-existing)
  - Onboarding.Application.Tests: 222 pass, 0 fail, 0 skip
  - Onboarding.Infrastructure.Tests: 217 pass, 0 fail, 0 skip
  - Doer-reported counts confirmed by authoritative re-run.

- **[G11 Coverage --- new files]** PASS
  - CommandDispatcher.cs: 93.5% block coverage (uncovered: GetOrAdd lambda throw branch --- structurally unreachable for valid ICommandHandler)
  - QueryDispatcher.cs: 93.5% block coverage (same pattern)
  - ValidationRunner.cs: 100% block coverage
  - Aggregate Dispatch: 91.3% block coverage (>80% threshold)

- **[G12 Playwright regression]** MAIN-THREAD-PENDING --- Docker stack not running. Per gate rules, verdict not failed solely on Docker unavailability.

- **[G13 Static scans]** NOT RUN (advisory) --- No new NuGet packages. Risk: LOW.

---

## D-62 Gate Judgement --- [FromServices] Method Injection

**Question:** Does moving single-endpoint repos from ctor to [FromServices] action-param injection legitimately satisfy D-62 (<=5 ctor deps), or is it gaming the metric?

**Ruling: LEGITIMATE. Not gaming.**

1. Gate text scopes deps to constructor injection. [FromServices] is not ctor injection.
2. Pre-existing pattern: [FromServices] for ForgotPassword/ResetPassword handlers existed at boundary commit 968eefb (confirmed via git show).
3. Correct scoping: repos used in exactly one action belong at action level, not ctor level.
4. Security invariant preserved: explicit ClienteId check immediately follows every [FromServices] repo call, unchanged from pre-refactor (confirmed against 044c2ac).
5. Counter-argument rejected: ctor deps resolve per-request regardless of action; [FromServices] resolve only for the specific action invoked.

**Gate result: All 9 controllers pass D-62.**

---

## Blockers

None.

---

## Warnings

- **W1 [G4, pre-existing]** Program.cs missing PiiScrubber, TenantBaggageMiddleware, TelemetryCommandHandlerDecorator --- predates phase 55; not introduced here. Carry-forward from Phase 54.

- **W2 [G11, lambda]** CommandDispatcher.cs and QueryDispatcher.cs GetOrAdd lambda: 66.7% block coverage --- uncovered block is throw for HandleAsync absent from interface (structurally unreachable). Aggregate Dispatch coverage 91.3%.

- **W3 [G12, pending]** Integration.Tests (Testcontainers) and UAT/Playwright not run --- Docker unavailable in sandbox. MUST run on main thread before shipping.

- **W4 [WARNINGS.md design debt]** AuthController.ForgotPassword/ResetPassword bypass ICommandDispatcher via [FromServices] ICommandHandler<T,Unit> --- pre-existing (W-AUTH-NONCQRS). Gate passes (ctor: 3 deps). Ambiguous Unit-routing risk documented.

- **W5 [WARNINGS.md SOLID-04]** AuthController.GetMe + CompaniesController.GetMe use [FromServices] repos for non-CQRS permission resolution --- SOLID-04 deferred from Phase 54 (W-AUTHCONTROLLER-REPO-SOLID04). Gate passes. Future: GetPermissionsFromTokenQuery handler.

---

## Coverage gaps (new files)

| File | Block Coverage | Required | Delta |
|---|---|---|---|
| src/Onboarding.Infrastructure/Dispatch/CommandDispatcher.cs | 93.5% | 80% | +13.5% |
| src/Onboarding.Infrastructure/Dispatch/QueryDispatcher.cs | 93.5% | 80% | +13.5% |
| src/Onboarding.Infrastructure/Dispatch/ValidationRunner.cs | 100% | 80% | +20% |
| Dispatch aggregate | 91.3% | 80% | +11.3% |

---

## Regression captures (MAIN-THREAD-PENDING)

Required before /jdi-ship:

1. Registration --- POST /api/companies/registration returns 201 with Keycloak user created.
2. Login/logout --- POST /api/auth/login returns 200 with access token + httpOnly refreshToken cookie; POST /api/auth/logout returns 204; POST /api/auth/refresh returns 200.
3. Dispatched GET tenant-filtered --- GET /api/fundos with valid BearerClient token returns 200 paginated; tenant A token must not return tenant B rows.
4. Dispatched POST 422 --- POST /api/fundos/consultorias with missing required fields returns 422 with ValidationProblemDetails (D-61 preserved).
5. AuthZ 403 --- GET /api/fundos with token lacking fund:read returns 403.
6. AuthZ 401 --- GET /api/fundos with expired/invalid token returns 401.
7. AdminFundos 1-dep --- GET /api/admin/fundos with BearerBackoffice token returns 200 (AdminFundosController with IQueryDispatcher only).
8. Actor in audit --- POST mutation then GET /api/admin/audit-log shows correct ActorSub + ActorEmail.

UAT runner: node tests/run-uat.mjs (requires compose.test.yml stack).
Integration: dotnet test tests/Onboarding.Integration.Tests/ (Testcontainers).
