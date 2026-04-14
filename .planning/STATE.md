---
gsd_state_version: 1.0
milestone: v5.0
milestone_name: backoffice-gestao-administradores
status: in_progress
stopped_at: Phase 29 planned - ready to execute
last_updated: "2026-04-14T12:30:00.000Z"
last_activity: 2026-04-14 -- Phase 29 planned (29-01-PLAN.md created)
progress:
  total_phases: 1
  completed_phases: 0
  total_plans: 1
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-14)

**Core value:** Cadastro seguro e funcional de clientes PF/PJ com autenticação robusta via Keycloak — se a segurança falhar, nada mais importa.
**Current focus:** MILESTONE v5.0 — Backoffice: Gestão de Administradores (requirements defined, ready to plan)
**Last activity:** 2026-04-14 -- Milestone v5.0 requirements defined

## Current Position

Phase: Ready to plan
Plan: —
Status: Requirements defined — 3 requirements captured
Last activity: 2026-04-14 — Milestone v5.0 requirements discussion complete

## Milestone Breakdown

**Milestone v1.0 — Foundation:** ✅ COMPLETE (10/10 phases, 30/30 plans)
**Milestone v2.0 — UX/UI + Production:** ✅ COMPLETE (5/5 phases, 7+ plans)
**Milestone v3.0 — Admin Backoffice + Frontend Separation:** ✅ COMPLETE (5/5 phases, 13/13 plans)
**Milestone v4.0 — CI/CD Pipeline + Cybersecurity:** ✅ COMPLETE (8/8 phases, 20/20 plans)
**Milestone v5.0 — Backoffice: Gestão de Administradores:** 🔄 IN PROGRESS (requirements phase)

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Phase 21-frontend-separation]: DECISÃO DE ARQUITETURA — Dois projetos frontend independentes (`frontend/client` e `frontend/backoffice`) são obrigatórios — nenhum compartilhamento de código, builds separadas, deploys independentes
- [Phase 21-frontend-separation]: Regra de ouro: código duplicado é aceitável, import cruzado é proibido — cada frontend tem seu próprio ciclo de vida

### Pending Todos

- Phase 14 (E2E Testing): Playwright installation, E2E tests for registration → auto-login → profile, login → profile → F5 → session restored, direct /profile → redirect /login — *deferred*
- Admin user seed: Need admin user in Keycloak with "admin" role for manual testing — *needed for manual testing*
- GitHub UI follow-ups (v4.0): branch protection on `main`, Dependabot alerts, first CI run, review security findings

### Blockers/Concerns

- Phase 5 (Registration API): Need a rollback/compensation strategy if Keycloak user creation fails after app_db persist — *compensation handler exists but not tested end-to-end*
- Phase 9 (Login UI): ROPC grant is deprecated in OAuth 2.1 — document migration path — *documented in PROJECT.md, migration deferred*

## Session Continuity

Last session: 2026-04-14
Stopped at: Phase 29 planned - ready to execute Plan 01
Resume file: none

### Milestone v5.0 Requirements Captured (2026-04-14)

**1. Troca obrigatória de senha no primeiro login:**
- Admin seedado (`admin@onboarding.local`) muda `"temporary": false` para `"temporary": true` no realm.json
- Quando admin faz login pela primeira vez, sistema detecta e redireciona para tela de troca de senha
- Novos admins criados pelo backoffice também recebem `temporary: true` via Keycloak Admin API

**2. Criação de novos admins pelo backoffice:**
- Admin autenticado preenche nome + email do novo admin
- Sistema gera senha temporária aleatória e cria usuário no Keycloak com role "admin"
- Senha temporária é exibida na tela para quem criou comunicar ao novo admin

**3. Audit log imutável + visível com filtros:**
- Todas as ações administrativas são registradas (criação de admin, login/logout, edições, bloqueios, exclusões)
- Audit log é append-only (imutável) no banco de dados
- Visível no backoffice com filtros por: data, tipo de ação, usuário que realizou ação
