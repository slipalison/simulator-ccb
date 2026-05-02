# Phase 39: Keycloak Groups & Permissions - Context

**Gathered:** 2026-04-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Configurar Keycloak groups (admin-empresa, viewer, dashboard) no realm `client`, mapear
claims de grupos do JWT para permissões resource:action no backend, aplicar autorização
granular por política (6 políticas resource:action), e garantir isolamento entre empresas
com defense-in-depth. PJ pode atribuir/remover groups de funcionários — sincronização
banco → Keycloak é obrigatória.

**Requisitos:** PERM-01, PERM-02, PERM-03, PERM-04, PERM-05

</domain>

<decisions>
## Implementation Decisions

### JWT Claims — Abordagem Dual
- **D-01:** Realm `backoffice` continua usando `realm_access.roles` (mapper existente, `RealmRolesClaimsTransformation` existente). Nenhuma mudança no backoffice.
- **D-02:** Realm `client` recebe um novo **Group Membership mapper** no `roles` client scope → claim `groups` no JWT (array de strings com nomes dos groups). Mapper adicionado ao `client-realm.json`.
- **D-03:** Novo `GroupsClaimsTransformation` no backend extrai claim `groups` do JWT (apenas para BearerClient scheme) e adiciona como `ClaimTypes.Role` para compatibilidade com `[Authorize(Roles = "...")]`.

### Permissões — Group Name → DB Permissions
- **D-04:** Backend NÃO confia no nome do group isoladamente. Fluxo: JWT `groups` claim → nome do AccessGroup → lookup no banco (tabela `access_groups`) → lista de permissões `resource:action`.
- **D-05:** Permissões predefinidas (Phase 37 D-07): `employees:read`, `employees:write`, `employees:delete`, `audit:read`, `dashboard:access`, `access-groups:manage`.

### Políticas de Autorização — 6 Políticas Granulares
- **D-06:** Seis políticas de autorização registradas em `Program.cs`, uma por `resource:action`:
  - `EmployeeRead` → requer `employees:read`
  - `EmployeeWrite` → requer `employees:write`
  - `EmployeeDelete` → requer `employees:delete`
  - `AuditRead` → requer `audit:read`
  - `DashboardAccess` → requer `dashboard:access`
  - `AccessGroupsManage` → requer `access-groups:manage`
- **D-07:** Política adicional `CrossCompanyAccess` → requer role `admin` (para endpoints backoffice que bypassam HasQueryFilters).
- **D-08:** Requirement handler customizado: `PermissionAuthorizationHandler` recebe `ICurrentCompanyPermissionsService`, resolve permissões atuais do usuário logado, e verifica se a permissão requerida está na lista.

### CurrentCompanyService — JWT → DB Lookup
- **D-09:** Novo middleware `ClientClaimsMiddleware` (após `UseAuthentication`, antes de `UseAuthorization`):
  1. Lê JWT `sub` claim do usuário autenticado (BearerClient scheme apenas)
  2. Busca `Company` por `KeycloakUserId == sub` no banco
  3. Seta `ICurrentCompanyService.CompanyId` com o Company.Id encontrado
  4. Seta permissões no `ICurrentCompanyPermissionsService` baseado no AccessGroup do usuário (PJ dono = admin-empresa; funcionário = seu AccessGroup)
- **D-10:** `ICurrentCompanyPermissionsService` é novo — scoped service que expõe `IReadOnlyList<string> Permissions` e `Guid CompanyId`. Populado pelo `ClientClaimsMiddleware` a cada request.
- **D-11:** Se JWT `sub` não mapear a nenhuma Company → CompanyId = Guid.Empty → HasQueryFilter retorna vazio → 403 Forbidden.

### Keycloak Group Provisioning
- **D-12:** Client-realm.json recebe 3 groups na seção `groups`: `admin-empresa`, `viewer`, `dashboard`. Hierarquia plana (sem subgrupos).
- **D-13:** Quando `AccessGroup.CreateDefaultGroups()` roda no registro da Company (D-08/Phase 37), handler chama novo método `IKeycloakUserService.CreateGroupAsync(targetRealm, groupName)` para criar groups no Keycloak se não existirem (idempotente).
- **D-14:** Auto-sync banco → Keycloak (D-10/Phase 37): quando AccessGroup é criado/atualizado no banco, backend sincroniza como Keycloak Group no realm `client`. Fonte da verdade: banco.

### PJ-to-Employee Group Assignment Flow
- **D-15:** `ChangeEmployeeAccessGroupCommandHandler` (Phase 38, já existe) precisa ser estendido:
  1. Atualiza `employee.AccessGroupId` no banco (já faz)
  2. Resolve nome do novo AccessGroup via lookup no banco
  3. Chama `IKeycloakUserService.AddUserToGroupAsync(targetRealm, keycloakUserId, keycloakGroupId)` — novo método
  4. Remove usuário do group anterior: `IKeycloakUserService.RemoveUserFromGroupAsync(targetRealm, keycloakUserId, previousKeycloakGroupId)` — novo método
  5. Em caso de falha no Keycloak após DB commit: log warning + audit, não rollback (eventual consistency)
- **D-16:** Quando um funcionário é criado (`RegisterEmployeeCommandHandler`), step de Keycloak cria o usuário E adiciona ao group `viewer` (ou group informado) via novo método `AddUserToGroupAsync`.

### Company Isolation Defense-in-Depth
- **D-17:** HasQueryFilter já existe (Phase 37 D-17). Agora é funcional porque `CurrentCompanyService.CompanyId` será setado pelo middleware (D-09).
- **D-18:** Controller-level check: `CompaniesController` já verifica `companyId != _currentCompanyService.CompanyId` → 403. Padrão mantido em todos os endpoints.
- **D-19:** Service-layer check: handlers de comando verificam que o employee pertence à CompanyId do usuário logado antes de executar. Defense-in-depth sobre o HasQueryFilter.
- **D-20:** PJ dono (Company.KeycloakUserId == sub) recebe automaticamente todas as permissões (equivalente a admin-empresa), independente de JWT groups claim. O middleware detecta PJ dono e configura permissões completas.

### Agent's Discretion
- Nomes exatos dos novos métodos em `IKeycloakUserService` (AddUserToGroupAsync, RemoveUserFromGroupAsync, CreateGroupAsync, GetGroupByNameAsync, etc.)
- Estrutura do `GroupsClaimsTransformation` — detalhes de implementação
- Como mapear Keycloak Group ID (UUID) ↔ AccessGroup — tabela de mapeamento ou atributo no banco
- Tratamento de edge cases: usuário em múltiplos groups no Keycloak, group deletado no banco mas ainda no Keycloak
- Ordem do middleware pipeline (ClientClaimsMiddleware vs ClientSessionMiddleware)
- Estrutura do `ICurrentCompanyPermissionsService` — interface e implementação exatas

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Keycloak — Realm config a modificar
- `keycloak/client-realm.json` — Adicionar seção `groups` (admin-empresa, viewer, dashboard) + Group Membership mapper no scope `roles`
- `keycloak/backoffice-realm.json` — Nenhuma mudança necessária (D-01)

### Domain — AccessGroup e Permissions (existentes, referência)
- `src/Onboarding.Domain/Aggregates/EmployeeAggregate/AccessGroup.cs` — Entidade com CreateDefaultGroups(), UpdatePermissions(), companyId FK
- `src/Onboarding.Domain/Aggregates/EmployeeAggregate/Permissions.cs` — Constantes resource:action (All = 6 permissões)
- `src/Onboarding.Domain/Aggregates/EmployeeAggregate/Employee.cs` — Aggregate com AccessGroupId FK, SetAccessGroup()

### Application — Handlers que precisam de extensão
- `src/Onboarding.Application/Companies/Commands/ChangeEmployeeAccessGroupCommandHandler.cs` — Adicionar sync Keycloak Group
- `src/Onboarding.Application/Companies/Commands/RegisterEmployeeCommandHandler.cs` — Adicionar AddUserToGroupAsync
- `src/Onboarding.Application/Companies/Commands/RegisterCompanyCommandHandler.cs` — Seed AccessGroups + CreateGroupAsync no Keycloak
- `src/Onboarding.Application/Common/IKeycloakUserService.cs` — Interface a estender com novos métodos de Group
- `src/Onboarding.Application/Common/ICurrentCompanyService.cs` — Interface existente (CompanyId)

### Infrastructure — KeycloakUserService a estender
- `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs` — Implementação Admin API com targetRealm routing
- `src/Onboarding.Infrastructure/Persistence/CurrentCompanyService.cs` — Implementação existente (CompanyId = Guid.Empty por enquanto)

### API — Middleware e Security
- `src/Onboarding.API/Middleware/ClientSessionMiddleware.cs` — Referência de padrão de middleware (cookie → Bearer header)
- `src/Onboarding.API/Security/RealmRolesClaimsTransformation.cs` — Padrão de claims transformation para backoffice
- `src/Onboarding.API/Controllers/CompaniesController.cs` — Endpoints com [Authorize(AuthenticationSchemes = "BearerClient")] + _currentCompanyService.CompanyId check
- `src/Onboarding.API/Controllers/AdminUserController.cs` — Endpoints backoffice com [Authorize(Roles = "admin")]

### Context de fases anteriores
- `.planning/phases/37-domain-model-redesign/37-CONTEXT.md` — Decisões de AccessGroup, HasQueryFilter, CurrentCompanyService, auto-sync banco → Keycloak
- `.planning/phases/34-isolar-backoffice-e-client-em-realms-separados/34-CONTEXT.md` — Decisão de dois realms, targetRealm routing

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `RealmRolesClaimsTransformation`: padrão de claims transformation (extract realm_access.roles → ClaimTypes.Role) — reusar pattern para groups
- `ClientSessionMiddleware`: padrão de middleware por-scheme (BearerClient) — reusar pattern para ClientClaimsMiddleware
- `CurrentCompanyService`: scoped service com CompanyId — estender para incluir permissions
- `IKeycloakUserService`: API completa de Admin REST (create/delete/block/unblock/user) — estender com group operations
- `KeycloakUserService.GetClient(targetRealm)`: routing de realm (client/backoffice) — reusar para group API calls
- `AccessGroup.CreateDefaultGroups()`: factory de 3 grupos padrão — referência para Keycloak Group provisioning
- `Permissions.All`: lista de todas as 6 permissões — referencia para policy registration

### Established Patterns
- `IClaimsTransformation` do ASP.NET Core para extrair claims customizadas
- Middleware pattern: `app.Use(async (context, next) => { ... })` antes de UseAuthentication/UseAuthorization
- `[Authorize(AuthenticationSchemes = "BearerClient")]` e `[Authorize(Roles = "admin")]` já funcionam
- `ICurrentCompanyService` é scoped — uma instância por request HTTP
- `HasQueryFilter` em EmployeeConfiguration/AccessGroupConfiguration — `_currentCompanyService.CompanyId`

### Integration Points
- `Program.cs`: registrar novo ClientClaimsMiddleware, GroupsClaimsTransformation, ICurrentCompanyPermissionsService, 7 authorization policies
- `client-realm.json`: adicionar `groups` + Group Membership mapper
- `IKeycloakUserService`: estender com 4+ novos métodos de group management
- `CompaniesController`: substituir `[Authorize(AuthenticationSchemes = "BearerClient")]` por `[Authorize(Policy = "EmployeeRead")]` etc. onde aplicável
- `AdminUserController`: adicionar `[Authorize(Policy = "CrossCompanyAccess")]` em endpoints que bypassam HasQueryFilters

</code_context>

<specifics>
## Specific Ideas

- Backoffice realm (realm_access.roles) não muda — zero impacto no fluxo admin existente
- Client realm usa groups claim por ser semanticamente mais preciso que roles (groups = grupos de acesso de empresa, roles = papéis de sistema)
- PJ dono (Company.KeycloakUserId == JWT sub) sempre tem permissões totais — não depende de JWT groups claim
- Funcionário com admin-empresa tem mesmos poderes do PJ dono (PERM-01) → middleware detecta access group "admin-empresa" e seta todas as permissões
- Sincronização banco → Keycloak é eventual consistency: falha no Keycloak após DB commit é logged mas não causa rollback
- Mapeamento Keycloak Group ID: backend precisa saber o UUID do Keycloak Group para AddUserToGroupAsync — resolver via nome (GetGroupByNameAsync) ou armazenar KeycloakGroupId no banco

</specifics>

<deferred>
## Deferred Ideas

None — discussão ficou dentro do escopo da fase.

</deferred>

---

*Phase: 39-keycloak-groups-permissions*
*Context gathered: 2026-04-26*