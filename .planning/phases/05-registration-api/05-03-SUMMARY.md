---
phase: 05-registration-api
plan: "03"
subsystem: backend-application-api
tags: [fluent-validation, controller, registration, duplicate-check, keycloak-compensation, sec-08]
dependency_graph:
  requires:
    - "05-01: TDD stubs RED — RegistrationControllerTests, RegisterClientCommandHandlerTests"
    - "05-02: Infrastructure — IClientRepository.DeleteAsync, IKeycloakUserService, AddInfrastructure()"
    - "03: Domain layer — Client aggregate, DuplicateClientException needs domain home"
  provides:
    - "DuplicateClientException e RegistrationFailedException no domínio"
    - "RegisterClientCommandHandler com duplicate check + Keycloak call + compensation (REG-05, REG-06)"
    - "RegisterClientCommandValidator (FluentValidation AbstractValidator)"
    - "RegistrationController POST /api/registration com [ApiController] (BACK-05)"
    - "Program.cs wired: AddDistributedMemoryCache + AddApplication + AddInfrastructure"
    - "9 stubs RED dos Plans 01/02/03 viram GREEN"
  affects:
    - "05-04: IdempotencyFilter pode ser aplicado ao RegistrationController já funcional"
tech_stack:
  added:
    - "FluentValidation 12.1.1 (Apache 2.0) — validator manual sem auto-pipeline"
  patterns:
    - "AbstractValidator<T> com When() para regras condicionais PF/PJ"
    - "HTTP DTO (RegisterClientRequest) separado do Application command — desacoplamento"
    - "catch(ArgumentException) → 422 genérico — sem ex.Message no response (SEC-08)"
    - "catch(DuplicateClientException) → 409 sem field hint (SEC-08)"
    - "catch(RegistrationFailedException) → 503 (compensation já rodou no handler)"
    - "IsTransientKeycloakError() — filtro de exceção para compensation path"
key_files:
  created:
    - "src/Onboarding.Domain/Exceptions/DuplicateClientException.cs"
    - "src/Onboarding.Domain/Exceptions/RegistrationFailedException.cs"
    - "src/Onboarding.Application/Clients/Validators/RegisterClientCommandValidator.cs"
    - "src/Onboarding.API/Controllers/RegisterClientRequest.cs"
    - "src/Onboarding.API/Controllers/RegistrationController.cs"
  modified:
    - "src/Onboarding.Application/Clients/Commands/RegisterClientCommandHandler.cs (duplicate check + Keycloak + compensation)"
    - "src/Onboarding.Application/DependencyInjection.cs (adiciona IValidator<RegisterClientCommand>)"
    - "src/Onboarding.Application/Onboarding.Application.csproj (FluentValidation 12.1.1)"
    - "src/Onboarding.API/Program.cs (AddDistributedMemoryCache + AddApplication + AddInfrastructure)"
    - "tests/Onboarding.Domain.Tests/Application/Commands/RegisterClientCommandHandlerTests.cs (IKeycloakUserService mock + 4 stubs GREEN)"
    - "tests/Onboarding.API.Tests/Registration/RegistrationControllerTests.cs (factory expandida + 9 testes reais)"
decisions:
  - "FluentValidation registrado manualmente no DI (não via auto-pipeline) — FV 12 deprecou SetupFluentValidation; manual é mais explícito e controlado"
  - "IsTransientKeycloakError() filtra HttpRequestException, TaskCanceledException, InvalidOperationException — ArgumentException (erro de programação) não entra na compensation"
  - "ex.Message NÃO aparece em nenhuma resposta HTTP — SEC-08 — apenas em comentários explicativos no código"
  - "RegisterClientRequest (HTTP DTO) separado de RegisterClientCommand — desacopla concerns HTTP da Application layer"
metrics:
  duration_minutes: 25
  completed_date: "2026-04-06"
  tasks_completed: 2
  files_created: 5
  files_modified: 6
---

# Phase 05 Plan 03: Controller, Validator e Handler Summary

**One-liner:** RegistrationController [ApiController] + RegisterClientCommandValidator (FluentValidation) + handler com duplicate check / Keycloak call / compensation, 9 stubs RED virados GREEN, SEC-08 verificado por teste.

## What Was Built

Implementado o core do sistema de registro de clientes PF/PJ:

1. **DuplicateClientException** — exception de domínio com mensagem genérica; callers (controllers) não devem propagar para HTTP response (SEC-08)
2. **RegistrationFailedException** — exception de domínio para falha do Keycloak; InnerException preservado para logs via Serilog; não propagado para response
3. **RegisterClientCommandHandler reescrito** — agora com:
   - Duplicate check: ExistsByCpfAsync / ExistsByCnpjAsync / ExistsByEmailAsync → lança DuplicateClientException ANTES de AddAsync (REG-05)
   - AddAsync → CreateUserAsync (Keycloak) — ordem arquitetural definida no STATE.md
   - Compensation: se CreateUserAsync lança HttpRequestException/TaskCanceledException/InvalidOperationException → DeleteAsync + RegistrationFailedException (REG-06)
4. **RegisterClientCommandValidator** — AbstractValidator com regras PF (Nome + CPF 11 dígitos) e PJ (RazaoSocial + CNPJ 14 alfanuméricos), validação estrutural sem check-digit (domínio faz o check-digit)
5. **RegisterClientRequest** — DTO HTTP separado do command da Application layer
6. **RegistrationController** — [ApiController], [Route("api/[controller]")], POST único com mapeamento de exceções para HTTP sem vazar detalhes internos (SEC-08)
7. **Program.cs** — AddDistributedMemoryCache + AddApplication + AddInfrastructure registrados

## Tasks Completed

| Task | Name | Commit | Key Files |
|------|------|--------|-----------|
| 1 | Domain exceptions + Handler com duplicate check e Keycloak compensation | 98ecc53 | DuplicateClientException.cs, RegistrationFailedException.cs, RegisterClientCommandHandler.cs, RegisterClientCommandHandlerTests.cs |
| 2 | FluentValidation Validator + RegistrationController + Program.cs wiring | 3d2f71f | RegisterClientCommandValidator.cs, RegistrationController.cs, RegisterClientRequest.cs, Program.cs, DependencyInjection.cs |

## Verification Results

- `dotnet build src/Onboarding.API/` — 0 Erro(s), 1 warning inofensivo de versão de assembly
- `dotnet test tests/Onboarding.Domain.Tests/ --filter "CommandHandler"` — 9 Aprovados, 0 Com falha
- `dotnet test tests/Onboarding.API.Tests/ --filter "FullyQualifiedName~RegistrationControllerTests"` — 9 Aprovados, 2 Com falha (stubs REG-08 Plan 04, intencionais)
- `grep "ex.Message" src/Onboarding.API/Controllers/RegistrationController.cs` — apenas em comentários, nunca em código executável (SEC-08 PASS)
- `grep "AddApplication\|AddInfrastructure\|AddDistributedMemoryCache" src/Onboarding.API/Program.cs` — 3 matches

## Deviations from Plan

### Auto-fixed Issues

Nenhum — plano executado exatamente como escrito.

### Observações

**HealthCheck tests (pré-existentes):** 4 testes de HealthCheckEndpointTests falhavam antes deste plano (verificado via git stash). São falhas pré-existentes fora do escopo do Plan 03. Registradas em deferred-items para investigação futura.

## Known Stubs

- `PostPf_SameIdempotencyKey_SecondCallReturnsCached201` — REG-08, aguarda Plan 04 (IdempotencyFilter)
- `PostPf_NoIdempotencyKey_ProceedsNormally` — REG-08, aguarda Plan 04 (IdempotencyFilter)
- `IdempotencyFilterTests.*` (3 testes) — REG-08, aguarda Plan 04

Estes stubs são intencionais e não impedem o objetivo deste plano (registro PF/PJ funcional).

## Threat Flags

Nenhuma nova superfície de ataque introduzida além do documentado no threat_model do plano.

Os mitigations T-05-01 e T-05-02 foram implementados e verificados por teste:
- T-05-01: `PostPf_InvalidCpf_ResponseBodyIsGeneric` — body não contém "check digit" nem "ArgumentException"
- T-05-02: `PostPf_DuplicateCpf_ResponseBodyDoesNotLeakFieldName` — body não contém "cpf", "email" nem "already registered"

## Self-Check: PASSED

- [x] `src/Onboarding.Domain/Exceptions/DuplicateClientException.cs` — existe
- [x] `src/Onboarding.Domain/Exceptions/RegistrationFailedException.cs` — existe
- [x] `src/Onboarding.Application/Clients/Validators/RegisterClientCommandValidator.cs` — existe
- [x] `src/Onboarding.API/Controllers/RegisterClientRequest.cs` — existe
- [x] `src/Onboarding.API/Controllers/RegistrationController.cs` — existe
- [x] Commit 98ecc53 — existe (Task 1)
- [x] Commit 3d2f71f — existe (Task 2)
