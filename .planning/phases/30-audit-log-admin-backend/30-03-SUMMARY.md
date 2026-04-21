---
phase: 30-audit-log-admin-backend
plan: "03"
subsystem: frontend-backoffice
tags: [frontend, admin-api, react, typescript, ADM-04, gap-closure]
dependency_graph:
  requires:
    - 30-02 (GET /api/admin/administrators endpoint backend)
  provides:
    - AdminUserDto interface e getAdministrators() em admin-api.ts
    - AdminAdministratorsPage com tabela de admins
    - Rota /admin/administrators registrada no router
    - Link Administradores no sidebar do AdminLayout
  affects:
    - frontend/backoffice/src/lib/admin-api.ts
    - frontend/backoffice/src/tests/admin-api.test.ts
    - frontend/backoffice/src/components/pages/AdminAdministratorsPage.tsx
    - frontend/backoffice/src/router.tsx
    - frontend/backoffice/src/components/templates/AdminLayout.tsx
tech_stack:
  added: []
  patterns:
    - useEffect + useCallback para fetch com loading/error state (padrão existente)
    - Badge components para status visual (isEnabled, hasTemporaryPassword)
    - mockFetch.mockResolvedValue (sem Once) para testes com múltiplas chamadas
key_files:
  created:
    - frontend/backoffice/src/components/pages/AdminAdministratorsPage.tsx
  modified:
    - frontend/backoffice/src/lib/admin-api.ts
    - frontend/backoffice/src/tests/admin-api.test.ts
    - frontend/backoffice/src/router.tsx
    - frontend/backoffice/src/components/templates/AdminLayout.tsx
decisions:
  - "mockResolvedValue (sem Once) usado no teste de erro para suportar múltiplas chamadas na mesma asserção"
  - "Link Audit Log adicionado ao sidebar junto com Administradores — sidebar tinha comentário Future: Audit Log que foi convertido em link real"
metrics:
  duration: "~15 min"
  completed_date: "2026-04-16"
  tasks_completed: 2
  files_changed: 5
requirements:
  - ADM-04
---

# Phase 30 Plan 03: ADM-04 Frontend Gap Closure Summary

**One-liner:** Cliente TypeScript getAdministrators() com interface AdminUserDto, página de tabela AdminAdministratorsPage com badges de status, rota /admin/administrators registrada e link no sidebar do AdminLayout.

## What Was Built

### Task 1: AdminUserDto + getAdministrators() em admin-api.ts

Adicionado ao final de `admin-api.ts`:
- Interface `AdminUserDto` com campos: `id`, `email`, `fullName`, `isEnabled`, `hasTemporaryPassword`
- Função `getAdministrators()` fazendo `GET /api/admin/administrators` com `credentials: "include"`
- Padrão idêntico ao `getAuditLog()` existente no mesmo arquivo

Testes adicionados em `admin-api.test.ts` (3 novos, 10 total passando):
- Sucesso: retorna `AdminUserDto[]` com chamada correta ao endpoint
- Erro 503: lança `AdminApiError` com mensagem "Falha ao carregar administradores."
- Lista vazia: retorna array `[]` sem erro

### Task 2: AdminAdministratorsPage + rota + sidebar

**AdminAdministratorsPage.tsx** criado com:
- Estado: `admins[]`, `isLoading`, `isError` — padrão idêntico ao AdminUsersPage
- `useCallback` + `useEffect` para fetch ao montar
- Estados visuais: loading, error (com botão "Tentar novamente"), empty, tabela
- Tabela com 4 colunas: Nome, Email, Status, Senha Temporaria
- Status: `Badge variant="default"` (Ativo) / `Badge variant="destructive"` (Bloqueado)
- Senha Temporaria: `Badge variant="outline"` amber (Pendente) / green (Definida)
- data-testid em todos os elementos relevantes

**router.tsx** atualizado:
- Import de `AdminAdministratorsPage`
- Nova constante `adminAdministratorsRoute` em `path: "/admin/administrators"`
- Adicionada ao `routeTree.addChildren([...])`

**AdminLayout.tsx** atualizado:
- Link `Administradores` com `href="/admin/administrators"` e `data-testid="sidebar-administrators-link"`
- Link `Audit Log` com `href="/admin/audit-log"` e `data-testid="sidebar-audit-log-link"` (comentário Future convertido em link real)

## Verification Results

| Check | Result |
|-------|--------|
| `export interface AdminUserDto` em admin-api.ts | PASS |
| `export async function getAdministrators` em admin-api.ts | PASS |
| `npx vitest run src/tests/admin-api.test.ts` — 10/10 tests | PASS |
| `AdminAdministratorsPage` em router.tsx | PASS |
| `"/admin/administrators"` em router.tsx | PASS |
| `href="/admin/administrators"` em AdminLayout.tsx | PASS |
| `npx tsc --noEmit` exits 0 | PASS |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Teste de erro usava mockResolvedValueOnce com duas chamadas**

- **Found during:** Task 1 — primeira execução dos testes
- **Issue:** O teste "throws AdminApiError when response is not ok" chamava `getAdministrators()` duas vezes (duas asserções `rejects.toThrow`), mas o mock estava configurado com `mockResolvedValueOnce` (um único retorno). A segunda chamada recebia `undefined` e lançava `TypeError: Cannot read properties of undefined`.
- **Fix:** Alterado para `mockFetch.mockResolvedValue` (sem `Once`) para que o mock retorne o mesmo valor em todas as chamadas do teste.
- **Files modified:** `frontend/backoffice/src/tests/admin-api.test.ts`
- **Commit:** e15fcb6

### Additional Work (não desvio, melhoria aproveitada)

**Link Audit Log adicionado ao sidebar**

- O comentário `{/* Future: Audit Log, Settings */}` foi convertido em link real para `/admin/audit-log` (rota que já existia no router). A Task 2 do plano já previa a adição do link de Administradores — o link de Audit Log foi adicionado junto para completar a navegação conforme a intenção original do comentário.

## Known Stubs

Nenhum — todos os dados são buscados do backend via `getAdministrators()`. Nenhum dado hardcoded ou placeholder.

## Threat Flags

Nenhum novo surface de segurança além do mapeado no threat model do plano (T-30-10, T-30-11, T-30-12).

## Self-Check: PASSED

- `frontend/backoffice/src/components/pages/AdminAdministratorsPage.tsx` — FOUND
- `frontend/backoffice/src/lib/admin-api.ts` (AdminUserDto + getAdministrators) — FOUND
- `frontend/backoffice/src/tests/admin-api.test.ts` (10 testes passando) — FOUND
- `frontend/backoffice/src/router.tsx` (/admin/administrators) — FOUND
- `frontend/backoffice/src/components/templates/AdminLayout.tsx` (sidebar-administrators-link) — FOUND
- Commit e15fcb6 (Task 1) — EXISTS
- Commit 9e83a79 (Task 2) — EXISTS
