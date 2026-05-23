# Phase 52: frontend-backoffice-fundos — Plan  (slug: frontend-backoffice-fundos)

## Goal

SPA Backoffice (`frontend/backoffice` porta 5174) ganha módulo Fundos read-only cross-company: list/detail das 4 entidades (Fundo, ConsultoriaFundo, Custodiante, Cedente) + 3 N-N associations (FundoCedente, CedenteTipoAtivo, FundoTipoAtivo) com coluna Empresa + filtro dropdown + search global + URL params + detail page com audit history inline. Code-split lazy routes em ambos SPAs (resolve W-perf carry-forward de Phase 51). Sem CRUD (FRO-04).

## Locked decisions (from CONTEXT.md)

- **D-28:** Escopo full — 4 entidades + 3 N-N cross-company read-only; sem TipoAtivo (global D-5).
- **D-29:** TanStack Query backoffice independente do client SPA (D-4); QueryClient próprio.
- **D-30:** Detail page por entidade com audit history inline via AdminAuditLog filtrado.
- **D-31:** Coluna Empresa + dropdown filter + search global + URL params (TanStack Router validateSearch Zod).
- **D-32:** Code-split lazy routes ambos SPAs; bundle main ≤300 KB gz.

## Tasks

### Wave 1 (parallel-eligible)

#### T-1: Backend — extend AdminAuditLog filter + audit emission for Fundos transitions
- **Specialist:** jdi-doer-onboarding-keycloak-backend-csharp
- **Files modified:**
  - `src/Onboarding.Application/Admin/Queries/GetAuditLogQuery.cs` (add `string? EntityType` + `Guid? EntityId` params)
  - `src/Onboarding.Domain/Repositories/IAdminAuditLogRepository.cs` (extend `GetPagedAsync` signature)
  - `src/Onboarding.Infrastructure/Repositories/AdminAuditLogRepository.cs` (apply new WHERE filters)
  - `src/Onboarding.Domain/Aggregates/Audit/AdminAuditLog.cs` (add `EntityType` + `EntityId` columns if missing)
  - `src/Onboarding.Infrastructure/Persistence/Configurations/AdminAuditLogConfiguration.cs` (EF Core mapping)
  - `src/Onboarding.Infrastructure/Persistence/Migrations/<timestamp>_AddAuditLogEntityRef.cs` (EF migration — additive, nullable cols)
  - `src/Onboarding.API/Controllers/AdminUserController.cs` (audit-log endpoint accepts `entityType` + `entityId` query params)
  - Backfill / writer changes — wherever state-machine transitions (D-9/D-22) currently log via `AdminAuditLog.Create()`, pass `entityType` + `entityId` (FundosController.PostStatus, FundoCedentesController state transitions etc).
  - `tests/Onboarding.Application.Tests/Admin/GetAuditLogQueryTests.cs` (new test cases for entity filter)
  - `tests/Onboarding.API.Tests/Controllers/AdminUserControllerAuditLogTests.cs` (controller tests)
  - `tests/Onboarding.Integration.Tests/Admin/AuditLogEntityFilterIntegrationTests.cs` (round-trip)
- **Acceptance (DoD G0 + tests):**
  - `GET /api/admin/audit-log?entityType=Fundo&entityId=<guid>` returns 200 with only events scoped to that entity.
  - Existing call without filter returns all events (backward-compat — additive params).
  - EF migration is reversible. Existing rows have NULL EntityType/EntityId (acceptable — legacy events not scoped).
  - State-machine transition write paths populate EntityType+EntityId on new emissions (Fundo, FundoCedente, FundoTipoAtivo, CedenteTipoAtivo).
  - Cross-tenant guard: admin scheme `BearerBackoffice` only — confirmed.
  - Coverage ≥80% on new code.
  - **G0 DoD evidence:** curl with admin Bearer demonstrates filter works; integration test boots real DB and confirms round-trip.
- **Dependencies:** none
- **Test:** xUnit (domain + application + API + integration) + curl evidence
- **Status:** pending

#### T-2: Frontend backoffice foundation — TanStack Query + schemas + api + sidebar + route shell
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/backoffice/package.json` (add `@tanstack/react-query`, `@tanstack/react-query-devtools` dev — INDEPENDENT install from client per D-4)
  - `frontend/backoffice/src/main.tsx` (instantiate QueryClient + provider)
  - `frontend/backoffice/src/lib/query-client.ts` (new — independent config; `retry:1`, `staleTime:30_000`; `staleTime:5*60_000` para empresas list)
  - `frontend/backoffice/src/lib/admin-fundos-schemas.ts` (new — zod schemas para Admin*Dto cross-company com `companyId` + `companyName`)
  - `frontend/backoffice/src/lib/admin-fundos-api.ts` (new — typed query functions chamando endpoints `/api/admin/fundos/*` + `/api/admin/audit-log`)
  - `frontend/backoffice/src/lib/admin-companies-api.ts` (new — `listCompanies()` para dropdown empresa)
  - `frontend/backoffice/src/components/templates/AdminLayout.tsx` ou Sidebar (add grupo "Fundos" com items lazy)
  - `frontend/backoffice/src/router.tsx` (shell routes Fundos + lazy components per D-32)
  - `frontend/backoffice/src/tests/lib/admin-fundos-schemas.test.ts`
  - `frontend/backoffice/src/tests/lib/admin-fundos-api.test.ts`
- **Acceptance:**
  - `pnpm install` succeeds; backoffice has its OWN `@tanstack/react-query` install (NOT shared with client per D-4).
  - QueryClient + Provider wired; devtools optional in dev.
  - Schemas cover 4 admin entity DTOs + 3 N-N association DTOs + companies list.
  - Sidebar grupo "Fundos" renders only when admin authenticated.
  - Routes registered using `lazyRouteComponent` (Vinxi/TanStack Router pattern) — stub components OK iter inicial.
  - typecheck + lint exit 0; coverage ≥80% on new files.
  - **G0 DoD evidence:** Vinxi build report shows fundos route code in separate chunk (lazy).
- **Dependencies:** none
- **Test:** Vitest + Playwright (sidebar visibility)
- **Status:** pending

#### T-3: Frontend client — retroactive code-split lazy routes for Fundos (resolve W-perf carry-forward)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/client/src/router.tsx` (move imports `@/components/pages/Fundo*`, `@/components/pages/Cedente*`, `@/components/pages/Tipos*`, `@/components/pages/Consultorias*`, `@/components/pages/Custodiantes*` para `lazyRouteComponent(() => import('...'))`)
  - `frontend/client/vite.config.ts` ou `app.config.ts` (rollup chunk strategy if needed)
  - `frontend/client/src/tests/router.test.tsx` (update existing test that imports the routes)
  - Tests existentes que mockam pages — atualizar para shape lazy.
- **Acceptance:**
  - `pnpm --filter frontend-client build` — main chunk ≤300 KB gz (Vinxi build report). Fundos routes em chunks separados.
  - typecheck + lint clean.
  - Vitest 643+/15 still green.
  - **G0 DoD evidence:** MCP navigate `/fundos` — page still loads (Suspense fallback brief, depois page render). Zero invariant. Bundle visualizer screenshot mostra Fundos chunk separado.
- **Dependencies:** none
- **Test:** Vitest + Playwright (existing fundos suite still passes) + bundle report
- **Status:** pending

### Wave 2 (parallel-eligible — depend on T-2)

#### T-4: Backoffice — 4 entity list pages cross-company (Fundo, ConsultoriaFundo, Custodiante, Cedente)
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/backoffice/src/components/pages/AdminFundosListPage.tsx`
  - `frontend/backoffice/src/components/pages/AdminConsultoriasFundoListPage.tsx`
  - `frontend/backoffice/src/components/pages/AdminCustodiantesListPage.tsx`
  - `frontend/backoffice/src/components/pages/AdminCedentesListPage.tsx`
  - `frontend/backoffice/src/components/molecules/AdminFundoTable.tsx`
  - `frontend/backoffice/src/components/molecules/AdminCedenteTable.tsx`
  - `frontend/backoffice/src/components/molecules/AdminConsultoriaFundoTable.tsx`
  - `frontend/backoffice/src/components/molecules/AdminCustodianteTable.tsx`
  - `frontend/backoffice/src/components/molecules/EmpresaFilterDropdown.tsx` (new — shared, consome admin-companies-api)
  - `frontend/backoffice/src/components/molecules/AdminPaginator.tsx` (new — shared, padrão D-27)
  - `frontend/backoffice/src/components/molecules/AdminSearchInput.tsx` (new — debounce 300ms, shared)
  - `frontend/backoffice/src/router.tsx` (rotas `/admin/fundos`, `/admin/cedentes`, `/admin/consultorias-fundo`, `/admin/custodiantes` — todas lazy)
  - `frontend/backoffice/src/tests/pages/AdminFundosListPage.test.tsx`
  - `frontend/backoffice/src/tests/pages/AdminCedentesListPage.test.tsx`
  - `frontend/backoffice/src/tests/components/EmpresaFilterDropdown.test.tsx`
- **Acceptance:**
  - 4 listagens funcionais: tabela com coluna Empresa (companyName), paginator numérico (page size 20), search debounced 300ms, empresa filter dropdown.
  - URL params via `validateSearch` Zod: `?page=N&empresaId=UUID&search=Q`.
  - Skeleton em fetch inicial, spinner em refetch.
  - **G0 DoD evidence (MCP runtime):**
    - MCP login admin backoffice → navigate cada `/admin/<entity>` → tabela renderiza com dados de >=2 empresas.
    - Network MCP: GET `/api/admin/fundos?page=1&pageSize=20` retorna 200; click empresa filter → novo GET com `?empresaId=...`.
    - URL bookmarkable: nova aba com `?page=2&empresaId=X` carrega corretamente.
    - Console MCP: zero invariant errors.
    - Screenshot evidência cada listagem.
  - Coverage ≥80% nos arquivos novos.
- **Dependencies:** T-2
- **Test:** Vitest + Playwright regression (each list flow)
- **Status:** pending

#### T-5: Backoffice — 4 entity detail pages cross-company
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/backoffice/src/components/pages/AdminFundoDetailPage.tsx`
  - `frontend/backoffice/src/components/pages/AdminCedenteDetailPage.tsx`
  - `frontend/backoffice/src/components/pages/AdminConsultoriaFundoDetailPage.tsx`
  - `frontend/backoffice/src/components/pages/AdminCustodianteDetailPage.tsx`
  - `frontend/backoffice/src/components/molecules/AdminEntityHeader.tsx` (shared header: nome + empresa + status badge)
  - `frontend/backoffice/src/components/molecules/AdminFieldsGrid.tsx` (read-only key/value renderer)
  - `frontend/backoffice/src/router.tsx` (rotas detail `/admin/fundos/$fundoId`, etc — lazy)
  - `frontend/backoffice/src/tests/pages/AdminFundoDetailPage.test.tsx`
  - `frontend/backoffice/src/tests/pages/AdminCedenteDetailPage.test.tsx`
- **Acceptance:**
  - 4 detail pages: header com nome + empresa + status; grid de fields read-only; nav 404-gracioso para id inválido.
  - **G0 DoD evidence (MCP):**
    - Click row em cada list → navigate detail page → render sem invariant.
    - Network MCP: GET `/api/admin/fundos/$id` retorna 200; body shape mapeado.
    - Negative: navigate `/admin/fundos/00000000-0000-0000-0000-000000000000` → 404-friendly empty state, sem crash.
    - Screenshot cada detail.
  - Coverage ≥80%.
- **Dependencies:** T-2 (foundation); not T-4 (independent — detail tem rota dedicada).
- **Test:** Vitest + Playwright
- **Status:** pending

#### T-6: Backoffice — 3 N-N association list pages cross-company
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/backoffice/src/components/pages/AdminFundoCedentesListPage.tsx`
  - `frontend/backoffice/src/components/pages/AdminCedenteTiposAtivosListPage.tsx`
  - `frontend/backoffice/src/components/pages/AdminFundoTiposAtivosListPage.tsx`
  - `frontend/backoffice/src/components/molecules/AdminAssociationTable.tsx` (shared — colunas: Empresa, Entidade pai 1, Entidade pai 2, Status, Janela datas, Limites)
  - `frontend/backoffice/src/router.tsx` (rotas `/admin/fundo-cedentes`, etc — lazy)
  - `frontend/backoffice/src/tests/pages/AdminFundoCedentesListPage.test.tsx`
  - `frontend/backoffice/src/tests/components/AdminAssociationTable.test.tsx`
- **Acceptance:**
  - 3 association lists funcionais: tabela com empresa, ambos lados, status, janela datas, limites.
  - Paginator + empresa filter + URL params (reusam molecules T-4).
  - **G0 DoD:** MCP navigate cada list → 200 GET → tabela renderiza com >=2 empresas distintas visíveis. Screenshot cada.
  - Coverage ≥80%.
- **Dependencies:** T-2, T-4 (compartilha EmpresaFilterDropdown/AdminPaginator/AdminSearchInput criados em T-4).
- **Test:** Vitest + Playwright
- **Status:** pending

### Wave 3 (sequential — audit history depends on T-1 + T-5)

#### T-7: Backoffice — audit history inline component + integration nos detail pages
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/backoffice/src/components/molecules/AuditHistorySection.tsx` (new — consome `/api/admin/audit-log?entityType=<x>&entityId=<id>` via TanStack Query)
  - `frontend/backoffice/src/components/atoms/AuditEventRow.tsx` (new — render timestamp + admin + action + transition delta)
  - `frontend/backoffice/src/components/pages/AdminFundoDetailPage.tsx` (add `<AuditHistorySection entityType="Fundo" entityId={fundoId} />`)
  - `frontend/backoffice/src/components/pages/AdminCedenteDetailPage.tsx` (analog)
  - `frontend/backoffice/src/components/pages/AdminConsultoriaFundoDetailPage.tsx` (analog)
  - `frontend/backoffice/src/components/pages/AdminCustodianteDetailPage.tsx` (analog)
  - `frontend/backoffice/src/lib/admin-fundos-api.ts` (add `getAuditHistory(entityType, entityId, page)` function)
  - `frontend/backoffice/src/tests/components/AuditHistorySection.test.tsx`
- **Acceptance:**
  - Cada detail page mostra section "Histórico" com eventos ordenados (mais recente primeiro), paginated.
  - Empty state gracioso se entidade sem histórico.
  - **G0 DoD:** MCP navigate detail Fundo → audit section carrega → Network MCP confirma GET `/api/admin/audit-log?entityType=Fundo&entityId=<id>` 200. Disparar transição via API (curl ou flow client) → refresh detail → novo evento aparece (proof round-trip live). Screenshot evidência.
  - Coverage ≥80%.
- **Dependencies:** T-1 (backend filter), T-5 (detail pages mount point)
- **Test:** Vitest + Playwright (audit appears after transition fired)
- **Status:** pending

### Wave 4 (sequential — Playwright regression suite cobrindo tudo)

#### T-8: Playwright regression suite — backoffice Fundos end-to-end + bundle gate
- **Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
- **Files modified:**
  - `frontend/backoffice/tests/e2e/admin-fundos-list.spec.ts` (list flow: login → list → filter → search → paginator)
  - `frontend/backoffice/tests/e2e/admin-fundos-detail.spec.ts` (detail + audit history flow)
  - `frontend/backoffice/tests/e2e/admin-fundos-associations.spec.ts` (3 N-N lists)
  - `frontend/backoffice/tests/e2e/admin-fundos-permissions.spec.ts` (non-admin recusado; admin acessa)
  - `frontend/backoffice/playwright.config.ts` (register new fundos project — match Phase 51 client pattern per D-17 refined)
  - `frontend/backoffice/tests/e2e/fixtures/admin-creds.json` (gitignored — seeded by `scripts/seed-test-users.sh`)
  - `scripts/seed-test-users.sh` (extend if needed — backoffice admin seeded)
- **Acceptance:**
  - Suite passa em `pnpm --filter frontend-backoffice test:e2e` contra stack `docker compose up`.
  - Cobre golden path de cada entidade + associations + audit + permissions.
  - **G0 DoD:** este é o próprio test do gate — passar é evidência. Reviewer roda suite e cola output.
  - Sem regression em specs existentes backoffice (admin-companies, admin-employees, etc).
  - Bundle gate verificado em `pnpm --filter frontend-backoffice build` — main chunk ≤300 KB gz; Vinxi build report listado em REVIEW.md.
- **Dependencies:** T-1, T-2, T-3, T-4, T-5, T-6, T-7
- **Test:** Playwright (próprio) + bundle gate
- **Status:** pending

## Execution

- Total tasks: 8
- Waves: 4
- Estimated parallel speedup: ~2x

## Files modified (summary)

- Backend: `src/Onboarding.Application/Admin/Queries/GetAuditLogQuery.cs`, `src/Onboarding.Domain/{Aggregates/Audit,Repositories}/*`, `src/Onboarding.Infrastructure/{Repositories,Persistence/Configurations,Persistence/Migrations}/*`, `src/Onboarding.API/Controllers/AdminUserController.cs`, all FundosControllers writing audit, `tests/Onboarding.{Application,API,Integration}.Tests/Admin/**`.
- Frontend backoffice: `frontend/backoffice/{package.json,src/main.tsx,src/router.tsx,src/lib/*,src/components/{atoms,molecules,pages,templates}/**,src/tests/**,tests/e2e/**,playwright.config.ts}`.
- Frontend client: `frontend/client/src/router.tsx` (lazy routes retroativo).
- Scripts: `scripts/seed-test-users.sh` (admin user).

## Test requirements

- Backend: xUnit + Shouldly — `dotnet test` (Domain + Application + API + Integration). Integration with Docker preferred for G0.
- Frontend unit: Vitest — `pnpm --filter frontend-backoffice test`
- Frontend e2e: Playwright — `pnpm --filter frontend-backoffice test:e2e` (stack via `docker compose up`)
- Lint/typecheck: `pnpm --filter frontend-backoffice lint && pnpm --filter frontend-backoffice typecheck` (idem para client após T-3)
- Bundle: `pnpm --filter frontend-backoffice build` + `pnpm --filter frontend-client build` — main ≤300 KB gz
- Coverage: ≥80% em arquivos NOVOS (D-2 boundary 968eefb)
- Security: 13-tool pipeline + multi-tenant guard + D-12 cookies + D-4 separation

## DoD enforcement note

Reviewer DEVE seguir checklist DoD em CONTEXT.md secao "Definition of Done". Verdict APPROVED ou APPROVED_WITH_WARNINGS exige TODOS os checklist items confirmados com evidência. Warnings tipo "MCP not run" ou "endpoint not exercised" SÃO BLOCKERS — não warnings — per PROJECT.md DoD policy.
