---
gsd_state_version: 1.0
milestone: v5.0
milestone_name: Auth Code Flow + Admins + Auditoria
status: executing
stopped_at: Phases 29-33 complete — ready to plan Phase 34
last_updated: "2026-04-21T00:00:00.000Z"
last_activity: 2026-04-21
progress:
  total_phases: 36
  completed_phases: 33
  total_plans: 72
  completed_plans: 72
  percent: 97
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-14)

**Core value:** Cadastro seguro e funcional de clientes PF/PJ com autenticação robusta via Keycloak — se a segurança falhar, nada mais importa.
**Current focus:** Phase 34 — Isolar Backoffice e Client em Realms Separados
**Last activity:** 2026-04-21 - Verified phases 31-33 complete; fixed CreateAdminPage email display bug

## Current Position

Phase: 34
Plan: Not started
Status: Ready to plan Phase 34
Last activity: 2026-04-21 -- Phases 29-33 verified complete

## Milestone Breakdown

**Milestone v1.0 — Foundation:** ✅ COMPLETE (10/10 phases, 30/30 plans)
**Milestone v2.0 — UX/UI + Production:** ✅ COMPLETE (5/5 phases, 7+ plans)
**Milestone v3.0 — Admin Backoffice + Frontend Separation:** ✅ COMPLETE (5/5 phases, 13/13 plans)
**Milestone v4.0 — CI/CD Pipeline + Cybersecurity:** ✅ COMPLETE (8/8 phases, 20/20 plans)
**Milestone v5.0 — Auth Code Flow + Admins + Auditoria:** 🔄 IN PROGRESS (5/6 phases)

## Milestone v5.0 Phase Breakdown

| Phase | Name | Requirements | Status |
|-------|------|--------------|--------|
| 29 | Keycloak Config + Auth Code Flow Backend | ACF-01, ACF-02, ACF-03, ACF-04 | ✅ Complete |
| 30 | Audit Log Backend + Admin Management Backend | AUD-01, ADM-01, ADM-02, ADM-03, ADM-04 | ✅ Complete |
| 31 | Backoffice Auth Code Flow UI | ACF-01, ACF-02, ACF-03, ACF-04 (frontend) | ✅ Complete |
| 32 | Backoffice Admin Management UI + Audit Log UI | ADM-01, ADM-02, ADM-03, ADM-04, AUD-02, AUD-03 | ✅ Complete |
| 33 | PKCE + Custom Keycloak Themes (Backoffice + Client) | PKC-01..PKC-06 | ✅ Complete |
| 34 | Isolar Backoffice e Client em Realms Separados | ARCH-04 | ⬜ Not started |

**Coverage:** 11/11 v5.0 requirements + Phase 33 (PKC) + Phase 34 (realms) adicionadas ✓

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Phase 21-frontend-separation]: DECISÃO DE ARQUITETURA — Dois projetos frontend independentes (`frontend/client` e `frontend/backoffice`) são obrigatórios — nenhum compartilhamento de código, builds separadas, deploys independentes
- [Phase 21-frontend-separation]: Regra de ouro: código duplicado é aceitável, import cruzado é proibido — cada frontend tem seu próprio ciclo de vida
- [v5.0-auth-code-flow]: Backoffice migra para Auth Code Flow + PKCE (confidential client). Client frontend mantém ROPC (decisão consciente — UX priorizada)
- [v5.0-keycloak-client]: Novo client Keycloak `onboarding-backoffice` (confidential, standardFlowEnabled) com redirect URIs exatos — o client `onboarding-app` (ROPC) não é modificado
- [v5.0-audit-log]: AuditLog é append-only — nenhuma operação UPDATE ou DELETE é permitida na tabela. Nova EF Core migration em Phase 30.
- [v5.0-temp-password]: Senha temporária é gerada pelo backend, exibida UMA VEZ na UI, e não é armazenada. Keycloak força troca via UPDATE_PASSWORD requiredAction.

### Key Architecture for v5.0

**Phase 29 — Auth Code Flow Infrastructure:**

- New Keycloak client: `onboarding-backoffice` (confidential, standardFlowEnabled, exact redirect URIs)
- Vinxi server handles authorization code exchange (server-side) using client_secret
- Tokens written to httpOnly, Secure, SameSite=Strict cookies by Vinxi server actions
- Existing AdminAuthController (ROPC) endpoints are retired/removed
- Forced password change (UPDATE_PASSWORD requiredAction) is handled natively by Keycloak during Auth Code Flow — no extra backend code

**Phase 30 — Backend Data + API:**

- New `AuditLog` entity in app_db with EF Core migration (append-only)
- `IAuditService` injected into existing admin command handlers to record actions
- New endpoints: POST /api/admin/administrators, GET /api/admin/administrators
- Temporary password generated via `System.Security.Cryptography.RandomNumberGenerator`
- Keycloak.AuthServices.Sdk used for Admin API (create user + assign role + set requiredActions)

**Phase 31 — Backoffice Frontend ACF:**

- Remove AdminAuthController ROPC login form and replace with redirect flow
- Vinxi server route `/auth/callback` handles code exchange via server action
- Session guard middleware checks httpOnly cookie presence (using jose for JWT decode)
- Logout: clear cookies + redirect to Keycloak OIDC logout endpoint

**Phase 32 — Backoffice Frontend UI:**

- Create Administrator form (name + email) → calls POST /api/admin/administrators → modal shows one-time password
- Administrators list page → calls GET /api/admin/administrators
- Audit log page: paginated table with date range / action type / actor email filters (client-side state, server-side query params)

### Pending Todos

- Phase 14 (E2E Testing): Playwright installation, E2E tests for registration → auto-login → profile, login → profile → F5 → session restored, direct /profile → redirect /login — *deferred*
- Admin user seed: Need admin user in Keycloak with "admin" role for manual testing — *needed for manual testing*
- GitHub UI follow-ups (v4.0): branch protection on `main`, Dependabot alerts, first CI run, review security findings

### Roadmap Evolution

- Phase 33 adicionada: PKCE + Custom Keycloak Themes para Backoffice e Client (2026-04-15) — ROPC descartado por impossibilidade de 2FA; ACF+PKCE com custom themes substitui abordagem anterior
- Phase 34 added: Isolar Backoffice e Client em Realms Separados

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 260416-vq1 | fix keycloak hostname in frontend ACF redirect — use localhost instead of docker internal hostname | 2026-04-17 | f3005e5 | [260416-vq1-fix-keycloak-hostname-in-frontend-acf-re](./quick/260416-vq1-fix-keycloak-hostname-in-frontend-acf-re/) |
| 260417-eu6 | fix backoffice ACF token exchange — remove offline_access from requested scope | 2026-04-17 | 54b3995 | [260417-eu6-fix-backoffice-acf-token-exchange-remove](./quick/260417-eu6-fix-backoffice-acf-token-exchange-remove/) |
| 260418-d25 | fix admin API 401 by reading backoffice_access_token cookie + remove ROPC legacy controller | 2026-04-18 | f771642 | [260418-d25-fix-admin-api-401-by-reading-backoffice-](./quick/260418-d25-fix-admin-api-401-by-reading-backoffice-/) |
| 260418-dwi | force re-login after first password change (isFirstLogin flag + callback detection) | 2026-04-18 | fa4ef9b | [260418-dwi-force-re-login-after-first-password-chan](./quick/260418-dwi-force-re-login-after-first-password-chan/) |

### Blockers/Concerns

- Phase 5 (Registration API): Need a rollback/compensation strategy if Keycloak user creation fails after app_db persist — *compensation handler exists but not tested end-to-end*
- Phase 9 (Login UI): ROPC grant is deprecated in OAuth 2.1 — document migration path — *documented in PROJECT.md, backoffice migration in v5.0*

## Session Continuity

Last session: 2026-04-21
Stopped at: Phases 29-33 verified complete; STATE.md updated; ready to plan Phase 34
Resume file: none

### Milestone v5.0 Requirements (2026-04-14)

**ACF — Auth Code Flow (Backoffice)** → Phase 29 (backend) + Phase 31 (frontend)

- ACF-01: Admin login via Auth Code Flow + PKCE (confidential client, server-side code exchange)
- ACF-02: Forced password change works natively via Keycloak requiredActions
- ACF-03: Tokens stored in httpOnly cookies managed by Vinxi server
- ACF-04: Logout clears cookies and redirects to Keycloak OIDC logout endpoint

**ADM — Gestão de Administradores** → Phase 30 (backend) + Phase 32 (frontend)

- ADM-01: Admin can create new administrator (name + email) in backoffice
- ADM-02: System generates temporary password displayed once to creator
- ADM-03: New admin gets role "admin" + UPDATE_PASSWORD requiredAction in Keycloak via Admin API
- ADM-04: Admin can list other administrators in backoffice

**AUD — Auditoria** → Phase 30 (backend) + Phase 32 (frontend)

- AUD-01: All admin actions recorded append-only (actor, action, target, timestamp, details JSON)
- AUD-02: Admin can view paginated audit log in backoffice
- AUD-03: Audit log supports filters by date range, action type, and actor email
