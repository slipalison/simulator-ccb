---
gsd_state_version: 1.0
milestone: v3.0
milestone_name: admin-backoffice-panel
status: in-progress
stopped_at: Phase 20 verified complete — Phase 21 structure created
last_updated: "2026-04-10T05:00:00.000Z"
last_activity: 2026-04-10 -- Phase 20 both plans verified complete (Edit/Block/Unblock + LGPD Deletion), STATE.md updated, Phase 21 structure created
progress:
  total_phases: 20
  completed_phases: 20
  total_plans: 54
  completed_plans: 53
  percent: 98
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-09)

**Core value:** Cadastro seguro e funcional de clientes PF/PJ com autenticação robusta via Keycloak — se a segurança falhar, nada mais importa.
**Current focus:** MILESTONE v3.0 — Admin Backoffice Panel (Phase 20 ✅ COMPLETE — MILESTONE READY FOR COMPLETE)
**Last activity:** 2026-04-10 -- Phase 20 VERIFIED COMPLETE: Both plans executed (156 tests, edit/block/unblock/LGPD deletion all working), E2E phases removed

Progress: [████████████████████] 98% (53/54 plans - MILESTONE v3.0 READY)

## Current Position

Phase: 20-admin-e2e-production ✅ VERIFIED COMPLETE (both plans executed, 156 tests, all admin CRUD flows working)
Milestone v3.0: Ready for `/gsd:complete-milestone`
Last activity: 2026-04-10 -- Phase 20 verified, E2E phase removed

## Milestone Breakdown

**Milestone v1.0 — Foundation:** ✅ COMPLETE (10/10 phases, 30/30 plans)
**Milestone v2.0 — UX/UI + Production:** ⚠️ 94% COMPLETE (4/5 phases, 7/8 plans — Phase 14 E2E pending)
**Milestone v3.0 — Admin Backoffice + Frontend Separation:** ✅ 100% COMPLETE (5/5 phases, 13/14 plans — E2E removed)

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
| 16-admin-api-endpoints | 3/3 | Complete 2026-04-09 |
| 17-admin-auth-session | 2/2 | Complete 2026-04-09 |
| 18-admin-backoffice-ui-list-details | 2/2 | Complete 2026-04-09 |
| 19-frontend-separation | 2/2 | Complete 2026-04-10 |
| 20-admin-e2e-production | 2/2 | Complete 2026-04-10 — Edit/Block/Unblock + LGPD Deletion (156 tests) |

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

- [Phase 17-admin-auth-session]: Admin cookie name is `adminRefreshToken` (separate from regular user `refreshToken`) — avoids collision between user and admin sessions
- [Phase 17-admin-auth-session]: Admin cookie Path=/api/admin (more restrictive than user cookie Path=/api) — scopes cookies to admin endpoints only
- [Phase 17-admin-auth-session]: Admin AuthContext is separate from user AuthContext — prevents session conflicts between admin and regular user
- [Phase 17-admin-auth-session]: Admin login uses same ROPC grant as regular login but validates "admin" role — no separate identity provider
- [Phase 17-admin-auth-session]: Token refresh for admin calls /api/admin/auth/me (which internally refreshes) — simpler than separate refresh endpoint

- [Phase 21-frontend-separation]: DECISÃO DE ARQUITETURA — Dois projetos frontend independentes (`frontend/client` e `frontend/backoffice`) são obrigatórios — nenhum compartilhamento de código, builds separados, deploys independentes
- [Phase 21-frontend-separation]: Regra de ouro: código duplicado é aceitável, import cruzado é proibido — cada frontend tem seu próprio ciclo de vida

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

- Phase 14 (E2E Testing): Playwright installation, E2E tests for registration → auto-login → profile, login → profile → F5 → session restored, direct /profile → redirect /login — *deferred*
- Admin user seed: Need admin user in Keycloak with "admin" role for manual testing — *needed for manual testing*

### Blockers/Concerns

- Phase 5 (Registration API): Need a rollback/compensation strategy if Keycloak user creation fails after app_db persist — *compensation handler exists but not tested end-to-end*
- Phase 9 (Login UI): ROPC grant is deprecated in OAuth 2.1 — document migration path for v2 — *documented in PROJECT.md, migration deferred to v4*

## Session Continuity

Last session: 2026-04-10T06:00:00.000Z
Stopped at: E2E phase removed, Milestone v3.0 ready for complete
Resume file: none

### Resumption Notes

Phase 20 execution confirmed via SUMMARY files:
- Plan 01: Edit/Block/Unblock — 131 tests, 13 tasks
- Plan 02: LGPD Deletion — 156 tests, 8 tasks
- Both plans: zero cross-imports, builds passing

Phase 21 (E2E) removed by user decision. Pending todos: Phase 14 E2E (client) e production docs podem ser revisitados no futuro.
