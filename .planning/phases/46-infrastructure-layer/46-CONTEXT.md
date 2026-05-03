# Phase 46: Infrastructure Layer v8.0 - Context

**Gathered:** 2026-05-03
**Status:** Ready for planning

<domain>
## Phase Boundary

EF Core persistence layer para 5 aggregate roots + 3 join entities do módulo Gestão de Fundos.
HasQueryFilter em entidades company-scoped (TEN-01, TEN-02), unique constraints para CNPJ/CPF/CNPJ
por empresa (CAD-04, CAD-08, CAD-12, CAD-18), unique global para TipoAtivo codigo (CAD-22),
decimal precision para LimiteExposicaoPercentual/Valor, e CedenteDocumento DU persistence.
Uma migração criando 8 tabelas. Repositories seguem EmployeeRepository pattern.
Sem mudanças no domain layer — Phase 45 está completo e locked.

</domain>

<decisions>
## Implementation Decisions

### CedenteDocumento DU Persistence (CAD-18)
- **D-09:** Shadow properties com discriminator — colunas `documento_tipo` (varchar(2): "PF"/"PJ"),
  `cpf` (varchar(11) nullable), `cnpj_cedente` (varchar(14) nullable). HasConversion mapeia o DU
  para essas 3 colunas. Consistente com pattern existente (Employee.Cpf = nullable VO).
- **D-10:** CedenteDocumento uniqueness = composite filtered indexes:
  - `HasIndex(e => new { e.ClienteId, ... }).HasFilter("documento_tipo = 'PF' AND cpf IS NOT NULL")` para CPF
  - `HasIndex(e => new { e.ClienteId, ... }).HasFilter("documento_tipo = 'PJ' AND cnpj_cedente IS NOT NULL")` para CNPJ
  - Isso satisfaz CAD-18 (duplicate CPF ou CNPJ por company retorna 409 na Application layer)

### FundoCedente Unique Constraint (REL-09)
- **D-11:** Partial unique index no PostgreSQL — `HasIndex(fc => new { fc.FundoId, fc.CedenteId }).HasFilter("status = 1")`.
  Status ATIVO = enum value 1. Permite múltiplos inativos (histórico), garante ativo único por par.

### Repository Pattern
- **D-12:** Seguir EmployeeRepository pattern — IgnoreQueryFilters() para GetById e admin queries,
  GetPagedByCompanyAsync() com explicit companyId para listagem company-scoped.
  Repositories injetam AppDbContext diretamente (thin wrapper). `[ExcludeFromCodeCoverage]`.
- **D-13:** Admin methods (GetPagedAllAsync, GetByIdIgnoreFilterAsync) NÃO ficam nos repositories de
  Phase 46 — serão adicionados na Phase 48 (API + Permissions) quando os AdminFundosController forem criados.
  Phase 46 entrega apenas os contratos mínimos definidos nas interfaces IFundoRepository etc.

### Configuration Pattern
- **D-14:** Todas as 8 configurations seguem mesmo padrão: `IEntityTypeConfiguration<T>` com construtor
  injetando `ICurrentCompanyService` (para HasQueryFilter). TipoAtivoConfiguration é a exceção —
  sem ICurrentCompanyService (entidade global, sem HasQueryFilter).
- **D-15:** FundoCedenteConfiguration registra FundoCedente como owned collection de Fundo
  (`builder.OwnsMany(f => f.Cedentes, ...)`) com cascade delete. FundoTipoAtivo e CedenteTipoAtivo
  seguem mesmo pattern owned.

### Decimal Precision
- **D-16:** `HasPrecision(18, 4)` para LimiteExposicaoValor (valor monetário com 4 casas decimais).
  LimiteExposicaoPercentual usa decimal com sentinel -1 — `HasPrecision(5, 2)` suficiente (range -1 a 100.00).

### Migration Strategy
- **D-17:** Uma única migração EF Core para todas as 8 tabelas. Migration name:
  `AddFundosModule`. Snapshot atualizado junto.

### Multi-tenancy (HasQueryFilter)
- **D-01 (locked):** Fundo, ConsultoriaFundo, Custodiante, Cedente, FundoCedente usam HasQueryFilter
  com `_currentCompanyService.CompanyId`. FundoCedente herda filtro via Fundo (owned collection).
- **D-03 (locked):** TipoAtivo NÃO tem HasQueryFilter — global, sem ClienteId.
- **D-08 (locked):** FundoCedente dentro do Fundo aggregate — owned collection, não aggregate root separado.

### Claude's Discretion
- Nomes exatos de colunas nas configurações (snake_case seguindo pattern existente)
- Ordem de registro no DependencyInjection.cs
- Ordem de ApplyConfiguration no AppDbContext.OnModelCreating
- Details de indexes (nomes, composição exata) seguindo pattern já estabelecido
- Nomes de migration e snapshot
- Estrutura de pastas dos repositories (seguir padrão existente em Repositories/)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Domain layer (implementado no Phase 45)
- `src/Onboarding.Domain/Aggregates/FundoAggregate/Fundo.cs` — Fundo aggregate root com FundoCedente management
- `src/Onboarding.Domain/Aggregates/FundoAggregate/FundoCedente.cs` — Join entity com LimiteExposicao, datas, status
- `src/Onboarding.Domain/Aggregates/FundoAggregate/FundoTipoAtivo.cs` — Simple join entity (FundoId + TipoAtivoId)
- `src/Onboarding.Domain/Aggregates/FundoAggregate/FundoStatus.cs` — State machine enum + validator
- `src/Onboarding.Domain/Aggregates/FundoAggregate/FundoCedenteStatus.cs` — ATIVO/INATIVO enum
- `src/Onboarding.Domain/Aggregates/FundoAggregate/TipoFundo.cs` — Enum
- `src/Onboarding.Domain/Aggregates/ConsultoriaFundoAggregate/ConsultoriaFundo.cs` — Aggregate com CNPJ, RazaoSocial
- `src/Onboarding.Domain/Aggregates/ConsultoriaFundoAggregate/ConsultoriaFundoStatus.cs` — ATIVO/INATIVO enum
- `src/Onboarding.Domain/Aggregates/CustodianteAggregate/Custodiante.cs` — Aggregate com CNPJ, RazaoSocial, CodigoInterno
- `src/Onboarding.Domain/Aggregates/CustodianteAggregate/CustodianteStatus.cs` — ATIVO/INATIVO enum
- `src/Onboarding.Domain/Aggregates/CedenteAggregate/Cedente.cs` — Polymorphic aggregate (PF/PJ via CedenteDocumento)
- `src/Onboarding.Domain/Aggregates/CedenteAggregate/CedenteStatus.cs` — ATIVO/INATIVO enum
- `src/Onboarding.Domain/Aggregates/CedenteAggregate/CedenteTipo.cs` — PF/PJ enum
- `src/Onboarding.Domain/Aggregates/CedenteAggregate/CedenteTipoAtivo.cs` — Simple join entity
- `src/Onboarding.Domain/Aggregates/TipoAtivoAggregate/TipoAtivo.cs` — Global aggregate (NO ClienteId)
- `src/Onboarding.Domain/Aggregates/TipoAtivoAggregate/TipoAtivoStatus.cs` — ATIVO/INATIVO enum
- `src/Onboarding.Domain/Aggregates/TipoAtivoAggregate/TipoAtivoCategoria.cs` — Enum
- `src/Onboarding.Domain/ValueObjects/CedenteDocumento.cs` — Discriminated union (.Pf/.Pj)
- `src/Onboarding.Domain/ValueObjects/LimiteExposicaoPercentual.cs` — Sentinel -1 for unlimited
- `src/Onboarding.Domain/ValueObjects/Cnpj.cs` — CNPJ validation with alphanumeric check-digit
- `src/Onboarding.Domain/ValueObjects/Cpf.cs` — CPF validation
- `src/Onboarding.Domain/Exceptions/DuplicateEntityException.cs` — For REL-09 enforcement
- `src/Onboarding.Domain/Exceptions/InvalidStateTransitionException.cs` — For FundoStatus

### Repository interfaces (implementados no Phase 45)
- `src/Onboarding.Domain/Repositories/IFundoRepository.cs` — Add, Save, GetById, ExistsByCnpj, GetPagedByCompany
- `src/Onboarding.Domain/Repositories/IConsultoriaFundoRepository.cs` — Add, Save, GetById, ExistsByCnpj, GetPagedByCompany
- `src/Onboarding.Domain/Repositories/ICustodianteRepository.cs` — Add, Save, GetById, ExistsByCnpj, GetPagedByCompany
- `src/Onboarding.Domain/Repositories/ICedenteRepository.cs` — Add, Save, GetById, ExistsByDocumento, GetPagedByCompany
- `src/Onboarding.Domain/Repositories/ITipoAtivoRepository.cs` — Add, Save, GetById, ExistsByCodigo, GetPaged (global)

### Infrastructure patterns a seguir
- `src/Onboarding.Infrastructure/Persistence/Configurations/EmployeeConfiguration.cs` — HasQueryFilter pattern com ICurrentCompanyService
- `src/Onboarding.Infrastructure/Persistence/Configurations/AccessGroupConfiguration.cs` — Composite unique index + HasQueryFilter
- `src/Onboarding.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs` — VO HasConversion pattern
- `src/Onboarding.Infrastructure/Persistence/AppDbContext.cs` — Registration pattern for configurations
- `src/Onboarding.Infrastructure/Persistence/AppDbContextFactory.cs` — Design-time factory
- `src/Onboarding.Infrastructure/Repositories/EmployeeRepository.cs` — Repository pattern com IgnoreQueryFilters
- `src/Onboarding.Infrastructure/DependencyInjection.cs` — DI registration pattern
- `src/Onboarding.Application/Common/ICurrentCompanyService.cs` — Interface para CompanyId injection

### Context de fases anteriores
- `.planning/phases/45-domain-layer-v8/45-CONTEXT.md` — Decisões locked do Domain Layer (D-01 a D-08)
- `.planning/phases/39-keycloak-groups-permissions/39-CONTEXT.md` — Permissões resource:action, CurrentCompanyService, HasQueryFilter

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `EmployeeConfiguration` pattern: IEntityTypeConfiguration<T> com construtor injetando ICurrentCompanyService → HasQueryFilter
- `CompanyConfiguration` pattern: HasConversion para VOs (Email, Cnpj, PhoneNumber) com nullable handling
- `AccessGroupConfiguration` pattern: Composite unique index (CompanyId + Name), HasQueryFilter
- `EmployeeRepository` pattern: thin wrapper, IgnoreQueryFilters para GetById e admin, explicit CompanyId para listing
- `DependencyInjection.cs`: registration pattern para configs e repositories

### Established Patterns
- Configurações usam snake_case para nomes de colunas (nome da propriedade em PascalCase → coluna em snake_case)
- VOs nullable (Email, PhoneNumber, Cpf) usam HasConversion com null check: `vo => vo == null ? null : vo.Value`
- VOs non-nullable (Cnpj em ConsultoriaFundo, Custodiante, Fundo) usam HasConversion direto
- Foreign keys sem navigation property — `builder.HasOne<...>().WithMany().HasForeignKey(...).OnDelete(DeleteBehavior.Restrict)`
- Enums são stored como inteiros por padrão (sem HasConversion para string)
- HasQueryFilter sempre referencia `_currentCompanyService.CompanyId`

### Integration Points
- `AppDbContext.OnModelCreating`: adicionar 5+ novas ApplyConfiguration calls (5 com ICurrentCompanyService, 1 sem)
- `AppDbContext` DbSets: adicionar 8 novos DbSets (5 aggregates + 3 joins via owned collections)
- `DependencyInjection.cs`: adicionar 5 novos `services.AddScoped<IXxxRepository, XxxRepository>()`
- Migration: `dotnet ef migrations add AddFundosModule` no projeto Infrastructure

</code_context>

<specifics>
## Specific Ideas

- CedenteDocumento DU: 3 shadow properties (documento_tipo, cpf, cnpj_cedente) com HasConversion que
  mapeia Match() para ler e .Pf()/.Pj() para escrever. Column name `cnpj_cedente` (não `cnpj`) para
  evitar conflito com a coluna Cnpj do Fundo/ConsultoriaFundo/Custodiante.
- FundoCedente: owned collection via `OwnsMany` dentro de FundoConfiguration. Cascade delete automático.
- FundoTipoAtivo e CedenteTipoAtivo: owned collections similares (OwnsMany com FK apenas).
- LimiteExposicaoPercentual: HasConversion armazena decimal diretamente (sentinel -1 é um valor decimal válido).
- LimiteExposicaoValor: HasPrecision(18, 4) para valores monetários com precisão centesimal.

</specifics>

<deferred>
## Deferred Ideas

None — discussão ficou dentro do escopo da fase.

</deferred>

---

*Phase: 46-infrastructure-layer*
*Context gathered: 2026-05-03*