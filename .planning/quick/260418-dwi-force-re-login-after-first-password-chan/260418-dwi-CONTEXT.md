---
name: Force re-login after first password change - Context
description: Locked decisions from /gsd:quick --discuss session for quick task 260418-dwi
type: quick-task-context
---

# Quick Task 260418-dwi: Force re-login after first password change - Context

**Gathered:** 2026-04-18
**Status:** Ready for planning

<domain>
## Task Boundary

Forçar o admin a fazer login novamente após completar o UPDATE_PASSWORD (primeira troca de senha). Hoje, quando o admin criado com senha temporária termina o fluxo ACF+PKCE, ele cai logado no backoffice com os tokens da sessão iniciada com credencial temporária. A sessão já renovada via UPDATE_PASSWORD fica válida, mas o requisito é higienizar: o callback deve detectar "primeiro login" (`isFirstLogin=true`), marcar como concluído, limpar cookies locais e redirecionar para `/admin/login` para que o admin re-entre com a senha nova.

</domain>

<decisions>
## Implementation Decisions

### Onde armazenar a flag IsFirstLogin
- **Decisão:** Atributo do usuário no Keycloak (`user.attributes.isFirstLogin`).
- **Motivação:** Keycloak é a fonte única da verdade para dados de identidade; evita acoplar tabela local ao ciclo de vida de admins; atributo é persistido e pode ser exposto via Admin API e via claim no access token (protocol mapper).
- **Consequência:** `CreateAdminUserAsync` (`KeycloakUserService`) precisa popular o `UserRepresentation.Attributes` com `isFirstLogin=["true"]` na criação do admin. O valor é ausente para admins pré-existentes (igual a `false` por default).

### Como expor a limpeza da flag para o Vinxi callback
- **Decisão:** Endpoint `POST /api/admin/me/complete-first-login` no backend .NET.
- **Motivação:** Vinxi server-side não tem credenciais do service account Keycloak (apenas `onboarding-backoffice` confidential client para token exchange); toda escrita em atributo de usuário deve passar pelo backend que usa `KeycloakUserService` (service account `onboarding-api-admin` com `manage-users`).
- **Consequência:** Novo endpoint no `AdminUserController` (ou novo controller dedicado) com `[Authorize(Roles = "admin")]`. Recebe o sub do JWT, chama `KeycloakUserService.ClearFirstLoginFlagAsync(keycloakUserId)`. Retorna 204 No Content.

### Como deslogar o usuário após detectar first login no callback
- **Decisão:** Clear cookies locais (`backoffice_access_token` + `backoffice_refresh_token`) + redirect para `/admin/login`.
- **Motivação:** Recommended no discovery — dispensa chamar OIDC logout endpoint do Keycloak (evita round-trip extra e potencial ruído com SSO de outras apps). Como o access token expira em 5 min e o refresh é curto, basta invalidar localmente.
- **Consequência:** Callback `/auth/callback` no `auth-server.ts`, ao detectar `isFirstLogin=true` no access token decodificado: (1) chama `POST /api/admin/me/complete-first-login` com o próprio access token no header `Authorization`; (2) `deleteCookie("backoffice_access_token")` + `deleteCookie("backoffice_refresh_token")` + limpeza dos cookies PKCE remanescentes; (3) `sendRedirect(event, "/admin/login", 302)`.

### Como tratar admins pré-existentes na migration
- **Decisão:** Default `false` para admins já existentes no Keycloak; `true` apenas para novos admins criados após o deploy desta feature.
- **Motivação:** Recommended — admins existentes já fizeram login com senha real; forçar re-login deles seria UX hostil. A ausência do atributo `isFirstLogin` naturalmente equivale a `false`.
- **Consequência:** Sem migration script. O callback trata `claim.isFirstLogin !== "true"` como "não é primeiro login" (incluindo ausência do claim). Nenhum admin pré-existente é afetado.

### Claude's Discretion
- **Exposição da flag no access token:** decidir se usa um protocol mapper no Keycloak (mais eficiente — claim já vem no JWT) ou se o callback chama um endpoint backend `GET /api/admin/me/first-login-status` (mais defensivo — não depende de config do realm). **Recomendação do planner:** protocol mapper (User Attribute mapper) em `onboarding-backoffice` client, claim name `isFirstLogin`, adicionar ao access token. Mais leve, evita round-trip.
- **Nome exato do método em `IKeycloakUserService`:** `ClearFirstLoginFlagAsync(userId, ct)` (retorna `Task`). Implementação: GET user → remover/setar `isFirstLogin="false"` em `Attributes` → PUT user.
- **Onde colocar o endpoint:** pode ser em `AdminUserController` como método `CompleteFirstLogin()` com rota `POST /api/admin/me/complete-first-login`, seguindo o padrão do existente `PUT /api/admin/me/password`.
- **Testes:** pelo menos um unit test para `ClearFirstLoginFlagAsync` + um integration test do endpoint + um test no frontend (playwright ou unit do auth-server handler) validando o redirect comportamental.

</decisions>

<specifics>
## Specific Ideas

### Pontos de integração (arquivos conhecidos)

- `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs`
  - `CreateAdminUserAsync` (linhas 89-143): adicionar `Attributes = new Dictionary<string, IReadOnlyCollection<string>> { ["isFirstLogin"] = ["true"] }` no `UserRepresentation`.
  - Adicionar `ClearFirstLoginFlagAsync(string userId, CancellationToken ct)` que lê o usuário, atualiza `Attributes["isFirstLogin"] = ["false"]` (ou remove a key), e faz PUT.
- `src/Onboarding.Application/Common/IKeycloakUserService.cs`
  - Adicionar assinatura `Task ClearFirstLoginFlagAsync(string userId, CancellationToken ct = default)`.
- `src/Onboarding.API/Controllers/AdminUserController.cs`
  - Novo endpoint `[HttpPost("me/complete-first-login")]` lendo `HttpContext.Items["AdminEmail"]` → resolve Keycloak user → chama `ClearFirstLoginFlagAsync`.
- `frontend/backoffice/auth-server.ts`
  - Callback `/auth/callback` (linhas 68-140): após o token exchange, decodificar o access token, inspecionar `payload.isFirstLogin`, se `"true"` → chamar `POST /api/admin/me/complete-first-login` via fetch → deleteCookie dos tokens → redirect `/admin/login`. Se `false` ou ausente → fluxo atual (redirect `/admin/users`).
- `keycloak/realm-export.json` (ou equivalente onde clients são configurados)
  - Adicionar protocol mapper "isFirstLogin" ao client `onboarding-backoffice`: tipo `oidc-usermodel-attribute-mapper`, user attribute `isFirstLogin`, token claim name `isFirstLogin`, add to access token: true, add to ID token: false.

### Considerações de segurança

- O endpoint `/api/admin/me/complete-first-login` deve ser idempotente: se `isFirstLogin` já for `false`, retorna 204 sem efeito.
- O endpoint não aceita parâmetros — extrai identidade do JWT/cookie, não permite target user arbitrário.
- Audit: chamada do endpoint não precisa de audit log separado (já temos `AdminCreated` no momento da criação). Opcionalmente, log informativo em `ILogger` quando um admin completa primeiro login.

### Edge cases

- **Admin pré-existente sem atributo:** `payload.isFirstLogin` é `undefined` → tratado como falsy → fluxo normal (redirect `/admin/users`).
- **Falha na chamada a `POST /api/admin/me/complete-first-login`:** logar erro, MAS ainda limpar cookies + redirect `/admin/login`. O admin verá primeiro login novamente na próxima tentativa; auto-cura quando o endpoint voltar a responder. Nunca deixar o admin com sessão válida se a flag não pôde ser limpa.
- **Admin completou senha mas abortou o redirect antes da limpeza:** próximo login detectará `isFirstLogin=true` de novo e repetirá o ciclo. Não há risco de session travada porque a limpeza acontece no redirect.

</specifics>

<canonical_refs>
## Canonical References

- [Keycloak Admin REST API - User attributes](https://www.keycloak.org/docs-api/26.1/rest-api/#_userrepresentation) — `UserRepresentation.attributes` field semantics.
- [Keycloak Protocol Mappers](https://www.keycloak.org/docs/latest/server_admin/#_protocol-mappers) — User Attribute → Token Claim mapper configuration.
- `.planning/phases/29-admin-management-audit/29-01-PLAN.md` — contexto de `CreateAdminCommand` e UPDATE_PASSWORD flow original.
- `46b4fe8` (commit recente) — migração para ler `backoffice_access_token` cookie no `AdminSessionMiddleware`; contexto do fluxo de cookies atual.

</canonical_refs>
