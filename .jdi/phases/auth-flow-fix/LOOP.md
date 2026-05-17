---
phase_slug: auth-flow-fix
phase_position: 49
iter: 4
total_resets: 0
status: converged
max_iter_per_round: 5
max_resets: 3
created_at: 2026-05-16T00:00:00Z
reopened_at: 2026-05-16T00:00:00Z
prior_converged_at: 2026-05-16T00:00:00Z
prior_verdict: APPROVED_WITH_WARNINGS
converged_at: 2026-05-16T00:00:00Z
verdict: APPROVED_WITH_WARNINGS
---

## History

### iter=1 — start (2026-05-16)
- Wave 1 dispatch: T-1 (security: keycloak realms), T-2 (frontend: auth-server.ts), T-3 (security: seed-test-users.sh), T-4 (backend: Program.cs sanity)
- Wave 2 follow-on: T-5 (frontend: client race fix), T-6 (frontend: backoffice race fix)
- Wave 3 follow-on: T-7 (frontend: Playwright regression suite)
- Reviewer aggregate: backend + frontend + security

### iter=1 — doer commits (2026-05-16)
- T-1 `1fdd27a` — keycloak realms `post.logout.redirect.uris` + webOrigins audit
- T-2 `72b0d45` — fail-fast `KEYCLOAK_REALM` + cookie `sameSite=lax` for access tokens (refresh stays strict)
- T-3 `5162afc` — idempotent `scripts/seed-test-users.sh` fixture
- T-4 `5ec0da4` — backend auth sanity passed (zero code change; docs only)
- T-5 `1388746` — AuthGuard waits on `isLoading`; AuthCallbackPage deleted (Vinxi server-route owns `/auth/callback`)
- T-6 `381a334` — AdminLayout loading shell; admin-auth-context single 401 retry; `/admin/users` restored to AdminUsersPage; post-login redirect moved server-side to `/admin/companies`
- T-7 `f7a2b46` + `934ab5d` — Playwright regression suite (10 scenarios: 6 client + 4 backoffice; 1 test.skip with rationale)

### iter=1 — reviewer aggregate (2026-05-16)
- Backend C# `4269a00` — APPROVED_WITH_WARNINGS (build clean; Domain 378/378, Application 89/89, API 244/244+4-skip, Integration 20/20; 0 new .cs files so coverage trivially passes; G12 Playwright env-blocked, replaced by Integration.Tests; 4 warnings carried)
- Frontend `07a4d70` — APPROVED_WITH_WARNINGS (typecheck/lint clean both SPAs; client vitest 110/125 with 14 pre-existing failures + 1 new structural W-FE-1 — playwright spec picked up by vitest config; backoffice vitest 171/171; bundle sizes under threshold; D-12 + D-4 pass; G8/G9 Playwright env-blocked; 6 warnings)
- Security `02ffc1b` — APPROVED_WITH_WARNINGS (all 9 D-15 gates pass; Semgrep 0/0; Dependabot 0 high/critical; multi-tenant filter coverage trivially intact (no backend changes); 5 warnings, all advisory and non-blocking)
- Hash: 3approved-warnings-iter1 (n/a — first iter, no prior hash to compare)
- Aggregate verdict (worst-case rule per reviewers.md): APPROVED_WITH_WARNINGS

### iter=1 — converged (then REOPENED)
- All three reviewers returned APPROVED_WITH_WARNINGS at iter 1 close.
- Ship gate held by user — Bug 3 (api-proxy 503) surfaced when user tested company registration.
- See `INVESTIGATION-api-proxy.md` for full root-cause: vinxi-host stale process intercepts `localhost:5173`/`:5174` via IPv6 because `pnpm dev` was run on Windows host in parallel with `docker compose up`.
- Decision: do NOT ship until bug fixed. Phase 49 was incorrect at convergence.

--- REOPENED 2026-05-16 — added T-8 and T-9 to PLAN.md, status=running, iter advances to 2 ---

### iter=2 — Wave 4 (executed)
- T-8 (frontend: dev-workflow guard `scripts/check-dev-env.mjs` + predev + docs)
- T-9 (frontend: Playwright IPv4 hardening + api-proxy regression specs)
- New decisions appended: D-16 (compose-only dev workflow), D-17 (Playwright 127.0.0.1 pinning) — see `.jdi/DECISIONS.md`.
- Precondition (manual by user, completed 2026-05-16): host PIDs 50572 + 30044 killed via `Stop-Process`. Only `com.docker.backend` (PID 24376) remains as listener on `127.0.0.1:5173/:5174`.
- Doer commits:
  - T-8 `bd8f742` — guard `scripts/check-dev-env.mjs` + 9 vitest tests + predev hook + `docs/dev-setup.md` + README + CONTRIBUTING.
  - T-9 `a5edf06` — Playwright IPv4 swap + new `api-proxy.spec.ts` (3 scenarios × 2 SPAs = 6 new).
- Smoke test before reviewers: `curl http://localhost:5173/api/companies/registration -X POST -d '{}'` → 422 JSON (Bug 3 root cause resolved).
- Reviewer aggregate (worst-case rule):
  - Backend `49417a8` — BLOCKED (2 blockers + 3 warnings; build 0/0, all .cs suites green; Playwright client 2/3 api-proxy + 0/5 auth-flow; backoffice 2/3 api-proxy + 0/4 auth-flow)
  - Frontend `d339988` — BLOCKED (3 blockers, one pre-existing carry-over; D-16 guard PASS; D-17 PARTIAL — server-side `FRONTEND_URL` still localhost)
  - Security `abf0b5b` — APPROVED (D-15 unchanged; iter-2 specific risks clean; D-16 + D-17 properly scoped)
- Aggregate verdict: **BLOCKED** — iter 3 required.

### iter=3 — fix iter-2 blockers (pending)
- T-10 (frontend): Fix B-FE-1/B-BE-1 — `api-proxy.spec.ts` Scenario 3 path is wrong. Proxy ADDS `/api` prefix (per `server.ts:18`), so `GET /api/healthz/live` arrives at `/api/api/healthz/live`. Real backend healthz lives at `/healthz/live` (no `/api`). Replace Scenario 3 in both SPAs with an endpoint that genuinely round-trips through the proxy (or a healthz path that exists under `/api`).
- T-11 (frontend): Fix B-FE-2/B-BE-2 — D-17 mismatch. Two options:
  (a) Revert auth-flow specs and admin-auth-flow specs to `baseURL: localhost` (Keycloak realm `redirectUris` already pin localhost; cookie host must match). Keep `127.0.0.1` ONLY in `api-proxy.spec.ts` because that flow does not traverse Keycloak.
  (b) Add `127.0.0.1` variants to both realm JSONs `redirectUris` + `webOrigins` + `post.logout.redirect.uris` AND change `compose.yaml FRONTEND_URL` to `127.0.0.1`. More invasive; impacts security re-review.
  Recommended (a) — narrower blast radius, preserves Keycloak hardening, and `127.0.0.1` is only useful for IPv4 disambiguation in proxy probes which is exactly what api-proxy.spec.ts needs.
- T-12 (frontend): Fix W-FE-3 / W1 (now exploitable per iter-2 reviewer) — `frontend/client/auth-server.ts:171` add `&client_id=${encodeURIComponent(CLIENT_ID)}` to the Keycloak logout URL so end_session_endpoint scopes `post_logout_redirect_uri` validation. Mirror the backoffice pattern at line 270.
- T-13 (frontend): Fix W-BE-5 — `frontend/backoffice/playwright/global-setup.ts:87` resolve repo root with correct depth (3 levels up, not 4).
- B-FE-3 (telemetry): NOT in iter 3 scope per phase 48 precedent (pre-existing architectural debt, separate phase).

### iter=3 — doer commits (executed)
- T-10 `149247c` — `api-proxy.spec.ts` Scenario 3 → POST `/api/companies/registration` expecting 405 + Allow:POST.
- T-11 `baf5417` — D-17 narrowed; auth-flow specs revert to localhost; per-project baseURL overrides; D-17 addendum in DECISIONS.md.
- T-12 `419c40a` — `frontend/client/auth-server.ts` logout URL adds `&client_id=${encodeURIComponent(CLIENT_ID)}` + 3 vitest assertions.
- T-13 `2b24c20` — `frontend/backoffice/playwright/global-setup.ts` path depth 4→3 + ESM shims + compose.yaml existence guard; new `pw-no-setup.config.ts` for backoffice api-proxy.
- SUMMARY `6369454` — iter-3 entries.

### iter=3 — reviewer aggregate (worst-case rule)
- Backend `62cc713` — APPROVED_WITH_WARNINGS — 0 blockers, 4 warnings. Domain 378/378, Application 89/89, API 244/244+4-skip, Integration 20/20. Playwright (live compose): client api-proxy 3/3, client auth-flow 3/5+1skip, backoffice api-proxy 3/3, backoffice auth-flow 1/4. B-BE-1 + B-BE-2 RESOLVED.
- Frontend `3a52962` — APPROVED_WITH_WARNINGS — 0 blockers, 8 warnings (4 new + 4 carry-over). Vitest client 113/128 (+3 from T-12), backoffice 171/171. D-12, D-4, D-16, D-17 all pass. B-FE-1 + B-FE-2 RESOLVED. NF-1/NF-2 classified as WARNING (test-only spec defects).
- Security `89f7969` — APPROVED_WITH_WARNINGS — 0 blockers, 2 new warnings. T-12 deep-dive PASS (client_id env-sourced, encoded, no token leakage, backoffice-pattern match, 3 tests). D-15 unchanged on iter-3 diff. W-FE-3 / W1 RESOLVED.
- Aggregate verdict: **APPROVED_WITH_WARNINGS** (worst-case wins, all 3 reviewers approved).

### iter=3 — converged
- Bug 1 (login/logout loop), Bug 2 (post-login flash), Bug 3 (api-proxy 503): all three fixed and verified end-to-end.
- Ship-time outstanding (none block, all carry-over or low-priority):
  - NF-1, NF-2: spec-only defects — iter 4 1-line spec fixes or separate small follow-up phase.
  - W-FE-1: `frontend/client/vitest.config.ts` exclude for `playwright/specs/` (iter-1 carry).
  - W2: `tests/keycloak-hardening/verify-hardening.sh` realm name update (iter-1 carry, pre-existing).
  - W3: `keycloak/client-realm.json` clientProfiles parity with backoffice (iter-1 carry).
  - W5: legacy ROPC `onboarding-app` cleanup (acknowledged D-11, separate phase).
  - W-FE-5 / W-BE-6: `jq` dependency in `seed-test-users.sh` — install or rewrite in pure POSIX; out of scope for iter 4 unless prioritised.
- Next: `/jdi-ship auth-flow-fix` (user declined — chose to fix residual warnings first; see iter 4 below)

--- REOPENED 2026-05-16 (iter 4) — user declined ship at iter-3 convergence, requested NF-1/NF-2/W-FE-1/jq fixes ---

### iter=4 — Wave 5 dispatch (pending)
- T-14 (frontend): Fix NF-1 — `auth-flow.spec.ts` Scenario 2 + admin Scenario equivalent. `waitForURL` on `/auth/login` is wrong (server 302 hop); use final logout-resting URL.
- T-15 (frontend): Fix NF-2 — `auth-flow.spec.ts` Scenario 8 uses fresh `browser.newContext()` to avoid silent Keycloak SSO re-auth race after `clearCookies()`.
- T-16 (frontend): Fix W-FE-1 — `frontend/client/vitest.config.ts` (and backoffice if applicable) exclude `playwright/specs/` so vitest no longer attempts to compile Playwright tests.
- T-17 (security): Fix W-FE-5/W-BE-6 — `scripts/seed-test-users.sh` removes hard `jq` dependency (Python fallback or POSIX parsing).
- W-BE-7 (TanStack scroll sessionStorage), W2/W3/W4/W5 (pre-existing or future-phase): NOT in iter 4 scope (user choice).

### iter=4 — doer commits (executed)
- T-14 `637ff03` — logout spec asserts on actual KC resting URL per SPA (NF-1 fix).
- T-15 `96bcf8c` — cookie-blocked spec uses isolated `browser.newContext()` (NF-2 fix). Intermediate iteration commits: `0593897`, `d5b0739`, `8bf5161`, `fec49d9`.
- T-16 `62c9a50` — `vitest.config.ts` excludes `playwright/**` in both SPAs (W-FE-1 fix).
- T-17 — content bundled into `8bf5161` (commit msg says T-15 due to parallel-agent race; diff includes 197-line `scripts/seed-test-users.sh` rewrite with jq detection + Python fallback `json_get`/`json_has_key`).

### iter=4 — reviewer aggregate (worst-case rule)
- Backend `8264951` — APPROVED_WITH_WARNINGS. 0 blockers. Build/tests clean. Playwright live: client api-proxy 3/3, client auth-flow 5/5+1skip, backoffice api-proxy 3/3, backoffice admin-auth-flow 3/4 (S5 fail = spec defect). New warnings: W-BE-10 (S5 spec defect), W-BE-11 (`id_token_hint` UX).
- Frontend `f16c782` — APPROVED_WITH_WARNINGS. 0 blockers. NF-1 + NF-2 confirmed RESOLVED. T-16 vitest exclude validated. Backoffice S5 root-cause analyzed: `callbackIndex === -1` + IndexRoute pre-callback navigation = test-only measurement artifact; T-6 production fix correct.
- Security `61cd723` — APPROVED_WITH_WARNINGS. 0 blockers. D-15 gates unchanged (zero auth-surface touched iter 4). `id_token_hint` finding WARNING not BLOCKER (D-15 item 6 met via `client_id`; SPA cookies cleared; `/auth/me` 401 post-logout; SSO confirmation is UX friction not security bypass). T-17 Python fallback PASS (no shell injection, no eval/exec, idempotent).
- Aggregate verdict: **APPROVED_WITH_WARNINGS** (worst-case wins, all 3 approved).

### iter=4 — converged
- All Wave 5 fixes verified end-to-end against live `docker compose`.
- Client Playwright: 8/8+1skip (api-proxy 3/3 + auth-flow 5/5+1skip).
- Backoffice Playwright: 6/7 (api-proxy 3/3 + admin-auth-flow 3/4; S5 spec defect documented).
- Ship-time follow-ups (all non-blocking, deferred to future phase or iter 5 if user prefers):
  - W-BE-10 / W-FE-S5-spec: backoffice S5 spec design defect — 1-line `framenavigated` listener re-ordering + `callbackIndex === -1` guard.
  - W-BE-11 / W-SEC-IT4-1: backoffice logout `id_token_hint` — structural fix needs `id_token` capture at callback + short-lived HttpOnly cookie + logout forward. Hardening phase recommended.
  - W-BE-1: lint whitespace drift (5 pre-existing test files, pre-D-2 boundary).
  - W-BE-3 / G2: telemetry gaps (pre-existing architectural debt, separate phase).
  - W2: `verify-hardening.sh` realm rename (pre-Phase 34 carry, pre-existing).
  - W3: client-realm clientProfiles parity with backoffice.
  - W4: seed passwords echo to stdout (dev-only per D-14, cosmetic).
  - W5: ROPC `onboarding-app` cleanup (D-11 acknowledged, future phase).
- Next: `/jdi-ship auth-flow-fix`
