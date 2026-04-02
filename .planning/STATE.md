---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: executing
stopped_at: Completed 03-backend-domain-layer 03-01-PLAN.md — 33 domain tests green, zero-dependency domain layer
last_updated: "2026-04-02T21:16:33.972Z"
last_activity: 2026-04-02
progress:
  total_phases: 10
  completed_phases: 2
  total_plans: 6
  completed_plans: 5
  percent: 0
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-01)

**Core value:** Cadastro seguro e funcional de clientes PF/PJ com autenticação robusta via Keycloak — se a segurança falhar, nada mais importa.
**Current focus:** Phase 03 — backend-domain-layer

## Current Position

Phase: 03 (backend-domain-layer) — EXECUTING
Plan: 2 of 2
Status: Ready to execute
Last activity: 2026-04-02

Progress: [░░░░░░░░░░] 0%

## Performance Metrics

**Velocity:**

- Total plans completed: 0
- Average duration: -
- Total execution time: 0 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| - | - | - | - |

**Recent Trend:**

- Last 5 plans: -
- Trend: -

*Updated after each plan completion*
| Phase 01-infrastructure P01 | 2 | 3 tasks | 7 files |
| Phase 01-infrastructure P02 | 8 | 2 tasks | 1 files |
| Phase 01-infrastructure P03 | 4 | 2 tasks | 14 files |
| Phase 01-infrastructure P03 | 60 | 3 tasks | 15 files |
| Phase 02-keycloak-security-hardening P01 | 6 | 3 tasks | 4 files |
| Phase 03-backend-domain-layer P01 | 4 | 2 tasks | 16 files |

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

### Pending Todos

None yet.

### Blockers/Concerns

- Phase 5 (Registration API): Need a rollback/compensation strategy if Keycloak user creation fails after app_db persist
- Phase 9 (Login UI): ROPC grant is deprecated in OAuth 2.1 — document migration path for v2

## Session Continuity

Last session: 2026-04-02T21:16:33.969Z
Stopped at: Completed 03-backend-domain-layer 03-01-PLAN.md — 33 domain tests green, zero-dependency domain layer
Resume file: None
