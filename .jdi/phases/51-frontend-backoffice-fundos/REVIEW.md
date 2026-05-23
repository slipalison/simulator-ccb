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
