---
phase_slug: auth-flow-fix
phase_position: 49
iter: 3
total_resets: 0
status: running
max_iter_per_round: 5
max_resets: 3
created_at: 2026-05-16T00:00:00Z
reopened_at: 2026-05-16T00:00:00Z
prior_converged_at: 2026-05-16T00:00:00Z
prior_verdict: APPROVED_WITH_WARNINGS
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
