# Phase 37: Domain Model Redesign - Context

**Gathered:** 2026-04-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Novos aggregates Company (PJ) e Employee (PF) substituem Client. AccessGroup como entidade
configurável com permissões resource:action. TermsAcceptance value object. Remoção completa
do fluxo PF (zero vestígios). Migration limpa: drop `clients`, cria `companies`, `employees`,
`access_groups`. Admin endpoints que operam sobre Client são migrados nesta fase.
Base zerada via `docker compose down -v`.

</domain>

<decisions>
## Implementation Decisions

### Estrutura dos Aggregates
- **D-01:** Company e Employee são aggregate roots separados. Company NÃO contém lista de Employees — relacionamento via FK no Infrastructure (EF Core).
- **D-02:** Employee armazena `CompanyId` como `Guid` simples (sem propriedade de navegação no domain). EF Core configura FK `company_id` em `employees`.
- **D-03:** Propriedades de Company: Cnpj (VO), RazaoSocial (string), Email (VO), PhoneNumber (VO), KeycloakUserId (string?), TermsAcceptance (VO).
- **D-04:** Propriedades de Employee: Cpf (VO), Nome (string), Email (VO), PhoneNumber (VO), CompanyId (Guid), KeycloakUserId (string?), AccessGroupId (Guid), DeletedAt (DateTimeOffset?).
- **D-05:** Value objects Cpf, Cnpj, Email, PhoneNumber reutilizados sem mudanças — já validam check-digit.

### AccessGroup — Entidade Configurável
- **D-06:** `AccessGroup` é entidade (não enum) com CompanyId FK. Cada PJ gerencia seus próprios grupos.
- **D-07:** Permissões usam pattern `resource:action` — valores predefinidos no código (enum/const). Backend conhece todas as permissões possíveis. Tipos: `employees:read`, `employees:write`, `employees:delete`, `audit:read`, `dashboard:access`, `access-groups:manage`.
- **D-08:** Novas Companies nascem com 3 grupos padrão (seed automático no registro):
  - `admin-empresa` — todas as permissões
  - `viewer` — apenas `employees:read`, `audit:read`
  - `dashboard` — apenas `dashboard:access`
- **D-09:** PJ pode criar AccessGroups customizados selecionando permissões da lista predefinida.
- **D-10:** Auto-sync banco → Keycloak: quando AccessGroup é criado/atualizado no banco, backend sincroniza automaticamente como Keycloak Group no realm `client`. Fonte da verdade: banco.

### TermsAcceptance & IpAddress
- **D-11:** `TermsAcceptance` é value object com: `AcceptedAt` (DateTimeOffset), `TermsVersion` (string), `IpAddress` (string).
- **D-12:** Aceite de termos é obrigatório no registro PJ (REG-04). Value object lança exceção se não aceito.
- **D-13:** `TermsVersion` é constante hardcoded: `TermsCurrentVersion = "1.0"`. Suficiente para mock.
- **D-14:** IpAddress obtido via `X-Forwarded-For` (primeiro IP) com fallback `RemoteIpAddress`. Compatível com Docker/reverse proxy.

### Migration Strategy
- **D-15:** Drop tabela `clients` e criar `companies`, `employees`, `access_groups`. Base zerada — dados existentes são descartados.
- **D-16:** Preservar tabelas auxiliares `admin_audit_logs` e `password_reset_tokens` — não tocar nelas na migration.
- **D-17:** `HasQueryFilter` em `EmployeeConfiguration` e `AccessGroupConfiguration` filtrando por `CompanyId` — garantia de isolamento entre empresas.

### Remoção PF — Escopo Completo
- **D-18:** Fase 37 é completa: domain + migration + admin endpoints + testes. Não apenas domain.
- **D-19:** Remoção total sem vestígios: deletar Client.cs, ClientType.cs, ClientStatus.cs, IClientRepository, ClientConfiguration, RegisterClientCommand, RegisterClientCommandHandler, RegisterClientCommandValidator, ClientsController, RegistrationController, RegisterClientRequest, DTOs admin (UserSummaryDto, UserDetailDto, UpdateUserRequest), handlers admin (UpdateUserCommand, DeleteUserCommand, BlockUserCommand, UnblockUserCommand), queries admin (GetPaginatedUsersQuery, GetUserDetailsQuery).
- **D-20:** Testes unitários: deletar todos que referenciam Client/PF/PJ e reescrever para Company/Employee/AccessGroup/TermsAcceptance. Base zerada nos testes.

### Claude's Discretion
- Número exato de permissões resource:action e nomes — definir durante implementação baseado no que o sistema precisa.
- Estrutura de pastas dos novos arquivos domain — seguir padrão DDD existente (`Aggregates/`, `ValueObjects/`, `Repositories/`).
- Detalhes da migration EF Core — ordem de criação das tabelas, nomes de índices.
- Como refatorar AdminUserController — migrar endpoints existentes para operar sobre Company/Employee.

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

### Domain — arquivos a DELETAR
- `src/Onboarding.Domain/Aggregates/ClientAggregate/Client.cs` — aggregate obsoleto
- `src/Onboarding.Domain/Aggregates/ClientAggregate/ClientType.cs` — enum obsoleto
- `src/Onboarding.Domain/Aggregates/ClientAggregate/ClientStatus.cs` — enum não utilizado
- `src/Onboarding.Domain/Repositories/IClientRepository.cs` — repositório obsoleto
- `src/Onboarding.Domain/Exceptions/DuplicateClientException.cs` — renomear/recriar para Company

### Infrastructure — EF Core configs a DELETAR/substituir
- `src/Onboarding.Infrastructure/Persistence/Configurations/ClientConfiguration.cs` — substituir por CompanyConfiguration, EmployeeConfiguration, AccessGroupConfiguration
- `src/Onboarding.Infrastructure/Persistence/AppDbContext.cs` — remover DbSet<Client>, adicionar DbSet<Company/Employee/AccessGroup>

### Application — CQRS a DELETAR/substituir
- `src/Onboarding.Application/Clients/Commands/RegisterClientCommand.cs`
- `src/Onboarding.Application/Clients/Commands/RegisterClientCommandHandler.cs`
- `src/Onboarding.Application/Clients/Validators/RegisterClientCommandValidator.cs`
- `src/Onboarding.Application/Clients/DTOs/RegisterClientResult.cs`
- `src/Onboarding.Application/Admin/Commands/UpdateUserCommand.cs`
- `src/Onboarding.Application/Admin/Commands/DeleteUserCommand.cs`
- `src/Onboarding.Application/Admin/Commands/BlockUserCommand.cs`
- `src/Onboarding.Application/Admin/Commands/UnblockUserCommand.cs`
- `src/Onboarding.Application/Admin/Queries/GetPaginatedUsersQuery.cs`
- `src/Onboarding.Application/Admin/Queries/GetUserDetailsQuery.cs`
- `src/Onboarding.Application/Admin/DTOs/UserSummaryDto.cs`
- `src/Onboarding.Application/Admin/DTOs/UserDetailDto.cs`
- `src/Onboarding.Application/Admin/DTOs/UpdateUserRequest.cs`

### API — Controllers a DELETAR/substituir
- `src/Onboarding.API/Controllers/RegistrationController.cs` — substituir por CompanyRegistrationController
- `src/Onboarding.API/Controllers/ClientsController.cs` — substituir por CompaniesController
- `src/Onboarding.API/Controllers/AdminUserController.cs` — migrar para operar sobre Company/Employee
- `src/Onboarding.API/Controllers/RegisterClientRequest.cs` — deletar

### Keycloak — integração existente
- `src/Onboarding.Application/Common/IKeycloakUserService.cs` — interface com `targetRealm` param (reutilizar)
- `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs` — implementação com realm routing (reutilizar/adaptar)

### Audit — infra existente
- `src/Onboarding.Application/Common/IAuditService.cs` — serviço append-only (reutilizar)
- `src/Onboarding.Domain/Aggregates/AdminAuditLog.cs` — estender com CompanyId/TargetEmployeeId/ActionTypes novos
- `src/Onboarding.Domain/Aggregates/Audit/ActionType.cs` — estender enum com novos tipos

### Context de fases anteriores
- `.planning/phases/34-isolar-backoffice-e-client-em-realms-separados/34-CONTEXT.md` — decisão de dois realms (client + backoffice), targetRealm routing
- `.planning/phases/35-backoffice-admin-management-pagina-o-filtros-reset-senha-edi/35-CONTEXT.md` — padrões de geração de senha temporária, revogação de sessão

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Cnpj` VO: validação check-digit alfanumérica (juillet 2026 ready)
- `Cpf` VO: validação check-digit completa
- `Email` VO: validação de formato + lowercase
- `PhoneNumber` VO: validação 8-15 dígitos
- `Entity<TId>` base: Id + equality por tipo
- `IKeycloakUserService`: `CreateUserAsync(targetRealm, ...)`, `BlockUserAsync`, etc.
- `IAuditService`: `LogAsync(actionType, ...)` append-only
- `AdminAuditLog`: entidade audit extensível

### Established Patterns
- CQRS manual via DI: `ICommandHandler<TCommand, TResult>`, `IQueryHandler<TQuery, TResult>`
- FluentValidation: `AbstractValidator<T>` no Application layer
- EF Core fluent config: `IEntityTypeConfiguration<T>` separado por entidade
- Value objects como `sealed record` com factory `Create()` que valida
- Aggregate factory methods: `RegisterPessoaJuridica()`, `RegisterPessoaFisica()` (migrar para `Company.Register()`)
- Keycloak realm routing: `targetRealm` param em todas as chamadas IKeycloakUserService

### Integration Points
- `AppDbContext`: adicionar `DbSet<Company>`, `DbSet<Employee>`, `DbSet<AccessGroup>`
- `Program.cs`: registrar novos repositórios e handlers no DI
- `AdminUserController` (651 lines): migrar endpoints GET/PUT/POST/DELETE para Company/Employee
- `client-realm.json`: Keycloak groups para AccessGroups
- Frontend `admin-api.ts`: adaptar para novos endpoints (Phase 38/40/41)

</code_context>

<specifics>
## Specific Ideas

- AccessGroup `admin-empresa` = todas as permissões. PJ (dono da empresa) recebe automaticamente este grupo.
- `viewer`: apenas leitura — `employees:read` + `audit:read`.
- `dashboard`: apenas `dashboard:access`.
- Seed de AccessGroups acontece no registro da Company, não via migration.
- Permissões resource:action predefinidas: `employees:read`, `employees:write`, `employees:delete`, `audit:read`, `dashboard:access`, `access-groups:manage`. Novas permissões adicionadas via código.
- TermsAcceptance stored como owned type (EF Core `OwnsOne`) na tabela `companies`.
- `HasQueryFilter` isolamento: `e => e.CompanyId == _currentCompanyId` — `_currentCompanyId` injetado via `ICurrentCompanyService` ou similar no Infrastructure.

</specifics>

<deferred>
## Deferred Ideas

None — discussão ficou dentro do escopo da fase.

</deferred>

---

*Phase: 37-domain-model-redesign*
*Context gathered: 2026-04-25*