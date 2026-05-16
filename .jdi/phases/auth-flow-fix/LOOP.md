---
phase_slug: auth-flow-fix
phase_position: 49
iter: 2
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

### iter=2 — Wave 4 dispatch (pending)
- T-8 (frontend: dev-workflow guard `scripts/check-dev-env.mjs` + predev + docs)
- T-9 (frontend: Playwright IPv4 hardening + api-proxy regression specs)
- New decisions appended: D-16 (compose-only dev workflow), D-17 (Playwright 127.0.0.1 pinning) — see `.jdi/DECISIONS.md`.
- Precondition (manual by user, NOT a JDI task): kill host vinxi PIDs documented in `INVESTIGATION-api-proxy.md` (PIDs 50572, 17212, 30044, 50972 at the time of capture; user must re-query with `Get-NetTCPConnection` because PIDs may have changed). The doer for T-9 will hard-fail at Playwright run if host listener remains.
- Reviewer aggregate scheduled after Wave 4 commits.
