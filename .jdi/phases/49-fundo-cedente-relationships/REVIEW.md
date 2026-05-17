<!-- ITER1_FRONTEND_HERE -->

## Frontend (iter 1)

- Verdict: APPROVED_WITH_WARNINGS
- Frontend diff (this phase): EMPTY — Phase 50 (fundo-cedente-relationships) is API-only. Zero frontend files changed in commits `9da648e`–`325e073`. All `frontend/**` entries in `b48189e..HEAD` diff belong to Phase 49 (`auth-flow-fix`), confirmed by `git log --oneline 9da648e..HEAD -- "frontend/**"` returning empty.
- Typecheck (both SPAs): PASS — `pnpm tsc --noEmit` exits 0 on client and backoffice.
- Lint: PASS — `pnpm lint` (eslint --max-warnings 0) exits 0 on both SPAs.
- Tests vitest:
  - client: 122/137 passed (15 failures). Matches iter-5 baseline exactly — pre-existing failures in `registration-form.test.tsx`, `registration-form-redesign.test.tsx`, `profile-page.test.tsx`, `profile-page-redesign.test.tsx`. Carry-over from Phase 49; no new failures.
  - backoffice: 180/180 passed (19 test files). Clean.
- Playwright (regression):
  - client api-proxy: 3/3 PASS
  - client auth-flow: 5/6 passed + 1 skip (S7 expired-token — intentional skip, matches iter-5 baseline)
  - backoffice api-proxy: 3/3 PASS
  - backoffice admin-auth-flow: 3/4 passed + 1 skip (S5 post-login race — callbackIndex guard skip, matches iter-5 baseline)
- D-12 storage: PASS — zero `localStorage`/`sessionStorage` reads/writes for token/jwt/access/refresh in production source. Single test-file reference is a negative assertion ("tokens are NOT written to localStorage") confirming the invariant holds.
- D-4 separation: PASS — zero cross-SPA imports between `frontend/client` and `frontend/backoffice`.

### Blockers

None.

### Warnings

- **W-G2-TELEMETRY (carry-forward from Phase 49)** — OTel JS + W3C telemetry not implemented. Both `frontend/client/src/lib/telemetry` and `frontend/backoffice/src/lib/telemetry` composition roots are absent. Gate G2 is structurally BLOCKED per gate definition; APPROVED_WITH_WARNINGS per Phase 48/49 precedent (Phases 51/52 are the scheduled frontend implementation phases). This warning promotes to a BLOCKER at Phase 51.
- **W-FE-15-CARRY (carry-forward from Phase 49)** — 15 client vitest failures in `registration-form`, `registration-form-redesign`, `profile-page`, `profile-page-redesign` tests (navigation mocks + `act()` wrapping issues). Not introduced by Phase 50. Must be resolved in Phase 51 when those components receive UI work.
- **W-SEED-1 (carry-forward from Phase 49)** — `scripts/seed-test-users.sh` does not set `firstName`/`lastName` on created users. Keycloak 26 triggers `UPDATE_PROFILE` on first login after `docker compose down -v`. Workaround applied inline in Playwright global-setup. Fix deferred to T-3/T-17 scope.
- **W-IT5-1 (carry-forward from Phase 49)** — Backoffice S5 still skips via `callbackIndex` guard. Race scenario remains unverifiable via `framenavigated`. Recommendation: use `page.route` intercept on `/auth/callback` response.
- **W-console-backoffice (carry-forward from Phase 49)** — `frontend/backoffice/auth-server.ts` lines 129/250 contain `console.warn`/`console.error`. Server-side h3 handler, not bundled to client JS. Pre-existing.

### Findings detail

**Phase 50 is confirmed API-only.** The CONTEXT.md "Out of scope" section explicitly excludes frontend UI. The PLAN.md tasks T-1 through T-7 are all backend (Domain/Infrastructure/Application/API layers). `git log --oneline 9da648e..HEAD -- "frontend/**"` returns empty. The three new relationship controllers (`/api/fundos/{id}/cedentes/*`, `/api/fundos/{id}/tipos-ativos/*`, `/api/cedentes/{id}/tipos-ativos/*`) are not yet wired to any SPA screen — frontend integration is deferred to Phase 51 (client) and Phase 52 (backoffice).

**G11 Vinext migration debt:** PASS — zero Vinxi-specific imports introduced. Phase 50 has no frontend changes.

**G10 Accessibility:** N/A — no new UI components. Pre-existing advisory findings from Phase 49 carry forward.

**G3 Performance:** N/A — no frontend build changes. Bundle sizes unchanged from Phase 49 baselines.

**G6 Code-design:** N/A — no frontend source changes this phase.

**G7 Coverage (new files):** New frontend files added since boundary `968eefb` are exclusively test/infra: Playwright specs, `playwright.config.ts`, `pw-no-setup.config.ts`, `global-setup.ts`, `auth-server.test.ts`, `AuthGuard.test.tsx`. No new production source files. Coverage gate on new production files: vacuously satisfied (zero new production files).

### Regression captures
- Client screenshot: .jdi/cache/phase-50-fe-client-root.png (Keycloak ACF+PKCE login page — correct redirect)
- Backoffice screenshot: .jdi/cache/phase-50-fe-backoffice-root.png (/admin/login page — correct unauthenticated state)
