# Onboarding PF/PJ — Roadmap (adopted)

## Status
adopted: true
current_phase: 1
total_phases: 5

## Context
Projeto adotado em 2026-05-11. Codigo pre-existente nao esta neste roadmap — apenas features NOVAS adicionadas via JDI.

Existe sistema paralelo `.planning/` (legado, milestones v1-v8). Phases 1-3 deste roadmap derivam do trabalho em flight em `.planning/phases/48-50` da milestone v8.0 (Gestao de Fundos). Phases 4-5 sao gaps conhecidos. Phases futuras adicionadas via `/jdi-add-phase`.

## Phases

### Phase 1: API + Permissions for Fundos module
- **Slug:** 01-api-permissions-fundos
- **Status:** pending
- **Goal:** Expor endpoints REST para o modulo Fundos (Fundo, Cedente, Custodiante, ConsultoriaFundo, TipoAtivo) via FundosController + politicas de permissao (funds:read/write/admin), com isolamento multi-tenant aplicado.

### Phase 2: Frontend backoffice — Fundos UI
- **Slug:** 02-backoffice-fundos-ui
- **Status:** pending
- **Goal:** Telas administrativas no backoffice para CRUD de fundos e entidades relacionadas, com paginacao, filtros e respeito as permissoes funds:*.

### Phase 3: E2E Playwright — Fundos workflow
- **Slug:** 03-e2e-fundos
- **Status:** pending
- **Goal:** Cenarios end-to-end Playwright cobrindo lifecycle de Fundo (RASCUNHO -> ATIVO -> SUSPENSO -> EM_LIQUIDACAO -> ENCERRADO) e gestao de Cedentes/Custodiantes via backoffice.

### Phase 4: Remove ROPC legado backoffice
- **Slug:** 04-remove-ropc-legacy
- **Status:** pending
- **Goal:** Remover fluxo ROPC residual do backoffice apos migracao completa pra ACF+PKCE (referencia D-feedback memoria). Limpar codigo morto, configs Keycloak, testes.

### Phase 5: Audit log — Fundos events
- **Slug:** 05-audit-log-fundos
- **Status:** pending
- **Goal:** Estender AdminAuditLog para registrar eventos de Fundos (create/update/status-transition/delete) com actor sub/email ja capturados nos commands (commit 93a7332).
