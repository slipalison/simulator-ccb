# Phase 45: Domain Layer v8.0 - Context

**Gathered:** 2026-05-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Modelar 5 aggregate roots (Fundo, ConsultoriaFundo, Custodiante, Cedente, TipoAtivo) e 3 join
entities (FundoCedente, CedenteTipoAtivo, FundoTipoAtivo) com state machine FundoStatus,
multi-tenancy (company-scoped vs global), CedenteDocumento discriminated union, e
LimiteExposicaoPercentual sentinel value. Zero dependência de infraestrutura — TDD puro.
Unit tests passam sem DB, sem Keycloak, sem DI container.

</domain>

<decisions>
## Implementation Decisions

### Multi-tenancy & Scoping
- **D-01:** ConsultoriaFundo, Custodiante, Cedente são company-scoped (propriedade `ClienteId`, HasQueryFilter no Infrastructure). Cada empresa cadastra os seus.
- **D-03:** TipoAtivo é global — sem `ClienteId`, sem HasQueryFilter. Catálogo CVM compartilhado entre empresas.

### FundoStatus State Machine
- **D-02:** FundoStatus = enum com transições: RASCUNHO→ATIVO↔SUSPENSO→EM_LIQUIDACAO→ENCERRADO. Transições inválidas rejeitadas com domain exception.
- **D-07:** Implementação: enum `FundoStatus` + método `Fundo.TransitionTo(FundoStatus novo)` dentro do aggregate Fundo. Todas as transições e rejeições centralizadas num método.Não usar State Pattern — 5 estados finitos e ~6 transições não justificam 5+ classes. YAGNI.

### Cedente Polymorphic
- **D-05:** Cedente = aggregate único para PF e PJ. Não separar em dois entities.
- **D-06:** `CedenteDocumento` = discriminated union VO com `.Pf(Cpf)` e `.Pj(Cnpj)`. Zero null risk. Cada factory method do Cedente aceita CedenteDocumento. Pattern functional, type-safe.

### FundoCedente Join Entity
- **D-08:** FundoCedente é entity dentro aggregate Fundo (não aggregate root separado). Fundo gerencia coleção de FundoCedentes via `AddCedente()`, `UpdateCedente()`, `RemoveCedente()`. REL-09 ("max 1 associação ativa por par Fundo-Cedente") é invariante enforce dentro do Fundo.

### LimiteExposicao
- **D-04:** LimiteExposicaoPercentual = value object com sentinel value -1 = ilimitado. Validação: -1 (unlimited) ou 0-100 (percentual normal). Tipo decimal.

### Join Entities Simples
- FundoTipoAtivo e CedenteTipoAtivo = join entities simples (apenas Guid FKs: FundoId/CedenteId + TipoAtivoId). Sem payload. Gerenciadas via seus respectivos aggregates.

### Permissions
- PERM-01: Novas permission constants em `Permissions.cs`: `funds:read`, `funds:write`, `funds:delete`, `funds:manage`. Extende sistema existente (resource:action pattern).

### Claude's Discretion
- Estrutura de pastas — seguir DDD pattern existente (`Aggregates/FundoAggregate/`, etc.)
- Nomes exatos de propriedades e métodos nos novos entities
- Details de FundoCedente payload (campos de limite, datas, status)
- Ordem e nomes dos enums (TipoFundo, FundoStatus, TipoAtivoCategoria, CedenteTipo)
- Como modelar CedenteDocumento internalmente (record struct vs record class, match pattern)
- Nomes de novos ActionType enum values para audit

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Domain — arquivos existentes a reutilizar
- `src/Onboarding.Domain/ValueObjects/Cnpj.cs` — VO com validação check-digit alfanumérica (reutilizar sem mudanças)
- `src/Onboarding.Domain/ValueObjects/Cpf.cs` — VO com validação check-digit (reutilizar sem mudanças)
- `src/Onboarding.Domain/ValueObjects/Email.cs` — VO com validação de formato (reutilizar sem mudanças)
- `src/Onboarding.Domain/ValueObjects/PhoneNumber.cs` — VO com validação de tamanho (reutilizar sem mudanças)
- `src/Onboarding.Domain/Common/Entity.cs` — Base entity com Id + equality
- `src/Onboarding.Domain/Aggregates/EmployeeAggregate/Permissions.cs` — Permission constants pattern a estender
- `src/Onboarding.Domain/Aggregates/Audit/ActionType.cs` — Enum a estender com novos action types de fundos

### Domain — arquivos de referência (pattern)
- `src/Onboarding.Domain/Aggregates/CompanyAggregate/Company.cs` — Aggregate root pattern: factory method, update method, VOs
- `src/Onboarding.Domain/Aggregates/EmployeeAggregate/Employee.cs` — Aggregate com CompanyId FK, AccessGroupId FK
- `src/Onboarding.Domain/Aggregates/EmployeeAggregate/AccessGroup.cs` — Entity com CompanyId, CreateDefaultGroups()

### Context de fases anteriores
- `.planning/phases/37-domain-model-redesign/37-CONTEXT.md` — Decisões de Company/Employee, HasQueryFilter, VOs, CQRS
- `.planning/phases/39-keycloak-groups-permissions/39-CONTEXT.md` — Permissões resource:action, CurrentCompanyService, HasQueryFilter pattern

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Cnpj` VO: validação check-digit alfanumérica — reutilizar em ConsultoriaFundo, Custodiante, Cedente PJ, Fundo
- `Cpf` VO: validação check-digit — reutilizar em Cedente PF
- `Email` VO: validação formato + lowercase — reutilizar em todos entities com email
- `PhoneNumber` VO: 8-15 dígitos — reutilizar em todos entities com telefone
- `Entity<TId>` base: Id + equality — base para todos novos entities
- `Permissions.All`: pattern de constantes resource:action — estender com funds:*
- `ActionType` enum: extensível — adicionar action types de fundos

### Established Patterns
- Aggregate root com factory method estático: `Entity.Register(...)` — consistente
- VOs como `sealed record` com factory `Create()` que valida
- CompanyId FK como Guid simples (sem navigation property) — Fundo, ConsultoriaFundo, Custodiante, Cedente seguem mesmo
- AccessGroupId FK pattern (Employee) — mesmo para FKs de ConsultoriaFundo, Custodiante nos entities que referenciam
- HasQueryFilter: `_currentCompanyService.CompanyId` — Phase 46 aplica no Infrastructure

### Integration Points
- `Permissions.cs`: adicionar `funds:read`, `funds:write`, `funds:delete`, `funds:manage` + atualizar `All` array
- `ActionType.cs`: adicionar novos values (FundoCreated, FundoStatusChanged, etc.)
- `AppDbContext`: Phase 46 adiciona DbSet para cada novo entity
- `Program.cs`: Phase 47 registra novos repositories e handlers no DI

</code_context>

<specifics>
## Specific Ideas

- CedenteDocumento como discriminated union: match pattern no handler para saber se é PF ou PJ, sem null checks
- Fundo.TransitionTo() lança `DomainException` com mensagem clara sobre transição inválida
- FundoCedente dentro de Fundo: coleção `List<FundoCedente>` com método AddCedente que checa duplicidade ativa
- Permissões de fundos (funds:*) adicionadas ao Permissions.cs — admin-empresa recebe funds:manage por default

</specifics>

<deferred>
## Deferred Ideas

None — discussão ficou dentro do escopo da fase.

</deferred>

---

*Phase: 45-domain-layer-v8*
*Context gathered: 2026-05-03*