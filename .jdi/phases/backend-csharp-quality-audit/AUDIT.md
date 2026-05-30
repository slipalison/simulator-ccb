# Violation Inventory — Phase 54 (backend-csharp-quality-audit)

Measured: 2026-05-30. Scope: all 4 src layers (~309 files / ~18.3k LoC). Each finding: dimension + severity + `file:line` + safe-fix vs deferred.

Severity scale: CRITICAL / HIGH / MEDIUM / LOW.

---

## SECURITY

### SEC-01 — Email logged in auth/registration flows (PII leak)
- **Severity:** MEDIUM
- **Dimension:** Security (PII in logs)
- **Locations:**
  - `src/Onboarding.API/Controllers/AuthController.cs:136` — `LogWarning("Login attempt failed for email {Email}", request.Email)`
  - `src/Onboarding.API/Controllers/CompaniesController.cs:177` — `LogWarning(ex, "Duplicate Keycloak user during registration for {Email}", command.Email)`
  - `src/Onboarding.API/Controllers/CompaniesController.cs:253` — `LogWarning(ex, "Duplicate Keycloak user during employee registration for {Email}", command.Email)`
  - `src/Onboarding.Application/Auth/Commands/ForgotPasswordCommand.cs:64,69` — `LogInformation("Password reset email sent to {Email}", ...)`
  - `src/Onboarding.Application/Auth/Commands/LoginCommand.cs:30` — `LogInformation("Login successful for {Email}", ...)`
  - `src/Onboarding.Application/Auth/Commands/ResetPasswordCommand.cs:62` — `LogInformation("Password reset successful for {Email}", ...)`
  - `src/Onboarding.Application/Companies/Commands/RegisterEmployeeCommandHandler.cs:87` — email in info log
  - `src/Onboarding.Infrastructure/Services/ResendEmailService.cs:47` — `LogInformation("Password reset email sent to {Email}", email)`
  - `src/Onboarding.API/Controllers/AdminUserController.cs:475` — `LogInformation("Admin {AdminEmail} completed first login...")`
- **Note:** The `SensitiveDataDestructuringPolicy` is registered globally (Program.cs) and masks known sensitive fields. Whether `{Email}` is masked by this policy depends on how the policy is implemented. If it is (confirmed by checking `SensitiveDataDestructuringPolicy.cs`), this is a LOW concern. If the policy does not mask scalar `string` arguments with name "Email", these are true MEDIUM PII leaks.
- **Classification:** SAFE-FIX — use event IDs or mask email in log key names; or confirm policy coverage.

### SEC-02 — `GroupsClaimsTransformation` and `RealmRolesClaimsTransformation` mutate principal on every call
- **Severity:** LOW
- **Dimension:** Security (duplicate role injection)
- **Locations:**
  - `src/Onboarding.API/Security/GroupsClaimsTransformation.cs:46–48`
  - `src/Onboarding.API/Security/RealmRolesClaimsTransformation.cs:50–56`
- **Note:** Both implementations already guard `!principal.IsInRole(roleName)` before adding claims — correct. But `IClaimsTransformation.TransformAsync` is called on every request; the principal is cloned per ASP.NET Core docs, so additions are idempotent at the framework level. No security hole; coverage gap only.
- **Classification:** DEFERRED — behavior correct; just needs tests.

### SEC-03 — Keycloak Admin URL built via string interpolation (format-safe but worth noting)
- **Severity:** LOW
- **Dimension:** Security (injection surface)
- **Locations:**
  - `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs` — multiple lines using `$"admin/realms/{targetRealm}/users/..."` where `targetRealm` comes from internal constant (`"backoffice"` or `"client"`), not user input.
- **Note:** `targetRealm` is derived from internal logic (`GetClient`), not user-supplied. No injection risk. Documented for awareness.
- **Classification:** DEFERRED — no actionable fix needed.

### SEC-04 — `IdempotencyFilter`: cached `object? Value` is deserialized as opaque object
- **Severity:** MEDIUM
- **Dimension:** Security (unsafe deserialization)
- **Locations:**
  - `src/Onboarding.API/Filters/IdempotencyFilter.cs:52–53` — `JsonSerializer.Deserialize<IdempotentResponse>(cached)` where `IdempotentResponse.Value` is `object?`
- **Note:** The filter caches and deserializes `object?` from `IDistributedCache`. The cached value came from the API's own response, so it is trusted origin — not user-supplied. However, if `IDistributedCache` is backed by Redis in production, a compromised cache could inject arbitrary JSON. Severity is MEDIUM in hardened context.
- **Classification:** DEFERRED — contract change (change `object?` to a typed union) risks breaking idempotent response shape; flag as sub-phase. Current risk is low if Redis is ACL-protected.

### SEC-05 — `RegisterFundoRequest` / `UpdateConsultoriaFundoRequest` records defined in FundosController file (>4 params without command object)
- **Severity:** LOW
- **Dimension:** Security + Clean Code (request DTOs with nullable fields without explicit validation boundary)
- **Locations:**
  - `src/Onboarding.API/Controllers/FundosController.cs:1057–1099` — 9 request records, some with 5–8 fields
- **Note:** Validation is delegated to FluentValidation command validators (correct pattern). No security gap, but large param count in records is a Clean Code threshold violation (D-52).
- **Classification:** SAFE-FIX for Clean Code; not a security risk.

---

## PERFORMANCE

### PERF-01 — In-memory `.ToList()` on LINQ sequences before projection (redundant materialization)
- **Severity:** MEDIUM
- **Dimension:** Performance (premature ToList)
- **Locations:**
  - `src/Onboarding.Application/Admin/Queries/GetPaginatedAdministratorsQuery.cs:48,56` — `filtered.ToList()` then `.ToList()` again on list-of-list operation
  - `src/Onboarding.Application/Admin/Queries/GetPaginatedEmployeesQuery.cs:53,62,82` — `employees.Select().Distinct().ToList()` then another `.ToList()` on projection
  - `src/Onboarding.Application/Fundos/Queries/GetCedenteTiposAtivos/GetCedenteTiposAtivosQueryHandler.cs:23` — `.Select().ToList()` after `ToListAsync()` (double materialization)
  - `src/Onboarding.Application/Fundos/Queries/GetFundoCedentes/GetFundoCedentesQueryHandler.cs:27` — same pattern
  - `src/Onboarding.Application/Fundos/Queries/GetFundoTiposAtivos/GetFundoTiposAtivosQueryHandler.cs:23` — same pattern
  - `src/Onboarding.Application/Fundos/Queries/List*QueryHandler.cs:38–43` (5 list query handlers) — `.ToList()` after async query already materialized
- **Note:** Pattern is `.ToListAsync(ct)` → `items.Select(x => Dto(...)).ToList()`. The `.ToList()` on projection is not harmful semantically but allocates a second list. For read paths with large result sets this could be `.Select(...).ToList()` → eliminated by using `new List<T>(capacity)` fill or just returning `IReadOnlyList<T>` from projection directly.
- **Classification:** SAFE-FIX — mechanical: remove redundant second `.ToList()` where sequence is already materialized; return `.AsReadOnly()` instead.

### PERF-02 — `AsNoTracking()` missing in some read paths
- **Severity:** MEDIUM
- **Dimension:** Performance (unnecessary EF change tracking)
- **Locations:**
  - `src/Onboarding.Infrastructure/Repositories/FundoRepository.cs:37–39` — `GetByIdAsync` uses `IgnoreQueryFilters().Include(...).Include(...)` but **no `AsNoTracking()`** — entities are tracked but never modified in read paths that call this method.
  - `src/Onboarding.Infrastructure/Repositories/CedenteRepository.cs:43` — similar pattern with `Include(c => c.TiposAtivo)` and no `AsNoTracking()` on the `GetByIdAsync` path.
- **Note:** Both repositories use `[ExcludeFromCodeCoverage]` and rely on integration tests. The missing `AsNoTracking()` on read-by-ID paths means EF tracks the entity + collection navigation unnecessarily when the result is only used for DTO projection.
- **Classification:** SAFE-FIX — add `AsNoTracking()` to `GetByIdAsync` in both repositories (behavior-preserving; no tracked mutation follows these reads in the handlers that use them for DTO mapping).

### PERF-03 — `GetPaginatedEmployeesQuery`: N+1 potential — sequential lookups per employee
- **Severity:** HIGH
- **Dimension:** Performance (N+1 query pattern)
- **Locations:**
  - `src/Onboarding.Application/Admin/Queries/GetPaginatedEmployeesQuery.cs:53–82`
- **Note:** Handler fetches `employees` from DB, then does `employees.Select(e => e.CompanyId).Distinct().ToList()` to build `companyIds`, then separately fetches companies by IDs, then separately fetches access groups by IDs. This is 3 separate queries (not N+1 per-row, but potentially 3 round-trips on each page load). The more critical risk is that the `GetPaginatedAdministratorsQuery.cs:48,56` pattern materializes all admins in memory and then paginates in memory, which is an unbounded query if admin count grows.
- **Classification:** SAFE-FIX — in `GetPaginatedAdministratorsQuery`: replace in-memory pagination with DB-side pagination. In `GetPaginatedEmployeesQuery`: use single JOIN query or batch fetch pattern.

### PERF-04 — `GetPaginatedAdministratorsQuery`: unbounded in-memory pagination
- **Severity:** HIGH
- **Dimension:** Performance (missing DB-side pagination)
- **Locations:**
  - `src/Onboarding.Application/Admin/Queries/GetPaginatedAdministratorsQuery.cs:36–56`
- **Note:** Query fetches ALL administrators from Keycloak (`GetUsersByRoleAsync` returns full list), then paginates/filters in memory with `.ToList()`. This is an O(N) memory operation that bypasses DB pagination. If admin count grows to thousands, this will degrade linearly.
- **Classification:** SAFE-FIX (partially) — current Keycloak Admin API may not support server-side pagination per role. If not, document as accepted technical debt / WARN. If Keycloak API supports `first`/`max` pagination params, use them.

### PERF-05 — `SecurityHeadersMiddleware`: `IConfiguration` resolved per request via `RequestServices`
- **Severity:** MEDIUM
- **Dimension:** Performance (DI resolution in hot path)
- **Locations:**
  - `src/Onboarding.API/Middleware/SecurityHeadersMiddleware.cs:46–48` — `context.RequestServices.GetRequiredService<IConfiguration>()` called on every request with a `Sec-Fetch-Site` header
- **Note:** `IConfiguration` is a singleton and safe to resolve from the DI root, but resolving it per-request via the request scope DI container is unnecessary overhead. The allowed origins list should be resolved once at middleware initialization.
- **Classification:** SAFE-FIX — cache `allowedOrigins` at middleware init time (middleware factory pattern).

### PERF-06 — FundosController constructor: 38 parameters (DI injection overhead)
- **Severity:** MEDIUM
- **Dimension:** Performance + SOLID (SRP violation, excessive constructor injection)
- **Locations:**
  - `src/Onboarding.API/Controllers/FundosController.cs:101–138` — 38 constructor parameters
- **Note:** Each HTTP request constructs the controller with 38 injected services. This is the direct consequence of the god-class. While ASP.NET Core's DI is efficient, this amplifies the SRP violation identified in D-53. The W2/split sub-phase should address this structurally.
- **Classification:** DEFERRED — D-53 already defers full split. Partial extraction (D-53 partial fix) reduces this.

---

## SOLID

### SOLID-01 — SRP: FundosController handles 5 bounded sub-domains (god class)
- **Severity:** HIGH
- **Dimension:** SOLID (Single Responsibility Principle)
- **Location:** `src/Onboarding.API/Controllers/FundosController.cs:1–1100`
- **Note:** Controller covers ConsultoriaFundo, Custodiante, TipoAtivo, Fundo, and Cedente — 5 entity groups, 20+ endpoints, 38 injected services, 1100 LoC. Violates SRP severely. Split deferred per D-53.
- **Classification:** DEFERRED (D-53 — split to sub-phase). This iteration applies extraction of `ToValidationProblem` to shared helper.

### SOLID-02 — SRP: AdminUserController handles admin management and company/employee management
- **Severity:** MEDIUM
- **Dimension:** SOLID (SRP)
- **Location:** `src/Onboarding.API/Controllers/AdminUserController.cs:1–546`
- **Note:** Two separate Phase scopes (Phase 29 admin management + Phase 37 company/employee management) merged in one controller. Constructor has ~20 injected services across two distinct responsibilities.
- **Classification:** DEFERRED — split risks route reorganization (D-54). Document in WARNINGS.

### SOLID-03 — OCP: `GetClient()` in KeycloakUserService uses hardcoded string comparison for realm routing
- **Severity:** LOW
- **Dimension:** SOLID (Open/Closed Principle)
- **Location:** `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs:29–33` — `if (targetRealm == "backoffice") ... else ...`
- **Note:** Adding a third realm requires modifying this method. Low priority given two-realm constraint is architectural.
- **Classification:** DEFERRED — spec says two realms; YAGNI applies.

### SOLID-04 — DIP: `AuthController` depends directly on `ICompanyRepository`, `IEmployeeRepository`, `IAccessGroupRepository`
- **Severity:** MEDIUM
- **Dimension:** SOLID (Dependency Inversion / Layering violation)
- **Location:** `src/Onboarding.API/Controllers/AuthController.cs:29–32`
- **Note:** Controller directly queries domain repositories for permission resolution (`ResolvePermissionsFromAccessTokenAsync`). This logic belongs in an Application query handler (e.g., `GetPermissionsQuery`). Controller should dispatch to Application, not query repositories directly. This also means the Auth module tests in Application.Tests do not cover this path.
- **Classification:** DEFERRED — extracting to Application handler changes controller signature slightly but is behavior-preserving. Flag in WARNINGS; safe to extract in T-3 scope.

---

## DRY

### DRY-01 — `ToValidationProblem` private static method duplicated across 4+ controller files
- **Severity:** MEDIUM
- **Dimension:** DRY
- **Locations:**
  - `src/Onboarding.API/Controllers/FundosController.cs:1000–1015` — `ToValidationProblem(ValidationResult)`
  - `src/Onboarding.API/Controllers/AdminUserController.cs:525–537` — same method, same signature
  - `src/Onboarding.API/Controllers/CedenteTiposAtivosController.cs:283` — `ToValidationProblem(ValidationException)` variant
  - `src/Onboarding.API/Controllers/FundoCedentesController.cs:283` — same variant
  - `src/Onboarding.API/Controllers/FundoTiposAtivosController.cs:283` — same variant
- **Note:** Two variants of this helper exist: one taking `ValidationResult` (FundosController, AdminUserController) and one taking `ValidationException` (the relationship controllers). Both are duplicated. Safe to extract to a shared static class in API layer.
- **Classification:** SAFE-FIX — extract to `src/Onboarding.API/Extensions/ValidationExtensions.cs` as extension methods. Behavior-preserving, no contract change.

### DRY-02 — Actor capture pattern repeated across 20+ controller action methods
- **Severity:** LOW
- **Dimension:** DRY
- **Locations:**
  - `src/Onboarding.API/Controllers/FundosController.cs` — ~12 action methods each do:
    `var actorSub = User.FindFirst("sub")?.Value ?? string.Empty;`
    `var actorEmail = User.FindFirst("email")?.Value ?? string.Empty;`
  - `src/Onboarding.API/Controllers/CompaniesController.cs` — 6+ methods
  - `src/Onboarding.API/Controllers/AdminUserController.cs` — uses `GetAuditContextSafe()` (already extracted — good pattern)
- **Note:** `AdminUserController` already extracts this into `GetAuditContextSafe()` private method (correct). FundosController and CompaniesController repeat the raw pattern. Safe to extract to a base controller helper or extension.
- **Classification:** SAFE-FIX — extract `GetActorContext()` to `ControllerBase` extension or add private helper method to each controller (D-53 scope).

### DRY-03 — `GetClient(targetRealm)` called multiple times per method in KeycloakUserService
- **Severity:** LOW
- **Dimension:** DRY (minor)
- **Locations:**
  - `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs:142–143` — `GetClient(targetRealm)` called once in `DeleteUserByEmailAsync` but creates a client, does GET, then calls `GetClient(targetRealm)` again for DELETE.
  - Multiple similar patterns across 15+ methods.
- **Note:** `IHttpClientFactory.CreateClient()` is designed to be called per-request (returns a pooled handler). Technically not a performance issue. DRY concern is minimal.
- **Classification:** DEFERRED — behavior is correct; factory pattern by design.

---

## KISS

### KISS-01 — `AuthController.ResolvePermissionsFromAccessTokenAsync` decodes JWT without validation (comment documents intent, but adds complexity)
- **Severity:** LOW
- **Dimension:** KISS
- **Location:** `src/Onboarding.API/Controllers/AuthController.cs:62–88`
- **Note:** Method reads JWT without signature validation (intentional per comment). This is an unusual pattern that could confuse maintainers. A simpler approach: use the already-validated token from `HttpContext.User` claims directly (the token is validated by JwtBearer middleware before the endpoint runs for the `/me` endpoint).
- **Classification:** DEFERRED — behavioral change to switch to `HttpContext.User` claims could affect the `/me` endpoint that currently accepts a token in the body for post-login resolution; requires careful analysis.

### KISS-02 — `SecurityHeadersMiddleware` uses lambda-in-`Use` pattern; complex flow
- **Severity:** LOW
- **Dimension:** KISS (readability)
- **Location:** `src/Onboarding.API/Middleware/SecurityHeadersMiddleware.cs:17–111`
- **Note:** Static extension method with nested `app.Use(async (context, next) => {...})`. The `OnStarting` callback adds a second async level. Could be a conventional middleware class for clarity. Not a bug, just harder to test (contributes to the 71.6% coverage).
- **Classification:** SAFE-FIX — convert to conventional `IMiddleware` class to enable direct unit testing.

---

## YAGNI

### YAGNI-01 — Dead code: Migrations `AppDbContextModelSnapshot.cs` is regenerated by EF tooling; no need to maintain manually
- **Severity:** LOW
- **Dimension:** YAGNI (noted, not actionable)
- **Location:** `src/Onboarding.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- **Note:** EF Core tool-managed file. No developer action needed; this is expected.
- **Classification:** NOT A VIOLATION — documented for clarity.

### YAGNI-02 — `IQuery<TResult>` marker interface defined but appears unused
- **Severity:** LOW
- **Dimension:** YAGNI
- **Location:** `src/Onboarding.Application/Common/IQuery.cs`
- **Note:** `IQuery<TResult>` marker interface exists but no concrete class or struct implements it (queries use `IQueryHandler<TQuery, TResult>` directly without the marker). This may be intended for future tooling.
- **Classification:** SAFE-FIX — if unused, remove (YAGNI). Confirm with `grep -r IQuery src/` first.

---

## DEAD CODE

### DEAD-01 — `using Onboarding.Infrastructure.Keycloak;` in AuthController (not needed)
- **Severity:** LOW
- **Dimension:** Dead code (unused using)
- **Location:** `src/Onboarding.API/Controllers/AuthController.cs:10`
- **Note:** `AuthController` imports `Onboarding.Infrastructure.Keycloak` — a cross-layer dependency (API → Infrastructure directly). The only Infrastructure type referenced appears to be `KeycloakAuthException` (used in catch block at ~line 136). This creates a direct API→Infrastructure coupling bypassing the Application layer.
- **Classification:** SAFE-FIX — confirm if `KeycloakAuthException` is only needed here; if so, move exception to `Onboarding.Application.Common` or `Onboarding.Domain.Exceptions`. This would also fix the layering violation.

### DEAD-02 — Stale coverage XML files in test directories
- **Severity:** LOW (artifact management)
- **Dimension:** Dead code (untracked artifacts)
- **Location:**
  - `tests/Onboarding.API.Tests/coverage-iter6-api.xml` (untracked)
  - `tests/Onboarding.Integration.Tests/coverage-iter6-integration.xml` (untracked)
- **Classification:** SAFE-FIX — add `tests/**/coverage-iter6-*.xml` to `.gitignore`. Do not commit coverage XMLs as source artifacts.

---

## CLEAN CODE THRESHOLDS (D-52)

### CC-01 — Method > 20 LoC violations

| File | Method | Approx LoC | Line |
|---|---|---|---|
| `FundosController.cs` | Constructor | 43 LoC | 101 |
| `FundosController.cs` | `TransitionFundoStatus` | 42 LoC | 753 |
| `FundosController.cs` | `RegisterFundo` | 36 LoC | 614 |
| `FundosController.cs` | `UpdateCustodiante` | 34 LoC | 430 |
| `FundosController.cs` | `UpdateConsultoria` | 34 LoC | 289 |
| `FundosController.cs` | 8 more methods | 21–33 LoC | various |
| `CompaniesController.cs` | `RegisterEmployee` | 64 LoC | 197 |
| `CompaniesController.cs` | `RegisterCompany` | 60 LoC | 125 |
| `CompaniesController.cs` | 9 more methods | 21–23 LoC | various |
| `AdminUserController.cs` | Constructor | 24 LoC | 81 |
| `AdminUserController.cs` | `CreateAdmin` | 29 LoC | 234 |
| `AdminUserController.cs` | 4 more methods | 24–26 LoC | various |
| `AuthController.cs` | `Refresh` | 61 LoC | 152 |
| `AuthController.cs` | `GetMe` | 50 LoC | 238 |
| `AuthController.cs` | `Login` | 46 LoC | 98 |
| `AuthController.cs` | 3 more methods | 27–28 LoC | various |
| `KeycloakUserService.cs` | `CreateUserAsync` | ~45 LoC | 35 |
| `KeycloakUserService.cs` | `CreateAdminUserAsync` | ~50 LoC | 85 |
| `KeycloakUserService.cs` | `UpdateAdminUserAsync` | ~30 LoC | 318 |

**Total D-52 method violations (>20 LoC):** ~36 across the 4 hotspot files.
**Classification:** SAFE-FIX for controllers (extract guard clauses, actor capture, error handling blocks). DEFERRED for constructors (split of god class).

### CC-02 — Class > 200 LoC violations

| File | LoC | Status |
|---|---|---|
| `FundosController.cs` | 1100 | CRITICAL violation — D-53 split deferred |
| `CompaniesController.cs` | 590 | HIGH violation — partial extraction safe |
| `AdminUserController.cs` | 546 | HIGH violation — partial extraction safe |
| `AuthController.cs` | 375 | MEDIUM violation — partially addressable |
| `KeycloakUserService.cs` | 445 | MEDIUM violation — complex HTTP client wrapper |
| `Program.cs` | 346 | MEDIUM — but top-level startup file; expected |
| `GetPaginatedEmployeesQuery.cs` | 85 | OK |

**Classification:** D-53 addresses controllers. `KeycloakUserService` partial extraction feasible (group Keycloak group management methods separately if pattern justified — 2 concrete uses exist).

### CC-03 — Parameters > 3 violations

| File | Method | Params | Line |
|---|---|---|---|
| `FundosController.cs` | `RegisterFundoRequest` record | 8 | 1058 |
| `FundosController.cs` | `UpdateCedenteRequest` record | 5 | 1095 |
| `FundosController.cs` | `UpdateConsultoriaFundoRequest` record | 5 | 1019 |
| `FundosController.cs` | `RegisterCustodianteRequest` record | 5 | 1027 |
| `FundosController.cs` | `UpdateCustodianteRequest` record | 5 | 1035 |
| `AdminUserController.cs` | `GetAuditLog` | 9 | 482 |
| `AdminUserController.cs` | `GetEmployees` | 6 | 156 |
| `AdminUserController.cs` | `GetAdministratorsPaginated` | 6 | 292 |
| `AdminUserController.cs` | `GetCompanies` | 5 | 112 |

**Note:** Request records with >3 params are parameter objects (which is the D-52 intended remedy — they already are parameter objects). Query method params represent filter criteria — acceptable per query specification pattern. Genuine violations are the method signatures.
**Classification:** SAFE-FIX for query methods with >4 filter params — wrap in a `*Filter` record per D-55.

### CC-04 — Nesting > 3 levels

Identified via code inspection:

| File | Location | Note |
|---|---|---|
| `AdminUserController.cs:436–475` | `ForcePasswordChange` method | try→if→if nesting: 3 levels (borderline) |
| `KeycloakUserService.cs:380–392` | `CreateGroupAsync` | if→if→if: 3 levels |
| `SecurityHeadersMiddleware.cs:36–76` | Main handler lambda | `if(isApiPath) { if(hasAnySecFetch) { if(site==...) { ... } } }` = 3 levels (OK — at threshold) |
| `GetPaginatedAdministratorsQuery.cs` | Filter chain | 2–3 levels LINQ predicates (acceptable) |

**Classification:** No clear >3 nesting violations found; borderline cases at exactly 3. PASS.

---

## SUMMARY — Top 10 violations by severity

| # | Severity | Dimension | Finding | File:Line |
|---|---|---|---|---|
| 1 | HIGH | Performance | `GetPaginatedAdministratorsQuery`: unbounded in-memory pagination of all admins from Keycloak | `Application/Admin/Queries/GetPaginatedAdministratorsQuery.cs:36–56` |
| 2 | HIGH | Performance | `GetPaginatedEmployeesQuery`: 3 sequential DB round-trips per page (not true N+1 but avoidable) | `Application/Admin/Queries/GetPaginatedEmployeesQuery.cs:53–82` |
| 3 | HIGH | SOLID / Clean Code | `FundosController` god class: 5 sub-domains, 38 ctor params, 1100 LoC (D-53 deferred split) | `API/Controllers/FundosController.cs:1–1100` |
| 4 | MEDIUM | Security | Email PII in log calls (8 locations) — confirm SensitiveDataDestructuringPolicy coverage | `API/Controllers/AuthController.cs:136` + 7 others |
| 5 | MEDIUM | Security | `IdempotencyFilter` deserializes `object?` from `IDistributedCache` (opaque type from external store) | `API/Filters/IdempotencyFilter.cs:52–53` |
| 6 | MEDIUM | Performance | `AsNoTracking()` missing in `FundoRepository.GetByIdAsync` and `CedenteRepository.GetByIdAsync` | `Infrastructure/Repositories/FundoRepository.cs:37` |
| 7 | MEDIUM | Performance | `SecurityHeadersMiddleware` resolves `IConfiguration` per request via `RequestServices` | `API/Middleware/SecurityHeadersMiddleware.cs:46–48` |
| 8 | MEDIUM | SOLID | `AuthController` depends directly on 3 domain repositories (DIP violation, cross-layer coupling) | `API/Controllers/AuthController.cs:29–32` |
| 9 | MEDIUM | DRY | `ToValidationProblem` helper duplicated in FundosController, AdminUserController, 3 relationship controllers | `API/Controllers/FundosController.cs:1000` |
| 10 | MEDIUM | Clean Code | `CompaniesController.RegisterEmployee` (64 LoC) and `RegisterCompany` (60 LoC) exceed 20 LoC threshold | `API/Controllers/CompaniesController.cs:125,197` |

---

## EFFORT ESTIMATE FOR D-49 (>80% coverage, per layer)

| Layer | Current %Line | Files < 80% | Estimated new tests | Wave |
|---|---|---|---|---|
| Domain | 95.95% | 6 exception files | 8–12 unit tests | T-6 (small) |
| Application | 45.58% | 93 files (Admin+Companies+Auth+validators) | **200–300 unit tests** | T-6 (dominant) |
| Infrastructure | 94.86% (non-exempt) | 4 Keycloak/factory files | 15–20 mock/integration tests | T-7 |
| API | 70.28% | 4 files (2 transforms + filter + middleware) | 25–35 unit tests | T-7 |
| **Total** | | **~103 files** | **~250–370 new tests** | W4 |

**D-49 conflict flag:** Infrastructure repositories use `[ExcludeFromCodeCoverage]` throughout (28 files). If D-49 is enforced literally ("no exclusions"), these 28 files must be covered via Integration.Tests or have the attribute removed. The iter6 integration run shows 94.86% Infrastructure coverage (confirming they ARE tested via Docker Testcontainers). Recommendation: treat Docker-measured coverage as satisfying D-49 for Infrastructure repos, and remove the `[ExcludeFromCodeCoverage]` attribute from the coverage denominator report but retain integration test gate. Flag to orchestrator for confirmation.

---

## INTEGRATION WITH FRONT/KEYCLOAK CONSTRAINT

All violations identified are internal to the backend. None of the safe-fix classifications:
- Change API routes or HTTP methods
- Change response payload shapes or HTTP status codes
- Modify CORS configuration
- Change authentication cookie names or OIDC flow
- Modify Keycloak realm configuration

The deferred items (D-53 god class split, SOLID-02 controller split, SOLID-04 AuthController refactor) explicitly require route-level changes and remain deferred per D-54.

**Zero front/Keycloak integration risk from Wave 1 (this report).** Wave 2 safe-fix changes will be verified against the constraint by the reviewer.
