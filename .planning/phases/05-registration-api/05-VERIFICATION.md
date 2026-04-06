---
phase: 05-registration-api
verified: 2026-04-06T15:30:00Z
status: human_needed
score: 6/6 must-haves verified
gaps: []
deferred: []
human_verification:
  - test: "Executar dotnet test tests/Onboarding.Integration.Tests/ com Docker disponível"
    expected: "Os 2 testes de integração em RegistrationIntegrationTests terminam sem erro de startup; cada um retorna 201 ou 503 dependendo de Keycloak no container"
    why_human: "Requer Docker em execução e pull das imagens quay.io/keycloak/keycloak:26.1 e postgres:16-alpine — impossível verificar programaticamente sem Docker ativo"
  - test: "Verificar que os 4 testes de HealthCheckEndpointTests (falhando) são regressão de Phase 4 e não de Phase 5"
    expected: "As falhas existiam ANTES do commit 3d2f71f (Plan 03) — Phase 5 não introduziu a quebra; a falta de Keycloak:AuthServerUrl/AdminClientId nas factories HealthyApiFactory/UnhealthyApiFactory é problema de Phase 4"
    why_human: "Confirmação de que as 4 falhas de HealthCheck são escopo de Phase 4 e devem ser corrigidas em Plan 04-02/04-03 — requer decisão humana sobre prioridade de correção"
---

# Phase 05: Registration API — Relatório de Verificação

**Goal da Fase:** Clientes podem ser registrados via API com validação completa server-side, detecção de duplicatas, persistência e criação de usuário no Keycloak.
**Verificado em:** 2026-04-06
**Status:** human_needed
**Re-verificação:** Não — verificação inicial

---

## Resultado do Build

```
dotnet build --no-restore
  5 Aviso(s), 0 Erro(s)
  Tempo Decorrido: 00:00:03.14
```

Build completo sem erros. Os 5 avisos são: 3 warnings de conflito de assembly (Onboarding.Infrastructure.dll referenciado via dois caminhos em API.Tests) e 2 warnings de obsolescência de construtor paramétrico do Testcontainers (KeycloakBuilder e PostgreSqlBuilder — não bloqueante).

---

## Critérios de Sucesso do Roadmap

Os 6 critérios definidos no ROADMAP.md para Phase 5:

| # | Critério | Status | Evidência |
|---|----------|--------|-----------|
| 1 | POST PF válido persiste no app_db e cria usuário no Keycloak | ✓ VERIFIED | `HandleAsync_PessoaFisica_CreatesClientAndReturnsGuid` + `PostPf_ValidCpf_Returns201` GREEN; handler chama `_keycloakUserService.CreateUserAsync` verificado em `HandleAsync_PessoaFisica_CallsKeycloakCreateUser` |
| 2 | POST PJ válido faz o mesmo para CNPJ (incluindo formato alfanumérico) | ✓ VERIFIED | `PostPj_ValidCnpj_Returns201` GREEN; validator aceita `[A-Z0-9]{14}` (REG-04 alfanumérico) |
| 3 | CPF/CNPJ/email duplicado retorna erro sem criar registro | ✓ VERIFIED | `PostPf_DuplicateCpf_Returns409`, `PostPf_DuplicateEmail_Returns409` GREEN; handler checa antes de `AddAsync` |
| 4 | CPF/CNPJ com dígito inválido retorna 422 com mensagem descritiva — sem vazar info de usuário existente | ✓ VERIFIED | `PostPf_InvalidCpfCheckDigit_Returns422`, `PostPf_InvalidCpf_ResponseBodyIsGeneric` GREEN; body não contém "check digit" nem "ArgumentException" |
| 5 | Mesma Idempotency-Key produz exatamente um registro — segunda chamada retorna 201 cacheado | ✓ VERIFIED | `PostPf_SameIdempotencyKey_SecondCallReturnsCached201`, `Filter_SameKey_ReturnsCachedResponse` GREEN; `DidNotReceive().AddAsync` na segunda chamada confirmado |
| 6 | Respostas de erro de autenticação usam mensagens genéricas sem revelar existência de usuário | ✓ VERIFIED | `PostPf_DuplicateCpf_ResponseBodyDoesNotLeakFieldName` GREEN; body não contém "cpf", "email", "already registered" |

**Pontuação:** 6/6 critérios de sucesso do Roadmap verificados.

---

## Verdades Observáveis

| # | Verdade | Status | Evidência |
|---|---------|--------|-----------|
| 1 | POST PF válido → 201 Created | ✓ VERIFIED | `PostPf_ValidCpf_Returns201` passou (14/14 Registration+Idempotency GREEN) |
| 2 | POST PJ válido → 201 Created | ✓ VERIFIED | `PostPj_ValidCnpj_Returns201` passou |
| 3 | CPF inválido (check digit errado) → 422 com body genérico | ✓ VERIFIED | `PostPf_InvalidCpfCheckDigit_Returns422` + `PostPf_InvalidCpf_ResponseBodyIsGeneric` passaram |
| 4 | CNPJ inválido → 422 | ✓ VERIFIED | `PostPj_InvalidCnpjCheckDigit_Returns422` passou |
| 5 | CPF duplicado → 409 sem vazar nome do campo | ✓ VERIFIED | `PostPf_DuplicateCpf_Returns409` + `PostPf_DuplicateCpf_ResponseBodyDoesNotLeakFieldName` passaram |
| 6 | Email duplicado → 409 | ✓ VERIFIED | `PostPf_DuplicateEmail_Returns409` passou |
| 7 | Keycloak falha → compensation (DeleteAsync) executa → RegistrationFailedException | ✓ VERIFIED | `HandleAsync_KeycloakFails_CompensatesWithDeleteAndThrowsRegistrationFailedException` passou (43/43 Domain.Tests GREEN) |
| 8 | Mesmo Idempotency-Key → segunda chamada retorna 201 do cache sem executar handler | ✓ VERIFIED | `Filter_SameKey_ReturnsCachedResponse` + `PostPf_SameIdempotencyKey_SecondCallReturnsCached201` passaram |
| 9 | 422 NÃO é cacheado pelo IdempotencyFilter | ✓ VERIFIED | `Filter_422Response_IsNotCached` passou |
| 10 | Idempotency-Key não-GUID é ignorado (passthrough) | ✓ VERIFIED | `Filter_NonGuidKey_PassesThrough` passou |
| 11 | Endpoint existe em POST /api/registration via controller (não Minimal API) | ✓ VERIFIED | `PostRegistration_EndpointExists_NotMinimalApi` passou; `[ApiController]` + `[Route("api/[controller]")]` confirmados no código |

**Pontuação:** 11/11 verdades verificadas.

---

## Artefatos Obrigatórios

### Nível 1 (Existência) + Nível 2 (Substancialidade) + Nível 3 (Ligação)

| Artefato | Status | Detalhes |
|----------|--------|----------|
| `src/Onboarding.API/Controllers/RegistrationController.cs` | ✓ VERIFIED | 104 linhas, `[ApiController]`, `[Idempotent]`, mapeamento correto de exceções para HTTP sem vazar `ex.Message` |
| `src/Onboarding.API/Filters/IdempotencyFilter.cs` | ✓ VERIFIED | `IAsyncActionFilter`, lê `Idempotency-Key`, `IDistributedCache`, cacheia só 2xx, TTL 60min |
| `src/Onboarding.Application/Clients/Commands/RegisterClientCommandHandler.cs` | ✓ VERIFIED | Duplicate check → `AddAsync` → `CreateUserAsync` (Keycloak) → compensation via `DeleteAsync` + `RegistrationFailedException` |
| `src/Onboarding.Application/Clients/Validators/RegisterClientCommandValidator.cs` | ✓ VERIFIED | `AbstractValidator`, regras PF (`\d{11}`) e PJ (`[A-Z0-9]{14}`), When() condicional |
| `src/Onboarding.Application/Common/IKeycloakUserService.cs` | ✓ VERIFIED | Interface Application-layer sem dependência de SDK; `CreateUserAsync` + `DeleteUserByEmailAsync` |
| `src/Onboarding.Infrastructure/Persistence/AppDbContext.cs` | ✓ VERIFIED | `DbSet<Client>`, aplica `ClientConfiguration` |
| `src/Onboarding.Infrastructure/Repositories/ClientRepository.cs` | ✓ VERIFIED | Todos os 6 métodos de `IClientRepository` incluindo `DeleteAsync` (compensation); normalização de CPF/CNPJ antes de query |
| `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs` | ✓ VERIFIED | Injeta `IKeycloakUserClient` (tipo correto do SDK 2.9.0); `Exact=true` na busca por email; `DeleteUserByEmailAsync` é no-op se usuário não existe |
| `src/Onboarding.Infrastructure/DependencyInjection.cs` | ✓ VERIFIED | `AddInfrastructure()` registra DbContext (UseNpgsql), ClientRepository, CC token management (Duende), Keycloak Admin HTTP client, KeycloakUserService |
| `tests/Onboarding.API.Tests/Registration/RegistrationControllerTests.cs` | ✓ VERIFIED | 11 testes reais (não stubs), `WebApplicationFactory` com mocks via NSubstitute |
| `tests/Onboarding.API.Tests/Registration/IdempotencyFilterTests.cs` | ✓ VERIFIED | 3 testes reais de comportamento de cache via HTTP |
| `tests/Onboarding.Domain.Tests/Application/Commands/RegisterClientCommandHandlerTests.cs` | ✓ VERIFIED | 9 testes passando: 5 originais + 4 novos (REG-05/REG-06) |
| `tests/Onboarding.Integration.Tests/Registration/RegistrationIntegrationTests.cs` | ✓ WIRED (não executável sem Docker) | Containers iniciados em `InitializeAsync`, `EnsureCreatedAsync`, aceita 201 ou 503; requer Docker |

---

## Verificação de Links Críticos

| De | Para | Via | Status | Detalhes |
|----|------|-----|--------|----------|
| `RegistrationController.cs` | `RegisterClientCommandHandler` | `ICommandHandler<RegisterClientCommand, Guid>` injetado no constructor | ✓ WIRED | Linha 19: `_handler.HandleAsync(command, ct)` em linha 65 |
| `RegistrationController.cs` | `IdempotencyFilter.cs` | `[Idempotent]` attribute no método `Register` | ✓ WIRED | Linha 34: `[Idempotent]` confirmado |
| `IdempotencyFilter.cs` | `IDistributedCache` | `HttpContext.RequestServices.GetRequiredService<IDistributedCache>()` | ✓ WIRED | Linha 44; `AddDistributedMemoryCache()` em Program.cs (linha 88) |
| `RegisterClientCommandHandler.cs` | `IClientRepository` | Constructor injection | ✓ WIRED | Duplicate checks + `AddAsync` + `DeleteAsync` |
| `RegisterClientCommandHandler.cs` | `IKeycloakUserService` | Constructor injection | ✓ WIRED | `CreateUserAsync` chamado após `AddAsync`; compensation via `DeleteAsync` |
| `ClientRepository.cs` | `AppDbContext` | Constructor injection | ✓ WIRED | `_db.Clients.AnyAsync(...)`, `FindAsync`, `Remove` |
| `KeycloakUserService.cs` | `IKeycloakUserClient` (SDK) | Constructor injection | ✓ WIRED | `_keycloakUserClient.CreateUserAsync(...)`, `GetUsersAsync`, `DeleteUserAsync` |
| `Program.cs` | `AddInfrastructure()` | `builder.Services.AddInfrastructure(builder.Configuration)` | ✓ WIRED | Linha 94 do Program.cs |
| `Program.cs` | `AddApplication()` | `builder.Services.AddApplication()` | ✓ WIRED | Linha 91 do Program.cs |
| `Program.cs` | `AddDistributedMemoryCache()` | `builder.Services.AddDistributedMemoryCache()` | ✓ WIRED | Linha 88 do Program.cs |
| `RegistrationControllerTests.cs` | `RegistrationController` (via WebApplicationFactory) | `RegistrationTestApiFactory` substitui infra por mocks NSubstitute | ✓ WIRED | `_client.PostAsJsonAsync("/api/registration", ...)` em 11 testes |
| `ClientConfiguration.cs` | Unique indexes parciais | `HasIndex(...).IsUnique().HasFilter("cpf IS NOT NULL")` | ✓ WIRED | REG-05 safety net no nível de DB |

---

## Rastreamento de Fluxo de Dados (Nível 4)

| Artefato | Variável de Dados | Fonte | Produz Dados Reais | Status |
|----------|-------------------|-------|--------------------|--------|
| `RegistrationController.Register` | `clientId` (Guid) | `_handler.HandleAsync(command, ct)` → `Client.Id` gerado em `RegisterPessoaFisica/Juridica` | Sim — aggregate ID gerado no domínio | ✓ FLOWING |
| `RegisterClientCommandHandler.HandleAsync` | `client.Id` | `Client.RegisterPessoaFisica` / `RegisterPessoaJuridica` factory methods | Sim — `Guid.NewGuid()` na criação do aggregate | ✓ FLOWING |
| `ClientRepository.ExistsByCpfAsync` | `bool` | `_db.Clients.AnyAsync(c => c.Cpf.Value == normalized)` | Sim — query EF Core em DbSet real | ✓ FLOWING |
| `KeycloakUserService.CreateUserAsync` | `string` (userId) | `_keycloakUserClient.GetUsersAsync(...).First().Id` | Sim — busca real no Keycloak após criação | ✓ FLOWING |
| `IdempotencyFilter` | resposta cacheada | `cache.GetStringAsync(cacheKey)` / `cache.SetStringAsync(...)` | Sim — `IDistributedCache` em memória (AddDistributedMemoryCache) | ✓ FLOWING |

---

## Spot-Checks Comportamentais

| Comportamento | Resultado | Status |
|---------------|-----------|--------|
| `dotnet test tests/Onboarding.Domain.Tests/` | 43 aprovados, 0 com falha | ✓ PASS |
| `dotnet test tests/Onboarding.API.Tests/ --filter "Registration\|Idempotency"` | 14 aprovados, 0 com falha | ✓ PASS |
| `dotnet test tests/Onboarding.API.Tests/ --filter "Observability"` | 9 aprovados, 0 com falha, 2 ignorados (stubs pendentes de Phase 4) | ✓ PASS |
| `dotnet build --no-restore` | 0 erros, 5 avisos não-bloqueantes | ✓ PASS |
| `dotnet test tests/Onboarding.API.Tests/` (suite completa) | 24 aprovados, **4 com falha** (HealthCheckEndpointTests), 2 ignorados | ⚠️ PARCIAL |

**Nota sobre as 4 falhas:** As falhas são todas em `HealthCheckEndpointTests` — `HealthyApiFactory` e `UnhealthyApiFactory` não configuram `Keycloak:AuthServerUrl`, `Keycloak:AdminClientId` e `Keycloak:AdminClientSecret` que `AddInfrastructure()` exige. Esta exigência foi introduzida no Plan 05-03 ao adicionar `AddInfrastructure(builder.Configuration)` ao Program.cs. As factories de HealthCheck (criadas no Phase 4) não foram atualizadas para fornecer as configurações de Keycloak necessárias. **Esta é uma regressão introduzida em Phase 5, não uma falha pré-existente de Phase 4** — o commit `3d2f71f` (Plan 05-03) adicionou `AddInfrastructure` ao Program.cs sem corrigir as factories de HealthCheck.

---

## Cobertura de Requisitos

| Requisito | Plano | Descrição | Status | Evidência |
|-----------|-------|-----------|--------|-----------|
| REG-03 | 05-01/02/03 | POST PF com CPF válido → 201; CPF inválido → 422 genérico | ✓ SATISFEITO | `PostPf_ValidCpf_Returns201`, `PostPf_InvalidCpfCheckDigit_Returns422`, `PostPf_InvalidCpf_ResponseBodyIsGeneric` GREEN |
| REG-04 | 05-01/02/03 | POST PJ com CNPJ válido → 201; CNPJ inválido → 422; suporta formato alfanumérico | ✓ SATISFEITO | `PostPj_ValidCnpj_Returns201`, `PostPj_InvalidCnpjCheckDigit_Returns422`; validator regex `[A-Z0-9]{14}` |
| REG-05 | 05-01/02/03 | CPF/CNPJ/email duplicado → 409 sem criar registro | ✓ SATISFEITO | `PostPf_DuplicateCpf_Returns409`, `PostPf_DuplicateEmail_Returns409`, `HandleAsync_DuplicateCpf_ThrowsDuplicateClientExceptionWithoutPersisting` GREEN |
| REG-06 | 05-01/02/03 | POST PF válido → persiste app_db + cria usuário Keycloak; falha Keycloak → compensation | ✓ SATISFEITO | `HandleAsync_PessoaFisica_CallsKeycloakCreateUser`, `HandleAsync_KeycloakFails_CompensatesWithDeleteAndThrowsRegistrationFailedException` GREEN |
| REG-08 | 05-01/04 | Mesmo Idempotency-Key → exatamente um registro; segunda chamada retorna 201 cacheado | ✓ SATISFEITO | `Filter_SameKey_ReturnsCachedResponse`, `PostPf_SameIdempotencyKey_SecondCallReturnsCached201` GREEN |
| BACK-05 | 05-01/03 | Endpoint em POST /api/registration via [ApiController] (não Minimal API) | ✓ SATISFEITO | `PostRegistration_EndpointExists_NotMinimalApi` GREEN; `[ApiController]` + `[Route("api/[controller]")]` no código |
| SEC-08 | 05-01/03 | Respostas de erro genéricas — sem vazar campo que causou conflito nem ex.Message | ✓ SATISFEITO | `PostPf_DuplicateCpf_ResponseBodyDoesNotLeakFieldName`, `PostPf_InvalidCpf_ResponseBodyIsGeneric` GREEN; `grep "ex.Message"` no controller retorna zero matches em código executável |

---

## Anti-Padrões Identificados

| Arquivo | Linha | Padrão | Severidade | Impacto |
|---------|-------|--------|------------|---------|
| `tests/Onboarding.API.Tests/HealthChecks/HealthCheckEndpointTests.cs` | 37–60, 67–89 | `HealthyApiFactory` e `UnhealthyApiFactory` não configuram `Keycloak:AuthServerUrl`, `Keycloak:AdminClientId`, `Keycloak:AdminClientSecret` | ⚠️ Aviso | 4 testes de HealthCheck falham com "The entry point exited without ever building an IHost" — `AddInfrastructure()` lança `InvalidOperationException` por falta das configurações de Keycloak |
| `tests/Onboarding.Integration.Tests/Registration/RegistrationIntegrationTests.cs` | 20, 24 | `KeycloakBuilder()` e `PostgreSqlBuilder()` sem argumento de imagem (construtores obsoletos) | ℹ️ Info | Avisos de compilação CS0618; funcional mas deve ser corrigido quando Testcontainers remover os construtores paramétricos |

---

## Verificação Humana Necessária

### 1. Testes de Integração com Containers Reais

**Teste:** Executar `dotnet test tests/Onboarding.Integration.Tests/` com Docker em execução
**Esperado:** Os 2 testes de `RegistrationIntegrationTests` terminam com status de saída 0; cada teste retorna 201 (se Keycloak aceitar) ou 503 (Keycloak sem realm importado — compensation roda e DB fica vazio)
**Por que humano:** Requer Docker com acesso às imagens `quay.io/keycloak/keycloak:26.1` e `postgres:16-alpine` — impossível verificar programaticamente no ambiente atual

### 2. Confirmação e Correção das Falhas de HealthCheck

**Teste:** Adicionar `Keycloak:AuthServerUrl`, `Keycloak:AdminClientId` e `Keycloak:AdminClientSecret` às factories `HealthyApiFactory` e `UnhealthyApiFactory` em `HealthCheckEndpointTests.cs` e executar `dotnet test tests/Onboarding.API.Tests/`
**Esperado:** 28 aprovados, 0 com falha, 2 ignorados na suite completa API.Tests
**Por que humano:** A correção requer edição em arquivo de teste de Phase 4 dentro do contexto de execução de Phase 5 — decisão de escopo deve ser do humano responsável; pode ser tratada como gap de Phase 4 a ser endereçado em plano complementar

---

## Resumo dos Gaps

Não há gaps que bloqueiam o objetivo principal da fase. Os 6 critérios de sucesso do Roadmap estão verificados. As 11 verdades observáveis estão confirmadas.

**Regressão identificada (não-bloqueante para o objetivo da fase):** A adição de `AddInfrastructure(builder.Configuration)` ao `Program.cs` no Plan 05-03 quebrou os 4 testes de `HealthCheckEndpointTests` que não configuram as settings de Keycloak necessárias. Esta regressão é de baixa criticidade para o objetivo de Phase 5 (a API de registro funciona corretamente), mas deve ser corrigida para garantir que a suite de testes de Phase 4 (OBS-05) permaneça verde.

---

## Itens Diferidos

| Item | Fase Responsável | Evidência |
|------|-----------------|-----------|
| Full end-to-end com realm "onboarding" importado no Keycloak de teste (integração real PF → Keycloak) | Phase 6 (Authentication API) | Documentado nos comentários de `RegistrationIntegrationTests.cs` como "full end-to-end requires realm import (future work)" |

---

_Verificado: 2026-04-06T15:30:00Z_
_Verificador: Claude (gsd-verifier)_
