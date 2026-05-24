# Phase 52 Review

## Reviewer: jdi-reviewer-onboarding-keycloak-backend-csharp

**Verdict:** BLOCKED

---

## Gates

- [G1 Multi-tenant isolation] FAIL - SECURITY BLOCKER
- [G2 Endpoint AuthZ + audit] PASS
- [G3 Secret + raw SQL] PASS
- [G4 Telemetry] WARN (pre-existing carry-forward)
- [G5 Performance hygiene] PASS
- [G6 Index coverage] PASS
- [G7 Build] PASS (0 errors, 0 warnings)
- [G8 Lint] FAIL - BLOCKER
- [G9 DDD/Design] PASS
- [G10 Tests] FAIL - BLOCKER (47/187 integration tests failing)
- [G11 Coverage] FAIL - 21 new src files below 80%
- [G12 Playwright regression] PARTIAL
- [G13 Static scans] Not run (advisory)

---

## Blockers

### B1 - G1 SECURITY: Cross-tenant Fundo status transition allows PJ-B to mutate PJ-A entity

Files:
- src/Onboarding.API/Controllers/FundosController.cs:744
- src/Onboarding.Application/Fundos/Commands/TransitionFundoStatusCommandHandler.cs:38
- src/Onboarding.Infrastructure/Repositories/FundoRepository.cs:38

Evidence - FundoCrudIntegrationTests.StateMachine_CrossTenantTransition_Returns404 FAILS:
  Expected: HttpStatusCode.NotFound
  Actual:   HttpStatusCode.OK
  PJ-B successfully transitioned a Fundo owned by PJ-A. Multi-tenant data mutation leak.

Root cause: FundoRepository.GetByIdAsync uses IgnoreQueryFilters() without ClienteId check.
TransitionFundoStatusCommandHandler does NOT verify fundo.ClienteId == actorCompanyId.
PJ-B can POST /api/fundos/{pjA-fundoId}/status and it succeeds with HTTP 200.

Fix: In FundosController.TransitionFundoStatus (~line 760), after loading fundo result, add:
  if (result.ClienteId != _currentCompanyService.CompanyId) return NotFound();
Identical guard already exists in GetFundoById at line 679.

### B2 - G10: 42 T-4 association integration tests fail with invalid CNPJ seed data

Files:
- tests/Fundos/FundoCedenteAssociationIntegrationTests.cs:116 - CNPJ 44222999000144
- tests/Fundos/CedenteTipoAtivoAssociationIntegrationTests.cs:107 - CNPJ 66444222000166
- tests/Fundos/FundoTipoAtivoAssociationIntegrationTests.cs:110 - CNPJ 88666444000188

Evidence:
  System.ArgumentException: CNPJ invalido: 44222999000144 (Parameter raw)
    at Cnpj.Create(String raw) in Cnpj.cs:17
    at Company.Register() in FundoCedenteAssociationIntegrationTests.cs:116
  All 42 T-4 tests fail in InitializeAsync() seed phase - 0 of 42 tests execute.

Fix: Replace all three invalid seed CNPJs with mathematically valid ones.
Known-valid: 11222333000181 (used in UAT), 22333444000155, 33444555000128.
Verify via Cnpj.IsValid() algorithm in src/Onboarding.Domain/ValueObjects/Cnpj.cs.

### B3 - G10: 2 T-2 search tests fail with EF Core LINQ translation error

Files:
- src/Onboarding.Infrastructure/Repositories/ConsultoriaFundoRepository.cs:66
- src/Onboarding.Infrastructure/Repositories/CustodianteRepository.cs (analogous block)

Evidence:
  System.InvalidOperationException: The LINQ expression
    c.Cnpj.Value.Contains(@digitsOnly) could not be translated.
  HTTP 500 on GET /api/fundos/consultorias?search=<term>

Root cause: c.Cnpj.Value.Contains(digitsOnly) traverses a value object property inside OR chain.
EF Core 10 cannot translate ValueObject.Property.Contains() to SQL in this context.

Fix: Replace .Contains() with EF.Functions.ILike(c.Cnpj.Value, "%"+digitsOnly+"%")
in ConsultoriaFundoRepository.cs:66 and CustodianteRepository.cs analogous block.

### B4 - G8: Lint - whitespace violations in modified files

dotnet format Onboarding.slnx --verify-no-changes exits non-zero with 14 whitespace errors.
Files modified after boundary with violations:
- src/Onboarding.Domain/Aggregates/AdminAuditLog.cs:31,38
- src/Onboarding.API/Program.cs:258
- src/Onboarding.Application/Admin/Commands/CreateAdminCommand.cs:40
- src/Onboarding.Application/Admin/Commands/ResetAdministratorPasswordCommand.cs:97-101
- src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs:30,35,85
- src/Onboarding.Infrastructure/Persistence/AppDbContextFactory.cs:16-19
Fix: Run dotnet format Onboarding.slnx and commit.


---

## Warnings

### W1 - G4 Telemetry: PII scrubber naming mismatch (pre-existing)

File: src/Onboarding.API/Program.cs
Gate G4.8 expects pattern PiiScrubbing|PiiScrubber in Program.cs.
Actual wiring: .Destructure.With<SensitiveDataDestructuringPolicy>().
Functionally equivalent but gate regex does not match.
Pre-existing at boundary commit 968eefb. Action: rename class or add alias in telemetry phase.

### W2 - G4 Telemetry: TenantBaggageMiddleware not wired (pre-existing)

File: src/Onboarding.API/Program.cs
Gate G4.8 requires TenantBaggage|UseMiddleware<TenantBaggageMiddleware in Program.cs.
Not present. Pre-existing at boundary. Action: track in telemetry phase.

### W3 - G4 Telemetry: TelemetryCommandHandlerDecorator not registered (pre-existing)

File: src/Onboarding.API/Program.cs
Gate G4.9 requires TelemetryCommandHandlerDecorator|Decorate(...ICommandHandler.
Not present. Pre-existing at boundary. Action: track in telemetry phase.

### W4 - G11 Coverage: 21 new src files below 80%

Coverage measured on unit+API test projects (integration tests excluded from dotnet-coverage run).
New files added after boundary 968eefb with line-rate < 0.80:

| File | Est. Coverage | Required | Note |
|---|---|---|---|
| Domain/ValueObjects/JanelaVigencia.cs | ~65% | 80% | Exercised by T-4 (CNPJ fix needed) |
| Domain/ValueObjects/LimiteExposicao.cs | ~60% | 80% | Exercised by T-4 (CNPJ fix needed) |
| Application/Fundos/Commands/AssociateFundoCedenteCommandHandler.cs | ~0% | 80% | T-4 blocked by B2 |
| Application/Fundos/Commands/AssociateCedenteTipoAtivoCommandHandler.cs | ~0% | 80% | T-4 blocked by B2 |
| Application/Fundos/Commands/AssociateFundoTipoAtivoCommandHandler.cs | ~0% | 80% | T-4 blocked by B2 |
| Application/Fundos/Queries/GetFundoCedentesQueryHandler.cs | ~0% | 80% | T-4 blocked by B2 |
| Application/Fundos/Queries/GetFundoTiposAtivoQueryHandler.cs | ~0% | 80% | T-4 blocked by B2 |
| Application/Fundos/Queries/GetCedenteTiposAtivoQueryHandler.cs | ~0% | 80% | T-4 blocked by B2 |
| Application/Fundos/Commands/TransitionAssociationStatusCommandHandler.cs | ~0% | 80% | T-4 blocked by B2 |
| Domain/Aggregates/FundoCedenteAggregate.cs | ~10% | 80% | T-4 blocked by B2 |
| Domain/Aggregates/CedenteTipoAtivoAggregate.cs | ~10% | 80% | T-4 blocked by B2 |
| Domain/Aggregates/FundoTipoAtivoAggregate.cs | ~10% | 80% | T-4 blocked by B2 |
| Application/Fundos/Queries/SearchConsultoriaFundoQueryHandler.cs | ~0% | 80% | B3 LINQ untranslatable |
| Application/Fundos/Queries/SearchCustodianteQueryHandler.cs | ~0% | 80% | B3 LINQ untranslatable |
| API/Controllers/FundosController.cs | ~45% | 80% | TransitionFundoStatus untested (B1) |
| Application/Fundos/Commands/TransitionFundoStatusCommandHandler.cs | ~30% | 80% | B1 cross-tenant test fails |
| Infrastructure/Repositories/ConsultoriaFundoRepository.cs | ~20% | 80% | B3 search untranslatable |
| Infrastructure/Repositories/FundoCedenteRepository.cs | ~5% | 80% | T-4 blocked by B2 |
| Infrastructure/Repositories/CedenteTipoAtivoRepository.cs | ~5% | 80% | T-4 blocked by B2 |
| Infrastructure/Repositories/FundoTipoAtivoRepository.cs | ~5% | 80% | T-4 blocked by B2 |
| Infrastructure/Repositories/FundoRepository.cs | ~55% | 80% | Cross-tenant path untested (B1) |

ALL 21 gaps are direct consequences of B1/B2/B3. Fixing blockers recovers all coverage.

### W5 - G12 Playwright: UAT runner pre-existing failure

File: tests/run-uat.mjs
UAT runner targets /api/registration (legacy). Current route: /api/companies/registration.
Pre-existing failure confirmed via git log. Not introduced by phase 52.
All 5 MCP Playwright scenarios verified manually:
  - GET /api/health -> 200
  - GET /api/employees (no token) -> 401
  - GET /api/employees (malformed JWT) -> 401
  - Jaeger UI localhost:16686 -> 200, services [onboarding-api, onboarding-client] visible
  - OTel trace FundosController.GetAll span confirmed with 3 child spans


---

## Test Summary

| Suite | Total | Pass | Fail | Blocker |
|---|---|---|---|---|
| Pre-existing integration (T-1 refactored) | 61 | 61 | 0 | - |
| T-2 Fundo/ConsultoriaFundo/Custodiante | 48 | 46 | 2 | B3 LINQ search |
| T-3 Cedente PF+PJ / TipoAtivo | 34 | 34 | 0 | - |
| T-4 N-N associations | 42 | 0 | 42 | B2 invalid CNPJ seeds |
| Security cross-tenant | 1 | 0 | 1 | B1 missing tenant guard |
| **TOTAL** | **186** | **141** | **45** | B1+B2+B3 |

---

## Regression captures

- API liveness: GET http://localhost:5000/api/health -> 200 OK (MCP verified)
- Auth enforcement: GET http://localhost:5000/api/employees (no token) -> 401 (MCP verified)
- Invalid token: GET http://localhost:5000/api/employees (malformed JWT) -> 401 (MCP verified)
- Jaeger UI: GET http://localhost:16686 -> 200, services onboarding-api + onboarding-client visible
- OTel trace: FundosController.GetAll span confirmed, 3 child spans (span_id present)
- Console errors: 0 browser errors during MCP regression run
- Screenshots: .jdi/cache/phase-52-jaeger-trace.png (T-8 deliverable)
- HAR: .jdi/cache/phase-52-backend-network.json (MCP network log)
- UAT runner: pre-existing failure (/api/registration route mismatch, not phase 52)

---

## DoD G0 Checklist

| Item | Status | Evidence |
|---|---|---|
| Integration tests LIVE (not unit-only) | PASS | Testcontainers PostgreSQL via PostgreSqlFixture; real DB queries |
| State-machine test exists | FAIL | StateMachine_CrossTenantTransition_Returns404 FAILS (B1) |
| REL-09 test exists | FAIL | T-4 CNPJ seed failure blocks all association tests (B2) |
| Audit log test exists | PASS | FundoCrudIntegrationTests.StatusTransition_Creates_AuditLog PASS |
| Multi-tenant cross-probe test exists | FAIL | Cross-tenant test returns 200 not 404 - SECURITY LEAK (B1) |
| OTel init: collector receiving spans | PASS | Jaeger UI confirms onboarding-api traces live |
| PII scrub verified | PASS | otel-collector-config.yaml attributes/drop_pii_keys + redaction/pii_values active |
| Network traceparent via MCP | PASS | W3C traceparent header confirmed in MCP browser_network_request |

---

## Required fixes before re-verify

1. **B1 SECURITY - FIX FIRST**: Add ClienteId tenant guard in FundosController.TransitionFundoStatus (~line 760).
   Mirror existing guard from GetFundoById line 679:
   if (fundo.ClienteId != _currentCompanyService.CompanyId) return NotFound();

2. **B2**: Fix invalid CNPJ seeds in all 3 T-4 test files:
   - FundoCedenteAssociationIntegrationTests.cs:116 -> replace 44222999000144 with 11222333000181
   - CedenteTipoAtivoAssociationIntegrationTests.cs:107 -> replace 66444222000166 with 22333444000155
   - FundoTipoAtivoAssociationIntegrationTests.cs:110 -> replace 88666444000188 with 33444555000128
   Validate via Cnpj.IsValid() before committing.

3. **B3**: Fix EF Core LINQ in search repositories:
   - ConsultoriaFundoRepository.cs:66: c.Cnpj.Value.Contains(digitsOnly) -> EF.Functions.ILike(c.Cnpj.Value, "%" + digitsOnly + "%")
   - CustodianteRepository.cs: same fix on analogous search predicate block

4. **B4**: Run dotnet format Onboarding.slnx and commit. Fixes 14 whitespace violations in 6 files.

After B1-B4 fixed: re-run dotnet test (target 186/186 pass) then re-run /jdi-verify.
G11 coverage gaps self-resolve when T-4 tests execute (all 21 gaps are downstream of B1-B3).

---

## Reviewer: jdi-reviewer-onboarding-keycloak-frontend-vinext

**Verdict:** BLOCKED

---

### Gates

- [G1 Security frontend] PASS -- no token storage violations, no dangerouslySetInnerHTML, no secrets
- [G2 Telemetry (OTel JS + W3C)] BLOCKED -- BFE-2: root at wrong path; BFE-3: web-vitals.ts missing; BFE-4: FetchInstrumentation literal absent
- [G3 Perf + bundle] PASS -- client 210.06 KB gz, backoffice 205.75 KB gz (gate 300 KB)
- [G4 Build] PASS -- both SPAs build clean
- [G5 Typecheck+Lint] PASS -- tsc 0 errors both SPAs; eslint 0 errors both SPAs
- [G6 Code-design] WARN -- WFE-1: init after render; WFE-2: double-import; WFE-3: pt-BR pre-existing
- [G7 Coverage] BLOCKED -- BFE-1: admin-telemetry.ts stmts 50%, branch 58.62%, funcs 37.5%, lines 52.77% (gate 80%)
- [G8 Playwright client] PASS -- home, Keycloak ACF+PKCE, profile, fundos; no 5xx; no console errors
- [G9 Playwright backoffice] PASS -- custom theme, companies, fundos admin; no cross-app refs
- [G10 Accessibility] ADVISORY -- pre-existing contrast on Keycloak theme; no keyboard traps
- [G11 Vinext debt] PASS -- no new Vinxi imports after boundary 968eefb

---

### Blockers

#### BFE-1 -- G7: admin-telemetry.ts coverage fails 80% gate

File: frontend/backoffice/src/lib/admin-telemetry.ts

Measured (vitest --coverage):
  statements: 50% (gate 80%)
  branches: 58.62% (gate 80%)
  functions: 37.5% (gate 80%)
  lines: 52.77% (gate 80%)

Root cause: admin-telemetry.test.ts uses vi.resetModules() in beforeEach + await import() per test. v8 instruments only the first module evaluation. Lines 39-146 and 174-177 appear uncovered despite 13 tests passing.

Fix: Remove vi.resetModules() from beforeEach. Use vi.stubGlobal / vi.spyOn. Test side effects without full module re-evaluation.

#### BFE-2 -- G2: Telemetry composition root at wrong path (both SPAs)

Gate G2 requires frontend/client/src/lib/telemetry/index.ts and frontend/backoffice/src/lib/telemetry/index.ts.
Actual: client src/lib/telemetry.ts (flat); backoffice src/lib/admin-telemetry.ts (flat, different name).
G2 script tests Test-Path on directory src/lib/telemetry -- both fail. BLOCKED before other G2 checks.

Fix: Move client telemetry.ts -> src/lib/telemetry/index.ts and backoffice admin-telemetry.ts -> src/lib/telemetry/index.ts. Update all import sites and vitest.config.ts.

#### BFE-3 -- G2: src/lib/telemetry/web-vitals.ts missing both SPAs

Gate G2.7 requires src/lib/telemetry/web-vitals.ts in both SPAs. Neither exists.

Fix: Create frontend/client/src/lib/telemetry/web-vitals.ts and frontend/backoffice/src/lib/telemetry/web-vitals.ts. Import onCLS/onFID/onLCP/onFCP/onTTFB from web-vitals; record via OTel Metrics.

#### BFE-4 -- G2: FetchInstrumentation literal absent from client telemetry.ts

Gate G2.1 requires literal string FetchInstrumentation in src/lib/telemetry/index.ts for client SPA. Client uses getWebAutoInstrumentations() which wraps it internally but literal is absent. Gate grep fails.

Fix: After BFE-2, add explicit FetchInstrumentation import or inline comment with the literal and justification.

---

### Warnings

#### WFE-1 -- G6: Backoffice initAdminTelemetry fires after React render

File: frontend/backoffice/src/main.tsx

initAdminTelemetry called via dynamic import AFTER createRoot().render(). React renders before OTel SDK initializes. First interaction/navigation spans lost.

Fix: Static import + sync init before createRoot (mirror client main.tsx), or wrap createRoot inside dynamic import .then().

#### WFE-2 -- G6: Double-import of admin-telemetry.ts in backoffice main.tsx

File: frontend/backoffice/src/main.tsx

generateAnonymousSessionId imported statically; initAdminTelemetry dynamically. Vite warns: dynamically imported but also statically imported -- will not move to another chunk. Code-split defeated.

Fix: Move generateAnonymousSessionId into .then(). Remove static import of admin-telemetry.ts.

#### WFE-3 -- G6: pt-BR string in client main.tsx (pre-existing)

frontend/client/src/main.tsx -- pre-existing throw with pt-BR message. Not introduced by phase 52. Track for i18n phase.

#### WFE-4 -- G2 WARN: VITE_OTEL_ENABLED not set in compose.yaml frontend service

otel-trace.spec.ts positive traceparent test skips when window.__otelRegistered__ !== true. VITE_OTEL_ENABLED absent from compose. OTel never initializes in compose. Live traceparent evidence unconfirmed.

---

### Coverage gaps (new files after boundary 968eefb)

| File | Stmts | Branch | Funcs | Lines | Gate |
|---|---|---|---|---|---|
| frontend/client/src/lib/telemetry.ts | 96.77% | PASS | PASS | PASS | PASS |
| frontend/backoffice/src/lib/admin-telemetry.ts | 50% | 58.62% | 37.5% | 52.77% | BLOCKED |

---

### DoD G0 OTel JS Telemetry checklist

| Item | Status | Evidence |
|---|---|---|
| @opentelemetry/sdk-trace-web installed both SPAs | PASS | package.json both SPAs |
| OTel init before ReactDOM.render (client) | PASS | initTelemetry().catch() before createRoot |
| OTel init before ReactDOM.render (backoffice) | FAIL (WFE-1) | dynamic import resolves AFTER render |
| W3C propagator only | PASS | W3CTraceContextPropagator both files |
| propagateTraceHeaderCorsUrls excludes Keycloak | PASS | both SPAs exclude localhost:8180 |
| Bundle <= 300 KB gz | PASS | 210 KB / 205 KB |
| No console.* in production | PASS | eslint 0 errors |
| Composition root at src/lib/telemetry/index.ts | FAIL (BFE-2) | wrong path both SPAs |
| web-vitals.ts adapter | FAIL (BFE-3) | missing both SPAs |
| FetchInstrumentation wired (client) | FAIL (BFE-4) | literal absent |

---

### Evidencia obrigatoria (CONTEXT.md DoD)

- traceparent on /api/* (VITE_OTEL_ENABLED=true): NOT CAPTURED -- positive test skips in compose (WFE-4)
- no traceparent on /realms/*: CONFIRMED via negative test + MCP browser_network_requests
- Jaeger UI backend spans: CONFIRMED -- .jdi/cache/phase-52-jaeger-ui.png
- Frontend spans to collector: UNCONFIRMED -- OTel not enabled in running stack

---

### Regression captures

- Client: .jdi/cache/phase-52-client-home.png, phase-52-client-keycloak-login.png, phase-52-client-profile.png
- Backoffice: .jdi/cache/phase-52-backoffice-login-page.png, phase-52-backoffice-companies.png, phase-52-backoffice-fundos.png
- Jaeger: .jdi/cache/phase-52-jaeger-ui.png
- HAR: .jdi/cache/phase-52-client-har.json, phase-52-backoffice-har.json
- Console errors client: 0 | backoffice: 0

---

### Required fixes before re-verify

1. BFE-1: Rewrite admin-telemetry.test.ts -- eliminate vi.resetModules() + dynamic import. Use vi.spyOn / vi.stubGlobal. Target statements >=80%.
2. BFE-2: Move client src/lib/telemetry.ts -> src/lib/telemetry/index.ts. Move backoffice src/lib/admin-telemetry.ts -> src/lib/telemetry/index.ts. Update all import sites.
3. BFE-3: Create src/lib/telemetry/web-vitals.ts both SPAs. Wire Web Vitals into OTel Metrics.
4. BFE-4: Add FetchInstrumentation literal to client src/lib/telemetry/index.ts.

After all 4 fixes: re-run pnpm test --coverage both SPAs, verify >=80% perFile, then re-run /jdi-verify.

---

## Reviewer: jdi-reviewer-onboarding-keycloak-security

**Verdict:** BLOCKED

---

### Gates

- [G1 Multi-tenant filter] FAIL -- BLOCKER (confirmed independently; mirrors B1 from backend reviewer)
- [G2 Permission policy coverage] PASS -- all FundosController endpoints have [Authorize(Policy)]; FundRead/FundWrite/FundDelete/FundManage registered in Program.cs and mapped to Permissions constants
- [G3 Secrets + env hygiene] PASS with legacy WARNING -- no new secrets in phase diff; dev credentials pre-exist at D-2 boundary
- [G4 Semgrep] PASS -- 0 ERROR findings, 0 WARNING findings; 5 rules, 799 files, exit 0
- [G5 Trivy FS + container] ADVISORY -- volume mount fails on Windows host; Dockerfile unchanged; new images use :latest tag (no pinning)
- [G6 Keycloak hardening drift] PASS -- bruteForceProtected=true, failureFactor=5, ssoSessionIdleTimeout=1800 both realms; phase adds post.logout.redirect.uris + no-wildcard-redirects policy (improves posture)
- [G7 Security headers + CSP] PARTIAL -- CORS exact origins verified in code (no wildcard); live header check not performed
- [G8 Dependabot] NOT RUN -- gh CLI not available
- [G9 Audit log] PASS -- all new mutation commands capture ActorSub + ActorEmail

---

### Blockers

#### SEC-B1 -- G1: Cross-tenant Fundo status mutation (carries forward backend B1)

Files:
- src/Onboarding.API/Controllers/FundosController.cs:751-789
- src/Onboarding.Application/Fundos/Commands/TransitionFundoStatusCommandHandler.cs:36
- src/Onboarding.Infrastructure/Repositories/FundoRepository.cs:34-36

POST /api/fundos/{id}/status dispatches directly to handler with no ClienteId guard.
FundoRepository.GetByIdAsync uses IgnoreQueryFilters(). PJ-B can mutate Fundo owned by PJ-A.
Guard exists on GetFundoById (line 681) and all association status endpoints.
TransitionFundoStatus is the sole missing guard on a state-mutation endpoint.

Fix:
  var fundo = await _fundoRepository.GetByIdAsync(id, ct);
  if (fundo is null || fundo.ClienteId != _currentCompanyService.CompanyId)
      return NotFound();

---

### Warnings

#### SEC-W1 -- G5: OTel + Jaeger images use :latest tag (no version pinning)

compose.yaml:161 -- otel/opentelemetry-collector-contrib:latest
compose.yaml:187 -- jaegertracing/all-in-one:latest

Pin to version tags (e.g. otel/opentelemetry-collector-contrib:0.120.0). Dev-only images; low blast radius.

#### SEC-W2 -- G3 legacy: Dev secrets in keycloak realm exports (pre-existing at D-2 boundary)

keycloak/client-realm.json + backoffice-realm.json: placeholder client secrets committed.
src/Onboarding.API/appsettings.json: AdminClientSecret hardcoded.
All pre-exist at boundary, not introduced in phase 52. compose.yaml uses ${env} overrides.

#### SEC-W3 -- G3: E2E test credentials hardcoded in new otel-trace.spec.ts files (T-8)

frontend/client/playwright/specs/otel-trace.spec.ts:33 -- "E2EClient@123!" (new file, T-8)
frontend/backoffice/playwright/specs/otel-trace.spec.ts:38 -- "E2EAdmin@123!" (new file, T-8)

Same pattern as pre-existing E2E specs. Not production secrets. Use process.env.E2E_CLIENT_PASSWORD for consistency.

#### SEC-W4 -- G5: Trivy could not be run (Windows Docker volume mount issue)

Trivy Docker volume mount fails on Windows/Git Bash (path conversion). Dockerfile unchanged in phase.
Formal Trivy scan required in CI before ship.

#### SEC-W5 -- G7: VITE_OTEL_ENABLED absent from compose.yaml (carry-forward WFE-4)

OTel browser instrumentation never activates in docker compose up stack.
Positive traceparent test in otel-trace.spec.ts always skips.
DoD G0 live traceparent evidence not captured. Security negative assertion (no Keycloak traceparent) passes unconditionally.

#### SEC-W6 -- OTel collector: db.statement not in key-drop list

infra/otel-collector-config.yaml -- db.statement (SQL queries from npgsql/EF Core spans) not key-dropped.
Partially covered by value-redaction regex (CPF/CNPJ numerics, email). Add to key-drop list for full coverage.

---

### Phase-specific focus areas

#### OTel collector PII scrub effectiveness (T-5)

- Key-drop covers email, cpf, cnpj, sub, token variants, authorization, set-cookie -- COMPREHENSIVE for known auth PII keys.
- Value-redaction covers email/CPF/CNPJ/Bearer regex -- COVERS primary PII value paths.
- Client telemetry.ts: applyFetchSpanAttributes strips query params (path-only http.target) -- GOOD client-side pre-scrub.
- Gap (SEC-W6): db.statement not key-dropped.
- Collector CORS restricted to localhost:5173, localhost:5174 -- CORRECT.

#### CORS allowlist exact origins

Program.cs: WithOrigins("http://localhost:5173","http://localhost:5174") -- exact, no wildcard. PASS.
OTel collector CORS: same two origins. PASS.

#### traceparent propagator W3C only (Priority 1 / D-35)

Client SPA frontend/client/src/lib/telemetry.ts:171 -- W3CTraceContextPropagator only. PASS.
Backoffice SPA frontend/backoffice/src/lib/admin-telemetry.ts:125-126 -- W3CTraceContextPropagator only. PASS.
@opentelemetry/propagator-b3 NOT installed in either SPA. PASS.

#### Keycloak /realms/* excluded from traceparent (D-12 regression)

Client telemetry: ignoreUrls includes /realms/, /keycloak/, /auth/, .well-known. PASS.
Backoffice telemetry: ALLOWED_BACKEND_URLS restricted to /api/admin/; same ignoreUrls. PASS.
E2E negative assertion (no traceparent on Keycloak) unconditional. PASS.

#### DoD G0 Collector section (CONTEXT.md)

| Item | Status | Evidence |
|---|---|---|
| docker-compose adds otel-collector service | PASS | compose.yaml:160-179 |
| infra/otel-collector-config.yaml with attributes+redact+memory_limiter | PASS | file reviewed |
| PII scrub: span drops email/cpf/cnpj | PASS config-level | processors verified; live test requires running collector |
| W3C propagation browser->backend via traceparent | PARTIAL (SEC-W5) | VITE_OTEL_ENABLED absent; positive test skips |

---

### Pipeline artifacts

- Semgrep: .jdi/cache/phase-52-security-semgrep.json (0 findings, exit 0)
- Trivy FS: .jdi/cache/phase-52-security-trivy-fs.json (tool unavailable on Windows -- CI required)
- Gitleaks: .jdi/cache/phase-52-security-gitleaks.json (manual scan -- gitleaks not installed)

---

## Reviewer: jdi-reviewer-onboarding-keycloak-backend-csharp (iter 2)

**Verdict:** BLOCKED

---

### Gates

- [G1 Multi-tenant isolation] PASS -- B1/SEC-B1 tenant guard applied in TransitionFundoStatus; ClienteId check mirrors GetFundoById. IgnoreQueryFilters usage correct with Admin* prefix on admin queries.
- [G2 Endpoint AuthZ + audit] PASS -- all new endpoints carry Authorize(Policy=Fund*); actor (sub+email) captured in all mutation commands.
- [G3 Secret + raw SQL] PASS -- no new secrets; no FromSqlRaw interpolation or concatenation.
- [G4 Telemetry (OTel+Serilog+W3C)] WARN (pre-existing carry-forward) -- W1/W2/W3 from iter 1 unchanged; no new regressions.
- [G5 Performance hygiene] PASS -- no unbounded list endpoints; AsNoTracking on all read repo methods.
- [G6 Index coverage] PASS -- no new migration in phase 52 diff.
- [G7 Build] PASS -- 0 errors, 0 warnings (dotnet build clean).
- [G8 Lint] PASS -- dotnet format --verify-no-changes exits 0 (B4 fix confirmed).
- [G9 DDD/Design] PASS -- no anemic aggregates, no public setters on new domain code.
- [G10 Tests] FAIL -- BLOCKER: 36 tests still failing (32 integration + 4 API unit).
- [G11 Coverage] FAIL -- cascades from G10; 21 new files still below 80%.
- [G12 Playwright regression] PASS -- auth gates confirmed via MCP; API liveness confirmed.
- [G13 Static scans] Not run (advisory).

---

### Blockers

#### B1-iter2 -- G10: B1 fix incomplete -- unit tests for TransitionFundoStatus now fail (4 tests)

Files:
- tests/Onboarding.API.Tests/Controllers/FundosControllerTests.cs:1027, 1048, 1071, 1087

Root cause: The B1 security fix correctly added _fundoRepository.GetByIdAsync(id, ct) at the start of TransitionFundoStatus to enforce the tenant boundary before dispatching. However, the 4 unit tests do NOT stub _fundoRepo.GetByIdAsync(FundoId, ...). NSubstitute returns null by default. The controller then returns NotFound() before dispatching, causing all 4 tests to fail.

Evidence:
  TransitionFundoStatus_RascunhoToAtivo_ValidTransition_Returns200: expected OkObjectResult, got NotFoundResult
  TransitionFundoStatus_EncerradoToAtivo_InvalidTransition_Returns400: expected BadRequestObjectResult, got NotFoundResult
  TransitionFundoStatus_FundoNotFound_Returns404: expected NotFoundObjectResult, got NotFoundResult (controller returns bare NotFound() with no body)
  TransitionFundoStatus_CapturesActorFromJwt: handler Received(0) -- NotFound returned before dispatch

Fix:
  Tests at lines 1027, 1048, 1087: add before action call:
    _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(BuildFundo());
  Test at line 1071 (NotFound case): leave mock returning null (NSubstitute default). Fix assertion at line 1083:
    result.ShouldBeOfType<NotFoundResult>()  -- controller uses bare return NotFound() (no body).

#### B2-iter2 -- G10: B2 fix incomplete -- CPF seeds for Cedente still invalid (30 T-4 tests)

Files:
- tests/Onboarding.Integration.Tests/Fundos/FundoCedenteAssociationIntegrationTests.cs:144, 148
- tests/Onboarding.Integration.Tests/Fundos/CedenteTipoAtivoAssociationIntegrationTests.cs:131

Root cause: B2 fixed the 6 Company CNPJ seeds. It did NOT fix 3 CPF seeds used in Cedente.RegisterPf calls:
  - FCA line 144: Cedente.RegisterPf("74971027018") -- CPF check-digit invalid
  - FCA line 148: Cedente.RegisterPf("54896705091") -- CPF check-digit invalid
  - CTA line 131: Cedente.RegisterPf("59978867083") -- CPF check-digit invalid
All 3 throw ArgumentException in InitializeAsync(), aborting all 30 T-4 tests before executing.

Fix: Replace all 3 invalid CPF literals. Use GenerateCpf(9001), GenerateCpf(9002), GenerateCpf(9003) (counters outside existing range 1000-3000+). Validate via Cpf.IsValid() in src/Onboarding.Domain/ValueObjects/Cpf.cs before committing.

#### B3-iter2 -- G10: B3 fix incomplete -- c.Cnpj.Value still untranslatable by EF Core 10 (2 search tests)

Files:
- src/Onboarding.Infrastructure/Repositories/ConsultoriaFundoRepository.cs:63
- src/Onboarding.Infrastructure/Repositories/CustodianteRepository.cs:62

Root cause: B3 replaced Contains() with ILike() but left c.Cnpj.Value as the expression argument. EF Core 10 cannot translate ValueObject.Property navigation in LINQ-to-SQL regardless of the operator. HasConversion does not propagate through property member access in expression trees.

Evidence (runtime log):
  InvalidOperationException: The LINQ expression
    ILike(matchExpression: c.Cnpj.Value, pattern: @p) could not be translated.
  HTTP 500 on GET /api/fundos/consultorias?search=term and GET /api/fundos/custodiantes?search=term
  Both tests confirm: ListConsultorias_SearchByName_FiltersResults and ListCustodiantes_SearchByName_FiltersResults still fail with InternalServerError.

Fix: Replace c.Cnpj.Value with EF.Property<string>(c, "cnpj") in both files:
  ConsultoriaFundoRepository.cs:63:
    (digitsOnly.Length > 0 && EF.Functions.ILike(EF.Property<string>(c, "cnpj"), "%" + digitsOnly + "%"))
  CustodianteRepository.cs:62:
    (digitsOnly.Length > 0 && EF.Functions.ILike(EF.Property<string>(c, "cnpj"), "%" + digitsOnly + "%"))
  Column name "cnpj" confirmed via HasColumnName("cnpj") in CustodianteConfiguration.cs:50 and ConsultoriaFundoConfiguration.cs.

---

### Warnings (carry-forward from iter 1)

- W1 -- G4 Telemetry: PII scrubber class name (SensitiveDataDestructuringPolicy vs PiiScrubber) -- pre-existing, telemetry phase action.
- W2 -- G4 Telemetry: TenantBaggageMiddleware not wired -- pre-existing.
- W3 -- G4 Telemetry: TelemetryCommandHandlerDecorator not registered -- pre-existing.
- W4 -- G12 Playwright: run-uat.mjs targets /api/registration (legacy route) -- pre-existing.
- SEC-W1 -- OTel/Jaeger images use :latest tag -- dev-only, low blast radius.

---

### Test Summary (iter 2)

| Suite | Total | Pass | Fail | Root cause |
|---|---|---|---|---|
| Domain.Tests | 478 | 478 | 0 | clean |
| Application.Tests | 150 | 150 | 0 | clean |
| API.Tests | 382 | 374 | 4 | B1-iter2: fundoRepo mock not stubbed in TransitionFundoStatus unit tests |
| Integration.Tests | 187 | 155 | 32 | B2-iter2 (CPF seeds, 30 tests) + B3-iter2 (EF.Property, 2 tests) |
| **TOTAL** | **1197** | **1157** | **36** | 3 blockers remain |

API.Tests ignored: 4 (pre-existing).

---

### Regression captures (iter 2)

- GET /healthz/live -> 200 OK (MCP verified)
- GET /api/fundos (no token) -> 403 (MCP verified)
- GET /api/fundos (bad token) -> 401 (MCP verified)
- GET /api/fundos/consultorias (no token) -> 403 (MCP verified)
- GET /api/fundos/consultorias (bad token) -> 401 (MCP verified)
- GET /api/fundos/custodiantes (no token) -> 403 (MCP verified)
- GET /api/fundos/tipos-ativo (no token) -> 403 (MCP verified)
- GET /api/fundos/cedentes (no token) -> 403 (MCP verified)
- POST /api/fundos/{id}/status (no token) -> 403 (MCP verified -- B1 guard active, endpoint registered)
- POST /api/fundos/{id}/status (bad token) -> 401 (MCP verified)
- GET /api/fundos/{id}/cedentes (bad token) -> 401 (MCP verified)
- Jaeger UI: http://localhost:16686 -> 200, services [onboarding-api, onboarding-client] confirmed
- Network HAR: .jdi/cache/phase-52-backend-iter2-network.json
- Screenshot: .jdi/cache/phase-52-backend-iter2-api-health.png
- Console errors: 0 during regression run

---

### Coverage gaps (new files -- still blocked)

Same 21 files as iter 1. All gaps downstream of B1-iter2/B2-iter2/B3-iter2.
When all 3 blockers are resolved, T-4 (30 tests) and search (2 tests) will execute, recovering coverage above 80% on all 21 files.

---

### Required fixes before re-verify (iter 3)

1. **B1-iter2**: Update 4 TransitionFundoStatus unit tests to stub _fundoRepo.GetByIdAsync:
   - Lines 1027, 1048, 1087: add _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(BuildFundo()) before calling the action.
   - Line 1071 (NotFound case): leave mock returning null. Fix assertion line 1083: ShouldBeOfType<NotFoundResult>() not ShouldBeOfType<NotFoundObjectResult>().

2. **B2-iter2**: Replace 3 invalid CPF seeds in T-4 InitializeAsync:
   - FundoCedenteAssociationIntegrationTests.cs:144 -- replace "74971027018"
   - FundoCedenteAssociationIntegrationTests.cs:148 -- replace "54896705091"
   - CedenteTipoAtivoAssociationIntegrationTests.cs:131 -- replace "59978867083"
   Use GenerateCpf(9001), GenerateCpf(9002), GenerateCpf(9003).

3. **B3-iter2**: Fix EF.Property in search predicates:
   - ConsultoriaFundoRepository.cs:63: c.Cnpj.Value -> EF.Property<string>(c, "cnpj")
   - CustodianteRepository.cs:62: c.Cnpj.Value -> EF.Property<string>(c, "cnpj")

After all 3 fixes: dotnet test Onboarding.slnx targeting 0 failures, then /jdi-verify iter 3.


## Reviewer: jdi-reviewer-onboarding-keycloak-frontend-vinext (iter 2)

**Verdict:** BLOCKED

---

### Gates

- [G1 Security frontend] PASS -- no token storage violations; no dangerouslySetInnerHTML; no secrets in source; no cross-SPA imports (D-4 clean)
- [G2 Telemetry (OTel JS + W3C)] PASS -- BFE-2/3/4 resolved: src/lib/telemetry/ directory exists both SPAs; FetchInstrumentation literal present both SPAs; web-vitals.ts created both SPAs; propagateTraceHeaderCorsUrls allowlist; ignoreUrls covers auth/keycloak/well-known/realms both SPAs; PII_REGEX/scrubAttributes both SPAs; W3C propagator only; no B3/Jaeger; no wildcard allowlist
- [G3 Perf + bundle] PASS -- client 210.06 KB gz, backoffice 205.75 KB gz (gate 300 KB both pass)
- [G4 Build] PASS -- client build clean (5.71s); backoffice build clean (4.78s); 0 errors both SPAs; WFE-2 double-import Vite warning persists in backoffice build (carry-forward)
- [G5 Typecheck+Lint] PASS -- tsc 0 errors client; tsc 0 errors backoffice; eslint max-warnings 0 clean both SPAs
- [G6 Code-design] WARN -- WFE-1/WFE-2/WFE-3 carry-forward; no new violations from iter-2 commits
- [G7 Coverage] BLOCKED -- BFE-5: web-vitals.ts absent from coverage include both SPAs (0% measured); use-admin-list-search.ts absent from backoffice coverage include (0% measured)
- [G8 Playwright client] PASS -- HTTP 200 at :5173; ACF+PKCE redirect confirmed (code_challenge_method=S256 in URL); /fundos renders; /profile renders; auth guard redirects unauthenticated to /auth/login; no 5xx; no application console errors
- [G9 Playwright backoffice] PASS -- HTTP 200 at :5174; /admin/login renders custom Keycloak theme; ACF+PKCE chain confirmed (backoffice realm, S256); auth guard redirects /admin/companies to /admin/login; no 5xx; no CORS errors; no client-SPA code cross-refs
- [G10 Accessibility] ADVISORY -- client button-name critical (TanStack devtools, pre-existing); backoffice landmark/heading moderate (pre-existing login page); no keyboard traps; no new phase-52 violations
- [G11 Vinext debt] PASS -- no new Vinxi imports in telemetry files either SPA

---

### Blockers

#### BFE-5 -- G7: web-vitals.ts (both SPAs) and use-admin-list-search.ts (backoffice) missing from coverage include

Three new files added after boundary 968eefb are NOT in the vitest coverage include list, resulting in 0% measured coverage. G7 gate: < 80% on any new file = BLOCKED.

Files:
- frontend/client/src/lib/telemetry/web-vitals.ts (51 lines, new after boundary)
- frontend/backoffice/src/lib/telemetry/web-vitals.ts (51 lines, new after boundary)
- frontend/backoffice/src/lib/use-admin-list-search.ts (41 lines, new after boundary)

Evidence: git diff --name-only --diff-filter=A 968eefb..HEAD confirms all 3 as newly created. Both vitest.config.ts coverage include arrays confirmed absent via grep. No dedicated test files exist for any of the 3.

Root cause: BFE-2 fix updated coverage include for telemetry/index.ts but did not add web-vitals.ts to either SPA config. use-admin-list-search.ts was omitted when the hook was created in the original phase-52 doer work.

Fix client (frontend/client/vitest.config.ts): add src/lib/telemetry/web-vitals.ts to include array.

Fix backoffice (frontend/backoffice/vitest.config.ts): add src/lib/telemetry/web-vitals.ts and src/lib/use-admin-list-search.ts to include array.

Tests required:
- web-vitals.test.ts (both SPAs): mock @opentelemetry/api metrics.getMeter().createHistogram() and web-vitals onCLS/onINP/onLCP/onFCP/onTTFB; import registerWebVitals; call it; assert all 5 subscription functions called once; assert report callback fires histogram.record with correct vital.name and vital.rating.
- use-admin-list-search.test.tsx (backoffice): mock useNavigate from @tanstack/react-router; call useAdminListSearch with currentSearch object and path; invoke setPage/setSearch/setEmpresaId; assert navigate called with expected search params.

---

### Warnings (carry-forward from iter 1)

- WFE-1: initAdminTelemetry fires after React render in backoffice main.tsx (dynamic import resolves post-createRoot). Low severity. Fix: static import before createRoot.
- WFE-2: Double-import of telemetry/index.ts in backoffice main.tsx. Vite build warns: dynamic import will not chunk-split. Fix: consolidate to single import path.
- WFE-3: pt-BR string in client main.tsx (pre-existing, not phase-52).
- WFE-4: VITE_OTEL_ENABLED absent from compose.yaml. OTel inactive in running stack. Positive traceparent test always skips.

---

### Coverage gaps (new files after boundary 968eefb)

| File | Stmts | Branch | Funcs | Lines | Gate |
|---|---|---|---|---|---|
| client/src/lib/telemetry/index.ts | 96.96% | 80.64% | 83.33% | 96.66% | PASS |
| backoffice/src/lib/telemetry/index.ts | 100% | 96.55% | 100% | 100% | PASS (BFE-1 resolved) |
| client/src/lib/telemetry/web-vitals.ts | NOT MEASURED | NOT MEASURED | NOT MEASURED | NOT MEASURED | BLOCKED BFE-5 |
| backoffice/src/lib/telemetry/web-vitals.ts | NOT MEASURED | NOT MEASURED | NOT MEASURED | NOT MEASURED | BLOCKED BFE-5 |
| backoffice/src/lib/use-admin-list-search.ts | NOT MEASURED | NOT MEASURED | NOT MEASURED | NOT MEASURED | BLOCKED BFE-5 |
| All other included new files | All >= 80% | All >= 80% | All >= 80% | All >= 80% | PASS |

Aggregate covered backoffice files: 95.13% stmts / 90.8% branch / 93.6% funcs / 96.25% lines.
Aggregate covered client files: 96.26% stmts / 85.35% branch / 97.17% funcs / 97.09% lines.

---

### Regression captures

- Network HAR: .jdi/cache/phase-52-frontend-iter2-network.json
- Client Keycloak ACF+PKCE: .jdi/cache/phase-52-frontend-iter2-client-keycloak-login.png
- Client Fundos route: .jdi/cache/phase-52-frontend-iter2-client-fundos.png
- Backoffice route guard: .jdi/cache/phase-52-frontend-iter2-backoffice-home.png
- Backoffice Keycloak theme: .jdi/cache/phase-52-frontend-iter2-backoffice-keycloak-login.png
- Console errors client (app-level): 0 (Vite HMR WebSocket + /auth/me 401 are container/auth artefacts)
- Console errors backoffice (app-level): 0 (same artefacts)

---

### DoD G0 OTel JS checklist delta

| Item | Iter 1 | Iter 2 | Evidence |
|---|---|---|---|
| src/lib/telemetry/ composition root both SPAs | FAIL BFE-2 | PASS | Directory + index.ts at correct path both SPAs |
| web-vitals.ts adapter both SPAs | FAIL BFE-3 | PASS (file created) | Both SPAs registerWebVitals() wires onCLS/onINP/onLCP/onFCP/onTTFB |
| FetchInstrumentation literal client | FAIL BFE-4 | PASS | Client: literal in comment + instrumentation-fetch config key; backoffice: explicit import |
| backoffice telemetry coverage >= 80% | FAIL BFE-1 | PASS | 100% stmts / 96.55% branch / 100% funcs / 100% lines |
| web-vitals.ts coverage >= 80% both SPAs | N/A | FAIL BFE-5 | Not in vitest include either SPA |
| use-admin-list-search.ts coverage >= 80% | N/A | FAIL BFE-5 | Not in backoffice vitest include |

---

### Required fixes before re-verify (iter 3)

1. BFE-5a: Add src/lib/telemetry/web-vitals.ts to frontend/client/vitest.config.ts coverage include. Create frontend/client/src/tests/lib/web-vitals.test.ts.

2. BFE-5b: Add src/lib/telemetry/web-vitals.ts to frontend/backoffice/vitest.config.ts coverage include. Create frontend/backoffice/src/tests/lib/web-vitals.test.ts.

3. BFE-5c: Add src/lib/use-admin-list-search.ts to frontend/backoffice/vitest.config.ts coverage include. Create frontend/backoffice/src/tests/lib/use-admin-list-search.test.tsx.

After all 3 fixes: run pnpm test -- --coverage both SPAs; confirm >= 80% on all 3 new files; 0 new test failures. Then /jdi-verify iter 3.

## Reviewer: jdi-reviewer-onboarding-keycloak-security (iter 2)

**Verdict:** APPROVED_WITH_WARNINGS

---

### Gates

- [G1 Multi-tenant filter] PASS -- SEC-B1 resolved. Tenant guard at FundosController.cs:760. Fix: load fundo via _fundoRepository.GetByIdAsync; return NotFound() if null OR fundo.ClienteId != _currentCompanyService.CompanyId. Mirrors GetFundoById:679 and all association controllers. HasQueryFilter confirmed on all 4 company-scoped aggregates. Junction aggregates documented as NO HasQueryFilter -- tenant scoping via parent. No EF configurations changed in iter 2. G1 CLEAN.
- [G2 Permission policy coverage] PASS -- All changed controllers carry [Authorize] with explicit Policy. FundosController: BearerClient + FundRead/FundWrite/FundDelete/FundManage per endpoint. FundoCedentesController, FundoTiposAtivosController, CedenteTiposAtivosController: BearerClient class-level + FundRead/FundWrite per method. AdminFundosController: BearerBackoffice + CrossCompanyAccess class-level (11 admin GET endpoints). AdminUserController: same. Fund policies registered Program.cs:215-222. CrossCompanyAccess at Program.cs:225. No unprotected public HTTP endpoints.
- [G3 Secrets + env hygiene] PASS with carry-forward WARNINGs -- No new production secrets in iter 2 diff. appsettings.json unchanged (AdminClientSecret pre-existing at D-2 boundary). compose.yaml diff: zero new secret literals. New E2E spec files carry hardcoded test passwords matching pre-existing pattern (SEC-W3). Gitleaks binary unavailable; manual diff scan: 0 new production secrets.
- [G4 Semgrep] PASS -- 0 ERROR findings, 0 WARNING findings. 5 rules, 801 files scanned, exit 0. PartialParsing notice on IQuery.cs is a parse warning not a security finding. Artifacts: .jdi/cache/phase-52-security-iter2-semgrep.json.
- [G5 Trivy FS + container] ADVISORY -- trivy binary not installed on host (same as iter 1). Dockerfile unchanged in phase 52 iter 2. No new container images. Formal Trivy scan required in CI before ship. Carry-forward SEC-W4.
- [G6 Keycloak hardening drift] PASS with legacy WARNINGs -- client-realm.json: bruteForceProtected=true, failureFactor=5, ssoSessionIdleTimeout=1800. backoffice-realm.json: same. Phase 52 adds post.logout.redirect.uris (exact URIs) and clientProfiles/clientPolicies enforcing secure-redirect-uris-enforcer (allow-wildcard-in-redirect-uri: false, allow-open-redirect: false) -- posture improvement. ROPC on onboarding-app pre-existing at D-2 (SEC-W7). Password policy length(8) pre-existing (SEC-W7b).
- [G7 Security headers + CSP] PASS (code-only) -- CORS Program.cs:258: WithOrigins exact two origins, no wildcard. OTel collector CORS: allowed_origins localhost:5173 + localhost:5174, no wildcard. Live header inspection not performed. Carry-forward advisory from iter 1.
- [G8 Dependabot] NOT RUN -- gh CLI not available on host. No change from iter 1.
- [G9 Audit log] PASS -- All 41 new mutation command files contain ActorSub. Spot-checked: TransitionFundoStatusCommandHandler.cs, TransitionFundoCedenteStatusHandler.cs, TransitionCedenteTipoAtivoStatusHandler.cs, CreateFundoCedenteHandler.cs, CreateCedenteTipoAtivoHandler.cs, UpdateFundoCedenteLimiteHandler.cs, UpdateCedenteTipoAtivoLimiteHandler.cs. All capture actorSub + actorEmail from JWT claims before dispatch.

---

### SEC-B1 Status: RESOLVED

Commit 8a8978d -- fix(52-integration-tests-fundos): guard cross-tenant Fundo status mutation (B1/SEC-B1)

The cross-tenant mutation vulnerability is closed.

- FundosController.cs:757-761 loads fundo before dispatch; returns NotFound() if ClienteId does not match actor company.
- Security comment documents intent: return NotFound not Forbid -- entity existence must not be leaked to other tenants.
- Guard structurally identical to GetFundoById:679.
- All 4 state-mutation endpoints now have ClienteId guards; TransitionFundoStatus was the sole gap.
- No new G1 regressions introduced by iter 2 commits.

---

### Blockers

None. SEC-B1 resolved. No new blockers from iter 2 changes.
---

### Warnings

#### SEC-W1 -- G5: OTel + Jaeger images use :latest tag (carry-forward)

compose.yaml -- otel/opentelemetry-collector-contrib:latest and jaegertracing/all-in-one:latest. Dev-only; low blast radius. Pin to semver tags before production.

#### SEC-W2 -- G3 legacy: Dev secrets in Keycloak realm exports (carry-forward)

keycloak/client-realm.json + backoffice-realm.json: placeholder client secrets committed. src/Onboarding.API/appsettings.json:18: AdminClientSecret hardcoded. All pre-exist at D-2 boundary. compose.yaml uses env overrides at runtime.

#### SEC-W3 -- G3: E2E test credentials hardcoded in new Playwright spec files (expanded from iter 1)

New files with hardcoded ADMIN_PASSWORD: admin-fundos-associations.spec.ts:13, admin-fundos-detail.spec.ts:14, admin-fundos-list.spec.ts:18, admin-fundos-permissions.spec.ts:13, admin-auth-flow.spec.ts:28 (frontend/backoffice/playwright/specs/). CLIENT_PASSWORD in frontend/client/playwright/specs/auth-flow.spec.ts:20. Pattern consistent with pre-existing E2E specs. Not production secrets. Recommended: extract to process.env.E2E_ADMIN_PASSWORD and process.env.E2E_CLIENT_PASSWORD.
#### SEC-W4 -- G5: Trivy binary unavailable (carry-forward)

Formal Trivy scan required in CI before ship. Dockerfile unchanged; no new container images.

#### SEC-W5 -- G7: VITE_OTEL_ENABLED absent from compose.yaml (carry-forward)

OTel browser instrumentation inactive in docker compose up. Positive traceparent test skips unconditionally.

#### SEC-W6 -- OTel collector: db.statement not in key-drop list (carry-forward)

infra/otel-collector-config.yaml: db.statement (SQL query text from npgsql spans) not key-dropped. Value-redaction regex provides partial coverage. Add to processor key-drop list for full PII coverage.

#### SEC-W7 -- G6: ROPC enabled on non-legacy client (pre-existing, elevated)

keycloak/client-realm.json: onboarding-app has directAccessGrantsEnabled=true. Pre-existing at D-2 boundary. Not the legacy-backoffice exemption defined in G6 gate. Review for removal -- ACF+PKCE is the active flow (D-feedback). Not blocking (pre-existing), tracked for next auth-hardening phase.

#### SEC-W7b -- G6: Password policy length(8) not length(12) (pre-existing)

Both realms use passwordPolicy length(8). G6 gate requires length(12). Pre-existing at D-2 boundary. Tighten in next Keycloak hardening phase.
---

### Phase-specific confirmations (iter 2)

#### W3C propagator only (D-35)

- client/src/lib/telemetry/index.ts:186 -- W3CTraceContextPropagator only. PASS.
- backoffice/src/lib/telemetry/index.ts:36,141 -- W3CTraceContextPropagator only. PASS.
- @opentelemetry/propagator-b3 absent from both package.json files (zero grep matches). PASS.

#### CORS allowlist exact origins

- API Program.cs:258: WithOrigins(http://localhost:5173, http://localhost:5174) -- exact, no wildcard. PASS.
- OTel collector infra/otel-collector-config.yaml:21-22: allowed_origins localhost:5173 + localhost:5174 -- exact, no wildcard. PASS.

#### Keycloak /realms/* excluded from traceparent (D-12)

- client/src/lib/telemetry/index.ts:217 ignoreUrls confirmed in phase 52 diff. PASS.
- backoffice/src/lib/telemetry/index.ts:158 SUPPRESS_URL_PATTERNS confirmed. PASS.

#### TransitionFundoStatus tenant guard -- completeness across all state-mutation endpoints

All 4 Fundo-module status transition endpoints now carry ClienteId guard before dispatch:
  - POST /api/fundos/{id}/status (FundosController:760) -- ADDED commit 8a8978d (was the sole gap)
  - POST /api/fundos/{fundoId}/cedentes/{id}/status (FundoCedentesController:228) -- pre-existing
  - POST /api/fundos/{fundoId}/tipos-ativos/{id}/status (FundoTiposAtivosController) -- pre-existing
  - POST /api/cedentes/{cedenteId}/tipos-ativos/{id}/status (CedenteTiposAtivosController:228) -- pre-existing

No new G1 gaps introduced by iter 2 commits.

---

### Pipeline artifacts

- Semgrep: .jdi/cache/phase-52-security-iter2-semgrep.json (0 findings, 5 rules, 801 files, exit 0)
- Trivy FS: .jdi/cache/phase-52-security-iter2-trivy-fs.json (tool unavailable -- CI required)
- Gitleaks: .jdi/cache/phase-52-security-iter2-gitleaks.json (binary unavailable; manual diff scan: 0 new production secrets)

## Reviewer: jdi-reviewer-onboarding-keycloak-backend-csharp (iter 3)

**Verdict:** BLOCKED

---

### Gates

- [G1 Multi-tenant isolation] PASS
- [G2 Endpoint AuthZ + audit] PASS
- [G3 Secret + raw SQL] PASS
- [G4 Telemetry] WARN (pre-existing carry-forward)
- [G5 Performance hygiene] PASS
- [G6 Index coverage] PASS
- [G7 Build] PASS (0 errors, 0 warnings)
- [G8 Lint] PASS (dotnet format exits 0)
- [G9 DDD/Design] PASS
- [G10 Tests] FAIL - BLOCKER (2 integration tests still failing - B3-iter3)
- [G11 Coverage] FAIL (3 new files below 80% - tooling gap)
- [G12 Playwright regression] PASS
- [G13 Static scans] Not run (advisory)

---

### Blockers

#### B3-iter3 -- G10: EF.Property uses column name not C# property name (2 tests still failing)

Files:
- src/Onboarding.Infrastructure/Repositories/ConsultoriaFundoRepository.cs:63
- src/Onboarding.Infrastructure/Repositories/CustodianteRepository.cs:62

Root cause: Iter 3 used EF.Property<string>(c, "cnpj") where "cnpj" is the HasColumnName snake_case value. EF.Property requires the C# property name. The runtime exception confirms: "property does not exist on the entity type." Even correcting the casing to "Cnpj" will fail because the C# property is typed as Cnpj (value object), not string. EF.Property<string> cannot bridge the HasConversion gap in EF Core 10 LINQ-to-SQL for ILike expressions.

Evidence: ListConsultorias_SearchByName_FiltersResults and ListCustodiantes_SearchByName_FiltersResults both return HTTP 500. Exception at CountAsync (ConsultoriaFundoRepository.cs:66 and CustodianteRepository.cs analogue).

Fix (shadow property approach):

In ConsultoriaFundoConfiguration.Configure():
  builder.Property<string>("CnpjRaw").HasColumnName("cnpj").HasMaxLength(14).IsRequired();

In ConsultoriaFundoRepository.cs:63:
  (digitsOnly.Length > 0 && EF.Functions.ILike(EF.Property<string>(c, "CnpjRaw"), "%" + digitsOnly + "%"))

Apply identically to CustodianteConfiguration.cs and CustodianteRepository.cs:62.

EF Core allows multiple property mappings to the same column when only one participates in constraints/indexes. The existing Cnpj value-object property retains all uniqueness constraints; CnpjRaw is a read-only shadow property used only for ILike search translation.

#### G11-iter3 -- Coverage tooling gap (3 new files below 80% in available report)

Files: JanelaVigencia.cs (57.89%), LimiteExposicao.cs (45.16%), DuplicateActiveAssociationException.cs (71.42%)

Root cause: Onboarding.Domain.Tests lacks coverlet.msbuild. The dedicated domain unit tests (JanelaVigenciaTests.cs 127 lines, LimiteExposicaoTests.cs 141 lines, LimiteExposicaoPercentualTests.cs 80 lines) all pass but do not produce Cobertura XML. The only available coverage report is from API.Tests (coverlet.msbuild present), which exercises these value objects only through WebApplicationFactory-based handler tests.

Fix: Add coverlet.msbuild to tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj and re-run coverage. All 3 files are expected to reach >= 80% when Domain.Tests contributes.

---

### Progress vs iter 2

| Blocker | Iter 2 | Iter 3 |
|---|---|---|
| B1-iter2: 4 unit tests missing fundoRepo mock stub | FAIL (4 tests) | RESOLVED -- 5 TransitionFundoStatus tests pass |
| B2-iter2: 3 invalid CPF seeds in T-4 InitializeAsync | FAIL (30 tests) | RESOLVED -- 44 T-4 association tests pass (16+14+14) |
| B3-iter2: EF.Property CNPJ search translation | FAIL (2 tests) | STILL FAILING (2 tests) -- column name vs property name mistake |

---

### Test Summary (iter 3)

| Suite | Total | Pass | Fail | Root cause |
|---|---|---|---|---|
| Domain.Tests | 478 | 478 | 0 | clean |
| Application.Tests | 150 | 150 | 0 | clean |
| API.Tests | 382 | 378 | 0 | 4 pre-existing skips |
| Integration.Tests | 187 | 185 | 2 | B3-iter3: EF.Property("cnpj") wrong key |
| TOTAL | 1197 | 1191 | 2 | 1 blocker |

Target: 1197/1197 pass, 0 fail. Current: 1191 pass, 2 fail.

---

### Coverage gaps (new files -- iter 3)

Coverage from API.Tests (coverlet.msbuild). Domain.Tests lacks coverlet.

| File | Coverage | Required | Status | Notes |
|---|---|---|---|---|
| JanelaVigencia.cs | 57.89% (22/38) | 80% | FAIL | Dedicated tests in Domain.Tests; tooling gap |
| LimiteExposicao.cs | 45.16% (28/62) | 80% | FAIL | Dedicated tests in Domain.Tests; tooling gap |
| DuplicateActiveAssociationException.cs | 71.42% (10/14) | 80% | FAIL | Partially covered via aggregate tests |
| FundoCedenteAggregate.cs | 91.04% | 80% | PASS | |
| CedenteTipoAtivoAggregate.cs | 91.04% | 80% | PASS | |
| FundoTipoAtivoAggregate.cs | 91.04% | 80% | PASS | |
| TransitionFundoStatusCommandHandler.cs | 100% | 80% | PASS | B1-iter2 resolved |
| All Create/Transition/Update handlers | >=94% | 80% | PASS | T-4 now executes |
| Infrastructure repos | exempt | -- | -- | [ExcludeFromCodeCoverage] |

---

### Regression captures (iter 3)

- GET http://localhost:8080/healthz/live -> 200 Healthy (MCP verified)
- GET http://localhost:8080/api/fundos (no token) -> 401 (MCP verified)
- GET http://localhost:8080/api/fundos (bad token) -> 401 (MCP verified)
- GET http://localhost:8080/api/fundos/consultorias (no token) -> 401 (MCP verified)
- GET http://localhost:8080/api/fundos/custodiantes (no token) -> 401 (MCP verified)
- GET http://localhost:8080/api/fundos/tipos-ativo (no token) -> 401 (MCP verified)
- POST http://localhost:8080/api/fundos/{id}/status (no token) -> 401 (MCP verified)
- POST http://localhost:8080/api/fundos/{id}/status (bad token) -> 401 (MCP verified)
- Jaeger UI: http://localhost:16686 -> 200 (MCP verified, 6 services)
- Screenshot: .jdi/cache/phase-52-backend-iter3-health.png
- Screenshot: .jdi/cache/phase-52-backend-iter3-jaeger.png
- Console errors: 0

---

### Warnings (carry-forward from iter 2)

- W1 -- G4: PII scrubber class name (SensitiveDataDestructuringPolicy vs PiiScrubber) -- pre-existing.
- W2 -- G4: TenantBaggageMiddleware not wired -- pre-existing.
- W3 -- G4: TelemetryCommandHandlerDecorator not registered -- pre-existing.
- W4 -- G12: run-uat.mjs targets /api/registration (legacy route) -- pre-existing.

---

### Required fixes before re-verify (iter 4)

1. B3-iter3 (BLOCKING -- G10, 2 tests):
   - Add builder.Property<string>("CnpjRaw").HasColumnName("cnpj").HasMaxLength(14).IsRequired() to ConsultoriaFundoConfiguration.cs and CustodianteConfiguration.cs.
   - Replace EF.Property<string>(c, "cnpj") with EF.Property<string>(c, "CnpjRaw") in ConsultoriaFundoRepository.cs:63 and CustodianteRepository.cs:62.
   - Validate: ListConsultorias_SearchByName_FiltersResults and ListCustodiantes_SearchByName_FiltersResults return 200 OK.

2. G11-iter3 (BLOCKING -- 3 files below 80%):
   - Add coverlet.msbuild package to tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj.
   - Re-run dotnet test --collect:"XPlat Code Coverage" and confirm JanelaVigencia, LimiteExposicao, DuplicateActiveAssociationException all reach >= 80%.

After both fixes: dotnet test targeting 0 fail (1197/1197 pass), coverage >= 80% all new files, then /jdi-verify iter 4.

## Reviewer: jdi-reviewer-onboarding-keycloak-frontend-vinext (iter 3)

**Verdict:** APPROVED_WITH_WARNINGS

---

### Gates

- [G1 Security frontend] PASS -- no token storage violations; no dangerouslySetInnerHTML in source; no secrets in bundle; no cross-SPA imports (D-4 clean)
- [G2 Telemetry (OTel JS + W3C)] PASS -- src/lib/telemetry/ directory + index.ts present both SPAs; web-vitals.ts present both SPAs (BFE-3 resolved); FetchInstrumentation literal present both SPAs (BFE-4 resolved); WebTracerProvider, BatchSpanProcessor, OTLPTraceExporter, W3CTraceContextPropagator all present both SPAs; propagateTraceHeaderCorsUrls allowlist present both SPAs; ignoreUrls covers auth/keycloak/well-known both SPAs; PII_REGEX/scrub present both SPAs; no B3/Jaeger propagators; no wildcard allowlist
- [G3 Perf + bundle] PASS -- client 210.06 KB gz; backoffice 205.75 KB gz (gate 300 KB both pass)
- [G4 Build] PASS -- client build clean (5.37s); backoffice build clean (4.48s); 0 errors both SPAs; WFE-2 Vite double-import warning in backoffice persists (carry-forward, pre-existing)
- [G5 Typecheck+Lint] PASS -- tsc --noEmit exit 0 both SPAs; eslint --max-warnings 0 exit 0 both SPAs
- [G6 Code-design] WARN -- WFE-1/WFE-2/WFE-3/WFE-4 carry-forward; no new violations from BFE-5 commits
- [G7 Coverage] PASS -- all 3 BFE-5 files now >=80%; client web-vitals.ts 100% L/F; backoffice web-vitals.ts 100% L/F; backoffice use-admin-list-search.ts 100% L/F/B; all other D-2 included files >=80% (client aggregate 96.3% stmts / 85.35% branch; backoffice 47 files 453 tests 0 failures exit 0); pre-existing test failures in registration-form + profile-page (4 files, 15 tests) are pre-boundary (committed before 968eefb in Feat/client portal PR 3f238ea) -- not in D-2 coverage include list
- [G8 Playwright client] PASS -- HTTP 200 at :5173; fundos route renders ("Nenhum fundo encontrado" expected without auth); ACF+PKCE redirect chain confirmed (code_challenge_method=S256, client realm); auth guard active (401 on /auth/me before authentication); no 5xx; no new application-level console errors
- [G9 Playwright backoffice] PASS -- HTTP 200 at :5174; /admin/login renders Admin Backoffice login page; ACF+PKCE redirect confirmed (code_challenge_method=S256, backoffice realm, custom Keycloak theme "Sign in to Backoffice"); auth guard redirects /admin/companies and /admin/fundos to /admin/login; no 5xx; no CORS errors; no cross-SPA code refs
- [G10 Accessibility] ADVISORY -- carry-forward pre-existing: client button-name (TanStack devtools); backoffice landmark/heading on login page; no keyboard traps; no new violations from BFE-5 commits
- [G11 Vinext debt] PASS -- no new Vinxi imports in phase 52 BFE-5 commits; web-vitals.ts and use-admin-list-search.ts use no Vinxi APIs

---

### BFE-5 Resolution Verification

#### BFE-5a -- client web-vitals.ts coverage: RESOLVED

- frontend/client/vitest.config.ts: src/lib/telemetry/web-vitals.ts in coverage include array -- CONFIRMED
- frontend/client/src/tests/lib/web-vitals.test.ts: 16 tests, all pass
- Coverage: Lines 8/8 = 100%, Functions 2/2 = 100%, Branches 0/0 (no branches in file)
- Commits: b22bed3

#### BFE-5b -- backoffice web-vitals.ts coverage: RESOLVED

- frontend/backoffice/vitest.config.ts: src/lib/telemetry/web-vitals.ts in coverage include array -- CONFIRMED
- frontend/backoffice/src/tests/lib/web-vitals.test.ts: 16 tests, all pass
- Coverage: Lines 8/8 = 100%, Functions 2/2 = 100%, Branches 0/0
- Commits: ba88a1c

#### BFE-5c -- backoffice use-admin-list-search.ts coverage: RESOLVED

- frontend/backoffice/vitest.config.ts: src/lib/use-admin-list-search.ts in coverage include array -- CONFIRMED
- frontend/backoffice/src/tests/lib/use-admin-list-search.test.tsx: 13 tests, all pass
- Coverage: Lines 6/6 = 100%, Functions 4/4 = 100%, Branches 2/2 = 100%
- Commits: d4332d8

---

### New test count (BFE-5 commits b22bed3 + ba88a1c + d4332d8)

| File | Tests | Result |
|---|---|---|
| client/src/tests/lib/web-vitals.test.ts | 16 | all pass |
| backoffice/src/tests/lib/web-vitals.test.ts | 16 | all pass |
| backoffice/src/tests/lib/use-admin-list-search.test.tsx | 13 | all pass |
| **Total new** | **45** | **all pass** |

---

### Coverage gaps (D-2 boundary files, iter 3)

| File | Stmts | Branch | Funcs | Lines | Gate |
|---|---|---|---|---|---|
| client/src/lib/telemetry/index.ts | 96.96% | 80.64% | 83.33% | 96.66% | PASS |
| client/src/lib/telemetry/web-vitals.ts | 100% | N/A | 100% | 100% | PASS (BFE-5a resolved) |
| backoffice/src/lib/telemetry/index.ts | 100% | 96.55% | 100% | 100% | PASS |
| backoffice/src/lib/telemetry/web-vitals.ts | 100% | N/A | 100% | 100% | PASS (BFE-5b resolved) |
| backoffice/src/lib/use-admin-list-search.ts | 100% | 100% | 100% | 100% | PASS (BFE-5c resolved) |
| All other D-2 included files | All >=80% | All >=80% | All >=80% | All >=80% | PASS |

Aggregate client (excluding pre-existing failures): 96.3% stmts / 85.35% branch / 97.19% funcs / 97.12% lines.
Aggregate backoffice: 453/453 tests pass, exit 0, all thresholds pass.

---

### Warnings (carry-forward)

- WFE-1: initAdminTelemetry fires after React render in backoffice main.tsx (dynamic import post-createRoot). Pre-existing.
- WFE-2: Double-import of telemetry/index.ts in backoffice main.tsx. Vite warns: dynamic import will not chunk-split. Pre-existing.
- WFE-3: pt-BR string in client main.tsx. Pre-existing, not phase-52.
- WFE-4: VITE_OTEL_ENABLED absent from compose.yaml. OTel inactive in compose stack. Positive traceparent test skips. Pre-existing.

---

### Regression captures

- Client fundos route: .jdi/cache/phase-52-frontend-iter3-client-fundos.png
- Client Keycloak ACF+PKCE: .jdi/cache/phase-52-frontend-iter3-client-keycloak-login.png
- Backoffice route guard: .jdi/cache/phase-52-frontend-iter3-backoffice-home.png
- Backoffice Keycloak custom theme: .jdi/cache/phase-52-frontend-iter3-backoffice-keycloak-login.png
- Client network: .jdi/cache/phase-52-frontend-iter3-client-network.json
- Backoffice network: .jdi/cache/phase-52-frontend-iter3-backoffice-network.json
- Console errors client (app-level): 0 (401/403 from API + Vite HMR WebSocket are pre-existing infrastructure artefacts)
- Console errors backoffice (app-level): 0 (same artefacts)
- No 5xx observed on either SPA

---

### DoD G0 OTel JS checklist (final)

| Item | Iter 2 | Iter 3 | Evidence |
|---|---|---|---|
| src/lib/telemetry/ composition root both SPAs | PASS | PASS | Directory + index.ts at correct path both SPAs |
| web-vitals.ts adapter both SPAs | PASS (file) | PASS | Both SPAs: registerWebVitals wires onCLS/onINP/onLCP/onFCP/onTTFB; 100% coverage |
| FetchInstrumentation literal client | PASS | PASS | Client: explicit literal present in telemetry/index.ts |
| backoffice telemetry coverage >= 80% | PASS | PASS | 100% stmts / 96.55% branch |
| web-vitals.ts coverage >= 80% both SPAs | FAIL BFE-5 | PASS | 100% both SPAs |
| use-admin-list-search.ts coverage >= 80% | FAIL BFE-5 | PASS | 100% all metrics |

## Reviewer: jdi-reviewer-onboarding-keycloak-security (iter 3)

**Verdict:** APPROVED_WITH_WARNINGS

---

### Scope

Minimal re-verify. Iter 3 commits touch: unit test mock stubs (e29798e), CPF seed fixes (33df07d), EF.Property CNPJ search (54ec103), frontend coverage tests (b22bed3/ba88a1c/d4332d8). No production auth, no Keycloak config, no compose, no appsettings changes.

---

### Gates

- [G1 Multi-tenant filter] PASS -- No change to EF configurations or HasQueryFilter registrations. Tenant guard at FundosController.cs:760 (SEC-B1 fix from iter 2) still in place. Search queries in ConsultoriaFundoRepository and CustodianteRepository use IgnoreQueryFilters() but are scoped by explicit `.Where(c => c.ClienteId == companyId)` predicate -- no cross-tenant data leak possible even if ILike clause throws HTTP 500. All 5 ClienteId guard assertions confirmed at FundosController lines 263, 404, 681, 760, 933.
- [G2 Permission policy coverage] PASS -- No new controllers or HTTP endpoints introduced. Carry-forward from iter 2: all endpoints authorized.
- [G3 Secrets + env hygiene] PASS -- New test files (web-vitals.test.ts x2, use-admin-list-search.test.tsx, FundoCedenteAssociationIntegrationTests.cs, CedenteTipoAtivoAssociationIntegrationTests.cs, FundosControllerTests.cs) contain no production credentials. PII pattern literals in web-vitals tests (`/(email|sub|cpf|cnpj|token|password|authorization|user)/i`) are test assertions for PII scrubbing logic, not secrets. CPF seeds use GenerateCpf(9001/9002/9003) -- deterministic algorithmic generation, not real CPFs. No appsettings.json, compose.yaml, or Keycloak realm changes.
- [G4 Semgrep] PASS -- 0 ERROR findings, 0 WARNING findings. 5 rules, 804 files scanned, exit 0. Artifact: .jdi/cache/phase-52-security-iter3-semgrep.json.
- [G5 Trivy FS + container] ADVISORY -- trivy binary not installed on host. No Dockerfile changes in iter 3. No new container images. Carry-forward SEC-W4.
- [G6 Keycloak hardening drift] PASS -- No Keycloak realm exports changed in iter 3. Carry-forward from iter 2.
- [G7 Security headers + CSP] PASS (code-only carry-forward) -- No changes to Program.cs CORS config or OTel collector CORS config. Exact-origin allowlist unchanged.
- [G8 Dependabot] NOT RUN -- gh CLI not available on host.
- [G9 Audit log] PASS -- No new mutation commands introduced in iter 3 commits. Carry-forward from iter 2: all 41 mutation handlers confirmed with ActorSub capture.

---

### Blockers

None. No new security issues introduced by iter 3 commits.

---

### Security note on B3-iter3 (EF.Property column name)

The backend reviewer flags B3-iter3 as a functional blocker: `EF.Property<string>(c, "cnpj")` uses the column name instead of the C# property or a shadow-property key, which may still fail EF Core 10 translation at runtime. From a security standpoint this is NOT a concern: the search query is already tenant-scoped via `.Where(c => c.ClienteId == companyId)` before the ILike predicate is evaluated. An exception in the ILike clause produces HTTP 500 (no data returned), which is fail-safe from an isolation standpoint. The fix is a functional correctness issue only; it does not create a data leak path.

---

### Warnings (carry-forward from iter 2 -- unchanged)

- SEC-W1 -- G5: OTel + Jaeger images use :latest tag in compose.yaml. Dev-only; pin before production.
- SEC-W2 -- G3 legacy: Dev secrets in Keycloak realm exports and appsettings.json. Pre-existing at D-2 boundary.
- SEC-W3 -- G3: E2E test passwords hardcoded in Playwright spec files. Pattern matches pre-existing specs. Use process.env.E2E_*_PASSWORD.
- SEC-W4 -- G5: Trivy binary unavailable. CI scan required before ship.
- SEC-W5 -- G7: VITE_OTEL_ENABLED absent from compose.yaml; positive traceparent test always skips.
- SEC-W6 -- OTel collector: db.statement not in key-drop list.
- SEC-W7 -- G6: ROPC enabled on onboarding-app (directAccessGrantsEnabled=true). Pre-existing at D-2 boundary; ACF+PKCE is active flow.
- SEC-W7b -- G6: Password policy length(8), not length(12). Pre-existing; tighten in hardening phase.

---

### Pipeline artifacts

- Semgrep: .jdi/cache/phase-52-security-iter3-semgrep.json (0 findings, 5 rules, 804 files, exit 0)
- Trivy FS: not run (binary unavailable on host -- CI required)
- Gitleaks: not run (binary unavailable on host); manual diff confirms 0 new production secrets in iter 3 diff
