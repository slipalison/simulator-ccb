# Phase 30: Audit Log Backend + Admin Management Backend — Research

**Researched:** 2026-04-15
**Domain:** .NET 10 / ASP.NET Core / EF Core / Keycloak Admin API — refatoração de audit log + novo endpoint GET /api/admin/administrators
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

1. **Unificação de Audit Logs:** Criar `IAuditService` como abstração única. Implementação injeta `IAdminAuditLogRepository`, cria `AdminAuditLog.Create(...)`, chama `AddAsync + SaveChangesAsync`. Migrar todos os handlers que ainda usam `IAuditLogRepository + AuditLog` para `IAuditService`.

2. **Remoção da entidade legada:** Remover `AuditLog` entity, `IAuditLogRepository`, `AuditLogRepository`, `AuditLogConfiguration`, `AuditActions` class. Criar nova EF Core migration que dropa a tabela `audit_logs`.

3. **Rota:** `POST /api/admin/users` (CreateAdmin) renomear para `POST /api/admin/administrators`. `GET /api/admin/users` (clientes paginados) NÃO é afetado.

4. **Interface IAuditService definida em CONTEXT.md:**
   ```csharp
   Task RecordAsync(string actorSub, string actorEmail, ActionType action,
       Guid? targetUserId = null, string? targetUserName = null,
       string? details = null, string? ipAddress = null, CancellationToken ct = default);
   ```

5. **GET /api/admin/administrators:** Fonte de dados = Keycloak direto. Endpoint Keycloak: `GET /admin/realms/onboarding/roles/admin/users`. Retorna TODOS (ativos + bloqueados). Sem paginação. Ordenação: como Keycloak retornar.

6. **Novo método em IKeycloakUserService:**
   ```csharp
   Task<IReadOnlyList<AdminUserDto>> GetUsersByRoleAsync(string roleName, CancellationToken ct = default);
   ```

7. **AdminUserDto definido em CONTEXT.md:**
   ```csharp
   public sealed record AdminUserDto(string Id, string Email, string FullName,
       bool IsEnabled, bool HasTemporaryPassword);
   ```
   `HasTemporaryPassword` = presença de `UPDATE_PASSWORD` nos `requiredActions` do Keycloak.

8. **Nova Query:**
   ```csharp
   public sealed record GetAdministratorsQuery();
   public sealed class GetAdministratorsQueryHandler : IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>>
   ```

9. **Mapeamento ActionType para handlers migrados:**
   | Handler | ActionType |
   |---------|-----------|
   | BlockUserCommandHandler | `UserBlocked` (10) |
   | UnblockUserCommandHandler | `UserUnblocked` (11) |
   | DeleteUserCommandHandler | `UserDeleted` (12) |
   | UpdateUserCommandHandler | `UserUpdated` (13) |
   | CreateAdminCommandHandler | `AdminCreated` (1) |

10. **Frontend:** atualizar `frontend/backoffice/src/lib/admin-api.ts` linha ~324: `/api/admin/users` → `/api/admin/administrators` na função `createAdmin`.

### Claude's Discretion

- Estratégia de rollback se Keycloak falhar durante criação de admin (já implementada, não alterar)
- Validação FluentValidation para CreateAdminCommand (já existe, não alterar)
- Paginação do GetAdministratorsQuery (não necessária — lista de admins é pequena)

### Deferred Ideas (OUT OF SCOPE)

- Paginação do GET /api/admin/administrators
- Filtro por status (enabled/disabled) no GET /api/admin/administrators
- Audit log de ações de LEITURA (GET requests)

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| AUD-01 | All admin actions recorded append-only (actor, action, target, timestamp, details JSON) | `AdminAuditLog` + `IAdminAuditLogRepository` já existem. `IAuditService` unifica os 5 handlers restantes que usam `AuditLog` legado. Tabela `admin_audit_logs` já está no schema. |
| ADM-01 | Admin can create new administrator (name + email) in backoffice | `CreateAdminCommand` já implementado. Só precisa renomear rota `POST /api/admin/users` → `POST /api/admin/administrators`. |
| ADM-02 | System generates temporary password displayed once to creator | `GenerateTemporaryPassword()` já implementado em `CreateAdminCommandHandler`. Senha retornada em `CreateAdminResult`. |
| ADM-03 | New admin gets role "admin" + UPDATE_PASSWORD requiredAction in Keycloak via Admin API | `CreateAdminUserAsync` já faz isso via `KeycloakUserService`. |
| ADM-04 | Admin can list other administrators in backoffice | Requer nova `GetAdministratorsQuery` + `IKeycloakUserService.GetUsersByRoleAsync` + `GET /api/admin/administrators`. Nada disso existe ainda. |

</phase_requirements>

---

## Summary

Esta fase é predominantemente uma operação de **refatoração + extensão incremental** sobre código que já existe. O trabalho não é greenfield: a entidade `AdminAuditLog`, sua migration `AddAdminAuditLog`, o `IAdminAuditLogRepository`, o `CreateAdminCommandHandler` (com geração de senha temporária) e os handlers de block/unblock/update/delete já estão funcionando. O que falta é (1) unificar os dois sistemas de audit log via `IAuditService`, (2) remover o sistema legado `AuditLog`, (3) adicionar `GET /api/admin/administrators` chamando o Keycloak, e (4) renomear a rota do CreateAdmin.

O risco técnico mais relevante é a remoção da entidade `AuditLog` legada: ela está referenciada em 4 handlers, no `AppDbContext`, em `AuditLogConfiguration`, em `AuditLogRepository`, em `IAuditLogRepository`, na classe `AuditActions`, e nos testes de integração `AdminFullFlowTests` + `AdminTestFactory`. Todos esses pontos precisam ser atualizados atomicamente. A migration que dropa `audit_logs` deve garantir que a tabela está vazia em dev antes de executar.

O segundo ponto de risco é o `AdminFullFlowTests`: esse teste verifica o audit trail usando `_factory.AuditLogRepositoryMock.ReceivedCalls()` com `AuditLog` legado (linhas 128-136). Após a migração para `IAuditService`, o mock precisa ser substituído por `IAuditService` (ou `IAdminAuditLogRepository`) e as asserções atualizadas para verificar `ActionType` em vez de strings `AuditActions.*`.

**Recomendação principal:** Executar as mudanças em ordem sequencial dentro de um único plano — primeiro criar `IAuditService`, depois migrar handlers um a um, depois remover o legado, depois adicionar o novo endpoint. Não tentar fazer tudo em paralelo porque o compilador vai guiar a remoção de cada dependência.

---

## Standard Stack

### Core (já no projeto — verificado no codebase)

| Componente | Versão | Papel nesta fase |
|-----------|--------|-----------------|
| .NET 10 / ASP.NET Core Controllers | 10.0 | Runtime e framework HTTP |
| Entity Framework Core + Npgsql | 10.0 | Migration para dropar `audit_logs` |
| Keycloak.AuthServices.Sdk | 2.7.x | `IKeycloakUserClient` já usado em `KeycloakUserService` |
| `HttpClient` "keycloak-admin-api" | — | Named client com token handler, já configurado em `InfrastructureServiceExtensions` |
| xUnit + Shouldly + NSubstitute | 2.9.x / 4.x / 5.x | Testes unitários e de integração |

### Sem novos pacotes necessários

Toda a funcionalidade desta fase pode ser implementada com o stack já instalado. Nenhum novo NuGet é necessário. [VERIFIED: leitura direta do codebase]

---

## Architecture Patterns

### Estrutura de arquivos a criar/modificar

```
src/
├── Onboarding.Domain/
│   ├── Aggregates/Audit/
│   │   └── AuditLog.cs                   ← REMOVER
│   ├── Common/
│   │   └── AuditActions.cs               ← REMOVER
│   └── Repositories/
│       └── IAuditLogRepository.cs        ← remover IAuditLogRepository (manter IAdminAuditLogRepository)
│
├── Onboarding.Application/
│   ├── Common/
│   │   ├── IAuditService.cs              ← CRIAR (nova interface + coloca AdminUserDto aqui)
│   │   └── IKeycloakUserService.cs       ← adicionar GetUsersByRoleAsync
│   ├── Admin/
│   │   ├── Commands/
│   │   │   ├── BlockUserCommand.cs       ← migrar IAuditLogRepository → IAuditService
│   │   │   ├── UnblockUserCommand.cs     ← migrar
│   │   │   ├── UpdateUserCommand.cs      ← migrar
│   │   │   ├── DeleteUserCommand.cs      ← migrar
│   │   │   └── CreateAdminCommand.cs     ← migrar IAdminAuditLogRepository → IAuditService
│   │   └── Queries/
│   │       └── GetAdministratorsQuery.cs ← CRIAR
│   └── DependencyInjection.cs            ← registrar IAuditService + GetAdministratorsQueryHandler
│
├── Onboarding.Infrastructure/
│   ├── Keycloak/
│   │   └── KeycloakUserService.cs        ← implementar GetUsersByRoleAsync
│   ├── Persistence/
│   │   ├── AppDbContext.cs               ← remover DbSet<AuditLog> + ApplyConfiguration(AuditLogConfiguration)
│   │   └── Configurations/
│   │       └── AuditLogConfiguration.cs  ← REMOVER
│   ├── Repositories/
│   │   └── AuditLogRepository.cs         ← REMOVER
│   ├── Services/
│   │   └── AuditService.cs               ← CRIAR (implementação de IAuditService)
│   ├── DependencyInjection.cs            ← remover IAuditLogRepository + AuditLogRepository; registrar IAuditService
│   └── Persistence/Migrations/
│       └── XXXXXXXX_DropAuditLogs.cs     ← CRIAR via `dotnet ef migrations add`
│
└── Onboarding.API/
    └── Controllers/
        └── AdminUserController.cs        ← renomear rota POST + adicionar GET /administrators

frontend/
└── backoffice/src/lib/
    └── admin-api.ts                      ← atualizar URL linha ~324
```

### Pattern 1: IAuditService como Facade sobre IAdminAuditLogRepository

**O que é:** `IAuditService` é uma abstração fina que encapsula a criação de `AdminAuditLog.Create(...)` + persistência. Handlers injetam `IAuditService` em vez de manipular o repositório diretamente.

**Por que:** Centraliza a lógica de criação do log — se o schema de `AdminAuditLog.Create` mudar no futuro, só a implementação de `AuditService` precisa ser atualizada.

**Assinatura conforme CONTEXT.md:**
```csharp
// src/Onboarding.Application/Common/IAuditService.cs
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

**Implementação:**
```csharp
// src/Onboarding.Infrastructure/Services/AuditService.cs
public sealed class AuditService : IAuditService
{
    private readonly IAdminAuditLogRepository _repo;
    public AuditService(IAdminAuditLogRepository repo) => _repo = repo;

    public async Task RecordAsync(string actorSub, string actorEmail, ActionType action,
        Guid? targetUserId = null, string? targetUserName = null,
        string? details = null, string? ipAddress = null, CancellationToken ct = default)
    {
        // actorSub é o Keycloak user ID (Guid string); parse com fallback
        var adminId = Guid.TryParse(actorSub, out var parsed) ? parsed : Guid.Empty;
        var log = AdminAuditLog.Create(adminId, actorEmail, action, targetUserId, targetUserName, details, ipAddress);
        await _repo.AddAsync(log, ct);
        await _repo.SaveChangesAsync(ct);
    }
}
```

**Atenção:** `AdminAuditLog.Create` recebe `adminUserId: Guid` e `adminUserName: string`. Os handlers atuais passam `AdminSub` (string) e `AdminEmail` (string). O `actorSub` pode ser um Guid (sub do JWT) ou email. A implementação deve fazer `Guid.TryParse(actorSub, ...)` e usar o email como `adminUserName` (consistente com o que os handlers já fazem). [VERIFIED: leitura de AdminAuditLog.cs e CreateAdminCommand.cs]

### Pattern 2: Migração dos Handlers — Trocar IAuditLogRepository por IAuditService

**Padrão antes (BlockUserCommand exemplo):**
```csharp
// Injeção atual
private readonly IAuditLogRepository _auditLogRepository;

// Uso atual
var auditLog = AuditLog.Create(command.AdminSub, command.AdminEmail,
    AuditActions.UserBlocked, command.UserId, ...);
await _auditLogRepository.AddAsync(auditLog, ct);
await _auditLogRepository.SaveChangesAsync(ct);
```

**Padrão depois:**
```csharp
// Nova injeção
private readonly IAuditService _auditService;

// Novo uso
await _auditService.RecordAsync(
    actorSub: command.AdminSub,
    actorEmail: command.AdminEmail,
    action: ActionType.UserBlocked,
    targetUserId: command.UserId,
    targetUserName: client.Email.Value,  // ou client.Name se disponível
    ct: ct);
```

**Atenção `UpdateUserCommand`:** O handler atual persiste `SnapshotBefore`/`SnapshotAfter` no `AuditLog` legado (campos que não existem em `AdminAuditLog`). Em `AdminAuditLog`, o campo é `Details` (jsonb). A migration para este handler deve serializar as informações relevantes no campo `details` como JSON string. [VERIFIED: leitura de UpdateUserCommand.cs e AdminAuditLog.cs]

**Atenção `DeleteUserCommand`:** Mesmo cenário — `SnapshotBefore`/`SnapshotAfter` devem ser consolidados em `details`. [VERIFIED: leitura de DeleteUserCommand.cs]

### Pattern 3: GetUsersByRoleAsync — Keycloak API direta

**Endpoint Keycloak:** `GET /admin/realms/{realm}/roles/{roleName}/users`

**Implementação via `_adminHttpClient` (named client "keycloak-admin-api" já configurado):**
```csharp
// src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs
public async Task<IReadOnlyList<AdminUserDto>> GetUsersByRoleAsync(
    string roleName, CancellationToken ct = default)
{
    var response = await _adminHttpClient.GetAsync(
        $"admin/realms/{_realm}/roles/{roleName}/users", ct);
    response.EnsureSuccessStatusCode();

    var users = await response.Content.ReadFromJsonAsync<List<KeycloakUserRepresentation>>(ct)
        ?? [];

    return users.Select(u => new AdminUserDto(
        Id: u.Id ?? string.Empty,
        Email: u.Email ?? u.Username ?? string.Empty,
        FullName: BuildFullName(u.FirstName, u.LastName),
        IsEnabled: u.Enabled ?? true,
        HasTemporaryPassword: u.RequiredActions?.Contains("UPDATE_PASSWORD") ?? false
    )).ToList().AsReadOnly();
}

private static string BuildFullName(string? firstName, string? lastName)
    => string.Join(" ", new[] { firstName, lastName }
        .Where(s => !string.IsNullOrWhiteSpace(s) && s != "-")).Trim();
```

**Classe de deserialização interna (pode ser record privado no KeycloakUserService):**
```csharp
private sealed record KeycloakUserRepresentation(
    string? Id,
    string? Username,
    string? Email,
    string? FirstName,
    string? LastName,
    bool? Enabled,
    [property: JsonPropertyName("requiredActions")] List<string>? RequiredActions);
```

**Nota:** O `_adminHttpClient` já tem token handler automático via `AddClientCredentialsTokenHandler`. [VERIFIED: leitura de InfrastructureServiceExtensions.cs]

### Pattern 4: GetAdministratorsQuery — Nova Query Handler

```csharp
// src/Onboarding.Application/Admin/Queries/GetAdministratorsQuery.cs
public sealed record GetAdministratorsQuery();

public sealed class GetAdministratorsQueryHandler
    : IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>>
{
    private readonly IKeycloakUserService _keycloakUserService;

    public GetAdministratorsQueryHandler(IKeycloakUserService keycloakUserService)
        => _keycloakUserService = keycloakUserService;

    public async Task<IReadOnlyList<AdminUserDto>> HandleAsync(
        GetAdministratorsQuery query, CancellationToken ct = default)
        => await _keycloakUserService.GetUsersByRoleAsync("admin", ct);
}
```

### Pattern 5: Atualização do AdminUserController

**Mudanças necessárias no controller:**

1. Adicionar `IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>> _administratorsHandler` ao construtor.
2. Remover o action `[HttpPost("users")]` (CreateAdmin — rota atual).
3. Adicionar `[HttpPost("administrators")]` com a mesma lógica.
4. Adicionar `[HttpGet("administrators")]` novo:

```csharp
[HttpGet("administrators")]
[ProducesResponseType(typeof(IReadOnlyList<AdminUserDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetAdministrators(CancellationToken ct = default)
{
    var result = await _administratorsHandler.HandleAsync(new GetAdministratorsQuery(), ct);
    return Ok(result);
}
```

### Pattern 6: EF Core Migration para dropar audit_logs

**Comando:**
```bash
cd src/Onboarding.API
dotnet ef migrations add DropAuditLogs --project ../Onboarding.Infrastructure
```

**Conteúdo gerado (deve ser verificado antes de aplicar):**
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropTable(name: "audit_logs");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // Recriar a tabela se necessário para rollback
    migrationBuilder.CreateTable(
        name: "audit_logs",
        columns: table => new { ... });
}
```

**Pré-condição:** `audit_logs` deve estar vazia em dev antes de rodar `dotnet ef database update`. Se houver dados, a migration falhará ou perderá registros.

**Ordem importante:** A migration deve ser adicionada APÓS a remoção de `AuditLogConfiguration` e `DbSet<AuditLog>` do `AppDbContext`, caso contrário o EF Core tentará reconciliar a entidade removida com o snapshot.

### Anti-Patterns a Evitar

- **Não criar IAuditService em Application.Common como classe concreta** — a implementação fica em Infrastructure (acessa repositório EF Core). A interface em Application, a implementação em Infrastructure.
- **Não fazer SaveChangesAsync duplo** — A implementação de `AuditService` chama `SaveChangesAsync` internamente. Os handlers não devem chamar novamente. Em handlers que já chamam `SaveChangesAsync` para persistir dados de domínio, o audit pode ser emitido depois (dois SaveChanges são aceitáveis — audit não precisa ser atômico com a operação principal neste projeto).
- **Não remover IAdminAuditLogRepository** — ela continua sendo usada por `AuditService`, `GetAuditLogQueryHandler`, e pode ser necessária em testes de integração.
- **Não esquecer de remover `using Onboarding.Domain.Aggregates.Audit` (AuditLog)** dos handlers após migrar — o compilador vai apontar os restantes.

---

## Don't Hand-Roll

| Problema | Não construir | Usar em vez disso | Por que |
|---------|---------------|-------------------|---------|
| Listagem de usuários por role no Keycloak | Query manual na tabela app_db | `GET /admin/realms/{realm}/roles/{roleName}/users` via `_adminHttpClient` | Keycloak é a fonte de verdade para roles |
| Token para chamar Keycloak Admin API | Gerenciar CC grant manualmente | `AddClientCredentialsTokenHandler` (Duende) já configurado no `keycloak-admin-api` named client | Reutilizar infra existente |
| Migration SQL manual | Script SQL direto | `dotnet ef migrations add` | Mantém snapshot do EF Core sincronizado |

---

## Common Pitfalls

### Pitfall 1: AdminAuditLog.Create recebe Guid, handlers passam string

**O que pode dar errado:** `AdminAuditLog.Create` recebe `adminUserId: Guid`. Os handlers de block/unblock/update/delete passam `command.AdminSub` que é `string` (pode ser o sub do JWT = Guid, ou email, ou preferred_username). Se `Guid.TryParse` falhar, o `adminUserId` ficará `Guid.Empty` no log.

**Como evitar:** Na implementação de `AuditService.RecordAsync`, fazer `Guid.TryParse(actorSub, out var parsed)` e usar `parsed` se válido, `Guid.Empty` como fallback explícito e documentado. Não lançar exceção — audit não deve quebrar a operação principal.

**Verificação:** `CreateAdminCommandHandler` já usa esta lógica como referência (linhas 49-62).

### Pitfall 2: AdminFullFlowTests usa IAuditLogRepository (legado) — vai quebrar

**O que vai dar errado:** `AdminTestFactory` (linha 28) registra `IAuditLogRepository AuditLogRepositoryMock`. `AdminFullFlowTests` (linhas 128-136) verifica `_factory.AuditLogRepositoryMock.ReceivedCalls()` com `AuditLog` legado e strings `"USER_BLOCKED"` etc.

**Após a migração:** Os handlers não mais chamam `IAuditLogRepository`. O mock existente nunca receberá chamadas. As asserções vão falhar silenciosamente (lista vazia).

**Como corrigir:**
1. Remover `IAuditLogRepository AuditLogRepositoryMock` do `AdminTestFactory`.
2. Adicionar `IAuditService AuditServiceMock = Substitute.For<IAuditService>()`.
3. Registrar `services.AddScoped<IAuditService>(_ => AuditServiceMock)` no `ConfigureTestServices`.
4. Atualizar asserções em `AdminFullFlowTests` para verificar chamadas em `AuditServiceMock.RecordAsync(...)` com os `ActionType` corretos.

### Pitfall 3: Remoção parcial de AuditLog — build quebra em cascata

**O que vai dar errado:** Se remover `AuditLog` entity antes de atualizar todos os handlers + AppDbContext + configurações + migrations snapshot, o projeto não vai compilar.

**Como evitar:** Seguir a ordem:
1. Criar `IAuditService` (compilation-safe, nada referencia ainda)
2. Migrar handlers um a um (remover `using AuditLog`, atualizar para `IAuditService`)
3. Remover `IAuditLogRepository` (só depois que nenhum handler referencia)
4. Remover `AuditLog` entity, `AuditLogRepository`, `AuditLogConfiguration`
5. Atualizar `AppDbContext` (remover `DbSet<AuditLog>` e `ApplyConfiguration(new AuditLogConfiguration())`)
6. Adicionar EF Core migration `DropAuditLogs`
7. Remover `AuditActions` class (só depois que nenhum handler referencia)

### Pitfall 4: LastName="-" no CreateAdminUserAsync afeta FullName no AdminUserDto

**O que vai dar errado:** `CreateAdminUserAsync` cria usuários Keycloak com `LastName = "-"` (placeholder para Keycloak 26.x User Profile). Ao montar `FullName` no `GetUsersByRoleAsync`, o resultado seria `"Nome Admin -"`.

**Como evitar:** No `BuildFullName`, ignorar strings que são apenas `"-"` ou whitespace. Exemplo:
```csharp
private static string BuildFullName(string? firstName, string? lastName)
    => string.Join(" ", new[] { firstName, lastName }
        .Where(s => !string.IsNullOrWhiteSpace(s) && s != "-")).Trim();
```

### Pitfall 5: IAdminAuditLogRepository.SaveChangesAsync faz commit duplo em handlers que já persistem dados

**O que vai dar errado:** `UpdateUserCommandHandler` chama `await _adminRepository.SaveChangesAsync(ct)` para persistir o cliente atualizado. Depois, `AuditService.RecordAsync` chama outro `SaveChangesAsync`. Isso resulta em dois commits — o segundo inclui o `AdminAuditLog` adicionado.

**Isso não é um problema:** Dois commits sequenciais no mesmo DbContext/DbContextScope são seguros. O audit pode ser emitido após a operação principal. Não é necessário envolver os dois em uma transaction para este projeto (sem requisito de atomicidade explícita nos CONTEXT.md decisions). [ASSUMED — decisão de design aceitável dado o escopo, mas pode ser alterada se necessário]

### Pitfall 6: Keycloak GET roles/{role}/users pode retornar lista vazia se role não existe

**O que vai dar errado:** Se a role "admin" não existir no realm `onboarding` (ambiente novo/test), a chamada `GET /admin/realms/onboarding/roles/admin/users` retorna 404, não lista vazia.

**Como evitar:** Em `GetUsersByRoleAsync`, tratar 404 retornando lista vazia em vez de propagar exceção:
```csharp
if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    return Array.Empty<AdminUserDto>();
response.EnsureSuccessStatusCode();
```

---

## Code Examples

### Exemplo verificado: AssignAdminRoleAsync já usa _adminHttpClient

O padrão de chamada direta ao Keycloak Admin API via `_adminHttpClient` já está estabelecido em `KeycloakUserService` (linhas 261-302). `GetUsersByRoleAsync` seguirá o mesmo padrão. [VERIFIED: leitura de KeycloakUserService.cs]

### Exemplo verificado: GetUserByIdAsync retorna RequiredActions

`GetUserByIdAsync` já mapeia `user.RequiredActions` para `KeycloakUserDetails.RequiredActions`. A nova `GetUsersByRoleAsync` precisa incluir os `requiredActions` da resposta Keycloak para popular `HasTemporaryPassword`. [VERIFIED: leitura de KeycloakUserService.cs linhas 221-232]

### Exemplo verificado: IQueryHandler pattern

`GetAuditLogQueryHandler` é o template para `GetAdministratorsQueryHandler` — mesma estrutura `IQueryHandler<TQuery, TResult>`. [VERIFIED: leitura de GetAuditLogQuery.cs]

---

## Runtime State Inventory

Esta fase inclui remoção de tabela no banco de dados (`audit_logs`).

| Categoria | Itens encontrados | Ação necessária |
|-----------|------------------|-----------------|
| Stored data | Tabela `audit_logs` em app_db — contém registros de ações de block/unblock/update/delete desde que os handlers foram ativados | Migration `DropAuditLogs` — verificar que está VAZIA em dev antes de executar `dotnet ef database update` |
| Live service config | Nenhum serviço externo referencia `audit_logs` diretamente | Nenhuma |
| OS-registered state | Nenhum | Nenhum |
| Secrets/env vars | Nenhum afetado por esta fase | Nenhuma |
| Build artifacts | EF Core snapshot `AppDbContextModelSnapshot.cs` contém `AuditLog` entity — será atualizado automaticamente pela nova migration | `dotnet ef migrations add DropAuditLogs` regenera o snapshot |

**Atenção crítica:** Se a tabela `audit_logs` contiver registros em dev (handlers de block/unblock/update/delete foram executados), a migration `DropAuditLogs` vai dropar esses dados. Verificar com `SELECT COUNT(*) FROM audit_logs` antes de aplicar.

---

## Validation Architecture

### Test Framework

| Propriedade | Valor |
|------------|-------|
| Framework | xUnit 2.9.x + Shouldly 4.x + NSubstitute 5.x |
| Config file | `tests/Onboarding.API.Tests/xunit.runner.json` (inferido) |
| Quick run command | `dotnet test tests/Onboarding.API.Tests --filter "Category=Unit" -x` |
| Full suite command | `dotnet test tests/ --no-build` |

### Phase Requirements → Test Map

| Req ID | Comportamento | Tipo de Teste | Comando automatizado | Arquivo existe? |
|--------|--------------|---------------|---------------------|-----------------|
| AUD-01 | Handlers migrados chamam IAuditService.RecordAsync com ActionType correto | Unit | `dotnet test tests/Onboarding.API.Tests --filter "FullyQualifiedName~AuditService"` | ❌ Wave 0 |
| AUD-01 | IAuditService.RecordAsync cria AdminAuditLog e persiste via IAdminAuditLogRepository | Unit | `dotnet test tests/Onboarding.API.Tests --filter "FullyQualifiedName~AuditServiceTests"` | ❌ Wave 0 |
| ADM-01 | POST /api/admin/administrators retorna 201 com senha temporária | Integration | `dotnet test tests/Onboarding.API.Tests --filter "FullyQualifiedName~CreateAdmin"` | ❌ Wave 0 (rota existente — só renomear) |
| ADM-01 | POST /api/admin/users retorna 404 após renomeação | Integration | `dotnet test tests/Onboarding.API.Tests --filter "FullyQualifiedName~OldRouteGone"` | ❌ Wave 0 |
| ADM-04 | GET /api/admin/administrators retorna lista de admins | Integration | `dotnet test tests/Onboarding.API.Tests --filter "FullyQualifiedName~GetAdministrators"` | ❌ Wave 0 |
| ADM-04 | GET /api/admin/administrators retorna 403 para não-admin | Integration | `dotnet test tests/Onboarding.API.Tests --filter "FullyQualifiedName~GetAdministrators"` | ❌ Wave 0 |
| AUD-01 | AdminFullFlowTests atualizado — verifica IAuditService em vez de IAuditLogRepository | Integration | `dotnet test tests/Onboarding.API.Tests --filter "FullyQualifiedName~AdminFullFlowTests"` | ✅ (existente, precisa ser atualizado) |

### Sampling Rate

- **Por task commit:** `dotnet test tests/Onboarding.API.Tests -x --no-build`
- **Por wave merge:** `dotnet test tests/ --no-build`
- **Phase gate:** Todos os 149 testes passando (baseline atual) + novos testes verdes

### Wave 0 Gaps

- [ ] `tests/Onboarding.API.Tests/Admin/AuditServiceTests.cs` — cobre AUD-01 (unit: RecordAsync cria AdminAuditLog correto)
- [ ] `tests/Onboarding.API.Tests/Admin/GetAdministratorsTests.cs` — cobre ADM-04 (integration: GET /api/admin/administrators)
- [ ] `tests/Onboarding.API.Tests/Admin/AdminTestFactory.cs` — atualizar: remover `AuditLogRepositoryMock`, adicionar `AuditServiceMock`
- [ ] `tests/Onboarding.API.Tests/Admin/AdminFullFlowTests.cs` — atualizar asserções de audit para usar `AuditServiceMock`

---

## Environment Availability

Step 2.6: Esta fase é puramente código/config do backend + uma migration EF Core. As dependências externas (PostgreSQL, Keycloak) já estão disponíveis no Docker Compose e verificadas em fases anteriores.

| Dependência | Requerida por | Disponível | Versão | Fallback |
|------------|--------------|-----------|--------|---------|
| PostgreSQL (app_db) | EF Core migration | ✓ | 16-alpine (compose) | — |
| Keycloak | `GetUsersByRoleAsync` | ✓ | 26.1 (compose) | — |
| dotnet ef tools | `migrations add DropAuditLogs` | ✓ | verificado via build existente | — |

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | não | — |
| V3 Session Management | não | — |
| V4 Access Control | sim | `[Authorize(Roles = "admin")]` já em `AdminUserController` — cobre ambos os novos endpoints |
| V5 Input Validation | parcial | `CreateAdminCommand` já tem FluentValidation; `GetAdministratorsQuery` não tem input externo |
| V6 Cryptography | não aplicável | Senha temporária usa `RandomNumberGenerator` (já implementado) |

### Threat Patterns

| Pattern | STRIDE | Mitigação padrão |
|---------|--------|-----------------|
| Acesso não-admin a /administrators | Elevation of Privilege | `[Authorize(Roles = "admin")]` + teste 403 |
| Senha temporária logada | Information Disclosure | `GenerateTemporaryPassword` só retorna em `CreateAdminResult`, nunca é logada (já implementado) |
| Audit log mutável | Tampering | `AdminAuditLog` sem métodos Update/Delete; tabela sem UPDATE/DELETE statements; pattern append-only verificado no codebase |

---

## Assumptions Log

| # | Claim | Seção | Risco se errado |
|---|-------|-------|----------------|
| A1 | Dois `SaveChangesAsync` sequenciais (um para a operação, um para o audit) são aceitáveis sem envolver uma transaction | Common Pitfalls #5 | Se o segundo commit falhar, a operação foi persistida mas o audit não — registros de audit faltando |
| A2 | A tabela `audit_logs` está vazia em dev (nenhum dado precisa ser migrado para `admin_audit_logs`) | Runtime State Inventory | Se houver registros valiosos, eles serão perdidos na migration — investigar antes de executar |

---

## Open Questions

1. **Dados em `audit_logs` em dev**
   - O que sabemos: A tabela existe e os handlers legados já escrevem nela desde que foram ativados
   - O que não sabemos: Se há registros reais que precisam ser preservados
   - Recomendação: Executar `SELECT COUNT(*) FROM audit_logs` antes de criar a migration. Se > 0, decidir se os dados precisam ser migrados manualmente para `admin_audit_logs` ou simplesmente descartados (ambiente dev)

2. **`AdminFullFlowTests` referencia `AuditLog` legado nas asserções**
   - O que sabemos: Linhas 128-136 verificam `AuditLog` objects e strings `"USER_BLOCKED"` etc.
   - O que não sabemos: Se há outros testes além de `AdminFullFlowTests` que referenciam `IAuditLogRepository` ou `AuditLog`
   - Recomendação: Fazer grep em `tests/` por `AuditLogRepository`, `AuditLog`, `AuditActions` antes de remover — já identificado que `AdminTestFactory` e `AdminFullFlowTests` precisam de atualização; pode haver outros

---

## Sources

### Primary (HIGH confidence — leitura direta do codebase)

- `src/Onboarding.Domain/Aggregates/AdminAuditLog.cs` — campos, factory method, construtor privado
- `src/Onboarding.Domain/Aggregates/Audit/AuditLog.cs` — entidade legada a remover
- `src/Onboarding.Domain/Aggregates/Audit/ActionType.cs` — enum com valores 1-13
- `src/Onboarding.Domain/Common/AuditActions.cs` — strings legadas a remover
- `src/Onboarding.Domain/Repositories/IAuditLogRepository.cs` — ambas as interfaces
- `src/Onboarding.Application/Common/IKeycloakUserService.cs` — interface atual sem GetUsersByRoleAsync
- `src/Onboarding.Application/Admin/Commands/CreateAdminCommand.cs` — padrão de referência (já usa IAdminAuditLogRepository)
- `src/Onboarding.Application/Admin/Commands/BlockUserCommand.cs` — handler a migrar
- `src/Onboarding.Application/Admin/Commands/UnblockUserCommand.cs` — handler a migrar
- `src/Onboarding.Application/Admin/Commands/UpdateUserCommand.cs` — handler a migrar
- `src/Onboarding.Application/Admin/Commands/DeleteUserCommand.cs` — handler a migrar
- `src/Onboarding.Application/Admin/Queries/GetAuditLogQuery.cs` — padrão para GetAdministratorsQuery
- `src/Onboarding.Application/DependencyInjection.cs` — registros existentes
- `src/Onboarding.Infrastructure/DependencyInjection.cs` — registros de repositórios e Keycloak
- `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs` — implementação a estender
- `src/Onboarding.Infrastructure/Persistence/AppDbContext.cs` — DbSets atuais
- `src/Onboarding.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs` — config a remover
- `src/Onboarding.Infrastructure/Persistence/Configurations/AdminAuditLogConfiguration.cs` — config existente
- `src/Onboarding.Infrastructure/Repositories/AdminAuditLogRepository.cs` — implementação existente
- `src/Onboarding.Infrastructure/Repositories/AuditLogRepository.cs` — implementação a remover
- `src/Onboarding.Infrastructure/Persistence/Migrations/20260414191549_AddAdminAuditLog.cs` — migration existente
- `src/Onboarding.API/Controllers/AdminUserController.cs` — controller a atualizar
- `tests/Onboarding.API.Tests/Admin/AdminTestFactory.cs` — factory a atualizar
- `tests/Onboarding.API.Tests/Admin/AdminFullFlowTests.cs` — teste a atualizar
- `frontend/backoffice/src/lib/admin-api.ts` — linha ~324 a atualizar
- `.planning/config.json` — nyquist_validation: true confirmado

### Secondary (MEDIUM confidence)

- Keycloak Admin REST API docs (conhecimento de treinamento): endpoint `GET /admin/realms/{realm}/roles/{roleName}/users` retorna `UserRepresentation` com `id`, `username`, `email`, `firstName`, `lastName`, `enabled`, `requiredActions`. [ASSUMED] — padrão bem documentado mas não verificado via chamada real nesta sessão.

---

## Metadata

**Confidence breakdown:**
- Standard Stack: HIGH — verificado diretamente no codebase
- Architecture: HIGH — baseado em padrões já estabelecidos no projeto
- Pitfalls: HIGH — identificados por leitura do código que será removido/modificado
- Keycloak API contract: MEDIUM — baseado em conhecimento de treinamento + padrão consistente com outros endpoints já usados no projeto

**Research date:** 2026-04-15
**Valid until:** 2026-05-15 (stack estável, sem dependências externas novas)
