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
