---
gsd_state_version: 1.0
milestone: v7.0
milestone_name: PJ-Only Onboarding + Gestão de Funcionários
status: in_progress
last_updated: "2026-04-28T00:00:00Z"
last_activity: 2026-04-28
progress:
  total_phases: 8
  completed_phases: 3
  total_plans: 19
  completed_plans: 19
  percent: 65
  gaps:
    - id: GAP-6
      priority: P0
      description: "Sem formulário de registro de funcionário no frontend client"
    - id: GAP-8-9
      priority: P0
      description: "Backoffice chama /users (404) — falta páginas Empresas/Funcionários"
    - id: GAP-1-4
      priority: P1
      description: "AccessGroupName sempre vazio nos handlers (client + admin)"
    - id: GAP-2
      priority: P1
      description: "KeycloakEnabled ausente no EmployeeListItemDto"
    - id: GAP-3
      priority: P1
      description: "changeEmployeeAccessGroup: frontend manda name, backend espera GUID"
    - id: GAP-11
      priority: P2
      description: "KEYCLOAK_REALM=client pode faltar no docker-compose"
    - id: GAP-hasqueryfilter
      priority: P0
      description: "HasQueryFilter captura ICurrentCompanyService do primeiro request (Guid.Empty), quebrando EmployeeRepository e AccessGroupRepository — fix aplicado com IgnoreQueryFilters"
    - id: GAP-register-error-ux
      priority: P1
      description: "RegisterEmployeeDialog engolia erros da API sem mostrar ao usuário — fix aplicado: apiError state + exibição"
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-04-25)

**Core value:** Cadastro seguro PJ com gestão de funcionários e permissões via Keycloak — isolamento entre empresas é requisito de primeira classe.
**Current focus:** Fixing v7.0 gaps — backend OK, frontend integration quebrada
**Last activity:** 2026-04-28

## Current Position

Phase: v7.0 gap fixes — IN PROGRESS
Status: Backend implementado, mas 5 gaps de integração frontend impedem entrega real. Plans foram executados mas resultados não validados end-to-end.
Last activity: 2026-04-28 -- Audit revelou gaps P0+P1

## Milestone Breakdown

**Milestone v1.0 — Foundation:** ✅ COMPLETE (10/10 phases, 30/30 plans)
**Milestone v2.0 — UX/UI + Production:** ✅ COMPLETE (5/5 phases, 7+ plans)
**Milestone v3.0 — Admin Backoffice + Frontend Separation:** ✅ COMPLETE (5/5 phases, 13/13 plans)
**Milestone v4.0 — CI/CD Pipeline + Cybersecurity:** ✅ COMPLETE (8/8 phases, 20/20 plans)
**Milestone v5.0 — Auth Code Flow + Admins + Auditoria:** ✅ COMPLETE (6/6 phases)
**Milestone v6.0 — Gestão Completa de Administradores:** ✅ COMPLETE (2/2 phases)
**Milestone v7.0 — PJ-Only Onboarding + Gestão de Funcionários:** 🔧 IN PROGRESS (3/8 phases truly complete, 4 phases com gaps de integração, 1 phase planned)

## Milestone v7.0 Phase Breakdown

| Phase | Name | Requirements | Status |
|-------|------|--------------|--------|
| 37 | Domain Model Redesign | REG-02, REG-04, REG-05 | ✅ Complete |
| 38 | Employee Registration & Management API | REG-01, REG-03, MGMT-01..05 | ✅ Complete |
| 39 | Keycloak Groups & Permissions | PERM-01..05 | ✅ Complete |
| 40 | Client Frontend — PJ Registration & Employee Management | DASH-01, REG-01, REG-05, MGMT-01..05, PERM-04 | 🔧 Gaps: sem formulário registro funcionário, accessGroupId vs name, AccessGroupName vazio, KeycloakEnabled ausente |
| 41 | BackOffice Employee Management + Audit | ADM-01, ADM-02, AUD-01, AUD-02 | 🔧 Gaps: chama /users (404), falta páginas Empresas/Funcionários |
| 42 | CI Coverage Enforcement | CI-01 | ✅ Complete |
| 43 | E2E Playwright Validation | E2E-01..07 | ⚠️ Passa mas mascara gaps (cria employee via API, não via UI) |
| 44 | Custom Access Groups CRUD | PERM-04 (extended), PERM-06 | 📋 Planned |

## Accumulated Context

### Roadmap Evolution

- Phase 43 added: E2E Playwright Validation — create PJ, login, dashboard, create employee, login employee, validate permissions UI + JWT

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

### v7.0 Integration Gaps (blocking milestone completion)

**P0 — Crítico (funcionalidade inexistente)**:
1. **GAP-6**: Sem formulário "Novo Funcionário" no frontend client — E2E cria employee via API, não via UI
2. **GAP-8+9**: Backoffice chama `/api/admin/users/*` (endpoints removidos na Phase 37) — seção Usuários = 404. Faltam páginas Empresas + Funcionários

**P1 — Funcionalidade quebrada**:
3. **GAP-1+4**: `AccessGroupName` sempre vazio/string vazia nos handlers de listagem (client + admin)
4. **GAP-2**: `KeycloakEnabled` ausente no `EmployeeListItemDto` — frontend não mostra status do funcionário
5. **GAP-3**: `changeEmployeeAccessGroup()` frontend manda `{ accessGroupName: string }`, backend espera `{ accessGroupId: guid }` — troca de grupo retorna 400

**P2 — Verificar**:
6. **GAP-11**: `KEYCLOAK_REALM=client` pode faltar no docker-compose (hardcoded "onboarding" como fallback)

### Blockers/Concerns

- Isolamento entre empresas é CRÍTICO — qualquer bug que permita PJ ver dados de outra PJ é vulnerabilidade de segurança
- Migração de PF para "funcionário" requer redesign do domain model (aggregate Client PF/PJ → Company + Employee)
- Backoffice precisa reescrever API client para usar `/companies` e `/employees` ao invés de `/users`
