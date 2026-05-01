---
gsd_state_version: 1.0
milestone: v7.0
milestone_name: PJ-Only Onboarding + Gestão de Funcionários
status: ready
last_updated: "2026-05-01T12:00:00Z"
last_activity: 2026-05-01
progress:
  total_phases: 8
  completed_phases: 8
  total_plans: 19
  completed_plans: 19
  percent: 100
  gaps: []
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-05-01)

**Core value:** Cadastro seguro PJ com gestão de funcionários e permissões via Keycloak — isolamento entre empresas é requisito de primeira classe.
**Current focus:** Milestone v7.0 archived — awaiting next milestone
**Last activity:** 2026-05-01

## Current Position

Phase: v7.0 — ✅ ARCHIVED
Status: Milestone v7.0 archived to .planning/milestones/. Awaiting v8.0 definition.
Last activity: 2026-05-01 — Milestone v7.0 archived

## Milestone Breakdown

**Milestone v1.0 — Foundation:** ✅ COMPLETE (10/10 phases, 30/30 plans)
**Milestone v2.0 — UX/UI + Production:** ✅ COMPLETE (5/5 phases, 7+ plans)
**Milestone v3.0 — Admin Backoffice + Frontend Separation:** ✅ COMPLETE (5/5 phases, 13/13 plans)
**Milestone v4.0 — CI/CD Pipeline + Cybersecurity:** ✅ COMPLETE (8/8 phases, 20/20 plans)
**Milestone v5.0 — Auth Code Flow + Admins + Auditoria:** ✅ COMPLETE (6/6 phases)
**Milestone v6.0 — Gestão Completa de Administradores:** ✅ COMPLETE (2/2 phases)
**Milestone v7.0 — PJ-Only Onboarding + Gestão de Funcionários:** ✅ COMPLETE (8/8 phases, 19/19 plans)

## Milestone v7.0 Phase Breakdown

| Phase | Name | Requirements | Status |
|-------|------|--------------|--------|
| 37 | Domain Model Redesign | REG-02, REG-04, REG-05 | ✅ Complete |
| 38 | Employee Registration & Management API | REG-01, REG-03, MGMT-01..05 | ✅ Complete |
| 39 | Keycloak Groups & Permissions | PERM-01..05 | ✅ Complete |
| 40 | Client Frontend — PJ Registration & Employee Management | DASH-01, REG-01, REG-05, MGMT-01..05, PERM-04 | ✅ Complete |
| 41 | BackOffice Employee Management + Audit | ADM-01, ADM-02, AUD-01, AUD-02 | ✅ Complete |
| 42 | CI Coverage Enforcement | CI-01 | ✅ Complete |
| 43 | E2E Playwright Validation | E2E-01..07 | ✅ Complete |
| 44 | Custom Access Groups CRUD | PERM-04 (extended), PERM-06 | ✅ Complete |

## Accumulated Context

### Roadmap Evolution

- Phase 43 added: E2E Playwright Validation — create PJ, login, dashboard, create employee, login employee, validate permissions UI + JWT
- Phase 44 implemented: Custom Access Groups CRUD — backend commands + handlers + controller endpoints + frontend AccessGroupsPage + dialogs

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
- [v7.0]: Default groups (admin-empresa, viewer, dashboard) são imutáveis — PJ pode criar/editar/deletar groups customizados com qualquer combinação de permissões
- [v7.0]: Novo requisito PERM-06: CRUD de access groups customizados — extensível conforme o sistema cresce
- [Phase 37-03]: Cnpj e Cpf nullable no DB — necessário para Anonymize() LGPD que seta VO para null!
- [Phase 39-03]: Keycloak group sync follows eventual consistency — failures logged but not rethrown, DB is source of truth
- [Phase 39-03]: CrossCompanyAccess policy replaces Roles=admin on AdminUserController — semantically equivalent
- [Phase 43-01]: ESM-compatible Playwright setup files using import.meta.url instead of __dirname
- [Phase 43-01]: Single worker mode (workers: 1) to avoid Keycloak brute-force lockout
- [Phase 43-01]: No webServer config — Docker Compose must be running before E2E tests
- [Phase 43-02]: E2E-03 creates employees via API (POST /registration) since no UI form exists in Phase 40
- [Phase 43-02]: Card title locators in dashboard.page.ts must match exact CardTitle text from DashboardCards.tsx
- [Phase 43-03]: Employee-login spec uses fresh ACF logins (no storageState) to test redirect behavior from scratch
- [Phase 43-03]: Permission-ui spec runs in both viewer and admin-empresa projects — test.skip() guards admin-empresa-only test
- [Phase 43-03]: Dashboard employee login test conditional on E2E_DASHBOARD_EMAIL env var (skipped if not set)
- [Phase 21-frontend-separation]: DECISÃO DE ARQUITETURA — Dois projetos frontend independentes (`frontend/client` e `frontend/backoffice`) são obrigatórios — nenhum compartilhamento de código, builds separadas, deploys independentes
- [Phase 21-frontend-separation]: Regra de ouro: código duplicado é aceitável, import cruzado é proibido
- [v5.0-audit-log]: AuditLog é append-only — nenhuma operação UPDATE ou DELETE é permitida na tabela
- [v5.0-temp-password]: Senha temporária é gerada pelo backend, exibida UMA VEZ na UI, não é armazenada

### Pending Todos

- Phase 14 (E2E Testing v2.0): Absorvido pela Phase 43
- Admin user seed: Need admin user in Keycloak with "admin" role for manual testing

### v7.0 Integration Gaps — ALL RESOLVED

All gaps identified in the 2026-04-28 audit have been verified as resolved in code:

1. ~~GAP-6~~: RegisterEmployeeDialog.tsx exists with full form (nome, cpf, email, phone, accessGroupId)
2. ~~GAP-8+9~~: Backoffice has AdminCompaniesPage + AdminEmployeesPage; /admin/users redirects to /admin/companies
3. ~~GAP-1+4~~: AccessGroupName resolved via _accessGroupRepository.GetByIdAsync() in both client and admin handlers
4. ~~GAP-2~~: KeycloakEnabled present in EmployeeListItemDto + handler queries Keycloak
5. ~~GAP-3~~: changeEmployeeAccessGroup sends { accessGroupId: GUID } — matches backend ChangeAccessGroupRequest(Guid AccessGroupId)
6. ~~GAP-11~~: KEYCLOAK_REALM=client and KEYCLOAK_REALM=backoffice present in compose.yaml
7. ~~GAP-hasqueryfilter~~: Fixed with IgnoreQueryFilters for admin queries
8. ~~GAP-register-error-ux~~: apiError state + display in RegisterEmployeeDialog

### Blockers/Concerns

- Isolamento entre empresas é CRÍTICO — qualquer bug que permita PJ ver dados de outra PJ é vulnerabilidade de segurança

## Deferred Items

Items acknowledged and deferred at milestone close on 2026-05-01:

| Category | Item | Status |
|----------|------|--------|
| debug | admin-login-403-client-401 | unknown |
| debug | admin-users-list-401 | unknown |
| debug | backend-coverage-77-percent | unknown |
| debug | backoffice-acf-invalid-state | root_cause_identified |
| debug | ci-two-failures | unknown |
| debug | trivy-sca-npm-vulnerabilities | awaiting_human_verify |
| uat | Phase 15 UAT | unknown |
| verification | Phase 04 VERIFICATION | gaps_found |
| verification | Phase 05 VERIFICATION | human_needed |
| verification | Phase 06 VERIFICATION | human_needed |
| verification | Phase 07 VERIFICATION | human_needed |
| verification | Phase 10 VERIFICATION | human_needed |
| verification | Phase 18 VERIFICATION | gaps_found |
| verification | Phase 37 VERIFICATION | gaps_found |
| verification | Phase 43 VERIFICATION | human_needed |
| quick_task | fix-keycloak-hostname-in-frontend-acf | missing |
| quick_task | fix-backoffice-acf-token-exchange | missing |
| quick_task | corrigir-401-em-api-admin | missing |
| quick_task | fix-admin-api-401-by-reading-backoffice | missing |
| quick_task | force-re-login-after-first-password-change | missing |
| context | Phase 34 CONTEXT (2 questions) | open |

## Session Continuity

Last session: 2026-05-01
Stopped at: Milestone v7.0 validated complete — all gaps resolved, Phase 44 implemented