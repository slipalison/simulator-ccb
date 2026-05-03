# Roadmap: Onboarding de Clientes

## Overview

This roadmap builds a secure PF/PJ client onboarding system from infrastructure up, then extends it with an admin backoffice panel for user management. The delivery sequence mirrors the dependency chain: Docker infrastructure first, then a hardened Keycloak, then the DDD backend domain, then observability wiring, then registration and authentication endpoints, then a frontend scaffold, and finally the three user-facing screens (registration, login, profile). After v1.0/v2.0 completion, milestone v3.0 adds admin CRUD endpoints, role-based access control, and a backoffice UI with pagination, filtering, and LGPD-compliant user deletion. Milestone v3.0 concludes with a **frontend separation** into two independent projects (client + backoffice). Milestone v4.0 adds a full CI/CD security pipeline. Milestone v5.0 migrates the backoffice to Auth Code Flow + PKCE, adds administrator management, and introduces an immutable audit log. Milestone v6.0 adds complete administrator management. Milestone v7.0 transforms the system to PJ-only with employee management. Milestone v8.0 adds investment fund cadastral management.

Every phase delivers a coherent, independently verifiable capability before the next begins.

**⚠️ ARCHITECTURAL CONSTRAINT (Phase 21+):** Two separate frontend projects (`frontend/client` and `frontend/backoffice`) must remain fully independent — no shared code, no cross-imports, separate builds and deploys.

---

## Milestones

| Milestone | Name | Phases | Status |
|-----------|------|--------|--------|
| **v1.0** | Foundation — Cadastro e Login com Perfil Read-Only | 1-10 | ✅ Complete |
| **v2.0** | UX/UI Redesign + Production Readiness | 11-15 | ✅ Complete |
| **v3.0** | Admin Backoffice + Frontend Separation | 16-20 | ✅ Complete |
| **v4.0** | CI/CD Pipeline + Cybersecurity | 21-28 | ✅ Complete |
| **v5.0** | Auth Code Flow (Backoffice) + Gestão de Admins + Auditoria | 29-34 | ✅ Complete |
| **v6.0** | Gestão Completa de Administradores | 35-36 | ✅ Complete |
| **v7.0** | PJ-Only Onboarding + Gestão de Funcionários | 37-44 | ✅ Complete |
| **v8.0** | Gestão de Fundos | 45-52 | 📋 Planned |

---

<details>
<summary>✅ v1.0 Foundation (Phases 1-10) — SHIPPED 2026-04-08</summary>

### Phase 1: Infrastructure
**Goal**: The full stack can boot from a single `docker compose up` with all services healthy and isolated
**Depends on**: Nothing (first phase)
**Requirements**: INFRA-01, INFRA-02, INFRA-03, INFRA-04, INFRA-05
**Success Criteria** (what must be TRUE):
  1. Running `docker compose up` starts all services with no manual intervention
  2. Healthchecks pass for every service
  3. app_db and keycloak_db are separate containers
  4. Keycloak realm "onboarding" exists with required clients after first boot
**Plans**: 3 plans (COMPLETE)

### Phase 2: Keycloak Security Hardening
**Goal**: Keycloak hardened against all documented attack surfaces
**Depends on**: Phase 1
**Requirements**: SEC-01 through SEC-07
**Plans**: 1 plan (COMPLETE)

### Phase 3: Backend Domain Layer
**Goal**: Rich, fully-tested domain model with no infrastructure dependencies
**Depends on**: Phase 1
**Requirements**: BACK-01 through BACK-06
**Plans**: 2 plans (COMPLETE)

### Phase 4: Observability
**Goal**: Structured logs, distributed traces, metrics with correlation
**Depends on**: Phase 3
**Requirements**: OBS-01 through OBS-05
**Plans**: 4 plans (COMPLETE)

### Phase 5: Registration API
**Goal**: Client registration with full validation, Keycloak integration, duplicate detection
**Depends on**: Phase 4
**Requirements**: REG-01 through REG-09
**Plans**: 4 plans (COMPLETE)

### Phase 6: Authentication API
**Goal**: JWT issuance, token refresh, protected routes
**Depends on**: Phase 5
**Requirements**: AUTH-01 through AUTH-04
**Plans**: 3 plans (COMPLETE)

### Phase 7: Frontend Foundation
**Goal**: SPA scaffold with Atomic Design, routing, form infrastructure
**Depends on**: Phase 1
**Requirements**: FRONT-01 through FRONT-05
**Plans**: 4 plans (COMPLETE)

### Phase 8: Registration UI
**Goal**: PF/PJ registration forms with client-side validation
**Depends on**: Phase 7, Phase 5
**Requirements**: REG-01, REG-02, REG-07, REG-09
**Plans**: 3 plans (COMPLETE)

### Phase 9: Login UI
**Goal**: Custom login with ROPC, memory-only JWT storage
**Depends on**: Phase 8, Phase 6
**Requirements**: AUTH-01, SEC-10
**Plans**: 3 plans (COMPLETE)

### Phase 10: Profile UI
**Goal**: Read-only profile screen with PF/PJ visual distinction
**Depends on**: Phase 9, Phase 6
**Requirements**: PROF-01 through PROF-03
**Plans**: 3 plans (COMPLETE)

</details>

<details>
<summary>✅ v2.0 UX/UI Redesign (Phases 11-15) — SHIPPED 2026-04-09</summary>

### Phase 11: UX Redesign
**Goal**: Unified registration with password UX, auto-login, forgot password
**Depends on**: Phase 10, Phase 6
**Plans**: 2 plans (COMPLETE)

### Phase 12: UI Redesign
**Goal**: Professional shadcn/ui components with dark/light theme
**Depends on**: Phase 11
**Plans**: 3 plans (COMPLETE)

### Phase 13: Reset Password Fix
**Goal**: Configurable frontend URL in reset email
**Depends on**: Phase 11
**Plans**: 1 plan (COMPLETE)

### Phase 14: E2E Testing
**Goal**: Playwright E2E tests for critical flows
**Depends on**: Phase 12
**Plans**: 1 plan (PENDING)

### Phase 15: Production Cleanup
**Goal**: Cookie Secure flag, dead code removal, test fixes
**Depends on**: Phase 12
**Plans**: 1 plan (COMPLETE)

</details>

<details>
<summary>✅ v3.0 Admin Backoffice (Phases 16-20) — SHIPPED 2026-04-10</summary>

### Phase 16: Admin API Endpoints
**Goal**: Backend CRUD endpoints for user management with role-based authorization
**Plans**: 3 plans (COMPLETE)

### Phase 17: Admin Auth & Session Management
**Goal**: HttpOnly cookie-based authentication for backoffice
**Plans**: 2 plans (COMPLETE)

### Phase 18: Admin Backoffice UI — List & Details
**Goal**: Paginated user listing with search, filters, detail view
**Plans**: 2 plans (COMPLETE)

### Phase 19: Frontend Separation — Client vs Backoffice
**Goal**: Two independent frontend projects, zero cross-imports
**Plans**: 2 plans (COMPLETE)

### Phase 20: Admin Backoffice UI — Edit, Block, Delete
**Goal**: Edit form, block/unblock, LGPD deletion
**Plans**: 2 plans (COMPLETE)

</details>

<details>
<summary>✅ v4.0 CI/CD + Cybersecurity (Phases 21-28) — SHIPPED</summary>

### Phase 21: CI/CD Pipeline Foundation
**Plans**: 3 plans

### Phase 22: SAST — Static Application Security Testing
**Plans**: 3 plans

### Phase 23: SCA — Software Composition Analysis
**Plans**: 2 plans

### Phase 24: Container Security Scanning
**Plans**: 2 plans

### Phase 25: IaC Scanning
**Plans**: 2 plans

### Phase 26: Secrets Detection
**Plans**: 2 plans

### Phase 27: GitHub Security Integration
**Plans**: 2 plans

### Phase 28: Security Documentation + Hardening
**Plans**: 2 plans

</details>

<details>
<summary>✅ v5.0 Auth Code Flow + Admins + Auditoria (Phases 29-34) — SHIPPED</summary>

### Phase 29: Keycloak Config + Auth Code Flow Backend
### Phase 30: Audit Log Backend + Admin Management Backend
### Phase 31: Backoffice Auth Code Flow UI
### Phase 32: Backoffice Admin Management UI + Audit Log UI
### Phase 33-34: Verification and cleanup phases

</details>

<details>
<summary>✅ v6.0 Gestão Completa de Administradores (Phases 35-36) — SHIPPED</summary>

### Phase 35: Admin CRUD API
### Phase 36: Admin Management UI

</details>

<details>
<summary>✅ v7.0 PJ-Only Onboarding + Gestão de Funcionários (Phases 37-44) — SHIPPED</summary>

### Phase 37: PJ-Only Domain Restructure
### Phase 38: Employee Management Backend
### Phase 39: Multi-tenant Isolation + Access Groups
### Phase 40: Frontend PJ Registration + Dashboard
### Phase 41: Audit Trail + Employee Actions
### Phase 42: CI Pipeline Update for v7.0
### Phase 43: Frontend Client Employee Management
### Phase 44: Custom Access Groups CRUD

</details>

---

## Milestone v8.0 — Gestão de Fundos (Phases 45-52)

**Goal:** Adicionar módulo de cadastros de fundos de investimento ao sistema existente — consultorias, custodiantes, fundos, cedentes e tipos de ativo — com isolamento multi-tenant obrigatório e administração no backoffice.

**Key decisions:**
- D-01: ConsultoriaFundo/Custodiante/Cedente são company-scoped (ClienteId, HasQueryFilter)
- D-02: FundoStatus = state machine: RASCUNHO→ATIVO↔SUSPENSO→EM_LIQUIDACAO→ENCERRADO
- D-03: TipoAtivo é global (sem ClienteId) — catálogo CVM compartilhado
- D-04: LimiteExposicao ilimitado = sentinel value (-1)

**Depends on:** Milestone v7.0 completo (Phase 44)

**Phase order rationale:** Domain → Infrastructure → Application → API → Relationships → Frontend Client → Frontend Backoffice → Integration Tests. Each phase builds on the previous layer. The Fundos module follows the same DDD architecture established in v1.0. Domain must be first because all other layers depend on entities, value objects, and repository interfaces. Infrastructure defines persistence model and HasQueryFilter. Application implements business logic. API exposes HTTP endpoints. Relationships (N-N with payload) are a distinct bounded context. Frontends consume working APIs. Integration tests validate the complete stack.

### Phases

- [x] **Phase 45: Domain Layer** - Entities, value objects, enums, repository interfaces, FundoStatus state machine
- [ ] **Phase 46: Infrastructure Layer** - EF Core configs, HasQueryFilter, repositories, migration
- [ ] **Phase 47: Application Layer** - Commands, queries, handlers, validators, DTOs, audit integration
- [ ] **Phase 48: API + Permissions** - FundosController (client CRUD), AdminFundosController, permission policies
- [ ] **Phase 49: FundoCedente & Relationship CRUD** - N-N relationships with payload, TipoAtivo associations
- [ ] **Phase 50: Frontend Client** - FundosPage, forms, Zod validation, sidebar
- [ ] **Phase 51: Frontend Backoffice** - Admin fund views, read-only audit
- [ ] **Phase 52: Integration Tests** - Testcontainers, multi-tenancy, state machine transitions

---

## Phase Details

### Phase 45: Domain Layer
**Goal**: All 5 aggregate roots and 3 join entities are modeled with correct multi-tenancy, state machines, and domain invariants — unit tests pass with zero infrastructure dependencies
**Depends on**: Phase 44 (v7.0 complete)
**Requirements**: TEN-03, CAD-13, PERM-01, REL-08
**Success Criteria** (what must be TRUE):
  1. Domain model compiles with 5 aggregate roots (Fundo, ConsultoriaFundo, Custodiante, Cedente, TipoAtivo) and 3 join entities (FundoCedente, CedenteTipoAtivo, FundoTipoAtivo) with correct properties and relationships
  2. FundoStatus state machine rejects invalid transitions (e.g., ENCERRADO → ATIVO, RASCUNHO → SUSPENSO) with domain exceptions and allows valid transitions (RASCUNHO → ATIVO, ATIVO ↔ SUSPENSO, ATIVO → EM_LIQUIDACAO, EM_LIQUIDACAO → ENCERRADO)
  3. Company-scoped entities (Fundo, FundoCedente, ConsultoriaFundo, Custodiante, Cedente) have ClienteId property; TipoAtivo has no ClienteId — matching D-01 and D-03
  4. LimiteExposicaoPercentual value object treats -1 as "unlimited" sentinel and validates non-negative range otherwise — matching D-04
  5. Permission constants (funds:read, funds:write, funds:delete, funds:manage) defined in Permissions.cs extending existing permission system
**Plans**: 2 plans

Plans:
- [x] 45-01-PLAN.md — Value objects, enums, domain exceptions, permissions extension, repository interfaces
- [x] 45-02-PLAN.md — 5 aggregate roots + 3 join entities with unit tests

### Phase 46: Infrastructure Layer
**Goal**: EF Core persistence layer enforces multi-tenancy, unique constraints, and decimal precision — all 8 tables created correctly with a single migration
**Depends on**: Phase 45
**Requirements**: CAD-04, CAD-08, CAD-12, CAD-18, CAD-22, TEN-01, TEN-02
**Success Criteria** (what must be TRUE):
  1. EF Core migration creates 8 new tables (fundos, consultoria_fundos, custodiantes, cedentes, tipos_ativo, fundo_cedentes, cedente_tipos_ativo, fundo_tipos_ativo) with correct columns, foreign keys, and indexes
  2. HasQueryFilter on Fundo, FundoCedente, ConsultoriaFundo, Custodiante, Cedente automatically filters by current company — queries from Company A return zero rows for Company B's data
  3. Unique constraints reject duplicate CNPJ within same company for ConsultoriaFundo, Custodiante, and Fundo — returning constraint violation that Application layer translates to 409
  4. Unique constraint on TipoAtivo codigo rejects duplicates globally (no company scope) — matching D-03
  5. HasPrecision on monetary and percentage fields (LimiteExposicaoPercentual, LimiteExposicaoValor) prevents decimal precision loss in PostgreSQL
**Plans**: TBD

### Phase 47: Application Layer
**Goal**: CQRS handlers validate inputs, enforce business rules, persist via repositories, and log every mutation to audit trail
**Depends on**: Phase 46
**Requirements**: CAD-01, CAD-02, CAD-03, CAD-05, CAD-06, CAD-07, CAD-09, CAD-10, CAD-11, CAD-14, CAD-15, CAD-16, CAD-17, CAD-19, CAD-20, CAD-21, ADM-04
**Success Criteria** (what must be TRUE):
  1. Register ConsultoriaFundo handler validates CNPJ check digits, checks uniqueness within company, and persists entity with ATIVO status
  2. List handlers for ConsultoriaFundo, Custodiante, Cedente, and Fundo return paginated results (20/page) with search by razao social, nome, CNPJ, or CPF
  3. Register Fundo handler validates CNPJ, references ConsultoriaFundo and Custodiante FKs, assigns RASCUNHO status on creation, and validates TipoFundo enum
  4. Cedente polymorphic creation: PF path validates CPF, PJ path validates CNPJ — both paths apply company-scoped uniqueness checks
  5. TipoAtivo handler creates/updates with unique codigo validation (global scope, no company filter)
  6. Every fund management mutation (create, update, status transition) is logged to the existing audit trail with actor, action type, and details JSON
**Plans**: TBD

### Phase 48: API + Permissions
**Goal**: FundosController exposes CRUD endpoints for all 5 entity types with permission-gated access; AdminFundosController provides cross-company read-only views
**Depends on**: Phase 47
**Requirements**: PERM-02, PERM-03
**Success Criteria** (what must be TRUE):
  1. FundosController exposes Create/Read/Update endpoints for ConsultoriaFundo, Custodiante, Fundo, Cedente, and TipoAtivo with proper HTTP methods (POST, GET, PUT) and REST routes
  2. All fund CRUD endpoints require appropriate permission claims (funds:read for GET, funds:write for POST/PUT, funds:delete for DELETE) — requests without claims receive 403 Forbidden
  3. AdminFundosController exposes read-only GET endpoints that bypass HasQueryFilter for cross-company admin visibility of Fundos, ConsultoriaFundo, Custodiante, and Cedente
  4. Existing access groups (admin-empresa, viewer) are extended with fund permissions by default — admin-empresa gets funds:manage, viewer gets funds:read
**Plans**: TBD

### Phase 49: FundoCedente & Relationship CRUD
**Goal**: PJ can manage N-N relationships between Fundo↔Cedente (with payload), Cedente↔TipoAtivo, and Fundo↔TipoAtivo with full business rule enforcement
**Depends on**: Phase 48
**Requirements**: REL-01, REL-02, REL-03, REL-04, REL-05, REL-06, REL-07, REL-09
**Success Criteria** (what must be TRUE):
  1. PJ can associate a Cedente to a Fundo specifying exposure limits (percentage and value) and date range — creating a FundoCedente record with LimiteExposicaoPercentual, LimiteExposicaoValor, DataInicio, DataFim, Status
  2. PJ can list all Cedentes associated to a Fundo with their exposure limits and status — returning FundoCedente details alongside Cedente data
  3. PJ can update FundoCedente exposure limits, dates, and status (ATIVO/INATIVO) — at most ONE active association per Fundo-Cedente pair is enforced (REL-09)
  4. PJ can associate, list, and remove Tipos de Ativo to/from a Cedente (defining which assets they can work with)
  5. PJ can associate, list, and remove Tipos de Ativo to/from a Fundo (defining investment mandate)
**Plans**: TBD

### Phase 50: Frontend Client
**Goal**: PJ users can manage fund entities and relationships through a polished client UI with Zod validation, state machine awareness, and sidebar navigation
**Depends on**: Phase 49
**Requirements**: FRO-01, FRO-02, FRO-03, FRO-05
**Success Criteria** (what must be TRUE):
  1. Client sidebar includes Fundos section with sub-navigation: Fundos, Consultorias, Custodiantes, Cedentes
  2. FundosPage shows paginated list of funds with search by nome/CNPJ, status badges (RASCUNHO=gray, ATIVO=green, SUSPENSO=yellow, EM_LIQUIDACAO=orange, ENCERRADO=red)
  3. Registration and edit forms for all entity types validate fields with Zod schemas mirroring backend rules: CNPJ/CPF check-digit validation, required fields, status transitions
  4. Fundo status dropdown only shows valid transitions based on current status — RASCUNHO can only transition to ATIVO, SUSPENSO only to ATIVO, etc.
**UI hint**: yes
**Plans**: TBD

### Phase 51: Frontend Backoffice
**Goal**: Backoffice admin can view fund entities across all companies in read-only mode for auditing purposes
**Depends on**: Phase 48
**Requirements**: ADM-01, ADM-02, ADM-03, FRO-04
**Success Criteria** (what must be TRUE):
  1. Backoffice admin can list Fundo across all companies with pagination and search, seeing company name alongside fund data
  2. Backoffice admin can view Fundo details including consultoria, custodiante, and associated cedentes with exposure limits
  3. Backoffice admin can list ConsultoriaFundo, Custodiante, and Cedente across all companies with pagination and search
  4. All backoffice fund views are read-only — no create, update, or delete actions available (FRO-04)
**UI hint**: yes
**Plans**: TBD

### Phase 52: Integration Tests
**Goal**: Full-stack integration tests verify CRUD operations, multi-tenancy isolation, state machine transitions, and relationship constraints with real PostgreSQL via Testcontainers
**Depends on**: Phase 49
**Requirements**: (validates all v8.0 requirements end-to-end)
**Success Criteria** (what must be TRUE):
  1. Integration tests with Testcontainers verify full CRUD round-trip for all 5 entity types (ConsultoriaFundo, Custodiante, Fundo, Cedente, TipoAtivo) and all 3 relationship types (FundoCedente, CedenteTipoAtivo, FundoTipoAtivo)
  2. Multi-tenancy isolation tests confirm Company A cannot see Company B's fundos, consultorias, custodiantes, or cedentes — cross-company data leakage is impossible
  3. FundoStatus state machine transitions are validated end-to-end: RASCUNHO→ATIVO ✓, ATIVO↔SUSPENSO ✓, ATIVO→EM_LIQUIDACAO→ENCERRADO ✓, and invalid transitions (ENCERRADO→ATIVO, SUSPENSO→ENCERRADO) return 400
  4. FundoCedente constraint tests confirm at most one active association per Fundo-Cedente pair — attempting to create a second active association returns 409
  5. Duplicate detection tests confirm 409 responses for duplicate CNPJ/CPF/CNPJ within same company scope and duplicate TipoAtivo codigo globally
**Plans**: TBD

---

## Progress

**Execution Order:**
Phases execute in numeric order within milestone. Cross-milestone dependencies must be satisfied first.

```
v8.0:  45 → 46 → 47 → 48 → 49 → 50 → 51 → 52
```

Note: Phase 50 depends on Phase 49 (relationships). Phase 51 depends on Phase 48 (API). Phase 52 depends on Phase 49 (full stack). Phases 50 and 51 could be parallelized after their respective dependencies are met.

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 45. Domain Layer | v8.0 | 0/? | Not started | - |
| 46. Infrastructure Layer | v8.0 | 0/? | Not started | - |
| 47. Application Layer | v8.0 | 0/? | Not started | - |
| 48. API + Permissions | v8.0 | 0/? | Not started | - |
| 49. FundoCedente & Relationships | v8.0 | 0/? | Not started | - |
| 50. Frontend Client | v8.0 | 0/? | Not started | - |
| 51. Frontend Backoffice | v8.0 | 0/? | Not started | - |
| 52. Integration Tests | v8.0 | 0/? | Not started | - |

---

## Milestone Summary

| Milestone | Phases | Plans | Status | Requirements |
|-----------|--------|-------|--------|--------------|
| **v1.0** Foundation | 1-10 | 30 | ✅ Complete | 35 requirements |
| **v2.0** UX/UI + Production | 11-15 | 7+ | ✅ Complete | 14 requirements |
| **v3.0** Admin Backoffice | 16-20 | 13 | ✅ Complete | 22 requirements |
| **v4.0** CI/CD + Security | 21-28 | 20 | ✅ Complete | 25 requirements |
| **v5.0** Auth Code Flow + Admins + Audit | 29-34 | TBD | ✅ Complete | 11 requirements |
| **v6.0** Gestão de Administradores | 35-36 | 5 | ✅ Complete | 14 requirements |
| **v7.0** PJ-Only + Funcionários | 37-44 | 19 | ✅ Complete | TBD requirements |
| **v8.0** Gestão de Fundos | 45-52 | TBD | 📋 Planned | 46 requirements |

---
*Last updated: 2026-05-02 — Milestone v8.0 Gestão de Fundos roadmap created*