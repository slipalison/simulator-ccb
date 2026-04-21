---
plan: 30-04
phase: 30-audit-log-admin-backend
status: complete
gap_closure: true
completed: 2026-04-16
---

# Summary: Plan 30-04 — Gap Closure: Corrigir URL de createAdmin() no Frontend

## What Was Built

Corrigida a função `createAdmin()` em `frontend/backoffice/src/lib/admin-api.ts` que chamava a rota removida `/api/admin/users` (POST) em vez da rota atual `/api/admin/administrators`.

## Changes Made

### `frontend/backoffice/src/lib/admin-api.ts`
- Linha 314: comentário atualizado de `// POST /api/admin/users` → `// POST /api/admin/administrators`
- Linha 324: URL do `fetch()` corrigida de `"/api/admin/users"` → `"/api/admin/administrators"`

## Self-Check: PASSED

| Criterion | Result |
|-----------|--------|
| `createAdmin()` usa `/api/admin/administrators` | ✓ PASS (linha 324) |
| Comentário reflete rota atual | ✓ PASS (linha 314) |
| Demais funções inalteradas | ✓ PASS (apenas 2 linhas modificadas) |
| Commit criado | ✓ PASS (5e58d04) |

## key-files

### key-files.modified
- `frontend/backoffice/src/lib/admin-api.ts` — createAdmin() agora aponta para /api/admin/administrators

## Deviations

Nenhum. O plano especificava exatamente 2 linhas a alterar e ambas foram corrigidas.

> Nota: `grep '/api/admin/users'` ainda retorna resultados no arquivo — mas são referências corretas de outros endpoints (GET lista de usuários, GET detalhe, PUT, DELETE, block, unblock) que **não** foram renomeados. Somente o endpoint POST de criação de admins foi movido para `/api/admin/administrators`.
