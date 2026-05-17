# Phase 51: frontend-client-fundos — Plan  (slug: frontend-client-fundos)

## Goal

SPA cliente PJ (`frontend/client`, porta 5173) ganha seção Fundos completa: CRUD das 5 entidades (Fundo, ConsultoriaFundo, Custodiante, Cedente, TipoAtivo) + UI das 3 associações N-N (FundoCedente, CedenteTipoAtivo, FundoTipoAtivo) com payload (limites + janela de datas + status) + state-machine transitions via dropdown alimentado por backend `GET /allowed-transitions`. Listagens paginadas com search debounced 300ms; estado via URL search params; erros do backend refletidos inline+toast; permission-aware UI (`funds:read/write/delete/manage`).

## Locked decisions (from CONTEXT.md)

- **D-23:** Escopo full — 5 entidades CRUD + 3 associações N-N + status transitions; sem split em sub-phase.
- **D-24:** TanStack Query adicionado; `apiFetch` continua como transport; query keys padronizadas.
- **D-25:** Backend expõe `GET /api/{entity}/{id}/allowed-transitions` para Fundo + 3 associações.
- **D-26:** Erros refletidos inline por field (`react-hook-form.setError` via helper `mapApiErrorToForm`) + toast crítico para domain exceptions tipadas.
- **D-27:** Listagens skip/take + paginator numérico + URL search params (`?page=N&search=Q`) + debounce 300ms + page size 20.

## Tasks

### Wave 1 (parallel-eligible)

#### T-1: Backend `GET /allowed-transitions` endpoints + state-machine introspection
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `src/Onboarding.Domain/Aggregates/FundoAggregate/Fundo.cs` (add `GetAllowedNextStates()` if absent)
  - `src/Onboarding.Domain/Aggregates/FundoCedenteAggregate/FundoCedente.cs`
  - `src/Onboarding.Domain/Aggregates/FundoTipoAtivoAggregate/FundoTipoAtivo.cs`
  - `src/Onboarding.Domain/Aggregates/CedenteTipoAtivoAggregate/CedenteTipoAtivo.cs`
  - `src/Onboarding.Application/Fundos/Queries/GetAllowedTransitionsQuery.cs` (new — 4 variants or generic)
  - `src/Onboarding.API/Controllers/FundosController.cs` (`GET /{id}/allowed-transitions`)
  - `src/Onboarding.API/Controllers/FundoCedentesController.cs` (`GET /{fundoId}/cedentes/{cedenteId}/allowed-transitions`)
  - `src/Onboarding.API/Controllers/FundoTiposAtivosController.cs` (analog)
  - `src/Onboarding.API/Controllers/CedenteTiposAtivosController.cs` (analog)
  - `tests/Onboarding.Domain.Tests/Aggregates/Fundo/StateMachineTests.cs` (new + 3 association variants)
  - `tests/Onboarding.Application.Tests/Fundos/GetAllowedTransitionsQueryTests.cs`
  - `tests/Onboarding.API.Tests/Controllers/FundosControllerAllowedTransitionsTests.cs`
- **Acceptance:**
  - 4 endpoints retornam `200 OK` com body `string[]` representando próximos estados válidos a partir do estado atual.
  - `GetAllowedNextStates()` em cada aggregate produz mesmo conjunto que invariante de transição (parity test).
  - Endpoints autorizam via policy `funds:read`.
  - Cross-tenant guard (D-5): tenant errado retorna 404 (consistente com endpoints existentes).
  - Cobertura ≥80% nos arquivos novos (D-2).
- **Dependencies:** none
- **Test:** xUnit (domain + application + API) + Playwright regression (acessar endpoint logado)
- **Status:** pending

#### T-2: Frontend foundation — TanStack Query + zod schemas + api-errors helper + routes shell + sidebar
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/client/package.json` (add `@tanstack/react-query`, `@tanstack/react-query-devtools` dev)
  - `frontend/client/src/main.tsx` (instantiate `QueryClient`, wrap in `QueryClientProvider`)
  - `frontend/client/src/lib/query-client.ts` (new — config defaults)
  - `frontend/client/src/lib/api-errors.ts` (new — `mapApiErrorToForm`, domain-exception toast helpers)
  - `frontend/client/src/lib/fundos-schemas.ts` (new — zod schemas espelhando DTOs de Fundo, Cedente PF/PJ, ConsultoriaFundo, Custodiante, TipoAtivo + 3 associations + status enum + paginated list search params)
  - `frontend/client/src/lib/fundos-api.ts` (new — typed query functions + mutations usando `apiFetch`)
  - `frontend/client/src/components/organisms/Sidebar.tsx` (add grupo "Fundos" com items, permission-gated por `funds:read`)
  - `frontend/client/src/router.tsx` (declarar shell routes + lazy components; pages stub implementadas em T-3..T-7)
  - `frontend/client/src/tests/lib/api-errors.test.ts`
  - `frontend/client/src/tests/lib/fundos-schemas.test.ts`
- **Acceptance:**
  - `npm install` succeeds; `pnpm-lock.yaml` atualizado.
  - `QueryClient` configurado com `retry:1`, `staleTime:30_000`; mutations sem retry.
  - `mapApiErrorToForm(err, setError, fieldMap?)` mapeia `ProblemDetails.errors[]` → `setError` por field; domain exceptions (`DuplicateActiveAssociation*`, `DuplicateCnpj*`, `DuplicateCpf*`, `InvalidStatusTransition*`, `Conflict*`) viram `toast.error()` (sonner) com mensagem específica.
  - Zod schemas cobrem todos DTOs com erros pt-BR; status enums = `ATIVO|INATIVO|HISTORICO` (+ Fundo enum próprio).
  - Sidebar mostra/esconde grupo "Fundos" conforme `auth.permissions.includes('funds:read')`.
  - Routes registradas (componentes stub OK por enquanto); typecheck passa.
  - Cobertura ≥80% nos arquivos novos.
- **Dependencies:** none
- **Test:** Vitest (api-errors + schemas); Playwright regression (sidebar visibility por permission)
- **Status:** pending

### Wave 2 (parallel-eligible — depend on T-2; T-6 also depends on T-1)

#### T-3: TipoAtivo UI (catálogo global CRUD)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/client/src/components/pages/TiposAtivoListPage.tsx`
  - `frontend/client/src/components/pages/TipoAtivoFormPage.tsx` (new/edit)
  - `frontend/client/src/components/organisms/TipoAtivoForm.tsx`
  - `frontend/client/src/components/organisms/TipoAtivoTable.tsx`
  - `frontend/client/src/components/molecules/Paginator.tsx` (new — shared, reused por todas listas)
  - `frontend/client/src/components/molecules/SearchInput.tsx` (new — debounce 300ms, shared)
  - `frontend/client/src/router.tsx` (rotas `/tipos-ativos`, `/tipos-ativos/new`, `/tipos-ativos/$id`)
  - `frontend/client/src/tests/pages/TiposAtivoListPage.test.tsx`
  - `frontend/client/src/tests/components/Paginator.test.tsx`
  - `frontend/client/src/tests/components/SearchInput.test.tsx`
- **Acceptance:**
  - Lista paginada (skip/take, page size 20, paginator numérico) + search debounced 300ms; estado via URL search params (TanStack Router `validateSearch`).
  - Create/edit/delete funcional; erros refletidos inline+toast via `mapApiErrorToForm`.
  - Permission gating: `funds:write` mostra botão Create/Edit; `funds:delete` mostra Delete.
  - Skeleton em fetch inicial; spinner em refetch; toast sucesso em mutation OK.
  - Cobertura ≥80% nos arquivos novos.
- **Dependencies:** T-2
- **Test:** Vitest (componentes + page); Playwright regression (CRUD flow)
- **Status:** pending

#### T-4: Cedente UI (CRUD PF + PJ)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/client/src/components/pages/CedentesListPage.tsx`
  - `frontend/client/src/components/pages/CedenteFormPage.tsx`
  - `frontend/client/src/components/pages/CedenteDetailPage.tsx`
  - `frontend/client/src/components/organisms/CedentePfForm.tsx`
  - `frontend/client/src/components/organisms/CedentePjForm.tsx`
  - `frontend/client/src/components/organisms/CedenteTable.tsx`
  - `frontend/client/src/components/atoms/CedenteTipoToggle.tsx` (PF/PJ switch)
  - `frontend/client/src/router.tsx` (rotas `/cedentes`, `/cedentes/new`, `/cedentes/$id`)
  - `frontend/client/src/tests/pages/CedentesListPage.test.tsx`
  - `frontend/client/src/tests/pages/CedenteFormPage.test.tsx`
- **Acceptance:**
  - Lista paginada + search; Detail mostra dados + tabs (dados, tipos-ativos associados — placeholder until T-7).
  - Form PF (CPF) vs PJ (CNPJ) com toggle; validação Zod espelha backend (D-10 uniqueness retorna 409 → toast `DuplicateCpfException`/`DuplicateCnpjException`).
  - CRUD funcional com `mapApiErrorToForm`; permission-aware.
  - Cobertura ≥80%.
- **Dependencies:** T-2
- **Test:** Vitest + Playwright regression
- **Status:** pending

#### T-5: ConsultoriaFundo + Custodiante UI (CRUD)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/client/src/components/pages/ConsultoriasFundoListPage.tsx`
  - `frontend/client/src/components/pages/ConsultoriaFundoFormPage.tsx`
  - `frontend/client/src/components/pages/CustodiantesListPage.tsx`
  - `frontend/client/src/components/pages/CustodianteFormPage.tsx`
  - `frontend/client/src/components/organisms/ConsultoriaFundoForm.tsx`
  - `frontend/client/src/components/organisms/CustodianteForm.tsx`
  - `frontend/client/src/components/organisms/ConsultoriaFundoTable.tsx`
  - `frontend/client/src/components/organisms/CustodianteTable.tsx`
  - `frontend/client/src/router.tsx` (rotas `/consultorias-fundo/*`, `/custodiantes/*`)
  - `frontend/client/src/tests/pages/ConsultoriasFundoListPage.test.tsx`
  - `frontend/client/src/tests/pages/CustodiantesListPage.test.tsx`
- **Acceptance:**
  - 2 entidades CRUD completo (list paginada + create + edit + delete) com mesma estrutura.
  - Validação Zod, `mapApiErrorToForm`, permission gating.
  - Cobertura ≥80%.
- **Dependencies:** T-2
- **Test:** Vitest + Playwright regression
- **Status:** pending

#### T-6: Fundo UI (CRUD + status transition via allowed-transitions)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/client/src/components/pages/FundosListPage.tsx`
  - `frontend/client/src/components/pages/FundoFormPage.tsx`
  - `frontend/client/src/components/pages/FundoDetailPage.tsx`
  - `frontend/client/src/components/organisms/FundoForm.tsx`
  - `frontend/client/src/components/organisms/FundoTable.tsx`
  - `frontend/client/src/components/organisms/FundoStatusBadge.tsx`
  - `frontend/client/src/components/organisms/StatusTransitionDropdown.tsx` (new — shared, consumido também em T-7)
  - `frontend/client/src/lib/use-allowed-transitions.ts` (new — hook TanStack Query)
  - `frontend/client/src/router.tsx` (rotas `/fundos`, `/fundos/new`, `/fundos/$fundoId`)
  - `frontend/client/src/tests/pages/FundosListPage.test.tsx`
  - `frontend/client/src/tests/components/StatusTransitionDropdown.test.tsx`
- **Acceptance:**
  - List paginada + search + filtro por status (badge); Detail com tabs (dados, cedentes, tipos-ativos — placeholders até T-7).
  - Form Fundo CRUD com seleção de ConsultoriaFundo + Custodiante via dropdown (autocomplete simples buscando list endpoint).
  - `StatusTransitionDropdown` consome `GET /allowed-transitions` via TanStack Query (key `['allowed-transitions','fundo',id]`); dropdown só mostra estados válidos; ação dispara `POST /{id}/status`; refetch detalhe + lista; toast sucesso/erro.
  - `InvalidStatusTransitionException` (race vs revalidar) vira toast destrutivo.
  - Cobertura ≥80%.
- **Dependencies:** T-1, T-2
- **Test:** Vitest + Playwright regression (status transition flow)
- **Status:** pending

### Wave 3 (sequential — depends on entity UIs from Wave 2)

#### T-7: 3 associações N-N UI (FundoCedente, CedenteTipoAtivo, FundoTipoAtivo)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/client/src/components/pages/FundoCedentesTabPage.tsx` (sub-rota `/fundos/$fundoId/cedentes`)
  - `frontend/client/src/components/pages/FundoTiposAtivosTabPage.tsx` (sub-rota `/fundos/$fundoId/tipos-ativos`)
  - `frontend/client/src/components/pages/CedenteTiposAtivosTabPage.tsx` (sub-rota `/cedentes/$cedenteId/tipos-ativos`)
  - `frontend/client/src/components/organisms/AssociationForm.tsx` (new — shared form para 3 associações: limites + janela datas + entidade target dropdown)
  - `frontend/client/src/components/organisms/AssociationTable.tsx` (new — shared lista de associações com status badge + actions)
  - `frontend/client/src/components/molecules/DateRangeInput.tsx` (new — half-open `[data_inicio, data_fim)` per D-20)
  - `frontend/client/src/components/molecules/LimiteExposicaoInput.tsx` (new — percentual + valor)
  - `frontend/client/src/router.tsx` (sub-rotas dentro de `/fundos/$fundoId/*` e `/cedentes/$cedenteId/*`)
  - `frontend/client/src/tests/pages/FundoCedentesTabPage.test.tsx`
  - `frontend/client/src/tests/components/AssociationForm.test.tsx`
  - `frontend/client/src/tests/components/DateRangeInput.test.tsx`
- **Acceptance:**
  - 3 tabs/sub-páginas funcionais: listar associações + criar nova + editar limites/datas + transição de status via `StatusTransitionDropdown` (de T-6) consumindo `GET /allowed-transitions` específico de cada associação.
  - REL-09: tentativa de criar associação Fundo↔Cedente ATIVA quando já existe ATIVA → toast destrutivo `DuplicateActiveAssociationException` (D-18); UI sugere mover existente para INATIVO/HISTORICO antes.
  - Janela de datas Zod-validada (`data_fim` opcional, se presente > `data_inicio`).
  - Limites: regra final reflete decisão do planner backend (D-21 out-of-scope nota); form aceita ambos opcionais por default; refina conforme retorno do reviewer.
  - Cobertura ≥80%.
- **Dependencies:** T-1, T-2, T-3, T-4, T-6 (T-5 não é dependência direta — ConsultoriaFundo/Custodiante não participam de associações)
- **Test:** Vitest + Playwright regression (associação completa: create → status transition → REL-09 conflict)
- **Status:** pending

### Wave 4 (sequential — regression suite)

#### T-8: Playwright regression suite — Fundos client end-to-end
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/client/tests/e2e/fundos-tipo-ativo.spec.ts`
  - `frontend/client/tests/e2e/fundos-cedente.spec.ts`
  - `frontend/client/tests/e2e/fundos-consultoria-custodiante.spec.ts`
  - `frontend/client/tests/e2e/fundos-fundo.spec.ts` (CRUD + status transition + allowed-transitions integration)
  - `frontend/client/tests/e2e/fundos-associations.spec.ts` (3 associações + REL-09)
  - `frontend/client/tests/e2e/fundos-permissions.spec.ts` (viewer só vê; admin-empresa CRUD)
  - `frontend/client/playwright.config.ts` (registrar new project block se necessário — manter `localhost` per D-17 refined)
  - `scripts/seed-test-users.sh` (estender se necessário per D-14 — adicionar users com diferentes permission groups)
- **Acceptance:**
  - Suite passa em `npm run test:e2e` contra stack `docker compose up` (D-16).
  - Cobre golden path de cada entidade + state machine + REL-09 + permission gating + erro handling (inline + toast).
  - Baseado em `127.0.0.1`/`localhost` consistente com D-17 refined; HttpOnly cookies via auth-server flow.
  - Sem regression em specs existentes (`auth-flow.spec.ts`, `admin-auth-flow.spec.ts`, `api-proxy.spec.ts`).
- **Dependencies:** T-1, T-2, T-3, T-4, T-5, T-6, T-7
- **Test:** Playwright (próprio); CI run com `pnpm-lock.yaml` atualizado.
- **Status:** pending

## Execution

- Total tasks: 8
- Waves: 4
- Estimated parallel speedup: ~2x (Wave 1: 2 tasks parallel; Wave 2: 4 tasks parallel; Wave 3 + 4 sequential)

## Files modified (summary)

- Backend: `src/Onboarding.Domain/Aggregates/{Fundo,FundoCedente,FundoTipoAtivo,CedenteTipoAtivo}Aggregate/*.cs`, `src/Onboarding.Application/Fundos/Queries/*.cs`, `src/Onboarding.API/Controllers/{Fundos,FundoCedentes,FundoTiposAtivos,CedenteTiposAtivos}Controller.cs`, `tests/Onboarding.{Domain,Application,API}.Tests/**`
- Frontend: `frontend/client/package.json`, `frontend/client/pnpm-lock.yaml`, `frontend/client/src/{main.tsx,router.tsx}`, `frontend/client/src/lib/{query-client,api-errors,fundos-schemas,fundos-api,use-allowed-transitions}.ts`, `frontend/client/src/components/{atoms,molecules,organisms,pages}/**`, `frontend/client/src/tests/**`, `frontend/client/tests/e2e/fundos-*.spec.ts`, `frontend/client/src/components/organisms/Sidebar.tsx`
- Scripts: `scripts/seed-test-users.sh` (se aplicável)

## Test requirements

- Backend: xUnit + Shouldly (domain + application + API tests) — `dotnet test`
- Frontend unit: Vitest — `pnpm --filter frontend-client test`
- Frontend e2e: Playwright — `pnpm --filter frontend-client test:e2e` (stack via `docker compose up`)
- Lint: `pnpm --filter frontend-client lint`; typecheck: `pnpm --filter frontend-client typecheck`
- Coverage minimum: 80% em arquivos novos (D-2 boundary commit `968eefb`); legacy não enforced
- Security: pipeline 13-tool cross-cutting (Semgrep, CodeQL, Trivy, etc) — sem regression em D-12 (cookies HttpOnly), CORS, PKCE
