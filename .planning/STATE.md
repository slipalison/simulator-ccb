---
gsd_state_version: 1.0
milestone: v7.0
milestone_name: PJ-Only Onboarding + Gestão de Funcionários
status: defining_requirements
stopped_at: Requirements definition in progress
last_updated: "2026-04-25T00:00:00.000Z"
last_activity: 2026-04-25
progress:
  total_phases: 36
  completed_phases: 36
  total_plans: 81
  completed_plans: 81
  percent: 100
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-25)

**Core value:** Cadastro seguro PJ com gestão de funcionários e permissões via Keycloak — isolamento entre empresas é requisito de primeira classe.
**Current focus:** Milestone v7.0 — Defining requirements
**Last activity:** 2026-04-25

## Current Position

Phase: Not started (defining requirements)
Plan: —
Status: Defining requirements
Last activity: 2026-04-25 — Milestone v7.0 started

## Milestone Breakdown

**Milestone v1.0 — Foundation:** ✅ COMPLETE (10/10 phases, 30/30 plans)
**Milestone v2.0 — UX/UI + Production:** ✅ COMPLETE (5/5 phases, 7+ plans)
**Milestone v3.0 — Admin Backoffice + Frontend Separation:** ✅ COMPLETE (5/5 phases, 13/13 plans)
**Milestone v4.0 — CI/CD Pipeline + Cybersecurity:** ✅ COMPLETE (8/8 phases, 20/20 plans)
**Milestone v5.0 — Auth Code Flow + Admins + Auditoria:** ✅ COMPLETE (6/6 phases)
**Milestone v6.0 — Gestão Completa de Administradores:** ✅ COMPLETE (2/2 phases)

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [v7.0]: Cadastro agora é exclusivamente PJ — PF removido do fluxo de registro
- [v7.0]: Permissões via Keycloak roles/groups nativo — Bit Flags no JWT rejeitado
- [v7.0]: Grupos de acesso: Admin Empresa, Viewer, Dashboard
- [v7.0]: Aceite de termos de uso obrigatório (texto mock por enquanto)
- [v7.0]: Dashboard com dados estáticos (mock)
- [v7.0]: Base zerada — docker compose down -v para recriar tudo
- [Phase 21-frontend-separation]: DECISÃO DE ARQUITETURA — Dois projetos frontend independentes (`frontend/client` e `frontend/backoffice`) são obrigatórios — nenhum compartilhamento de código, builds separadas, deploys independentes
- [Phase 21-frontend-separation]: Regra de ouro: código duplicado é aceitável, import cruzado é proibido
- [v5.0-audit-log]: AuditLog é append-only — nenhuma operação UPDATE ou DELETE é permitida na tabela
- [v5.0-temp-password]: Senha temporária é gerada pelo backend, exibida UMA VEZ na UI, não é armazenada

### Pending Todos

- Phase 14 (E2E Testing): Playwright installation, E2E tests — *deferred*
- Admin user seed: Need admin user in Keycloak with "admin" role for manual testing — *needed for manual testing*

### Blockers/Concerns

- Isolamento entre empresas é CRÍTICO — qualquer bug que permita PJ ver dados de outra PJ é vulnerabilidade de segurança
- Migração de PF para "funcionário" requer redesign do domain model (aggregate Client PF/PJ → Company + Employee)