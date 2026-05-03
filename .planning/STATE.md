---
gsd_state_version: 1.0
milestone: v8.0
milestone_name: Gestão de Fundos
status: context_gathered
last_updated: "2026-05-03T18:00:00Z"
last_activity: 2026-05-03
progress:
  total_phases: 8
  completed_phases: 1
  total_plans: 2
  completed_plans: 2
  percent: 12
  gaps: []
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-05-02)

**Core value:** Cadastro seguro PJ com gestão de funcionários e permissões via Keycloak — isolamento entre empresas é requisito de primeira classe.
**Current focus:** Phase 46 — Infrastructure Layer (v8.0 Gestão de Fundos)
**Last activity:** 2026-05-03 — Phase 45 complete

## Current Position

Phase: 46 of 52 (Infrastructure Layer) — 🔄 Context gathered
Plan: Context captured, ready for planning
Status: CONTEXT.md created. Key decisions: D-09 shadow properties for CedenteDocumento, D-11 partial unique index for FundoCedente, D-12 EmployeeRepository pattern for repos

Progress: [█░░░░░░░░░] 12%

## Milestone Breakdown

**Milestone v1.0 — Foundation:** ✅ COMPLETE (10/10 phases, 30/30 plans)
**Milestone v2.0 — UX/UI + Production:** ✅ COMPLETE (5/5 phases, 7+ plans)
**Milestone v3.0 — Admin Backoffice + Frontend Separation:** ✅ COMPLETE (5/5 phases, 13/13 plans)
**Milestone v4.0 — CI/CD Pipeline + Cybersecurity:** ✅ COMPLETE (8/8 phases, 20/20 plans)
**Milestone v5.0 — Auth Code Flow + Admins + Auditoria:** ✅ COMPLETE (6/6 phases)
**Milestone v6.0 — Gestão Completa de Administradores:** ✅ COMPLETE (2/2 phases)
**Milestone v7.0 — PJ-Only Onboarding + Gestão de Funcionários:** ✅ COMPLETE (8/8 phases, 19/19 plans)
**Milestone v8.0 — Gestão de Fundos:** 🔄 1/8 phases complete

## Accumulated Context

### Key Decisions (v8.0)

- D-01: ConsultoriaFundo/Custodiante/Cedente are company-scoped (ClientId, HasQueryFilter)
- D-02: FundoStatus = state machine: RASCUNHO→ATIVO↔SUSPENSO→EM_LIQUIDACAO→ENCERRADO
- D-03: TipoAtivo is global (no ClienteId) — CVM catalog shared across companies
- D-04: LimiteExposicao unlimited = sentinel value (-1)
- D-05: Cedente = aggregate único PF/PJ
- D-06: CedenteDocumento = discriminated union (.Pf/.Pj)
- D-07: FundoStatus = enum + CanTransitionTo (no State Pattern)
- D-08: FundoCedente within Fundo aggregate

- D-09: CedenteDocumento DU → shadow properties (documento_tipo + cpf/cnpj_cedente) com HasConversion
- D-10: CedenteDocumento uniqueness = composite filtered indexes per PF/PJ + company scope
- D-11: FundoCedente unique = partial index (FundoId, CedenteId) WHERE status = ATIVO (enum value 1)
- D-12: Repository pattern = EmployeeRepository pattern (IgnoreQueryFilters, explicit CompanyId)
- D-13: Admin methods deferidos para Phase 48 (AdminFundosController)
- D-14: Todas configs usam IEntityTypeConfiguration com ICurrentCompanyService (exceto TipoAtivo, global)
- D-15: FundoCedente/FundoTipoAtivo/CedenteTipoAtivo = owned collections via OwnsMany
- D-16: LimiteExposicaoValor HasPrecision(18,4), LimiteExposicaoPercentual HasPrecision(5,2)
- D-17: Migração única AddFundosModule para 8 tabelas

### Security Fix (Phase 45 Review)

- WR-03 fixed: Guid.Empty guards on all company-scoped aggregate factory methods (Fundo, ConsultoriaFundo, Custodiante, Cedente)

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
Stopped at: Phase 46 context gathered, ready for planning
Resume file: .planning/phases/46-infrastructure-layer/46-CONTEXT.md