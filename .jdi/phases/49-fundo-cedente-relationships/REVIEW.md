## Security (iter 1)

### Security Verdict
Verdict: APPROVED_WITH_WARNINGS

### Gates

- [G1 Multi-tenant filter] PASS - 3/3 relationship aggregates correctly omit HasQueryFilter (D-5). All 3 controllers guard every endpoint inline via parent-aggregate lookup + ClienteId comparison -> 404. GET-by-id verifies association belongs to route parent aggregate. Parent aggregates (Fundo, Cedente) retain HasQueryFilter. No missing filter on any company-scoped entity.

- [G2 Permission policy coverage] PASS - All 15 new user-facing endpoints carry explicit [Authorize] at class-level (AuthenticationSchemes) and method-level (Policy = FundRead or FundWrite). 3 new admin endpoints inherit AdminFundosController class-level [Authorize(BearerBackoffice, CrossCompanyAccess)]. No new PermissionPolicyConstants added. No wiring gap.

- [G3 Secrets + env hygiene] WARNING (legacy) - appsettings.json:18 AdminClientSecret plaintext value pre-existing at brownfield boundary 968eefb (D-2). Not introduced by Phase 50. No new secrets in Phase 50 diff. Gitleaks not available; manual inspection clean.

- [G4 Semgrep] PASS - 0 findings at ERROR severity (5 rules, 623 files scanned, exit 0).

- [G5 Trivy FS + container] UNTESTED - Trivy binary not available. No Dockerfile changes in Phase 50. Must validate in CI.

- [G6 Keycloak hardening] WARNING (pre-existing drift) - Phase 50 modified both realm files (added post.logout.redirect.uris + clientProfiles/clientPolicies no-wildcard-redirect enforcer - net hardening improvement). Both realms pass: bruteForceProtected=true, failureFactor=5, ssoSessionIdleTimeout=1800. FAIL: passwordPolicy=length(8) does not meet gate minimum length(12). Pre-existing since before Phase 49 boundary. ROPC on onboarding-app pre-existing (D-11, marked for removal).

- [G7 Security headers] UNTESTED - API not running; Playwright deferred (API-only phase). Must validate in CI.

- [G8 Dependabot] PASS - 0 open HIGH/CRITICAL alerts.

- [G9 Audit log] PASS - All 9 new commands carry ActorSub + ActorEmail. All handlers write synchronously via await IAuditService.RecordAsync with ConfigureAwait(false). AuditService calls SaveChangesAsync inline. No fire-and-forget.

### Blockers

None.

### Warnings

- keycloak/client-realm.json + keycloak/backoffice-realm.json: passwordPolicy = length(8); gate requires length(12). Pre-existing drift (968eefb). Must be remediated before production cutover. Schedule dedicated hardening phase.
- src/Onboarding.API/appsettings.json:18 AdminClientSecret plaintext. Pre-existing legacy (D-2). Inject via env var or secrets manager in staging/prod.
- Migration 20260517044302_AddRelationshipAggregates.cs: status column varchar(20) without DB-level CHECK constraint. Enum enforced only by EF conversion + domain. Advisory: add CHECK (status IN (ATIVO, INATIVO, HISTORICO)) in follow-on migration.
- G5 (Trivy) and G7 (headers) untested due to tooling unavailability; must pass in CI before final ship gate.

### D-5 Audit Summary

3/3 controllers cross-tenant guarded:

| Controller | Parent lookup | Guard expression | Cross-tenant result |
|---|---|---|---|
| FundoCedentesController | IFundoRepository.GetByIdAsync | fundo.ClienteId != _currentCompanyService.CompanyId | 404 |
| FundoTiposAtivosController | IFundoRepository.GetByIdAsync | fundo.ClienteId != _currentCompanyService.CompanyId | 404 |
| CedenteTiposAtivosController | ICedenteRepository.GetByIdAsync | cedente.ClienteId != _currentCompanyService.CompanyId | 404 |

GET-by-id additionally verifies association.FundoId/CedenteId matches route param (prevents cross-aggregate enumeration within same tenant). 6 integration tests assert 404 (not 403, not 200):

1. FundoCedente_CrossTenantCreate_Returns404
2. FundoCedente_GetList_OnlyReturnsTenantOwnedRows
3. CedenteTipoAtivo_CrossTenantCreate_Returns404
4. CedenteTipoAtivo_GetList_CrossTenantCedente_Returns404
5. FundoTipoAtivo_CrossTenantCreate_Returns404
6. FundoTipoAtivo_GetList_CrossTenantFundo_Returns404

### REL-09 Race Outcome

FundoCedente_ConcurrentCreate_OnlyOneSucceeds: PRESENT. Two concurrent POSTs to same (FundoId, CedenteId); asserts exactly one 201 + one 409. GlobalExceptionHandler.cs:52 maps DbUpdateException -> 409. DB partial unique index IX_rel_fundo_cedente_active on (fundo_id, cedente_id) WHERE status=ATIVO is authoritative gate. In-memory ActivateGuard is defesa-em-profundidade (D-18).

### AuthZ Policy Table (new endpoints)

| Controller | Action | HTTP verb + path | Policy |
|---|---|---|---|
| FundoCedentesController | CreateFundoCedente | POST /api/fundos/{fundoId}/cedentes | FundWrite |
| FundoCedentesController | ListFundoCedentes | GET /api/fundos/{fundoId}/cedentes | FundRead |
| FundoCedentesController | GetFundoCedenteById | GET /api/fundos/{fundoId}/cedentes/{id} | FundRead |
| FundoCedentesController | UpdateFundoCedenteLimits | PATCH /api/fundos/{fundoId}/cedentes/{id}/limits | FundWrite |
| FundoCedentesController | TransitionFundoCedenteStatus | POST /api/fundos/{fundoId}/cedentes/{id}/status | FundWrite |
| FundoTiposAtivosController | CreateFundoTipoAtivo | POST /api/fundos/{fundoId}/tipos-ativos | FundWrite |
| FundoTiposAtivosController | ListFundoTiposAtivos | GET /api/fundos/{fundoId}/tipos-ativos | FundRead |
| FundoTiposAtivosController | GetFundoTipoAtivoById | GET /api/fundos/{fundoId}/tipos-ativos/{id} | FundRead |
| FundoTiposAtivosController | UpdateFundoTipoAtivoLimits | PATCH /api/fundos/{fundoId}/tipos-ativos/{id}/limits | FundWrite |
| FundoTiposAtivosController | TransitionFundoTipoAtivoStatus | POST /api/fundos/{fundoId}/tipos-ativos/{id}/status | FundWrite |
| CedenteTiposAtivosController | CreateCedenteTipoAtivo | POST /api/cedentes/{cedenteId}/tipos-ativos | FundWrite |
| CedenteTiposAtivosController | ListCedenteTiposAtivos | GET /api/cedentes/{cedenteId}/tipos-ativos | FundRead |
| CedenteTiposAtivosController | GetCedenteTipoAtivoById | GET /api/cedentes/{cedenteId}/tipos-ativos/{id} | FundRead |
| CedenteTiposAtivosController | UpdateCedenteTipoAtivoLimits | PATCH /api/cedentes/{cedenteId}/tipos-ativos/{id}/limits | FundWrite |
| CedenteTiposAtivosController | TransitionCedenteTipoAtivoStatus | POST /api/cedentes/{cedenteId}/tipos-ativos/{id}/status | FundWrite |
| AdminFundosController | ListFundoCedentes (admin) | GET /api/admin/fundos/fundo-cedentes | CrossCompanyAccess / BearerBackoffice (class-level) |
| AdminFundosController | ListFundoTiposAtivos (admin) | GET /api/admin/fundos/fundo-tipos-ativos | CrossCompanyAccess / BearerBackoffice (class-level) |
| AdminFundosController | ListCedenteTiposAtivos (admin) | GET /api/admin/fundos/cedente-tipos-ativos | CrossCompanyAccess / BearerBackoffice (class-level) |

No AllowAnonymous present on any new endpoint.

### AdminAuditLog Status

All 9 mutation handlers (3 Create + 3 UpdateLimite + 3 TransitionStatus) write synchronously to IAuditService.RecordAsync. AuditService.RecordAsync calls _repo.SaveChangesAsync(ct) inline. No fire-and-forget task. Audit trail complete for all mutation paths.

### Pipeline artifacts
- Trivy FS: not generated (binary unavailable)
- Semgrep: .jdi/cache/phase-50-semgrep.json (0 findings, exit 0)
- Gitleaks: not generated (binary unavailable)
