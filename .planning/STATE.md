---
gsd_state_version: 1.0
milestone: v3.0
milestone_name: admin-backoffice-panel
status: roadmap-defined
stopped_at: Milestone v3.0 roadmap created — 5 phases defined (16-20)
last_updated: "2026-04-09T16:00:00.000Z"
last_activity: 2026-04-09 -- Milestone v3.0 roadmap created with Phase 16-20 for Admin Backoffice Panel
progress:
  total_phases: 20
  completed_phases: 14
  total_plans: 48
  completed_plans: 38
  percent: 79
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-09)

**Core value:** Cadastro seguro e funcional de clientes PF/PJ com autenticação robusta via Keycloak — se a segurança falhar, nada mais importa.
**Current focus:** MILESTONE v3.0 — Admin Backoffice Panel (Roadmap defined, ready for requirements formalization)
**Last activity:** 2026-04-09 -- Milestone v3.0 roadmap created with 5 new phases (16-20) for admin backoffice

Progress: [███████████████░░░] 79% (38/48 plans - v3.0 defining requirements)

## Current Position

Phase: Roadmap complete — Milestone v3.0 phases 16-20 defined
Next: Formalize v3.0 requirements (ADMIN-01 to ADMIN-16) in REQUIREMENTS.md, then plan Phase 16
Last activity: 2026-04-09 -- Roadmap updated with milestone v3.0 breakdown

Progress: [███████████████░░░] 79% (38/48 plans - v3.0 defining requirements)

## Milestone Breakdown

**Milestone v1.0 — Foundation:** ✅ COMPLETE (10/10 phases, 30/30 plans)
**Milestone v2.0 — UX/UI + Production:** ⚠️ 94% COMPLETE (4/5 phases, 7/8 plans — Phase 14 E2E pending)
**Milestone v3.0 — Admin Backoffice:** 📋 DEFINING (0/5 phases, 0/11 plans — roadmap created)

## Performance Metrics

**Velocity:**

- Total plans completed: 37
- Phases completas: 15/15 (MILESTONE COMPLETE + post-milestone fixes + production cleanup)

**By Phase:**

| Phase | Plans | Status |
|-------|-------|--------|
| 01-infrastructure | 3/3 | Complete 2026-04-01 |
| 02-keycloak-security-hardening | 1/1 | Complete 2026-04-02 |
| 03-backend-domain-layer | 2/2 | Complete 2026-04-02 |
| 04-observability | 4/4 | Complete 2026-04-03 |
| 05-registration-api | 4/4 | Complete 2026-04-05 |
| 06-authentication-api | 3/3 | Complete 2026-04-06 |
| 07-frontend-foundation | 4/4 | Complete 2026-04-07 |
| 08-registration-ui | 3/3 | Complete 2026-04-07 |
| 09-login-ui | 3/3 | Complete 2026-04-07 |
| 10-profile-ui | 3/3 | Complete 2026-04-08 |
| 11-ux-redesign | 2/2 | Complete 2026-04-08 |
| 12-ui-redesign | 3/3 | Complete 2026-04-08 |
| 13-reset-password-fix | 1/1 | Complete 2026-04-08 |
| 15-production-cleanup | 1/1 | Complete 2026-04-09 |

*Updated after each plan completion*
| Phase 01-infrastructure P01 | 2 | 3 tasks | 7 files |
| Phase 01-infrastructure P02 | 8 | 2 tasks | 1 files |
| Phase 01-infrastructure P03 | 4 | 2 tasks | 14 files |
| Phase 01-infrastructure P03 | 60 | 3 tasks | 15 files |
| Phase 02-keycloak-security-hardening P01 | 6 | 3 tasks | 4 files |
| Phase 03-backend-domain-layer P01 | 4 | 2 tasks | 16 files |
| Phase 03-backend-domain-layer P02 | 2 | 2 tasks | 10 files |
| Phase 07-frontend-foundation P00 | 2 | 2 tasks | 5 files |
| Phase 07-frontend-foundation P01 | 5 | 2 tasks | 6 files |
| Phase 08-registration-ui P01 | 6 | 2 tasks | 2 files |
| Phase 08-registration-ui P02 | 8 | 2 tasks | 3 files |
| Phase 08-registration-ui P03 | 8 | 3 tasks | 4 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- Roadmap: ROPC grant chosen over Auth Code + PKCE — conscious tradeoff, revisit if security requirements increase
- Roadmap: Registration flow persists to app_db FIRST, then creates Keycloak user — rollback strategy needed if Keycloak call fails
- Roadmap: Two separate PostgreSQL containers (app_db + keycloak_db) — strict isolation
- [Phase 01-infrastructure]: KC_BOOTSTRAP_ADMIN_USERNAME used (not deprecated KEYCLOAK_ADMIN) — Keycloak 26.x silently ignores the old variable name
- [Phase 01-infrastructure]: Keycloak healthcheck targets port 9000 via /dev/tcp (no curl in Keycloak 26.x image)
- [Phase 01-infrastructure]: keycloak_db has no host port binding — strictly internal Docker network only
- [Phase 01-infrastructure]: All host port bindings use 127.0.0.1 loopback prefix — prevents 0.0.0.0 exposure
- [Phase 01-infrastructure]: clientScopeMappings used (not scopeMappings) for service account role binding in realm JSON — confirmed correct Keycloak import format
- [Phase 01-infrastructure]: Realm import is idempotent — Keycloak skips re-import if realm already exists in keycloak_db volume
- [Phase 01-infrastructure]: Used classic .sln format (--format sln) — .NET 10 defaults to .slnx but Dockerfile COPY requires Onboarding.sln
- [Phase 01-infrastructure]: package.json type:module required for Vinxi — ESM-only framework; commonjs prevents config loading
- [Phase 01-infrastructure]: createApp (not defineConfig) is the correct Vinxi 0.5.x API — defineConfig is not exported
- [Phase 01-infrastructure]: vinxi dev --port 5173 --host CLI flags used — port from app.config.ts alone is not respected at runtime
- [Phase 01-infrastructure]: index.html added as SPA entry point — required to prevent Vinxi SSR fallback crash (document is not defined)
- [Phase 02-keycloak-security-hardening]: SEC-03 wildcard check excludes Keycloak built-in system clients (account, account-console, security-admin-console) which always have wildcard redirect URIs by design
- [Phase 02-keycloak-security-hardening]: KC_SPI_LOGIN_PROTOCOL__OPENID_CONNECT__REQUEST_URI_ENABLED=false silently accepted by Keycloak 26.1 (no unrecognized-key warning) — feature appears disabled at code level in Keycloak 26.x
- [Phase 02-keycloak-security-hardening]: clientPolicies and clientProfiles imported cleanly from realm JSON on first boot — no Admin API PATCH fallback required
- [Phase 03-backend-domain-layer]: Alphanumeric CNPJ (July 2026): ASCII-48 algorithm is backward-compatible; true alphanumeric test deferred until Receita Federal publishes verified samples
- [Phase 03-backend-domain-layer]: protected Client() for EF Core materialization — CS0628 warning suppressed with pragma; intentional Pitfall 3 pattern
- [Phase 03-backend-domain-layer]: No Password property on Client aggregate — auth credentials belong entirely to Keycloak
- [Phase 03-backend-domain-layer]: No MediatR — ICommandHandler<TCommand,TResult> interface used directly for CQRS; handlers injected via built-in .NET DI (MediatR is commercial)
- [Phase 03-backend-domain-layer]: Password in RegisterClientCommand but absent from Client aggregate — Phase 5 handler will forward to IKeycloakUserService.CreateUserAsync
- [Phase 04-observability]: xUnit 2.9.3 has no Assert.Fail — use true.ShouldBeFalse(message) as RED stub pattern
- [Phase 04-observability]: Single Onboarding.API.Tests project covers all observability test categories (Observability, Security, HealthCheck)
- [Phase 04-observability]: Bootstrap logger pattern used in Program.cs — Log.Logger configured before builder.Build() to capture startup exceptions before DI is ready
- [Phase 04-observability]: SensitiveDataDestructuringPolicy.MaskEmail() is public (not internal) — allows direct test assertion without InternalsVisibleTo
- [Phase 04-observability]: OTel using directives must be explicit in Program.cs — using OpenTelemetry.Trace, OpenTelemetry.Metrics, Serilog.Enrichers.Span not in global usings
- [Phase 11-ux-redesign]: Used `<a>` tags instead of TanStack `<Link>` for Criar/Esqueci links — avoids router context dependency in unit tests
- [Phase 11-ux-redesign]: ProfilePage self-wraps with AuthGuard (not router-level guard) — simpler test isolation
- [Phase 11-ux-redesign]: Auto-login reuses existing `login()` from auth-context — no separate `autoLogin()` function needed
- [Phase 11-ux-redesign]: Zod `superRefine()` used for conditional PF/PJ validation based on `personType` field
- [Post-milestone auth fix]: Refresh token stored in httpOnly cookie (Path=/api, not /api/auth/refresh — too restrictive)
- [Post-milestone auth fix]: Vinxi api-proxy must forward Set-Cookie headers from backend to browser (manual fetch approach, sendProxy caused 405)
- [Post-milestone auth fix]: CORS AllowCredentials required for cookie-based auth — frontend origin http://localhost:5173
- [Post-milestone auth fix]: GET /api/auth/me endpoint created for session restoration on page load

### Pending Todos

None yet.

### Blockers/Concerns

- Phase 5 (Registration API): Need a rollback/compensation strategy if Keycloak user creation fails after app_db persist
- Phase 9 (Login UI): ROPC grant is deprecated in OAuth 2.1 — document migration path for v2

## Session Continuity

Last session: 2026-04-09T13:30:00.000Z
Stopped at: Phase 15 Plan 01 created — Production Cleanup plan ready for execution
Resume file: none

### Last Session Summary

User requested: Plan Phase 15 (Production Cleanup)

**Tasks Completed:**
1. Phase 15 plan 01 created — 5 tasks covering all ROADMAP success criteria
2. Context gathered: dead code identified, cookie config analyzed, failing tests identified
3. STATE.md updated

**Key Findings:**
- `client.tsx` and `LabeledField.tsx` confirmed as dead code (no imports)
- Cookie `Secure = false` hardcoded in 5 locations in AuthController
- 3 tests failing: LoginEndpointTests (1), RefreshTokenEndpointTests (2) — likely assertion mismatch on refresh token location (cookie vs body)
- HealthCheckEndpointTests actually PASS (5/5) — ROADMAP may be outdated on this point
- 5 test files have stale TDD comments (GREEN/RED references)
- Total backend tests: 55 (50 pass, 3 fail, 2 skipped)

**Documents Created:**
- `.planning/phases/15-production-cleanup/15-01-PLAN.md` — Production Cleanup plan
