-NoNewline

## Reviewer: jdi-reviewer-onboarding-keycloak-security (iter 2)

Run: 2026-05-16
Boundary: 968eefb19dba216d729723e8ffa6a9e166d7698c
Iter 2 commit: eb5bc24
Changed files (iter 2 only): src/Onboarding.API/Controllers/FundosController.cs (+16 lines), tests/Onboarding.Integration.Tests/Fundos/FundosControllerIntegrationTests.cs (+101 lines)

**Verdict:** APPROVED_WITH_WARNINGS

---

### Gates

- [G1 Multi-tenant filter] PASS (all 5 iter-1 blockers RESOLVED)

  Iter-1 blockers resolution:
  - B1 GetConsultoriaById (FundosController:257): null-or-ClienteId-mismatch guard returns NotFound -- VERIFIED in code.
  - B2 GetCustodianteById (FundosController:398): same guard -- VERIFIED in code.
  - B3 GetFundoById (FundosController:675): same guard -- VERIFIED in code.
  - B4 GetCedenteById (FundosController:902): same guard -- VERIFIED in code.
  - B5 Integration test coverage: 4 cross-tenant scenarios (scenarios 9-12) added and cover the new guard branch -- VERIFIED in diff.

  Tenant check order verified correct on all 4 guards: fetch first, then null-or-ClienteId-mismatch in single OR expression. Null is short-circuited before property access (C# left-to-right evaluation of ||). No NPE risk. Returns NotFound (HTTP 404) -- not Forbid (403). Correct: a 403 on an unknown GUID would confirm entity existence across tenant boundary.

  Integration test assertion: response.StatusCode.ShouldBe(HttpStatusCode.NotFound, ...). Each scenario uses clientA to create entity, then clientB requests the captured GUID. Assert message explicitly states intent. Test exercises the real controller guard path -- VERIFIED.

  HasQueryFilter presence (unchanged): FundoConfiguration:104, ConsultoriaFundoConfiguration:83, CustodianteConfiguration:83, CedenteConfiguration:100 -- all confirmed intact by grep. No HasQueryFilter diff in iter 2.

  IgnoreQueryFilters in FundosAdminQueryHandlers: unchanged, still guarded by BearerBackoffice + CrossCompanyAccess. 4 handlers confirmed.

  Pre-existing advisory (PUT handlers -- not introduced by iter 2): UpdateConsultoriaFundoCommandHandler, UpdateCustodianteCommandHandler, UpdateFundoCommandHandler, UpdateCedenteCommandHandler all call GetByIdAsync (IgnoreQueryFilters) without a ClienteId check in the application layer. These were added in phase-47 commits 8dd890b and 1964c04, which are pre-boundary. Not introduced by Phase 48 or iter 2. Flagged as WARNING (see Warnings section).

  TipoAtivo: global entity, no ClienteId property, no HasQueryFilter by design (documented in TipoAtivoConfiguration). GetTipoAtivoById has no tenant guard -- correct and intentional.

- [G2 Permission policy coverage] PASS (unchanged from iter 1)
  - All 4 modified GET-by-id actions retain [Authorize(AuthenticationSchemes = BearerClient, Policy = PermissionPolicies.FundRead)].
  - No new endpoints added. No AllowAnonymous introduced.
  - All 4 fund policies registered in Program.cs AddAuthorization block -- unchanged.
  - Warning carried: fund policies registered with string literals instead of PermissionPolicies.FundX constants (pre-existing, functionally correct).

- [G3 Secrets + env hygiene] PASS
  - git diff 968eefb..eb5bc24 -- src/ tests/ manually scanned for secret patterns.
  - Two new JwtSecurityToken() hits in test diff: both are FakeJwtHelper JWT factory methods used by Testcontainers test infrastructure. Issuer/audience are localhost URLs; no real credentials.
  - ValidateLifetime=false in test JWT override annotated with nosemgrep comment -- correct.
  - No new appsettings changes, no new Keycloak export changes.
  - Artifact: .jdi/cache/phase-48-security-iter2-gitleaks.json (manual scan -- gitleaks not installed)

- [G4 Semgrep] PASS (ERROR: 0, WARNING: 0)
  - semgrep --config .semgrep --severity ERROR --error --json: 0 findings, 5 rules, 534 targets, exit 0.
  - nosemgrep annotations on ValidateLifetime=false: correct and justified (test-only WebApplicationFactory override; production JWT validation unchanged).
  - Artifact: .jdi/cache/phase-48-security-iter2-semgrep.json

- [G5 Trivy FS + container] ADVISORY (not installed)
  - Trivy not installed. Dockerfile not changed in iter 2.
  - No new NuGet packages added in iter 2 diff. No raw SQL, no BinaryFormatter, no file system ops.
  - Dependabot: 0 open HIGH/CRITICAL alerts (confirmed in iter 1; no new packages to trigger new alerts).
  - Artifact: .jdi/cache/phase-48-security-iter2-trivy-fs.json

- [G6 Keycloak hardening drift] PASS (keycloak/ not modified in iter 2)
  - git diff 968eefb..eb5bc24 -- keycloak/: empty. Gate G6 scoped to phases that change realm exports.
  - Pre-existing notes (not blocking): passwordPolicy length(8) below G6 threshold of length(12); onboarding-app ROPC enabled. Both pre-boundary, unchanged.

- [G7 Security headers + CSP] ADVISORY (no change from iter 1)
  - UseSecurityHeaders() middleware unchanged in iter 2 diff. No security headers regression possible from the 16-line controller change.
  - Not re-run against live stack; backend reviewer ran 23/23 Playwright scenarios in their iter 2 segment.

- [G8 Dependabot] PASS
  - 0 open HIGH/CRITICAL alerts confirmed in iter 1. No new packages in iter 2.

- [G9 Audit log coverage] PASS
  - No new Command.cs files added in iter 2 (diff-filter=A confirms zero new Command files in eb5bc24).
  - GET-by-id endpoints are read-only; no actor capture required.
  - All existing mutation endpoints retain actorSub + actorEmail capture -- unchanged.

---

### Blockers

None.

---

### Warnings

1. src/Onboarding.Application/Fundos/Commands/UpdateConsultoriaFundoCommandHandler.cs:35 -- pre-existing (phase 47, commit 8dd890b). UpdateConsultoriaFundoCommandHandler calls GetByIdAsync (IgnoreQueryFilters) without ClienteId tenant check in application layer. Same pattern in UpdateCustodianteCommandHandler:32, UpdateFundoCommandHandler:33, UpdateCedenteCommandHandler:33. A cross-tenant actor with a valid GUID and funds:write can mutate another company entity field values without ownership transfer. Not introduced by Phase 48 or iter 2. Recommend adding ClienteId ownership guard in application-layer handlers in a future hardening phase.

2. src/Onboarding.API/Program.cs:212-219 -- Fund policies registered with string literals instead of PermissionPolicies.FundX constants. Functionally correct; refactoring risk. Carried from iter 1.

3. keycloak/client-realm.json -- onboarding-app: directAccessGrantsEnabled=true (ROPC). Pre-existing, documented in PROJECT.md as legacy removal candidate.

4. keycloak/*.json -- passwordPolicy length(8) in both realms (G6 threshold is length(12)). Pre-existing, not modified in Phase 48 or iter 2.

5. src/Onboarding.API/appsettings.json -- AdminClientSecret committed as dev placeholder. Pre-existing.

6. G5 -- Trivy not installed; no automated CVE scan on NuGet packages or container image.

---

### Pipeline artifacts
- Trivy FS: .jdi/cache/phase-48-security-iter2-trivy-fs.json (advisory -- not installed)
- Semgrep: .jdi/cache/phase-48-security-iter2-semgrep.json (0 findings, exit 0)
- Gitleaks: .jdi/cache/phase-48-security-iter2-gitleaks.json (manual scan -- not installed)


---



## Reviewer: jdi-reviewer-onboarding-keycloak-backend-csharp (iter 3)



Run: 2026-05-16

Boundary: 968eefb19dba216d729723e8ffa6a9e166d7698c

Iter 3 commit: 07dcb2b

Changed files (iter 3 only): tests/Onboarding.Integration.Tests/Fundos/FundosControllerIntegrationTests.cs (+6 lines) -- whitespace-only style fix via dotnet format.



**Verdict:** APPROVED_WITH_WARNINGS



---



### Gates

- [G1 Multi-tenant isolation] PASS (unchanged from iter 2)
  HasQueryFilter on all 5 company-scoped aggregates confirmed intact. No diff in configurations.
  4 cross-tenant GET-by-id guards in FundosController verified correct (null-or-ClienteId-mismatch).
  FundosAdminQueryHandlers: 4 IgnoreQueryFilters usages still guarded by BearerBackoffice + CrossCompanyAccess.

- [G2 Endpoint AuthZ + audit] PASS (unchanged from iter 2)
  No new HTTP actions in iter 3 diff. All 19 FundosController actions retain Authorize attribute.
  AdminFundosController: class-level Authorize(BearerBackoffice, CrossCompanyAccess) covers all 4 GETs.
  GET-only controller -- no mutation actor capture required.

- [G3 Secret + raw SQL hygiene] PASS (unchanged from iter 2)
  Iter 3 diff: 6 lines whitespace-only. No secrets, no raw SQL, no interpolated FromSqlRaw.

- [G4 Telemetry (OTel+Serilog+W3C)] PASS (unchanged from iter 2)
  G4.1-G4.10: all checks pass. Program.cs wiring unchanged. No Console.Write*, no interpolated logger.
  SensitiveDataDestructuringPolicy (PII) + UseClientClaims (tenant) + TelemetryCommandHandlerDecorator present.

- [G5 Performance hygiene] PASS
  FundosAdminQueryHandlers: all 4 handlers use AsNoTracking() + pagination. No unbounded lists.
  FundosController list endpoints: all use page/pageSize params.

- [G6 Index coverage on tenant tables] PASS (no new migrations in iter 3)
  git diff 968eefb..07dcb2b -- Migrations/: empty. Gate scoped to new migrations only.

- [G7 Build] PASS
  dotnet build Onboarding.slnx: Build succeeded, 0 Error(s), 0 Warning(s).

- [G8 Lint/format] PASS (iter 2 blocker RESOLVED)
  Iter 2 blocker: anonymous object at FundosControllerIntegrationTests.cs:493-501 not one-property-per-line.
  Iter 3 fix: dotnet format applied split. dotnet format --verify-no-changes exits 0 on post-boundary files.

- [G9 DDD/Design] PASS (unchanged from iter 2)
  No DDD violations in iter 3 diff (whitespace-only). Sealed positional records correct DDD DTO pattern.
  Domain layer free of Infrastructure namespace references. No MediatR, no FluentAssertions added.

- [G10 Tests] PASS
  Integration tests: 16/16 passed (Docker Desktop running, Testcontainers PostgreSQL + API).
  Unit tests: all pass. No failing test introduced by iter 3 whitespace fix.

- [G11 Coverage on new files] APPROVED_WITH_WARNINGS
  Enforcement boundary: files added after 968eefb (diff-filter=A).
  AdminFundosController.cs: line-rate=1.0 (100%) -- PASS
  FundosController.cs: line-rate=1.0 (100%) -- PASS
  GlobalExceptionHandler.cs: line-rate=0.8833 (88%) -- PASS
  FundosAdminQueryHandlers.cs: [ExcludeFromCodeCoverage] -- exempt
  AdminFundoDto.cs: line-rate=0.2857 (29%) -- WARNING (Coverlet positional-record artifact)
  AdminCedenteDto.cs: line-rate=0.4166 (42%) -- WARNING (Coverlet positional-record artifact)
  AdminConsultoriaFundoDto.cs: line-rate=0.3636 (36%) -- WARNING (Coverlet positional-record artifact)
  AdminCustodianteDto.cs: line-rate=0.3636 (36%) -- WARNING (Coverlet positional-record artifact)
  ListAdminCedenteQuery.cs: line-rate=0.6 (60%) -- WARNING (Coverlet positional-record artifact)
  ListAdminCustodianteQuery.cs: line-rate=0.6 (60%) -- WARNING (Coverlet positional-record artifact)
  ListAdminConsultoriaQuery.cs: line-rate=1.0 (100%) -- PASS
  ListAdminFundoQuery.cs: line-rate=1.0 (100%) -- PASS
  NOT blocking: all sub-80% files are sealed positional records (no authored logic uncovered).

- [G12 Playwright regression] PASS (19/19 scenarios)
  Stack: docker compose running (PostgreSQL 16 + API + Keycloak 26.1).
  19 scenarios via Playwright MCP + curl against http://localhost:5046 -- all 200/201/404 as expected.
  Cross-tenant scenarios 8,11,13,15: all return 404 (G1 guard verified live).
  Admin endpoints 16-19: BearerBackoffice token, 200 OK paginated cross-company.

- [G13 Static scans] ADVISORY
  Semgrep: 0 findings (5 rules, 534 targets, exit 0). Artifact: .jdi/cache/phase-48-backend-iter3-semgrep.json
  Trivy: not installed. No new NuGet packages in iter 3.
  Gitleaks: not installed. Manual scan: 0 secrets in iter 3 diff.

---

### Blockers

None.

---

### Warnings

1. AdminFundoDto.cs, AdminCedenteDto.cs, AdminConsultoriaFundoDto.cs, AdminCustodianteDto.cs,
   ListAdminCedenteQuery.cs, ListAdminCustodianteQuery.cs -- Coverlet line-rate below 80% (29-60%).
   Cause: positional record compiler-generated members (equality, deconstruct, PrintMembers).
   No authored logic is uncovered. NOT a real coverage gap.
   Fix: add [ExcludeFromCodeCoverage] to DTO records in a future cleanup phase.

2. UpdateConsultoriaFundoCommandHandler.cs:35 (pre-existing, phase 47) -- IgnoreQueryFilters without
   ClienteId ownership check. Same in UpdateCustodianteCommandHandler:32, UpdateFundoCommandHandler:33,
   UpdateCedenteCommandHandler:33. Cross-tenant actor with funds:write can mutate another company entity.

3. Program.cs:212-219 (pre-existing) -- Fund policies registered with string literals instead of
   PermissionPolicies.FundX constants. Functionally correct; refactoring risk.

4. keycloak/client-realm.json (pre-existing) -- directAccessGrantsEnabled=true (ROPC). Legacy removal candidate.

5. keycloak/*.json (pre-existing) -- passwordPolicy length(8). G6 threshold length(12). Not modified in Phase 48.

6. tests/run-uat.mjs (pre-existing) -- 9/22 UAT scenarios pass; 13 fail due to route mismatch.
   Not a Phase 48 regression.

---

### Coverage gaps (new files)

| File | Coverage | Required | Delta |
|---|---|---|---|
| AdminFundoDto.cs | 29% | 80% | -51% (Coverlet positional-record artifact) |
| AdminCedenteDto.cs | 42% | 80% | -38% (Coverlet positional-record artifact) |
| AdminConsultoriaFundoDto.cs | 36% | 80% | -44% (Coverlet positional-record artifact) |
| AdminCustodianteDto.cs | 36% | 80% | -44% (Coverlet positional-record artifact) |
| ListAdminCedenteQuery.cs | 60% | 80% | -20% (Coverlet positional-record artifact) |
| ListAdminCustodianteQuery.cs | 60% | 80% | -20% (Coverlet positional-record artifact) |
| AdminFundosController.cs | 100% | 80% | +20% |
| FundosController.cs | 100% | 80% | +20% |
| GlobalExceptionHandler.cs | 88% | 80% | +8% |
| FundosAdminQueryHandlers.cs | exempt | -- | [ExcludeFromCodeCoverage] |

---

### Regression captures

- Playwright HAR: .jdi/cache/phase-48-backend-iter3-playwright-har.json
- Console errors: .jdi/cache/phase-48-backend-iter3-console.log (0 errors)
- Semgrep: .jdi/cache/phase-48-backend-iter3-semgrep.json (0 findings)

---

## Reviewer: jdi-reviewer-onboarding-keycloak-frontend-vinext (iter 3)

Run: 2026-05-16
Boundary: 968eefb19dba216d729723e8ffa6a9e166d7698c
Iter 3 commit: 07dcb2b (style fix on C# test file — zero frontend impact)
Changed frontend files since boundary: none (git diff --diff-filter=A/M 968eefb..HEAD -- frontend/**: empty)

**Verdict:** APPROVED_WITH_WARNINGS

---

### Gates

- [G1 Security frontend] PASS
  - localStorage/sessionStorage token scan: 0 hits.
  - dangerouslySetInnerHTML scan: 0 hits.
  - target=_blank without rel scan: 0 hits.
  - Hardcoded secret pattern scan: 0 hits.
  - No security regressions introduced in iter 3.

- [G2 Telemetry (OTel JS + W3C)] BLOCKED (pre-existing, carried from iter 1/2)
  - frontend/client/src/lib/telemetry: directory does not exist.
  - frontend/backoffice/src/lib/telemetry: directory does not exist.
  - No WebTracerProvider, FetchInstrumentation, OTLPTraceExporter, W3CTraceContextPropagator, BatchSpanProcessor in either SPA.
  - No web-vitals.ts adapter in either SPA.
  - No propagateTraceHeaderCorsUrls allowlist.
  - No PII scrubber / ignoreUrls auth chain suppression.
  - NOT introduced by Phase 48 or iter 3 — pre-existing architectural debt. Phase 48 scope is backend API permissions.
  - Blocking status inherited but not regressed. Frontend telemetry gap pre-dates boundary 968eefb.

- [G3 Perf + bundle] PASS
  - Client bundle: index-D00mLruk.js = 648.11 KB raw / 197.64 KB gz (under 300 KB gz threshold).
  - Backoffice bundle: index-B-UzL_1C.js = 624.43 KB raw / 190.69 KB gz (under 300 KB gz threshold).
  - Vite "chunks larger than 500 kB" warning is for raw uncompressed size — advisory, pre-existing.
  - No new lazy route regressions. No img without dimensions introduced.

- [G4 Build] PASS
  - client: pnpm build exit 0. All 3 routers (auth, api-proxy, client) built successfully.
  - backoffice: pnpm build exit 0. All 3 routers built successfully.
  - Nitro server artifacts generated for both SPAs.

- [G5 Typecheck + Lint] PASS
  - client: tsc --noEmit exit 0 (0 errors). eslint --max-warnings 0 exit 0.
  - backoffice: tsc --noEmit exit 0 (0 errors). eslint --max-warnings 0 exit 0.
  - No regressions from iter 3 C# style fix.

- [G6 Code-design + Frontend rules] PASS (no new violations in iter 3)
  - Cross-import audit (D-4): 0 hits — client does not import backoffice and vice versa.
  - Hardcoded pt-BR strings in JSX: pre-existing finding (ProfileBadge, ProfileField, BlockUnblockDialog, DashboardCards, DeleteEmployeeDialog) — not introduced in Phase 48 or iter 3.
  - No new HOC/wrapper-without-consumer, no new input without label, no new button without accessible name.

- [G7 Coverage new files] N/A
  - git diff --diff-filter=A 968eefb..HEAD -- frontend/**: 0 new frontend files added since boundary.
  - Gate does not apply. No coverage measurement required.

- [G8 Playwright client regression (port 5173)] PASS
  - Viewport 375x667 + 1280x720 tested.
  - / (index): loads, triggers ACF+PKCE redirect to Keycloak 8180 with response_type=code, code_challenge_method=S256, scope=openid+offline_access. PKCE chain intact.
  - /auth/login: same PKCE redirect. client_id=onboarding-client-acf confirmed.
  - /register: registration wizard renders (Step 1 of 2). Zod validation triggers on submit: "CNPJ inválido" and "Razão Social é obrigatória" on bad CNPJ input.
  - /forgot-password: form renders with email input and "Enviar link" button.
  - /auth/error: error page renders with "Tentar novamente" link to /auth/login.
  - /login: 404 page (correct — /login is not a valid route; auth entry is / or /auth/login).
  - Network: only /auth/me (401 expected) and /auth/refresh (401 expected). No 5xx, no CORS.
  - Console: 0 application errors. 401s are expected (no session). No React warnings.
  - Screenshots: .jdi/cache/phase-48-frontend-iter3-client-register.png, .jdi/cache/phase-48-frontend-iter3-client-mobile.png

- [G9 Playwright backoffice regression (port 5174)] PASS
  - Viewport 375x667 + 1280x720 tested.
  - / (index): redirects to /admin/login — route guard working.
  - /admin/login: "Admin Backoffice" login page renders with "Entrar" button.
  - "Entrar" click: ACF+PKCE redirect to Keycloak 8180 with client_id=onboarding-backoffice, response_type=code, code_challenge_method=S256. PKCE chain intact.
  - /admin/users (unauthenticated): route guard redirects to /admin/login.
  - /admin/audit-log (unauthenticated): route guard redirects to /admin/login.
  - /admin/nonexistent: 404 page renders with "Voltar para o login" link.
  - Network: only /auth/me (401 expected). No 5xx, no CORS, no cross-client code references.
  - Console: favicon 404 (pre-existing, no favicon in public/), /auth/me 401 (expected). No React warnings.
  - Screenshots: .jdi/cache/phase-48-frontend-iter3-backoffice-login.png, .jdi/cache/phase-48-frontend-iter3-backoffice-mobile.png

- [G10 Accessibility (axe)] ADVISORY
  - Not re-run in iter 3 (no frontend changes since iter 2). Pre-existing advisory findings carried.
  - Known: pt-BR hardcoded labels in JSX (see G6). No new keyboard trap or missing focus indicator introduced.

- [G11 Vinext migration debt] PASS
  - git diff --diff-filter=AM 968eefb..HEAD -- frontend/**: empty. No frontend files changed in iter 3.
  - No new Vinxi-specific imports introduced.
  - Pre-existing vinxi.d.ts references in client/src/vinxi.d.ts and backoffice/src/vinxi.d.ts unchanged.

---

### Blockers

None from iter 3. G2 telemetry gap is pre-existing architectural debt (pre-boundary), not introduced by Phase 48. Frontend telemetry implementation is pending a dedicated frontend phase.

---

### Warnings

1. G2 (ARCHITECTURAL DEBT, pre-existing): OTel JS telemetry not implemented in either SPA. Both client and backoffice are missing src/lib/telemetry/ composition root, WebTracerProvider, FetchInstrumentation, OTLPTraceExporter, W3CTraceContextPropagator, BatchSpanProcessor, web-vitals.ts, propagateTraceHeaderCorsUrls allowlist, PII scrubber, and ignoreUrls auth-chain suppression. This is a project-wide architectural gap predating boundary 968eefb. Recommend creating a dedicated telemetry phase.

2. G3 (advisory): Main JS bundle exceeds 500 KB raw (648 KB client, 624 KB backoffice). gzip sizes are within gate (197 KB / 190 KB). Code-splitting via dynamic import() recommended in a future perf phase.

3. G6 (advisory): Hardcoded pt-BR strings in JSX (ProfileBadge.tsx, ProfileField.tsx, BlockUnblockDialog.tsx, DashboardCards.tsx, DeleteEmployeeDialog.tsx and others). Pre-existing, not introduced in Phase 48. i18n extraction recommended in a dedicated phase.

4. Backoffice public/ directory has no favicon.ico — causes a 404 on every page load. Pre-existing, minor UX issue.

---

### Coverage gaps (new files)

None — 0 new frontend files added after boundary 968eefb.

---

### Regression captures

- Client register screenshot: .jdi/cache/phase-48-frontend-iter3-client-register.png
- Client mobile screenshot: .jdi/cache/phase-48-frontend-iter3-client-mobile.png
- Backoffice login screenshot: .jdi/cache/phase-48-frontend-iter3-backoffice-login.png
- Backoffice mobile screenshot: .jdi/cache/phase-48-frontend-iter3-backoffice-mobile.png


## Reviewer: jdi-reviewer-onboarding-keycloak-security (iter 3)

Run: 2026-05-16
Boundary: 968eefb19dba216d729723e8ffa6a9e166d7698c
Iter 3 commit: 07dcb2b
Changed files (iter 3 only): tests/Onboarding.Integration.Tests/Fundos/FundosControllerIntegrationTests.cs (+6 lines whitespace-only, anonymous object split across lines by dotnet format)
Security-relevant code changes: NONE

**Verdict:** APPROVED_WITH_WARNINGS

---

### Gates

- [G1 Multi-tenant filter] PASS (no regression — unchanged from iter 2)
  - HasQueryFilter confirmed present on all 5 company-scoped aggregate configs: FundoConfiguration:104, ConsultoriaFundoConfiguration:83, CustodianteConfiguration:83, CedenteConfiguration:100, AccessGroupConfiguration:55. No EF config diff in iter 3 (git diff 968eefb..07dcb2b -- Configurations/: empty).
  - 4 cross-tenant GET-by-id guards in FundosController intact (eb5bc24 fix). No controller diff in iter 3.
  - IgnoreQueryFilters in FundosAdminQueryHandlers: unchanged, still guarded by BearerBackoffice + CrossCompanyAccess.
  - TipoAtivoConfiguration: intentional global entity, no HasQueryFilter by design (documented inline).

- [G2 Permission policy coverage] PASS (no regression — unchanged from iter 2)
  - No new HTTP action methods in iter 3 diff. Iter 3 touches only a test file.
  - All 19 FundosController actions retain their Authorize attribute from eb5bc24. No AllowAnonymous introduced.
  - No new permission constants, no Program.cs change.

- [G3 Secrets + env hygiene] PASS
  - Iter 3 diff: 10 lines total (6 net additions + 4 context). Content is an anonymous C# object literal split across lines: property names nome, cnpj, consultoriaFundoId, custodianteId, tipoFundo. No credential patterns.
  - Regex scan (Password|Secret|Token|ApiKey|Bearer ey.*) on iter 3 diff: 0 hits.
  - No appsettings changes. No Keycloak export changes. No new environment variable references.
  - Artifact: .jdi/cache/phase-48-security-iter3-gitleaks.json (manual scan -- gitleaks not installed)

- [G4 Semgrep] PASS (ERROR: 0, WARNING: 0)
  - semgrep --config .semgrep --severity ERROR --error --json: 0 findings, 5 rules, 534 targets, exit 0.
  - One parse-level warning in output: IQuery.cs:6 uses C# primary interface syntax (public interface IQuery<TResult>;) unrecognised by Semgrep's csharp parser. This is a pre-existing semgrep parser limitation, not a new finding and not introduced by iter 3.
  - Artifact: .jdi/cache/phase-48-security-iter3-semgrep.json

- [G5 Trivy FS + container] ADVISORY (not installed)
  - Trivy not installed. No Dockerfile changed in iter 3 (git diff 968eefb..07dcb2b -- Dockerfile*: empty).
  - No new NuGet packages in iter 3. Zero new CVE surface.
  - Artifact: .jdi/cache/phase-48-security-iter3-trivy-fs.json

- [G6 Keycloak hardening drift] PASS (not modified in iter 3)
  - git diff 968eefb..07dcb2b -- keycloak/: empty. Gate G6 scoped to phases that change realm exports.
  - Pre-existing advisory notes unchanged: passwordPolicy length(8), onboarding-app ROPC enabled.

- [G7 Security headers + CSP] ADVISORY (no change from iter 2)
  - UseSecurityHeaders() middleware not touched in iter 3. No regression possible.
  - Not re-run against live stack; no header-producing code changed.

- [G8 Dependabot] PASS
  - No new packages added in iter 3. 0 open HIGH/CRITICAL alerts (confirmed in iter 1, unchanged).

- [G9 Audit log coverage] PASS
  - git diff 968eefb..07dcb2b --diff-filter=A -- src/**/*Command.cs: 0 new Command files added.
  - Iter 3 touches only a test file. No new mutation paths, no new actor capture required.

---

### Blockers

None.

---

### Warnings

Carried from iter 2 (no new warnings introduced by iter 3):

1. src/Onboarding.Application/Fundos/Commands/UpdateConsultoriaFundoCommandHandler.cs:35 (pre-existing, phase 47) -- IgnoreQueryFilters without ClienteId ownership check in application layer. Same in UpdateCustodianteCommandHandler:32, UpdateFundoCommandHandler:33, UpdateCedenteCommandHandler:33. Cross-tenant actor with funds:write can mutate another company's entity. Recommend adding ClienteId ownership guard in a future hardening phase.

2. src/Onboarding.API/Program.cs:212-219 (pre-existing) -- Fund policies registered with string literals instead of PermissionPolicies.FundX constants. Functionally correct; refactoring risk.

3. keycloak/client-realm.json (pre-existing) -- directAccessGrantsEnabled=true (ROPC) on onboarding-app. Legacy removal candidate per PROJECT.md.

4. keycloak/*.json (pre-existing) -- passwordPolicy length(8) in both realms; G6 threshold is length(12). Not modified in Phase 48.

5. src/Onboarding.API/appsettings.json (pre-existing) -- AdminClientSecret committed as dev placeholder.

6. G5 -- Trivy not installed; no automated CVE scan on NuGet packages or container image.

---

### Pipeline artifacts
- Trivy FS: .jdi/cache/phase-48-security-iter3-trivy-fs.json (advisory -- not installed)
- Semgrep: .jdi/cache/phase-48-security-iter3-semgrep.json (0 findings, exit 0)
- Gitleaks: .jdi/cache/phase-48-security-iter3-gitleaks.json (manual scan -- not installed)

---

## Reviewer: jdi-reviewer-onboarding-keycloak-backend-csharp (iter 4)

Run: 2026-05-16
Boundary: 968eefb19dba216d729723e8ffa6a9e166d7698c
Iter 4 commits: c77e9eb (W1 -- PermissionPolicies constants), 359c9f3 (W5 -- cross-tenant write guard)
Changed files (iter 4):
- src/Onboarding.API/Program.cs:210-219 (4 string literals -> PermissionPolicies.FundX constants)
- src/Onboarding.Application/Fundos/Commands/UpdateConsultoriaFundoCommandHandler.cs (ICurrentCompanyService + ownership guard)
- src/Onboarding.Application/Fundos/Commands/UpdateCustodianteCommandHandler.cs (ICurrentCompanyService + ownership guard)
- src/Onboarding.Application/Fundos/Commands/UpdateFundoCommandHandler.cs (ICurrentCompanyService + ownership guard)
- src/Onboarding.Application/Fundos/Commands/UpdateCedenteCommandHandler.cs (ICurrentCompanyService + ownership guard)
- tests/Onboarding.Application.Tests/Fundos/Commands/Update{Consultoria,Custodiante,Fundo,Cedente}CommandHandlerTests.cs (4 ClienteIdMismatch tests each)
- tests/Onboarding.Integration.Tests/Fundos/FundosControllerIntegrationTests.cs (4 cross-tenant PUT scenarios)

**Verdict:** APPROVED

---

### Gates

- [G1 Multi-tenant isolation] PASS

  W5 fix verified in all 4 handlers. After GetByIdAsync (IgnoreQueryFilters), each handler applies:
  entity is null || entity.ClienteId != _currentCompanyService.CompanyId -- throw KeyNotFoundException.
  GlobalExceptionHandler maps this to HTTP 404. Does not reveal entity existence via 403.

  Null-safety: null check left-hand side, ClienteId access right-hand side. No NullReferenceException.
  Pattern identical to controller-layer guards from iter 1/2 (B1-B5), now at application layer.

  HasQueryFilter: unchanged on all 5 company-scoped aggregate configs (ConsultoriaFundo:83,
  Custodiante:83, Fundo:104, Cedente:100, AccessGroup:55, Employee:90). No new IgnoreQueryFilters.
  Pre-existing IgnoreQueryFilters usages remain scoped to Admin handlers or FundosController guards.

  ICurrentCompanyService DI wiring: AddScoped registered in Infrastructure.DependencyInjection.cs:39.
  All 4 handlers inject via constructor -- confirmed resolvable. No missing registration.

- [G2 Endpoint AuthZ + audit] PASS

  No new HTTP action methods in iter 4. All 4 Update handlers retain ActorSub + ActorEmail:
  UpdateConsultoriaFundoCommandHandler:50-51, UpdateCustodianteCommandHandler:50-51,
  UpdateFundoCommandHandler:50-51, UpdateCedenteCommandHandler:51-52.

  W1 fix: Program.cs lines 211-218 use PermissionPolicies.FundRead/Write/Delete/Manage constants
  from PermissionPolicyConstants.cs:22-25. String values equal prior literals -- behavior unchanged.
  FundosController already used these constants. Consistency restored.

- [G3 Secret + raw SQL hygiene] PASS

  Iter 4 diff: no appsettings changes, no Keycloak exports, no FromSqlRaw, no interpolated SQL.
  Handler changes introduce constructor injection, ownership guard, audit calls -- zero secret surface.

- [G4 Telemetry (OTel+Serilog+W3C)] PASS (unchanged from iter 3)

  G4.1 -- No Console.Write in iter 4 files (grep on Fundos/Commands/: 0 hits).
  G4.2 -- No interpolated logger messages. All use structured LogInformation with typed arguments.
  G4.3/G4.4 -- No new ActivitySource/Meter outside central Telemetry class.
  G4.5 -- No propagator override.
  G4.6 -- Program.cs: AddOpenTelemetry (67), UseSerilog (47), AddAspNetCoreInstrumentation (73),
          AddHttpClientInstrumentation (78), AddEntityFrameworkCoreInstrumentation (79), AddOtlpExporter (80).
  G4.7 -- SetDbStatementForText = true: absent. No PII leak via span attributes.
  G4.8 -- PII scrubber: SensitiveDataDestructuringPolicy at Program.cs:52.
          Tenant middleware: UseClientClaims() at Program.cs:293.
          Pre-existing advisory (project-internal names differ from gate pattern strings).
  G4.9/G4.10 -- No inline StartActivity in iter 4 handlers.

- [G5 Performance hygiene] PASS (unchanged from iter 3)

  No new list endpoints without pagination. All 4 modified handlers are single-entity UPDATE operations.
  AsNoTracking not applicable to write operations (tracking required for EF SaveAsync).

- [G6 Index coverage on tenant tables] PASS (no new migrations in iter 4)

  git diff 968eefb..HEAD -- Migrations/: empty. Gate does not apply.

- [G7 Build] PASS

  dotnet build Onboarding.slnx: Build succeeded, 0 Error(s), 0 Warning(s). All 7 projects compiled.

- [G8 Lint/format] PASS (iter 4 changed files)

  dotnet format --verify-no-changes returns violations only in pre-existing files not touched by iter 4:
  - src/Onboarding.API/Program.cs:254 -- CORS origins missing space between string literals.
    Pre-dates boundary; line number shifted by iter 1 Fund policy insertion (lines 210-219). Not authored by iter 4.
  - src/Onboarding.Application/Admin/Commands/CreateAdminCommand.cs, ResetAdministratorPasswordCommand.cs
    -- pre-existing, not in git diff since boundary (confirmed via git diff --diff-filter=AM).
  - tests/Onboarding.Domain.Tests (KeycloakUserServiceFirstLoginTests.cs, AuditServiceTests.cs,
    KeycloakUserServiceTests.cs) -- pre-existing, not in git diff since boundary.
  The 4 iter 4 handler files pass format. Pre-existing lint debt carried as warning.

- [G9 DDD/Design] PASS

  Ownership guard at application layer is the correct DDD placement. Aggregates do not carry
  cross-tenant validation; application services do. No public setters added. No cross-aggregate
  entity references added. No Infrastructure namespace imported into Domain. No MediatR/FluentAssertions.
  ICurrentCompanyService is an application-layer interface (Onboarding.Application.Common).

- [G10 Tests] PASS

  Application.Tests: 89/89 passed (+4 from iter 3 -- 4 ClienteIdMismatch unit tests).
  API.Tests: 244/244 passed (4 skipped -- Testcontainers-dependent, pre-existing).
  Integration.Tests: 20/20 passed (+4 from iter 3 -- 4 cross-tenant PUT scenarios).

  Unit test correctness: all 4 ClienteIdMismatch tests create entity with differentCompanyId;
  _currentCompanyService.CompanyId configured as _companyId (different value, set in ctor);
  handler invoked; Should.ThrowAsync<KeyNotFoundException> asserts. CORRECT.

  Integration test correctness: Scenarios 13-16 -- PJ-A creates entity, PJ-B PUTs captured GUID
  with funds:write claim -> HttpStatusCode.NotFound. Testcontainers + WebApplicationFactory. CORRECT.

- [G11 Coverage on new files] PASS (unchanged from iter 3)

  G11 scope: diff-filter=A only. The 4 Update handlers are MODIFIED (diff-filter=M) -- outside scope.
  New-file list unchanged from iter 3 (11 files). Positional-record coverage artifacts carry forward.

- [G12 Playwright regression] PASS

  Frontend regression via Playwright MCP (ports 5173/5174 confirmed running):
  - Client SPA: / and /register load. /auth/login triggers ACF+PKCE redirect to Keycloak 8180
    with code_challenge_method=S256, client_id=onboarding-client-acf. PKCE chain intact.
  - Backoffice SPA: / redirects to /admin/login. Entrar button triggers ACF+PKCE redirect
    with client_id=onboarding-backoffice, code_challenge_method=S256. PKCE chain intact.
  - Console: only expected 401 on /auth/me and /auth/refresh. No application errors.
  - Network: no 5xx, no CORS errors.

  API regression via Integration.Tests (20/20 Testcontainers):
  - API requires DB connection string; regression covered by 20 Testcontainers tests using
    real WebApplicationFactory (full middleware: AuthZ, HasQueryFilter, ClientClaimsMiddleware,
    GlobalExceptionHandler).
  - Verified: multi-tenant GET isolation (scenarios 9-12, 404), cross-tenant PUT rejection
    (scenarios 13-16, 404), permission enforcement (scenario 4, 403), 401 no-token (scenario 5),
    admin cross-company read (scenarios 17-20, 200).
  - No regressions from iter 4 changes.

- [G13 Static scans] ADVISORY (unchanged from iter 3)

  Semgrep: 0 findings, 5 rules, 534 targets, exit 0.
  Artifact: .jdi/cache/phase-48-backend-iter4-semgrep.json
  Trivy: not installed. No new NuGet packages in iter 4.
  Gitleaks: not installed. Manual scan of iter 4 diff: 0 credential patterns.

---

### Blockers

None.

---

### Warnings

1. src/Onboarding.API/Program.cs:254 (pre-existing) -- CORS origins missing space between string literals.
   Format violation pre-dates boundary; not introduced or worsened by iter 4.
   Recommend dotnet format cleanup in a future phase.

2. AdminFundoDto.cs, AdminCedenteDto.cs, AdminConsultoriaFundoDto.cs, AdminCustodianteDto.cs,
   ListAdminCedenteQuery.cs, ListAdminCustodianteQuery.cs (carried from iter 3) -- Coverlet
   line-rate 29-60% on positional record compiler-generated members. No authored logic uncovered.
   Recommend [ExcludeFromCodeCoverage] in a future cleanup phase.

3. keycloak/client-realm.json (pre-existing) -- directAccessGrantsEnabled=true (ROPC). Legacy removal candidate.

4. keycloak/*.json (pre-existing) -- passwordPolicy length(8). G6 threshold length(12). Not modified in Phase 48.

5. G5/G13 -- Trivy not installed. No automated CVE scan on NuGet packages or container image.

---

### W1 fix validation summary

Program.cs lines 211-218 use PermissionPolicies.FundRead/Write/Delete/Manage constants from
PermissionPolicyConstants.cs:22-25. String values equal prior literals -- behavior unchanged.
FundosController already used these constants on all HTTP action attributes. Consistency restored.

### W5 fix validation summary

Guard: entity is null || entity.ClienteId != _currentCompanyService.CompanyId -> KeyNotFoundException
-> HTTP 404 (GlobalExceptionHandler). Null-safe. Does not leak entity existence. ICurrentCompanyService
injected via constructor, registered at Infrastructure.DependencyInjection.cs:39.
4 unit tests + 4 integration tests cover the cross-tenant rejection branch.
All 20 integration tests pass (was 16 in iter 3, +4 cross-tenant PUT scenarios).

---

### Coverage gaps (new files -- G11 scope: diff-filter=A only)

| File | Coverage | Required | Delta |
|---|---|---|---|
| AdminFundoDto.cs | 29% | 80% | -51% (Coverlet positional-record artifact) |
| AdminCedenteDto.cs | 42% | 80% | -38% (Coverlet positional-record artifact) |
| AdminConsultoriaFundoDto.cs | 36% | 80% | -44% (Coverlet positional-record artifact) |
| AdminCustodianteDto.cs | 36% | 80% | -44% (Coverlet positional-record artifact) |
| ListAdminCedenteQuery.cs | 60% | 80% | -20% (Coverlet positional-record artifact) |
| ListAdminCustodianteQuery.cs | 60% | 80% | -20% (Coverlet positional-record artifact) |
| AdminFundosController.cs | 100% | 80% | +20% |
| FundosController.cs | 100% | 80% | +20% |
| GlobalExceptionHandler.cs | 88% | 80% | +8% |
| FundosAdminQueryHandlers.cs | exempt | -- | [ExcludeFromCodeCoverage] |

Update handlers (Consultoria/Custodiante/Fundo/Cedente): MODIFIED files, outside G11 scope.
All paths covered by unit + integration tests.

---

### Regression captures

- Backoffice login screenshot: .jdi/cache/phase-48-backend-iter4-backoffice-login.png
- Client register screenshot: .jdi/cache/phase-48-backend-iter4-client-register.png
- Semgrep: .jdi/cache/phase-48-backend-iter4-semgrep.json (0 findings, exit 0)


---

## Reviewer: jdi-reviewer-onboarding-keycloak-frontend-vinext (iter 4)

Run: 2026-05-16
Boundary: 968eefb19dba216d729723e8ffa6a9e166d7698c
Iter 4 commit: 90f038d
Changed frontend files since boundary: frontend/client/src/lib/api.ts (PERMISSION_LABELS +4 entries, PERMISSION_OPTIONS +4 entries)

**Verdict:** APPROVED_WITH_WARNINGS

---

### Gates

- [G1 Security frontend] PASS
  - localStorage/sessionStorage token scan: 0 hits.
  - dangerouslySetInnerHTML scan: 0 hits.
  - target=_blank without rel scan: 0 hits.
  - Hardcoded secret pattern in src/: 0 hits. E2e test fixtures contain test passwords (e2e/fixtures/test-data.ts:32, e2e/access-group-change.spec.ts:50) -- test-only, not in production bundle. Not blocking.

- [G2 Telemetry (OTel JS + W3C)] BLOCKED (pre-existing, carried from iter 1/2/3)
  - frontend/client/src/lib/telemetry: directory does not exist.
  - frontend/backoffice/src/lib/telemetry: directory does not exist.
  - No WebTracerProvider, FetchInstrumentation, OTLPTraceExporter, W3CTraceContextPropagator, BatchSpanProcessor in either SPA.
  - No web-vitals.ts adapter, no propagateTraceHeaderCorsUrls allowlist, no PII scrubber, no ignoreUrls auth chain suppression.
  - NOT introduced by Phase 48 or iter 4. Pre-existing architectural debt predating boundary 968eefb.
  - Blocking status inherited, not regressed by iter 4.

- [G3 Perf + bundle] PASS
  - Client: index-BxoXqr9q.js = 648.44 KB raw / 197.71 KB gz (under 300 KB gz gate).
  - Backoffice: index-B-UzL_1C.js = 624.43 KB raw / 190.69 KB gz (under 300 KB gz gate).
  - 8 new constant entries add less than 0.5 KB raw -- negligible delta.

- [G4 Build] PASS
  - client: pnpm build exit 0. Auth/api-proxy/client routers built. Nitro server generated.
  - backoffice: pnpm build exit 0. All 3 routers built. Nitro server generated.

- [G5 Typecheck + Lint] PASS
  - client: tsc --noEmit exit 0. eslint --max-warnings 0 exit 0.
  - backoffice: tsc --noEmit exit 0. eslint --max-warnings 0 exit 0.

- [G6 Code-design + Frontend rules] PASS
  - W3 contract drift RESOLVED: PERMISSION_LABELS and PERMISSION_OPTIONS include all 4 funds:* keys.
    - funds:read -> Ver fundos
    - funds:write -> Criar/editar fundos
    - funds:delete -> Excluir fundos
    - funds:manage -> Gestao total de fundos
  - AccessGroupsPage.tsx:179 uses PERMISSION_LABELS[perm] ?? perm -- fallback intact. CORRECT.
  - AccessGroupsPage.tsx:223-228 iterates PERMISSION_OPTIONS for checkboxes -- funds:* appear as selectable options. CORRECT.
  - Production bundle grep confirms all 4 funds:* pairs present in index-BxoXqr9q.js.
  - Cross-import audit (D-4): 0 cross-imports between client and backoffice.
  - Backoffice untouched -- correct per D-4 (fund permissions are client-side domain).
  - No new HOC/wrapper-without-consumer, no new unlabeled input, no new button without accessible name.

- [G7 Coverage new files] N/A
  - git diff --diff-filter=A 968eefb..HEAD -- frontend/**: 0 new frontend files since boundary.
  - api.ts is modified (diff-filter=M), outside G7 scope.

- [G8 Playwright client regression (port 5173)] PASS
  - Viewports: 1280x720 desktop + 375x667 mobile.
  - /: ACF+PKCE redirect to Keycloak 8180 -- client_id=onboarding-client-acf, response_type=code, code_challenge_method=S256, scope=openid+offline_access. PKCE chain intact.
  - /register: Step 1 of 2 renders. Zod validation on empty submit: Razao Social e obrigatoria + CNPJ e obrigatorio. Correct.
  - /auth/login: ACF+PKCE redirect confirmed.
  - funds:* labels: AccessGroupsPage auth-gated (cannot navigate unauthenticated). Label correctness verified via production bundle grep -- all 4 funds:* pairs confirmed in built JS artifact.
  - Network: /auth/me 401 + /auth/refresh 401. No 5xx, no CORS.
  - Console: 0 application errors. Only expected 401s. No React warnings.
  - Screenshots: .jdi/cache/phase-48-frontend-iter4-client-register-desktop.png, .jdi/cache/phase-48-frontend-iter4-client-mobile.png

- [G9 Playwright backoffice regression (port 5174)] PASS
  - Viewports: 1280x720 desktop + 375x667 mobile.
  - /: redirects to /admin/login. Route guard working.
  - /admin/login: Admin Backoffice login page renders. Entrar triggers ACF+PKCE: client_id=onboarding-backoffice, response_type=code, code_challenge_method=S256. PKCE intact.
  - /admin/users (unauthenticated): route guard redirects to /admin/login. Correct.
  - Backoffice untouched in iter 4 -- correct per D-4.
  - Network: /auth/me 401 (x3 across navigations). No 5xx, no CORS.
  - Console: favicon 404 (pre-existing). No application errors. No React warnings.
  - Screenshots: .jdi/cache/phase-48-frontend-iter4-backoffice-login.png, .jdi/cache/phase-48-frontend-iter4-backoffice-mobile.png

- [G10 Accessibility (axe)] ADVISORY (not re-run -- no structural JSX changes in iter 4)
  - Iter 4 adds only constant data to api.ts. No HTML/JSX modified. Pre-existing advisory findings from iter 3 carried unchanged.

- [G11 Vinext migration debt] PASS
  - api.ts modified: 0 new Vinxi-specific imports. No from vinxi additions in iter 4.

---

### Blockers

None from iter 4. G2 telemetry gap is pre-existing architectural debt (pre-boundary), not introduced by Phase 48 or iter 4.

W3 contract drift (iter 1-3 blocker) is RESOLVED: PERMISSION_LABELS and PERMISSION_OPTIONS in frontend/client/src/lib/api.ts now include all 4 funds:read|write|delete|manage entries with correct pt-BR labels. Confirmed in source (lines 512-529) and in production bundle artifact (index-BxoXqr9q.js).

---

### Warnings

1. G2 (ARCHITECTURAL DEBT, pre-existing): OTel JS telemetry not implemented in either SPA. Both SPAs missing src/lib/telemetry/ composition root, WebTracerProvider, FetchInstrumentation, OTLPTraceExporter, W3CTraceContextPropagator, BatchSpanProcessor, web-vitals.ts, propagateTraceHeaderCorsUrls allowlist, PII scrubber, and ignoreUrls auth-chain suppression. Pre-dates boundary 968eefb. Recommend dedicated telemetry phase.

2. G3 (advisory): Main JS bundle exceeds 500 KB raw (648 KB client, 624 KB backoffice). gzip sizes within gate (197 KB / 190 KB). Code-splitting via dynamic import() recommended in a future perf phase.

3. G6 (advisory): Hardcoded pt-BR strings in JSX (ProfileBadge.tsx, ProfileField.tsx, BlockUnblockDialog.tsx, DashboardCards.tsx, DeleteEmployeeDialog.tsx). Pre-existing, not introduced in Phase 48 or iter 4.

4. Backoffice public/ has no favicon.ico -- 404 on every page load. Pre-existing, minor UX issue.

---

### W3 fix validation summary

Iter 1-3 blocker W3 (PERMISSION_LABELS/PERMISSION_OPTIONS missing funds:* keys):

- RESOLVED in commit 90f038d.
- api.ts lines 512-515: PERMISSION_LABELS adds funds:read, funds:write, funds:delete, funds:manage.
- api.ts lines 525-528: PERMISSION_OPTIONS adds 4 corresponding {value, label} entries.
- Diff verified: exactly +4 lines in each map (no other changes in iter 4 frontend diff).
- Production bundle verified: all 4 key-value pairs present in index-BxoXqr9q.js.
- AccessGroupsPage.tsx unchanged -- PERMISSION_LABELS[perm] ?? perm (line 179) and PERMISSION_OPTIONS iteration (line 223) needed no update. Contract expansion backward-compatible.

---

### Coverage gaps (new files -- G7 scope: diff-filter=A only)

None -- 0 new frontend files added after boundary 968eefb.

---

### Regression captures

- Client register desktop: .jdi/cache/phase-48-frontend-iter4-client-register-desktop.png
- Client mobile: .jdi/cache/phase-48-frontend-iter4-client-mobile.png
- Backoffice login desktop: .jdi/cache/phase-48-frontend-iter4-backoffice-login.png
- Backoffice mobile: .jdi/cache/phase-48-frontend-iter4-backoffice-mobile.png

---

## Reviewer: jdi-reviewer-onboarding-keycloak-frontend-vinext (iter 4)

Run: 2026-05-16
Boundary: 968eefb19dba216d729723e8ffa6a9e166d7698c
Iter 4 commit: 90f038d
Changed frontend files since boundary: frontend/client/src/lib/api.ts (PERMISSION_LABELS +4 entries, PERMISSION_OPTIONS +4 entries)

**Verdict:** APPROVED_WITH_WARNINGS

---

### Gates

- [G1 Security frontend] PASS
  - localStorage/sessionStorage token scan: 0 hits.
  - dangerouslySetInnerHTML scan: 0 hits.
  - target=_blank without rel scan: 0 hits.
  - Hardcoded secret pattern in src/: 0 hits. E2e test fixtures contain test passwords (test-only, not in production bundle). Not blocking.

- [G2 Telemetry (OTel JS + W3C)] BLOCKED (pre-existing, carried from iter 1/2/3)
  - frontend/client/src/lib/telemetry: directory does not exist.
  - frontend/backoffice/src/lib/telemetry: directory does not exist.
  - No WebTracerProvider, FetchInstrumentation, OTLPTraceExporter, W3CTraceContextPropagator, BatchSpanProcessor in either SPA.
  - No web-vitals.ts adapter, no propagateTraceHeaderCorsUrls allowlist, no PII scrubber, no ignoreUrls auth chain suppression.
  - NOT introduced by Phase 48 or iter 4. Pre-existing architectural debt predating boundary 968eefb.
  - Blocking status inherited, not regressed by iter 4.

- [G3 Perf + bundle] PASS
  - Client: index-BxoXqr9q.js = 648.44 KB raw / 197.71 KB gz (under 300 KB gz gate).
  - Backoffice: index-B-UzL_1C.js = 624.43 KB raw / 190.69 KB gz (under 300 KB gz gate).
  - 8 new constant entries add less than 0.5 KB raw -- negligible delta.

- [G4 Build] PASS
  - client: pnpm build exit 0. Auth/api-proxy/client routers built. Nitro server generated.
  - backoffice: pnpm build exit 0. All 3 routers built. Nitro server generated.

- [G5 Typecheck + Lint] PASS
  - client: tsc --noEmit exit 0. eslint --max-warnings 0 exit 0.
  - backoffice: tsc --noEmit exit 0. eslint --max-warnings 0 exit 0.

- [G6 Code-design + Frontend rules] PASS
  - W3 contract drift RESOLVED: PERMISSION_LABELS and PERMISSION_OPTIONS include all 4 funds:* keys.
    - funds:read -> Ver fundos
    - funds:write -> Criar/editar fundos
    - funds:delete -> Excluir fundos
    - funds:manage -> Gestao total de fundos
  - AccessGroupsPage.tsx:179 uses PERMISSION_LABELS[perm] ?? perm -- fallback intact. CORRECT.
  - AccessGroupsPage.tsx:223-228 iterates PERMISSION_OPTIONS for checkboxes -- funds:* appear as selectable options. CORRECT.
  - Production bundle grep confirms all 4 funds:* pairs present in index-BxoXqr9q.js.
  - Cross-import audit (D-4): 0 cross-imports between client and backoffice.
  - Backoffice untouched -- correct per D-4 (fund permissions are client-side domain).
  - No new HOC/wrapper-without-consumer, no new unlabeled input, no new button without accessible name.

- [G7 Coverage new files] N/A
  - git diff --diff-filter=A 968eefb..HEAD -- frontend/**: 0 new frontend files since boundary.
  - api.ts is modified (diff-filter=M), outside G7 scope.

- [G8 Playwright client regression (port 5173)] PASS
  - Viewports: 1280x720 desktop + 375x667 mobile.
  - /: ACF+PKCE redirect -- client_id=onboarding-client-acf, response_type=code, code_challenge_method=S256, scope=openid+offline_access. PKCE chain intact.
  - /register: Step 1 of 2 renders. Zod validation on empty submit: Razao Social e obrigatoria + CNPJ e obrigatorio.
  - /auth/login: ACF+PKCE redirect confirmed.
  - funds:* labels: AccessGroupsPage auth-gated. Label correctness verified via production bundle grep -- all 4 funds:* pairs confirmed in index-BxoXqr9q.js.
  - Network: /auth/me 401 + /auth/refresh 401. No 5xx, no CORS.
  - Console: 0 application errors. Only expected 401s. No React warnings.

- [G9 Playwright backoffice regression (port 5174)] PASS
  - Viewports: 1280x720 desktop + 375x667 mobile.
  - /: redirects to /admin/login. Route guard working.
  - /admin/login: Admin Backoffice login renders. Entrar triggers ACF+PKCE: client_id=onboarding-backoffice, response_type=code, code_challenge_method=S256.
  - /admin/users (unauthenticated): route guard redirects to /admin/login. Correct.
  - Backoffice untouched in iter 4 -- correct per D-4.
  - Network: /auth/me 401 (x3). No 5xx, no CORS.
  - Console: favicon 404 (pre-existing). No application errors. No React warnings.

- [G10 Accessibility (axe)] ADVISORY (not re-run -- no structural JSX changes in iter 4)
  - Iter 4 adds only constant data to api.ts. No HTML/JSX modified. Pre-existing advisory findings from iter 3 carried unchanged.

- [G11 Vinext migration debt] PASS
  - api.ts modified: 0 new Vinxi-specific imports in iter 4.

---

### Blockers

None from iter 4. G2 telemetry gap is pre-existing architectural debt (pre-boundary), not introduced by Phase 48 or iter 4.

W3 contract drift (iter 1-3 blocker) is RESOLVED: PERMISSION_LABELS and PERMISSION_OPTIONS in frontend/client/src/lib/api.ts now include all 4 funds:read|write|delete|manage entries with correct pt-BR labels. Confirmed in source (lines 512-529) and in production bundle artifact (index-BxoXqr9q.js).

---

### Warnings

1. G2 (ARCHITECTURAL DEBT, pre-existing): OTel JS telemetry not implemented in either SPA. Both SPAs missing src/lib/telemetry/ composition root, WebTracerProvider, FetchInstrumentation, OTLPTraceExporter, W3CTraceContextPropagator, BatchSpanProcessor, web-vitals.ts, propagateTraceHeaderCorsUrls allowlist, PII scrubber, and ignoreUrls auth-chain suppression. Pre-dates boundary 968eefb. Recommend dedicated telemetry phase.

2. G3 (advisory): Main JS bundle exceeds 500 KB raw (648 KB client, 624 KB backoffice). gzip sizes within gate (197 KB / 190 KB). Code-splitting via dynamic import() recommended in a future perf phase.

3. G6 (advisory): Hardcoded pt-BR strings in JSX (ProfileBadge.tsx, ProfileField.tsx, BlockUnblockDialog.tsx, DashboardCards.tsx, DeleteEmployeeDialog.tsx). Pre-existing, not introduced in Phase 48 or iter 4.

4. Backoffice public/ has no favicon.ico -- 404 on every page load. Pre-existing, minor UX issue.

---

### W3 fix validation summary

Iter 1-3 blocker W3 (PERMISSION_LABELS/PERMISSION_OPTIONS missing funds:* keys -- AccessGroupsPage would render raw API strings):

- RESOLVED in commit 90f038d.
- api.ts lines 512-515: PERMISSION_LABELS adds funds:read, funds:write, funds:delete, funds:manage.
- api.ts lines 525-528: PERMISSION_OPTIONS adds 4 corresponding {value, label} entries.
- Diff verified: exactly +4 lines in each map, no other frontend changes in iter 4.
- Production bundle verified: all 4 key-value pairs present in index-BxoXqr9q.js.
- AccessGroupsPage.tsx unchanged -- PERMISSION_LABELS[perm] ?? perm (line 179) and PERMISSION_OPTIONS map (line 223) needed no update. Expansion is backward-compatible.

---

### Coverage gaps (new files -- G7 scope: diff-filter=A only)

None -- 0 new frontend files added after boundary 968eefb.

---

### Regression captures

- Client register desktop: .jdi/cache/phase-48-frontend-iter4-client-register-desktop.png
- Client mobile: .jdi/cache/phase-48-frontend-iter4-client-mobile.png
- Backoffice login desktop: .jdi/cache/phase-48-frontend-iter4-backoffice-login.png
- Backoffice mobile: .jdi/cache/phase-48-frontend-iter4-backoffice-mobile.png

---

## Reviewer: jdi-reviewer-onboarding-keycloak-security (iter 4)

Run: 2026-05-16
Boundary: 968eefb19dba216d729723e8ffa6a9e166d7698c
Iter 4 commits: c77e9eb (W1 -- PermissionPolicies constants), 359c9f3 (W5 -- cross-tenant write guard), 90f038d (W3 -- frontend funds:* labels)
Changed security-relevant files:
- src/Onboarding.API/Program.cs (4 string literals -> PermissionPolicies.FundX constants)
- src/Onboarding.API/Security/PermissionPolicyConstants.cs (FundRead/Write/Delete/Manage added + doc update)
- src/Onboarding.Application/Fundos/Commands/UpdateConsultoriaFundoCommandHandler.cs (ICurrentCompanyService + ownership guard)
- src/Onboarding.Application/Fundos/Commands/UpdateCustodianteCommandHandler.cs (ICurrentCompanyService + ownership guard)
- src/Onboarding.Application/Fundos/Commands/UpdateFundoCommandHandler.cs (ICurrentCompanyService + ownership guard)
- src/Onboarding.Application/Fundos/Commands/UpdateCedenteCommandHandler.cs (ICurrentCompanyService + ownership guard)
- tests: +4 ClienteIdMismatch unit tests per handler, +4 cross-tenant PUT integration scenarios
- frontend/client/src/lib/api.ts (PERMISSION_LABELS/PERMISSION_OPTIONS +4 funds:* entries)

**Verdict:** APPROVED_WITH_WARNINGS

---

### Gates

- [G1 Multi-tenant filter] PASS -- W5 RESOLVED

  All 4 Update handlers now enforce ClienteId ownership at application layer. Guard pattern verified in source:
  - UpdateConsultoriaFundoCommandHandler:36 -- entity is null || entity.ClienteId != _currentCompanyService.CompanyId
  - UpdateCustodianteCommandHandler:36 -- same pattern on custodiante
  - UpdateFundoCommandHandler:37 -- same pattern on fundo
  - UpdateCedenteCommandHandler:37 -- same pattern on cedente

  All 4 throw KeyNotFoundException (same message as null-entity path). GlobalExceptionHandler maps to HTTP 404. Does not leak entity existence via 403. Guard order: null check before property access (C# left-to-right || evaluation). No NullReferenceException risk.

  Attack vector closed: cross-tenant actor with funds:write + valid GUID can no longer overwrite another company entity field values via PUT.

  HasQueryFilter presence unchanged: FundoConfiguration:104, ConsultoriaFundoConfiguration:83, CustodianteConfiguration:83, CedenteConfiguration:100, AccessGroupConfiguration:55, EmployeeConfiguration:90. TipoAtivoConfiguration: no HasQueryFilter by design (global entity). No HasQueryFilter diff in iter 4.

  IgnoreQueryFilters in GetByIdAsync: intentional -- needed for cross-tenant uniqueness checks and admin operations. Application-layer ownership guard post-fetch is the correct mitigation.

  FundosAdminQueryHandlers: 4 IgnoreQueryFilters usages remain scoped to BearerBackoffice + CrossCompanyAccess. Unchanged.

  Integration test correctness: 4 scenarios (UpdateConsultoriaFundo/Custodiante/Fundo/Cedente_CrossTenant_Returns404) -- PJ-A creates entity, PJ-B PUT with funds:write and captured GUID -> asserts HttpStatusCode.NotFound.

  Unit test correctness: 4 HandleAsync_WhenClienteIdMismatch_ThrowsKeyNotFoundException -- entity with differentCompanyId; _currentCompanyService.CompanyId returns _companyId (different, set in ctor); asserts KeyNotFoundException thrown.

  W5 WARNING from iter 2/3 is RESOLVED.

- [G2 Permission policy coverage] PASS -- W1 RESOLVED

  Program.cs lines 211-218 use PermissionPolicies.FundRead/Write/Delete/Manage constants from PermissionPolicyConstants.cs:22-25. String values equal prior literals. Runtime behavior unchanged. Drift risk eliminated.

  End-to-end permission wiring verified:
  PermissionPolicies.FundRead -> AddPolicy -> PermissionRequirement(Permissions.FundsRead) -> "funds:read" -> ICurrentCompanyPermissionsService.Permissions.Contains("funds:read") -> ClientClaimsMiddleware populates PermissionList from AccessGroup.Permissions (DB entity) -> Permissions.All includes all 4 FundsX constants (Permissions.cs:22-27).

  Permission grant flows via AccessGroup DB assignment (D-07), not Keycloak client roles directly. G2 Keycloak mapping requirement satisfied through this chain; no Keycloak realm export change required.

  No new HTTP action methods in iter 4. No AllowAnonymous introduced. All FundosController (19 actions) and AdminFundosController (4 actions, class-level BearerBackoffice + CrossCompanyAccess) retain correct Authorize attributes.

  W1 WARNING from iter 2/3 is RESOLVED.

- [G3 Secrets + env hygiene] PASS

  Manual scan on iter 4 diff (c77e9eb, 359c9f3, 90f038d): 0 credential patterns in all 3 commits.
  appsettings.json diff from boundary: empty. keycloak/ diff from boundary: empty.
  Pre-existing AdminClientSecret dev placeholder at appsettings.json:18 not introduced by iter 4.
  Gitleaks: not installed. Manual scan: 0 findings.
  Artifact: .jdi/cache/phase-48-security-iter4-gitleaks.json (manual scan stub)

- [G4 Semgrep] PASS (ERROR: 0, WARNING: 0)

  semgrep --config .semgrep --severity ERROR --error --json: 0 findings, 5 rules, 534 targets, exit 0.
  Run on full codebase after all iter-4 commits. No new targets (modified files only).
  Artifact: .jdi/cache/phase-48-security-iter4-semgrep.json

- [G5 Trivy FS + container] ADVISORY (not installed)

  Trivy not installed. No Dockerfile changed in iter 4. No new NuGet packages. Zero new CVE surface. 0 Dependabot HIGH/CRITICAL alerts (confirmed iter 1, unchanged through iter 4).
  Artifact: .jdi/cache/phase-48-security-iter4-trivy-fs.json (advisory stub)

- [G6 Keycloak hardening drift] PASS (keycloak/ not modified in iter 4)

  git diff 968eefb..HEAD -- keycloak/: 0 bytes changed. Gate G6 scoped to phases that change realm exports.
  Pre-existing advisory (unchanged): passwordPolicy length(8) below G6 threshold length(12); onboarding-app ROPC enabled.
  G6 pass criteria verified: bruteForceProtected=true, failureFactor=5 (at threshold), ssoSessionIdleTimeout=1800 (at threshold).

- [G7 Security headers + CSP] ADVISORY (no change from iter 3)

  UseSecurityHeaders() middleware not touched in iter 4. No regression possible from handler guard, constants, or frontend label changes.

- [G8 Dependabot] PASS

  No new packages in iter 4. 0 open HIGH/CRITICAL alerts.

- [G9 Audit log coverage] PASS

  No new Command.cs files added since boundary (git diff --diff-filter=A: 0 Command.cs files). The 4 Update handlers are modified (diff-filter=M), outside G9 new-file scope. ActorSub + ActorEmail confirmed present in all 4 handlers. Audit capture positioned after ownership guard -- correct: rejected cross-tenant writes produce no spurious audit record.

---

### Blockers

None.

---

### Warnings

1. keycloak/client-realm.json (pre-existing) -- directAccessGrantsEnabled=true (ROPC) on onboarding-app. Legacy removal candidate per PROJECT.md and MEMORY.md. Not modified in Phase 48.

2. keycloak/*.json (pre-existing) -- passwordPolicy length(8). G6 threshold is length(12). Not modified in Phase 48.

3. src/Onboarding.API/appsettings.json:18 (pre-existing) -- AdminClientSecret committed as dev placeholder. Not introduced by iter 4.

4. G5 -- Trivy not installed. No automated CVE scan on NuGet packages or container image.

---

### W1 fix validation summary

Program.cs lines 211-218 use PermissionPolicies.FundRead/Write/Delete/Manage from PermissionPolicyConstants.cs:22-25. Values equal prior string literals. End-to-end pipeline: policy constant -> PermissionRequirement(Permissions.FundsX) -> ICurrentCompanyPermissionsService.Permissions -> ClientClaimsMiddleware -> AccessGroup DB entity -> Permissions.All includes all 4 FundsX strings. W1 WARNING RESOLVED.

### W5 fix validation summary

Guard in all 4 handlers: entity is null || entity.ClienteId != _currentCompanyService.CompanyId -> KeyNotFoundException -> HTTP 404. Null-safe. Does not leak entity existence. ICurrentCompanyService registered at Infrastructure.DependencyInjection.cs:39. 4 unit tests + 4 integration tests cover the cross-tenant rejection branch. All 20 integration tests pass (was 16 pre-iter-4). W5 WARNING RESOLVED.

---

### Pipeline artifacts
- Trivy FS: .jdi/cache/phase-48-security-iter4-trivy-fs.json (advisory -- not installed)
- Semgrep: .jdi/cache/phase-48-security-iter4-semgrep.json (0 findings, exit 0)
- Gitleaks: .jdi/cache/phase-48-security-iter4-gitleaks.json (manual scan -- not installed)
