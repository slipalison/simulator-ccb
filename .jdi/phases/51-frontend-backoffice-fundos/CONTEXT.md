# Phase 52 — frontend-backoffice-fundos — CONTEXT

## Goal

SPA Backoffice admin (`frontend/backoffice`, porta 5174, auth ACF+PKCE com tema custom Keycloak) ganha módulo Fundos **read-only cross-company**: list/detail das 4 entidades (Fundo, ConsultoriaFundo, Custodiante, Cedente) + 3 associações N-N (FundoCedente, CedenteTipoAtivo, FundoTipoAtivo). Cada lista mostra coluna "Empresa" (razão social), filtro dropdown por empresa, search global, paginação skip/take 20/page, URL search params via Zod. Detail page por entidade exibe campos + hist órico inline de transições do AdminAuditLog (D-9/D-22). Sem CRUD (FRO-04 — backoffice é read-only para Fundos). Endpoints já prontos em `AdminFundosController` (Phase 48) — BearerBackoffice scheme + Policy CrossCompanyAccess.

## Locked decisions (Phase 52)

- **D-28 (DA-1):** Escopo full — UI cobre 4 entidades cross-company (Fundo, ConsultoriaFundo, Custodiante, Cedente) + 3 associações N-N (FundoCedente, CedenteTipoAtivo, FundoTipoAtivo) read-only. TipoAtivo NÃO incluído (catálogo global D-5, não cross-company-relevant). Planner organiza waves por entidade.

- **D-29 (DA-2):** **TanStack Query** (`@tanstack/react-query`) adicionado ao `frontend/backoffice` (instância INDEPENDENTE do client SPA per D-4 — sem cross-import, sem shared QueryClient config). Mesmo padrão de query keys + cache invalidation. Setup `QueryClient` + `QueryClientProvider` em `main.tsx`. `retry:1`, `staleTime:30_000`. Transport via existing apiFetch backoffice (verificar se já existe; senão criar paralelo ao client `lib/api.ts`).

- **D-30 (DA-3):** **Detail page por entidade** (`/admin/fundos/$fundoId`, `/admin/cedentes/$cedenteId` etc) com **audit history inline**. Tab/section dedicada renderiza transições de status do AdminAuditLog filtradas pela entidade (entityType + entityId match). Backend já loga via D-9/D-22 — frontend consome endpoint existente de AdminAuditLog (`/api/admin/audit-log`) filtrado. Se endpoint não suportar filter por entityId, planner inclui task backend para adicionar query param.

- **D-31 (DA-4):** Listas cross-company têm **coluna "Empresa" (razão social)** + **filtro dropdown por empresa** (lista todas via endpoint companies) + **search global** (debounce 300ms, hits backend search param) + **paginator numérico** (page size 20) + URL search params via TanStack Router `validateSearch` (Zod). Estado `?page=N&empresaId=UUID&search=Q` bookmarkable. Skeleton em fetch inicial; spinner em refetch.

- **D-32 (DA-5):** **Code-split lazy routes em AMBOS SPAs** (`frontend/client` + `frontend/backoffice`). Resolve W-perf carry-forward de Phase 51 (client bundle 765 KB raw, Vite chunk warning) + previne bundle grow no backoffice desde o início. Pattern: `createRoute({ component: lazyRouteComponent(() => import('@/components/pages/...')) })` ou React.lazy + Suspense fallback. Aplicar especialmente em routes pesadas (forms, tables Fundos). Bundle gate: main ≤300 KB gz após split.

## Canonical refs

- `.jdi/DECISIONS.md` D-4 (frontend separation — sem cross-import client/backoffice), D-5 (multi-tenant — TipoAtivo global; demais aggregates company-scoped), D-9/D-22 (state-machine + AdminAuditLog pattern), D-11/D-12 (ACF+PKCE + cookies HttpOnly), D-18..D-22 (associações shape), D-28..D-32 (esta phase).
- `.jdi/PROJECT.md` — stack confirmed: React 19 + Vinxi 0.5 + TanStack Router + Tailwind 4 + radix-ui + sonner.
- `frontend/backoffice/src/router.tsx` — routes existentes (`/admin/companies`, `/admin/employees`, `/admin/users`, `/admin/audit-log`). Phase 52 adiciona child routes Fundos.
- `frontend/backoffice/src/components/{atoms,molecules,pages,templates}/` — atomic design existente (sem `organisms/` — backoffice não usa essa camada).
- `frontend/backoffice/src/components/templates/AdminLayout.tsx` — layout wrapper com sidebar.
- `src/Onboarding.API/Controllers/AdminFundosController.cs` — 7 GET endpoints cross-company prontos (fundos, consultorias, custodiantes, cedentes, fundo-cedentes, fundo-tipos-ativos, cedente-tipos-ativos). Auth `BearerBackoffice` + Policy `CrossCompanyAccess` (requires role "admin" do realm backoffice).
- `src/Onboarding.Application/Fundos/Queries/Admin/` (ou similar) — query handlers cross-company com `IgnoreQueryFilters()`.
- `src/Onboarding.API/Controllers/AdminAuditLogController.cs` (verificar nome exato) — endpoint audit log para integração D-30.

## Out of scope

- **CRUD operations** — Backoffice é estritamente read-only para Fundos (FRO-04). Create/Update/Delete fica no client SPA (Phase 51).
- **Status transitions** — backoffice NÃO dispara state-machine actions; apenas visualiza histórico.
- **TipoAtivo** — catálogo global (D-5), não cross-company. Se backoffice precisar gerenciar TipoAtivo (catálogo CVM), phase futura dedicada.
- **Exportação CSV/Excel** — backlog.
- **Bulk operations** — backlog.
- **W-G4.4** (Meter placement) — Phase 53 mandate (Telemetry/ directory creation).
- **W-G2** (Authorize justification comment) — micro-cleanup, Phase 53.
- **W-otel** OTel JS SPAs — Phase 53 mandate.
- **Vinxi→Vinext migration** — Phase 54 dedicada.
- **W-react-setstate** Transitioner intermittent — pre-existing, sem fix nesta phase.
- **W-audit-format** JsonStringEnumConverter int→string carry — operacional, atualizar dashboards externos fora deste repo.

## Notes

- **Permission gating:** backoffice users têm role `admin` do realm backoffice. Não há sub-roles para Fundos read — qualquer admin acessa. Mas Sidebar deve verificar `auth.isAuthenticated && hasAdminRole` antes de mostrar menu Fundos.
- **Sidebar:** backoffice atual tem entries para Companies/Employees/Users/AuditLog. Phase 52 adiciona grupo "Fundos" com items (Fundos, Cedentes, Consultorias, Custodiantes) + sub-items associações (em accordion ou separador visual). Lazy-load routes per D-32.
- **AdminAuditLog inline (D-30):** filter shape `entityType=Fundo&entityId=<guid>` ou similar. Se endpoint atual `/api/admin/audit-log` aceitar filter via query params, plug-and-play. Se não, planner inclui task backend pra adicionar `entityType`/`entityId` filter (minimal, additive — não quebra contrato existente).
- **Empresa filter dropdown (D-31):** endpoint pra listar empresas — provavelmente `GET /api/admin/companies` já existe (backoffice tem AdminCompaniesPage). Verifica e reusa. Cachear via TanStack Query com staleTime longo (5min) — lista de empresas muda pouco.
- **Code-split (D-32):** Vinxi suporta dynamic `import()` para route components. Aplicar em `frontend/client/src/router.tsx` retroativamente (move imports de `@/components/pages/Fundo*` etc para lazy). Backoffice nasce já lazy. Test que initial bundle não inclui Fundos page code antes do route hit. Bundle analyzer (Vinxi build report) ou rollup-plugin-visualizer pra evidência.
- **TanStack Query setup (D-29):** sem cross-import client. Criar `frontend/backoffice/src/lib/query-client.ts` paralelo ao client. `frontend/backoffice/src/lib/admin-fundos-api.ts` para query/mutation functions tipadas. Zod schemas em `frontend/backoffice/src/lib/admin-fundos-schemas.ts` espelham DTOs cross-company (incluem `companyId` + `companyName`).
- **Mandatory Playwright regression:** suite cobre admin login → load /admin/fundos → empresa filter dropdown → row click → detail page com audit history. Reviewer rejeita ship sem isso (reviewer spec).
- **W-seed carry-forward (Phase 51):** seed-test-users.sh já sincroniza companies.keycloak_user_id. Phase 52 testa que admin user (backoffice realm) consegue ver Fundos cross-company. Adicionar admin test user no seed se ausente.
- **Specialist routing:** `frontend/backoffice/**` → `jdi-doer-onboarding-keycloak-frontend-vinext` (mesmo specialist, escopo D-4 isolado por glob path).
- **Coverage 80%:** D-2 boundary apenas em arquivos novos.

## Definition of Done (Phase 52 specific — derived from PROJECT.md DoD policy)

Reviewer DEVE confirmar TODOS os itens abaixo antes de stamp APPROVED ou APPROVED_WITH_WARNINGS. Caso contrario, verdict eh BLOCKED.

### Per entity (Fundo, ConsultoriaFundo, Custodiante, Cedente)

**Listagem cross-company (4 itens):**
- [ ] MCP login admin backoffice → navigate `/admin/<entity>` → tabela renderiza com colunas (Nome, Empresa, Status, ...) — screenshot evidencia.
- [ ] Coluna "Empresa" mostra razao social — dados reais de >=2 empresas diferentes visiveis na mesma pagina.
- [ ] Dropdown filtro empresa → change dispara GET com `?empresaId=<UUID>` (verificado via Network MCP); tabela refiltra.
- [ ] Search input → debounced 300ms → GET com `?search=<term>`; tabela refiltra.
- [ ] Paginator → click next → GET com `?page=2`; URL atualiza com search params persistidos.
- [ ] Estado bookmarkable — abrir URL `?page=N&empresaId=X&search=Q` em nova aba carrega no estado correto.

**Detail page (4 itens):**
- [ ] Click row → navigate `/admin/<entity>/$id` sem invariant errors (Console MCP zero erros).
- [ ] Campos da entidade renderizam (id, nome, status, datas, etc) — proof via screenshot.
- [ ] Section "Histórico" carrega via GET `/api/admin/audit-log?entityType=<type>&entityId=<id>` (Network MCP verificado); eventos renderizam em ordem cronologica.
- [ ] Detail page de id invalido → empty state gracioso (sem crash, sem invariant).

### Per association (FundoCedente, CedenteTipoAtivo, FundoTipoAtivo)

- [ ] Listagem cross-company com colunas Empresa + entidades pai (FundoNome/CedenteNome etc) + Status + janela datas + limites.
- [ ] Paginator + search + URL params per D-31.
- [ ] Detail (se aplicavel ao plan) com audit history inline.

### Backend (se Phase 52 introduzir endpoints novos, ex: audit log filter)

- [ ] Endpoint responde 2xx para happy path com Bearer admin valido (curl ou integration test).
- [ ] Cross-company guard ativo — admin scheme `BearerBackoffice` + Policy `CrossCompanyAccess`. Token client SPA recusado → 401/403.
- [ ] Tests integration boot real container.

### Sidebar

- [ ] AdminLayout sidebar mostra group "Fundos" para admin authenticated.
- [ ] Cada item navegavel sem erro.

### Cross-cutting

- [ ] Console MCP — zero "Invariant failed" ou erros nao tratados durante toda navegacao.
- [ ] Network MCP request list — todas chamadas `/api/admin/*` retornam 2xx (exceto deliberate negative test).
- [ ] Bundle main chunk ≤300 KB gz APOS code-split D-32 (Vinxi build report).
- [ ] No token em `localStorage`/`sessionStorage` (D-12). Verificado via `browser_evaluate`.
- [ ] D-4 — backoffice nao importa nada de `frontend/client/`. Grep zero matches.

### Evidencia obrigatoria em REVIEW.md

- HTTP status verbatim para cada endpoint exercitado (admin fundos, audit log filter, etc).
- Screenshot path em `.jdi/cache/phase-51-<scenario>.png` para cada cenario UI.
- Bundle gzip size (Vinxi build report output).
- Multi-tenant cross-probe evidence (Network filter showing 404 para tenant errado se aplicavel).
