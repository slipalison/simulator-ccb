---
phase: 06-authentication-api
verified: 2026-04-06T19:30:00Z
status: human_needed
score: 3/3 must-haves verified (com ressalva: SC-2 parcialmente verificável de forma automatizada)
re_verification: false
human_verification:
  - test: "Redirecionar ao login quando token ausente (SC-2)"
    expected: "Ao chamar GET /api/clients/me sem Bearer token, a aplicação completa redireciona o usuário para a tela de login"
    why_human: "A API retorna 401 corretamente (verificado em testes automatizados). O 'redirect to login' é comportamento do frontend (React + TanStack Router), que ainda não existe (Phase 7-9). Verificação end-to-end requer ambiente com frontend completo."
---

# Phase 6: Authentication API — Relatório de Verificação

**Goal da fase:** O backend pode emitir tokens JWT, proteger rotas e silenciosamente renovar access tokens expirados
**Verificado em:** 2026-04-06
**Status:** human_needed
**Re-verificação:** Não — verificação inicial

---

## Achievment do Objetivo

### Success Criteria do ROADMAP

| # | Success Criteria | Status | Evidência |
|---|-----------------|--------|-----------|
| SC-1 | Par de credenciais válido retorna access token e refresh token na resposta da API | VERIFIED | `POST /api/auth/login` implementado em `AuthController.cs`; teste `Login_WithValidCredentials_Returns200WithTokens` passa |
| SC-2 | Chamar GET /api/clients/me sem Bearer token retorna 401 e redireciona ao login | PARTIAL | 401 verificado por teste automatizado `GetMe_WithoutToken_Returns401`; redirecionamento ao login é comportamento frontend (Phase 9) — necessita verificação humana |
| SC-3 | Quando access token está próximo do vencimento, o refresh token obtém novo access token sem re-autenticar o usuário | VERIFIED | `POST /api/auth/refresh` implementado; teste `Refresh_WithValidRefreshToken_Returns200WithNewTokens` passa |

**Pontuação:** 3/3 truths verificadas (SC-2 necessita verificação humana para o componente frontend)

---

### Truths Observáveis

| # | Truth | Status | Evidência |
|---|-------|--------|-----------|
| 1 | POST /api/auth/login com credenciais válidas retorna 200 com access_token + refresh_token | VERIFIED | `AuthController.Login` delega a `LoginCommandHandler` que chama `IKeycloakTokenService.ExchangePasswordAsync`; teste verde |
| 2 | POST /api/auth/login com credenciais inválidas retorna 401 com mensagem genérica (SEC-08) | VERIFIED | `KeycloakAuthException` capturada → 401 genérico `"Invalid credentials."` sem revelar dados de usuário; teste verde |
| 3 | POST /api/auth/login com campo ausente retorna 422 (FluentValidation) | VERIFIED | `LoginRequest` com `string?` nullable; FluentValidation captura antes do handler; testes `Returns422` passam |
| 4 | POST /api/auth/refresh com refresh_token válido retorna 200 com novos tokens | VERIFIED | `AuthController.Refresh` chama `IKeycloakTokenService.RefreshTokenAsync`; teste verde |
| 5 | POST /api/auth/refresh com refresh_token inválido retorna 401 genérico | VERIFIED | `KeycloakAuthException` → 401 `"Invalid or expired refresh token."`; teste verde |
| 6 | GET /api/clients/me sem Bearer token retorna 401 | VERIFIED | `[Authorize]` em `ClientsController.GetMe`; `UseAuthentication()` → `UseAuthorization()` ordem correta em `Program.cs`; teste verde |
| 7 | GET /api/clients/me com Bearer token válido retorna 200 com perfil do cliente | VERIFIED | Lookup por email claim (`User.FindFirst("email")`); `GetByEmailAsync` retorna `Client`; mapeado para `ClientProfileDto`; teste verde |
| 8 | GET /api/clients/me com token válido mas cliente não no DB retorna 404 genérico | VERIFIED | `GetByEmailAsync` retorna null → 404 com `ProblemDetails` genérico; teste verde |
| 9 | JWT Bearer configurado com Authority Keycloak, ValidateAudience=false, MapInboundClaims=false | VERIFIED | `Program.cs` linhas 93-110; testes `JwtBearerConfigurationTests` passam |
| 10 | Redirecionamento ao login quando token ausente (componente frontend) | NEEDS HUMAN | Comportamento do frontend ainda não implementado (Phase 7-9) |

---

### Artefatos Obrigatórios

| Artefato | Fornece | Status | Detalhes |
|----------|---------|--------|---------|
| `tests/Onboarding.API.Tests/Authentication/FakeJwtTokenHelper.cs` | JWT falso para testes sem Keycloak real | VERIFIED | Existe, contém `new JwtSecurityToken(`, usado em `ClientsMeEndpointTests` |
| `tests/Onboarding.API.Tests/Authentication/AuthTestApiFactory.cs` | WebApplicationFactory com JWT validation desabilitado | VERIFIED | Existe, `PostConfigure<JwtBearerOptions>` presente, `ValidateIssuerSigningKey = false`, `RequireSignedTokens = false` |
| `tests/Onboarding.API.Tests/Authentication/JwtBearerConfigurationTests.cs` | Testes de configuração JWT (AUTH-02) | VERIFIED | Existe, 3 testes verdes |
| `tests/Onboarding.API.Tests/Authentication/AuthorizationMiddlewareTests.cs` | Teste 401 sem token (AUTH-02) | VERIFIED | Existe, 1 teste verde |
| `tests/Onboarding.API.Tests/Api/ClientsMeEndpointTests.cs` | Testes GET /api/clients/me (AUTH-03) | VERIFIED | Existe, 3 testes verdes |
| `tests/Onboarding.API.Tests/Api/LoginEndpointTests.cs` | Testes POST /api/auth/login (AUTH-04) | VERIFIED | Existe, 4 testes verdes |
| `tests/Onboarding.API.Tests/Api/RefreshTokenEndpointTests.cs` | Testes POST /api/auth/refresh (AUTH-04) | VERIFIED | Existe, 3 testes verdes |
| `src/Onboarding.Application/Common/IKeycloakTokenService.cs` | Interface de abstração sobre endpoint de token | VERIFIED | Existe, contém `ExchangePasswordAsync` e `RefreshTokenAsync` |
| `src/Onboarding.Application/Auth/DTOs/TokenResponse.cs` | DTO de resposta de token (6 campos) | VERIFIED | Existe, campos: AccessToken, RefreshToken, ExpiresIn, TokenType, RefreshExpiresIn, Scope |
| `src/Onboarding.Infrastructure/Keycloak/KeycloakTokenService.cs` | Implementação ROPC via IHttpClientFactory | VERIFIED | Existe, usa `grant_type=password` e `grant_type=refresh_token`, `keycloak-token` named client |
| `src/Onboarding.Infrastructure/Keycloak/KeycloakAuthException.cs` | Exception para erros Keycloak (mapeada para 401) | VERIFIED | Existe no diretório Keycloak da Infrastructure |
| `src/Onboarding.API/Controllers/AuthController.cs` | POST /api/auth/login e POST /api/auth/refresh | VERIFIED | Existe, `[HttpPost("login")]` e `[HttpPost("refresh")]`, captura `KeycloakAuthException` |
| `src/Onboarding.API/Controllers/ClientsController.cs` | GET /api/clients/me com [Authorize] | VERIFIED | Existe, `[Authorize]`, `User.FindFirst("email")`, `GetByEmailAsync` |
| `src/Onboarding.Application/Auth/Commands/LoginCommand.cs` | Command + Handler para login CQRS manual | VERIFIED | Existe, `LoginCommandHandler` chama `IKeycloakTokenService.ExchangePasswordAsync` |
| `src/Onboarding.Application/Auth/Commands/RefreshTokenCommand.cs` | Command + Handler para refresh CQRS manual | VERIFIED | Existe no diretório Commands |

---

### Verificação de Key Links

| De | Para | Via | Status | Detalhes |
|----|------|-----|--------|---------|
| `AuthController.cs` | `LoginCommandHandler.cs` | `ICommandHandler<LoginCommand, TokenResponse>` via DI | WIRED | `Application/DependencyInjection.cs` registra `LoginCommandHandler`; controller injeta via construtor |
| `LoginCommandHandler.cs` | `KeycloakTokenService.cs` | `IKeycloakTokenService.ExchangePasswordAsync` | WIRED | `Infrastructure/DependencyInjection.cs` registra `KeycloakTokenService`; handler recebe `IKeycloakTokenService` |
| `ClientsController.cs` | `IClientRepository.GetByEmailAsync` | `User.FindFirst("email")` → `GetByEmailAsync(email, ct)` | WIRED | Código em `ClientsController.GetMe` linha 44 faz chamada direta |
| `Program.cs` | `IKeycloakTokenService` | `AddAuthentication().AddJwtBearer()` com Authority | WIRED | `UseAuthentication()` antes de `UseAuthorization()` antes de `MapControllers()` confirmado |
| `AuthTestApiFactory.cs` | `FakeJwtTokenHelper.cs` | Uso de `FakeJwtTokenHelper.GenerateFakeJwt` em testes | WIRED | `ClientsMeEndpointTests` usa `FakeJwtTokenHelper.GenerateFakeJwt(TestEmail)` |

---

### Data-Flow Trace (Nível 4)

| Artefato | Variável de Dados | Fonte | Produz Dados Reais | Status |
|----------|-------------------|-------|-------------------|--------|
| `AuthController.Login` | `tokens` (TokenResponse) | `_loginHandler.HandleAsync` → `IKeycloakTokenService.ExchangePasswordAsync` | Sim — resposta JSON do Keycloak deserializada para TokenResponse | FLOWING |
| `ClientsController.GetMe` | `client` (Client?) | `_repository.GetByEmailAsync(email, ct)` → EF Core query `FirstOrDefaultAsync` | Sim — query real no banco via EF Core | FLOWING |

---

### Verificações Comportamentais (Spot-Checks)

| Comportamento | Resultado | Status |
|---------------|-----------|--------|
| `dotnet test tests/Onboarding.API.Tests/` | 42 aprovados, 0 falhas, 2 ignorados | PASS |
| Testes de auth (14 testes GREEN) | JwtBearerConfigurationTests(3) + AuthorizationMiddlewareTests(1) + LoginEndpointTests(4) + RefreshTokenEndpointTests(3) + ClientsMeEndpointTests(3) = 14 | PASS |
| Build sem erros de compilação | 0 erros, avisos MSB3277 de versão conflitante de EFCore (pré-existente) | PASS |

---

### Cobertura de Requisitos

| Requisito | Planos | Descrição | Status | Evidência |
|-----------|--------|-----------|--------|-----------|
| AUTH-02 | 06-01, 06-02, 06-03 | JWT Bearer configurado; access token e refresh token emitidos via ROPC | SATISFIED | `AddJwtBearer` em Program.cs; `AuthController`; 7 testes verdes cobrindo login/refresh |
| AUTH-03 | 06-01, 06-02, 06-03 | Rota protegida GET /api/clients/me retorna perfil do cliente autenticado | SATISFIED | `ClientsController` com `[Authorize]`; lookup por email claim; 3 testes verdes |
| AUTH-04 | 06-01, 06-03 | Endpoint POST /api/auth/refresh renova access token sem re-autenticar | SATISFIED | `AuthController.Refresh` + `RefreshTokenCommandHandler` + `KeycloakTokenService.RefreshTokenAsync`; 3 testes verdes |

---

### Anti-Padrões Encontrados

| Arquivo | Linha | Padrão | Severidade | Impacto |
|---------|-------|--------|------------|---------|
| `src/Onboarding.API/Onboarding.API.csproj` | — | Aviso MSB3277: conflito de versão `Microsoft.EntityFrameworkCore.Relational` 10.0.4 vs 10.0.5 | Aviso (pre-existente) | Nenhum — testes passam; conflito pré-existente da fase 5, não introduzido pela fase 6 |

Nenhum stub acidental encontrado nos arquivos de produção criados pela fase 6. Os únicos `true.ShouldBeFalse` eram os stubs RED do Plan 01, que foram convertidos para GREEN no Plan 03.

---

### Verificação Humana Necessária

#### 1. Redirecionamento ao login (SC-2 — componente frontend)

**Teste:** Com o frontend completo (Phase 9) em execução, tentar acessar `/profile` ou qualquer rota protegida sem token de autenticação
**Esperado:** O usuário é redirecionado automaticamente para a tela de login sem ver a tela de perfil
**Por que humano:** O backend retorna 401 corretamente (verificado automaticamente). O "redirect to login" é responsabilidade do TanStack Router + lógica de guarda de rota no frontend React, que será implementado nas Phases 7-9. Não é possível verificar programaticamente sem o frontend.

---

### Resumo de Gaps

Nenhum gap bloqueante encontrado. A fase 6 entregou todos os artefatos planejados e todos os 14 testes GREEN confirmam que o objetivo foi alcançado do lado do backend.

**Única ressalva:** O Success Criteria 2 do ROADMAP menciona "redirects to login" — essa parte é responsabilidade do frontend e está corretamente adiada para as Phases 7/8/9. O backend faz a sua parte (retorna 401), conforme verificado pelo teste `GetMe_WithoutToken_Returns401`.

**Aviso de versão pré-existente:** O conflito de versão MSB3277 no `Microsoft.EntityFrameworkCore.Relational` não é um gap da fase 6 — é pré-existente desde a fase 5. Não afeta a compilação nem os testes.

---

### Itens Adiados (Deferred)

| # | Item | Adiado para | Evidência |
|---|------|-------------|-----------|
| 1 | Redirecionamento visual ao login após 401 | Phase 9 (Login UI) | Phase 9 SC-1: "on success navigates to the profile screen" e lógica de rota protegida implícita no TanStack Router |

---

## Resumo da Suite de Testes

```
dotnet test tests/Onboarding.API.Tests/
Aprovado!  – Com falha: 0, Aprovado: 42, Ignorado: 2, Total: 44, Duração: 1m 6s
```

Os 2 ignorados são testes de observabilidade da fase 4 (`TracePropagationTests`) que foram marcados com `[Skip]` por necessitarem de infraestrutura OTel completa — não relacionados à fase 6.

---

_Verificado em: 2026-04-06_
_Verificador: Claude (gsd-verifier)_
