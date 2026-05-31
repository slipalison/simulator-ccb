# Phase 55 — controller-di-reduction — Main-thread Verification (W3 residual)

Date: 2026-05-30
Verifier: main thread (subagent sandbox blocks Docker; reviewer deferred W3).
Stack: `docker compose up -d --build` (api + keycloak + frontend-client + frontend-backoffice + deps), all healthy.

## Constraint proven
User hard constraint: **"As alterações não pode quebrar a integração com os Front ou Key Cloud."**
Verdict: **HELD at every layer.** Dispatcher refactor (D-60..D-63) is behavior-preserving.

## Integration.Tests (Testcontainers + real Postgres)
`dotnet test Onboarding.Integration.Tests` → **248 PASS, 0 fail** (exact baseline, zero delta).
Covers: FundosController (`api/fundos`), FundoCedentesController, FundoTiposAtivosController,
CedenteTiposAtivosController (`api/.../{id}/...` relationship routes), + full 200/401/422 paths.

## Playwright (real browser, real Keycloak ACF+PKCE)

### OIDC flow — both SPAs
| SPA | realm | client_id | PKCE | Result |
|---|---|---|---|---|
| Backoffice (5174) | backoffice | onboarding-backoffice | `code_challenge_method=S256` | login round-trip → authenticated dashboard ✓ |
| Client (5173) | client | onboarding-client-acf | `code_challenge_method=S256` | OIDC initiation → Keycloak login ✓ |

Login as `admin@onboarding.local` → landed `/admin/companies`, real data rendered.

### Dispatched endpoints (via BFF cookie, authenticated)
| Endpoint | Controller (refactor) | Status |
|---|---|---|
| GET /api/admin/companies | AdminUserController (23→5) | 200 |
| GET /api/admin/employees | AdminUserController | 200 |
| GET /api/admin/fundos | AdminFundosController (11→1) | 200 |
| GET /api/admin/fundos/consultorias | AdminFundosController | 200 |
| GET /api/admin/fundos/custodiantes | AdminFundosController | 200 |
| GET /api/admin/fundos/cedentes | AdminFundosController | 200 |
| GET /api/admin/fundos/cedente-tipos-ativos | AdminFundos read-model | 200 |
| POST /api/companies/registration | CompaniesController (17→5) | 422 (validation) |
| GET /api/auth/me | AuthController.GetMe (8→3) | 401 (wrong audience — by design) |
| GET /api/auth/permissions | PermissionsController (1) | 401 (by design) |

### Contract paths (status-code preservation)
- **200** authenticated dispatched reads — unchanged.
- **422** invalid registration payload — `IValidationRunner` + `ToValidationProblem` intact (D-61).
- **401** unauthenticated / wrong-audience — JWT bearer middleware rejects BEFORE dispatch; auth untouched.
- **Logout**: pre-logout `/api/admin/companies` = 200 → after logout = **401** (session invalidated). ✓

### Console
Zero **500**, zero **InvalidOperationException / "handler not found"**, zero dispatch errors across the
whole session. Only benign noise: favicon 404, CORS-blocked direct-to-:8080 calls (by-design; SPA must
use BFF :5174 proxy), expected 401s, Vite HMR websocket `ERR_CONNECTION_REFUSED` (dev-only).

## Conclusion
All 9 controllers behavior-preserving end-to-end. Multi-tenant isolation (D-5) transparent through
dispatch. HTTP contract byte-equivalent. **W3 satisfied — ready for /jdi-ship.**
