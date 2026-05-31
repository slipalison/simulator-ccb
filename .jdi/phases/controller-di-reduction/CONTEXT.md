# Phase 55 — controller-di-reduction — CONTEXT

## Goal

Eliminar a explosão de parâmetros de construtor nos controllers da `Onboarding.API`, que viraram god classes de injeção de dependência:

| Controller | Deps no ctor (hoje) |
|---|---|
| `FundosController` | **37** |
| `AdminUserController` | **23** |
| `CompaniesController` | **17** |
| `AdminFundosController` | 11 |
| `Fundo/Cedente/FundoTipos/CedenteTipos` (×4) | 8 cada |
| `AuthController` | 8 |
| `PermissionsController` | 1 |

Causa raiz: cada operação injeta um par **`ICommandHandler<,>`/`IQueryHandler<,>` + `IValidator<>`** separado. ~17 operações no Fundos × 2 ≈ 34 + serviços.

Esse problema passou no /jdi-verify do Phase 54 porque o gate D-52 (`params ≤ 3`) foi aplicado só a parâmetros de **método**, nunca à **injeção de construtor** (ver D-59). Esta phase corrige e fecha o gap.

## Locked decisions (Phase 55)

- **D-60 (DA-1) — Dispatcher manual (sem MediatR).** Introduzir `ICommandDispatcher` + `IQueryDispatcher` que resolvem o handler concreto (`ICommandHandler<TCommand,TResult>` / `IQueryHandler<TQuery,TResult>`) via `IServiceProvider` por tipo (reflection com cache, ou `dynamic` — sem source-gen complexo). **Sem MediatR** (D-3 OSS-only, memória `no_mediatr`) — é CQRS manual sobre DI. **Sem split de rota** — controllers continuam os mesmos tipos, mesmas rotas.

- **D-61 (DA-2) — Dispatch puro + `IValidationRunner` (validação fica no controller).** O dispatcher **NÃO** valida. A validação continua EXPLÍCITA no controller, mas via 1 abstração injetada `IValidationRunner` que resolve `IValidator<T>` do `IServiceProvider` e roda. Fluxo preservado byte-a-byte: `var v = await _validation.Validate(cmd, ct); if (!v.IsValid) return UnprocessableEntity(ToValidationProblem(v)); var r = await _commands.Send<TDto>(cmd, ct);`. `ToValidationProblem`/422 (de `ValidationExtensions`, Phase 54) ficam no controller — **contrato 422 intacto**.

- **D-62 (DA-3) — Gate de ctor-params ≤ 5 deps/controller.** O reviewer passa a enforçar ≤ 5 dependências injetadas por controller (fecha o gap do Phase 54). Após o refactor, o típico é 3-4 (2 dispatchers + validation-runner + logger).

- **D-63 (DA-4) — Escopo: todos os 9 controllers.** Aplicar o dispatcher uniformemente (mesmo os de 8 deps) — consistência + previne regressão futura do padrão handler-por-operação.

- **Constraint herdado (D-54 + invocação do usuário):** zero mudança de rota, contrato HTTP, shape de payload/resposta, status codes, cookies, CORS, fluxo OIDC. Refactor behavior-preserving. **Não pode quebrar Front (5173/5174) nem Keycloak (ACF+PKCE).** Regressão Integration.Tests + Playwright obrigatória.

## Design alvo (referência pro planner)

```csharp
// Application/Common
public interface ICommandDispatcher { Task<TResult> Send<TResult>(object command, CancellationToken ct = default); }
public interface IQueryDispatcher   { Task<TResult> Query<TResult>(object query,  CancellationToken ct = default); }
public interface IValidationRunner  { Task<ValidationResult> Validate<T>(T instance, CancellationToken ct = default); }

// Infrastructure (or Application/Common impl) — resolve do IServiceProvider, registrar scoped
sealed class CommandDispatcher(IServiceProvider sp) : ICommandDispatcher {
    public Task<TResult> Send<TResult>(object command, CancellationToken ct = default) {
        var t = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        dynamic h = sp.GetRequiredService(t);
        return h.HandleAsync((dynamic)command, ct);
    }
}
// ValidationRunner: sp.GetService<IValidator<T>>() ?? no-op → ValidationResult (válido).

// Controller depois (37 → ~4):
public FundosController(ICommandDispatcher commands, IQueryDispatcher queries,
                        IValidationRunner validation, ILogger<FundosController> logger) { ... }
```

## Canonical refs

- `.jdi/DECISIONS.md` — D-59 (o gap), D-60..D-63 (esta phase), D-54 (constraint behavior-preserving), D-3 (OSS-only, no MediatR), D-58 (FromSql search — não tocar).
- `src/Onboarding.API/Controllers/*` — 9 controllers alvo; pior caso `FundosController.cs` (37).
- `src/Onboarding.Application/Common/ICommandHandler.cs` + `IQueryHandler.cs` — interfaces que o dispatcher resolve.
- `src/Onboarding.API/Extensions/ValidationExtensions.cs` — `ToValidationProblem` (Phase 54 DRY-01) fica no controller.
- `src/Onboarding.API/.../GlobalExceptionHandler` — NÃO precisa mudar (validação não migra pra exception nesta abordagem).
- Registro DI: `Program.cs` / extensão de DI — registrar os 3 dispatchers/runner scoped.
- `.jdi/phases/backend-csharp-quality-audit/WARNINGS.md` — `W-FUNDOS-SPLIT` é subsumido (o split vira opcional/cosmético, fora de escopo aqui).

## Out of scope

- **Split do FundosController em controllers menores** — D-60 mantém os tipos atuais. Após o dispatcher, Fundos não é mais god class de DI; o split físico vira cosmético e fica como candidato futuro (não nesta phase).
- **AuthController SOLID-04 (deps diretas de repo)** — fora de escopo, EXCETO se necessário pra AuthController atingir ≤ 5 (ver Notes — risco).
- **Mudança de contrato / rotas / auth / Keycloak** — proibido (constraint).
- **Frontend** — backend-only.
- **Refactor dos handlers/validators em si** — só muda COMO o controller os invoca (via dispatcher/runner), não os handlers.
- **Migração pra MediatR ou qualquer lib de mediator** — proibido (D-3).

## Notes

- **RISCO — AuthController e deps não-CQRS.** O dispatcher remove só as injeções de handler/validator. Controllers com deps NÃO-CQRS (ex.: `AuthController` injeta 3 repos direto — SOLID-04 diferido) podem ficar acima de ≤ 5 mesmo após o dispatcher (2 dispatchers + runner + 3 repos + logger ≈ 7). O `/jdi-plan` decide: (a) abordar o SOLID-04 do AuthController (rotear permissões por um query handler) pra cair em ≤ 5, ou (b) documentar exceção justificada do gate pra esse controller. Medir cada controller pós-dispatcher.
- **Contrato 422 preservado.** Como a validação fica no controller (D-61), o `ToValidationProblem` e o status 422 não mudam — não há risco de divergência de shape (diferente da opção "dispatcher valida"). Esse é o ponto forte da escolha.
- **Cobertura.** As 3 classes novas (`CommandDispatcher`/`QueryDispatcher`/`ValidationRunner`) são código novo → testes unit >80% per-file (alinha com o rigor do Phase 54). Controllers refatorados: cobertura existente (API.Tests 504 + Integration 248) deve continuar verde — são a rede de regressão.
- **Verificação (regra do projeto):** build 0 warning; suite completa verde (baseline 1687); Integration.Tests (Testcontainers) + Playwright regression nos endpoints (login/logout + um endpoint dispatchado por controller). Zero mudança de comportamento observável.
- **Reflection/dynamic no dispatcher:** custo desprezível (resolução scoped por request + cache de tipo). KISS — não introduzir source generator.
- **Specialist routing:** `jdi-doer-onboarding-keycloak-backend-csharp` (refactor + dispatcher + tests). Verify: backend reviewer (gate ≤5 + suite + Playwright) + security reviewer (confirmar que multi-tenant D-5 e authz não mudaram — dispatch é transparente).

## Definition of Done (Phase 55)

### Implementação
- [ ] `ICommandDispatcher`/`IQueryDispatcher`/`IValidationRunner` + impls criados, registrados scoped no DI.
- [ ] Todos os 9 controllers refatorados pra injetar dispatchers + runner (em vez de N handlers/validators).
- [ ] Fluxo validate→422→dispatch preservado em cada action; `ToValidationProblem`/status codes idênticos.

### Gate (D-62)
- [ ] Cada controller ≤ 5 deps injetadas (ou exceção documentada com justificativa — ex.: AuthController SOLID-04).
- [ ] Reviewer enforça o gate (script/contagem de `private readonly` + ctor params por controller).

### Constraint (D-54 herdado)
- [ ] Zero mudança de rota / atributo `[Http*]`/`[Authorize]`/policy / DTO / status code (git-diff prova só troca de mecanismo de invocação).
- [ ] Multi-tenant D-5 e authz inalterados (dispatch é transparente).

### Cobertura & verificação
- [ ] Dispatcher/runner novos > 80% line (per-file).
- [ ] Build 0 warning; suite completa verde (≥ 1687 + testes novos do dispatcher).
- [ ] Integration.Tests + Playwright regression PASS (endpoints continuam respondendo igual: 200/401/422/etc).
