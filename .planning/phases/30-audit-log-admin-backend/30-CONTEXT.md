# Phase 30 Context: Audit Log Backend + Admin Management Backend

**Gathered:** 2026-04-15
**Status:** Ready for planning
**Source:** /gsd-discuss-phase 30

<domain>
## Phase Boundary

Backend-only phase. Entrega:
1. Unificação dos dois sistemas de audit log em `IAuditService` + `AdminAuditLog`
2. `GET /api/admin/administrators` — lista todos os admins via Keycloak
3. Renomeação `POST /api/admin/users` (CreateAdmin) → `POST /api/admin/administrators` + update no frontend api client

O que já existe e NÃO precisa ser reimplementado:
- `AdminAuditLog` entity + EF Core migration `AddAdminAuditLog` ✅
- `IAdminAuditLogRepository` + implementation ✅
- `CreateAdminCommand` + handler (inclui geração de senha temporária + audit) ✅
- `GetAuditLogQuery` + handler (paginado) ✅
- Block/unblock/update/delete handlers (funcionais, mas ainda usando AuditLog legado) ✅
- `AdminUserController` com endpoints user management ✅

</domain>

<decisions>
## Implementation Decisions

### Unificação de Audit Logs

**Decisão:** Criar `IAuditService` como abstração única para gravar em `AdminAuditLog`. Migrar todos os handlers que ainda usam `IAuditLogRepository` + `AuditLog` para usar `IAuditService`.

Handlers a migrar (de `IAuditLogRepository` → `IAuditService`):
- `BlockUserCommandHandler` — grava `UserBlocked`
- `UnblockUserCommandHandler` — grava `UserUnblocked`
- `UpdateUserCommandHandler` — grava `UserUpdated`
- `DeleteUserCommandHandler` — grava `UserDeleted`

`CreateAdminCommandHandler` já usa `IAdminAuditLogRepository` diretamente — migrar para `IAuditService` também (consistência).

**Destino da tabela AuditLog legada:** Remover a entidade `AuditLog`, a interface `IAuditLogRepository`, e criar nova EF Core migration que dropa a tabela `AuditLogs` (validar que está vazia em dev antes). `AdminAuditLogs` se torna a única tabela de audit.

### IAuditService Interface

```csharp
public interface IAuditService
{
    Task RecordAsync(
        string actorSub,
        string actorEmail,
        ActionType action,
        Guid? targetUserId = null,
        string? targetUserName = null,
        string? details = null,
        string? ipAddress = null,
        CancellationToken ct = default);
}
```

Implementação: injeta `IAdminAuditLogRepository`, cria `AdminAuditLog.Create(...)`, chama `AddAsync` + `SaveChangesAsync`.

### Route Strategy — /administrators

**Decisão:** Renomear `POST /api/admin/users` (CreateAdmin) para `POST /api/admin/administrators`.

- Remover o action `POST /api/admin/users` do `AdminUserController`
- Adicionar `POST /api/admin/administrators` mapeando para o mesmo `CreateAdminCommand`
- Adicionar `GET /api/admin/administrators` (novo)
- Atualizar `frontend/backoffice/src/lib/admin-api.ts` linha ~324: mudar `/api/admin/users` → `/api/admin/administrators` na função `createAdmin`

**Atenção:** `GET /api/admin/users` (listagem de clientes paginada) NÃO é afetado — continua igual.

### GET /api/admin/administrators — Implementação

**Fonte de dados:** Keycloak direto (única fonte de verdade para roles). Não usar cache em app_db.

**Keycloak API:** `GET /admin/realms/onboarding/roles/admin/users` retorna todos os usuários com a role "admin".

**Novo método em IKeycloakUserService:**
```csharp
Task<IReadOnlyList<AdminUserDto>> GetUsersByRoleAsync(string roleName, CancellationToken ct = default);
```

**Escopo da listagem:**
- Retorna TODOS os admins: ativos (Enabled=true) e bloqueados (Enabled=false)
- Inclui o próprio caller (admin logado aparece na lista)
- Ordenação: Keycloak retorna por ordem de criação (não reordenar)

**DTO de resposta:**
```csharp
public sealed record AdminUserDto(
    string Id,           // Keycloak user ID
    string Email,
    string FullName,
    bool IsEnabled,
    bool HasTemporaryPassword);  // UPDATE_PASSWORD requiredAction present
```

**Nova Query:**
```csharp
public sealed record GetAdministratorsQuery();
public sealed class GetAdministratorsQueryHandler : IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>>
```

### Audit — ActionType para ações de usuário

Handlers migrados precisam mapear ações para o enum `ActionType` existente:

| Handler | ActionType |
|---------|-----------|
| BlockUserCommandHandler | `UserBlocked` (10) |
| UnblockUserCommandHandler | `UserUnblocked` (11) |
| DeleteUserCommandHandler | `UserDeleted` (12) |
| UpdateUserCommandHandler | `UserUpdated` (13) |
| CreateAdminCommandHandler | `AdminCreated` (1) |

Os TargetUserId e TargetUserName devem ser preenchidos nos handlers que já têm o UserId do target.

### Claude's Discretion

- Estratégia de rollback se Keycloak falhar durante criação de admin (já implementada no CreateAdminCommandHandler, não alterar)
- Validação FluentValidation para CreateAdminCommand (já existe, não alterar)
- Paginação do GetAdministratorsQuery (não necessária — lista de admins é pequena, retornar tudo)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Domínio e Infraestrutura
- `src/Onboarding.Domain/Aggregates/Audit/AdminAuditLog.cs` — Entidade append-only, factory method, campos disponíveis
- `src/Onboarding.Domain/Aggregates/Audit/AuditLog.cs` — Entidade legada a ser removida
- `src/Onboarding.Domain/Aggregates/Audit/ActionType.cs` — Enum de tipos de ação (AdminCreated=1..UserUpdated=13)
- `src/Onboarding.Domain/Repositories/IAuditLogRepository.cs` — Interfaces a serem substituídas por IAuditService

### Application Layer
- `src/Onboarding.Application/Common/IKeycloakUserService.cs` — Interface a ser estendida com GetUsersByRoleAsync
- `src/Onboarding.Application/Admin/Commands/CreateAdminCommand.cs` — Handler de referência para padrão CQRS + audit
- `src/Onboarding.Application/Admin/Commands/BlockUserCommand.cs` — Handler a ser migrado de IAuditLogRepository → IAuditService
- `src/Onboarding.Application/Admin/Commands/UnblockUserCommand.cs` — Handler a ser migrado
- `src/Onboarding.Application/Admin/Commands/UpdateUserCommand.cs` — Handler a ser migrado
- `src/Onboarding.Application/Admin/Commands/DeleteUserCommand.cs` — Handler a ser migrado
- `src/Onboarding.Application/Admin/Queries/GetAuditLogQuery.cs` — Query existente (referência para GetAdministratorsQuery)
- `src/Onboarding.Application/DependencyInjection.cs` — Registrar IAuditService aqui

### API Controller
- `src/Onboarding.API/Controllers/AdminUserController.cs` — Controller a ser atualizado (renomear rota + adicionar GET /administrators)

### Infrastructure
- `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs` — Implementação a ser estendida
- `src/Onboarding.Infrastructure/Persistence/Migrations/` — EF Core migrations (nova migration para dropar AuditLogs)
- `src/Onboarding.Infrastructure/DependencyInjection.cs` — Registrar implementação de IAuditService

### Frontend (atualização necessária)
- `frontend/backoffice/src/lib/admin-api.ts` — Linha ~324: função `createAdmin` chama `/api/admin/users` → atualizar para `/api/admin/administrators`

### Plano de Referência
- `.planning/phases/16-admin-api-endpoints/16-CONTEXT.md` — Decisões da API admin original
- `.planning/ROADMAP.md` — Success criteria da Phase 30

</canonical_refs>

<specifics>
## Specific Ideas

- `HasTemporaryPassword` no AdminUserDto: derivar da presença de `UPDATE_PASSWORD` nos `requiredActions` do usuário Keycloak — `IKeycloakUserService.GetUsersByRoleAsync` deve incluir essa informação via `GET /admin/realms/onboarding/roles/admin/users`
- Keycloak API endpoint de listagem por role retorna `UserRepresentation` com campos `id`, `username`, `email`, `firstName`, `lastName`, `enabled`, `requiredActions` — mapear esses campos para `AdminUserDto`
- Se Keycloak não retornar `email` no `UserRepresentation` (campo pode ser null), usar `username` como fallback

</specifics>

<deferred>
## Deferred Ideas

- Paginação do GET /api/admin/administrators — lista de admins é pequena, retornar tudo por agora
- Filtro por status (enabled/disabled) no GET /api/admin/administrators — frontend filtrará client-side (Phase 32)
- Audit log de ações de LEITURA (GET requests) — fora do escopo v5.0

</deferred>

---

*Phase: 30-audit-log-admin-backend*
*Context gathered: 2026-04-15 via /gsd-discuss-phase 30*
