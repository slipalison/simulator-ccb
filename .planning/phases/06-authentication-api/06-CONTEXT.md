# Phase 6: Authentication API - Context

**Gathered:** 2026-04-06
**Status:** Ready for planning

<domain>
## Phase Boundary

O backend expõe endpoints de autenticação: login (ROPC via Keycloak), refresh de token, e rota protegida `/api/clients/me`. O objetivo é que o backend emita/valide JWTs e proteja rotas — sem nenhuma UI (Phase 9 cobre o login screen).

Fora do escopo desta fase: tela de login (Phase 9), lógica de detecção de "quase expirado" no frontend (Phase 9), profile screen (Phase 10).

</domain>

<decisions>
## Implementation Decisions

### Endpoints expostos

- **D-01:** `POST /api/auth/login` — recebe `{email, password}`, faz ROPC call ao Keycloak, retorna `{access_token, refresh_token, expires_in, token_type}`. Endpoint público (sem [Authorize]).
- **D-02:** `POST /api/auth/refresh` — recebe `{refresh_token}`, faz refresh call ao Keycloak, retorna novos tokens. Endpoint público (sem [Authorize]).
- **D-03:** `GET /api/clients/me` — rota protegida com `[Authorize]`. Retorna perfil do cliente autenticado. Retorna 401 se token ausente/inválido/expirado.

### JWT Validation

- **D-04:** `AddAuthentication().AddJwtBearer()` configurado com `Authority = Keycloak:RealmUrl` (ex: `http://keycloak:8080/realms/onboarding`). Auto-discovery via OIDC metadata endpoint.
- **D-05:** `ValidateAudience = false` — tokens ROPC do Keycloak têm `aud: ["account"]` ou `aud: ["onboarding-app", "account"]`, não o nosso API. Evita misconfiguration.
- **D-06:** `[Authorize]` sem policy explícita — apenas validação de token JWT assinado e não expirado. Nenhuma role/claim adicional exigida para `/api/clients/me`.

### Lookup de cliente em GET /api/clients/me

- **D-07:** Lookup por `email` claim do JWT. O Keycloak inclui `email` nos access tokens (username = email no realm `onboarding`). Buscar via `IClientRepository.GetByEmailAsync(email, ct)`.
- **D-08:** Adicionar `GetByEmailAsync` ao `IClientRepository` + implementação em `ClientRepository`. Sem modificação no aggregate `Client` e sem nova migration.
- **D-09:** Retorna 404 com ProblemDetails genérico se nenhum cliente encontrado (não deve ocorrer em fluxo normal — mas não revelar "usuário não existe").

### Keycloak Token Service

- **D-10:** Nova interface `IKeycloakTokenService` na camada Application. Métodos: `ExchangePasswordAsync(email, password, ct)` e `RefreshTokenAsync(refreshToken, ct)`. Retornam `TokenResponse` DTO.
- **D-11:** Implementação `KeycloakTokenService` na Infrastructure via `IHttpClientFactory` (named client `"keycloak-token"`). POST para `{RealmUrl}/protocol/openid-connect/token` com `grant_type=password` ou `grant_type=refresh_token`.
- **D-12:** Credenciais Keycloak para ROPC: `client_id = onboarding-app` (public client, sem secret). Lido de configuração `Keycloak:PublicClientId`.
- **D-13:** Erros Keycloak (401 invalid_grant) → retornar 401 com ProblemDetails genérico (`"Invalid credentials"`) — sem revelar se email existe (SEC-08).

### AUTH-04 — Refresh scope

- **D-14:** Backend só expõe `POST /api/auth/refresh`. A detecção de "token próximo da expiração" e o trigger automático de refresh são responsabilidade do frontend (Phase 9). Satisfaz AUTH-04 ("backend or frontend token logic").

### Claude's Discretion

- Estrutura exata do `TokenResponse` DTO (incluir `expires_in`, `token_type`, etc.)
- Configuração do timeout do `HttpClient` para o Keycloak token endpoint
- Estrutura de pastas para `AuthController` (junto com `RegistrationController` em `Controllers/`)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requisitos e scope oficiais
- `.planning/REQUIREMENTS.md` §Autenticação — AUTH-02, AUTH-03, AUTH-04 (requisitos formais)
- `.planning/ROADMAP.md` §Phase 6 — Goal e success criteria oficiais

### Tech stack aprovado
- `CLAUDE.md` — `Microsoft.AspNetCore.Authentication.JwtBearer` (aprovado), sem keycloak-js, sem localStorage

### Código existente (pontos de integração)
- `src/Onboarding.API/Program.cs` — onde `AddAuthentication().AddJwtBearer()` e `UseAuthentication()` / `UseAuthorization()` serão adicionados
- `src/Onboarding.Infrastructure/DependencyInjection.cs` — onde o `IHttpClientFactory` named client `"keycloak-token"` será registrado
- `src/Onboarding.Application/Common/IKeycloakUserService.cs` — padrão a seguir para `IKeycloakTokenService`
- `src/Onboarding.Domain/Repositories/IClientRepository.cs` — interface que receberá `GetByEmailAsync`
- `src/Onboarding.Infrastructure/Repositories/ClientRepository.cs` — implementação que receberá `GetByEmailAsync`
- `src/Onboarding.API/Controllers/RegistrationController.cs` — padrão de controller a seguir para `AuthController`

### Configurações Keycloak existentes
- `infra/keycloak/realm-export.json` — confirmar `directAccessGrantsEnabled: true` em `onboarding-app` e `access_token_lifespan` configurado

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ICommandHandler<TCommand, TResult>` / `IQueryHandler<TQuery, TResult>` — padrão CQRS para novo `GetClientByEmailQuery`
- `SensitiveDataDestructuringPolicy` — já mascara `password` e `token` em todos os logs globalmente
- `IdempotencyFilter` — provavelmente NÃO aplicar em login/refresh (idempotência não faz sentido para auth)
- `IDistributedCache` (já registrado via `AddDistributedMemoryCache()`) — disponível se necessário

### Established Patterns
- Controller → FluentValidation → CommandHandler → Repository (padrão estabelecido em Phase 5)
- Erros de domínio/infraestrutura mapeados para ProblemDetails com mensagens genéricas (SEC-08)
- `IHttpClientFactory` já em uso via `AddKeycloakAdminHttpClient` — seguir mesmo padrão
- Duende.AccessTokenManagement já no projeto (para o service account) — NÃO usar para ROPC user tokens
- Bootstrap logger + `UseSerilogRequestLogging()` já ativo — requests de auth serão logados automaticamente

### Integration Points
- `Program.cs` precisa de `UseAuthentication()` e `UseAuthorization()` ANTES de `MapControllers()`
- `AddAuthentication().AddJwtBearer()` adicionado em `Program.cs` (ou extension method `AddJwtBearerAuth()`)
- `AddInfrastructure()` receberá registro do named HttpClient `"keycloak-token"`
- `IClientRepository` receberá `GetByEmailAsync` — Domain + Infrastructure layers

</code_context>

<specifics>
## Specific Ideas

- Lookup por `email` claim confirmado: `User.FindFirst("email")?.Value` — sem KeycloakUserId no aggregate
- `POST /api/auth/refresh` sem [Authorize] — o refresh_token NÃO é um JWT Bearer, não passar pelo middleware JWT
- Mensagens de erro genéricas em auth: nunca revelar se email existe ou não (SEC-08 já estabelecido em Phase 5)

</specifics>

<deferred>
## Deferred Ideas

- Detecção de "token quase expirado" e refresh automático — Phase 9 (frontend)
- Logout endpoint (revogar refresh_token no Keycloak) — não está em AUTH-02/03/04, backlog se necessário
- Rate limiting no endpoint de login — BACK-07 ou fase de hardening

</deferred>

---

*Phase: 06-authentication-api*
*Context gathered: 2026-04-06*
