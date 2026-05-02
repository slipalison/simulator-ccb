# Feature Landscape: Fundos de Investimento Module

**Domain:** Brazilian Investment Fund Cadastral Management (multi-tenant, extending existing onboarding system)
**Researched:** 2026-05-02
**Stack context:** .NET 10 + DDD + PostgreSQL + Keycloak (existing), adding 5 entities + 3 relationship tables

---

## Domain Glossary

| Term | Portuguese | Definition | Analogy |
|------|-----------|------------|---------|
| ConsultoriaFundo | Consultoria de Fundo / Gestora | Investment fund advisory/management company. Legally responsible for fund strategy and administration. | Like a "fund manager" company |
| Custodiante | Custodiante | Financial institution that holds/protects fund assets. Required by CVM regulation. | Like a "custodian bank" |
| Fundo | Fundo de Investimento | The investment fund itself. Has consultoria + custodiante references. | The actual fund product |
| Cedente | Cedente | PF or PJ that assigns (cedes) receivables to a fund. In FIDC context, sells credit rights. | Like a "seller/assignor" of credit |
| TipoAtivo | Tipo de Ativo | Catalog of asset types a fund or cedente can deal with (RF, ACOES, CRI, CARTAO, etc.) | Like a "product category" enum |
| FundoCedente | — | N-N relationship between Fundo and Cedente with payload (status, limits, dates) | Like a "contract" linking a fund to an assignor |
| CedenteTipoAtivo | — | N-N: which asset types a cedente can work with | Like "skills" of an assignor |
| FundoTipoAtivo | — | N-N: which asset types a fund can invest in (optional) | Like "investment mandate" of a fund |

---

## Table Stakes

Features users expect. Missing = product feels incomplete or unreliable.

| # | Feature | Why Expected | Complexity | Notes |
|---|---------|--------------|------------|-------|
| TS-01 | ConsultoriaFundo CRUD (create, read, update, list) | Core entity — without it, no fund can exist | Medium | CNPJ validation (reuse existing Cnpj value object). Multi-tenant by ClienteId. |
| TS-02 | Custodiante CRUD | Core entity — CVM regulation requires every fund to have a custodian | Medium | CNPJ validation. Multi-tenant. Status (Ativo/Inativo). |
| TS-03 | Fundo CRUD | Core entity — the product itself. Depends on ConsultoriaFundo + Custodiante existing | High | FK to ConsultoriaFundo and Custodiante. Status lifecycle (Ativo/Encerrado/etc.). CNPJ próprio do fundo required. Multi-tenant. |
| TS-04 | Cedente CRUD (PF or PJ) | Core entity — represents who assigns receivables to funds | High | Polymorphic: can be PF (CPF) or PJ (CNPJ). Reuse existing Cpf/Cnpj value objects. Multi-tenant. |
| TS-05 | TipoAtivo catalog CRUD | Reference data — needed for CedenteTipoAtivo and FundoTipoAtivo relationships | Low | Simple catalog. Seeded or user-managed. Name + code. Status field. Multi-tenant. |
| TS-06 | FundoCedente relationship CRUD | N-N with payload — the "contract" linking a fund to a cedente with limits and dates | High | Core business logic: status transitions, exposure limits (% and value), start/end dates. Deactivate ≠ delete. Multi-tenant. |
| TS-07 | CedenteTipoAtivo relationship CRUD | N-N — which asset types a cedente operates with | Medium | Simple link with optional status field. Multi-tenant. |
| TS-08 | FundoTipoAtivo relationship CRUD (optional) | N-N — which asset types a fund invests in | Low | Optional subset. Simple link table. Multi-tenant. |
| TS-09 | Multi-tenant isolation (ClienteId) on all entities | Existing pattern — Company data must never leak to another Company | Medium | Extend existing HasQueryFilter pattern from EmployeeConfiguration. All new entities get ClienteId FK → Company. |
| TS-10 | Server-side validation on all endpoints | Security requirement — client-side is UX convenience only | Medium | FluentValidation on all commands. Reuse Cnpj, Cpf, Email, PhoneNumber value objects. New validators for business rules. |
| TS-11 | Status field on active entities | Business requirement — entities can be Ativo/Inativo/Encerrado | Medium | ConsultoriaFundo, Custodiante, Fundo, Cedente all need status. FundoCedente also has status. Type-safe enums (not magic strings). |
| TS-12 | Audit trail for all mutations | LGPD/regulatory — fund management requires traceability | Medium | Extend existing AdminAuditLog pattern or create domain-specific audit. Who changed what, when, old/new values. |
| TS-13 | Paginated listing with filters | UX requirement — lists of funds, cedentes, etc. will be long | Low | Reuse existing PaginatedResult<T> pattern. Filter by status, search by name/CNPJ. |

---

## Differentiators

Features not universally expected but that add meaningful value for investment fund management.

| # | Feature | Value Proposition | Complexity | Notes |
|---|---------|-----|------------|-------|
| D-01 | Fundo status lifecycle enforcement | Prevents invalid transitions (e.g., Encerrado → Ativo). Business rule validation in domain layer. Protects data integrity. | Medium | State machine in Fundo aggregate. Define allowed transitions. Enforce via domain method. |
| D-02 | FundoCedente exposure limit validation | Prevents overcommit — a fund/cedente relationship should respect configurable limits (% and absolute value). Validates before activation. | Medium | Percentage limit (0-100% or 0-1000% for leverage). Value limit (currency). Validate at domain level. |
| D-03 | FundoCedente date range enforcement | Status transitions validate against start/end dates. Can't activate a relationship that hasn't started or has expired. | Low | StartDate required, EndDate optional. Status → Ativo requires StartDate <= today. |
| D-04 | Cedente polymorphic PF/PJ | Single entity that represents either a person or company, with appropriate validation (CPF or CNPJ). Reduces schema duplication. | Medium | Use PersonType enum (existing pattern from original design). CPF nullable when PJ, CNPJ nullable when PF, exactly one required. |
| D-05 | TipoAtivo as semi-static catalog with status | Types like RF, ACOES, CRI, CARTAO are regulated and semi-static. Status (Ativo/Inativo) allows deactivating without deleting. | Low | Not a free-for-all tag system. Predefined types with controlled lifecycle. Deactivation preserves historical relationships. |
| D-06 | Referential integrity enforcement — Fundo depends on ConsultoriaFundo and Custodiante | A fund cannot exist without its management company and custodian. Deactivating a ConsultoriaFundo or Custodiante that's in use by active funds should be blocked. | Medium | Domain rule: check for active Fundo references before allowing Inativo status change. |
| D-07 | Multi-tenant query filters (HasQueryFilter) on all entities | Extends existing pattern to new entities. Guarantees a company NEVER sees another company's funds, cedentes, etc. | Low | Same approach as EmployeeConfiguration. Apply to every new entity. Critical for data isolation. |

---

## Anti-Features

Features to deliberately NOT build. Each has an explicit reason.

| # | Anti-Feature | Why Avoid | What to Do Instead |
|---|-------------|-----------|-------------------|
| AF-01 | Financial transaction processing (bookkeeping, NAV calculation) | Out of scope — this is cadastral management (CRUD), not fund accounting. No P&L, no position tracking, no cash flow. | Return a "not implemented" if an endpoint is accidentally called. Document as out of scope. |
| AF-02 | Real-time market data integration (B3, CETIP) | Complex external dependency with unreliable data feeds. Not cadastral. | Manual data entry for now. Integration deferred to future milestone if needed. |
| AF-03 | Document management (upload/store fund prospectus, bylaws) | File storage, virus scanning, document signing — entire domain on its own. | Store only document URLs/references as metadata (string field). No file upload. |
| AF-04 | Workflow/approval process for entity creation | Multi-step approval (draft → pending → approved) adds significant CRUD complexity without being requested. | Direct CRUD with status field. If workflow is needed later, add as a separate milestone. |
| AF-05 | CEDENTE authentication (login as cedente) | Cedentes are data records, not authenticated users. They don't log in. | Company (PJ) employees manage cedentes via backoffice. Cedentes have no Keycloak presence. |
| AF-06 | Fundo share/quota management (cotas, valuation) | Out of scope — cadastral only. Quota management is a full financial module. | Store only fund metadata (name, CNPJ, consultoria, custodiante, status). |
| AF-07 | Complex reporting/analytics dashboard | Not cadastral. Queries for reports can be added later without schema changes. | Simple list/detail views. No aggregations, charts, or exports beyond paginated lists. |
| AF-08 | Soft-delete on FundoCedente (preserve all relationship history) | FundoCedente is a contract-like relationship. Deactivation via status is the correct approach — no LGPD-style anonymization needed. | Status transitions only (Ativo → Inativo/Cancelado). No DeletedAt column. History is in audit log. |
| AF-09 | Auto-notification/events when fund/cedente status changes | Event bus, webhooks, email notifications — too much infrastructure for cadastral CRUD. | If needed later, add domain events that publish to an outbox. But not in this milestone. |
| AF-10 | Hierarchical fund structures (master-feeder, fund of funds) | Adds entity self-referencing complexity. Not requested for v8. | Fundo is flat. No parent_fundo_id. Can be added later. |

---

## Entity Relationship Map

```
Company (existing aggregate)
  │
  ├── ConsultoriaFundo (NEW)
  │     ├── Id: Guid (PK)
  │     ├── ClienteId: Guid (FK → Company)
  │     ├── RazaoSocial: string (required, max 200)
  │     ├── Cnpj: Cnpj (required, value object)
  │     ├── Status: ConsultoriaFundoStatus (Ativo | Inativo)
  │     └── CreatedAt/UpdatedAt: DateTimeOffset
  │
  ├── Custodiante (NEW)
  │     ├── Id: Guid (PK)
  │     ├── ClienteId: Guid (FK → Company)
  │     ├── Nome: string (required, max 200)
  │     ├── Cnpj: Cnpj (required, value object)
  │     ├── Status: CustodianteStatus (Ativo | Inativo)
  │     └── CreatedAt/UpdatedAt: DateTimeOffset
  │
  ├── Fundo (NEW)
  │     ├── Id: Guid (PK)
  │     ├── ClienteId: Guid (FK → Company)
  │     ├── Nome: string (required, max 300)
  │     ├── Cnpj: Cnpj (required — CNPJ do fundo)
  │     ├── ConsultoriaFundoId: Guid (FK → ConsultoriaFundo, required)
  │     ├── CustodianteId: Guid (FK → Custodiante, required)
  │     ├── Status: FundoStatus (Ativo | EmLiquidacao | Encerrado | Suspenso)
  │     ├── Classe: string? (optional — e.g., "FIDC", "FIQ", "FIC")
  │     └── CreatedAt/UpdatedAt: DateTimeOffset
  │
  ├── Cedente (NEW — polymorphic PF/PJ)
  │     ├── Id: Guid (PK)
  │     ├── ClienteId: Guid (FK → Company)
  │     ├── PersonType: PersonType (PF | PJ)
  │     ├── Nome: string (required, max 200)
  │     ├── Cpf: Cpf? (required when PF)
  │     ├── Cnpj: Cnpj? (required when PJ)
  │     ├── Email: Email? (optional contact)
  │     ├── Phone: PhoneNumber? (optional contact)
  │     ├── Status: CedenteStatus (Ativo | Inativo)
  │     └── CreatedAt/UpdatedAt: DateTimeOffset
  │
  ├── TipoAtivo (NEW — catalog)
  │     ├── Id: Guid (PK)
  │     ├── ClienteId: Guid (FK → Company)
  │     ├── Codigo: string (required, max 20, unique per company)
  │     ├── Nome: string (required, max 100)
  │     ├── Status: TipoAtivoStatus (Ativo | Inativo)
  │     └── CreatedAt/UpdatedAt: DateTimeOffset
  │
  ├── FundoCedente (NEW — N-N with payload)
  │     ├── Id: Guid (PK)
  │     ├── ClienteId: Guid (FK → Company)
  │     ├── FundoId: Guid (FK → Fundo)
  │     ├── CedenteId: Guid (FK → Cedente)
  │     ├── Status: FundoCedenteStatus (Ativo | Inativo | Cancelado)
  │     ├── LimiteExposicaoPercentual: decimal? (0-10000, percentage)
  │     ├── LimiteExposicaoValor: decimal? (monetary value)
  │     ├── DataInicio: DateTimeOffset (required)
  │     ├── DataFim: DateTimeOffset? (optional)
  │     └── CreatedAt/UpdatedAt: DateTimeOffset
  │
  ├── CedenteTipoAtivo (NEW — N-N)
  │     ├── Id: Guid (PK)
  │     ├── ClienteId: Guid (FK → Company)
  │     ├── CedenteId: Guid (FK → Cedente)
  │     ├── TipoAtivoId: Guid (FK → TipoAtivo)
  │     └── CreatedAt: DateTimeOffset
  │
  └── FundoTipoAtivo (NEW — N-N, optional)
        ├── Id: Guid (PK)
        ├── ClienteId: Guid (FK → Company)
        ├── FundoId: Guid (FK → Fundo)
        ├── TipoAtivoId: Guid (FK → TipoAtivo)
        └── CreatedAt: DateTimeOffset
```

---

## Status Enums (Domain-Specific)

### ConsultoriaFundoStatus
- `Ativo` — active management company
- `Inativo` — deactivated (cannot be assigned to new funds)

### CustodianteStatus
- `Ativo` — active custodian
- `Inativo` — deactivated (cannot be assigned to new funds)

### FundoStatus
- `Ativo` — fund is operational
- `EmLiquidacao` — fund is winding down (transition state)
- `Encerrado` — fund is closed (terminal state)
- `Suspenso` — fund operations suspended (reversible)

**Allowed transitions:**
```
Ativo → EmLiquidacao → Encerrado (linear closure path)
Ativo → Suspenso → Ativo (suspension cycle)
Suspenso → EmLiquidacao (suspended fund can close)
EmLiquidacao → Encerrado (liquidation completes)
```
**NOT allowed:** Encerrado → Ativo, Encerrado → EmLiquidacao (terminal state).

### CedenteStatus
- `Ativo` — active assignor
- `Inativo` — deactivated

### TipoAtivoStatus
- `Ativo` — can be assigned to new relationships
- `Inativo` — deactivated (preserves historical references)

### FundoCedenteStatus
- `Ativo` — relationship is active
- `Inativo` — relationship deactivated (reversible)
- `Cancelado` — relationship cancelled (terminal)

**Allowed transitions:**
```
Ativo → Inativo → Ativo (can reactivate)
Ativo → Cancelado (terminal)
Inativo → Cancelado (terminal)
```
**NOT allowed:** Cancelado → Ativo, Cancelado → Inativo (terminal state).

---

## Feature Dependencies

```
Company (existing)
  │
  ├── ConsultoriaFundo ──→ Fundo (Fundo requires ConsultoriaFundo)
  ├── Custodiante ──→ Fundo (Fundo requires Custodiante)
  │
  ├── TipoAtivo ──→ CedenteTipoAtivo (CTA requires TipoAtivo)
  │              ──→ FundoTipoAtivo (FTA requires TipoAtivo)
  │
  ├── Cedente ──→ FundoCedente (FC requires Cedente)
  │           ──→ CedenteTipoAtivo (CTA requires Cedente)
  │
  └── Fundo ──→ FundoCedente (FC requires Fundo)
            ──→ FundoTipoAtivo (FTA requires Fundo)

Build order (must respect foreign keys):
  1. ConsultoriaFundo (no deps besides Company)
  2. Custodiante (no deps besides Company)
  3. TipoAtivo (no deps besides Company)
  4. Cedente (no deps besides Company)
  5. Fundo (depends on ConsultoriaFundo + Custodiante)
  6. CedenteTipoAtivo (depends on Cedente + TipoAtivo)
  7. FundoTipoAtivo (depends on Fundo + TipoAtivo)
  8. FundoCedente (depends on Fundo + Cedente)
```

---

## Validation Rules (Per Entity)

### ConsultoriaFundo
- `RazaoSocial`: required, max 200 chars, trimmed
- `Cnpj`: required, valid CNPJ (reuse Cnpj value object), unique per ClienteId
- `Status`: required, enum (Ativo | Inativo)
- **Business rule:** Cannot set Inativo if there are active Fundos referencing this ConsultoriaFundo (referential integrity)

### Custodiante
- `Nome`: required, max 200 chars, trimmed
- `Cnpj`: required, valid CNPJ, unique per ClienteId
- `Status`: required, enum (Ativo | Inativo)
- **Business rule:** Cannot set Inativo if there are active Fundos referencing this Custodiante

### Fundo
- `Nome`: required, max 300 chars, trimmed
- `Cnpj`: required, valid CNPJ (fund's own CNPJ), unique per ClienteId
- `ConsultoriaFundoId`: required, must reference an Ativo ConsultoriaFundo in same ClienteId
- `CustodianteId`: required, must reference an Ativo Custodiante in same ClienteId
- `Status`: required, enum with transition rules (see above)
- `Classe`: optional, max 50 chars
- **Business rule:** Status transitions must follow the allowed lifecycle
- **Business rule:** Cannot delete a Fundo that has FundoCedente relationships
- **Business rule:** New Fundo defaults to Ativo status

### Cedente
- `PersonType`: required (PF or PJ)
- `Nome`: required, max 200 chars, trimmed
- `Cpf`: required when PersonType = PF, must be valid CPF, unique per ClienteId
- `Cnpj`: required when PersonType = PJ, must be valid CNPJ, unique per ClienteId
- `Email`: optional, valid email format when provided
- `Phone`: optional, valid phone format when provided
- `Status`: required, enum (Ativo | Inativo)
- **Business rule:** Exactly one of CPF/CNPJ must be populated based on PersonType
- **Business rule:** Cannot set Inativo if there are active FundoCedente relationships

### TipoAtivo
- `Codigo`: required, max 20 chars, alphanumeric, unique per ClienteId (e.g., "RF", "ACOES", "CRI")
- `Nome`: required, max 100 chars (e.g., "Renda Fixa", "Ações", "CRI", "Cartão")
- `Status`: required, enum (Ativo | Inativo)
- **Business rule:** Cannot set Inativo if there are active CedenteTipoAtivo or FundoTipoAtivo referencing it

### FundoCedente
- `FundoId`: required, must reference existing Fundo in same ClienteId
- `CedenteId`: required, must reference existing Cedente in same ClienteId
- `Status`: required, enum with transition rules
- `LimiteExposicaoPercentual`: optional, 0 to 10000 (allows >100% for leverage)
- `LimiteExposicaoValor`: optional, must be > 0 if provided
- `DataInicio`: required, must be <= DataFim if DataFim is set
- `DataFim`: optional, must be >= DataInicio if set
- **Business rule:** Unique constraint on (FundoId, CedenteId) per active relationship — one Ativo relationship per pair
- **Business rule:** Status → Ativo requires DataInicio <= today
- **Business rule:** Cannot transition Cancelado → Ativo or Cancelado → Inativo (terminal)

### CedenteTipoAtivo
- `CedenteId`: required, existing Cedente in same ClienteId
- `TipoAtivoId`: required, existing TipoAtivo in same ClienteId
- **Business rule:** Unique constraint on (CedenteId, TipoAtivoId)

### FundoTipoAtivo
- `FundoId`: required, existing Fundo in same ClienteId
- `TipoAtivoId`: required, existing TipoAtivo in same ClienteId
- **Business rule:** Unique constraint on (FundoId, TipoAtivoId)

---

## MVP Recommendation

**Phase 1 — Foundation entities (independent, no cross-entity FK dependencies):**
1. ConsultoriaFundo CRUD (table stakes, no deps)
2. Custodiante CRUD (table stakes, no deps)
3. TipoAtivo CRUD (table stakes, simple catalog)
4. Cedente CRUD (table stakes, no deps besides Company)

**Phase 2 — Dependent entities (FK references to Phase 1):**
5. Fundo CRUD (depends on ConsultoriaFundo + Custodiante) — includes status lifecycle validation
6. CedenteTipoAtivo CRUD (depends on Cedente + TipoAtivo)

**Phase 3 — Relationship entities and advanced logic:**
7. FundoTipoAtivo CRUD (depends on Fundo + TipoAtivo)
8. FundoCedente CRUD (depends on Fundo + Cedente) — includes status transitions, exposure limits, date validation
9. Referential integrity enforcement (cannot deactivate entity with active dependents)

**Phase 4 — Frontend + Integration:**
10. All frontend forms and list views
11. Integration testing of entity lifecycle flows

**Defer:**
- FundoTipoAtivo (marked optional — can ship in Phase 3 or defer if not immediately needed)
- Complex reporting/analytics (AF-07)
- Document management (AF-03)
- Workflow/approval process (AF-04)

---

## Complexity Assessment

| Entity | CRUD Complexity | Key Reasons |
|--------|---------------|-------------|
| ConsultoriaFundo | Low-Medium | Standard CRUD + CNPJ unique constraint + referential integrity check before deactivation |
| Custodiante | Low-Medium | Same pattern as ConsultoriaFundo |
| TipoAtivo | Low | Simplest entity — catalog with code + name + status |
| Fundo | Medium-High | Two required FKs + status lifecycle (4 states with transition rules) + CNPJ unique constraint |
| Cedente | Medium | Polymorphic PF/PJ validation + CPF/CNPJ conditional requirement |
| FundoCedente | High | N-N payload + status transitions + exposure limits + date range validation + unique active constraint |
| CedenteTipoAtivo | Low | Simple N-N link table |
| FundoTipoAtivo | Low | Simple N-N link table (optional) |

**Highest-risk entity:** FundoCedente — most business rules, most validation, status state machine, financial limits.

---

## Reuse from Existing Codebase

| Pattern | Existing Implementation | Reuse For |
|---------|------------------------|-----------|
| `Entity<T>` base class | `Onboarding.Domain.Common.Entity<T>` | All new entities |
| `Cnpj` value object | `Onboarding.Domain.ValueObjects.Cnpj` | ConsultoriaFundo, Custodiante, Fundo, Cedente (PJ) |
| `Cpf` value object | `Onboarding.Domain.ValueObjects.Cpf` | Cedente (PF) |
| `Email` value object | `Onboarding.Domain.ValueObjects.Email` | Cedente |
| `PhoneNumber` value object | `Onboarding.Domain.ValueObjects.PhoneNumber` | Cedente |
| `PaginatedResult<T>` | `Onboarding.Application.Common.PaginatedResult<T>` | All list endpoints |
| HasQueryFilter pattern | `EmployeeConfiguration` + `ICurrentCompanyService` | All new entity configurations (multi-tenant isolation) |
| `ICommandHandler<T>` / `IQueryHandler<T>` | `Onboarding.Application.Common` | All new commands/queries |
| FluentValidation | Existing validators in `Companies/Validators/` | All new command validators |
| `AdminAuditLog` | `Onboarding.Domain.Aggregates.AdminAuditLog` | Audit logging for new entities |
| Repository pattern | `ICompanyRepository`, `IEmployeeRepository` | New repository interfaces for each aggregate |
| Factory method pattern | `Company.Register()`, `Employee.Register()` | Factory methods on all new entities |

---

## Sources

- **CVM Instruction 558/2024** — Brazilian fund regulation mandating consultoria and custodiante — HIGH confidence (official regulation)
- **CVM Instruction 393/2003** — FIDC (Fundos de Investimento em Direitos Creditórios) definition of cedente — HIGH confidence (official regulation)
- **Existing codebase patterns** — Entity<T>, value objects, HasQueryFilter, CQRS handlers — HIGH confidence (verified in source)
- **Brazilian financial market domain knowledge** — Fund lifecycle stages, tipoAtivo catalog, exposure limits — MEDIUM confidence (domain expert input needed for specific business rules)
- **CNPJ alphanumeric format** — Cnpj value object already supports alphanumeric format (validated in v1 research) — HIGH confidence