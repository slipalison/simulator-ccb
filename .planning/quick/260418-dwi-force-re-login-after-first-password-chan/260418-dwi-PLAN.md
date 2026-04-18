---
phase: quick/260418-dwi
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - src/Onboarding.Application/Common/IKeycloakUserService.cs
  - src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs
  - src/Onboarding.API/Controllers/AdminUserController.cs
  - keycloak/onboarding-realm.json
  - frontend/backoffice/auth-server.ts
  - tests/Onboarding.Domain.Tests/Application/Commands/KeycloakUserServiceFirstLoginTests.cs
  - tests/Onboarding.API.Tests/Admin/AdminFirstLoginEndpointTests.cs
autonomous: true
requirements: [ACF-02, ADM-03]
must_haves:
  truths:
    - "Novos admins criados via POST /api/admin/administrators recebem o atributo Keycloak isFirstLogin=true"
    - "O claim isFirstLogin aparece no access token emitido pelo client onboarding-backoffice"
    - "Callback /auth/callback detecta isFirstLogin=true, chama POST /api/admin/me/complete-first-login, limpa cookies e redireciona para /admin/login"
    - "POST /api/admin/me/complete-first-login zera a flag isFirstLogin do admin autenticado (idempotente)"
    - "Admins pré-existentes (sem o atributo) seguem o fluxo normal após login e caem em /admin/users"
  artifacts:
    - path: "src/Onboarding.Application/Common/IKeycloakUserService.cs"
      provides: "Assinatura ClearFirstLoginFlagAsync(string userId, CancellationToken ct)"
      contains: "ClearFirstLoginFlagAsync"
    - path: "src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs"
      provides: "Implementação ClearFirstLoginFlagAsync + CreateAdminUserAsync com Attributes[isFirstLogin]=true"
      contains: "isFirstLogin"
    - path: "src/Onboarding.API/Controllers/AdminUserController.cs"
      provides: "Endpoint POST /api/admin/me/complete-first-login"
      contains: "complete-first-login"
    - path: "keycloak/onboarding-realm.json"
      provides: "Protocol mapper isFirstLogin no client onboarding-backoffice (oidc-usermodel-attribute-mapper)"
      contains: "isFirstLogin"
    - path: "frontend/backoffice/auth-server.ts"
      provides: "Callback detecta claim isFirstLogin e força re-login"
      contains: "isFirstLogin"
    - path: "tests/Onboarding.Domain.Tests/Application/Commands/KeycloakUserServiceFirstLoginTests.cs"
      provides: "Unit test do fluxo de ClearFirstLoginFlagAsync (via IKeycloakUserService mock)"
    - path: "tests/Onboarding.API.Tests/Admin/AdminFirstLoginEndpointTests.cs"
      provides: "Integration test do endpoint POST /api/admin/me/complete-first-login"
  key_links:
    - from: "frontend/backoffice/auth-server.ts /callback"
      to: "POST /api/admin/me/complete-first-login"
      via: "fetch com Authorization: Bearer <access_token>"
      pattern: "complete-first-login"
    - from: "src/Onboarding.API/Controllers/AdminUserController.cs"
      to: "IKeycloakUserService.ClearFirstLoginFlagAsync"
      via: "DI injection"
      pattern: "ClearFirstLoginFlagAsync"
    - from: "keycloak/onboarding-realm.json protocolMappers"
      to: "access token claim isFirstLogin"
      via: "oidc-usermodel-attribute-mapper no client onboarding-backoffice"
      pattern: "isFirstLogin"
---

<objective>
Forçar re-login do admin após a primeira troca de senha (UPDATE_PASSWORD concluído pelo Keycloak durante o ACF+PKCE).

Purpose: Higienizar a sessão iniciada com senha temporária — hoje o admin cai logado em `/admin/users` com os tokens derivados da credencial temporária. A higiene é: marcar o admin com `isFirstLogin=true` na criação, detectar o claim no callback do Vinxi, chamar o backend para limpar a flag (`POST /api/admin/me/complete-first-login`), limpar os cookies locais (`backoffice_access_token`, `backoffice_refresh_token`, e os cookies PKCE remanescentes) e redirecionar para `/admin/login` para que o admin re-entre com a senha definitiva.

Output:
- Flag `isFirstLogin` em atributo de usuário Keycloak, populada em `CreateAdminUserAsync`.
- Método `ClearFirstLoginFlagAsync` em `IKeycloakUserService` + implementação idempotente em `KeycloakUserService`.
- Endpoint `POST /api/admin/me/complete-first-login` em `AdminUserController`, `[Authorize(Roles = "admin")]`, retorna 204.
- Protocol mapper no client `onboarding-backoffice` (realm export) que expõe o atributo como claim no access token.
- Fluxo no `auth-server.ts` /callback que detecta o claim e força o re-login.
- Unit test + integration test cobrindo o novo endpoint.
</objective>

<execution_context>
@$HOME/.claude/get-shit-done/workflows/execute-plan.md
@$HOME/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@./CLAUDE.md
@.planning/STATE.md
@.planning/quick/260418-dwi-force-re-login-after-first-password-chan/260418-dwi-CONTEXT.md

<!-- Arquivos da implementação (ler antes de tocar) -->
@src/Onboarding.Application/Common/IKeycloakUserService.cs
@src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs
@src/Onboarding.API/Controllers/AdminUserController.cs
@frontend/backoffice/auth-server.ts
@keycloak/onboarding-realm.json
@tests/Onboarding.API.Tests/Admin/AdminAuthorizationTests.cs
@tests/Onboarding.API.Tests/Admin/AdminTestFactory.cs
@tests/Onboarding.Domain.Tests/Application/Commands/GetAdministratorsQueryHandlerTests.cs

<interfaces>
<!-- Contratos existentes que o executor usa diretamente — sem explorar o codebase -->

From src/Onboarding.Application/Common/IKeycloakUserService.cs (trecho relevante):
```csharp
public interface IKeycloakUserService
{
    Task<string> CreateAdminUserAsync(string email, string temporaryPassword, string fullName, CancellationToken ct = default);
    Task<KeycloakUser?> GetUserByEmailAsync(string email, CancellationToken ct = default);
    // ... outros métodos existentes
    // ADICIONAR:
    Task ClearFirstLoginFlagAsync(string userId, CancellationToken ct = default);
}

public sealed record KeycloakUser(string Id, string Email, bool Enabled = true, bool EmailVerified = true);
```

From src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs (padrão a seguir — ver BlockUserAsync, que faz GET → update field → PUT):
```csharp
public async Task BlockUserAsync(string keycloakUserId, CancellationToken ct = default)
{
    var user = await _keycloakUserClient.GetUserAsync(_realm, keycloakUserId, cancellationToken: ct)
        ?? throw new InvalidOperationException($"Keycloak user '{keycloakUserId}' not found.");
    if (user.Enabled == false) return;
    user.Enabled = false;
    await _keycloakUserClient.UpdateUserAsync(_realm, keycloakUserId, user, ct);
}
```
`UserRepresentation.Attributes` é `IDictionary<string, IReadOnlyCollection<string>>?` (Keycloak.AuthServices.Sdk 2.7.x).

From src/Onboarding.API/Controllers/AdminUserController.cs (padrão existente para endpoint "me" — ver `PUT me/password`):
```csharp
[HttpPut("me/password")]
public async Task<IActionResult> ForcePasswordChange([FromBody] ForcePasswordChangeRequest request, CancellationToken ct = default)
{
    var adminEmail = HttpContext.Items["AdminEmail"] as string
        ?? User.FindFirst("email")?.Value
        ?? User.FindFirst("preferred_username")?.Value
        ?? throw new InvalidOperationException("Missing admin email context.");
    var keycloakUser = await _keycloakUserService.GetUserByEmailAsync(adminEmail, ct)
        ?? throw new InvalidOperationException($"Keycloak user not found for email: {adminEmail}");
    // ...
    return NoContent();
}
```

From frontend/backoffice/auth-server.ts (callback atual — ponto de extensão):
```ts
// após exchangeCodeForTokens + setCookie tokens + deleteCookie PKCE:
return sendRedirect(event, "/admin/users", 302);
// ← SUBSTITUIR: decodificar access token, inspecionar payload.isFirstLogin, se === "true" → chamar backend + limpar cookies + redirect /admin/login
```

From keycloak/onboarding-realm.json (client onboarding-backoffice — ln 63-91): hoje NÃO tem `protocolMappers`. Adicionar um array `protocolMappers` com o mapper `isFirstLogin`.

Padrão de protocol mapper (adaptar de outros mappers do mesmo arquivo):
```json
{
  "name": "isFirstLogin",
  "protocol": "openid-connect",
  "protocolMapper": "oidc-usermodel-attribute-mapper",
  "consentRequired": false,
  "config": {
    "user.attribute": "isFirstLogin",
    "claim.name": "isFirstLogin",
    "jsonType.label": "String",
    "id.token.claim": "false",
    "access.token.claim": "true",
    "userinfo.token.claim": "false"
  }
}
```
</interfaces>
</context>

<tasks>

<task type="auto" tdd="true">
  <name>Task 1: Backend — isFirstLogin attribute + ClearFirstLoginFlagAsync + endpoint</name>
  <files>
    src/Onboarding.Application/Common/IKeycloakUserService.cs,
    src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs,
    src/Onboarding.API/Controllers/AdminUserController.cs,
    tests/Onboarding.Domain.Tests/Application/Commands/KeycloakUserServiceFirstLoginTests.cs,
    tests/Onboarding.API.Tests/Admin/AdminFirstLoginEndpointTests.cs
  </files>
  <behavior>
    - Test 1 (unit, via IKeycloakUserService mock ou fake handler):
      `ClearFirstLoginFlagAsync("userId")` quando o usuário tem `Attributes["isFirstLogin"]=["true"]` → chama `UpdateUserAsync` com `Attributes["isFirstLogin"]=["false"]` (ou sem a chave).
    - Test 2 (unit): quando o usuário não tem atributo `isFirstLogin` (pré-existente) → método é no-op (não chama UpdateUserAsync) e não lança. Idempotente.
    - Test 3 (integration): `POST /api/admin/me/complete-first-login` sem token → 401.
    - Test 4 (integration): `POST /api/admin/me/complete-first-login` com token não-admin → 403.
    - Test 5 (integration): `POST /api/admin/me/complete-first-login` com token admin + email em `HttpContext.Items["AdminEmail"]` (via AdminTestFactory pattern) → 204 e `IKeycloakUserService.ClearFirstLoginFlagAsync` é chamado com o `userId` resolvido via `GetUserByEmailAsync`.
  </behavior>
  <action>
    Honra D-01 (atributo Keycloak), D-02 (endpoint backend), D-04 (default false para pré-existentes) do CONTEXT.md.

    **1. `IKeycloakUserService.cs`** — adicionar assinatura:
    ```csharp
    /// <summary>
    /// Clears the isFirstLogin user attribute in Keycloak (sets to "false"). Idempotent:
    /// if the attribute is absent or already "false", the method is a no-op.
    /// </summary>
    Task ClearFirstLoginFlagAsync(string userId, CancellationToken ct = default);
    ```

    **2. `KeycloakUserService.cs`**:
    - Em `CreateAdminUserAsync` (linha ~95), adicionar no inicializador do `UserRepresentation`:
      ```csharp
      Attributes = new Dictionary<string, IReadOnlyCollection<string>>
      {
          ["isFirstLogin"] = new[] { "true" }
      },
      ```
      Verificar o tipo exato de `UserRepresentation.Attributes` na versão 2.7.x do SDK — se for `IDictionary<string, ICollection<string>>` ou similar, ajustar o tipo concreto mantendo a semântica (chave `isFirstLogin`, valor `["true"]`).
    - Adicionar método novo, seguindo o padrão de `BlockUserAsync`:
      ```csharp
      public async Task ClearFirstLoginFlagAsync(string userId, CancellationToken ct = default)
      {
          var user = await _keycloakUserClient.GetUserAsync(_realm, userId, cancellationToken: ct)
              ?? throw new InvalidOperationException($"Keycloak user '{userId}' not found.");

          var attributes = user.Attributes?.ToDictionary(kv => kv.Key, kv => kv.Value)
              ?? new Dictionary<string, IReadOnlyCollection<string>>();

          // Idempotência: ausente OU já "false" → no-op
          if (!attributes.TryGetValue("isFirstLogin", out var current)
              || current.FirstOrDefault() == "false")
          {
              return;
          }

          attributes["isFirstLogin"] = new[] { "false" };
          user.Attributes = attributes;
          await _keycloakUserClient.UpdateUserAsync(_realm, userId, user, ct);
      }
      ```
      Se o tipo de `Attributes` no SDK não bater, ajustar mantendo a semântica (ler → setar chave → PUT).

    **3. `AdminUserController.cs`** — adicionar endpoint novo abaixo do `ForcePasswordChange`:
    ```csharp
    /// <summary>POST /api/admin/me/complete-first-login — Clears isFirstLogin flag after admin finished first login + password change.</summary>
    [HttpPost("me/complete-first-login")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteFirstLogin(CancellationToken ct = default)
    {
        var adminEmail = HttpContext.Items["AdminEmail"] as string
            ?? User.FindFirst("email")?.Value
            ?? User.FindFirst("preferred_username")?.Value
            ?? throw new InvalidOperationException("Missing admin email context.");

        var keycloakUser = await _keycloakUserService.GetUserByEmailAsync(adminEmail, ct)
            ?? throw new InvalidOperationException($"Keycloak user not found for email: {adminEmail}");

        await _keycloakUserService.ClearFirstLoginFlagAsync(keycloakUser.Id, ct);
        _logger.LogInformation("Admin {AdminEmail} completed first login; isFirstLogin flag cleared.", adminEmail);
        return NoContent();
    }
    ```

    **4. Unit tests — `KeycloakUserServiceFirstLoginTests.cs`** (em `tests/Onboarding.Domain.Tests/Application/Commands/`): seguir o padrão de `GetAdministratorsQueryHandlerTests.cs` (NSubstitute + Shouldly + `[Trait("Category","Unit")]`). Como o SUT é a implementação `KeycloakUserService`, mocar `IKeycloakUserClient` (da SDK Keycloak.AuthServices) + `IHttpClientFactory` + `IConfiguration` (realm). Se essa montagem ficar pesada, colocar os tests em `tests/Onboarding.API.Tests/` dentro de uma classe dedicada ou usar `Substitute.For<IKeycloakUserService>()` direto para testar a forma esperada (preferir o primeiro — exercita o código de produção).
      - Test: `ClearFirstLoginFlagAsync_WhenAttributeTrue_CallsUpdateWithFalse` — mock `GetUserAsync` retornando user com `Attributes["isFirstLogin"]=["true"]`; assert `UpdateUserAsync` recebido 1 vez com user cujo `Attributes["isFirstLogin"].First() == "false"`.
      - Test: `ClearFirstLoginFlagAsync_WhenAttributeAbsent_IsNoOp` — mock `GetUserAsync` retornando user com `Attributes=null`; assert `UpdateUserAsync` NÃO é chamado.
      - Test: `ClearFirstLoginFlagAsync_WhenAttributeAlreadyFalse_IsNoOp` — mock `GetUserAsync` com `Attributes["isFirstLogin"]=["false"]`; assert `UpdateUserAsync` NÃO é chamado.

    **5. Integration test — `AdminFirstLoginEndpointTests.cs`** (em `tests/Onboarding.API.Tests/Admin/`): seguir o padrão de `AdminAuthorizationTests.cs`. Usar `AdminTestFactory`, `FakeJwtTokenHelper.GenerateAdminJwt()`, `[Collection(WebAppFactoryCollection.Name)]`, `[Trait("Category","Integration")]`.
      - `CompleteFirstLogin_WithoutToken_Returns401` → `_unauthenticatedClient.PostAsync("/api/admin/me/complete-first-login", null)` → `HttpStatusCode.Unauthorized`.
      - `CompleteFirstLogin_WithNonAdminToken_Returns403` → `_nonAdminClient` → `Forbidden`.
      - `CompleteFirstLogin_WithAdminToken_Returns204_AndCallsClearFirstLoginFlagAsync` — mocar em `AdminTestFactory` os retornos de `IKeycloakUserService.GetUserByEmailAsync(...)` (retornar `new KeycloakUser("user-uuid","admin@onboarding.local")`) e `ClearFirstLoginFlagAsync(...)` (retornar `Task.CompletedTask`). Após a chamada, `status == 204` e `await IKeycloakUserService.Received(1).ClearFirstLoginFlagAsync("user-uuid", Arg.Any<CancellationToken>())`.
      - Se `AdminTestFactory` ainda não expõe um mock de `IKeycloakUserService`, consultar como ele monta os outros mocks (`AdminRepositoryMock`) e adicionar um `KeycloakUserServiceMock` no mesmo padrão, sem alterar a API dos outros testes.

    Usar Serilog/ILogger existente (campo `_logger` do controller) para o log informativo. Não adicionar audit log separado (já coberto por `AdminCreated`).
  </action>
  <verify>
    <automated>dotnet test tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj --filter "FullyQualifiedName~KeycloakUserServiceFirstLoginTests" --nologo --verbosity minimal &amp;&amp; dotnet test tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj --filter "FullyQualifiedName~AdminFirstLoginEndpointTests" --nologo --verbosity minimal</automated>
  </verify>
  <done>
    - `IKeycloakUserService` expõe `ClearFirstLoginFlagAsync`.
    - `KeycloakUserService.CreateAdminUserAsync` popula `Attributes["isFirstLogin"]=["true"]`.
    - `KeycloakUserService.ClearFirstLoginFlagAsync` implementa GET → mutate → PUT com idempotência (ausente / já "false" → no-op).
    - `POST /api/admin/me/complete-first-login` existe, protegido por `[Authorize(Roles = "admin")]` (herdado do controller), retorna 204.
    - Todos os unit tests e integration tests passam.
    - `dotnet build src/Onboarding.API/Onboarding.API.csproj` compila sem erros.
  </done>
</task>

<task type="auto">
  <name>Task 2: Keycloak protocol mapper + Vinxi callback — expor claim e forçar re-login</name>
  <files>
    keycloak/onboarding-realm.json,
    frontend/backoffice/auth-server.ts
  </files>
  <action>
    Honra D-03 (callback limpa cookies + redirect /admin/login) e a recomendação do CONTEXT.md de usar protocol mapper (claim no access token) em vez de round-trip backend.

    **1. `keycloak/onboarding-realm.json`** — no client `onboarding-backoffice` (linhas 63-91), adicionar um array `protocolMappers` com o mapper `isFirstLogin`. Como o objeto atualmente não tem a chave, inserir antes da chave `attributes` (ordem JSON não importa, mas preservar vírgulas válidas):
    ```json
    "protocolMappers": [
      {
        "name": "isFirstLogin",
        "protocol": "openid-connect",
        "protocolMapper": "oidc-usermodel-attribute-mapper",
        "consentRequired": false,
        "config": {
          "user.attribute": "isFirstLogin",
          "claim.name": "isFirstLogin",
          "jsonType.label": "String",
          "id.token.claim": "false",
          "access.token.claim": "true",
          "userinfo.token.claim": "false"
        }
      }
    ],
    ```
    NÃO modificar o client `onboarding-client-acf` (escopo diferente — público, não admin). NÃO modificar `onboarding-app` (ROPC legado). Manter `secret`, `redirectUris`, `attributes.login_theme` e demais campos intocados.

    **2. `frontend/backoffice/auth-server.ts`** — modificar o handler GET `/callback` (linhas 68-140). Depois do `setCookie` dos tokens e `deleteCookie` dos cookies PKCE (antes do `sendRedirect` atual para `/admin/users`):
    ```ts
    // Detectar primeiro login via claim isFirstLogin no access token
    let isFirstLogin = false;
    try {
      const parts = tokens.accessToken.split(".");
      if (parts.length >= 2) {
        const payload = JSON.parse(Buffer.from(parts[1], "base64").toString("utf-8")) as Record<string, unknown>;
        isFirstLogin = payload.isFirstLogin === "true";
      }
    } catch {
      // Token inválido/indecodificável → tratar como não-primeiro-login (fluxo normal)
      isFirstLogin = false;
    }

    if (isFirstLogin) {
      // Best-effort: chamar backend para limpar a flag.
      // Mesmo que falhe, seguimos com a limpeza de cookies + redirect para /admin/login
      // (self-healing: próximo login repete o ciclo se a limpeza falhou).
      const backendUrl = process.env.BACKEND_URL || "http://localhost:5000";
      try {
        await fetch(`${backendUrl}/api/admin/me/complete-first-login`, {
          method: "POST",
          headers: {
            Authorization: `Bearer ${tokens.accessToken}`,
            "Content-Type": "application/json",
          },
        });
      } catch (err) {
        // Log apenas — não bloquear o redirect
        console.error("[auth/callback] Failed to clear isFirstLogin flag:", err);
      }

      // Limpar cookies de sessão para forçar re-login com a senha nova
      deleteCookie(event, "backoffice_access_token", { path: "/" });
      deleteCookie(event, "backoffice_refresh_token", { path: "/" });

      return sendRedirect(event, "/admin/login", 302);
    }

    return sendRedirect(event, "/admin/users", 302);
    ```
    Obs: `Buffer` já é usado em `/auth/me` (linha 173), então não precisa import novo. Nome da variável de ambiente do backend: seguir o padrão do repo — se já existir `process.env.BACKEND_URL` ou `process.env.API_URL` em outro handler, reusar; senão usar `BACKEND_URL` com fallback `http://localhost:5000` (porta do Onboarding.API). Validar na execução qual var é a padrão do projeto grepping por `process.env.` no `frontend/backoffice/`.

    Observação: se o claim vier como boolean `true` (em vez de string `"true"`) em alguma config futura do mapper, aceitar ambos: `payload.isFirstLogin === "true" || payload.isFirstLogin === true`. Deixar assim por defensividade.

    Admins pré-existentes: `payload.isFirstLogin` será `undefined` → `isFirstLogin = false` → fluxo normal (`/admin/users`). OK.
  </action>
  <verify>
    <automated>node -e "const j=require('./keycloak/onboarding-realm.json'); const c=j.clients.find(x=>x.clientId==='onboarding-backoffice'); const m=(c.protocolMappers||[]).find(p=>p.name==='isFirstLogin'); if(!m) {console.error('missing mapper'); process.exit(1);} if(m.config['claim.name']!=='isFirstLogin'||m.config['access.token.claim']!=='true'){console.error('bad mapper config'); process.exit(1);} console.log('realm mapper OK');" &amp;&amp; node -e "const fs=require('fs'); const s=fs.readFileSync('./frontend/backoffice/auth-server.ts','utf8'); if(!s.includes('isFirstLogin')||!s.includes('complete-first-login')||!s.includes('/admin/login')){console.error('callback missing first-login logic'); process.exit(1);} console.log('auth-server callback OK');"</automated>
  </verify>
  <done>
    - `keycloak/onboarding-realm.json` tem o mapper `isFirstLogin` no client `onboarding-backoffice` (access.token.claim=true, id.token.claim=false).
    - `auth-server.ts` /callback decodifica o access token, detecta o claim, chama `POST /api/admin/me/complete-first-login` (best-effort), limpa cookies e redireciona para `/admin/login` quando `isFirstLogin === "true"`.
    - Fluxo atual (claim ausente ou `"false"`) continua redirecionando para `/admin/users`.
    - Verificação manual (após restart do compose): criar novo admin → logar com senha temporária → Keycloak força UPDATE_PASSWORD → após submit da nova senha, o callback detecta a flag e redireciona para `/admin/login` (não para `/admin/users`). Segundo login com a senha nova cai normalmente em `/admin/users`.
  </done>
</task>

</tasks>

<verification>
- Backend: `dotnet test tests/Onboarding.Domain.Tests/Onboarding.Domain.Tests.csproj --filter "FullyQualifiedName~KeycloakUserServiceFirstLoginTests"` passa.
- Backend: `dotnet test tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj --filter "FullyQualifiedName~AdminFirstLoginEndpointTests"` passa.
- Build: `dotnet build Onboarding.slnx` sem erros.
- Realm JSON válido: `node -e "JSON.parse(require('fs').readFileSync('./keycloak/onboarding-realm.json','utf8'))"` sem erro.
- Callback TS: inspeção do arquivo garante que existe ramo `isFirstLogin === "true"` → POST + delete cookies + redirect `/admin/login`.
- Suíte geral (opcional, se rápido): `dotnet test Onboarding.slnx --nologo` — nenhum teste existente deve regredir.
</verification>

<success_criteria>
- Novos admins são criados com `Attributes["isFirstLogin"]=["true"]` em Keycloak.
- Access token emitido pelo client `onboarding-backoffice` contém o claim `isFirstLogin`.
- Callback do Vinxi, ao detectar o claim, chama `POST /api/admin/me/complete-first-login`, limpa `backoffice_access_token` + `backoffice_refresh_token`, e redireciona para `/admin/login`.
- Endpoint `POST /api/admin/me/complete-first-login` retorna 204 para admin autenticado e é idempotente (flag ausente ou `false` → 204 sem efeito).
- Admins pré-existentes (sem atributo) não são afetados.
- Unit + integration tests passam.
</success_criteria>

<output>
After completion, create `.planning/quick/260418-dwi-force-re-login-after-first-password-chan/260418-dwi-SUMMARY.md`.
</output>
