# Phase 55: controller-di-reduction — Plan  (slug: controller-di-reduction)

## Goal

Colapsar os construtores god-class dos controllers (`FundosController` 37 deps, `AdminUserController` 23, `CompaniesController` 17) introduzindo um dispatcher manual (`ICommandDispatcher`/`IQueryDispatcher`, sem MediatR) + `IValidationRunner`, levando cada controller a ≤ 5 deps — sem mudar rota/contrato/auth (D-54 herdado).

## Locked decisions (from CONTEXT.md)

- **D-60:** Dispatcher manual via `IServiceProvider` (sem MediatR, sem split de rota).
- **D-61:** Dispatch puro + `IValidationRunner` — validação fica no controller (1 runner em vez de N validators); `ToValidationProblem`/422 intacto.
- **D-62:** Gate ≤ 5 deps/controller (reviewer enforça).
- **D-63:** Todos os 9 controllers; behavior-preserving; Integration + Playwright regression.

## Partition strategy

Os 9 controllers estão **todos em `Onboarding.API`** → builds concorrentes de doers colidem em bin/obj (D-56, Phase 54). **Execução sequencial**, 1 task por wave. T-1 (infra) bloqueia tudo; refactor dividido em 2 tasks por tamanho de contexto (god classes vs resto), não por paralelismo.

## Tasks

### Wave 1 — Dispatcher infrastructure (bloqueia tudo)

#### T-1: Dispatcher + ValidationRunner abstractions + impls + DI + unit tests
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `src/Onboarding.Application/Common/ICommandDispatcher.cs` (new) — `Task<TResult> Send<TResult>(object command, CancellationToken)`
  - `src/Onboarding.Application/Common/IQueryDispatcher.cs` (new) — `Task<TResult> Query<TResult>(object query, CancellationToken)`
  - `src/Onboarding.Application/Common/IValidationRunner.cs` (new) — `Task<ValidationResult> Validate<T>(T instance, CancellationToken)`
  - `src/Onboarding.Infrastructure/Dispatch/CommandDispatcher.cs` + `QueryDispatcher.cs` + `ValidationRunner.cs` (new impls — resolvem `ICommandHandler<,>`/`IQueryHandler<,>`/`IValidator<>` via `IServiceProvider`; reflection com cache de tipo, ou `dynamic`)
  - `src/Onboarding.API/Program.cs` (ou DI extension) — registrar os 3 scoped
  - `tests/Onboarding.Application.Tests/Common/DispatcherTests.cs` (new — Shouldly) ou `Onboarding.Infrastructure.Tests/Dispatch/*`
- **Acceptance:**
  - Send resolve o `ICommandHandler<TCommand,TResult>` correto e invoca `HandleAsync`; Query idem; tipo errado → `InvalidOperationException` claro.
  - `IValidationRunner.Validate<T>` resolve `IValidator<T>` e roda; ausente → `ValidationResult` válido (no-op).
  - Registrados scoped; resolução não vaza estado entre requests.
  - Unit tests > 80% line nos 3 impls. Build 0 warning. Suite existente (1687) verde.
- **Dependencies:** none
- **Test:** xUnit unit (mock `IServiceProvider`)
- **Status:** pending

### Wave 2 — Refactor god classes

#### T-2: Refactor FundosController + AdminUserController + CompaniesController → ≤ 5 deps
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `src/Onboarding.API/Controllers/FundosController.cs` (37 → ~4)
  - `src/Onboarding.API/Controllers/AdminUserController.cs` (23 → ~4)
  - `src/Onboarding.API/Controllers/CompaniesController.cs` (17 → ~4)
- **Acceptance:**
  - Ctor injeta só `ICommandDispatcher` + `IQueryDispatcher` + `IValidationRunner` + deps NÃO-CQRS legítimas (logger, services específicos). ≤ 5 cada (ou exceção documentada — ver T-3/AuthController, não esperado aqui).
  - Cada action: fluxo `Validate → if(!IsValid) UnprocessableEntity(ToValidationProblem) → Send/Query` **idêntico**. Zero mudança de rota/atributo/DTO/status code.
  - `git diff` prova: só troca de mecanismo de invocação (handler/validator fields → dispatcher.Send/runner.Validate). Nenhuma assinatura pública/rota muda.
  - Build 0 warning; `Onboarding.API.Tests` + suite verde.
- **Dependencies:** T-1
- **Test:** xUnit API.Tests (controllers já cobertos) + build
- **Status:** pending

### Wave 3 — Refactor remaining controllers

#### T-3: Refactor remaining 6 controllers → ≤ 5 (handle AuthController non-CQRS)
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `src/Onboarding.API/Controllers/AdminFundosController.cs` (11 → ~4)
  - `src/Onboarding.API/Controllers/FundoCedentesController.cs`, `FundoTiposAtivosController.cs`, `CedenteTiposAtivosController.cs` (8 → ~4 cada)
  - `src/Onboarding.API/Controllers/AuthController.cs` (8 — **risco**: 3 repos diretos não-CQRS)
  - `src/Onboarding.API/Controllers/PermissionsController.cs` (1 — já ok; aplicar padrão se usar handler)
  - (se SOLID-04: `src/Onboarding.Application/Auth/Queries/GetPermissionsQuery.cs` + handler — new)
- **Acceptance:**
  - Controllers de CRUD (AdminFundos + 3 relationships) ≤ 5 via dispatcher.
  - **AuthController:** após dispatcher, contar deps. Se ≤ 5 → ok. Se > 5 por causa dos repos diretos (SOLID-04), DECIDIR e executar: (a) rotear permissões/lookups por um query handler (move repos pra Application) → ≤ 5; OU (b) documentar exceção justificada do gate pra AuthController em WARNINGS (Keycloak-crítico, behavior-preserving prioritário). **Não** mudar o fluxo OIDC/token/cookie.
  - Build 0 warning; suite verde; zero mudança de contrato/auth.
- **Dependencies:** T-1, T-2
- **Test:** xUnit API.Tests + (auth flow coberto por Integration/Playwright em T-4)
- **Status:** pending

### Wave 4 — Gate + verification prep

#### T-4: Ctor-param gate check + WARNINGS + green-suite self-check
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `.jdi/phases/controller-di-reduction/GATE.md` (new — tabela final de deps por controller, todos ≤ 5 ou exceção justificada)
  - `.jdi/phases/controller-di-reduction/WARNINGS.md` (new — exceções de gate, ex.: AuthController se aplicável; nota subsumindo W-FUNDOS-SPLIT)
  - (script opcional de contagem de ctor-params pro reviewer reusar)
- **Acceptance:**
  - Tabela: cada controller com nº de deps pós-refactor; todos ≤ 5 OU exceção documentada com razão.
  - Suite completa verde (≥ 1687 + dispatcher tests); build 0 warning; lint/format limpos.
  - Dispatcher/runner > 80% per-file.
  - Pronto pra /jdi-verify (que roda Integration + Playwright regression + confirma gate).
- **Dependencies:** T-3
- **Test:** full suite + build/lint
- **Status:** pending

## Execution

- **Total tasks:** 4
- **Waves:** 4
- **Critical path:** T-1 → T-2 → T-3 → T-4 (sequencial — single project, build-race constraint D-56)
- **Parallel-eligible:** nenhum (todos os controllers em `Onboarding.API`)
- **Specialist:** backend-csharp (todas)

## Risks & notes

- **RISCO #1 — AuthController deps não-CQRS.** O dispatcher não remove os 3 repos diretos (SOLID-04 diferido do Phase 54). Pode ficar ~7 > gate. T-3 decide: abordar SOLID-04 (query handler) ou exceção documentada. Não tocar o fluxo de auth.
- **Constraint (D-54 herdado):** o refactor é puramente mecânico (troca campo injetado por chamada de dispatcher) → contrato HTTP byte-equivalente. `/jdi-verify` exige Integration.Tests (Testcontainers) + Playwright regression (login/logout + endpoint dispatchado por controller) provando 200/401/422 inalterados.
- **Dispatcher correctness:** todo command/query type já tem handler registrado (hoje injetado direto) → o dispatcher resolve os mesmos. Validar que nenhum handler ficou órfão (T-1 + suite verde cobrem).
- **KISS:** reflection + cache de tipo, sem source generator (D-60 / CONTEXT Notes).
- **Cobertura:** controllers já cobertos (API.Tests 504 + Integration 248) = rede de regressão; só o dispatcher/runner novo precisa de testes novos.
