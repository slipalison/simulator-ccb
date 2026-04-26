---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: — Foundation
status: executing
last_updated: "2026-04-26T12:51:34Z"
last_activity: 2026-04-26
progress:
  total_phases: 41
  completed_phases: 33
  total_plans: 87
  completed_plans: 86
  percent: 99
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-25)

**Core value:** Cadastro seguro PJ com gestão de funcionários e permissões via Keycloak — isolamento entre empresas é requisito de primeira classe.
**Current focus:** Phase 39 — keycloak-groups-permissions
**Last activity:** 2026-04-26

## Current Position

Phase: 39 (keycloak-groups-permissions) — EXECUTING
Plan: 3 of 3
Status: Plan 03 complete — Phase 39 execution finished
Last activity: 2026-04-26 -- Plan 03: handlers + authorization policies committed

## Milestone Breakdown

**Milestone v1.0 — Foundation:** ✅ COMPLETE (10/10 phases, 30/30 plans)
**Milestone v2.0 — UX/UI + Production:** ✅ COMPLETE (5/5 phases, 7+ plans)
**Milestone v3.0 — Admin Backoffice + Frontend Separation:** ✅ COMPLETE (5/5 phases, 13/13 plans)
**Milestone v4.0 — CI/CD Pipeline + Cybersecurity:** ✅ COMPLETE (8/8 phases, 20/20 plans)
**Milestone v5.0 — Auth Code Flow + Admins + Auditoria:** ✅ COMPLETE (6/6 phases)
**Milestone v6.0 — Gestão Completa de Administradores:** ✅ COMPLETE (2/2 phases)
**Milestone v7.0 — PJ-Only Onboarding + Gestão de Funcionários:** 🔄 Active (Phases 37-42)

## Milestone v7.0 Phase Breakdown

| Phase | Name | Requirements | Status |
|-------|------|--------------|--------|
| 37 | Domain Model Redesign | REG-02, REG-04, REG-05 | ✅ Complete |
| 38 | Employee Registration & Management API | REG-01, REG-03, MGMT-01..05 | ✅ Complete |
| 39 | Keycloak Groups & Permissions | PERM-01..05 | ✅ Complete |
| 40 | Client Frontend — PJ Registration & Employee Management | DASH-01 | 📋 Planned |
| 41 | BackOffice Employee Management + Audit | ADM-01, ADM-02, AUD-01, AUD-02 | 📋 Planned |
| 42 | CI Coverage Enforcement | CI-01 | 📋 Planned |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [v7.0]: Cadastro agora é exclusivamente PJ — PF removido do fluxo de registro
- [v7.0]: Permissões via Keycloak roles/groups nativo — Bit Flags no JWT rejeitado
- [v7.0]: Grupos de acesso: Admin Empresa, Viewer, Dashboard (seed automático no registro)
- [v7.0]: Aceite de termos de uso obrigatório (texto mock por enquanto)
- [v7.0]: Dashboard com dados estáticos (mock)
- [v7.0]: Base zerada — docker compose down -v para recriar tudo
- [v7.0]: AccessGroup como entidade no banco com permissões resource:action (employees:read, employees:write, etc.)
- [Phase 37-03]: Cnpj e Cpf nullable no DB — necessário para Anonymize() LGPD que seta VO para null!
- [Phase 39-03]: Keycloak group sync follows eventual consistency — failures logged but not rethrown, DB is source of truth
- [Phase 39-03]: CrossCompanyAccess policy replaces Roles=admin on AdminUserController — semantically equivalent
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
