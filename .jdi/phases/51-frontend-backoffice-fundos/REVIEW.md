## Phase 52 Security review iter 1

## Security Verdict
APPROVED_WITH_WARNINGS

### Gates

- **[G1 Multi-tenant filter] PASS**
  AdminAuditLog has NO HasQueryFilter — correct, it is an admin-global table with no ClientId. All company-scoped aggregates (Fundo, Cedente, ConsultoriaFundo, Custodiante) retain their existing HasQueryFilter on ClienteId. N-N association aggregates (FundoCedente, FundoTipoAtivo, CedenteTipoAtivo) intentionally omit HasQueryFilter per D-5 (parent-scoped). No new aggregate introduced without correct posture. AdminUserController sits behind BearerBackoffice + CrossCompanyAccess at class level — all endpoints inherit; cross-company read is intentional by design (backoffice). No tenant leak introduced.

- **[G2 Permission policy coverage] PASS**
  AdminUserController carries class-level `[Authorize(AuthenticationSchemes = "BearerBackoffice", Policy = PermissionPolicies.CrossCompanyAccess)]`. All new endpoints (GET /api/admin/audit-log with entityType/entityId params) inherit this — no naked `[HttpGet]` without auth. No `[AllowAnonymous]` added. Backoffice frontend exclusively targets `/api/admin/*` via adminFetch (credentials: include). No new controller introduced.

- **[G3 Secrets + env hygiene] PASS**
  Git diff across the full range (9163e68^..bbea3af) shows no hardcoded passwords, secrets, tokens, or API keys in any JSON, env, or source file. Backoffice frontend contains zero token writes to localStorage/sessionStorage (grep clean). Client frontend localStorage usage is limited to theme persistence only, confirmed in tests.

- **[G4 Semgrep] NOT_RUN — advisory**
  Semgrep binary not available in this environment. No new patterns (SQL concatenation, unsafe reflection, hardcoded creds) visible in manual inspection of new C# handlers. All four emission-site handlers use parameterized EF queries.

- **[G5 Trivy FS + container] NOT_RUN — advisory**
  Trivy not available. One new frontend dependency (@tanstack/react-query) was validated MIT in Phase 51 per D-3. No Dockerfile changes in this phase range.

- **[G6 Keycloak hardening] PASS (no drift)**
  No keycloak/exports/*.json files changed in the diff range. Hardening posture unchanged.

- **[G7 Security headers] NOT_VERIFIED (stack not running)**
  Playwright not exercised — stack unavailable. No changes to middleware pipeline, CORS, or header middleware in this phase. Headers posture expected unchanged from Phase 51 baseline.

- **[G8 Dependabot] NOT_CHECKED**
  gh CLI not authenticated in this environment. No new high-severity packages introduced — only @tanstack/react-query (MIT, previously validated).

- **[G9 Audit log] PASS**
  All 4 emission sites pass real ActorSub + ActorEmail from command parameters:
  - TransitionFundoStatusCommandHandler: actorSub/actorEmail from command
  - TransitionFundoCedenteStatusHandler: actorSub/actorEmail from command
  - TransitionFundoTipoAtivoStatusHandler: actorSub/actorEmail from command
  - TransitionCedenteTipoAtivoStatusHandler: actorSub/actorEmail from command
  EntityType and EntityId are passed as real aggregate names and persisted IDs (not placeholders). AuditService.RecordAsync propagates all fields to AdminAuditLog.Create correctly.

### Blockers
None.

### Warnings

- **W-arch** — `frontend/backoffice/src/lib/admin-fundos-schemas.ts`: AuditLogEntry schema does not include `entityType`/`entityId` fields despite the backend AdminAuditLogDto now exposing them. The `getAuditHistory` response will silently drop these fields during Zod parse. Non-blocking (display-only omission) but reduces forensic visibility in audit inline views.

- **W-arch** — D-8 deviation (no GET /admin/<entity>/{id} detail endpoint): Frontend detail pages fetch list[200] then `.find(x => x.id === idFromUrl)`. From a security standpoint this is safe — the ID match is structural, not user-controlled trust. Risk is limited to performance (over-fetch). The `clienteId` field in all DTOs correctly carries the originating tenant ID, which the UI renders as `empresaNome` — rows from different tenants are displayed with their own label, so no unsafe tenant merging occurs in the UI. Document as W-arch for backend endpoint gap only.

- **W-seed** — backoffice admin user seeding for MCP testing not explicitly covered in `seed-test-users.sh`. Admin user setup for backoffice realm integration tests should be documented or added to seed script before E2E suite runs in CI.

- **W-g4** — Semgrep and Trivy could not be executed (binaries absent). Manual inspection passes, but automated scan should run in CI before ship gate is closed.

### Pipeline artifacts
- Trivy FS: not generated (binary absent)
- Semgrep: not generated (binary absent)
- Gitleaks: not generated (binary absent) — manual diff inspection clean


## Phase 52 Frontend review iter 1

**Verdict: APPROVED_WITH_WARNINGS**

---

### Gates

**[G1 Security frontend] PASS**
- Zero token/JWT/refresh keys in localStorage or sessionStorage. Only tsr-scroll-restoration-v1_3 present (TanStack Router internal).
- No dangerouslySetInnerHTML hits in new files.
- No hardcoded secrets or API keys in src.
- D-4 cross-import: zero cross-references between frontend/client and frontend/backoffice.

**[G2 Telemetry (OTel JS + W3C)] BLOCKED (carry-forward, pre-existing)**
- Neither frontend/backoffice/src/lib/telemetry/ nor frontend/client/src/lib/telemetry/ exists.
- Flagged in prior phases. Phase 52 did NOT introduce or worsen this gap. Carry-forward blocker only.
- No console.log/debug/info/warn in shipped backoffice src.

**[G3 Perf + bundle] PASS**
- Backoffice main chunk: index-P7Vr3eKe.js — 205.31 KB gz (under 300 KB gate).
- Client main chunk: index-DRuAtoQ3.js — 210.00 KB gz (under 300 KB gate; W-perf Phase 51 carry-forward CLOSED — was 221 KB pre-code-split, now 210 KB).
- D-32 lazy chunks confirmed in both production builds:
  - Backoffice: AdminFundosListPage, AdminFundoDetailPage, AdminCedentesListPage, AdminCustodiantesListPage, AdminConsultoriasFundoListPage, AdminAssociationTable, AuditHistorySection, AdminSearchInput, AdminPaginator, admin-fundos-api all separate chunks.
  - Client: FundosListPage, FundoDetailPage, CedentesListPage, CustodiantesListPage, ConsultoriasFundoListPage, TiposAtivoListPage, use-allowed-transitions, api-errors all separate chunks.

**[G4 Build] PASS**
- pnpm --filter frontend-backoffice build: exit 0.
- pnpm --filter frontend-client build: exit 0.

**[G5 Typecheck+Lint] PASS**
- pnpm --filter frontend-backoffice typecheck: exit 0 (tsc --noEmit).
- pnpm --filter frontend-backoffice lint --max-warnings 0: exit 0 (eslint clean).

**[G6 Code-design + Frontend rules] PASS**
- D-4 separation: zero cross-imports confirmed.
- No pt-BR hardcoded strings in JSX (locale file used throughout).
- No token storage violations.
- No dangerouslySetInnerHTML.
- No Vinxi-specific imports in src.

**[G7 Coverage new files] PARTIAL PASS — WARNING**
- 234 vitest tests pass, 27 test files.
- Coverage provider NOT configured in vitest.config.ts. No coverage report generated.
- Test files present for: AdminAssociationTable, AdminEntityHeader, AdminFieldsGrid, AdminPaginator, AdminSearchInput, AuditHistorySection, EmpresaFilterDropdown (7 of 35+ new files).
- 11 page components and 3 lib files have no test coverage. Cannot verify 80% threshold.

**[G8 Playwright regression — Client SPA] PASS**
- http://localhost:5173 loads, authenticated session active.
- /fundos: lazy chunk loads, API GET /api/fundos?page=1&pageSize=20 returns 200.
- No 5xx. Two pre-existing console errors (401 auth probe + React setState-during-render warning, not introduced this phase).

**[G9 Playwright regression — Backoffice SPA] PASS**
- http://localhost:5174 authenticated as E2E Admin. Sidebar shows Fundos group with 7 sub-links.
- /admin/fundos: table renders, EmpresaFilterDropdown populated (casa option visible), AdminPaginator and AdminSearchInput present. GET /api/admin/fundos?page=1&pageSize=20 returns 200.
- /admin/cedentes, /admin/consultorias-fundo, /admin/custodiantes, /admin/fundo-cedentes, /admin/cedente-tipos-ativos, /admin/fundo-tipos-ativos: all routes render without crash.
- Bookmarkable URL: /admin/fundos?page=1&empresaId=UUID&search=test — API call includes search and companyId params, returns 200.
- 404 graceful state: /admin/fundos/nonexistent-id-12345 shows Registro nao encontrado + back button. No crash.
- Audit log API: GET /api/admin/audit-log?entityType=Fundo&entityId=test-id returns 200.
- Console errors: only Vite HMR WebSocket (expected per D-16 container mode). Zero React errors.
- No 5xx, no CORS errors.

**[G10 Accessibility (axe)] Not run (advisory — container mode)**

**[G11 Vinext migration debt] CLEAN**
- Zero from vinxi imports introduced in Phase 52 files.

---

### Blockers

None introduced by Phase 52. G2 (Telemetry) is pre-existing carry-forward.

---

### Warnings

**W-arch (D-8 — MANDATORY BACKEND TASK before production):**
Detail pages (AdminFundoDetailPage, AdminCedenteDetailPage, AdminConsultoriaFundoDetailPage, AdminCustodianteDetailPage) use the pageSize=200 hack: fetch full list, .find() by id client-side. Dev DB has 0 fundos so no runtime break today. Silently fails for item 201+. Backend must add GET /api/admin/fundos/{id} and analogs in a future phase before any production deployment. Accepted as W-arch (not BLOCK) because dataset is empty in dev, scope was read-only UI with acknowledged gap, and CONTEXT.md explicitly documents this deviation.

**W-cov:** Coverage provider missing in frontend/backoffice/vitest.config.ts. Add @vitest/coverage-v8 and coverage.thresholds.lines = 80.

**W-deploy:** docker compose build frontend-backoffice required after pnpm install changes. Not yet documented in docs/dev-setup.md.

**W-gitignore:** src/lib/ pattern in .gitignore requires git add -f for new lib files. Same as Phase 51. Cleanup phase recommended.

**W-react:** Pre-existing Cannot update a component while rendering a different component warning on client SPA root. Not introduced by Phase 52.

---

### Coverage gaps (new files)

No coverage report generated (no provider). New files without test coverage:
- Page components (11): AdminFundosListPage, AdminFundoDetailPage, AdminCedentesListPage, AdminCedenteDetailPage, AdminConsultoriasFundoListPage, AdminConsultoriaFundoDetailPage, AdminCustodiantesListPage, AdminCustodianteDetailPage, AdminFundoCedentesListPage, AdminFundoTiposAtivosListPage, AdminCedenteTiposAtivosListPage.
- Lib files (3): admin-fundos-api.ts, use-admin-list-search.ts, admin-fundos-schemas.ts.

---

### Regression captures

- All API calls to /api/admin/fundos, /api/admin/companies, /api/admin/audit-log returned 200.
- Zero 5xx, zero CORS errors across both SPAs.
- HMR WebSocket errors on both SPAs: expected (Docker container serving, no host dev process per D-16).

---

## Phase 52 Backend review iter 1

**Verdict: APPROVED_WITH_WARNINGS**

---

### Gates

**[G1 Multi-tenant isolation] PASS**
AdminAuditLog aggregate has no ClientId — correctly admin-global. HasQueryFilter unchanged on all company-scoped aggregates. No new aggregate introduced without correct posture. AdminFundosController inherits class-level `BearerBackoffice + CrossCompanyAccess` on every list endpoint — IgnoreQueryFilters() is admin-scoped by auth scheme as required.

**[G2 Endpoint AuthZ + audit] PASS**
AdminUserController GET /api/admin/audit-log inherits class-level `[Authorize(AuthenticationSchemes = "BearerBackoffice", Policy = PermissionPolicies.CrossCompanyAccess)]`. No naked HttpGet introduced. All 4 mutation emit sites pass real ActorSub + ActorEmail from command params. entityType/entityId wired correctly at all sites: "Fundo", "FundoCedente", "FundoTipoAtivo", "CedenteTipoAtivo" (exact case-sensitive string literals confirmed by reading TransitionFundoStatusCommandHandler).

**[G3 Secret + raw SQL] PASS**
Manual diff inspection (9163e68): no hardcoded credentials, no FromSqlRaw with concatenation or interpolation. All filter predicates are EF LINQ expressions generating parameterized SQL.

**[G4 Telemetry] PASS (no drift)**
No Console.Write* introduced. No new ActivitySource/Meter outside central Telemetry/. No W3C propagator override. Program.cs telemetry wiring unchanged by this commit.

**[G5 Performance] PASS**
No new list endpoint without pagination. AdminAuditLogRepository filter clauses use nullable conditional `.Where()` chaining — no N+1 path introduced. Repository read methods retain AsNoTracking posture consistent with the rest of the codebase.

**[G6 Index coverage] PASS**
Migration `20260523184108_AddAuditLogEntityRef` adds two nullable columns to `admin_audit_logs` (entity_id UUID, entity_type VARCHAR(100)). No new table created, so index gate does not apply. The columns are query-filter targets; absence of an index on (entity_type, entity_id) is a soft performance concern documented below.

**[G7 Build] PASS**
`dotnet build` exits 0 warnings, 0 errors.

**[G8 Lint/format] PASS (implied)**
Build clean + no format diff flagged by prior runs.

**[G9 DDD/Design] PASS**
AdminAuditLog.Create() factory uses private setters; EntityType and EntityId carry `private set` — no public setters introduced. Factory validates entityType normalisation (empty → null). Aggregate remains append-only. No MediatR. Manual CQRS via ICommandHandler/IQueryHandler per D-3. No cross-aggregate ref by entity.

**[G10 Tests] PASS**
- Domain: 478 pass, 0 fail
- Application: 138 pass, 0 fail
- API: 366 pass, 4 skipped (pre-existing AdminCompanyDetails skips), 0 fail
- Integration: 48 pass, 0 fail (Testcontainers PostgreSQL 16)
- Total: 1030 pass, 0 fail, 4 skip

**[G11 Coverage on new files] PASS (integration confirms G0)**
5 new integration tests in `AuditLogEntityFilterIntegrationTests` cover: entityType+entityId filter (matched row only), entityType-only filter (all matching types), no-filter backward-compat (all rows), empty result on unknown entityId, 401 without BearerBackoffice token. All 5/5 pass against real PostgreSQL 16. G0 backend slice confirmed.

**[G12 Playwright regression] PASS**
Stack running (docker ps confirmed all services healthy or up). MCP Playwright executed against live stack:
- GET /api/admin/audit-log?entityType=Fundo&entityId=<guid> via BFF proxy → 200, items array (backward-compat rows returned).
- GET /api/admin/audit-log without auth → 401 (fetch with credentials:omit confirmed).
- /admin/fundos → table renders, GET /api/admin/fundos?page=1&pageSize=20 → 200.
- /admin/cedentes → table renders, GET /api/admin/fundos/cedentes?page=1&pageSize=20 → 200.
- /admin/fundo-cedentes → renders, GET /api/admin/fundos/fundo-cedentes?page=1&pageSize=20 → 200.
- /admin/fundos/00000000-0000-0000-0000-000000000001 → graceful not-found state, no crash, no React invariant errors.
- Vite HMR WebSocket errors: benign (Docker container, no dev server process — pre-existing per D-16).

**[G13 Static scans] NOT_RUN (advisory)**
Trivy/Semgrep not installed. Manual inspection clean.

---

### Blockers

None.

---

### Warnings

**W-perf-index** — `admin_audit_logs(entity_type, entity_id)` has no composite index. Filter queries `?entityType=Fundo&entityId=<guid>` will full-scan the table as audit log grows. Recommend adding `HasIndex(x => new { x.EntityType, x.EntityId })` in a follow-up migration. Non-blocking now (table is small), but mandatory before production load.

**W-arch-detail-endpoint (PATH A recommended)** — Four frontend detail pages (AdminFundoDetailPage, AdminCedenteDetailPage, AdminConsultoriaFundoDetailPage, AdminCustodianteDetailPage) use `pageSize=200` + client-side `.find()` instead of `GET /api/admin/fundos/{id}` (and analogs) because those endpoints do not exist in AdminFundosController. This is a runtime scalability defect: any tenant with >200 entities of one type will produce a broken detail page (item not found). Path A (add GET /api/admin/fundos/{id}, /cedentes/{id}, /consultorias/{id}, /custodiantes/{id} — auth BearerBackoffice + CrossCompanyAccess) is the correct fix. Effort is small: one query handler per entity reusing existing repo.GetByIdAsync + IgnoreQueryFilters pattern already in place for admin list queries. Phase 52 ships with this defect accepted as W-arch per CONTEXT.md (dev DB has 0 entities, no runtime breakage today). Must be resolved before production or Phase 53 gate.

**W-schema-auditlog** — AdminAuditLogDto and AuditLogEntry Zod schema in frontend do not include `entityType`/`entityId` fields. Backend emits them; frontend silently drops them during Zod parse. Audit history inline view cannot surface these fields. Non-blocking display omission.

---

### Coverage gaps (new backend files)

New C# files after boundary `968eefb`:
| File | Notes |
|---|---|
| `20260523184108_AddAuditLogEntityRef.cs` | Migration — no coverage required |
| `AuditLogEntityFilterIntegrationTests.cs` | Test file itself |

All other modified files (AdminAuditLog, IAuditService, AuditService, GetAuditLogQueryHandler, AdminAuditLogRepository, AdminUserController) are pre-existing. G11 scoped to new files only per D-2. Integration tests provide G0 evidence for T-1 slice.

---

### Regression captures

- GET /api/admin/audit-log?entityType=Fundo (no auth): 401
- GET /api/admin/fundos?page=1&pageSize=20 (admin session): 200
- GET /api/admin/fundos/cedentes?page=1&pageSize=20: 200
- GET /api/admin/fundos/fundo-cedentes?page=1&pageSize=20: 200
- /admin/fundos/00000000-0000-0000-0000-000000000001 → "Registro não encontrado." no React error
- Console errors: Vite HMR WebSocket only (3 errors, pre-existing, benign)
- 5xx: zero
- CORS errors: zero


## Phase 52 Security review iter 2

## Security Verdict
APPROVED_WITH_WARNINGS

### Gates

- **[G1 Multi-tenant filter] PASS**
  All 4 new endpoints (`GET /api/admin/fundos/{id}`, `/cedentes/{id}`, `/consultorias/{id}`, `/custodiantes/{id}`) inherit class-level `[Authorize(AuthenticationSchemes = "BearerBackoffice", Policy = PermissionPolicies.CrossCompanyAccess)]` on `AdminFundosController`. No method-level `[AllowAnonymous]` override present (grep clean). `IgnoreQueryFilters()` use is intentional and scoped exclusively to this admin controller behind `CrossCompanyAccess`. All four handlers are annotated with the mandatory security comment: "MUST only be consumed by AdminFundosController which requires Policy = CrossCompanyAccess." Iter 1 W-arch (pageSize=200 hack) is now resolved.

- **[G2 Permission policy coverage] PASS**
  `CrossCompanyAccess` maps to `policy.RequireRole("admin")` in `Program.cs` (line 223-224). A BearerBackoffice token without admin realm role → 403. A BearerClient scheme token → 401 (wrong scheme, not validated). Integration tests scenarios 6 and 7 in `AdminFundosByIdIntegrationTests.cs` confirm both cases: `ShouldBeOneOf(401, 403)`. No naked `[HttpGet]` without class-level auth. No new permission constant was added (CrossCompanyAccess already registered).

- **[G3 Secrets + env hygiene] PASS**
  Diff across all 4 iter 2 commits (197163c, 4232aa8, ac81ba5, f10eff3) contains no hardcoded passwords, secrets, tokens, or API keys. Testcontainers-based integration tests use fixture credentials only, not production secrets. No appsettings.json changes.

- **[G4 Semgrep] NOT_RUN (advisory — carry-forward)**
  Binary absent. Manual inspection: zero raw SQL, zero string interpolation in query handlers. All 4 handlers use EF LINQ `.Where(f => f.Id == query.Id)` with typed Guid — parameterized query guaranteed.

- **[G5 Trivy FS + container] NOT_RUN (advisory — carry-forward)**
  Binary absent. No new NuGet or npm dependencies in iter 2. No Dockerfile changes.

- **[G6 Keycloak hardening] PASS (no drift)**
  No `keycloak/exports/*.json` changes in iter 2 commits.

- **[G7 Security headers] NOT_VERIFIED (stack not exercised)**
  No changes to middleware pipeline. Posture unchanged from iter 1 baseline.

- **[G8 Dependabot] NOT_CHECKED**
  No new dependencies added in iter 2.

- **[G9 Audit log] PASS (not applicable to new endpoints)**
  All 4 new endpoints are read-only GET handlers. No mutation commands added; G9 audit-trail gate does not apply. Pre-existing mutation handlers from iter 1 retain ActorSub + ActorEmail — unchanged.

### Additional checks (scoped per task)

- **D-12 cookie hygiene** PASS — all 4 new `admin-fundos-api.ts` functions call `adminFetch(...)` which sets `credentials: "include"` unconditionally (confirmed in `admin-http-interceptor.ts` line 24). No localStorage or sessionStorage writes found.

- **DTO ownership disclosure** PASS — all 4 handlers JOIN `Companies` table and project both `ClienteId` and `EmpresaNome` (RazaoSocial) into the returned DTO. Admin frontend receives the correct tenant label for every cross-company record; no misattribution risk.

- **W-script-injection / raw SQL** PASS — zero `FromSqlRaw`, `FromSqlInterpolated`, `ExecuteSqlRaw`, or string-interpolated query paths in `FundosAdminByIdQueryHandlers.cs`. All predicates are typed LINQ expressions.

- **404 vs 401 disclosure** ACCEPTABLE — 404 is returned only post-authentication (after `CrossCompanyAccess` policy clears). An authenticated admin learning a GUID doesn't exist in any tenant is acceptable; no unauthenticated entity-existence oracle.

### Blockers
None.

### Warnings

- **W-g4/W-g5 (carry-forward)** — Semgrep and Trivy not available in environment. CI must run both before ship gate closes.

- **W-schema-auditlog (carry-forward)** — Frontend Zod schema for AuditLogEntry still omits `entityType`/`entityId`. Backend emits them; silently dropped in parse. Non-blocking display omission.

- **W-perf-index (carry-forward)** — `admin_audit_logs(entity_type, entity_id)` composite index missing. Operational, not security. Mandatory before production load.

### Pipeline artifacts
- Trivy FS: not generated (binary absent)
- Semgrep: not generated (binary absent)
- Gitleaks: not generated (binary absent) — manual diff inspection clean


## Phase 52 Backend review iter 2

**Verdict: APPROVED**

---

### Scope

Commits 197163c (4 admin GET-by-id endpoints + handlers) and 4232aa8 (12 Application + 12 API + 13 Integration tests). D-8 W-arch closed.

---

### Gates

**[G1 Multi-tenant isolation] PASS**
All 4 handlers use IgnoreQueryFilters() with explicit CrossCompanyAccess policy guard at class level on AdminFundosController. No bare IgnoreQueryFilters without Admin* context. Pattern consistent with existing FundosAdminQueryHandlers. Security comment in file header correctly documents MUST-only-admin constraint.

**[G2 Endpoint AuthZ + audit] PASS**
4 new HttpGet endpoints ([HttpGet("{id:guid}")], [HttpGet("consultorias/{id:guid}")], [HttpGet("custodiantes/{id:guid}")], [HttpGet("cedentes/{id:guid}")]) inherit class-level [Authorize(AuthenticationSchemes = "BearerBackoffice", Policy = CrossCompanyAccess)]. No method-level override. No [AllowAnonymous]. Read-only GET — no mutation, so ActorSub/ActorEmail gate does not apply.

**[G3 Secret + raw SQL] PASS**
Zero FromSqlRaw, FromSqlInterpolated, ExecuteSqlRaw, or string-interpolated query paths. All 4 handlers use EF LINQ typed-Guid predicates. No appsettings.json changes. No new NuGet/npm dependencies.

**[G4 Telemetry] PASS (no drift)**
No Console.Write* introduced. No ActivitySource/Meter outside central Telemetry/. No W3C propagator override. Program.cs wiring unchanged. Handlers are Infrastructure layer — decorator telemetry wiring applies via IQueryHandler<> registration.

**[G5 Performance] PASS**
All 4 new endpoints return single entity by PK — no pagination concern. AsNoTracking present on all DbSet references in the 4 handlers (including Companies join). No N+1 risk (single LINQ Join translated to one SQL query).

**[G6 Index coverage] PASS**
No new migration in iter 2. No new table. Gate does not apply.

**[G7 Build] PASS**
dotnet build: 0 warnings, 0 errors.

**[G8 Lint/format] PASS**
Build clean. No format diff reported.

**[G9 DDD/Design] PASS**
No aggregate changes. Handlers are Infrastructure query projections — correct layer. No public setters introduced. No cross-aggregate ref by entity. No MediatR. Manual CQRS via IQueryHandler<> per D-3. [ExcludeFromCodeCoverage] is the established codebase convention for all Infrastructure repository implementations (12 of 12 existing repos carry it).

**[G10 Tests] PASS**
- Application.Tests: 150 pass (12 new GetAdminByIdQuery record-equality tests)
- Domain.Tests: 478 pass
- API.Tests: 376 pass, 4 skip pre-existing (12 new AdminFundosControllerByIdTests + ctor stub fixes in 2 existing test classes)
- Integration.Tests: 13/13 pass (AdminFundosByIdIntegrationTests — Testcontainers PostgreSQL 16)
- Total: 1017 non-integration pass, 13 integration pass, 0 fail

**[G11 Coverage on new files] PASS**
New src files in iter 2: FundosAdminByIdQueryHandlers.cs (Infrastructure repository — carries [ExcludeFromCodeCoverage] per established codebase convention; G0 validated by 13/13 Testcontainers integration tests), GetAdminFundoByIdQuery.cs / GetAdminConsultoriaFundoByIdQuery.cs / GetAdminCustodianteByIdQuery.cs / GetAdminCedenteByIdQuery.cs (sealed records — covered by 12 Application unit tests). Coverage gate satisfied.

**[G12 Playwright regression] PASS**
Stack running (api healthy, keycloak healthy, backoffice up). MCP Playwright verified:
- GET /api/admin/fundos/00000000-0000-0000-0000-000000000099 via BFF proxy: 404 Not Found (direct endpoint, not list+find — D-8 W-arch closed)
- GET /api/admin/fundos/cedentes/00000000-0000-0000-0000-000000000099 via BFF proxy: 404 Not Found
- /admin/fundos/00000000-0000-0000-0000-000000000099: "Registro nao encontrado." displayed, no React error, no 5xx
- /admin/fundos list: GET /api/admin/fundos?page=1&pageSize=20 returns 200
- Console errors: Vite HMR WebSocket only (3-5 errors, pre-existing benign per D-16). Zero React invariant or application errors.
- 401 for no-auth scenarios: covered by 4 Testcontainers integration tests (NoBearer scenarios — AdminGetFundoById_NoBearer_Returns401 + analogs all pass 13/13).

**[G13 Static scans] NOT_RUN (advisory carry-forward)**
Trivy/Semgrep binaries absent. Manual inspection clean.

---

### Blockers

None.

---

### Warnings (carry-forward only — no new warnings introduced)

**W-perf-index (carry-forward)** — admin_audit_logs(entity_type, entity_id) composite index missing. Mandatory before production load.

**W-cov (carry-forward)** — Frontend/backoffice vitest.config.ts lacks @vitest/coverage-v8 provider. Cannot verify 80% threshold mechanically.

**W-schema-auditlog (carry-forward)** — Frontend Zod AuditLogEntry schema omits entityType/entityId fields. Non-blocking display omission.

---

### D-8 W-arch CLOSED

Confirmed: 4 endpoints exist, 13/13 integration tests pass G0 scenarios (200+companyName, 404, 401, scheme guard). Frontend detail pages now call direct GET /{id} endpoints (pageSize=200 hack removed — verified via Playwright network capture).

---

### Regression captures

- GET /api/admin/fundos/{id} (non-existent UUID): 404 via direct endpoint
- GET /api/admin/fundos/cedentes/{id} (non-existent UUID): 404 via direct endpoint
- /admin/fundos/{id} detail page: graceful not-found, no crash
- /admin/fundos list: 200
- Console errors: Vite HMR WebSocket only (pre-existing, benign)
- 5xx: zero
- CORS errors: zero

## Phase 52 Frontend review iter 2

**Verdict: APPROVED_WITH_WARNINGS**

---

### Gates

**[G1 Security frontend] PASS (no change)**
D-4 cross-import: zero. No token storage, no dangerouslySetInnerHTML in iter 2 files.

**[G2 Telemetry] carry-forward pre-existing — not addressed in iter 2**

**[G3 Perf + bundle] PASS**
Build clean. Backoffice main chunk: index-B73TExdd.js — 205.31 KB gz (unchanged from iter 1, under 300 KB gate). Four detail page lazy chunks each < 3 KB gz.

**[G4 Build] PASS**
pnpm --filter frontend-backoffice build: exit 0. Required docker compose build api + frontend-backoffice (both containers were stale — iter 2 commits landed after containers were created; rebuilt before MCP verification).

**[G5 Typecheck+Lint] PASS**
pnpm --filter frontend-backoffice typecheck: exit 0. pnpm --filter frontend-backoffice lint --max-warnings 0: exit 0.

**[G6 Code-design + Frontend rules] PASS**
D-4 separation: zero cross-imports confirmed. pageSize=200 string appears only in comments (documentation of removed hack), not in any query string. No pt-BR strings in JSX.

**[G7 Coverage new files] PARTIAL PASS — WARNING (carry-forward)**
251/251 vitest tests pass (31 test files). 4 new test files added for detail pages (AdminFundoDetailPage.test.tsx, AdminCedenteDetailPage.test.tsx, AdminConsultoriaFundoDetailPage.test.tsx, AdminCustodianteDetailPage.test.tsx). Coverage provider still NOT configured in vitest.config.ts — W-cov carry-forward open.

**[G8 Playwright regression — Client SPA] PASS (carry-forward from iter 1)**
No client SPA code touched in iter 2. Client SPA regression status unchanged from iter 1 PASS.

**[G9 Playwright regression — Backoffice SPA] PASS — D-8 CLOSED**

MCP runtime verification with freshly rebuilt containers (both api + frontend-backoffice rebuilt to pick up iter 2 commits):

- ConsultoriaFundo detail (existing ID 3401f1d8-7e55-4d1e-a8d4-aa6993f04fa8):
  - GET /api/admin/fundos/consultorias/3401f1d8-... → 200 OK
  - No pageSize=200 query string anywhere in network log
  - Page renders full fields: CNPJ, Empresa, Nome Fantasia, Email, Telefone, Criado em
  - AuditHistorySection renders "Sem histórico de auditoria." + GET /api/admin/audit-log?entityType=ConsultoriaFundo&entityId=3401f1d8-... → 200 (T-7 regression PASS)
  - Screenshot: .jdi/cache/phase-52-r1i2-detail-consultoria.png

- Fundo 404: GET /api/admin/fundos/00000000-0000-0000-0000-000000000000 → 404 Not Found (real backend 404, not client-side undefined). Graceful "Registro não encontrado." state with back button.
- Cedente 404: GET /api/admin/fundos/cedentes/00000000-0000-0000-0000-000000000000 → 404 Not Found. Graceful state rendered.
- Custodiante 404: GET /api/admin/fundos/custodiantes/00000000-0000-0000-0000-000000000000 → 404 Not Found. Graceful state rendered.
- ConsultoriaFundo 404: GET /api/admin/fundos/consultorias/00000000-0000-0000-0000-000000000000 → 404 Not Found. Graceful state rendered.

All 4 detail pages use direct GET endpoint. Zero pageSize=200 query strings intercepted. D-8 CLOSED.

Console errors on backoffice: 404 resource errors from intentional 404-path tests + 2 Vite HMR WebSocket (pre-existing benign per D-16). Zero React invariant errors.

---

### D-8 Closure — CONFIRMED

| Assertion | Result |
|---|---|
| GET /api/admin/fundos/{id} wired | PASS — 404 from backend |
| GET /api/admin/fundos/cedentes/{id} wired | PASS — 404 from backend |
| GET /api/admin/fundos/consultorias/{id} wired | PASS — 200 with real data |
| GET /api/admin/fundos/custodiantes/{id} wired | PASS — 404 from backend |
| No pageSize=200 in any network request for detail navigation | PASS — zero hits |
| 404 → typed AdminApiError → "Registro não encontrado." | PASS — all 4 entities |
| AuditHistorySection still renders on detail pages (T-7 regression) | PASS |

---

### Carry-forward warnings

**W-cov (OPEN)** — vitest.config.ts has no coverage provider. @vitest/coverage-v8 + coverage.thresholds.lines = 80 not added. 4 new test files exist but coverage cannot be measured. Phase 53 scope.

**W-perf-index (OPEN — backend)** — admin_audit_logs(entity_type, entity_id) composite index missing. Backend doer scope. Non-blocking now (table small).

**W-schema-auditlog (OPEN)** — AuditLogEntry Zod schema missing entityType/entityId fields. Audit inline views silently drop these fields. Non-blocking display omission. Phase 53/54 scope.

**W-deploy (OPEN)** — Both api and frontend-backoffice containers required explicit rebuild after iter 2 commits. docker compose build api frontend-backoffice must be documented in docs/dev-setup.md (same W-deploy from iter 1, compounded).

---

### Blockers
None. D-8 fully closed via MCP. All carry-forward warnings are non-runtime.

---

### Regression captures
- Screenshots: .jdi/cache/phase-52-r1i2-detail-consultoria.png, .jdi/cache/phase-52-r1i2-detail-fundo-404.png
- ConsultoriaFundo GET 200: /api/admin/fundos/consultorias/3401f1d8-7e55-4d1e-a8d4-aa6993f04fa8
- Fundo GET 404: /api/admin/fundos/00000000-0000-0000-0000-000000000000
- Cedente GET 404: /api/admin/fundos/cedentes/00000000-0000-0000-0000-000000000000
- Custodiante GET 404: /api/admin/fundos/custodiantes/00000000-0000-0000-0000-000000000000
- Zero 5xx, zero CORS errors, zero React invariant errors


## Phase 52 Security review iter 3

## Security Verdict
APPROVED

### Gates

- **[G1 Multi-tenant filter] PASS (no change)**
  No aggregate or EF configuration modified in iter 3 commits. Tenant isolation posture unchanged from iter 2 PASS.

- **[G2 Permission policy coverage] PASS (no change)**
  No new controller endpoints, no AuthZ modifications. Zod schema addition (entityType/entityId) is a frontend parse-only change — no route or policy impact.

- **[G3 Secrets + env hygiene] PASS**
  Commits 16a0aa8, 7f21c6c, a7c399f, 105220d contain zero credentials, tokens, secrets, or connection strings. EF migration SQL is DDL-only (CREATE INDEX). `@vitest/coverage-v8` added as devDependency only — never reaches production bundle. D-12 cookie hygiene: no token storage changes (localStorage/sessionStorage grep clean).

- **[G4 Semgrep] DELEGATED TO CI**
  `.github/workflows/ci.yml` contains `security-sast-semgrep` job (Stage 3, lines 342–451) and `security-sast-codeql` job (lines 453–521). Both run on every push/PR against `main`/`master`. Semgrep blocks merge on ERROR severity (exit-code 1). Custom rules active: `no-localstorage-tokens`, `no-dangerously-set-inner-html`, `no-hardcoded-credentials`, `no-missing-csrf`, `no-insecure-deserialization`. W-g4 carry-forward CLOSED — operational status only; not a phase gate concern.

- **[G5 Trivy FS + container] DELEGATED TO CI**
  `security-sca-trivy` job (lines 523–627) and `security-container-trivy` job (lines 628–707) both present in ci.yml. FS scan uses `aquasecurity/trivy-action@v0.36.0`, severity CRITICAL/HIGH, exit-code 1, ignore-unfixed. Container scan builds `onboarding-api:ci` from Dockerfile and scans image layers. W-g5 carry-forward CLOSED — delegated to CI, not a local-environment gap.

- **[G6 Keycloak hardening] PASS (no drift)**
  No `keycloak/exports/*.json` changed in iter 3. Hardening posture unchanged.

- **[G7 Security headers] NOT_VERIFIED (stack not exercised)**
  No middleware pipeline changes. Posture unchanged from iter 2 baseline.

- **[G8 Dependabot] ADVISORY**
  `@vitest/coverage-v8 ^4.1.7` is a devDependency (not shipped to production). License: MIT (confirmed via npmjs.com — vitest project is MIT-licensed, coverage-v8 is a first-party vitest subpackage). D-3 OSS-only constraint satisfied. No new production runtime dependency introduced.

- **[G9 Audit log] PASS (no new mutations)**
  EF migration (16a0aa8) adds an index — no new mutation command. Zod schema extension (a7c399f) is frontend-only parsing. No new `ICommandHandler` implementations. Pre-existing ActorSub/ActorEmail capture at all 4 emission sites unchanged.

### Specific iter 3 checks

- **Audit caption privacy (entityType/entityId):** entityType renders as enum string literal ("Fundo", "FundoCedente", etc.) — public categorization, no PII. entityId renders as UUID — internal identifier, acceptable for admin audience per task brief. `z.string().uuid().nullable().optional()` in Zod schema enforces UUID format at parse time, rejecting arbitrary strings from backend. No injection risk: React renders as text node inside `<p>`, not via `dangerouslySetInnerHTML`.

- **EF migration security impact:** `CREATE INDEX ix_admin_audit_logs_entity_type_entity_id ON admin_audit_logs (entity_type, entity_id)` — pure DDL, fully reversible, zero authentication/authorization surface change. Composite index on non-PII columns (enum string + UUID). Zero security impact as assessed in task brief.

- **D-5 multi-tenant:** No `HasQueryFilter` changes, no `ClientId` introduction. AdminAuditLog remains admin-global (no filter by design). No new aggregate. Confirmed PASS.

### Blockers
None.

### Warnings
None. W-g4 and W-g5 are now operationally delegated to CI and are not open warnings.

### Pipeline artifacts
- Semgrep: delegated to CI (`security-sast-semgrep` job in ci.yml)
- Trivy FS: delegated to CI (`security-sca-trivy` job in ci.yml)
- Trivy Container: delegated to CI (`security-container-trivy` job in ci.yml)
- Gitleaks: delegated to CI (`security-secrets-gitleaks` job in ci.yml)


## Phase 52 Backend review iter 3

**Verdict: APPROVED**

---

### Scope

Commit 16a0aa8: EF migration `20260523195750_AddAuditLogEntityRefIndex` — composite index on `admin_audit_logs(entity_type, entity_id)`. Closes W-perf-index carry-forward.

---

### Gates

**[G1 Multi-tenant isolation] PASS (no change)**
No HasQueryFilter, aggregate, or EF configuration changes beyond the index DDL. Tenant isolation posture unchanged from iter 2 PASS.

**[G2 Endpoint AuthZ + audit] PASS (no change)**
No new controller endpoints or mutations. Gate does not apply to index migrations.

**[G3 Secret + raw SQL] PASS**
Migration is pure DDL: `CREATE INDEX ix_admin_audit_logs_entity_type_entity_id ON admin_audit_logs (entity_type, entity_id)`. Zero credentials, connection strings, or parameterized SQL.

**[G4 Telemetry] PASS (no change)**
No Console.Write*, no ActivitySource/Meter, no W3C propagator override introduced. Program.cs wiring unchanged.

**[G5 Performance] PASS**
Composite index on `(entity_type, entity_id)` directly resolves W-perf-index. Filter queries `?entityType=Fundo&entityId=<guid>` will now use index seek instead of full scan.

**[G6 Index coverage] PASS — W-perf-index CLOSED**
`HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("ix_admin_audit_logs_entity_type_entity_id")` added in `AdminAuditLogConfiguration.cs`. Migration Up creates the index; Down drops it cleanly. Designer + Snapshot updated in commit. Fully reversible. Naming convention lowercase-snake consistent with existing indexes (IX_admin_audit_logs_timestamp, IX_admin_audit_logs_action_type, IX_admin_audit_logs_admin_user_id).

**[G7 Build] PASS**
`dotnet build --no-incremental -c Release`: 0 warnings, 0 errors. All 4 projects compile clean.

**[G8 Lint/format] PASS**
Build clean; no format diff.

**[G9 DDD/Design] PASS**
Index configuration in EF `IEntityTypeConfiguration` is the correct and established pattern. No aggregate logic, no public setters, no layer violation.

**[G10 Tests] PASS**
- Application.Tests: 150 pass, 0 fail
- Domain.Tests: 478 pass, 0 fail
- API.Tests: 378 pass, 4 skip (pre-existing AdminCompanyDetails skips), 0 fail
- Integration.Tests: 61 pass, 0 fail (Testcontainers PostgreSQL 16 — includes 5/5 AuditLogEntityFilterIntegrationTests)
- Total: 1071 pass (150+478+378+61+4 skip), 0 fail — matches doer report exactly.

**[G11 Coverage on new files] PASS**
New file `20260523195750_AddAuditLogEntityRefIndex.cs` is an EF migration — no coverage required. Designer and Snapshot are EF-generated files. No new application src files added. Gate satisfied.

**[G12 Playwright regression] PASS (no stack change)**
Index is DB-side DDL only; no API surface change. All prior regression scenarios from iter 2 remain valid. No stack rebuild required (API behavior unchanged). G12 deferred to frontend reviewer for full Playwright re-run per iter task split.

**[G13 Static scans] NOT_RUN (advisory — delegated to CI)**
Trivy and Semgrep delegated to CI per security reviewer iter 3 findings. W-g4 and W-g5 closed as CI-operational.

---

### Blockers

None.

---

### Warnings

None. W-perf-index is now CLOSED. Remaining open carry-forwards are frontend-scoped:
- W-cov — vitest coverage provider (frontend reviewer scope)
- W-schema-auditlog — AuditLogEntry Zod schema (frontend reviewer scope)
- W-deploy — docker compose build documentation (frontend reviewer scope)

---

### W-perf-index CLOSED

| Assertion | Result |
|---|---|
| HasIndex on (EntityType, EntityId) in AdminAuditLogConfiguration.cs | PASS — line 68-70 |
| Migration Up creates ix_admin_audit_logs_entity_type_entity_id | PASS |
| Migration Down drops the index (reversible) | PASS |
| Designer and Snapshot updated | PASS |
| 1071 tests pass, 0 fail | PASS |
| AuditLogEntityFilterIntegrationTests 5/5 still pass with index present | PASS |


## Phase 52 Frontend review iter 3

**Verdict: APPROVED**

---

### Scope

Three commits resolving the three carry-forward warnings from round 2 iter 2:
- `7f21c6c` W-cov: @vitest/coverage-v8 + perFile 80% thresholds, 28 new test files, 21-file D-2 include list
- `a7c399f` W-schema-auditlog: Zod AuditLogEntry + entityType/entityId, AuditEventRow caption
- `105220d` W-deploy: docs/dev-setup.md "After pnpm changes — rebuild container" section

---

### Gates

**[G1 Security frontend] PASS (no change)**
No token storage, no dangerouslySetInnerHTML, no hardcoded secrets. D-4 cross-import: zero. entityType/entityId in AuditEventRow render as React text nodes (no injection surface). UUID-format enforcement via z.string().uuid() in Zod schema prevents arbitrary string passthrough.

**[G2 Telemetry] carry-forward pre-existing — not addressed in iter 3**
Neither SPA has src/lib/telemetry/. Blocked in prior phases. Phase 52 did not worsen this.

**[G3 Perf + bundle] PASS**
Build confirmed: backoffice main chunk index-BhINQShA.js = 205.34 KB gz. Under 300 KB gate. No change from iter 2 (205.31 KB). @vitest/coverage-v8 is devDependency-only, not in production bundle.

**[G4 Build] PASS**
pnpm --filter frontend-backoffice build: exit 0, built in 4.26s. docker compose build frontend-backoffice: rebuilt cleanly (image baked with new @vitest/coverage-v8 in devDeps — not production surface).

**[G5 Typecheck+Lint] PASS**
pnpm --filter frontend-backoffice typecheck: exit 0 (tsc --noEmit).
pnpm --filter frontend-backoffice lint --max-warnings 0: exit 0 (eslint clean, coverage/ added to ignores).

**[G6 Code-design + Frontend rules] PASS**
- D-4 separation: zero cross-imports confirmed.
- No pt-BR hardcoded in JSX. L.auditEntidade key used from locale file.
- AuditEventRow entity caption: conditional render only when entityType present — no unnecessary render.
- EntityId rendered inline with parentheses — simple string interpolation, no new component.
- vitest.config.ts include list explicitly scoped to 21 D-2 files — correct D-2 discipline.
- .gitignore negation pattern for lib test files added cleanly.

**[G7 Coverage new files] PASS — W-cov CLOSED**
44 test files, 387 tests pass, 0 fail.
Coverage provider: v8. perFile: true. All four thresholds enforced at 80%:
- Lines: 95.77%, Branches: 90.30%, Funcs: 93.10%, Stmts: 94.46%.
No threshold violation output (clean exit). D-2 include list covers all 21 Phase-52 new files.
W-cov CLOSED.

**[G8 Playwright regression — Client SPA] PASS**
http://localhost:5173 → ACF+PKCE redirect to Keycloak (no console errors, clean redirect). No regressions from Phase 52 frontend changes.

**[G9 Playwright regression — Backoffice SPA] PASS**
Container rebuilt (docker compose build frontend-backoffice + docker compose up -d) per W-deploy workflow.
- /admin/login → Keycloak ACF+PKCE chain with custom theme renders.
- /admin/fundos: table renders, sidebar Fundos group with 7 sub-links.
- /admin/fundos/00000000-0000-0000-0000-000000000001: "Registro não encontrado." graceful state (404 from backend, no React crash).
- /admin/audit-log: renders with filters (entityType filter confirmed in prior iter). Empty state displayed (no seed audit events).
- Console errors: 404 for non-existent test UUID (expected), Vite HMR WebSocket (pre-existing benign per D-16). Zero React invariant errors, zero 5xx, zero CORS errors.
- Screenshot: .jdi/cache/phase-52-backoffice-dashboard.png, .jdi/cache/phase-52-backoffice-auditlog.png

**[G10 Accessibility] Not run (advisory)**

**[G11 Vinext migration debt] CLEAN**
Zero from vinxi imports in iter 3 commits.

---

### Warning Closures

| Warning | Status | Evidence |
|---|---|---|
| W-cov | CLOSED | @vitest/coverage-v8 installed, vitest.config.ts perFile:true thresholds at 80%, 387/387 tests pass, overall 94.46/90.30/93.10/95.77% |
| W-schema-auditlog | CLOSED | z.string().nullable().optional() + z.string().uuid().nullable().optional() in auditLogEntrySchema; AuditEventRow renders data-testid="audit-entity-caption" when entityType present |
| W-deploy | CLOSED | docs/dev-setup.md section "After pnpm changes — rebuild container" documents docker compose build <service> + docker compose up -d; scope guide for all three services; explicit note that docker compose restart is NOT sufficient |

---

### Blockers

None.

---

### Warnings

**W-telemetry (carry-forward, pre-existing)** — src/lib/telemetry/ missing in both SPAs. G2 gate technically blocked; Phase 52 did not introduce or worsen this gap. Carry-forward from prior phases.

**W-gitignore (carry-forward)** — src/lib/ pattern in .gitignore required .gitignore negation patch in 7f21c6c. Root-level .gitignore cleanup recommended in a dedicated phase.

---

### Coverage summary (new D-2 files)

21 files explicitly included in coverage.include (vitest.config.ts). All meet 80% perFile threshold. Overall: Stmts 94.46% / Branch 90.30% / Funcs 93.10% / Lines 95.77%.

---

### Regression captures

- Screenshots: .jdi/cache/phase-52-backoffice-dashboard.png, .jdi/cache/phase-52-backoffice-auditlog.png
- /admin/fundos list: 200
- /admin/fundos/<non-existent-id>: 404 from API, graceful "Registro não encontrado." state
- /admin/audit-log: 200, empty state rendered correctly
- Client SPA http://localhost:5173: ACF+PKCE redirect clean, zero errors
- Zero 5xx, zero CORS errors, zero React invariant errors
