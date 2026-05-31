# Phase 51 — frontend-client-fundos — CONTEXT

## Goal

SPA cliente PJ (`frontend/client`, porta 5173) ganha seção Fundos completa: CRUD das 5 entidades (`Fundo`, `ConsultoriaFundo`, `Custodiante`, `Cedente`, `TipoAtivo`) + UI das 3 associações N-N (`FundoCedente`, `CedenteTipoAtivo`, `FundoTipoAtivo`) com payload (limites + janela de datas + status) + state-machine transitions via dropdown. Forms Zod espelham regras backend; erros do domínio refletidos por field; listagens paginadas com search debounced; estado navegável via URL search params. Permissões `funds:read/write/delete/manage` enforced no router/UI (esconder/desabilitar ações conforme `auth.accessGroup`).

## Locked decisions (Phase 51)

- **D-23 (DA-1):** Escopo full — UI cobre as 5 entidades CRUD + 3 associações N-N nesta phase. Sem split em sub-phase. Phase grande aceita; planner organiza em waves de paralelismo por entidade. Reviewer recusa shortcuts tipo "Cedente só dropdown read-only" sem aprovação explícita.

- **D-24 (DA-2):** **TanStack Query** (`@tanstack/react-query`) adicionado como dependência. Padrão de queries + mutations + cache invalidation + optimistic updates centralizado. `apiFetch` existente (`frontend/client/src/lib/api.ts` — auto-refresh 401) continua sendo o transport. Query keys padronizadas por entidade (`['fundos', { page, search }]` etc). Mutations invalidam keys relevantes. Sem persistência cache (memória only).

- **D-25 (DA-3):** Backend expõe **GET /api/{entity}/{id}/allowed-transitions** para cada entidade com state-machine (Fundo + 3 associações). Retorna `string[]` dos próximos estados válidos a partir do estado atual. Fonte única de verdade — frontend NÃO mantém tabela espelho. Implica trabalho backend dentro da Phase 51 (specialist backend invocado em wave dedicada antes da UI consumir). Domínio continua sendo gate definitivo (rejeita transição inválida com `InvalidStatusTransitionException` → 400).

- **D-26 (DA-4):** Reflexão de erros nos forms é **inline por field + toast crítico**. `ProblemDetails.errors[]` (FluentValidation) mapeado para `react-hook-form.setError` por campo. Exceptions tipadas de domínio (`DuplicateActiveAssociationException`, `DuplicateCnpjException`, `InvalidStatusTransitionException`, `ConflictException`) viram **toast sonner** com `description` específica e variant `destructive`. Erros não-mapeáveis a field ficam em `setError("root.serverError", ...)` exibido no topo do form. Toasts de sucesso usam variant default. Helper centralizado `mapApiErrorToForm()` em `lib/`.

- **D-27 (DA-5):** Listagens usam **skip/take + paginator numérico + search debounce 300ms + URL search params**. Estado `?page=N&search=Q&sort=X` persistido via TanStack Router `validateSearch` (Zod). Página/search bookmarkable e shareable. Skeleton loader durante fetch inicial; spinner inline em refetch. Page size default 20 (ajustável via constante). Sem infinite scroll, sem DataTable shadcn nesta phase (backlog se filtros complexos surgirem).

## Canonical refs

- `.jdi/DECISIONS.md` D-4 (frontend separation — sem cross-imports com backoffice), D-11 (ACF+PKCE), D-12 (cookies HttpOnly), D-9/D-22 (state-machine pattern `POST /status`), D-18..D-21 (associações shape).
- `.jdi/PROJECT.md` — stack: React 19 + Vinxi 0.5 + TanStack Router + Tailwind 4 + react-hook-form + zod + radix-ui + sonner.
- `frontend/client/src/router.tsx` — TanStack Router routes existentes (dashboard, employees, access-groups, profile). Phase 51 adiciona child routes em `authenticatedRoute`.
- `frontend/client/src/lib/api.ts` — `apiFetch` com auto-refresh 401. Reusar como transport por baixo do TanStack Query.
- `frontend/client/src/lib/validation-schemas.ts` — padrão Zod existente (Cnpj/Cpf/Email shared schemas — reusar).
- `frontend/client/src/components/{atoms,molecules,organisms,pages,templates}/` — atomic design existente. Novos componentes seguem essa estrutura.
- `src/Onboarding.API/Controllers/FundosController.cs` — endpoints Phase 48 (CRUD Fundo + status).
- `src/Onboarding.Application/Fundos/` — DTOs (espelhar shape no zod).
- `src/Onboarding.API/Security/PermissionPolicyConstants.cs` — `funds:read/write/delete/manage` (UI esconde/desabilita ações conforme group).

## Out of scope

- **Backoffice Fundos UI** — Phase 52 (`frontend-backoffice-fundos`), read-only cross-company.
- **i18n** das strings novas — manter pt-BR estruturado em arquivo `locales/pt-BR/fundos.ts` (sem hardcoded JSX), mas sem multi-locale nesta phase.
- **Imports em massa / bulk operations** — backlog.
- **Exportação CSV/Excel** das listagens — backlog.
- **Audit log viewer** integrado às telas de Fundos — usa AdminAuditLog existente, mas tela própria fica em phase futura.
- **DataTable shadcn (TanStack Table)** — não adicionada nesta phase; reavaliar se filtros multi-coluna virarem requisito.
- **Webhooks / notifications** em mudança de status — backlog (já fora de escopo desde Phase 50).
- **Migração Vinxi→Vinext** — Phase 54 dedicada; aqui continua Vinxi 0.5.11.
- **Optimistic updates agressivos** em mutations destrutivas (delete) — usar pessimistic (refetch após confirm). Optimistic só em status transition e edits de campos não-críticos.

## Notes

- **Permission-aware UI:** `funds:read` vê listas; `funds:write` vê create/edit; `funds:delete` vê delete; `funds:manage` vê status transition + admin. UI checa `auth.permissions` (já presente no auth-context) e renderiza condicionalmente. Backend é gate definitivo.
- **Allowed-transitions endpoint (D-25):** specialist backend cria 4 endpoints (`GET /api/fundos/{id}/allowed-transitions`, `GET /api/fundos/{fundoId}/cedentes/{cedenteId}/allowed-transitions`, idem para FundoTipoAtivo, CedenteTipoAtivo). Reusa state-machine definitions do domínio. Implementação: query handler que lê aggregate + chama método `GetAllowedNextStates()` no aggregate (já candidato a existir, ou adicionar como invariante explícita).
- **TanStack Query setup (D-24):** `QueryClient` instanciado em `main.tsx`, `QueryClientProvider` no root. Devtools opcionais em dev. `defaultOptions.queries.retry: 1` (404/401 já tratados pelo `apiFetch`). `staleTime: 30_000` default. Mutations sem retry.
- **Route topology:** novas routes em `frontend/client/src/router.tsx`:
  - `/fundos` (lista)
  - `/fundos/new`
  - `/fundos/$fundoId` (detail + tabs: dados, cedentes, tipos-ativos, status history)
  - `/cedentes` (lista) + `/cedentes/new` + `/cedentes/$cedenteId`
  - `/tipos-ativos` (lista admin global), `/consultorias-fundo`, `/custodiantes` — análogo.
  - Todas crianças de `authenticatedRoute` (com `AppLayout` + sidebar).
- **Sidebar:** adicionar grupo "Fundos" com items (Fundos, Cedentes, ConsultoriasFundo, Custodiantes, TiposAtivo). Visibilidade condicional via `funds:read`.
- **Form pattern (D-26):** helper `mapApiErrorToForm(err, setError, fieldMap?)` em `lib/api-errors.ts`. Toast destrutivo via `toast.error()` do sonner com `description`. Toast sucesso via `toast.success()`.
- **Pagination state (D-27):** URL schema validado por Zod em `validateSearch`. Default page=1, pageSize=20. Trocar página/search atualiza URL → invalida query (key inclui `{ page, search }`) → refetch.
- **Coverage 80%:** apenas em arquivos novos (D-2 boundary). Reviewer enforce via vitest + Playwright regression suite (mandatory por reviewer spec) em SPA client (5173).
- **Specialist routing:** `frontend/**` → `jdi-doer-onboarding-keycloak-frontend-vinext`. Endpoints novos backend (D-25) → `jdi-doer-onboarding-keycloak-backend-csharp` em wave dedicada.
- **Security:** sem regression em D-12 (cookies HttpOnly), sem token em storage, CORS preservado. Reviewer security verifica em verify.
