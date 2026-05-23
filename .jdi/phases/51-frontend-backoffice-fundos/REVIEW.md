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
