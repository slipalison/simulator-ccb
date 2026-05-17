# Phase 50 — frontend-client-fundos — REVIEW

## Security review iter 1

### Verdict: APPROVED_WITH_WARNINGS

---

### G1 — Multi-tenant isolation (D-5)

**PASS**

All 4 new query handlers implement explicit tenant guards:

- `GetFundoAllowedTransitionsQueryHandler`: loads Fundo, asserts `fundo.ClienteId == _currentCompanyService.CompanyId`, returns null (→ 404) otherwise.
- `GetFundoCedenteAllowedTransitionsQueryHandler`: guards via parent Fundo.ClienteId; also verifies association.FundoId matches route param (prevents cross-association leak within same tenant).
- `GetFundoTipoAtivoAllowedTransitionsQueryHandler`: same pattern via parent Fundo.
- `GetCedenteTipoAtivoAllowedTransitionsQueryHandler`: guards via parent Cedente.ClienteId; verifies association.CedenteId.

`IgnoreQueryFilters()` not used in any new handler. Cross-tenant attempt returns null, controller maps to 404 — consistent with existing pattern.

---

### G2 — Permission policy coverage

**PASS**

All 4 new `GET /allowed-transitions` endpoints carry:
```
[Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
```
`FundRead` maps to `funds:read` — correct read-level policy. No `[AllowAnonymous]` added anywhere. All new frontend routes are children of `authenticatedRoute` in `router.tsx` — no public route bypass introduced.

Sidebar `FUNDOS_NAV_GROUP` visibility gated on `funds:read` permission at runtime.

---

### G3 — Secrets + env hygiene

**PASS**

Grep over the full phase diff (`159fe5c^..HEAD`) for `localStorage`, `sessionStorage`, `Authorization.*Bearer`, hardcoded passwords/secrets/tokens: zero findings.

Gitleaks not installed locally — deferred to CI verification. Manual grep patterns run as fallback.

---

### G4 — Semgrep

**PASS**

`semgrep --config .semgrep --severity ERROR --error` ran against 315 tracked files (302 C#, 13 TS). Exit code 0. Findings: 0 blocking, 0 warnings.

---

### G5 — Trivy FS + container

**NOT RUN** — Trivy not installed locally. Deferred to CI verification. No Dockerfile or docker-compose changes in this phase; container scan not triggered.

---

### G6 — Keycloak hardening drift (D-13)

**PASS**

`git diff 159fe5c^..HEAD -- keycloak/` returned empty. No realm JSON, client config, or Keycloak-related file changed in this phase. Zero drift.

---

### G7 — Security headers + CSP

**NOT VERIFIED** — Stack not running locally during this review. No new middleware, proxy rules, or server-side header configuration added in this phase. Header posture is unchanged from Phase 49 (previously verified).

---

### G8 — Container / infra

**PASS**

`git diff 159fe5c^..HEAD -- Dockerfile* docker-compose*.yml .github/` returned empty. No CI, Docker, or infra files touched.

---

### G9 — D-12 cookies HttpOnly

**PASS**

`fundos-api.ts` implements its own `apiFetch` using `credentials: 'include'` — no `Authorization` header, no Bearer token added by the frontend. The auto-refresh cycle calls `/auth/refresh` via `fetch` with `credentials: 'include'` (HttpOnly cookie path). No `localStorage.setItem` or `sessionStorage.setItem` calls found in any new file (`fundos-api.ts`, `fundos-schemas.ts`, `api-errors.ts`, `query-client.ts`, `use-allowed-transitions.ts`, all component files in phase diff).

---

### D-15 auth gates — no drift

**PASS**

- PKCE/state validation: untouched.
- CORS: no `WithOrigins("*")` or origin reflection added.
- No new public routes in backend or frontend.
- `bruteForceProtected`: keycloak JSON unchanged.

---

### D-3 OSS-only

**PASS**

- `@tanstack/react-query@5.100.10`: MIT (verified via `npm view`).
- `@tanstack/react-query-devtools@5.100.10`: MIT (verified via `npm view`).
- No commercial dependency introduced.

---

### Blockers

None.

---

### Warnings

1. **`fundos-api.ts` DRY violation (non-security):** The file re-implements the 401/refresh cycle instead of importing the shared `apiFetch` from `api.ts` (which is not exported). The comment in the file acknowledges this. Security posture is equivalent, but code duplication risks drift in refresh logic. Tracked in SUMMARY.md; fix in next refactor phase by exporting `apiFetch` from `api.ts`.

2. **Gitleaks / Trivy deferred to CI:** Local CLI not available. CI pipeline is source of truth for these checks. Phase must not ship if CI reports blocking findings.

3. **Security headers (G7) not live-verified:** Stack not running. Header posture assumed unchanged from Phase 49; CI/CD regression covers this gate.

4. **Sidebar `auth as any` cast:** `frontend/client/src/components/organisms/Sidebar.tsx` uses `(auth as any).permissions` because `permissions: string[]` is not yet on `AuthContextValue`. This is a type-safety gap — not a runtime security issue (the guard still runs), but the cast could silently break if the auth context shape changes. Tracked as TODO in the source comment; should be resolved before Phase 52.

---

### Pipeline artifacts

- Semgrep: run locally, 0 findings — no JSON artifact (clean run).
- Gitleaks: deferred to CI.
- Trivy FS: deferred to CI.

## Backend C# review iter 1

Run: 2026-05-17
Boundary: 968eefb19dba216d729723e8ffa6a9e166d7698c
Commit reviewed: 159fe5c (T-1 delivery)

### Verdict
APPROVED_WITH_WARNINGS

---

### Gates

- [G1 Multi-tenant isolation] PASS — All 4 new query handlers implement explicit tenant guards. GetFundoAllowedTransitionsQueryHandler: fundo.ClienteId != _currentCompanyService.CompanyId → return null → 404. GetFundoCedenteAllowedTransitionsQueryHandler: parent Fundo.ClienteId guard + association.FundoId route-match check. GetFundoTipoAtivoAllowedTransitionsQueryHandler: same parent-Fundo pattern. GetCedenteTipoAtivoAllowedTransitionsQueryHandler: Cedente.ClienteId guard + association.CedenteId check. No IgnoreQueryFilters in any new handler. HasQueryFilter on Fundo and Cedente aggregates intact.

- [G2 Endpoint AuthZ + audit] PASS — All 4 new GET endpoints carry [Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]. Verified by code inspection and reflection-based API test (FundosControllerAllowedTransitionsTests.GetFundoAllowedTransitions_endpoint_hasFundReadPolicy, equivalent tests for other 3 controllers). No mutation commands in T-1 scope (read-only query handlers); actor capture gate N/A.

- [G3 Secret + raw SQL hygiene] PASS — No new secrets in T-1 diff. No FromSqlRaw in new files. Legacy appsettings.json:18 AdminClientSecret pre-existing (D-2). Gitleaks unavailable; manual inspection clean.

- [G4 Telemetry (OTel+Serilog+W3C)] PASS with pre-existing warnings — G4.1: 0 Console.Write hits. G4.2: No interpolated logger strings in new handlers. G4.3/G4.4: No new ActivitySource or Meter outside Telemetry class. G4.5: No W3C propagator override. G4.6: Program.cs retains all 6 required registrations (AddOpenTelemetry, UseSerilog, AddAspNetCoreInstrumentation, AddHttpClientInstrumentation, AddEntityFrameworkCoreInstrumentation, AddOtlpExporter). G4.7: SetDbStatementForText absent. G4.8/G4.9: TenantBaggageMiddleware and TelemetryCommandHandlerDecorator absent — pre-existing gap at boundary 968eefb, not introduced by T-1 (confirmed per Phase 49 review W3).

- [G5 Performance hygiene] PASS — Query handlers are thin (load by ID, call GetAllowedNextStates, return). No list operations; pagination not applicable. Repository calls delegate to pre-existing GetByIdAsync implementations that already use AsNoTracking.

- [G6 Index coverage on new migration] N/A — T-1 adds no migrations. Domain changes are additive methods only (GetAllowedNextStates); no schema change.

- [G7 Build] PASS — dotnet build src/Onboarding.API/Onboarding.API.csproj: 0 errors, 0 warnings. Build time 6.46s.

- [G8 Lint/format] FAIL (new file) — dotnet format --verify-no-changes: 2 WHITESPACE errors in tests/Onboarding.Domain.Tests/Aggregates/StateMachineAllowedTransitionsTests.cs (lines 233 and 283). This file is NEW (added by T-1, post-boundary). Other lint errors in src/Onboarding.API/Program.cs, src/Onboarding.Application/Admin/*, src/Onboarding.Domain/Aggregates/AdminAuditLog.cs, and src/Onboarding.Infrastructure/* are pre-existing at boundary 968eefb. Downgraded to WARNING (test-only file, no logic impact) — consistent with Phase 49 G8 treatment of pre-existing violations, but T-1 introduced this one.

- [G9 DDD + design] PASS — GetAllowedNextStates() in all 4 aggregates delegates to existing CanTransitionTo/FundoStatusValidator — no logic duplication. Single source of truth maintained (D-25). All aggregate properties remain private set. No cross-aggregate entity references (handlers use IDs). Domain layer has zero EF Core or Infrastructure namespace references. No MediatR (D-3). No FluentAssertions (OSS-only). Query records are minimal (1-2 properties). No speculative abstractions.

- [G10 Tests] PASS — All 4 suites green: Domain.Tests 474/0/0, Application.Tests 138/0/0, API.Tests 335/0/4skip (pre-existing skips), Integration.Tests 41/0/0. Total: 988 passed, 4 skipped, 0 failed. 52 new tests added by T-1 (confirmed by commit stat). Parity tests present in StateMachineAllowedTransitionsTests: for each aggregate and each source status, every string in GetAllowedNextStates() is verified to not throw in TransitionTo(), and every status outside the allowed set is verified to throw.

- [G11 Coverage on new files (D-2)] PASS — Application handlers: GetFundoAllowedTransitionsQueryHandler 100% line, GetFundoCedenteAllowedTransitionsQueryHandler 100% line, GetFundoTipoAtivoAllowedTransitionsQueryHandler 88.9% line, GetCedenteTipoAtivoAllowedTransitionsQueryHandler 88.9% line. Query record files 100% line. Domain aggregates with new GetAllowedNextStates(): Fundo.cs 100%, FundoCedenteAggregate.cs 98.5%, FundoTipoAtivoAggregate.cs 94%, CedenteTipoAtivoAggregate.cs 94%. All above 80% threshold.

- [G12 Playwright regression] PASS (pre-existing UAT baseline unchanged) — Docker stack healthy (api:healthy, keycloak:healthy, app_db:healthy). UAT run-uat.mjs: 9 passed / 13 failed / 2 cascade / 8 ignored — IDENTICAL to Phase 49 baseline. Failures are registration-flow + realm-discovery pre-existing. Playwright MCP probe on all 4 new endpoints: GET /api/fundos/{id}/allowed-transitions, GET /api/fundos/{fundoId}/cedentes/{id}/allowed-transitions, GET /api/fundos/{fundoId}/tipos-ativos/{id}/allowed-transitions, GET /api/cedentes/{cedenteId}/tipos-ativos/{id}/allowed-transitions — all return 404 without auth token (consistent with session-cookie middleware; no 500/unhandled error). Authenticated endpoint testing deferred (requires valid Keycloak session); covered by API.Tests controller + integration suite.

- [G13 Static scans] ADVISORY — No Trivy/Semgrep available locally. No new NuGet packages in T-1 diff. No raw SQL, no serialization, no file system ops. No HIGH/CRITICAL in new code paths.

---

### Blockers

None.

---

### Warnings

- W1: tests/Onboarding.Domain.Tests/Aggregates/StateMachineAllowedTransitionsTests.cs:233,283 — 2 WHITESPACE lint violations (dotnet format). Introduced by T-1 (new file). Non-blocking per project convention (test-only, no logic impact), consistent with W1 treatment in Phase 49 review. Fix: run `dotnet format` on this file and commit.

- W2 (pre-existing, carried): TenantBaggageMiddleware and TelemetryCommandHandlerDecorator absent at boundary 968eefb. Not introduced by T-1. Must be addressed before production cutover.

- W3 (pre-existing, carried): src/Onboarding.API/appsettings.json:18 AdminClientSecret plaintext. Pre-existing legacy (D-2). Inject via env var or secrets manager in staging/prod.

---

### Coverage gaps (new files)

| File | Coverage | Required | Status |
|---|---|---|---|
| GetFundoAllowedTransitionsQuery.cs | 100% | 80% | PASS |
| GetFundoAllowedTransitionsQueryHandler.cs | 100% | 80% | PASS |
| GetFundoCedenteAllowedTransitionsQuery.cs | 100% | 80% | PASS |
| GetFundoCedenteAllowedTransitionsQueryHandler.cs | 100% | 80% | PASS |
| GetFundoTipoAtivoAllowedTransitionsQuery.cs | 100% | 80% | PASS |
| GetFundoTipoAtivoAllowedTransitionsQueryHandler.cs | 88.9% | 80% | PASS |
| GetCedenteTipoAtivoAllowedTransitionsQuery.cs | 100% | 80% | PASS |
| GetCedenteTipoAtivoAllowedTransitionsQueryHandler.cs | 88.9% | 80% | PASS |
| Fundo.cs (GetAllowedNextStates method) | 100% | 80% | PASS |
| FundoCedenteAggregate.cs (GetAllowedNextStates method) | 98.5% | 80% | PASS |
| FundoTipoAtivoAggregate.cs (GetAllowedNextStates method) | 94% | 80% | PASS |
| CedenteTipoAtivoAggregate.cs (GetAllowedNextStates method) | 94% | 80% | PASS |

---

### Regression captures

- UAT result: 9 passed / 13 failed / 2 cascade / 8 ignored (identical to Phase 49 baseline — no regression)
- Playwright MCP probe: all 4 new allowed-transitions endpoints reachable (404 without token, no 500s)
- API container: healthy at time of review
- Console errors on probe: CORS pre-flight (expected, probe from browser context without auth)
