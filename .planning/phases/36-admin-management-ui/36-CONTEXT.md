# Phase 36: Admin Management UI - Context

**Gathered:** 2026-04-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Evoluir `AdminAdministratorsPage.tsx` com paginação real (endpoint `/paginated`), dois campos
de busca independentes (nome e email), filtro de status, e as 4 ações de gerenciamento por linha
— editar, resetar senha, desativar, reativar — via dropdown contextual com modais de confirmação
e feedback via toast. A rota `/admin/administrators` já existe; nenhuma nova rota é necessária.

</domain>

<decisions>
## Implementation Decisions

### Ações por linha
- **D-01:** Cada linha usa um botão `⋯` (DropdownMenu) na coluna "Ações" — NÃO botões inline.
  O dropdown existente (`src/components/ui/dropdown-menu.tsx`) é utilizado.
- **D-02:** Itens do dropdown são **contextuais por status**: admin ativo mostra "Desativar",
  admin inativo mostra "Reativar". Nunca os dois juntos no mesmo dropdown.
- **D-03:** Itens fixos presentes em todos os dropdowns (exceto linha própria): "Editar" e "Resetar senha".

### One-time Password UX
- **D-04:** Após reset de senha bem-sucedido, abrir dialog dedicada com:
  - Campo de texto visível (não obscurecido) exibindo a senha gerada
  - Botão "Copiar" (copia para clipboard, muda ícone/texto para confirmação)
  - Aviso proeminente: **"⚠️ Esta senha não pode ser recuperada. Feche somente após copiar."**
  - Botão "Fechar" sem countdown ou checkbox — sem bloqueio forçado
- **D-05:** Dialog de reset-senha NÃO pode ser reaberta. Após fechar, senha é descartada do state.

### Self-action — SEC-01
- **D-06:** A linha do admin logado mostra o botão `⋯` **desabilitado** (opacity-50, pointer-events-none).
- **D-07:** Hover sobre o botão `⋯` desabilitado exibe tooltip: "Você não pode modificar a própria conta".
- **D-08:** Identificar a própria linha via comparação do `id` da row com o `sub` do JWT
  (disponível via `useAdminAuth()` context já existente, campo `adminId` ou equivalente).

### Campos de busca e filtros
- **D-09:** Dois inputs distintos na toolbar de filtros:
  - Input "Buscar por nome..." → parâmetro `name` da API
  - Input "Buscar por email..." → parâmetro `email` da API
  - Ambos usam debounce 300ms (padrão já estabelecido no projeto)
- **D-10:** `AdminSearchBar` pode ser reutilizado duas vezes com `placeholder` diferentes.
- **D-11:** Status filter com opções: "Todos" (null), "Ativo" ("active"), "Inativo" ("inactive").
  O componente `AdminStatusFilter` atual inclui "blocked" e "deleted" que NÃO se aplicam a admins.
  Criar variante ou sobrescrever as opções via prop.
- **D-12:** Qualquer mudança em filtros ou busca reseta para página 1 (padrão do `AdminUsersPage`).

### Loading e feedback
- **D-13:** Skeleton loading durante fetch (padrão do `AdminUsersTable` — usar `Skeleton` component).
- **D-14:** Toast via Sonner para confirmação de cada ação bem-sucedida (editar, reset, desativar, reativar).
- **D-15:** Após qualquer ação bem-sucedida, refetch automático da lista para refletir o estado atualizado.

### Claude's Discretion
- Estrutura dos modais de confirmação para editar/desativar/reativar (campos, layout) — seguir padrão de `BlockDialog`/`DeleteDialog` existentes.
- Validação inline do form de edição (nome obrigatório, email válido) — usar padrão já estabelecido com `validation-schemas.ts`.
- Número de colunas da tabela de admins — nome, email, status + coluna ações.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Frontend — páginas e componentes existentes a evoluir
- `frontend/backoffice/src/components/pages/AdminAdministratorsPage.tsx` — página existente a ser reescrita; atualmente sem paginação, filtros ou ações
- `frontend/backoffice/src/router.tsx` — rota `/admin/administrators` já registrada (não alterar rota)
- `frontend/backoffice/src/lib/admin-api.ts` — funções de API a serem estendidas com os 4 novos endpoints

### Frontend — componentes reutilizáveis (reuse diretamente)
- `frontend/backoffice/src/components/molecules/AdminPagination.tsx` — reutilizar sem alteração
- `frontend/backoffice/src/components/molecules/AdminSearchBar.tsx` — reutilizar dois vezes (nome e email) com placeholders distintos
- `frontend/backoffice/src/components/molecules/AdminStatusFilter.tsx` — adaptar opções para admins (remover "blocked"/"deleted", trocar por "inactive")
- `frontend/backoffice/src/components/ui/dropdown-menu.tsx` — usar para botão de ações por linha
- `frontend/backoffice/src/components/ui/dialog.tsx` — usar para todos os modais
- `frontend/backoffice/src/components/ui/skeleton.tsx` — usar para loading state da tabela

### Frontend — padrão de referência para ações (BlockDialog, DeleteDialog)
- `frontend/backoffice/src/components/molecules/BlockDialog.tsx` — referência para dialog de confirmação
- `frontend/backoffice/src/components/molecules/EditUserForm.tsx` — referência para form de edição

### Backend — contratos de API (Phase 35)
- `src/Onboarding.API/Controllers/AdminUserController.cs` — endpoints: `/paginated`, PUT `/{id}`, POST `/{id}/reset-password`, POST `/{id}/toggle-status`
- `src/Onboarding.Application/Admin/Queries/GetPaginatedAdministratorsQuery.cs` — confirma params: `name`, `email`, `status` ("active" | "inactive" | null)

### Context de fases anteriores
- `.planning/phases/35-backoffice-admin-management-pagina-o-filtros-reset-senha-edi/35-CONTEXT.md` — decisões do backend (toggle pattern, password gerada sem chars ambíguos)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AdminPagination`: aceita `page`, `pageSize`, `totalCount`, `onPageChange` — pronto para uso
- `AdminSearchBar`: debounce 300ms embutido, aceita `value`, `onChange`, `placeholder` — reutilizar 2× 
- `DropdownMenu` (shadcn): `DropdownMenuTrigger`, `DropdownMenuContent`, `DropdownMenuItem` disponíveis
- `Dialog` (shadcn): `Dialog`, `DialogContent`, `DialogHeader`, `DialogTitle`, `DialogFooter` disponíveis
- `Skeleton` (shadcn): usado em `AdminUsersTable` para loading rows
- `toast` (sonner): importado via `import { toast } from "sonner"` — padrão no projeto

### Established Patterns
- `AdminUsersPage.tsx`: padrão completo de página com paginação + busca + filtro + debounce 300ms + reset de página — seguir exatamente esse padrão para a nova página de admins
- `BlockDialog.tsx` / `UnblockDialog.tsx`: padrão de dialog de confirmação com título, descrição e botões Cancel/Confirm
- `useAdminAuth()` context em `admin-auth-context.tsx`: fornece identidade do admin logado para comparação SEC-01

### Integration Points
- Rota `/admin/administrators` já registrada em `router.tsx` → componente `AdminAdministratorsPage` (substituir/evoluir)
- `admin-api.ts`: adicionar funções `getAdministratorsPaginated()`, `updateAdministrator()`, `resetAdministratorPassword()`, `toggleAdministratorStatus()`
- Backend: `AdminUserDto` já tem `id`, `email`, `fullName`, `isEnabled`, `hasTemporaryPassword`
- `PaginatedResult<AdminUserDto>` retorna `items`, `totalCount`, `page`, `pageSize`

</code_context>

<specifics>
## Specific Ideas

- Dropdown contextual: item de status ("Desativar" vs "Reativar") determinado por `admin.isEnabled`
- One-time password dialog: senha em `<code>` ou Input read-only com fonte monospace; botão Copiar muda para "Copiado! ✓" por 2s após copiar
- Self-action detection: `currentAdminId === admin.id` → desabilitar DropdownMenuTrigger com `disabled` prop + Tooltip sobre o trigger

</specifics>

<deferred>
## Deferred Ideas

None — discussão ficou dentro do escopo da fase.

</deferred>

---

*Phase: 36-admin-management-ui*
*Context gathered: 2026-04-24*
