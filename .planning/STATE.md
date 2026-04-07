---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: executing
stopped_at: Phase 08 complete — Registration UI delivered
last_updated: "2026-04-07T20:00:00.000Z"
last_activity: 2026-04-07 -- Phase 08 Registration UI completa: 3/3 plans, registration forms com validação + API integration + redirect login
progress:
  total_phases: 10
  completed_phases: 8
  total_plans: 27
  completed_plans: 24
  percent: 80
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-01)

**Core value:** Cadastro seguro e funcional de clientes PF/PJ com autenticação robusta via Keycloak — se a segurança falhar, nada mais importa.
**Current focus:** Phase 09 — Login UI (próxima)

## Current Position

Phase: 08 (registration-ui) — COMPLETE
Next: 09 (login-ui) — NOT STARTED
Last activity: 2026-04-07 -- Phase 08 completa; registration forms com validação client-side, API integration, redirect para /login

Progress: [████████░░] 80% (8/10 fases)

## Performance Metrics

**Velocity:**

- Total plans completed: 21
- Phases completas: 7/10

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
| 08-registration-ui | 0/3 | Planned 2026-04-07 |

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

### Pending Todos

None yet.

### Blockers/Concerns

- Phase 5 (Registration API): Need a rollback/compensation strategy if Keycloak user creation fails after app_db persist
- Phase 9 (Login UI): ROPC grant is deprecated in OAuth 2.1 — document migration path for v2

## Session Continuity

Last session: 2026-04-07T19:00:00.000Z
Stopped at: Phase 08 Registration UI planejada — 3 plans criados (08-01, 08-02, 08-03)
Resume file: none
