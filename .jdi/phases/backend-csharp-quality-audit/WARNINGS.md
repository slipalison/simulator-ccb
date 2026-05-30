# Warnings & Deferred Items — Phase 54 (backend-csharp-quality-audit)

Items found during the audit that were intentionally NOT applied this phase (per D-54 safe-fix boundary, the hard constraint "don't break Front/Keycloak", or scope). Each is a candidate for a future phase / `todos.md`.

## Deferred refactors (D-53 / D-54)

- **W-FUNDOS-SPLIT** — `FundosController` (1100 LoC god class, W2 carry from Phase 48) only partially reduced via private extraction (D-53). Full split into per-resource controllers (ConsultoriaFundo/Custodiante/TipoAtivo/Fundo/Cedente) is deferred: it changes ASP.NET route discovery per controller type and needs a dedicated sub-phase with full Playwright regression.
- **W-AUTH-DIP** (SEC/SOLID) — `AuthController` depends directly on 3 domain repositories; `ResolvePermissionsFromAccessTokenAsync` belongs in an Application query handler. Deferred — Keycloak-critical auth flow (ACF+PKCE); refactor needs full auth regression before shipping.
- **W-AUTH-LAYERING** — `AuthController` imports `Onboarding.Infrastructure.Keycloak.KeycloakAuthException` directly (API→Infrastructure). Moving the exception to Application/Domain is behavior-preserving but touches multiple exception paths. Deferred.
- **W-COMPANIES-METHODSIZE** — `CompaniesController.RegisterEmployee` (~54 LoC) and `RegisterCompany` (~52 LoC) still exceed the D-52 method-size threshold (≤20). Further reduction needs Application-side guard absorption or per-endpoint middleware for actor/IP extraction — out of the behavior-preserving boundary for this phase.

## Security (D-51 — triaged, not fixed)

- **SEC-02/04** — `IdempotencyFilter` deserializes `object?` from `IDistributedCache`. NOT a real RCE: System.Text.Json without `TypeNameHandling` materializes as `JsonElement` (no gadget chains). A typed-union fix risks changing the idempotent response shape. Mitigation: Redis ACL in production. Deferred.
- **SEC-KC-PWD** — Keycloak password policy min length is 8 (convention target 12). Pre-existing; not changed (hard constraint forbids Keycloak config changes this phase).
- **SEC-ROPC** — `onboarding-app` ROPC legacy client still enabled. Planned removal under D-11. Pre-existing.
- **SEC-SECRET** — `appsettings.json` `AdminClientSecret` is a dev placeholder (pre-D-2 boundary, runtime-injected via compose env var). Cosmetic; not a leaked production secret.

## Performance (deferred / accepted debt)

- **PERF-04** — `GetPaginatedAdministratorsQuery` paginates in memory (Keycloak Admin REST API has no server-side role+filter pagination in one call). Accepted debt for small admin sets; documented in code.
- **PERF-CEDENTE** — `CedenteRepository.GetByIdAsync` intentionally retains change-tracking (shadow-property `Documento` reconstruction via `_db.Entry`); `ChangeTracker.Clear()` covers the list path. Documented in code.
- **W-SEARCH-CLIENTSIDE** — `ConsultoriaFundoRepository`/`CustodianteRepository` `GetPagedByCompanyAsync` filter CNPJ client-side (in-memory after `ToListAsync`). Not broken (no EF translation attempted), but inefficient. Could migrate to the FromSql pattern (D-58) for consistency/perf. Minor.

## Coverage / testing notes

- **D-56 InMemory redundancy** — the 200 InMemory repo tests added after removing `[ExcludeFromCodeCoverage]` are partly redundant with the Testcontainers Integration.Tests (which exercise the same repos more faithfully, incl. raw SQL / partial indexes / shadow props that InMemory cannot). Kept per the user's literal "no exclusions" choice (D-56). Several repos' provider-specific paths (ILIKE search, REL-09 partial index, shadow-property LINQ) are covered ONLY by Integration.Tests — InMemory exercises the C# lines but not the SQL fidelity.

## Bug FIXED this phase (not deferred — recorded for visibility)

- **D-58 — admin search 500** — Found via W4 coverage work: admin search by name/email/CNPJ (Company/Admin/Employee/Fundo/Consultoria/Custodiante/Cedente) returned 500 because Email/Cnpj value-converter columns are opaque to LINQ (no LIKE/ILIKE translation). Fixed across 7 sites with `FromSqlInterpolated` (migration-free, parameterized). Now tested + covered. This was a real latent production defect surfaced by the audit.

## Carry-forward from prior phases (NOT this phase's scope — backend-only audit)

- Frontend / OTel JS telemetry warnings (WFE-*) from Phase 53 remain (both SPAs).
- Backend telemetry wiring carry-forward (PII scrubber naming, TenantBaggageMiddleware/TelemetryCommandHandlerDecorator) from Phase 53 — telemetry was out of scope here.
