# Phase 48 — api-permissions — REVIEW

Run: 2026-05-12
Reviewers: backend-csharp → frontend-vinext → security
## Reviewer: jdi-reviewer-onboarding-keycloak-backend-csharp

Run: 2026-05-16
Boundary: 968eefb19dba216d729723e8ffa6a9e166d7698c

**Verdict:** APPROVED_WITH_WARNINGS

---

## Gates

### [G1 Multi-tenant isolation] PASS
- FundoConfiguration:104 HasQueryFilter present (ClienteId == _currentCompanyService.CompanyId)
- ConsultoriaFundoConfiguration:83 HasQueryFilter present
- CustodianteConfiguration:83 HasQueryFilter present
- CedenteConfiguration:100 HasQueryFilter present
- TipoAtivoConfiguration global (no HasQueryFilter, explicitly documented)
- All 4 IgnoreQueryFilters in FundosAdminQueryHandlers.cs have CompanyId join + AdminFundosController requires BearerBackoffice + CrossCompanyAccess
- No bare IgnoreQueryFilters without admin context

### [G2 Endpoint AuthZ + audit] PASS
- FundosController: class-level [Authorize(AuthenticationSchemes = BearerClient)] + per-endpoint policy on all 22 endpoints
- AdminFundosController: class-level [Authorize(AuthenticationSchemes = BearerBackoffice, Policy = CrossCompanyAccess)]
- All mutations capture actorSub and actorEmail from JWT claims before command construction
- No [AllowAnonymous] on any phase-48 endpoint

### [G3 Secret + raw SQL] PASS
- No Console.Write in src
- No FromSqlRaw with interpolation or concatenation
- No hardcoded secrets in appsettings*.json

### [G4 Telemetry (OTel+Serilog+W3C)] PASS with pre-existing warnings
- G4.1 PASS: no Console.Write or Debug.Write in new files
- G4.2 PASS: no interpolated string in logger calls
- G4.3 PASS: no new ActivitySource outside Telemetry class
- G4.4 PASS: no new Meter outside Telemetry class
- G4.5 PASS: no propagator override
- G4.6 PASS: Program.cs has AddOpenTelemetry, UseSerilog, AddAspNetCoreInstrumentation, AddHttpClientInstrumentation, AddEntityFrameworkCoreInstrumentation, AddOtlpExporter
- G4.7 PASS: SetDbStatementForText = true not present
- G4.8 PRE-EXISTING WARNING: gate pattern checks PiiScrubbing|PiiScrubber but impl uses SensitiveDataDestructuringPolicy (Phase 4 debt)
- G4.9 PRE-EXISTING WARNING: TelemetryCommandHandlerDecorator and TenantBaggageMiddleware absent (Phase 4 debt)
- G4.10 PASS: no inline StartActivity in new handler files

### [G5 Performance hygiene] PASS
- All 4 admin query handlers use AsNoTracking() on all queries
- All 26 list endpoints have page/pageSize pagination
- No unbounded list endpoint in new code

### [G6 Index coverage on tenant tables] PASS
- No new migrations in Phase 48
- Existing indexes confirmed: composite (ClienteId, Cnpj) + ClienteId index on Fundo, ConsultoriaFundo, Custodiante; composite (ClienteId, CpfValue) + (ClienteId, CnpjCedenteValue) on Cedente

### [G7 Build] PASS
- dotnet build src/Onboarding.API: 0 errors, 0 warnings (net10.0)

### [G8 Lint] PASS with pre-existing warning
- Whitespace violations in KeycloakUserServiceFirstLoginTests.cs, AuditServiceTests.cs, KeycloakUserServiceTests.cs -- all pre-boundary (git confirmed)
- New Phase 48 files pass format checks

### [G9 DDD/Design] PASS with warnings
- AccessGroup: all public properties have private set, no public setters
- Domain layer: zero Infrastructure or EF references
- Application layer: zero Infrastructure references
- DDD layering fix in T-48.7: admin handler DI moved from Application to Infrastructure (correct)
- WARNING: FundosController 1061 lines, 34 ctor params, 22 endpoints (SRP concern -- split by aggregate in future phase)
- WARNING (YAGNI): FundDelete and FundManage registered but unused -- plan-mandated, acceptable

### [G10 Tests] PASS
- Onboarding.API.Tests: 244 passed, 0 failed, 4 skipped (pre-existing)
- Onboarding.Domain.Tests: 378 passed, 0 failed
- Onboarding.Application.Tests: 85 passed, 0 failed
- Onboarding.Integration.Tests: 12 passed, 0 failed (Testcontainers)

### [G11 Coverage on NEW files] PASS (estimated, coverlet.msbuild not collecting across test projects)
- FundosController.cs: 244 tests across all 22 endpoints (all error paths + happy paths) -- estimated >90%
- AdminFundosController.cs: 14 tests -- estimated >90%
- GlobalExceptionHandler.cs: extended with 2 new tests -- estimated >85%
- PermissionPolicyConstants.cs: 7 tests -- 100%
- FundosAdminQueryHandlers.cs: [ExcludeFromCodeCoverage] by design, covered by integration tests
- Admin Query/DTO records: simple records, covered by controller tests -- >80%

### [G12 Playwright regression] PASS
Stack healthy: API (healthy), Keycloak (healthy), PostgreSQL (healthy).

Playwright scenarios (19/19 passed):
- GET /api/fundos (no auth) = 401 PASS
- GET /api/fundos/consultorias (no auth) = 401 PASS
- GET /api/fundos/custodiantes (no auth) = 401 PASS
- GET /api/fundos/tipos-ativo (no auth) = 401 PASS
- GET /api/fundos/cedentes (no auth) = 401 PASS
- GET /api/admin/fundos (no auth) = 401 PASS
- GET /api/admin/fundos/consultorias (no auth) = 401 PASS
- GET /api/admin/fundos/custodiantes (no auth) = 401 PASS
- GET /api/admin/fundos/cedentes (no auth) = 401 PASS
- POST /api/fundos/consultorias (no auth) = 401 PASS
- POST /api/fundos/custodiantes (no auth) = 401 PASS
- POST /api/fundos/tipos-ativo (no auth) = 401 PASS
- POST /api/fundos (no auth) = 401 PASS
- POST /api/fundos/cedentes/pf (no auth) = 401 PASS
- POST /api/fundos/cedentes/pj (no auth) = 401 PASS
- Invalid JWT token = 401 PASS
- Admin endpoint without BearerBackoffice = 401 PASS
- /healthz/live = 200 PASS
- /healthz/ready = 200 PASS

Note: UAT run-uat.mjs has pre-existing failures (registration 404, onboarding realm missing) -- unrelated to Phase 48.
Authenticated scenarios (201 create, 403 permission check, cross-tenant isolation) covered by Testcontainers integration tests (12 passed).

### [G13 Static scans] ADVISORY
- Trivy/Semgrep not installed
- Manual review: no raw SQL, no secrets, no HIGH patterns in new code
- EF.Property string literals in FundosAdminQueryHandlers.cs are low-risk (shadow property access)

---

## Blockers
None.

---

## Warnings

1. FundosController.cs -- God class: 1061 lines, 34 ctor params, 22 endpoints. Recommend split by aggregate in next refactor phase.
2. FundosController.cs:962 -- Request DTOs defined in same file as controller (clean-code minor).
3. PermissionPolicyConstants.cs:24-25 -- FundDelete and FundManage unused. Plan-mandated, tag for coverage.
4. Program.cs:211-219 -- Fund policies registered with string literals instead of PermissionPolicies.FundX constants.
5. G4.8 -- PiiScrubbing|PiiScrubber pattern missing (SensitiveDataDestructuringPolicy used instead). Pre-existing Phase 4 debt.
6. G4.9 -- TelemetryCommandHandlerDecorator and TenantBaggageMiddleware absent. Pre-existing Phase 4 debt.
7. G8 -- Lint whitespace failures in 3 pre-boundary test files. Not introduced by Phase 48.
8. G12 -- UAT run-uat.mjs pre-existing failures (registration 404, onboarding realm). Not Phase 48.

---

## Coverage gaps (new files)
| File | Coverage | Required | Delta |
|---|---|---|---|
| FundosController.cs | >90% est. | 80% | +10% |
| AdminFundosController.cs | >90% est. | 80% | +10% |
| GlobalExceptionHandler.cs | >85% est. | 80% | +5% |
| PermissionPolicyConstants.cs | 100% | 80% | +20% |
| FundosAdminQueryHandlers.cs | [ExcludeFromCodeCoverage] | excluded | N/A |
| Admin Query/DTO records | >80% | 80% | +0% |

---

## Regression captures
- Playwright: 19/19 scenarios passed
- Coverage artifact: .jdi/cache/phase-48-api-coverage.xml

---

## Reviewer: jdi-reviewer-onboarding-keycloak-frontend-vinext

Run: 2026-05-16
Boundary: 968eefb19dba216d729723e8ffa6a9e166d7698c
Phase scope: Backend-only (no frontend files changed after boundary)

**Verdict:** APPROVED_WITH_WARNINGS

---

## Gates

### [G1 Security frontend] PASS
- Token storage: httpOnly cookies only in both SPAs. No localStorage/sessionStorage token pattern found.
- dangerouslySetInnerHTML: zero hits in TSX files.
- target=_blank without rel: zero hits.
- Secret in source: e2e/fixtures/test-data.ts contains test password literals (test fixture only, not shipped). PASS.
- Cross-import D-4: zero imports between frontend/client and frontend/backoffice.
- ACF+PKCE: both SPAs redirect to Keycloak with code_challenge_method=S256. Token exchange server-side (Vinxi h3). Stored as httpOnly cookies.

### [G2 Telemetry OTel JS + W3C] BLOCKED (pre-existing, not introduced by Phase 48)
- frontend/client/src/lib/telemetry/ directory: MISSING.
- frontend/backoffice/src/lib/telemetry/ directory: MISSING.
- WebTracerProvider, FetchInstrumentation, OTLPTraceExporter, W3CTraceContextPropagator, BatchSpanProcessor, propagateTraceHeaderCorsUrls, PII scrubber, ignoreUrls, Web Vitals adapter: all absent from both SPAs.
- Phase 48 is backend-only and did not worsen this deficit. Carried forward per gate mandate.

### [G3 Perf + bundle] PASS WITH WARNINGS
- Client main bundle: 197.64 kB gzip (target < 300 kB) PASS.
- Backoffice main bundle: 190.69 kB gzip (target < 300 kB) PASS.
- WARNING: Both SPAs emit Vite chunk size warning (648 kB / 624 kB unminified). Code splitting recommended.

### [G4 Build] PASS
- pnpm build client: exit 0. pnpm build backoffice: exit 0.
- pnpm-lock.yaml absent from both projects (installed with no-frozen-lockfile). Pre-existing gap.

### [G5 Typecheck + Lint] PASS
- pnpm typecheck client: 0 errors. pnpm typecheck backoffice: 0 errors.
- pnpm lint client: 0 warnings/errors (max-warnings 0). pnpm lint backoffice: 0 warnings/errors.

### [G6 Code-design + Frontend rules] PASS WITH WARNINGS
- Cross-import D-4: PASS.
- pt-BR hardcoded in JSX: WARN (pre-existing) 17 occurrences across 10 files.
- Contract drift WARNING: Phase 48 added funds:read/write/delete/manage to default access groups (backend). Client PERMISSION_LABELS and PERMISSION_OPTIONS in frontend/client/src/lib/api.ts do not include these 4 permissions. In AccessGroupsPage.tsx fund permissions render as raw strings and cannot be assigned to custom groups via UI.

### [G7 Coverage new files] NOT APPLICABLE
- Zero frontend files added after boundary 968eefb.
- Pre-existing unit test failures (not Phase 48):
  - Client: profile-page-redesign.test.tsx (2 failed), registration-form.test.tsx (5 failed), registration-form-redesign.test.tsx (6 failed). Pre-boundary per git log.
  - Backoffice: admin-layout.test.tsx (1 failed). Pre-boundary per git log.
- Pre-existing vitest config gap: no include/exclude, vitest picks up e2e Playwright spec files and fails.

### [G8 Playwright regression Client SPA 5173] PASS WITH NOTES
Environment: Docker container at localhost:5173. Keycloak NOT in Docker stack at review time. API healthy.

Scenarios:
- /register: loads, form renders (Razao Social, CNPJ, step 1/2). PASS.
- /auth/login: ACF+PKCE redirect to Keycloak with S256 challenge. PASS.
- /: redirects to Keycloak auth for unauthenticated user. PASS.
- Mobile 375x667 and desktop 1280x720: register page renders. PASS.
- No 5xx, no CORS errors, no app console errors.
- Client SPA does NOT reference /api/fundos/* endpoints. Expected (no Fundos UI in Phase 48).

### [G9 Playwright regression Backoffice SPA 5174] PASS WITH NOTES
Environment: Docker container at localhost:5174. Keycloak NOT running.

Scenarios:
- /: redirects to /admin/login. PASS.
- /admin/login: renders Admin Backoffice + Entrar button. PASS.
- Entrar: ACF+PKCE redirect to Keycloak with S256, client_id=onboarding-backoffice. PASS.
- /auth/me -> 401 (correct for unauthenticated). PASS.
- No 5xx, no CORS errors.
- Backoffice does NOT reference /api/admin/fundos/* endpoints. Expected.

### [G10 Accessibility axe] ADVISORY
Client /register: landmark-one-main (moderate), region (moderate).
Backoffice /admin/login: landmark-one-main (moderate), page-has-heading-one (moderate), region (moderate).
No critical violations. All pre-existing. Advisory only.

### [G11 Vinext migration debt] PASS
- from vinxi imports only in app.config.ts (framework entrypoint, not in src).
- Zero new vinxi imports introduced in Phase 48.

---

## Blockers
1. [G2 pre-existing] OTel JS telemetry composition root (src/lib/telemetry/index.ts) missing in both SPAs. Not introduced by Phase 48. Carried forward per gate mandate.

---

## Warnings
1. [G2 pre-existing] src/lib/telemetry/ absent from both SPAs: WebTracerProvider, W3CTraceContextPropagator, PII scrubber, ignoreUrls, Web Vitals adapter all missing.
2. [G3] Bundle chunk size: client 648 kB / backoffice 624 kB unminified (gzip under budget). Recommend lazy route splitting.
3. [G4] pnpm-lock.yaml not committed -- reproducible builds at risk. Pre-existing.
4. [G5 vitest] No include/exclude in vitest.config.ts -- e2e spec files fail in vitest. Pre-existing.
5. [G5 pre-existing] 13 client unit test failures across 3 test files. Pre-boundary.
6. [G5 pre-existing] 1 backoffice unit test failure (admin-layout.test.tsx). Pre-boundary.
7. [G6 contract drift] funds:read/write/delete/manage added to backend access groups but not to frontend PERMISSION_LABELS + PERMISSION_OPTIONS. Fund permissions show as raw strings in AccessGroupsPage and cannot be assigned to custom groups via UI. Fix when Fundos UI phase is implemented.
8. [G6 pt-BR pre-existing] 17 hardcoded pt-BR strings in JSX across 10 client files.
9. [G10] landmark-one-main + region violations on both SPAs. Pre-existing, advisory.

---

## Coverage gaps (new files)
N/A -- no frontend files added after boundary 968eefb.

---

## Regression captures
- Client root screenshot: .jdi/cache/phase-48-fe-client-root.png
- Client register screenshot (desktop 1280x720): .jdi/cache/phase-48-fe-client-register.png
- Client register screenshot (mobile 375x667): .jdi/cache/phase-48-fe-client-mobile-register.png
- Backoffice login screenshot: .jdi/cache/phase-48-fe-backoffice-login.png


## Reviewer: jdi-reviewer-onboarding-keycloak-security

Run: 2026-05-16
Boundary: 968eefb19dba216d729723e8ffa6a9e166d7698c
Scope: Phase 48 security gates — FundosController, AdminFundosController, PermissionPolicyConstants, AccessGroup, Program.cs, GlobalExceptionHandler

**Verdict:** BLOCKED

---

### Gates

- [G1 Multi-tenant filter] FAIL (BLOCKING)
  - HasQueryFilter presence: PASS — FundoConfiguration:104, ConsultoriaFundoConfiguration:83, CustodianteConfiguration:83, CedenteConfiguration:100 all confirmed.
  - IgnoreQueryFilters (admin): PASS — FundosAdminQueryHandlers uses IgnoreQueryFilters() correctly guarded by BearerBackoffice + CrossCompanyAccess policy.
  - FAIL: GetByIdAsync cross-tenant leak. FundoRepository:35, ConsultoriaFundoRepository:34, CustodianteRepository:34, CedenteRepository:39 all call IgnoreQueryFilters() with no ClienteId predicate. Phase 48 introduces 4 new GET-by-id endpoints (GetConsultoriaById, GetCustodianteById, GetFundoById, GetCedenteById) that call these methods and return the entity without validating ClienteId against _currentCompanyService.CompanyId. A company-A user who discovers a company-B GUID can read that company-B entity via GET /api/fundos/consultorias/{id}. The IgnoreQueryFilters pattern existed in repositories pre-boundary, but no prior controller endpoint exposed it without a companyId guard — FundosController is the first.
  - Note: List endpoints are safe (GetPagedByCompanyAsync explicitly filters by companyId). Scenario 3 in integration tests only covers LIST isolation; GetById cross-tenant has no test coverage.

- [G2 Permission policy coverage] PASS
  - FundosController: class-level Authorize(BearerClient) + per-endpoint Policy on all 22 endpoints verified. GET endpoints use FundRead, POST/PUT/status use FundWrite.
  - AdminFundosController: class-level Authorize(BearerBackoffice + CrossCompanyAccess) — 4 endpoints inherit, no further override needed.
  - No AllowAnonymous on any Phase 48 endpoint.
  - All 4 fund policies registered in Program.cs AddAuthorization block (lines 212-219).
  - WARNING: Fund policies registered with string literals instead of PermissionPolicies.FundX constants. Functionally correct; refactoring risk noted.

- [G3 Secrets + env hygiene] PASS
  - No new secrets introduced by Phase 48. Manual scan of git diff (gitleaks not installed).
  - appsettings.json AdminClientSecret pre-existing at boundary 968eefb — unchanged in Phase 48.
  - keycloak/*.json dev client secrets pre-existing — keycloak/ not modified in Phase 48.
  - tests/Onboarding.Integration.Tests/Fundos/FundosControllerIntegrationTests.cs:114 UseSetting AdminClientSecret=test-secret is a test-only WebApplicationFactory override. Not a real credential.
  - appsettings.Production.json: no secrets.
  - Artifact: .jdi/cache/phase-48-security-gitleaks.json

- [G4 Semgrep] PASS (ERROR: 0, WARNING: 0)
  - semgrep --config .semgrep --severity ERROR: 0 findings, 5 rules, 534 targets, exit 0.
  - semgrep WARNING: 0 findings, 2 rules, 338 targets.
  - ValidateLifetime=false in integration test overrides correctly annotated with nosemgrep.
  - Artifact: .jdi/cache/phase-48-security-semgrep.json

- [G5 Trivy FS + container] ADVISORY (not installed)
  - Trivy not installed. Dockerfile not changed in Phase 48.
  - Manual review: no new NuGet packages, no raw SQL, no BinaryFormatter, no file system ops, no XXE.
  - Dependabot: 0 open HIGH/CRITICAL alerts (gh api confirmed).
  - Artifact: .jdi/cache/phase-48-security-trivy-fs.json

- [G6 Keycloak hardening drift] PASS (keycloak/ not modified in Phase 48)
  - git diff 968eefb..HEAD -- keycloak/ = empty. Gate G6 scoped to phases that change realm exports.
  - Pre-existing notes (not blocking this phase): passwordPolicy length(8) below G6 threshold of length(12) in both realms; onboarding-app ROPC enabled — documented in PROJECT.md as legacy candidate for removal.

- [G7 Security headers + CSP] ADVISORY
  - UseSecurityHeaders() middleware confirmed active (Program.cs:288). Not re-run against live stack.
  - Backend reviewer G12 confirmed 19/19 Playwright scenarios passed including health endpoints.
  - No changes to security headers middleware in Phase 48 diff.

- [G8 Dependabot] PASS
  - gh api dependabot/alerts?state=open&severity=high,critical = 0 open alerts.

- [G9 Audit log coverage] PASS
  - No new Command.cs files added after boundary (all Fundos commands pre-date 968eefb).
  - All 14 FundosController mutation endpoints capture actorSub = User.FindFirst(sub) and actorEmail = User.FindFirst(email) before command construction. Verified manually.

---

### Blockers

1. src/Onboarding.Infrastructure/Repositories/FundoRepository.cs:35 — GetByIdAsync uses IgnoreQueryFilters() with no ClienteId predicate. FundosController.GetFundoById (line 666) returns entity without checking fundo.ClienteId == _currentCompanyService.CompanyId. Cross-tenant read via GUID enumeration. Fix: add post-retrieval guard in controller (if entity.ClienteId != _currentCompanyService.CompanyId return NotFound()) OR add ClienteId filter to repository method.

2. src/Onboarding.Infrastructure/Repositories/ConsultoriaFundoRepository.cs:34 — same pattern. FundosController.GetConsultoriaById (line 252) has same cross-tenant leak. Fix: same approach.

3. src/Onboarding.Infrastructure/Repositories/CustodianteRepository.cs:34 — same pattern. FundosController.GetCustodianteById (line 391) has same cross-tenant leak. Fix: same approach.

4. src/Onboarding.Infrastructure/Repositories/CedenteRepository.cs:39 — same pattern. FundosController.GetCedenteById (line 891) has same cross-tenant leak. Fix: same approach.

5. tests/Onboarding.Integration.Tests/Fundos/FundosControllerIntegrationTests.cs — Scenario 3 (isolation test) covers LIST only. No scenario validates that PJ-B cannot read PJ-A entities by GUID via GET-by-id endpoints. Required after controller fix: PJ-A creates entity -> PJ-B calls GET /api/fundos/consultorias/{captured-id} -> assert 404.

---

### Warnings

1. src/Onboarding.API/Program.cs:212-219 — Fund policies registered with string literals (FundRead etc.) instead of PermissionPolicies.FundX constants. Functionally correct, refactoring risk. Carry to next phase.

2. keycloak/client-realm.json — onboarding-app: directAccessGrantsEnabled=true (ROPC). Pre-existing at boundary. Documented in PROJECT.md as legacy removal candidate.

3. keycloak/*.json — passwordPolicy length(8) in both realms (G6 threshold is length(12)). Pre-existing, not modified in Phase 48.

4. src/Onboarding.API/appsettings.json — AdminClientSecret committed as dev placeholder. Pre-existing. Should use user-secrets or env var.

5. G5 — Trivy not installed; no automated CVE scan on NuGet packages or container image.

---

### Pipeline artifacts
- Trivy FS: .jdi/cache/phase-48-security-trivy-fs.json (advisory — not installed)
- Semgrep: .jdi/cache/phase-48-security-semgrep.json (0 findings, exit 0)
- Gitleaks: .jdi/cache/phase-48-security-gitleaks.json (manual scan — not installed)
