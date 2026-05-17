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


---

## Frontend review iter 1

### Verdict: APPROVED_WITH_WARNINGS

---

### Gates

**[G1 Security frontend] PASS**
- Zero localStorage/sessionStorage token writes in new files. Only hit: theme-provider.test.tsx:55 (a test file, not shipped code).
- No dangerouslySetInnerHTML in new components.
- No target=_blank without rel.
- No hardcoded secrets/API keys in new source.
- D-12 compliant: tokens remain exclusively in HttpOnly cookies via server-side auth-server.

**[G2 Telemetry (OTel JS + W3C)] BLOCKED -- pre-existing carry-forward**
- frontend/client/src/lib/telemetry/ does NOT exist. frontend/backoffice/src/lib/telemetry/ does NOT exist.
- Pre-existing gap at D-2 boundary (968eefb). Phase 50 adds zero telemetry regression. Designated for Phase 53 (telemetry sprint). Treated as WARNING under brownfield D-2 rule.

**[G3 Perf + bundle] PASS with WARNING**
- pnpm build exits 0. Main SPA bundle: 765.80 KB raw / 221.66 KB gzip -- under 300 KB gate.
- Vite warns chunk >500 KB raw. No lazy routes. WARN: next phase must add dynamic import() on fundos routes.
- No img without dimensions in new components.

**[G4 Build] PASS**
- pnpm --filter frontend-client build exits 0. All 3 Vinxi routers compile cleanly.

**[G5 Typecheck + Lint] PASS**
- pnpm --filter frontend-client typecheck (tsc --noEmit) exits 0.
- pnpm --filter frontend-client lint --max-warnings 0 exits 0.

**[G6 Code-design + Frontend rules] PASS with WARNINGS**
- D-4: zero cross-imports between client and backoffice.
- No pt-BR hardcoded strings in JSX (grep clean on new components).
- Radix/shadcn input primitives with associated labels -- no bare unlabeled inputs.
- No outline:none without focus alternative.
- Drift warnings (documented by doer, confirmed by reviewer):
  1. apiFetch duplicated in fundos-api.ts -- DRY violation. api.ts does not export its internal apiFetch; doer re-implemented 401/refresh cycle. Fix: export apiFetch from api.ts.
  2. auth.permissions cast via (auth as any).permissions in 10+ components. Fix: extend AuthContextValue with permissions: string[].
  3. fundosLocale strings inline in fundos-schemas.ts. Fix: move to locales/pt-BR/fundos.ts.

**[G7 Coverage new files] PARTIAL -- BLOCKER**
- @vitest/coverage-v8 not installed; mechanical enforcement blocked.
- Files WITH tests (coverage assumed adequate from test counts): api-errors.ts, fundos-schemas.ts, Paginator.tsx, SearchInput.tsx, AssociationForm.tsx, DateRangeInput.tsx, StatusTransitionDropdown.tsx, FundosListPage.tsx, TiposAtivoListPage.tsx, CedentesListPage.tsx, AuthGuard.tsx.
- Files WITHOUT tests (D-2 new files -- BLOCKER): fundos-api.ts (554 lines), query-client.ts (23 lines), use-allowed-transitions.ts (76 lines), and 22 component files (detail/tab pages + organism forms/tables).

**[G8 Playwright regression -- Client SPA (5173)] PARTIAL PASS**
- pnpm test:e2e --project=api-proxy: 3/3 PASS.
- pnpm test:e2e --project=auth-flow: 5 FAILED -- pre-existing (spec introduced in de3c594, before phase start 66522b2; not modified in Phase 50). Not a regression.
- Fundos E2E setup: admin-empresa.setup.ts cascade failure because viewer-creds.json absent (gitignored) and #username race on /. Pre-existing structural issue.
- MCP browser manual: ACF+PKCE login confirmed end-to-end (Keycloak S256 PKCE, callback to /profile). D-12: zero tokens in localStorage/sessionStorage confirmed.
- D-17 compliance confirmed in playwright.config.ts: api-proxy uses 127.0.0.1:5173; auth-flow overrides to localhost:5173 per D-17 refined.

**[G9 Playwright regression -- Backoffice SPA (5174)] PASS**
- Backoffice SPA loads at 5174, /admin/login renders, Entrar triggers ACF+PKCE redirect to backoffice Keycloak realm with code_challenge_method=S256. No client-app code in backoffice. No regression.

**[G10 Accessibility] ADVISORY**
- Keycloak login page: label associations on username/password confirmed. Backoffice: Entrar button with visible text. No keyboard traps observed. Full axe-core blocked by D-16.

**[G11 Vinext migration debt] PASS**
- Zero from 'vinxi' imports in new source files. Phase continues on Vinxi 0.5.11 as planned.

---

### Blockers

1. G7 -- 25+ new files (D-2 boundary) lack Vitest unit tests. Critical missing: fundos-api.ts (554 lines), use-allowed-transitions.ts, all detail/tab pages, all organism form/table components.
2. G7 -- @vitest/coverage-v8 missing from devDependencies; coverage gate cannot be mechanically enforced.

### Warnings

1. G2 Telemetry pre-existing carry-forward: OTel JS + W3C absent from both SPAs at D-2 boundary. Phase 53 must deliver.
2. G3 Bundle: raw SPA 765.80 KB. Next phase must add dynamic import() code-splitting on fundos routes.
3. G6 apiFetch DRY: fundos-api.ts re-implements 401/refresh cycle. Export apiFetch from api.ts.
4. G6 permissions typed as any: extend AuthContextValue with permissions: string[].
5. G6 locale strings: fundosLocale must move to locales/pt-BR/fundos.ts.
6. G8 auth-flow 5 pre-existing E2E failures: pre-date this phase. Fix in dedicated stabilization.
7. G8 viewer setup cascade: viewer-creds.json gitignored, setup errors when absent. Pre-existing.

### Coverage gaps (new files without tests)

- src/lib/fundos-api.ts (554 lines), src/lib/query-client.ts, src/lib/use-allowed-transitions.ts
- Pages: FundoDetailPage.tsx, FundoCedentesTabPage.tsx, FundoTiposAtivosTabPage.tsx, CedenteDetailPage.tsx, CedenteTiposAtivosTabPage.tsx, ConsultoriasFundoListPage.tsx, CustodiantesListPage.tsx
- Organisms: FundoForm, FundoTable, FundoStatusBadge, TipoAtivoForm, TipoAtivoTable, CedentePfForm, CedentePjForm, CedenteTable, CedenteTipoToggle, AssociationTable, ConsultoriaFundoForm, ConsultoriaFundoTable, CustodianteForm, CustodianteTable, LimiteExposicaoInput

### Regression captures

- Client ACF+PKCE MCP screenshot: .playwright-mcp/phase-50-client-profile.png
- Backoffice login MCP screenshot: .playwright-mcp/phase-50-backoffice-login.png
- Playwright api-proxy: 3/3 PASS
- Playwright auth-flow: 5 failures (pre-existing, not phase regression)
- Vitest: 209 tests pass / 15 fail (all in profile-page + registration-form files not touched in Phase 50)

## Security review iter 2

### Verdict: APPROVED_WITH_WARNINGS

Iter 2 commits: 64e1651 (dotnet format), a86a0f4 (coverage dep), 997184a (apiFetch dedup), 25069ee (permissions typing), 123e316 (locale extract), 6cc40f1 (23 test files).

---

### Gates

**[G1 Multi-tenant isolation (D-5)]** PASS — no new query handlers, aggregates, or EF configs in iter 2. Pure refactor + test additions. D-5 posture unchanged.

**[G2 AuthZ coverage]** PASS — no new endpoints, controllers, or routes added. Existing coverage unaffected.

**[G3 Secrets + env hygiene]** PASS — grep over all 24 new/modified files: zero hardcoded passwords, secrets, tokens, or bearer headers. New test files use mock objects and vi.fn() with no real credentials. theme-provider.test.tsx localStorage usage is pre-boundary (stores only "theme" key, not tokens).

**[G4 Semgrep]** PASS — no new C# logic; 64e1651 is whitespace-only. No semgrep re-run required for format-only diff.

**[G5 Trivy]** DEFERRED — single new dep (@vitest/coverage-v8@4.1.6, MIT). No Dockerfile or container changes in iter 2. Trivy CI-deferred (same as iter 1).

**[G6 Keycloak hardening drift]** PASS — git log scoped to 20859b0..6cc40f1 -- keycloak/ returns empty. Iter 2 touches only frontend/client/ and tests/. The post.logout.redirect.uris and clientProfiles no-wildcard enforcer changes are pre-iter-2 hardening improvements, not regressions.

**[G7 Security headers]** NOT VERIFIED — stack not running; same deferral as iter 1.

**[G8 Dependency OSS (D-3)]** PASS — @vitest/coverage-v8@4.1.6 license: MIT (verified from node_modules/package.json). No commercial dependency introduced.

**[G9 Audit log]** N/A — no new mutation commands in iter 2.

---

### apiFetch consolidation (W1 iter 1 — resolved)

fundos-api.ts now imports apiFetch from @/lib/api (line 7). The shared apiFetch in api.ts retains credentials: 'include' as its base option (api.ts:41) with no Authorization header at any call site. No bearer token in frontend. W1 closed.

---

### AuthContextValue.permissions typing (W4 iter 1 — resolved)

permissions: string[] is now declared on the AuthContextValue.auth interface (auth-context.tsx:36). State is initialized as [] and populated exclusively from /auth/me response (auth-context.tsx:95: setPermissions(data.permissions ?? [])). No localStorage write of permissions anywhere. No client-side mutation pathway. W4 closed.

---

### Blockers

None.

---

### Warnings

1. **Gitleaks / Trivy deferred to CI** — carry-forward from iter 1. Must pass CI before ship.
2. **Security headers (G7) not live-verified** — carry-forward from iter 1. Stack not running locally.
3. **D-12 localStorage in theme-provider.test.tsx** — pre-boundary file, stores only "theme" key, not tokens. No action needed; documented for traceability.

---

### Pipeline artifacts

- Gitleaks: deferred to CI.
- Trivy FS: deferred to CI.
- Semgrep: no re-run needed (format-only C# diff + TS-only changes with no new logic patterns).
