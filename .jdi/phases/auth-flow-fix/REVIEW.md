# Phase 49 — auth-flow-fix — REVIEW (iter 1)

## Backend C# (iter 1)
- Verdict: APPROVED_WITH_WARNINGS
- Build: ok, 0 errors, 0 warnings
- Tests:
  - Onboarding.Domain.Tests: 378 passed / 0 failed / 0 skipped (261 ms)
  - Onboarding.Application.Tests: 89 passed / 0 failed / 0 skipped (153 ms)
  - Onboarding.API.Tests: 244 passed / 0 failed / 4 skipped (pre-existing: TracePropagationTests x2 + AdminCompanyDetailsTests x2) (2m 15s)
  - Onboarding.Integration.Tests: 20 passed / 0 failed / 0 skipped (3m 11s)
- Coverage: n/a — zero new .cs files added in phase 49 (confirmed: git diff --name-only --diff-filter=A b48189e..HEAD -- src/*.cs returns empty)
- Lint: WARN — pre-existing whitespace drift in 5 test files (see Warnings); phase 49 introduced zero .cs changes so cannot have introduced new drift
- DDD enforcement: pass — phase 49 added zero .cs files; Domain layer has no Infrastructure references; no public setters introduced; cross-aggregate references unchanged
- Playwright regression: partially ran — compose stack running (api, keycloak, frontend-client, frontend-backoffice all healthy); 401/403 scenarios confirmed via curl against live API; full login E2E blocked by Keycloak environment state drift (see Findings detail); Integration.Tests (Testcontainers) served as backend regression substitute for auth/tenant scenarios

### Blockers

None.

### Warnings

- **W-BE-1** — dotnet format reports whitespace violations in 5 pre-existing test files: `tests/Onboarding.Domain.Tests/Application/Commands/CreateAdminCommandHandlerTests.cs:69`, `tests/Onboarding.Domain.Tests/Application/Commands/GetAuditLogQueryHandlerTests.cs:98`, `tests/Onboarding.Domain.Tests/Application/Commands/KeycloakUserServiceFirstLoginTests.cs` (lines 25, 27, 32, 50, 60, 77, 101, 121), `tests/Onboarding.Domain.Tests/Infrastructure/AuditServiceTests.cs` (lines 31-35, 51), `tests/Onboarding.Domain.Tests/Infrastructure/KeycloakUserServiceTests.cs` (lines 40, 41, 68, 78, 163). All from commit `8455567`, predating D-2 boundary. Phase 49 modified none of these files. Fix: run `dotnet format` on these 5 files in a dedicated cleanup commit.

- **W-BE-2** — `src/Onboarding.API/appsettings.json:18` — `AdminClientSecret: "onboarding-api-admin-secret"` is a dev placeholder committed to source. Pre-existing since commit `a940fb7` (Phase 06), predates D-2 boundary. Actual secret is injected via compose env var `${KC_ADMIN_CLIENT_SECRET}` at runtime. Recommend replacing hardcoded placeholder with empty string or user-secrets pattern.

- **W-BE-3** — G4 Telemetry pre-existing gaps in Program.cs: `TenantBaggageMiddleware` not wired, `TelemetryCommandHandlerDecorator` not registered, `PiiScrubber`/`PiiScrubbing` pattern absent. Note: `SensitiveDataDestructuringPolicy` is registered on both Serilog pipelines (lines 38, 52) providing PII masking at the Serilog layer. These gaps predate D-2 boundary and phase 49 did not touch Program.cs (phase 48 diff only added fund permission policies). Flagged for future resolution.

- **W-BE-4** — G12 Playwright full E2E blocked by Keycloak environment state drift. Keycloak container was last started at 11:49 UTC-3 importing pre-T-1 realm state; T-1 updated realm JSONs at 14:12 without restarting Keycloak. Live Keycloak does not reflect T-1 changes. Keycloak logs confirm `CODE_TO_TOKEN_ERROR: invalid_client_credentials` for `onboarding-backoffice` starting at 17:33, before this reviewer session. PKCE S256 was confirmed at the authorize URL level via browser. Resolution: `docker compose down -v && docker compose up -d` per D-13.

### Findings detail

**G1 Multi-tenant isolation (IgnoreQueryFilters analysis):**
`git diff 968eefb..HEAD -- src/**/*.cs` shows 14 occurrences of `IgnoreQueryFilters` in the diff. All are from phase 48 Fundos files. Each usage is compliant: (a) `AdminFundosController` / `ListAdmin*QueryHandler` — `Admin*`-prefixed handlers behind `CrossCompanyAccess` policy requiring `admin` realm role; (b) `FundosController` GetById methods — controller explicitly re-applies tenant check (`if (entity.ClienteId != _currentCompanyService.CompanyId) return NotFound()`) immediately after unfiltered fetch, with security comment. Both patterns comply with G1 rules. Phase 49 added zero EF configuration or aggregate changes.

**G4 Telemetry pre-existing gaps:**
`SensitiveDataDestructuringPolicy` in `src/Onboarding.API/Observability/` is registered on both Serilog pipelines, masking sensitive fields at the Serilog destructuring layer. This satisfies the PII scrubbing intent for log output. The OTel ActivityProcessor-based `PiiScrubber` pattern for W3C trace attributes is not present. `TenantBaggageMiddleware` and `TelemetryCommandHandlerDecorator` are also absent. All gaps predate D-2 boundary — Program.cs was last substantively modified before the boundary. Flagged as W-BE-3.

**G12 Playwright regression — backend API scenarios executed:**
The following scenarios were validated against the live API (port 8080):
- 401 with no token: `GET /api/companies/me` — HTTP 401 (PASS)
- 401 with malformed token (`Bearer eyJ.fake.token`) — HTTP 401 (PASS)
- 403 service account lacking admin role: `GET /api/admin/companies` — HTTP 403 (PASS)
- No-auth on admin route: `GET /api/admin/companies` — HTTP 401 (PASS)
- PKCE S256 at authorize URL: backoffice Entrar click → URL contained `code_challenge_method=S256`, realm=`backoffice` (not `onboarding`) — PASS (Bug 1 fix confirmed at URL level)
Integration.Tests (20/20 pass) covers tenant isolation, 403 on missing permission, 401 with no auth, cross-tenant 404 — all G12 backend scenarios.
Screenshot: `.jdi/cache/phase-49-auth-error.png`

---

<!-- FRONTEND_SECTION_HERE -->

## Security (iter 1)
- Verdict: APPROVED_WITH_WARNINGS

### D-15 gate checklist

- **PKCE S256: pass** - `buildAuthorizationUrl` in both `frontend/client/src/lib/auth-code-flow.ts:39` and `frontend/backoffice/src/lib/auth-code-flow.ts:39` hardcodes `code_challenge_method: "S256"` with no runtime override or fallback to `plain`. Keycloak-side enforcement: `attributes."pkce.code.challenge.method": "S256"` in both `onboarding-client-acf` (client-realm.json) and `onboarding-backoffice` (backoffice-realm.json). Playwright specs intercept the authorize URL and assert `code_challenge_method=S256` per T-7.

- **HttpOnly: pass** - Every `setCookie` call in both `auth-server.ts` files carries `httpOnly: true`. Verified on: `client_access_token`, `client_refresh_token`, `backoffice_access_token`, `backoffice_refresh_token`, `pkce_code_verifier`, `pkce_state`, `pkce_retry`. Zero exceptions found.

- **Secure (prod): pass** - `IS_PROD = (process.env.NODE_ENV === 'production')` gates `secure: IS_PROD` uniformly on every cookie write in both SPAs. No cookie bypasses this conditional.

- **SameSite: pass with judgment** - Access tokens changed from `strict` to `lax` (T-2 commit 72b0d45). Refresh tokens remain `strict`. Justified: (1) Keycloak->SPA cross-origin 302 chain suppresses `strict` cookies on first post-redirect request, causing the post-login 401 race (Bug 2). `lax` resolves this while blocking cross-site subresource requests and form POSTs. (2) CSRF maintained by PKCE `state` parameter: both SPAs reject `state !== storedState` before code exchange. (3) Refresh tokens ride only same-origin `POST /auth/refresh`; `strict` is appropriate. Professional risk read: acceptable. Code comments cite D-15.

- **CORS allowlist: pass** - `Program.cs:254` uses `policy.WithOrigins("http://localhost:5173","http://localhost:5174")` with `AllowCredentials()`. No wildcard, no origin reflection. Confirmed by T-4.

- **bruteForceProtected: pass** - Both realm JSONs: `bruteForceProtected: true`, `failureFactor: 5`, `waitIncrementSeconds: 30`. Static JSON verification passes both realms.

- **end_session_endpoint logout: pass with warning** - Both SPAs redirect to Keycloak `protocol/openid-connect/logout`. `post.logout.redirect.uris` added in T-1 covers each SPA login URL. Discrepancy: `frontend/backoffice/auth-server.ts:270` includes `&client_id=...`; `frontend/client/auth-server.ts:171` does NOT. Without `client_id`, Keycloak validates `post_logout_redirect_uri` globally. Not a hard block since T-1 sets per-client URIs; flagged Warning W1.

- **State validation: pass** - `frontend/client/auth-server.ts:104-107`: storedState compare, redirect to `/auth/error` on mismatch. `frontend/backoffice/auth-server.ts:125`: `if (state !== storedState || !codeVerifier)` triggers diagnostic log + auto-retry (max 1) + error redirect. Both SPAs reject mismatched state before code exchange.

- **Storage gate (D-12): pass** - Grep across `frontend/{client,backoffice}/src/**/*.{ts,tsx}` for `localStorage.setItem` / `sessionStorage.setItem` returns one hit: `frontend/client/src/tests/theme-provider.test.tsx:55` writing `"theme"` (UI preference, not a token). Zero token-keyed writes found. Playwright specs assert `localStorage.length === 0 && sessionStorage.length === 0` post-login (T-7).

---

### Security pipeline

- **Semgrep: 0 ERROR, 0 WARNING** - v1.159.0, config `.semgrep`, 5 rules, 541 files scanned. Exit code 0. No findings.
- **Gitleaks: NOT INSTALLED** - Manual regex scan on `git diff b48189e..HEAD`. Zero findings in new code. Dev fixtures (`dev-admin-secret`, `*-dev-change-in-prod-2026`) are pre-existing before phase boundary per D-14. Seed passwords (`E2EClient@123!`, `E2EAdmin@123!`) are dev-only per D-14.
- **TruffleHog: NOT INSTALLED** - Skipped; covered by manual scan above.
- **Trivy FS: NOT INSTALLED** - Skipped.
- **Trivy image: skipped** - No Dockerfile changed in phase 49 diff.
- **CodeQL: CI-only** - No CI runs found on branch `agents/add-new-agents` via `gh run list`. Not available for this iter.
- **Dependabot: 0 HIGH/CRITICAL** - `gh api` returned empty array for open high/critical alerts.
- **Dockle / Checkov / Kubescape / Syft: NOT INSTALLED** - Skipped on this host.

---

### Multi-tenant (D-5)

pass - Phase 49 modified zero backend files under `src/`. `git diff b48189e..HEAD --name-only -- src/` returns empty. No aggregate, EF config, or controller was touched. Tenant filter coverage cannot have regressed.

---

### Keycloak hardening drift

Static JSON checks (compose stack not running in reviewer sandbox):

| Check | client-realm | backoffice-realm |
|---|---|---|
| bruteForceProtected=true | PASS | PASS |
| failureFactor<=5 | PASS | PASS |
| ssoSessionIdleTimeout<=1800 | PASS | PASS |
| sslRequired=external | PASS | PASS |
| No wildcard redirectUris | PASS | PASS |
| PKCE S256 on ACF clients | PASS | PASS |
| frontchannelLogout=true | PASS | PASS |
| post.logout.redirect.uris set | PASS | PASS |

`tests/keycloak-hardening/verify-hardening.sh` NOT RUN - The script hardcodes `KC_REALM="onboarding"` (line 5), which is the realm removed in Phase 34. Pre-existing defect not introduced in phase 49. Static JSON checks above substitute for the live run. See Warning W2.

---

### Blockers

None.

---

### Warnings

- **W1** - `frontend/client/auth-server.ts:171` - Logout URL missing `client_id` parameter. Backoffice pattern (line 270) includes `&client_id=...` for scoped `post_logout_redirect_uri` validation; client SPA omits it. Keycloak 26 falls back to global URI validation when `client_id` is absent. Low risk in current setup; recommend adding `&client_id=${encodeURIComponent(CLIENT_ID)}` to client SPA logout URL.

- **W2** - `tests/keycloak-hardening/verify-hardening.sh:5` - `KC_REALM="onboarding"` targets a non-existent realm (removed Phase 34). Pre-existing defect, not introduced by phase 49. Blocks future automated hardening regression runs. Should be updated to iterate over `client` and `backoffice` realms.

- **W3** - `keycloak/client-realm.json` - Missing `clientProfiles`/`clientPolicies` no-wildcard redirect URI enforcer. `backoffice-realm.json` has the `enforce-no-wildcard-redirects` policy active via Keycloak `secure-redirect-uris-enforcer` executor. `client-realm.json` relies only on the static JSON having no wildcards today, without server-side enforcement. Recommend porting the policy block.

- **W4** - `scripts/seed-test-users.sh:273-274` - E2E passwords printed in plaintext to stdout at script completion. Acceptable per D-14 (dev-only), but CI log exposure is avoidable. Recommend masking passwords in final echo lines.

- **W5** - `keycloak/client-realm.json:22-46` - Legacy `onboarding-app` ROPC client (`directAccessGrantsEnabled: true`, `publicClient: true`). Acknowledged in D-11 for future removal. Not introduced by phase 49. Schedule removal before production deployment.

---

### Findings detail

**D-15 SameSite lax judgment:**
The `strict` to `lax` change for access tokens is the key security trade-off of this phase. Risk analysis: (a) Cross-site form POSTs and subresource requests do not carry `lax` cookies - primary CSRF vector blocked. (b) Cross-site top-level GET navigations carry `lax` cookies, but `/auth/callback` state validation is the primary anti-CSRF control independent of SameSite. (c) `/auth/refresh` is POST-only; `strict` refresh tokens will not accompany cross-site POSTs even with `lax` access tokens - silent refresh cannot be triggered cross-site. (d) Industry precedent: major IdP SDKs recommend `lax` for redirect-flow access cookies. Verdict: acceptable, properly justified in inline code comments citing D-15.

**Logout client_id gap (W1):**
Keycloak 26 OIDC RP-Initiated Logout: when `client_id` is present the IdP validates `post_logout_redirect_uri` against that client-specific `validPostLogoutRedirectUris`; when absent it uses a global check. T-1 added per-client `post.logout.redirect.uris` entries which are the primary guard. The missing `client_id` in the client SPA only weakens the scoping of Keycloak validation, not the redirect URI allowlist itself. In the current setup (single URI per client) this is not exploitable. Fix is a one-liner.

**verify-hardening.sh realm mismatch (W2):**
The script predates Phase 34 realm split and was never updated. Phase 49 modified realm JSONs but not the test script, so the automated regression gate is silently broken for both realms. Static checks above confirm realm JSON correctness, but future phases touching KC config should treat updating this script as a mandatory acceptance criterion.

---

### Pipeline artifacts
- Trivy FS: `.jdi/cache/phase-49-trivy-fs.json` (not-installed placeholder)
- Semgrep: `.jdi/cache/phase-49-semgrep.json` (0 findings, 5 rules, 541 files)
- Gitleaks: `.jdi/cache/phase-49-gitleaks.json` (not-installed placeholder; manual scan clean)
