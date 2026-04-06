---
phase: 06-authentication-api
plan: 02
subsystem: authentication-contracts
tags: [jwt, keycloak, ropc, contracts, infrastructure]

requires:
  - phase: 06-authentication-api
    plan: 01
    provides: AuthTestApiFactory, FakeJwtTokenHelper, RED stubs for AUTH-02/03/04

provides:
  - "IKeycloakTokenService com ExchangePasswordAsync e RefreshTokenAsync na camada Application"
  - "TokenResponse DTO com 6 campos para mapear resposta do token endpoint do Keycloak"
  - "IClientRepository.GetByEmailAsync — contrato para busca por email com normalização"
  - "ClientRepository.GetByEmailAsync — implementação com ToLowerInvariant + Email.Create()"
  - "AddAuthentication().AddJwtBearer() configurado em Program.cs com Authority, ValidateAudience=false, MapInboundClaims=false"
  - "UseAuthentication() -> UseAuthorization() -> MapControllers() na ordem correta no pipeline"
  - "AuthTestApiFactory do Plan 01 agora compila — IKeycloakTokenService existe"

affects:
  - 06-authentication-api (plan 03 implementa controllers e handlers que dependem destes contratos)
  - IClientRepository (novo método GetByEmailAsync disponível)
  - Program.cs (JWT middleware ativo — rotas [Authorize] retornam 401 sem token)

tech-stack:
  added:
    - "Microsoft.AspNetCore.Authentication.JwtBearer 10.0.5 (Onboarding.API.csproj)"
    - "Microsoft.AspNetCore.Authentication.JwtBearer 10.0.5 (Onboarding.API.Tests.csproj — atualizado de 10.0.0)"
  patterns:
    - "IKeycloakTokenService: abstração Application layer sobre token endpoint — permite unit-testing sem HTTP"
    - "TokenResponse: sealed record com 6 campos mapeando resposta JSON do Keycloak"
    - "GetByEmailAsync: normaliza via Email.Create() que chama ToLowerInvariant() internamente"
    - "AddJwtBearer: Authority via OIDC auto-discovery, ValidateAudience=false (ROPC tokens), MapInboundClaims=false"

key-files:
  created:
    - "src/Onboarding.Application/Common/IKeycloakTokenService.cs"
    - "src/Onboarding.Application/Auth/DTOs/TokenResponse.cs"
  modified:
    - "src/Onboarding.Domain/Repositories/IClientRepository.cs"
    - "src/Onboarding.Infrastructure/Repositories/ClientRepository.cs"
    - "src/Onboarding.API/Program.cs"
    - "src/Onboarding.API/Onboarding.API.csproj"
    - "tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj"

key-decisions:
  - "GetByEmailAsync usa Email.Create() + comparação de value object (mesmo padrão do ExistsByEmailAsync) — consistência com o repositório existente"
  - "ValidateAudience=false obrigatório: tokens ROPC do Keycloak têm aud: ['account'], não a nossa API — sem isso todos os requests retornariam 401"
  - "MapInboundClaims=false obrigatório: sem isso User.FindFirst('email') retorna null (claim mapeado para URI XML namespace pelo JwtBearer default)"
  - "Authority usa ?? throw InvalidOperationException — falha explícita na inicialização se Keycloak:RealmUrl ausente (T-6-02)"
  - "JwtBearer atualizado para 10.0.5 no projeto de testes — resolve NU1605 downgrade causado pelo upgrade no projeto API"

requirements-completed:
  - AUTH-02
  - AUTH-03

duration: 5min
completed: 2026-04-06
---

# Phase 6 Plan 02: Authentication Contracts and JWT Bearer Configuration Summary

**Contratos de autenticação criados (IKeycloakTokenService, TokenResponse, GetByEmailAsync) e JWT Bearer configurado em Program.cs com Authority Keycloak, ValidateAudience=false e MapInboundClaims=false**

## Performance

- **Duration:** 5 min
- **Started:** 2026-04-06T18:00:00Z
- **Completed:** 2026-04-06T18:05:00Z
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments

- `IKeycloakTokenService` criada em `src/Onboarding.Application/Common/` com `ExchangePasswordAsync` e `RefreshTokenAsync` — abstração da camada Application sobre o token endpoint ROPC do Keycloak
- `TokenResponse` sealed record criado em `src/Onboarding.Application/Auth/DTOs/` com 6 campos mapeando a resposta JSON do Keycloak
- `GetByEmailAsync` adicionado à interface `IClientRepository` e implementado em `ClientRepository` usando `Email.Create()` com normalização `ToLowerInvariant()`
- `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer()` configurado em Program.cs com Authority, ValidateAudience=false (D-05), MapInboundClaims=false (D-04)
- Pipeline de request configurado com ordem correta: `UseAuthentication()` → `UseAuthorization()` → `MapControllers()`
- `AuthTestApiFactory` do Plan 01 agora compila — o erro CS0246 esperado (IKeycloakTokenService not found) foi resolvido
- `dotnet build Onboarding.slnx` passa com 0 erros

## Task Commits

1. **Task 1: IKeycloakTokenService, TokenResponse, GetByEmailAsync** — `2afd905` (feat)
2. **Task 2: JwtBearer instalado e configurado em Program.cs** — `9f1c1fe` (feat)

## Contratos Disponíveis para Plan 03

| Contrato | Localização | Usado por |
|----------|-------------|-----------|
| `IKeycloakTokenService.ExchangePasswordAsync` | Application/Common | LoginCommandHandler (Plan 03) |
| `IKeycloakTokenService.RefreshTokenAsync` | Application/Common | RefreshTokenCommandHandler (Plan 03) |
| `TokenResponse` | Application/Auth/DTOs | AuthController (Plan 03) |
| `IClientRepository.GetByEmailAsync` | Domain/Repositories | GetClientProfileQueryHandler (Plan 03) |

## Configuração JWT Bearer

```
Authority: builder.Configuration["Keycloak:RealmUrl"]  (auto-discovery OIDC)
ValidateAudience: false   (ROPC tokens têm aud: ["account"], não a nossa API)
MapInboundClaims: false   (preserva claim names do Keycloak — ex: "email" não vira URI)
```

## Ameaças Mitigadas

| Threat ID | Severidade | Status |
|-----------|-----------|--------|
| T-6-02 | Critical | Mitigado — `?? throw InvalidOperationException` na configuração do Authority |
| T-6-03 | High | Mitigado — UseAuthentication() antes de UseAuthorization() confirmado (linhas 121/122) |
| T-6-04 | High | Mitigado — MapInboundClaims = false configurado explicitamente |
| T-6-05 | Medium | Mitigado — ValidateAudience = false configurado explicitamente |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Atualização de JwtBearer 10.0.0 → 10.0.5 no projeto de testes**
- **Found during:** Task 2 (verificação do build do projeto de testes)
- **Issue:** Erro NU1605 "Downgrade de pacote detectado: Microsoft.AspNetCore.Authentication.JwtBearer de 10.0.5 para 10.0.0" — projeto de testes tinha 10.0.0, API instalou 10.0.5; NuGet trata como downgrade e falha o build
- **Fix:** Atualizada `PackageReference` no `Onboarding.API.Tests.csproj` de `10.0.0` para `10.0.5`
- **Files modified:** `tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj`
- **Commit:** `9f1c1fe` (incluído no commit da Task 2)

**Total deviations:** 1 auto-fixed (Rule 1 — bug de versão conflitante)

## Known Stubs

Nenhum stub acidental. Os 14 stubs RED do Plan 01 continuam RED intencionalmente — nenhum controller ou handler foi implementado neste plano. Plan 03 implementa os handlers e controllers que farão os stubs passarem.

## Threat Flags

Nenhuma nova superfície de ataque introduzida. As configurações JWT adicionadas são mitigações explícitas das ameaças T-6-02 a T-6-05 documentadas no threat model do plano.

## Self-Check

Verificando artefatos criados e commits:

- [x] `src/Onboarding.Application/Common/IKeycloakTokenService.cs` existe
- [x] `src/Onboarding.Application/Auth/DTOs/TokenResponse.cs` existe
- [x] `IClientRepository.cs` contém `GetByEmailAsync`
- [x] `ClientRepository.cs` contém `GetByEmailAsync` com `ToLowerInvariant()`
- [x] `Program.cs` contém `AddAuthentication`, `ValidateAudience = false`, `MapInboundClaims = false`
- [x] `Program.cs` contém `UseAuthentication()` antes de `UseAuthorization()` antes de `MapControllers()`
- [x] Commit `2afd905` — Task 1
- [x] Commit `9f1c1fe` — Task 2
- [x] `dotnet build Onboarding.slnx` — 0 erros

## Self-Check: PASSED

---
*Phase: 06-authentication-api*
*Completed: 2026-04-06*
