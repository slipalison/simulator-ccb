# Project Research Summary

**Project:** Onboarding de Clientes — Fundos de Investimento Module (v8.0)
**Domain:** Brazilian Investment Fund Cadastral Management (multi-tenant DDD system)
**Researched:** 2026-05-02
**Confidence:** HIGH

## Executive Summary

This module adds investment fund cadastral management (Fundos, Cedentes, Custodiantes, Consultorias, TiposAtivo) to an existing .NET 10 DDD multi-tenant PJ onboarding system. The domain is well-understood — Brazilian CVM-regulated fund registration with exposure limits, status lifecycles, and N-N relationships carrying payload. The existing codebase provides proven patterns (HasQueryFilter multi-tenancy, CQRS manual DI, Entity<T> base, Cnpj/Cpf value objects, PaginatedResult<T>, audit logging) that the Fundos module must replicate, not reinvent.

**The recommended approach** is to extend the existing DDD bounded context (same AppDbContext, same Company aggregate boundary) with 5 new aggregate roots + 3 join entities, following the established patterns exactly. No new NuGet packages. No new infrastructure. No microservice split. The critical architectural decision is that **Cedente and Custodiante are GLOBAL entities** (shared across companies, no HasQueryFilter, unique CNPJ constraint) while **Fundo and FundoCedente are company-scoped** (CompanyId FK, HasQueryFilter). This distinction is essential for multi-tenancy correctness.

**Key risks** are: (1) multi-tenancy leak on company-scoped vs global entity confusion, (2) Fundo status treated as a simple enum setter instead of a state machine, (3) FundoCedente modeled as a simple join table instead of a full domain entity with payload, and (4) decimal precision loss on monetary values. The prevention strategy is clear: state machine methods on aggregates, HasQueryFilter on every company-scoped entity, integration tests that verify cross-company isolation, and explicit `HasPrecision()` on all financial fields.

## Key Findings

### Recommended Stack

**No new NuGet packages.** The existing stack (EF Core 10.0.7, Npgsql 10.0.1, FluentValidation 12.1.1, xUnit/Shouldly/NSubstitute/Testcontainers, Serilog + OpenTelemetry) fully covers the Fundos module. All work is domain modeling — aggregates, value objects, enums, join entities, EF Core configurations, and decimal precision mapping.

**Core technologies (existing, no changes):**
- **EF Core 10.0.7:** ORM — N-N join entities with payload, HasPrecision for monetary fields, HasQueryFilter for multi-tenancy
- **Npgsql 10.0.1:** PostgreSQL provider — `numeric(p,s)` maps directly via HasPrecision
- **FluentValidation 12.1.1:** Command/DTO validation for fund registration, exposure limits, status transitions
- **xUnit/Shouldly/NSubstitute/Testcontainers:** TDD — unit tests for domain, integration tests for full DB round-trip
- **Serilog + OpenTelemetry:** Structured logging + traces for fund CRUD operations (already instrumented)

### Expected Features

**Must have (table stakes):**
- **ConsultoriaFundo CRUD** — fund advisory companies, required FK for Fundo
- **Custodiante CRUD** — custodian institutions, required FK for Fundo
- **Fundo CRUD** — the core entity with status lifecycle, CNPJ, consultoria/custodiante FKs
- **Cedente CRUD** — assignors (PF/PJ), central entity in fund-cedente relationships
- **TipoAtivo CRUD** — asset type catalog, required for cedente-tipo and fundo-tipo relationships
- **FundoCedente CRUD** — N-N with payload (exposure limits, date ranges, status transitions)
- **Multi-tenant isolation** — HasQueryFilter on all company-scoped entities
- **Server-side validation** — FluentValidation on all endpoints, state machine enforcement

**Should have (differentiators):**
- **Fundo status state machine** — enforces Ativo → EmLiquidação → Encerrado transitions, prevents invalid transitions
- **FundoCedente exposure limits** — validates percentage and value limits before activation
- **Referential integrity enforcement** — cannot deactivate entity with active dependents
- **Cedente polymorphic PF/PJ** — single entity for both person types with conditional CPF/CNPJ

**Defer (v2+):**
- Fundo_tipo_ativo (optional, low priority)
- Complex reporting/analytics dashboard (AF-07)
- Document management (AF-03)
- Workflow/approval process (AF-04)

### Architecture Approach

The Fundos module integrates into the existing DDD architecture as a **new aggregate group within the same Company bounded context**. Same `Onboarding.Domain` project, same `AppDbContext`, same multi-tenancy `HasQueryFilter` pattern, same CQRS manual DI wiring. No microservice, no separate database.

**Major components:**
1. **Domain Layer** — FundosAggregate folder with 5 aggregate roots (Fundo, ConsultoriaFundo, Custodiante, Cedente, TipoAtivo) + 3 join entities (FundoCedente, CedenteTipoAtivo, FundoTipoAtivo). State machine methods on Fundo and FundoCedente. Reuse existing Cnpj/Cpf/Email/PhoneNumber value objects.
2. **Infrastructure Layer** — 5 repository implementations + 8+ EF Core configurations (HasQueryFilter on company-scoped, HasPrecision on monetary, composite keys on join entities). Single migration adding 8 new tables.
3. **Application Layer** — ~35 CQRS files (commands, queries, handlers, validators, DTOs). Manual DI registration in DependencyInjection.cs. Audit logging on every mutation.
4. **API Layer** — FundosController with ~20 endpoints. New permission policies (funds:read, funds:write, funds:delete). AdminFundosController for cross-company admin access.
5. **Frontend Client SPA** — FundosPage with sub-tabs for consultorias, custodiantes, cedentes, tipos ativo. Form dialogs with Zod validation. Atomic Design components.
6. **Frontend Backoffice SPA** — AdminFundosPage for cross-company read-only access.

### Critical Pitfalls

1. **Multi-tenancy leak on global vs company-scoped entities** — Cedente and Custodiante are GLOBAL (no HasQueryFilter, unique CNPJ across system). Fundo and FundoCedente are company-scoped (HasQueryFilter on CompanyId). Mixing these up causes cross-company data leakage.
2. **Fundo status treated as simple enum** — Status must be enforced via domain methods (`Activate()`, `Close()`, `EnterLiquidation()`), not a direct setter. Invalid transitions (Encerrado → Ativo) must be rejected at the aggregate root level.
3. **FundoCedente modeled as simple join** — Must be a full domain entity with its own payload (LimiteExposicaoPercentual, LimiteExposicaoValor, DataInicio, DataFim, Status) and its own state transitions (Ativo → Cancelado is terminal).
4. **Decimal precision loss on monetary values** — Use `numeric(18,2)` for PL and limits, `numeric(20,8)` for quota values, `numeric(5,4)` for percentages. Never use float/double.
5. **Missing permission granularity** — Need `funds:read`, `funds:write`, `funds:delete` in Permissions.cs and PermissionPolicyConstants.cs. Not piggybacking on employee permissions.

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 1: Domain Entities & Value Objects
**Rationale:** Domain layer has zero dependencies on Infrastructure or API. Must be built first because all other layers depend on entities, value objects, and repository interfaces. State machine enforcement must be defined before any persistence logic.
**Delivers:** All 5 aggregate roots, 3 join entities, value objects, repository interfaces, enums, factory methods
**Addresses:** TS-01 through TS-05, TS-09, TS-11 / D-01, D-04, D-06, D-07
**Avoids:** PITFALL-02 (CNPJ validation), PITFALL-03 (decimal precision), PITFALL-04 (status state machine), PITFALL-05 (FundoCedente as full entity), PITFALL-12 (Portuguese enum names)

### Phase 2: Infrastructure — EF Core Configs & Repositories
**Rationale:** Must follow Domain. EF Core configurations define table mapping, HasQueryFilter (multi-tenancy), HasPrecision (monetary), composite keys (join entities). Repositories implement the interfaces. Single migration for all 8 tables.
**Delivers:** 8 EF Core configurations, 5 repository implementations, AppDbContext DbSet additions, EF Core migration
**Addresses:** TS-09 (multi-tenant isolation), D-07
**Avoids:** PITFALL-01 (HasQueryFilter on company-scoped entities), PITFALL-09 (Cedente/Custodiante as global), PITFALL-15 (DbSet registration), PITFALL-17 (indexes)

### Phase 3: Auth & Permissions
**Rationale:** API endpoints need permission policies before controllers can be decorated with `[Authorize]`. Must extend Permissions.cs, PermissionPolicyConstants.cs, AccessGroup defaults, and audit logging BEFORE building handlers that check permissions.
**Delivers:** New permission constants (funds:read/write/delete), policy registration, AccessGroup default updates, FundActionType audit constants
**Addresses:** TS-12 (audit trail)
**Avoids:** PITFALL-06 (permission scope), PITFALL-08 (missing audit), PITFALL-10 (Keycloak confusion)

### Phase 4: Application Layer — Commands, Queries, Handlers
**Rationale:** Handlers implement business logic using domain entities and repository interfaces. Validation via FluentValidation. Audit on every mutation. Manual DI registration.
**Delivers:** ~35 CQRS files (commands, queries, handlers, validators, DTOs), DI registration
**Addresses:** TS-10 (server-side validation), TS-06 (FundoCedente CRUD), TS-07, TS-08
**Avoids:** PITFALL-11 (optimistic concurrency on FundoCedente)

### Phase 5: API Controllers
**Rationale:** Controllers depend on Application handlers. FundosController with CRUD endpoints for all entities. AdminFundosController for cross-company admin access.
**Delivers:** FundosController (~20 endpoints), AdminFundosController (read-only cross-company)
**Addresses:** TS-13 (paginated listing)
**Avoids:** PITFALL-13 (missing pagination)

### Phase 6: Frontend — Client SPA
**Rationale:** Frontend depends on working API endpoints. FundosPage with sub-tabs, form dialogs, Zod validation, permission-gated UI, status transition awareness.
**Delivers:** FundosPage, FundosTable, form dialogs (CreateFundo, CreateConsultoria, CreateCustodiante, CreateCedente, CreateTipoAtivo), route + sidebar updates
**Addresses:** D-02, D-03 (frontend validation of exposure limits and date ranges)
**Avoids:** PITFALL-16 (frontend status dropdowns showing invalid transitions)

### Phase 7: Frontend — Backoffice SPA
**Rationale:** Simpler — read-only listing for admin support. Depends on admin API endpoints.
**Delivers:** AdminFundosPage with cross-company fund listing, route + sidebar updates

### Phase 8: Integration Tests
**Rationale:** Depends on all layers complete. Testcontainers for PostgreSQL + full round-trip testing. Multi-tenancy isolation tests. Status transition tests. CNPJ validation tests (including alphanumeric).
**Delivers:** Integration test suite covering all CRUD operations, permission checks, multi-tenancy isolation, concurrent updates
**Avoids:** PITFALL-01 (cross-company data leak), PITFALL-02 (alphanumeric CNPJ), PITFALL-11 (concurrent modification)

### Phase Ordering Rationale

- **Domain first** because all layers depend on entity definitions, value objects, and repository interfaces. State machine enforcement MUST exist before any handler code.
- **Infrastructure second** because EF Core configs define the persistence model and HasQueryFilter guarantees. Without correct configs, multi-tenancy leaks.
- **Auth/Permissions before Application** because handlers need to know which permissions exist. The audit constants define what gets logged.
- **Application before API** because controllers call handlers. Handlers must exist first.
- **API before Frontend** because frontend makes HTTP calls. Endpoints must be stable.
- **Client before Backoffice** because client SPA is the primary user interface.
- **Tests last** because they need the full stack. But unit tests in each phase (TDD).

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 1 (Domain):** Cedente global vs company-scoped is a critical architectural decision. PITFALLS.md argues global, ARCHITECTURE.md sketches company-scoped. Must resolve before entity design.
- **Phase 4 (Application):** FundoCedente exposure limit validation rules need domain expert input. What are the exact business rules for percentage limits (0-100%? 0-1000% for leverage?) and value limits?
- **Phase 6 (Frontend):** Fund detail UI with status transitions, cedente multi-select with exposure limits — needs UX research. How to present FundoCedente relationships with payload editing?

Phases with standard patterns (skip research-phase):
- **Phase 2 (Infrastructure):** Well-documented EF Core patterns. HasQueryFilter, HasPrecision, composite keys all have clear examples in existing codebase.
- **Phase 3 (Auth/Permissions):** Additive extension of existing pattern. Copy Permissions.cs pattern.
- **Phase 5 (API):** FundosController follows CompaniesController pattern exactly.
- **Phase 7 (Backoffice Frontend):** Read-only listing. Standard pattern.
- **Phase 8 (Integration Tests):** Testcontainers pattern already established.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | No new packages needed. Existing stack verified against NuGet versions and Context7 docs. All packages are .NET 10 compatible. |
| Features | HIGH | Feature list derived from CVM regulations and existing codebase patterns. Only FundoCedente exposure limit rules need domain expert validation. |
| Architecture | MEDIUM-HIGH | Established DDD patterns in codebase. One key conflict: Cedente/Custodiante global vs company-scoped. ARCHITECTURE.md and PITFALLS.md disagree. |
| Pitfalls | HIGH | Based on codebase analysis, EF Core docs, and Brazilian regulatory knowledge. All pitfalls have clear prevention strategies. |

**Overall confidence:** HIGH

### Gaps to Address

- **Cedente/Custodiante scope (global vs company-scoped):** ARCHITECTURE.md shows them with CompanyId and HasQueryFilter (company-scoped). PITFALLS.md argues they should be global (shared across companies, unique CNPJ constraint without company filter). This conflict must be resolved during Phase 1 planning. **Recommendation:** PITFALLS.md has the stronger argument — a real-world Cedente (Itaú, Bradesco) should be a single global entity referenced by multiple companies via FundoCedente. But FEATURES.md lists Cedente with ClienteId. **This requires explicit stakeholder decision.**
- **FundoCedente primary key:** STACK.md defines compound key (FundoId + CedenteId), but FEATURES.md shows `Id: Guid (PK)`. Compound key enforces uniqueness; Guid PK allows surrogate key with separate unique constraint. **Recommendation:** FundoCedente has payload (status, limits, dates) making it a full entity — use Guid PK with unique constraint on (FundoId, CedenteId) to enforce uniqueness while allowing EF Core tracking.
- **FundoStatus enum values:** FEATURES.md has `Ativo, EmLiquidacao, Encerrado, Suspenso`. PITFALLS.md mentions `RASCUNHO` as a draft state. **Recommendation:** Include `Rascunho` as the initial state — funds should be created in draft before being activated, matching PITFALLS-04's state machine.
- **Exposure limit business rules:** Percentage (0-100%? 0-1000% for leverage?) and value limits (minimum? maximum?) are not clearly defined in regulations. **Recommendation:** Make both optional nullable decimals. Validate percentage 0-10000 (allows >100% leverage). Validate value > 0 if provided. Business rules refined during Phase 4 after domain expert input.

## Sources

### Primary (HIGH confidence)
- Existing codebase analysis — Entity<T>, Cnpj.cs, Cpf.cs, CompanyConfiguration.cs, EmployeeConfiguration.cs, AppDbContext.cs, Permissions.cs, ActionType.cs, DependencyInjection.cs, ClientClaimsMiddleware.cs — direct pattern extraction
- EF Core 10 official docs — HasQueryFilter, HasPrecision, N-N with payload, owned collections — Microsoft Learn
- CVM Instruction 558/2015 — Brazilian fund regulation mandating consultoria and custodiante per fund
- CVM Instruction 393/2003 — FIDC definition of cedente role

### Secondary (MEDIUM confidence)
- Architectural patterns from DDD microservice guide (Microsoft) — aggregate design
- CNPJ alphanumeric format 2026 — Receita Federal announcement via Wikipedia pt-BR
- BACEN Resolution 4,943/2022 — financial institution registration

### Tertiary (needs validation)
- Exposure limit business rules for FundoCedente — domain expert input needed during implementation
- FundoStatus lifecycle transition map — needs stakeholder confirmation (Rascunho inclusion)
- Cedente global vs company-scoped — architectural decision needed from stakeholder

---
*Research completed: 2026-05-02*
*Ready for roadmap: yes*