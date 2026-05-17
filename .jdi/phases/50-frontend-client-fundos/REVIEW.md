# Phase 50 — frontend-client-fundos — REVIEW

## Security review iter 1

### Verdict: APPROVED_WITH_WARNINGS

---

### G1 — Multi-tenant isolation (D-5)

**PASS**

All 4 new query handlers implement explicit tenant guards:

- `GetFundoAllowedTransitionsQueryHandler`: loads Fundo, asserts `fundo.ClienteId == _currentCompanyService.CompanyId`, returns null (→ 404) otherwise.
- `GetFundoCedenteAllowedTransitionsQueryHandler`: guards via parent Fundo.ClienteId; also verifies association.FundoId matches route param (prevents cross-association leak within same tenant).
- `GetFundoTipoAtivoAllowedTransitionsQueryHandler`: same pattern via parent Fundo.
- `GetCedenteTipoAtivoAllowedTransitionsQueryHandler`: guards via parent Cedente.ClienteId; verifies association.CedenteId.

`IgnoreQueryFilters()` not used in any new handler. Cross-tenant attempt returns null, controller maps to 404 — consistent with existing pattern.

---

### G2 — Permission policy coverage

**PASS**

All 4 new `GET /allowed-transitions` endpoints carry:
```
[Authorize(AuthenticationSchemes = "BearerClient", Policy = PermissionPolicies.FundRead)]
```
`FundRead` maps to `funds:read` — correct read-level policy. No `[AllowAnonymous]` added anywhere. All new frontend routes are children of `authenticatedRoute` in `router.tsx` — no public route bypass introduced.

Sidebar `FUNDOS_NAV_GROUP` visibility gated on `funds:read` permission at runtime.

---

### G3 — Secrets + env hygiene

**PASS**

Grep over the full phase diff (`159fe5c^..HEAD`) for `localStorage`, `sessionStorage`, `Authorization.*Bearer`, hardcoded passwords/secrets/tokens: zero findings.

Gitleaks not installed locally — deferred to CI verification. Manual grep patterns run as fallback.

---

### G4 — Semgrep

**PASS**

`semgrep --config .semgrep --severity ERROR --error` ran against 315 tracked files (302 C#, 13 TS). Exit code 0. Findings: 0 blocking, 0 warnings.

---

### G5 — Trivy FS + container

**NOT RUN** — Trivy not installed locally. Deferred to CI verification. No Dockerfile or docker-compose changes in this phase; container scan not triggered.

---

### G6 — Keycloak hardening drift (D-13)

**PASS**

`git diff 159fe5c^..HEAD -- keycloak/` returned empty. No realm JSON, client config, or Keycloak-related file changed in this phase. Zero drift.

---

### G7 — Security headers + CSP

**NOT VERIFIED** — Stack not running locally during this review. No new middleware, proxy rules, or server-side header configuration added in this phase. Header posture is unchanged from Phase 49 (previously verified).

---

### G8 — Container / infra

**PASS**

`git diff 159fe5c^..HEAD -- Dockerfile* docker-compose*.yml .github/` returned empty. No CI, Docker, or infra files touched.

---

### G9 — D-12 cookies HttpOnly

**PASS**

`fundos-api.ts` implements its own `apiFetch` using `credentials: 'include'` — no `Authorization` header, no Bearer token added by the frontend. The auto-refresh cycle calls `/auth/refresh` via `fetch` with `credentials: 'include'` (HttpOnly cookie path). No `localStorage.setItem` or `sessionStorage.setItem` calls found in any new file (`fundos-api.ts`, `fundos-schemas.ts`, `api-errors.ts`, `query-client.ts`, `use-allowed-transitions.ts`, all component files in phase diff).

---

### D-15 auth gates — no drift

**PASS**

- PKCE/state validation: untouched.
- CORS: no `WithOrigins("*")` or origin reflection added.
- No new public routes in backend or frontend.
- `bruteForceProtected`: keycloak JSON unchanged.

---

### D-3 OSS-only

**PASS**

- `@tanstack/react-query@5.100.10`: MIT (verified via `npm view`).
- `@tanstack/react-query-devtools@5.100.10`: MIT (verified via `npm view`).
- No commercial dependency introduced.

---

### Blockers

None.

---

### Warnings

1. **`fundos-api.ts` DRY violation (non-security):** The file re-implements the 401/refresh cycle instead of importing the shared `apiFetch` from `api.ts` (which is not exported). The comment in the file acknowledges this. Security posture is equivalent, but code duplication risks drift in refresh logic. Tracked in SUMMARY.md; fix in next refactor phase by exporting `apiFetch` from `api.ts`.

2. **Gitleaks / Trivy deferred to CI:** Local CLI not available. CI pipeline is source of truth for these checks. Phase must not ship if CI reports blocking findings.

3. **Security headers (G7) not live-verified:** Stack not running. Header posture assumed unchanged from Phase 49; CI/CD regression covers this gate.

4. **Sidebar `auth as any` cast:** `frontend/client/src/components/organisms/Sidebar.tsx` uses `(auth as any).permissions` because `permissions: string[]` is not yet on `AuthContextValue`. This is a type-safety gap — not a runtime security issue (the guard still runs), but the cast could silently break if the auth context shape changes. Tracked as TODO in the source comment; should be resolved before Phase 52.

---

### Pipeline artifacts

- Semgrep: run locally, 0 findings — no JSON artifact (clean run).
- Gitleaks: deferred to CI.
- Trivy FS: deferred to CI.
