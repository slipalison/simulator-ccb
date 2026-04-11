---
phase: 19
plan: 02
name: Frontend Backoffice — Estrutura e Limpeza do Client
completed_at: "2026-04-09"
strategy_used: copy-first-then-delete
---

# Phase 19 — Plan 02 SUMMARY: Frontend Backoffice Migration + Monolith Cleanup

## Objective

Criar o projeto `frontend/backoffice` independente, migrar todas as telas e componentes admin, remover o `frontend/` monolith original, limpar rotas admin do client, e validar que ambos projetos rodam independentemente.

## What Was Done

### Files Created/Modified

**Scaffolding backoffice (8 files):**
- `frontend/backoffice/package.json` — name: "frontend-backoffice", dev script: `vinxi dev --port 5174 --host`
- `frontend/backoffice/tsconfig.json` — mesmo config do monolith
- `frontend/backoffice/components.json` — shadcn/ui config
- `frontend/backoffice/.dockerignore` — Docker exclusions
- `frontend/backoffice/Dockerfile` — `EXPOSE 5174`
- `frontend/backoffice/index.html` — SPA entry point
- `frontend/backoffice/app.config.ts` — 3 routers, port 5174, HMR config
- `frontend/backoffice/server.ts` — h3 proxy `/api/*` → `http://api:8080`

**Admin components migrated (15+ files):**
- **atoms/**: ProfileBadge.tsx, ProfileField.tsx
- **molecules/**: AdminLoginForm.tsx, AdminUsersTable.tsx, AdminPagination.tsx, AdminStatusFilter.tsx, AdminSearchBar.tsx, KeycloakStatusBadge.tsx, UserDetailCard.tsx
- **pages/**: AdminLoginPage.tsx, AdminAccessDeniedPage.tsx, AdminUsersPage.tsx, AdminUserDetailPage.tsx, NotFoundPage.tsx
- **templates/**: AdminLayout.tsx
- **ui/**: 13 shadcn/ui components (duplicados do client)

**Admin libs migrated (6 files):**
- `admin-auth-context.tsx` — Admin auth provider (cookie-based, httpOnly)
- `admin-api.ts` — Admin API clients (login, list users, detail, etc)
- `admin-error-handler.ts` — Error handling (401→login, 403→access-denied)
- `admin-http-interceptor.ts` — adminFetch with retry logic
- `theme-provider.tsx` — next-themes wrapper
- `types.ts` — Backoffice-specific types (UserSummaryDto, UserDetailDto, PaginatedResult)

**Tests migrated (10 files):**
- Todos os testes admin copiados para `frontend/backoffice/src/tests/`

**Client router cleaned:**
- `frontend/client/src/router.tsx` — ZERO rotas /admin/* removidas
- `frontend/client/src/main.tsx` — ZERO imports de AdminAuthProvider

**Monolith deleted:**
- `frontend/app.config.ts` ❌
- `frontend/server.ts` ❌
- `frontend/package.json` ❌
- `frontend/src/` ❌
- `frontend/node_modules/` ❌
- `frontend/` agora contém apenas `client/` e `backoffice/`

**Infrastructure updated:**
- `compose.yaml` — serviço `frontend` original removido, `frontend-backoffice` adicionado
- Agora: `frontend-client` (5173) + `frontend-backoffice` (5174)

### Key Decisions

1. **Backoffice usa cookies httpOnly** — diferente do client (memory tokens), mais seguro para admin
2. **Admin auth separada** — `AdminAuthProvider` isolado, sem conflito com user auth
3. **Porta 5174 para backoffice** — separação clara de ambientes
4. **Code duplication aceita** — shadcn/ui duplicados, sem imports cruzados

### Verification Results

- [x] `frontend/backoffice/` estrutura completa
- [x] package.json, app.config.ts, Dockerfile, tsconfig.json independentes
- [x] Todas as páginas admin migradas
- [x] Zero imports de módulos client no backoffice
- [x] Client router limpo — sem rotas /admin/
- [x] 10 testes unitários no backoffice
- [x] Monolith deletado — `frontend/` contém apenas `client/` e `backoffice/`
- [x] compose.yaml com ambos serviços, original removido

## Concerns / Lessons

- 10 testes no backoffice — cobertura menor que client (20), mas admin tem menos flows
- Backoffice usa cookie httpOnly — mais seguro, mas requer proxy config correto
- Nenhum cross-import detectado — isolamento total funcionou

## Next Steps

→ Phase 20: Admin E2E + Production (próxima fase do roadmap)
