---
gsd_state_version: 1.0
milestone: v8.0
milestone_name: Gestão de Fundos
status: ready_to_execute
last_updated: "2026-05-03T00:00:00Z"
last_activity: 2026-05-03
progress:
  total_phases: 8
  completed_phases: 0
  total_plans: 2
  completed_plans: 0
  percent: 0
  gaps: []
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-05-02)

**Core value:** Cadastro seguro PJ com gestão de funcionários e permissões via Keycloak — isolamento entre empresas é requisito de primeira classe.
**Current focus:** Phase 45 — Domain Layer (v8.0 Gestão de Fundos)
**Last activity:** 2026-05-02 — Roadmap created for v8.0

## Current Position

Phase: 45 of 52 (Domain Layer)
Plan: 45-01, 45-02
Status: Ready to execute — 2 plans in 2 waves
Last activity: 2026-05-03 — Phase 45 planned

Progress: [░░░░░░░░░░] 0%

## Milestone Breakdown

**Milestone v1.0 — Foundation:** ✅ COMPLETE (10/10 phases, 30/30 plans)
**Milestone v2.0 — UX/UI + Production:** ✅ COMPLETE (5/5 phases, 7+ plans)
**Milestone v3.0 — Admin Backoffice + Frontend Separation:** ✅ COMPLETE (5/5 phases, 13/13 plans)
**Milestone v4.0 — CI/CD Pipeline + Cybersecurity:** ✅ COMPLETE (8/8 phases, 20/20 plans)
**Milestone v5.0 — Auth Code Flow + Admins + Auditoria:** ✅ COMPLETE (6/6 phases)
**Milestone v6.0 — Gestão Completa de Administradores:** ✅ COMPLETE (2/2 phases)
**Milestone v7.0 — PJ-Only Onboarding + Gestão de Funcionários:** ✅ COMPLETE (8/8 phases, 19/19 plans)
**Milestone v8.0 — Gestão de Fundos:** 📋 Phase 45 planned, 0/8 phases complete

## Accumulated Context

### Key Decisions (v8.0)

- D-01: ConsultoriaFundo/Custodiante/Cedente are company-scoped (ClientId, HasQueryFilter)
- D-02: FundoStatus = state machine: RASCUNHO→ATIVO↔SUSPENSO→EM_LIQUIDACAO→ENCERRADO
- D-03: TipoAtivo is global (no ClienteId) — CVM catalog shared across companies
- D-04: LimiteExposicao unlimited = sentinel value (-1)

### Research Findings (v8.0)

- Zero new NuGet packages — existing stack covers all needs
- FundoCedente is a full domain entity with payload (exposure limits, date ranges)
- Cedente is polymorphic PF/PJ — reuses Cpf/Cnpj value objects
- Build order: Domain → Infrastructure → Application → API → Frontend Client → Frontend Backoffice → Tests

### Deferred Items (from v7.0)

| Category | Item | Status |
|----------|------|--------|
| debug | admin-login-403-client-401 | unknown |
| debug | admin-users-list-401 | unknown |
| debug | backend-coverage-77-percent | unknown |
| debug | backoffice-acf-invalid-state | root_cause_identified |
| debug | ci-two-failures | unknown |
| debug | trivy-sca-npm-vulnerabilities | awaiting_human_verify |
| verification | Phase 04, 05, 06, 07, 10, 18, 37, 43 | gaps_found/human_needed |

### Blockers/Concerns

- Isolamento multi-tenant é CRÍTICO — qualquer leak entre empresas é vulnerabilidade de segurança
- Cedente is PF/PJ polymorphic — single entity with conditional CPF/CNPJ validation

## Session Continuity

Last session: 2026-05-03
Stopped at: Phase 45 planned (2 plans), ready to execute
Resume file: None