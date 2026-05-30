# T-5 Security Audit — backend-csharp-quality-audit

Iteration: 3 / Wave 3 / T-5. Scope: W2 diff (cac26d4..HEAD, 33 files) + full multi-tenant D-5 audit + SEC findings + secret scan + Semgrep + CI-fallback matrix.

Date: 2026-05-30.

---

## Tool-Availability Matrix (D-56 fallback)

| Tool | Category | Local run | CI evidence (ci.yml job) | Gap |
|---|---|---|---|---|
| Semgrep | SAST | PASS (0 findings) | `security-sast-semgrep` | None |
| CodeQL | SAST | NOT INSTALLED | `security-sast-codeql` | Local gap; CI covers |
| Trivy FS | SCA | NOT INSTALLED | `security-sca-trivy` | Local gap; CI covers |
| Trivy Image | Container | NOT INSTALLED (Docker blocked) | `security-container-trivy` | Local gap; CI covers |
| Dockle | Container | NOT INSTALLED (Docker blocked) | `security-container-dockle` | Local gap; CI covers |
| Checkov | IaC | NOT INSTALLED | `security-iac-checkov` | Local gap; CI covers |
| Gitleaks | Secrets | NOT INSTALLED | `security-secrets-gitleaks` | Local gap; manual scan done |
| TruffleHog | Secrets | NOT INSTALLED | `security-secrets-trufflehog` | Local gap; manual scan done |
| Syft | SBOM | NOT INSTALLED (Docker blocked) | `security-sbom-syft` | Local gap; CI covers |
| ZAP DAST | DAST | NOT INSTALLED (Docker blocked) | `security-dast-zap` | Local gap; CI covers |
| Dependabot | SCA | NOT INSTALLED | configured via GitHub | Local gap; CI covers |
| Kubescape | K8s | NOT INSTALLED | NOT found in ci.yml | GAP: no K8s IaC exists (no manifest files) |
| Custom Semgrep rules | SAST | PASS (.semgrep/ config) | `security-sast-semgrep` | None |

Note: Kubescape absent from CI because the project has no Kubernetes manifests (compose.yaml only, covered by Checkov). This is not a gap in practice.

---

## Semgrep Results

Command run: `semgrep --config .semgrep/ --severity ERROR --severity WARNING --json src/`
Rules applied: 5 custom rules from `.semgrep/`
Files scanned: 310 (git-tracked)
Findings: **0 ERROR, 0 WARNING**
Exit code: 0

Custom rules confirmed active: `no-localstorage-tokens`, `no-dangerously-set-inner-html`, `no-hardcoded-credentials`, `no-missing-csrf`, `no-insecure-deserialization`.

Note: `semgrep --config auto` was attempted but fails with a Unicode codec error (Windows locale issue with auto-download of rules containing U+202A). The project's `.semgrep/` rules run cleanly; `auto` is run in CI (Linux) where the encoding issue does not occur.

---

## Secret Scan (Pattern-Based, D-56 Fallback)

Manual grep for `(password|secret|connectionstring|apikey|jwt_secret)` patterns in `src/`, `keycloak/`, `.github/`:

**Finding: `src/Onboarding.API/appsettings.json:18`**
- Value: `"AdminClientSecret": "onboarding-api-admin-secret"`
- Classification: **PRE-EXISTING DEV PLACEHOLDER** — documented in REVIEW.md archives since Phase 06 (pre-D-2 boundary commit `a940fb7`). Not a new finding from W2 diff.
- Runtime: injected via `compose.yaml` env var `${KC_ADMIN_CLIENT_SECRET}`. CI uses empty string. No production credential.
- Status: **DEFERRED (pre-existing)** — replacement with empty string + user-secrets is a safe-fix that avoids Gitleaks false-positive; tracked as carry-forward WARNING.

All other scanned files: **CLEAN**. No new secrets introduced by W2 (cac26d4..HEAD diff verified clean — no credential literals in the 33 changed files).

---

## Multi-Tenant D-5 Audit (CRITICAL)

### HasQueryFilter coverage — all company-scoped aggregates

| Aggregate | EF Config file | HasQueryFilter present | Scope column | Status |
|---|---|---|---|---|
| Fundo | `FundoConfiguration.cs:104` | YES | `ClienteId == _currentCompanyService.CompanyId` | PASS |
| Cedente | `CedenteConfiguration.cs:100` | YES | `ClienteId == _currentCompanyService.CompanyId` | PASS |
| ConsultoriaFundo | `ConsultoriaFundoConfiguration.cs` | YES | `ClienteId == _currentCompanyService.CompanyId` | PASS |
| Custodiante | `CustodianteConfiguration.cs` | YES | `ClienteId == _currentCompanyService.CompanyId` | PASS |
| Employee | `EmployeeConfiguration.cs:90` | YES | `CompanyId == _currentCompanyService.CompanyId` | PASS |
| AccessGroup | `AccessGroupConfiguration.cs` | YES | company-scoped | PASS |
| TipoAtivo | `TipoAtivoConfiguration.cs` | NONE (intentional) | Global CVM catalog (D-5, D-8) | PASS — by design |
| Company | No HasQueryFilter | Global admin table | Admin access only via `IgnoreQueryFilters` | PASS — Company is the tenant root, not a scoped entity |

### IgnoreQueryFilters usage audit — W2 touched repositories

All 6 W2-touched repositories were audited for `IgnoreQueryFilters()` patterns:

| Repository | `IgnoreQueryFilters()` calls | Explicit company/ID guard | Status |
|---|---|---|---|
| `FundoRepository.cs` | `GetByIdAsync` (L36), `ExistsByCnpjAsync` (L45), `GetPagedByCompanyAsync` (L54) | L47: `f.ClienteId == companyId`; L55: `f.ClienteId == companyId` | PASS |
| `CompanyRepository.cs` | `GetByIdsAsync` (L44), `GetByEmailAsync`, `GetByKeycloakSubAsync` | No filter on `GetByIdsAsync` — explicit id-list provided by caller. Admin-only path. | PASS |
| `ConsultoriaFundoRepository.cs` | `GetByIdAsync` (L35), `ExistsByCnpjAsync` (L43), `GetPagedByCompanyAsync` (L52) | L44: `c.Cnpj == cnpjVo && c.ClienteId == companyId`; L53: `c.ClienteId == companyId` | PASS |
| `CustodianteRepository.cs` | `GetByIdAsync` (L34), `ExistsByCnpjAsync` (L43), `GetPagedByCompanyAsync` (L52) | L44: `c.Cnpj == cnpjVo && c.ClienteId == companyId`; L53: `c.ClienteId == companyId` | PASS |
| `AccessGroupRepository.cs` | `GetByIdAsync` (L39), `GetByCompanyAndNameAsync` (L47), `GetByCompanyIdAsync` (L52), `GetByIdsAsync` (L65) | `GetByCompanyAndNameAsync` L48: `a.CompanyId == companyId`; `GetByCompanyIdAsync` L54: `a.CompanyId == companyId`; `GetByIdsAsync`: id-list from caller, admin-only | PASS |
| `TipoAtivoRepository.cs` | None (global catalog, no filter by design) | N/A | PASS |

### T-4 specific queries (AsNoTracking + GetByIdsAsync batch)

| Query | T-4 change | Tenant impact |
|---|---|---|
| `GetPaginatedEmployeesQuery` — `_companyRepository.GetByIdsAsync(companyIds, ct)` | T-4 batch pattern | `companyIds` derived from `employees.Select(e => e.CompanyId)` which are already tenant-filtered (came from `GetPagedByCompanyAsync` or `GetPagedAllAsync` with admin cross-company intent). No cross-tenant leak. PASS |
| `GetPaginatedEmployeesQuery` — `_accessGroupRepository.GetByIdsAsync(accessGroupIds, ct)` | T-4 batch pattern | `accessGroupIds` derived from page-local employees. `GetByIdsAsync` uses `IgnoreQueryFilters + Where(a => idList.Contains(a.Id))` — loads only the specific IDs from the already-filtered employee page. No orphan access group from another tenant can be loaded unless its ID appears in the employee list, which is impossible since employees are tenant-filtered. PASS |
| `FundoRepository.GetByIdAsync` | Pre-existing (W2 added `AsNoTracking`) | Uses `IgnoreQueryFilters` for cross-company admin read — callers (`AdminFundosController`) are `[Authorize(Policy = CrossCompanyAccess)]`. Regular `FundosController` actions use the EF filter via paged query. PASS |

**D-5 VERDICT: PASS — No W2 change widened tenant scope or introduced cross-tenant leakage.**

---

## SEC Findings Status

### SEC-01 — Email PII in logs (MEDIUM)

Original audit locations per AUDIT.md: 9 call sites logging raw email scalars.

**T-3 fixes (confirmed):**
- `src/Onboarding.Application/Auth/Commands/LoginCommand.cs:30` — fixed to `LogInformation("Login successful")` (no email)
- `src/Onboarding.Application/Auth/Commands/ForgotPasswordCommand.cs:64,69` — fixed to generic messages
- `src/Onboarding.Application/Auth/Commands/ResetPasswordCommand.cs:62` — fixed to `LogInformation("Password reset successful")`

**Root cause confirmed (T-5 finding):** `SensitiveDataDestructuringPolicy.TryDestructure()` returns `false` for `string` (line 43) — it only masks complex objects. Raw string scalar arguments passed to `{Email}` or `{AdminEmail}` in `LogWarning`/`LogInformation` are emitted in plain text.

**Remaining locations after T-3 (5 calls) — FIXED in T-5:**

| File | Line | Fix applied |
|---|---|---|
| `src/Onboarding.API/Controllers/AuthController.cs:132` | `LogWarning ... {Email}` scalar | Replaced with `MaskEmail(request.Email)` |
| `src/Onboarding.API/Controllers/CompaniesController.cs:169` | `LogWarning ... {Email}` scalar | Replaced with `MaskEmail(command.Email)` |
| `src/Onboarding.API/Controllers/CompaniesController.cs:236` | `LogWarning ... {Email}` scalar | Replaced with `MaskEmail(command.Email)` |
| `src/Onboarding.API/Controllers/AdminUserController.cs:455` | `LogInformation ... {AdminEmail}` scalar | Replaced with `MaskEmail(adminEmail)` |
| `src/Onboarding.Infrastructure/Services/ResendEmailService.cs:47` | `LogInformation ... {Email}` scalar | Private `MaskEmail` helper added (Infrastructure cannot reference API layer) |

Status: **FIXED** — all 9 original locations now either removed (T-3) or masked (T-5).

### SEC-02 / SEC-04 — IdempotencyFilter `object?` deserialization (MEDIUM)

`src/Onboarding.API/Filters/IdempotencyFilter.cs:52–53`

Analysis confirmed: `JsonSerializer.Deserialize<IdempotentResponse>(cached)` where `IdempotentResponse.Value` is `object?`. The cached value originates from the API's own `ObjectResult.Value` (serialized as JSON by the API). `System.Text.Json` without `TypeNameHandling` does not perform gadget-chain deserialization (unlike `Newtonsoft.Json` with `TypeNameHandling.Auto`) — the `object?` will deserialize as `JsonElement`. Not a true insecure deserialization vector.

Risk remains if Redis backing store is compromised: injected JSON could produce unexpected `JsonElement` types that callers don't handle. Current risk is MEDIUM if Redis ACL is absent.

Status: **DEFERRED** — contract change (typing `object?` to a specific union) risks breaking the idempotent response shape. Deferred to sub-phase. Mitigation: ensure Redis ACL is enforced in production (infra constraint, outside this phase scope).

### SEC-03 — Keycloak URL string interpolation (LOW)

`src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs` — `targetRealm` from internal constant, not user-supplied. No injection risk.

Status: **DEFERRED (no action needed)** — documented for awareness.

---

## Keycloak Realm Hardening Audit

No Keycloak config was changed (hard constraint). Verified current state of both realm exports:

| Setting | client-realm.json | backoffice-realm.json | Hardening target | Status |
|---|---|---|---|---|
| `bruteForceProtected` | true | true | true | PASS |
| `failureFactor` (lockout) | 5 | 5 | ≤5 | PASS |
| Password policy | length(8)+upper+lower+digits+special | length(8)+upper+lower+digits+special | min 12 per hardening convention | WARNING (below 12 min length) |
| SSO idle timeout | 1800 sec (30 min) | 1800 sec (30 min) | ≤30 min | PASS |
| SSO max lifetime | 28800 sec (8 h) | 28800 sec (8 h) | ≤12 h | PASS |
| SSL required | external | external | external | PASS |
| `onboarding-client-acf` implicit flow | false | N/A | disabled | PASS |
| `onboarding-backoffice` implicit flow | N/A | false | disabled | PASS |
| PKCE method | S256 | S256 | S256 | PASS |
| CORS webOrigins | `http://localhost:5173` (explicit) | `http://localhost:5174` (explicit) | no `*` | PASS |
| `onboarding-app` ROPC | directAccess=true | N/A | legacy, slated for removal (D-11) | WARNING (known, pre-existing) |
| `onboarding-api-admin` | service-account only, no direct grants | service-account only | restricted | PASS |

WARNING: password min length is 8, not 12 as per the security convention. This predates D-2 and is a pre-existing configuration. Changing it requires Keycloak admin action; not applied here (hard constraint: do not change Keycloak config without explicit DECISIONS.md entry).

**No drift detected from W2 changes.** Realm JSON files were not touched in the W2 diff.

---

## W2 Diff Security Review (33 files, cac26d4..HEAD)

Reviewed each file category for: injection, broken authz, PII in logs, insecure deserialization, missing validation, secrets.

### API layer changes (7 controller files + 1 extension)

| File | Change | Security assessment |
|---|---|---|
| `AdminUserController.cs` | `ResolveAdminEmail()` helper extracted; `ResolveKeycloakUserIdAsync()` extracted; `ToValidationProblem()` → extension | No new attack surface. `ResolveAdminEmail()` reads from `HttpContext.Items["AdminEmail"]` then JWT claims — same logic, correctly extracted. PASS |
| `AuthController.cs` | Validation extraction (`ToValidationProblem`) | No auth contract change. Login cookie path, PKCE flow, refresh flow unchanged. PASS |
| `CompaniesController.cs` | `GetActorContext()`, `ResolveClientIpAddress()` helpers extracted | Same claim reads, no new surface. Tenant isolation `companyId != _currentCompanyService.CompanyId` guards intact. PASS |
| `FundosController.cs` | Actor context + validation extraction | No route change. All `[Authorize(Policy=...)]` attributes intact. PASS |
| `CedenteTiposAtivosController.cs`, `FundoCedentesController.cs`, `FundoTiposAtivosController.cs` | Validation extraction | No contract change. PASS |
| `ValidationExtensions.cs` | New shared extension | No security surface — pure DTO mapping. PASS |

### Application layer changes (7 files)

| File | Change | Security assessment |
|---|---|---|
| `GetPaginatedEmployeesQuery.cs` | Batch `GetByIdsAsync` pattern (PERF-03 fix) | Tenant isolation verified (see D-5 table above). PASS |
| `GetPaginatedAdministratorsQuery.cs` | In-memory pagination guard (`Math.Min(pageSize, 100)`, page > 0 guard) | No security regression. Size cap prevents memory abuse. PASS |
| `Auth/Commands/ForgotPasswordCommand.cs` | Email PII log fix (T-3) | Fixed per SEC-01. PASS |
| `Auth/Commands/LoginCommand.cs` | Email PII log fix (T-3) | Fixed per SEC-01. PASS |
| `Auth/Commands/ResetPasswordCommand.cs` | Email PII log fix (T-3) | Fixed per SEC-01. PASS |
| `Auth/Validators/ForgotPasswordCommandValidator.cs`, `ResetPasswordCommandValidator.cs` | Validator refactor | No security change. PASS |
| `Companies/Commands/RegisterCompanyCommandHandler.cs`, `RegisterEmployeeCommandHandler.cs` | Minor refactors | Tenant isolation preserved. Crypto temp password generation (`RandomNumberGenerator`) unchanged. PASS |

### Domain layer changes (7 files)

All domain changes are value object refinements and aggregate property changes (read-only sealed records). No security invariant change. HasQueryFilter is at EF config level (not domain), so domain changes cannot affect tenant filtering. PASS.

### Infrastructure layer changes (6 repository files)

- `AsNoTracking()` added to read paths — performance only, no security impact.
- `GetByIdsAsync()` batch methods added — explicit ID-list pattern, no filter bypass.
- All `IgnoreQueryFilters()` calls retain explicit company ID guards where required.

PASS.

---

## Findings Triage

| ID | Severity | Status | Action |
|---|---|---|---|
| SEC-01 (email PII in logs) | MEDIUM | **FIXED (T-5)** | All 5 remaining scalar email log calls masked |
| SEC-02/SEC-04 (IdempotencyFilter `object?`) | MEDIUM | **DEFERRED** | Not true gadget-chain risk; Redis ACL mitigates; contract change risky |
| SEC-03 (Keycloak URL interpolation) | LOW | **DEFERRED** | No user input, no action needed |
| SECRET: appsettings.json AdminClientSecret | LOW | **DEFERRED (pre-existing)** | Dev placeholder, runtime-injected; documented since Phase 06 |
| KC: password min length 8 (target: 12) | LOW | **WARNING (pre-existing)** | Pre-D-2, no change to Keycloak config per hard constraint |
| KC: onboarding-app ROPC enabled | LOW | **WARNING (pre-existing)** | Legacy client, slated for removal (D-11); no new change |

---

## Constraint Check

- No API HTTP contract changed (routes, status codes, response shapes unchanged).
- No auth/OIDC flow changed (ACF+PKCE, cookies, PKCE S256 intact).
- No Keycloak realm configuration changed.
- No CORS configuration changed.
- All fixes are string-level masking at log call sites — zero observable behavior change to callers.

---

## Tests

Build: `dotnet build Onboarding.slnx --configuration Release` — **0 warnings, 0 errors**.

| Suite | Passed | Failed | Skipped |
|---|---|---|---|
| `Onboarding.Domain.Tests` | 481 | 0 | 0 |
| `Onboarding.Application.Tests` | 150 | 0 | 0 |
| `Onboarding.API.Tests` | 384 | 0 | 4 (pre-existing) |
| **Total** | **1015** | **0** | **4** |

Integration.Tests: not run (Docker blocked in sandbox — orchestrator runs against live stack).
