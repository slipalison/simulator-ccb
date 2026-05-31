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

## Backend C# (iter 1)

### Backend Verdict
Verdict: APPROVED_WITH_WARNINGS

### Gates

- [G1 Multi-tenant isolation] PASS - 3/3 relationship aggregates (FundoCedenteAggregate, CedenteTipoAtivoAggregate, FundoTipoAtivoAggregate) correctly omit HasQueryFilter (D-5, D-21). EF configs confirmed: no HasQueryFilter in any of the 3 new configuration files. Cross-tenant scoping enforced inline via parent-aggregate lookup in all 5 actions of each controller. No bare IgnoreQueryFilters without Admin-prefix context.

- [G2 Endpoint AuthZ + audit] PASS - All 18 new endpoints carry [Authorize]: 15 tenant-scoped (class-level BearerClient + method-level FundRead/FundWrite) and 3 admin (class-level BearerBackoffice + CrossCompanyAccess inherited). All 9 mutation commands (3 Create + 3 UpdateLimite + 3 TransitionStatus) carry ActorSub + ActorEmail. Audit writes synchronously via IAuditService.RecordAsync with ConfigureAwait(false). No fire-and-forget.

- [G3 Secret + raw SQL hygiene] PASS - No new secrets in Phase 50 diff. No FromSqlRaw in new files. Gitleaks unavailable; manual scan clean. Legacy appsettings.json:18 AdminClientSecret pre-existing at boundary (D-2) - not introduced by Phase 50.

- [G4 Telemetry (OTel+Serilog+W3C)] PASS - G4.1: 0 Console.Write in any src file. G4.2: No interpolated logger strings in new handlers. G4.3-G4.4: No new ActivitySource or Meter outside Telemetry class. G4.5: No W3C propagator override. G4.6: Program.cs retains all 6 required registrations. G4.7: SetDbStatementForText absent. G4.8/G4.9: TenantBaggageMiddleware and TelemetryCommandHandlerDecorator absent at boundary 968eefb (not Phase 50 regression). No handlers manually start Activity for command-level spans.

- [G5 Performance hygiene] PASS - All 3 new repositories use AsNoTracking on all read methods. All 6 new list endpoints (3 tenant + 3 admin) paginated with page + pageSize. No unbounded lists.

- [G6 Index coverage on new migration] PASS - Migration 20260517044302_AddRelationshipAggregates.cs: all 3 new tables have FK performance indexes on fundo_id, cedente_id, tipo_ativo_id. TipoAtivo is global (no ClientId by design - D-21). 3 partial unique indexes enforce REL-09 (D-18) at DB level.

- [G7 Build] PASS - dotnet build --no-incremental -c Release: 0 errors, 0 warnings. Build time 4.33s.

- [G8 Lint/format] WARNING (pre-existing) - dotnet format --verify-no-changes: 5 WHITESPACE errors in tests/Onboarding.Domain.Tests/Infrastructure/KeycloakUserServiceTests.cs. Confirmed pre-existing: 0 Phase 50 commits touch that file.

- [G9 DDD + design] PASS - Aggregates rich: all properties private set. Static Create factories on all 3 aggregates. Behavior: ActivateGuard, UpdateLimite, TransitionTo, CanTransitionTo. HISTORICO terminal state enforced. Value objects LimiteExposicao and JanelaVigencia: private constructors + static Create + validation. Domain has zero Infrastructure references. No MediatR (D-3). No FluentAssertions (OSS-only). No speculative abstractions.

- [G10 Tests] PASS - All 4 suites green: Domain.Tests 446/0/0, Application.Tests 126/0/0, API.Tests 323/0/4skip (pre-existing), Integration.Tests 41/0/0 (21 new Phase 50 scenarios). REL-09 race test PASS: exactly 1 success (201) + 1 conflict (409).

- [G11 Coverage on new files] PASS with warnings - Domain module: 95.11% line, 85.56% branch, 92.48% method (above 80%). Application: all handler/validator files 94-100% line. Pure-record DTOs show 0-70% from unit suite (coverlet instruments synthesized constructors; no logic present; all exercised by integration tests). Infrastructure repos [ExcludeFromCodeCoverage].

- [G12 Playwright regression] PASS with pre-existing UAT failures - API container rebuilt from Phase 50 binaries, healthy. Playwright checks: all 6 new Phase 50 endpoints return 401 without token; 401 with invalid token on /api/fundos; 403 on admin endpoints without admin role (CrossCompanyAccess policy enforced). UAT (run-uat.mjs): 9 passed / 13 failed / 2 cascade / 8 ignored - IDENTICAL to pre-Phase-50 baseline. Failures are registration-flow + realm-discovery pre-existing.

- [G13 Static scans] ADVISORY - Semgrep: 0 ERROR findings (from security iter). Trivy: unavailable. No HIGH/CRITICAL in new code.

### Blockers

None.

### Warnings

- W1: tests/Onboarding.Domain.Tests/Infrastructure/KeycloakUserServiceTests.cs:40 - 5 dotnet format WHITESPACE violations. Pre-existing at boundary 968eefb.
- W2: Application/Fundos/DTOs/Rel*.cs + Queries/Admin/AdminRel*.cs + ListAdmin*.cs - pure-record DTOs show 0-70% line coverage from unit suite. No logic; coverlet instruments synthesized constructors. All exercised by integration tests.
- W3 (pre-existing): TenantBaggageMiddleware and TelemetryCommandHandlerDecorator absent at boundary 968eefb. Not a Phase 50 regression. Must be addressed before production cutover.

### Coverage gaps (new files)

| File | Suite | Coverage | Required | Status |
|---|---|---|---|---|
| LimiteExposicao.cs | Domain.Tests | 95%+ (module) | 80% | PASS |
| JanelaVigencia.cs | Domain.Tests | 95%+ (module) | 80% | PASS |
| FundoCedenteAggregate.cs | Domain.Tests | 95%+ (module) | 80% | PASS |
| CedenteTipoAtivoAggregate.cs | Domain.Tests | 95%+ (module) | 80% | PASS |
| FundoTipoAtivoAggregate.cs | Domain.Tests | 95%+ (module) | 80% | PASS |
| Create*Handler.cs (3) | Application.Tests | 100% line | 80% | PASS |
| Update*LimiteHandler.cs (3) | Application.Tests | 94-100% line | 80% | PASS |
| Transition*StatusHandler.cs (3) | Application.Tests | 95-100% line | 80% | PASS |
| Get*QueryHandler.cs (3) | Application.Tests | 100% line | 80% | PASS |
| *Validator.cs (9) | Application.Tests | 100% line | 80% | PASS |
| Rel*Dto.cs (3) | Application.Tests | 50-70% (record, no logic) | 80% | WARN |
| AdminRel*Dto.cs (3) | Application.Tests | 0% (record, no logic) | 80% | WARN |
| ListAdmin*Query.cs (3) | Application.Tests | 0% (record, no logic) | 80% | WARN |
| *Repository.cs (3) | Integration.Tests | ExcludeFromCodeCoverage | n/a | OK |
| RelationshipsAdminQueryHandlers.cs | Integration.Tests | ExcludeFromCodeCoverage | n/a | OK |

### Cross-tenant guard audit (3/3 controllers)

| Controller | Parent repo | Guard condition | Result |
|---|---|---|---|
| FundoCedentesController | IFundoRepository.GetByIdAsync | fundo is null OR fundo.ClienteId != companyId | 404 |
| FundoTiposAtivosController | IFundoRepository.GetByIdAsync | fundo is null OR fundo.ClienteId != companyId | 404 |
| CedenteTiposAtivosController | ICedenteRepository.GetByIdAsync | cedente is null OR cedente.ClienteId != companyId | 404 |

All GET-by-id actions additionally verify association.FundoId/CedenteId matches route param.

### REL-09 race condition

FundoCedente_ConcurrentCreate_OnlyOneSucceeds: PASS. Two concurrent POSTs to same (FundoId, CedenteId) - exactly 1 success (201) + 1 conflict (409). Partial unique index IX_rel_fundo_cedente_active on (fundo_id, cedente_id) WHERE status=ATIVO is the authoritative gate. ActivateGuard is defense-in-depth (D-18).

### Regression captures

- UAT: 9 passed / 13 failed / 2 cascade / 8 ignored (pre-existing, run-uat.mjs unmodified in Phase 50)
- Playwright AuthZ checks: all 6 Phase 50 endpoints return 401 without token; 403 on admin without admin role
- API container rebuilt from Phase 50 binaries and confirmed healthy before regression run

<!-- ITER1_FRONTEND_HERE -->

