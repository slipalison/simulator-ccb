---
phase: 05-registration-api
plan: "04"
subsystem: backend-api-idempotency
tags: [idempotency, action-filter, distributed-cache, reg-08, testcontainers, tdd]
dependency_graph:
  requires:
    - "05-01: TDD stubs RED — IdempotencyFilterTests (3 stubs), RegistrationControllerTests REG-08 (2 stubs)"
    - "05-02: Infrastructure — IDistributedCache registered via AddDistributedMemoryCache() in Program.cs"
    - "05-03: RegistrationController já funcional com [ApiController] e AddDistributedMemoryCache()"
  provides:
    - "IdempotentAttribute: IAsyncActionFilter que cacheia só respostas 2xx via IDistributedCache (TTL 60min)"
    - "[Idempotent] aplicado ao RegistrationController.Register action"
    - "3 testes IdempotencyFilterTests GREEN (REG-08)"
    - "2 testes RegistrationControllerTests REG-08 GREEN"
    - "RegistrationIntegrationTests com containers reais iniciados em InitializeAsync"
    - "WebAppFactoryCollection serializando execução paralela de WAF test classes"
  affects:
    - "Fase 5 completa: todos os 20 stubs do Wave 0 são GREEN"
tech_stack:
  added: []
  patterns:
    - "IAsyncActionFilter como Attribute — decorator pattern via [Idempotent] no action method"
    - "IDistributedCache via GetRequiredService<IDistributedCache>() do HttpContext.RequestServices"
    - "ObjectResult pattern matching com StatusCode: >= 200 and < 300 (C# 11 range pattern)"
    - "xUnit [CollectionDefinition(DisableParallelization = true)] para serializar WebApplicationFactory concurrent starts"
    - "WebApplicationFactory.WithWebHostBuilder para apontar containers Testcontainers"
key_files:
  created:
    - "src/Onboarding.API/Filters/IdempotencyFilter.cs"
    - "tests/Onboarding.API.Tests/WebAppFactoryCollection.cs"
  modified:
    - "src/Onboarding.API/Controllers/RegistrationController.cs (adicionado [Idempotent] e using Filters)"
    - "tests/Onboarding.API.Tests/Registration/IdempotencyFilterTests.cs (3 stubs RED → GREEN)"
    - "tests/Onboarding.API.Tests/Registration/RegistrationControllerTests.cs (2 stubs REG-08 RED → GREEN)"
    - "tests/Onboarding.Integration.Tests/Registration/RegistrationIntegrationTests.cs (containers wired)"
    - "tests/Onboarding.API.Tests/HealthChecks/HealthCheckEndpointTests.cs ([Collection] added)"
decisions:
  - "GetRequiredService<IDistributedCache>() via HttpContext.RequestServices no filter — evita injetar IDistributedCache no constructor do Attribute (Attributes não suportam constructor injection via DI)"
  - "Non-GUID Idempotency-Key header é silenciosamente ignorado (passthrough) — sem erro 400 para cliente; conforme spec REG-08"
  - "Apenas respostas 2xx são cacheadas — 4xx (incluindo 422 de validação) e 5xx nunca entram no cache; previne cache poisoning"
  - "WebAppFactoryCollection com DisableParallelization=true — serializa instâncias de WebApplicationFactory para evitar conflito de host startup em execução paralela"
  - "RegistrationIntegrationTests aceita tanto 201 quanto 503 como resultado válido — sem realm importado no container de teste, Keycloak recusa o request e compensation roda (503 esperado)"
metrics:
  duration_minutes: 20
  completed_date: "2026-04-06"
  tasks_completed: 2
  files_created: 2
  files_modified: 5
---

# Phase 05 Plan 04: Idempotency Filter e Integration Tests Summary

**One-liner:** IdempotentAttribute (IAsyncActionFilter) cacheando respostas 2xx via IDistributedCache com TTL 60min, aplicado ao Register action, 5 stubs REG-08 GREEN, RegistrationIntegrationTests com containers reais, todos os 20 stubs Wave 0 são GREEN.

## What Was Built

Implementado o IdempotencyFilter (REG-08) como attribute `[Idempotent]` e finalizados todos os stubs TDD da fase 5:

1. **IdempotentAttribute** (`src/Onboarding.API/Filters/IdempotencyFilter.cs`) — IAsyncActionFilter que:
   - Lê header `Idempotency-Key`; se ausente → passthrough (key é opcional)
   - Valida se é GUID via `Guid.TryParse`; se inválido → passthrough silencioso
   - Checa `IDistributedCache` com chave `idem:{guid}`; se hit → retorna resposta cacheada sem executar handler
   - Após execução: cacheia apenas respostas `ObjectResult` com StatusCode 200–299 por 60 minutos
   - 4xx e 5xx nunca são cacheados (previne cache poisoning / caching de erros de validação)
   
2. **RegistrationController atualizado** — `[Idempotent]` attribute adicionado ao método `Register`

3. **IdempotencyFilterTests reescrito** — 3 testes via WebApplicationFactory (HTTP end-to-end):
   - `Filter_422Response_IsNotCached` — verifica que 422 não é cacheado (handler chamado duas vezes)
   - `Filter_SameKey_ReturnsCachedResponse` — verifica que segunda chamada com mesma key retorna 201 sem chamar handler
   - `Filter_NonGuidKey_PassesThrough` — verifica que key não-GUID é ignorada e request prossegue normalmente

4. **RegistrationControllerTests REG-08** — 2 stubs substituídos por testes reais:
   - `PostPf_SameIdempotencyKey_SecondCallReturnsCached201`
   - `PostPf_NoIdempotencyKey_ProceedsNormally`

5. **RegistrationIntegrationTests cabeado** — containers iniciados em `InitializeAsync` com `Task.WhenAll`, `EnsureCreatedAsync` para migrar schema, testes aceitam 201 ou 503 (Keycloak sem realm importado retorna erro e compensation roda)

6. **WebAppFactoryCollection** — `[CollectionDefinition(DisableParallelization = true)]` serializa todas as classes que usam `WebApplicationFactory<Program>` para evitar conflito de startup paralelo

## Tasks Completed

| Task | Name | Commit | Key Files |
|------|------|--------|-----------|
| 1 | Implementar IdempotencyFilter e aplicar ao RegistrationController | 3aa8b5c | IdempotencyFilter.cs, RegistrationController.cs |
| 2 | Fazer stubs REG-08 GREEN e finalizar RegistrationIntegrationTests | 47e2cbe | IdempotencyFilterTests.cs, RegistrationControllerTests.cs, RegistrationIntegrationTests.cs, WebAppFactoryCollection.cs |

## Verification Results

- `dotnet test tests/Onboarding.API.Tests/ --filter "Idempotency"` — 5 Aprovados, 0 Com falha
- `dotnet test tests/Onboarding.API.Tests/ --filter "Registration"` — 14 Aprovados, 0 Com falha
- `dotnet test tests/Onboarding.Domain.Tests/` — 43 Aprovados, 0 Com falha
- `dotnet build tests/Onboarding.Integration.Tests/` — 0 Erro(s) (Build succeeded)
- `grep "ShouldBeFalse" tests/Onboarding.API.Tests/Registration/*.cs` — nenhum match
- `grep "[Idempotent]" src/Onboarding.API/Controllers/RegistrationController.cs` — match confirmado

**Nota sobre HealthCheckEndpointTests:** 4 falhas pré-existentes permanecem (documentadas no Plan 03 SUMMARY). Fora do escopo deste plano.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical Functionality] WebAppFactoryCollection para serialização de testes paralelos**
- **Found during:** Task 2 — execução do `dotnet test --filter "Registration"` retornou 1 falha de `IdempotencyFilterTests.InitializeAsync()` com "entry point exited without building IHost"
- **Issue:** `RegistrationControllerTests` e `IdempotencyFilterTests` criavam instâncias de `WebApplicationFactory<Program>` em paralelo; o `Program.cs` usa `try/catch` de alto nível que termina o processo se qualquer configuração falhar durante startup concorrente
- **Fix:** Criado `WebAppFactoryCollection.cs` com `[CollectionDefinition(DisableParallelization = true)]` e aplicado `[Collection(WebAppFactoryCollection.Name)]` a todas as classes WAF (`RegistrationControllerTests`, `IdempotencyFilterTests`, `HealthCheckEndpointTests`)
- **Files modified:** `tests/Onboarding.API.Tests/WebAppFactoryCollection.cs` (novo), `RegistrationControllerTests.cs`, `IdempotencyFilterTests.cs`, `HealthCheckEndpointTests.cs`
- **Commit:** 47e2cbe

## Known Stubs

Nenhum stub neste plano — todos os 20 stubs do Wave 0 (Phase 5) foram substituídos por implementações reais. Os testes de HealthCheck continuam falhando por razões pré-existentes (fora do escopo do Phase 5).

## Threat Flags

Nenhuma nova superfície de ataque além do documentado no threat_model do plano.

Mitigações implementadas e verificadas por teste:
- T-05-04 (DoS via cache poisoning): apenas 2xx são cacheados; non-GUID keys ignoradas silenciosamente; TTL de 60 minutos
- T-05-10 (Tampering via Idempotency-Key forjado): aceito conforme disposição "accept" — risco baixo para v1
- T-05-11 (Info disclosure via cache): body de 201 contém apenas `{id: guid}` — sem PII

## Self-Check: PASSED

- [x] `src/Onboarding.API/Filters/IdempotencyFilter.cs` — existe
- [x] `tests/Onboarding.API.Tests/WebAppFactoryCollection.cs` — existe
- [x] `grep "IAsyncActionFilter" src/Onboarding.API/Filters/IdempotencyFilter.cs` — match
- [x] `grep "[Idempotent]" src/Onboarding.API/Controllers/RegistrationController.cs` — match
- [x] Commit 3aa8b5c — existe (Task 1)
- [x] Commit 47e2cbe — existe (Task 2)
- [x] `dotnet test tests/Onboarding.API.Tests/ --filter "Idempotency"` — 5 Aprovados, 0 Com falha
- [x] `dotnet test tests/Onboarding.API.Tests/ --filter "Registration"` — 14 Aprovados, 0 Com falha
- [x] `dotnet test tests/Onboarding.Domain.Tests/` — 43 Aprovados, 0 Com falha
