# Onboarding PF/PJ — Roadmap (adopted)

## Status
adopted: true
current_phase: 48
total_phases: 53

## Context
Projeto adotado em 2026-05-11. Vinha sendo desenvolvido com GSD (`.planning/`, milestones v1-v8). Phases 1-47 estao completas e documentadas em `.planning/phases/`. JDI continua daqui em diante, comecando em **Phase 48** que estava em flight (plans criados em commit `968eefb`, execucao nao iniciada).

Numeracao preservada pra alinhar com `.planning/` historico. Phases pre-48 nao sao re-executadas — sao contexto.

## Phases historicas (GSD `.planning/`, ja completas)
- v1.0 Foundation (1-10): docker infra, Keycloak hardening, DDD domain, observability, registration/auth API, frontend foundation, registration/login/profile UI
- v2.0 UX/UI + Production (11-15)
- v3.0 Admin Backoffice + Frontend Separation (16-20)
- v4.0 CI/CD + Cybersecurity (21-28)
- v5.0 Auth Code Flow + Admins + Auditoria (29-34)
- v6.0 Gestao Completa de Administradores (35-36)
- v7.0 PJ-Only Onboarding + Funcionarios (37-44)
- v8.0 Gestao de Fundos: Phase 45 Domain (done), Phase 46 Infrastructure (done), Phase 47 Application (done — 3 plans com SUMMARY, sem VERIFICATION formal — JDI fara verify retroativo se necessario)

## Phases ativas JDI

### Phase 48: API + Permissions for Fundos
- **Slug:** 48-api-permissions
- **Status:** pending (plans escritos em `.planning/phases/48-api-permissions/48-01-PLAN.md` + `48-02-PLAN.md`, execucao nao iniciada)
- **Goal:** FundosController expoe CRUD pra ConsultoriaFundo/Custodiante/TipoAtivo/Fundo/Cedente + AdminFundosController read-only cross-company + policies funds:read/write/delete/manage + extensao access groups (admin-empresa=funds:manage, viewer=funds:read).
- **Plans existentes (GSD):**
  - 48-01: FundosController parte 1 (ConsultoriaFundo/Custodiante/TipoAtivo) + permission policies + GlobalExceptionHandler enhancement
  - 48-02: FundosController parte 2 (Fundo/Cedente) + AdminFundosController + access group extension
- **Acao JDI:** `/jdi-discuss 48` + `/jdi-plan 48` (pode reusar/importar plans GSD) + `/jdi-do 48`

### Phase 49: FundoCedente & Relationship CRUD
- **Slug:** 49-fundo-cedente-relationships
- **Status:** pending
- **Goal:** Endpoints N-N Fundo↔Cedente (com payload — LimiteExposicaoPercentual/Valor + janela de datas), Cedente↔TipoAtivo, Fundo↔TipoAtivo. Enforcement REL-09 (uma unica associacao ATIVA por par Fundo-Cedente).

### Phase 50: Frontend Client — Fundos UI
- **Slug:** 50-frontend-client-fundos
- **Status:** pending
- **Goal:** SPA cliente PJ ganha secao Fundos (paginacao, search, badges de status, forms Zod espelhando regras backend). Dropdown de status filtra transicoes validas baseado no estado atual (RASCUNHO→ATIVO, ATIVO↔SUSPENSO, etc).

### Phase 51: Frontend Backoffice — Fundos UI (read-only)
- **Slug:** 51-frontend-backoffice-fundos
- **Status:** pending
- **Goal:** Backoffice admin lista/visualiza Fundo/ConsultoriaFundo/Custodiante/Cedente cross-company em read-only. Mostra nome da empresa alongside fund data. Sem create/update/delete (FRO-04).

### Phase 52: Integration Tests — v8.0 Fundos end-to-end
- **Slug:** 52-integration-tests-fundos
- **Status:** pending
- **Goal:** Testcontainers PostgreSQL real cobrindo CRUD round-trip dos 5 entity types + 3 relationship types, isolamento multi-tenant, transicoes de state machine, REL-09, deteccao de duplicatas (409).

### Phase 53: Migracao Vinxi -> Vinext (Cloudflare fork)
- **Slug:** 53-vinxi-to-vinext-migration
- **Status:** pending (decisao user em /jdi-bootstrap)
- **Goal:** Migrar `frontend/client` e `frontend/backoffice` de Vinxi 0.5.11 para Vinext (https://github.com/cloudflare/vinext). Aproveita "Vinext migration debt" acumulada nos SUMMARY.md de phases 50/51 pra mapear changes. Validar build, dev server, Playwright e2e em ambos SPAs apos cutover. Sem regressoes funcionais nem perda de SSR/hydration.
- **Specialist responsavel:** jdi-doer-onboarding-keycloak-frontend-vinext
