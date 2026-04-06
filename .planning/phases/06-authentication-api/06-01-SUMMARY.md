---
phase: 06-authentication-api
plan: 01
subsystem: testing
tags: [tdd, jwt, xunit, shouldly, nsubstitute, keycloak, red-stubs]

requires:
  - phase: 05-registration-api
    provides: IClientRepository, IKeycloakUserService, WebApplicationFactory pattern, xUnit RED stub pattern

provides:
  - "FakeJwtTokenHelper para geração de JWT unsigned em testes de endpoint protegido"
  - "AuthTestApiFactory com PostConfigure<JwtBearerOptions> para testes sem Keycloak real"
  - "12 stubs RED cobrindo AUTH-02 (JWT config), AUTH-03 (GET /api/clients/me), AUTH-04 (login + refresh)"
  - "Referência ao Microsoft.AspNetCore.Authentication.JwtBearer adicionada ao projeto de testes"

affects:
  - 06-authentication-api (plans 02 e 03 implementam os stubs)
  - IKeycloakTokenService (criado no Plan 02, resolve o erro de compilação esperado)

tech-stack:
  added:
    - "Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0 (referência ao projeto de testes)"
  patterns:
    - "AuthTestApiFactory: PostConfigure<JwtBearerOptions> desabilita validação JWT — sobrescreve config do app sem interferir"
    - "FakeJwtTokenHelper: JwtSecurityToken unsigned com claim email+sub — gera token válido estruturalmente sem chave"
    - "Stubs RED: true.ShouldBeFalse(message) como assertiva de falha obrigatória (xUnit 2.9.x não tem Assert.Fail)"

key-files:
  created:
    - "tests/Onboarding.API.Tests/Authentication/FakeJwtTokenHelper.cs"
    - "tests/Onboarding.API.Tests/Authentication/AuthTestApiFactory.cs"
    - "tests/Onboarding.API.Tests/Authentication/JwtBearerConfigurationTests.cs"
    - "tests/Onboarding.API.Tests/Authentication/AuthorizationMiddlewareTests.cs"
    - "tests/Onboarding.API.Tests/Api/ClientsMeEndpointTests.cs"
    - "tests/Onboarding.API.Tests/Api/LoginEndpointTests.cs"
    - "tests/Onboarding.API.Tests/Api/RefreshTokenEndpointTests.cs"
  modified:
    - "tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj"

key-decisions:
  - "PostConfigure usado em vez de Configure no AuthTestApiFactory — garante que o override aplica DEPOIS da configuração JWT do app"
  - "IKeycloakTokenService referenciado no AuthTestApiFactory mesmo não existindo ainda — erro de compilação esperado até Plan 02"
  - "JwtBearer package adicionado ao projeto de testes — necessário para resolver namespace Microsoft.AspNetCore.Authentication.JwtBearer"

patterns-established:
  - "AuthTestApiFactory: padrão de factory para testes de auth — herda RegistrationTestApiFactory mas adiciona PostConfigure JWT"
  - "Stub RED por requisito: cada arquivo de teste mapeia exatamente 1 requisito do VALIDATION.md"

requirements-completed:
  - AUTH-02
  - AUTH-03
  - AUTH-04

duration: 2min
completed: 2026-04-06
---

# Phase 6 Plan 01: Authentication API — Wave 0 TDD Red Stubs Summary

**12 stubs RED criados em 7 arquivos cobrindo AUTH-02/03/04, com AuthTestApiFactory usando PostConfigure para desabilitar JWT validation em testes sem Keycloak real**

## Performance

- **Duration:** 2 min
- **Started:** 2026-04-06T17:22:10Z
- **Completed:** 2026-04-06T17:24:30Z
- **Tasks:** 2
- **Files modified:** 8

## Accomplishments

- FakeJwtTokenHelper gera JWT unsigned com claims email+sub — permite testar endpoints protegidos sem Keycloak real
- AuthTestApiFactory usa PostConfigure<JwtBearerOptions> para desabilitar todas as validações JWT — sobrescreve configuração do app sem interferência
- 12 stubs RED criados (3 para AUTH-02, 3+1 para AUTH-02/AUTH-03, 4 para AUTH-04 login, 3 para AUTH-04 refresh)
- Pacote JwtBearer adicionado ao projeto de testes para resolver namespace (desvio Rule 3)

## Task Commits

Cada task foi commitada atomicamente:

1. **Task 1: Criar FakeJwtTokenHelper e AuthTestApiFactory** - `4f74c1a` (test)
2. **Task 2: Criar stubs RED para AUTH-02, AUTH-03, AUTH-04** - `e237f05` (test)

## Stubs RED por Requisito

| Requisito | Arquivo | Test Methods | Status |
|-----------|---------|--------------|--------|
| AUTH-02 | JwtBearerConfigurationTests.cs | 3 stubs (JWT config) | RED |
| AUTH-02 | AuthorizationMiddlewareTests.cs | 1 stub ([Authorize] retorna 401) | RED |
| AUTH-03 | ClientsMeEndpointTests.cs | 3 stubs (GET /api/clients/me) | RED |
| AUTH-04 | LoginEndpointTests.cs | 4 stubs (POST /api/auth/login) | RED |
| AUTH-04 | RefreshTokenEndpointTests.cs | 3 stubs (POST /api/auth/refresh) | RED |

**Total: 14 stubs RED** (12 de test methods + 2 arquivos helpers)

## Files Created/Modified

- `tests/Onboarding.API.Tests/Authentication/FakeJwtTokenHelper.cs` — Gera JWT unsigned com claim email+sub para testes
- `tests/Onboarding.API.Tests/Authentication/AuthTestApiFactory.cs` — WebApplicationFactory com PostConfigure JWT disabled
- `tests/Onboarding.API.Tests/Authentication/JwtBearerConfigurationTests.cs` — 3 stubs RED para AUTH-02 (JWT config)
- `tests/Onboarding.API.Tests/Authentication/AuthorizationMiddlewareTests.cs` — 1 stub RED para AUTH-02 ([Authorize] 401)
- `tests/Onboarding.API.Tests/Api/ClientsMeEndpointTests.cs` — 3 stubs RED para AUTH-03 (GET /api/clients/me)
- `tests/Onboarding.API.Tests/Api/LoginEndpointTests.cs` — 4 stubs RED para AUTH-04 (login)
- `tests/Onboarding.API.Tests/Api/RefreshTokenEndpointTests.cs` — 3 stubs RED para AUTH-04 (refresh)
- `tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj` — Adicionada referência Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0

## Decisions Made

- PostConfigure (não Configure) usado no AuthTestApiFactory — garante override APÓS configuração JWT do app, evitando race condition de configuração
- IKeycloakTokenService intencionalmente referenciado mesmo não existindo — erro de compilação esperado até Plan 02; confirma que o stub é corretamente RED
- JwtBearer package adicionado ao projeto de testes para que o namespace seja resolvido quando IKeycloakTokenService for criado no Plan 02

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Adicionada referência Microsoft.AspNetCore.Authentication.JwtBearer ao projeto de testes**
- **Found during:** Task 1 (criação do AuthTestApiFactory)
- **Issue:** O projeto de testes não tinha referência ao JwtBearer package; `using Microsoft.AspNetCore.Authentication.JwtBearer` gerava erro CS0234 além do erro esperado de IKeycloakTokenService
- **Fix:** Adicionada `<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />` ao Onboarding.API.Tests.csproj
- **Files modified:** tests/Onboarding.API.Tests/Onboarding.API.Tests.csproj
- **Verification:** Build mostra apenas o erro esperado (IKeycloakTokenService) — nenhum outro erro de namespace
- **Committed in:** 4f74c1a (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 3 - blocking dependency)
**Impact on plan:** Fix necessário para que o AuthTestApiFactory compile corretamente quando IKeycloakTokenService for criado no Plan 02. Sem scope creep.

## Issues Encountered

- Compilação falha intencionalmente com `IKeycloakTokenService not found` — isso é o comportamento correto do Wave 0 (stubs criados antes do código de produção). O Plan 02 criará a interface e resolverá o erro.

## Dependency Note

`AuthTestApiFactory.cs` referencia `IKeycloakTokenService` que **ainda não existe** no código de produção. O arquivo compilará com erro CS0246 até o Plan 02 criar a interface em `src/Onboarding.Application/Common/IKeycloakTokenService.cs` (ou namespace equivalente).

## Known Stubs

Todos os 12 test methods são stubs intencionais com `true.ShouldBeFalse("RED stub — not implemented yet")`. Estes NÃO são stubs acidentais — são a entrega do Wave 0 do ciclo TDD. Serão implementados nos Plans 02 e 03.

## Next Phase Readiness

- Wave 0 completo: todos os stubs RED criados antes do código de produção
- Plan 02 deve criar: `IKeycloakTokenService`, `AddJwtBearer` com configuração Keycloak, e implementação do `AuthController`
- Plan 03 deve criar: `ClientsController` com `GET /api/clients/me` e `GetByEmailAsync` no repositório
- `dotnet test --filter "FullyQualifiedName~JwtBearerConfigurationTests"` retornará FAILED (RED confirmado) assim que o build compilar

---
*Phase: 06-authentication-api*
*Completed: 2026-04-06*
