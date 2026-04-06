---
phase: 06-authentication-api
plan: 03
subsystem: authentication-implementation
tags: [jwt, keycloak, ropc, tdd-green, controllers, handlers, nsubstitute]

requires:
  - phase: 06-authentication-api
    plan: 02
    provides: IKeycloakTokenService, TokenResponse, GetByEmailAsync, AddJwtBearer config

provides:
  - "KeycloakTokenService: ROPC e refresh via named HttpClient 'keycloak-token' (IHttpClientFactory)"
  - "KeycloakAuthException: exception para erros de autenticação Keycloak — mapeada para 401 genérico"
  - "LoginCommand + LoginCommandHandler: CQRS manual para troca de credenciais via IKeycloakTokenService"
  - "RefreshTokenCommand + RefreshTokenCommandHandler: CQRS manual para refresh de token"
  - "LoginCommandValidator + RefreshTokenCommandValidator: FluentValidation para os commands de auth"
  - "AuthController: POST /api/auth/login e POST /api/auth/refresh (endpoints públicos)"
  - "ClientsController: GET /api/clients/me com [Authorize] + lookup por email claim (D-07)"
  - "14 stubs RED do Plan 01 convertidos para GREEN — ciclo TDD completo"

affects:
  - 06-authentication-api (AUTH-02, AUTH-03, AUTH-04 implementados)
  - Program.cs (RequireHttpsMetadata=false para desenvolvimento local)
  - Onboarding.Application.csproj (Microsoft.Extensions.Logging.Abstractions adicionado)

tech-stack:
  added:
    - "Microsoft.Extensions.Logging.Abstractions 10.0.5 (Onboarding.Application.csproj — necessário para ILogger no LoginCommandHandler)"
  patterns:
    - "KeycloakTokenService: FormUrlEncodedContent para ROPC — grant_type=password e refresh_token"
    - "KeycloakAuthException: exception simples sem detalhes do Keycloak — SEC-08 enforced"
    - "LoginRequest/RefreshTokenRequest: properties nullable (string?) — permite FluentValidation receber campos null sem 400 do model binding"
    - "AuthController.Login: FluentValidation → 422; KeycloakAuthException → 401 genérico (D-13)"
    - "ClientsController.GetMe: User.FindFirst('email') para lookup — MapInboundClaims=false garante claim name correto"
    - "NSubstitute ThrowsAsync alternativa: .Returns(Task.FromException<T>(new Exception())) — ThrowsAsync não existe no NSubstitute 5.x"

key-files:
  created:
    - "src/Onboarding.Infrastructure/Keycloak/KeycloakAuthException.cs"
    - "src/Onboarding.Infrastructure/Keycloak/KeycloakTokenService.cs"
    - "src/Onboarding.Application/Auth/Commands/LoginCommand.cs"
    - "src/Onboarding.Application/Auth/Commands/RefreshTokenCommand.cs"
    - "src/Onboarding.Application/Auth/Validators/LoginCommandValidator.cs"
    - "src/Onboarding.Application/Auth/Validators/RefreshTokenCommandValidator.cs"
    - "src/Onboarding.API/Controllers/AuthController.cs"
    - "src/Onboarding.API/Controllers/ClientsController.cs"
  modified:
    - "src/Onboarding.Infrastructure/DependencyInjection.cs"
    - "src/Onboarding.Application/DependencyInjection.cs"
    - "src/Onboarding.Application/Onboarding.Application.csproj"
    - "src/Onboarding.API/appsettings.json"
    - "src/Onboarding.API/Program.cs"
    - "tests/Onboarding.API.Tests/Authentication/JwtBearerConfigurationTests.cs"
    - "tests/Onboarding.API.Tests/Authentication/AuthorizationMiddlewareTests.cs"
    - "tests/Onboarding.API.Tests/Api/ClientsMeEndpointTests.cs"
    - "tests/Onboarding.API.Tests/Api/LoginEndpointTests.cs"
    - "tests/Onboarding.API.Tests/Api/RefreshTokenEndpointTests.cs"

key-decisions:
  - "LoginRequest/RefreshTokenRequest com string? nullable — evita 400 do model binding ASP.NET Core quando campos são null; FluentValidation captura e retorna 422 conforme esperado"
  - "RequireHttpsMetadata=false em Program.cs — necessário para desenvolvimento local onde Keycloak roda em HTTP; não afeta produção que usa HTTPS"
  - "Microsoft.Extensions.Logging.Abstractions adicionado à Application layer — ILogger<LoginCommandHandler> requer o pacote; Application layer não tinha dependência de logging antes"
  - "NSubstitute ThrowsAsync não existe no NSubstitute 5.x — usar .Returns(Task.FromException<T>(exception)) como padrão equivalente"

requirements-completed:
  - AUTH-02
  - AUTH-03
  - AUTH-04

duration: 18min
completed: 2026-04-06
---

# Phase 6 Plan 03: Authentication Implementation — TDD GREEN Summary

**KeycloakTokenService (ROPC via IHttpClientFactory), AuthController (login/refresh), ClientsController (GET /me com [Authorize]) implementados — 14 stubs RED do Plan 01 convertidos para GREEN com 42 testes passando**

## Performance

- **Duration:** 18 min
- **Started:** 2026-04-06T18:30:00Z
- **Completed:** 2026-04-06T18:48:00Z
- **Tasks:** 2
- **Files modified:** 20

## Accomplishments

- `KeycloakAuthException` criada — exception simples capturando qualquer erro 4xx do Keycloak sem expor detalhes internos (SEC-08)
- `KeycloakTokenService` implementado com ROPC (`grant_type=password`) e refresh (`grant_type=refresh_token`) via named HttpClient `keycloak-token` — sem Duende.AccessTokenManagement (D-11, D-12)
- `LoginCommand` + `LoginCommandHandler` e `RefreshTokenCommand` + `RefreshTokenCommandHandler` criados — CQRS manual via DI sem MediatR
- `LoginCommandValidator` e `RefreshTokenCommandValidator` com FluentValidation 12.x
- DI wiring completo: named HttpClient `keycloak-token`, `IKeycloakTokenService`, handlers e validators registrados nas camadas Infrastructure e Application
- `appsettings.json` atualizado com `Keycloak:PublicClientId = "onboarding-app"`
- `AuthController` com `POST /api/auth/login` e `POST /api/auth/refresh` — FluentValidation → 422; KeycloakAuthException → 401 genérico
- `ClientsController` com `GET /api/clients/me` + `[Authorize]` — lookup por email claim (D-07); cliente não encontrado → 404 genérico (D-09)
- `Program.cs` atualizado: `RequireHttpsMetadata=false` para desenvolvimento local
- 14 stubs RED do Plan 01 convertidos para GREEN — ciclo TDD completo (RED → GREEN)
- `dotnet test` final: **42 aprovados, 2 ignorados, 0 falhas**

## Task Commits

1. **Task 1: KeycloakTokenService, commands/handlers e DI wiring** — `a940fb7` (feat)
2. **Task 2: AuthController, ClientsController e stubs GREEN** — `fae3c19` (feat)

## Endpoints Implementados

| Endpoint | Método | Auth | Comportamento |
|----------|--------|------|---------------|
| `/api/auth/login` | POST | Público | FluentValidation → 422; credenciais válidas → 200 + TokenResponse; inválidas → 401 genérico |
| `/api/auth/refresh` | POST | Público | FluentValidation → 422; token válido → 200 + TokenResponse; inválido → 401 genérico |
| `/api/clients/me` | GET | [Authorize] | Sem token → 401; com token → lookup por email claim; cliente não encontrado → 404 genérico; encontrado → 200 + ClientProfileDto |

## Contagem Final de Testes

| Arquivo | Testes | Status |
|---------|--------|--------|
| JwtBearerConfigurationTests.cs | 3 | GREEN |
| AuthorizationMiddlewareTests.cs | 1 | GREEN |
| LoginEndpointTests.cs | 4 | GREEN |
| RefreshTokenEndpointTests.cs | 3 | GREEN |
| ClientsMeEndpointTests.cs | 3 | GREEN |
| **Total auth** | **14** | **GREEN** |
| **Total suite** | **42 aprovados** | **0 falhas** |

## Padrões de Segurança Implementados

| Padrão | Localização | Descrição |
|--------|-------------|-----------|
| SEC-08 | AuthController | Mensagem genérica "Invalid credentials." — não revela se email existe |
| SEC-08 | ClientsController | 404 genérico — não revela "usuário não existe" |
| D-07 | ClientsController | Lookup por email claim do JWT (MapInboundClaims=false preserva "email") |
| D-09 | ClientsController | 404 com ProblemDetails genérico quando cliente não encontrado no DB |
| D-11 | KeycloakTokenService | IHttpClientFactory (não Duende.AccessTokenManagement) para ROPC calls |
| D-12 | KeycloakTokenService | client_id = onboarding-app (public client, sem secret) |
| D-13 | AuthController | KeycloakAuthException captura todos os erros 4xx — mapeados para 401 genérico |
| T-6-10 | AuthController | [Idempotent] ausente — confirmado (grep não encontra) |

## Notas para Phase 9 (Frontend)

- `POST /api/auth/login` disponível: `{ email, password }` → `{ accessToken, refreshToken, expiresIn, tokenType, refreshExpiresIn, scope }`
- `POST /api/auth/refresh` disponível: `{ refreshToken }` → `{ accessToken, refreshToken, ... }`
- `GET /api/clients/me` disponível: Bearer token no header → `{ id, name, email, phone, type, cpf?, cnpj?, razaoSocial? }`
- TokenResponse usa camelCase (JSON serialization padrão do ASP.NET Core) — frontend deve usar `accessToken`, não `access_token`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Microsoft.Extensions.Logging.Abstractions ausente na Application layer**
- **Found during:** Task 1 (build após criar LoginCommand.cs)
- **Issue:** CS0234 — `ILogger<LoginCommandHandler>` requer `Microsoft.Extensions.Logging.Abstractions` mas a camada Application não tinha o pacote
- **Fix:** Adicionada `PackageReference` para `Microsoft.Extensions.Logging.Abstractions 10.0.5` no `Onboarding.Application.csproj`
- **Files modified:** `src/Onboarding.Application/Onboarding.Application.csproj`
- **Commit:** `a940fb7`

**2. [Rule 1 - Bug] RequireHttpsMetadata necessário para testes com Authority HTTP**
- **Found during:** Task 2 (execução dos testes — `JwtBearerPostConfigureOptions` lançava InvalidOperationException)
- **Issue:** `The MetadataAddress or Authority must use HTTPS unless disabled` — AuthTestApiFactory usa `http://localhost:8180/realms/onboarding` como Authority
- **Fix:** Adicionado `options.RequireHttpsMetadata = false` em `Program.cs` no `AddJwtBearer`
- **Files modified:** `src/Onboarding.API/Program.cs`
- **Commit:** `fae3c19`

**3. [Rule 1 - Bug] LoginRequest/RefreshTokenRequest com properties non-nullable causavam 400 ao invés de 422**
- **Found during:** Task 2 (testes Login_WithMissingEmail_Returns422 e Login_WithMissingPassword_Returns422 falhavam com 400)
- **Issue:** ASP.NET Core model binding falha antes do FluentValidation quando propriedades não-nullable recebem `null` no JSON — retorna 400 BadRequest em vez de 422
- **Fix:** Properties de `LoginRequest` e `RefreshTokenRequest` alteradas para `string?` (nullable); controller mapeia para `string.Empty` antes de passar ao command
- **Files modified:** `src/Onboarding.API/Controllers/AuthController.cs`
- **Commit:** `fae3c19`

**4. [Rule 1 - Bug] NSubstitute 5.x não tem ThrowsAsync — sintaxe incorreta no plano**
- **Found during:** Task 2 (build falhou com CS1061 — ThrowsAsync não encontrado)
- **Issue:** O plano usava `.ThrowsAsync(new Exception())` que não existe no NSubstitute 5.x
- **Fix:** Substituído por `.Returns(Task.FromException<TokenResponse>(new Exception()))` — equivalente correto no NSubstitute 5.x
- **Files modified:** `tests/Onboarding.API.Tests/Api/LoginEndpointTests.cs`, `tests/Onboarding.API.Tests/Api/RefreshTokenEndpointTests.cs`
- **Commit:** `fae3c19`

**Total deviations:** 4 auto-fixed (Rule 1 — bugs de compilação e comportamento de teste)
**Impact on plan:** Nenhum scope creep — todas as correções são necessárias para o comportamento correto do plano.

## Known Stubs

Nenhum stub acidental. `ClientProfileDto` retorna dados reais do aggregate `Client` — não há dados hardcoded ou mock na implementação de produção.

## Threat Flags

Nenhuma nova superfície de ataque introduzida além das mitigadas explicitamente neste plano (T-6-06 a T-6-10 documentadas no threat model do plano).

## Self-Check

Verificando artefatos criados e commits:

- [x] `src/Onboarding.Infrastructure/Keycloak/KeycloakAuthException.cs` existe
- [x] `src/Onboarding.Infrastructure/Keycloak/KeycloakTokenService.cs` existe — contém `grant_type"] = "password"` e `grant_type"] = "refresh_token"`
- [x] `src/Onboarding.API/Controllers/AuthController.cs` existe — contém `[HttpPost("login")]`, `[HttpPost("refresh")]`, `catch (KeycloakAuthException`
- [x] `src/Onboarding.API/Controllers/ClientsController.cs` existe — contém `[Authorize]`, `User.FindFirst("email")`, `GetByEmailAsync(email, ct)`
- [x] `src/Onboarding.Infrastructure/DependencyInjection.cs` contém `"keycloak-token"` e `AddScoped<IKeycloakTokenService, KeycloakTokenService>()`
- [x] `src/Onboarding.Application/DependencyInjection.cs` contém `LoginCommandHandler` e `RefreshTokenCommandHandler`
- [x] `src/Onboarding.API/appsettings.json` contém `"PublicClientId"`
- [x] Commit `a940fb7` — Task 1
- [x] Commit `fae3c19` — Task 2
- [x] `dotnet build Onboarding.slnx` — 0 erros
- [x] `dotnet test tests/Onboarding.API.Tests/` — 42 aprovados, 0 falhas

## Self-Check: PASSED

---
*Phase: 06-authentication-api*
*Completed: 2026-04-06*
