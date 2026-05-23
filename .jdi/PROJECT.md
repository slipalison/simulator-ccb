# Onboarding PF/PJ — Keycloak

## Vision
Sistema de onboarding seguro para clientes PJ com gestão de funcionários, permissões via Keycloak e (milestone v8.0) gestão cadastral de fundos de investimento. Isolamento multi-tenant entre empresas é requisito de primeira classe.

## Type
Full-stack web app (API + 2 SPAs)

## Status
**Adopted** em 2026-05-11. Codigo pre-existente — JDI adicionado depois. Repo ja possui sistema paralelo `.planning/` (legado, milestones v1-v8). JDI segue daqui pra frente, sem conflito.

## Stack (detected + confirmed)
- **Language:** C# / TypeScript
- **Backend:** .NET 10 (ASP.NET Core Controllers + EF Core 10) + PostgreSQL 16
- **Frontend client:** React 19 + Vinxi (Vite) + TanStack Router + Tailwind 4 + react-hook-form + zod
- **Frontend backoffice:** same stack as client, projeto separado (constraint arquitetural — sem code-sharing)
- **Auth:** Keycloak 26.1 (hardened). Client = ACF+PKCE (Phase 33). Backoffice = ACF+PKCE com tema customizado (D-feedback).
- **Test backend:** xUnit + Shouldly + NSubstitute + coverlet (cobertura 80%)
- **Test frontend:** Vitest + Playwright (e2e)
- **Lint/format:** ESLint 9 (frontend), .editorconfig + analyzers (.NET)
- **Hooks:** husky + lint-staged
- **Observability:** Serilog + OpenTelemetry
- **Security pipeline (13 checks):** Semgrep, CodeQL, Trivy, Dependabot, Syft, ZAP, Dockle, Checkov, Kubescape, Gitleaks, TruffleHog
- **Conventional commits:** sim (~30/30 commits recentes seguem)

## Code Design
**LOCKED:** DDD (Domain-Driven Design)

Confirmado pelo usuario em /jdi-adopt. Estruturalmente as camadas existem (`Domain`, `Application`, `Infrastructure`, `API`) mas o pattern dominante e a decisao locked eh DDD puro:
- Aggregates ricos com invariantes em `src/Onboarding.Domain/Aggregates/`
- ValueObjects (Cnpj, Cpf, Email, etc) em `src/Onboarding.Domain/ValueObjects/`
- Repository interfaces em `src/Onboarding.Domain/Repositories/`
- Domain exceptions tipadas em `src/Onboarding.Domain/Exceptions/`

Implementacao practica: handlers via DI manual (`ICommandHandler<TCommand>` / `IQueryHandler<TQuery, TResult>`) — sem MediatR (restricao OSS-only, D-3). FluentValidation pra validators. Shouldly em vez de FluentAssertions.

## Slug
onboarding-keycloak

## Existing assets (snapshot em 2026-05-11)

Modulos/diretorios encontrados (agrupados):
- `src/Onboarding.Domain/Aggregates/` — Company, Employee, Audit, AdminAuditLog, PasswordReset, Fundo, ConsultoriaFundo, Custodiante, Cedente, TipoAtivo (10 aggregates)
- `src/Onboarding.Domain/ValueObjects/`, `Common/`, `Exceptions/`, `Repositories/`
- `src/Onboarding.Application/Companies/` — Commands (24 files: register, employee CRUD, access groups, password reset), Queries, DTOs
- `src/Onboarding.Application/Fundos/` — Commands (TipoAtivo, Cedente PF/PJ, ConsultoriaFundo, Custodiante, Fundo lifecycle), Queries, DTOs
- `src/Onboarding.Application/Admin/`, `Auth/`, `Common/`, `Services/`
- `src/Onboarding.API/Controllers/` — incluindo `FundosController.cs` (em desenvolvimento — phase 48)
- `src/Onboarding.API/Security/PermissionPolicyConstants.cs`
- `src/Onboarding.API/Middleware/`, `Filters/`, `Configuration/`, `Observability/`
- `src/Onboarding.Infrastructure/Persistence/`, `Repositories/`, `Keycloak/`, `Services/`
- `tests/Onboarding.Domain.Tests/`, `Application.Tests/`, `API.Tests/`, `Integration.Tests/`
- `tests/keycloak-hardening/`, `tests/uat-tests.http`, `tests/run-uat.mjs`
- `frontend/client/src/` — SPA cliente PJ registration + profile
- `frontend/backoffice/src/` — SPA admin (employee/access-group management, audit log)
- `keycloak/` — realm exports, themes (PKCE custom)
- `infra/`, `scripts/`, `docs/`, `.github/`, `.semgrep/`

Total: 267 .cs em src/, 124 .cs em tests/, 168 .ts/.tsx em frontend.

Schema/migrations: EF Core migrations em `src/Onboarding.Infrastructure/Persistence/Migrations/` (inclui AddFundosModule recente)
Routes/endpoints: Controllers em `src/Onboarding.API/Controllers/` — registration, auth, admin, employees, access-groups, audit-log, fundos (WIP)
Existing tests: xUnit 4 projetos, cobertura 80% enforced via coverlet+CI

**Importante:** Estes assets sao contexto pro planner, NAO TODO. Phases adicionam novas features.

## Global constraints
- Cobertura minima 80% (apenas em codigo NOVO criado apos D-2 boundary)
- Conventional commits (ja em uso)
- Atomic commits — 1 task = 1 commit
- Codigo, commits, PRs: ingles
- Discussao, docs em `.jdi/`: pt-BR
- i18n: nunca string hardcoded em pt-BR no JSX
- **OSS-only:** todos NuGet packages devem ser MIT/Apache 2.0 — sem MediatR (commercial), sem FluentAssertions (paid). Usar CQRS manual via DI, Shouldly
- **Isolamento multi-tenant:** CRITICO — qualquer leak entre empresas eh vulnerabilidade. Aggregates company-scoped tem HasQueryFilter + ClientId
- **Frontend separation:** `frontend/client` e `frontend/backoffice` totalmente independentes — sem shared code, sem cross-imports
- Prioridade quando conflita: Seguranca > Performance > Boas praticas

## Definition of Done (DoD) — MANDATORY policy

Esta secao eh **POLICY GLOBAL**, aplica a TODAS as phases JDI deste projeto. Estabelecida apos Phase 51 onde gates mecanicos (typecheck, lint, coverage) passaram mas o feature nao funcionava no browser (POST 4xx, Sidebar oculta por hardcoded map errado, route invariant errors). Reviewer DEVE bloquear ship se DoD nao for cumprido — NAO eh suficiente passar build + tests.

### Princıpio
**Cada task entrega algo integrado e funcional end-to-end.** Nao basta "codigo compila" ou "test verde" — o feature precisa funcionar no browser/cliente real contra o stack real (`docker compose up`).

### Criterios DoD por categoria

**Feature de cadastro/CRUD (create/update/delete):**
1. Frontend renderiza form sem erros de runtime (zero console errors, zero invariant failed).
2. Submit do form via UI dispara POST/PUT/DELETE real contra backend rodando em `docker compose up`.
3. Response HTTP 2xx confirmada via Playwright `browser_network_request` ou MCP equivalente (screenshot + status code logado em REVIEW.md).
4. Lista (refetch ou navegacao) mostra a nova/atualizada/removida entidade — proof de round-trip completo.
5. Caso de erro tipado (Duplicate*, InvalidStatus*, FluentValidation 422) reflete inline no form OU toast destrutivo conforme D-26 — UI nao engole erro silenciosamente.
6. Permission gate verificado: usuario sem permissao recebe 401/403 e UI degrada graciosamente (esconde botao OU mostra toast).

**Feature de busca/listagem (read):**
1. Frontend renderiza tabela/lista sem erros runtime.
2. GET real ao backend retorna 200 + paginacao funciona (next/prev/page jump).
3. Search input dispara fetch debounced com termo, response filtra resultados — verificado MCP.
4. Filtros (status, empresa, categoria etc) funcionais — dropdown change dispara refetch com query param novo.
5. URL search params estado bookmarkable — abrir URL com `?page=N&search=Q` carrega no estado correto.
6. Loading skeleton em fetch inicial; spinner em refetch — sem flash de empty state durante load.

**Feature de detalhe (drill-down):**
1. Click em row navega para `/<entity>/$id` sem invariant errors.
2. Detail page carrega dados completos via GET — verificado MCP screenshot.
3. Caso 404 (id invalido) renderiza empty state gracioso, NAO crash.
4. Audit/history inline (se aplicavel D-30) carrega via endpoint separado e renderiza eventos.

**Feature backend (endpoint novo):**
1. Endpoint responde 2xx para happy path autenticado — verificado via curl OU integration test rodando.
2. Auth scheme correto (BearerClient/BearerBackoffice) + Policy aplicada.
3. Multi-tenant guard (D-5) ativo — request com sub de outra empresa retorna 404, NAO leak.
4. Validation error retorna 422 ProblemDetails com `errors[]` field-mapped.
5. Domain exception tipada (DuplicateActiveAssociationException etc) traduzida para HTTP code correto via GlobalExceptionHandler.

**Migration de codigo (refactor sem feature):**
1. Suite de testes anterior + nova ambas verdes.
2. Caso a refactor tocou route/component visivel ao usuario: MCP smoke test confirma render sem regression.

### Gate de reviewer

Reviewer DEVE rejeitar verdict `APPROVED` ou `APPROVED_WITH_WARNINGS` se qualquer DoD acima nao for cumprido para o escopo do PLAN.md daquela phase. Verdict correto nesse caso eh **BLOCKED** com a DoD nao cumprida listada explicitamente.

`APPROVED_WITH_WARNINGS` eh aceitavel APENAS para warnings que NAO afetam runtime do feature da phase — exemplo: bundle size advisory, lint cosmetico em arquivo legado, telemetria operacional. Warnings que mascaram runtime gaps (POST nao testado live, Sidebar nao verificada, route nao acessada) sao **BLOCKERS disfarcados** e devem ser BLOCKED.

### Gate de /jdi-ship

`/jdi-ship` exige verdict APPROVED ou APPROVED_WITH_WARNINGS COM DoD cumprido. Se REVIEW.md tem warning categoria "runtime not verified" / "MCP not run" / "endpoint not exercised" — /jdi-ship aborta e exige nova iteracao do loop.

### Evidencia obrigatoria em REVIEW.md

Reviewer documenta no REVIEW.md, secao por categoria DoD aplicavel:
- HTTP status + body snippet (verbatim) para cada endpoint exercitado
- Screenshot path `.jdi/cache/phase-NN-<scenario>.png` para cada cenario UI verificado
- Console MCP filter output (assertion zero invariant errors)
- Network MCP request list (proof de round-trip)
- Comando exato usado pra disparar o test (reproduzivel)

Sem evidencia, gate nao cumprido.

## Research notes
- Repo possui sistema paralelo `.planning/` (legado). JDI nao toca nele — segue adiante.
- Phase 48 (.planning) atualmente em planning: API + Permissions for Fundos module — vira phase 1 do JDI roadmap.
- Backoffice migrou de ROPC pra ACF+PKCE em phase 33. ROPC legado pode ser removido futuro.

## LLM config
- Provider: Anthropic Claude (default — projeto roda em Claude Code)
- Modelo padrao: Opus pro planejamento/research, Sonnet pra execucao
