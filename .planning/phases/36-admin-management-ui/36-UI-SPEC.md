---
phase: 36
slug: admin-management-ui
status: draft
shadcn_initialized: true
preset: "style=default baseColor=neutral cssVariables=true"
created: 2026-04-24
---

# Phase 36 — UI Design Contract: Admin Management UI

> Visual and interaction contract para evolução do AdminAdministratorsPage.tsx.
> Gerado por gsd-ui-researcher. Verificado por gsd-ui-checker.

---

## Design System

| Propriedade        | Valor                                                          | Fonte                          |
|--------------------|----------------------------------------------------------------|-------------------------------|
| Tool               | shadcn/ui                                                      | components.json detectado     |
| Style              | default                                                        | components.json               |
| Base color         | neutral                                                        | components.json               |
| CSS variables      | true                                                           | components.json               |
| Component library  | Radix UI (via shadcn primitives)                               | componentes existentes        |
| Icon library       | lucide-react                                                   | components.json + uso nos tsx |
| Font               | sistema (sem fonte customizada — herda do browser)             | globals.css — sem @font-face  |
| Toast              | Sonner (`import { toast } from "sonner"`)                      | AdminUsersPage.tsx, BlockDialog.tsx |
| Border radius      | `--radius: 0.625rem` (10px)                                    | globals.css                   |

---

## Spacing Scale

Escala 8-point herdada do Tailwind CSS 4. Valores declarados para uso nesta fase:

| Token | Valor | Uso nesta fase                                    |
|-------|-------|---------------------------------------------------|
| xs    | 4px   | Gap interno de ícones (ex: `gap-1`), badge padding |
| sm    | 8px   | Gap entre toolbar items (`gap-2`), padding interno de botões pequenos |
| md    | 16px  | Padding horizontal de células de tabela (`px-4`), gap de toolbar principal |
| lg    | 24px  | Padding vertical de seções (`p-6`), gap entre card sections |
| xl    | 32px  | Margem top de paginação (`mt-4` = 16px — exceção listada abaixo) |
| 2xl   | 48px  | Não utilizado nesta fase                          |
| 3xl   | 64px  | Não utilizado nesta fase                          |

**Exceções:**

- `py-3` (12px) para células de tabela — padrão do `AdminAdministratorsPage` existente; mantido para consistência visual com `AdminUsersPage`.
- `mt-4` (16px) para separação entre tabela e paginação — padrão do `AdminUsersPage`.
- Touch target do botão `⋯` (DropdownMenuTrigger): mínimo 32px altura via `size="icon"` do shadcn Button (`h-8 w-8`).

---

## Typography

Tokens CSS do shadcn/neutral. Sem fonte customizada — herda Inter ou fonte do sistema.

| Role     | Tamanho    | Peso              | Line Height | Uso nesta fase                                           |
|----------|------------|-------------------|-------------|----------------------------------------------------------|
| Body     | 14px (sm)  | 400 (normal)      | 1.5         | Células de tabela, labels de filtro, descrição de dialogs |
| Label    | 14px (sm)  | 500 (medium)      | 1.4         | Cabeçalhos de coluna (`font-medium text-muted-foreground`), labels de form |
| Heading  | 20px (xl)  | 600 (semibold)    | 1.2         | Título da página (`text-xl font-semibold "Administradores"`) |
| Caption  | 12px (xs)  | 400 (normal)      | 1.5         | Info de paginação (`text-sm text-muted-foreground`) — na prática 14px; o token caption cobre textos auxiliares |

**Fonte monospace:** Aplicada exclusivamente no campo de exibição da senha temporária no one-time password dialog. Usar `font-mono` (Tailwind). Exemplo: `<code className="font-mono text-sm bg-muted px-2 py-1 rounded select-all">`.

---

## Color

Tokens CSS herdados do `globals.css` (shadcn/neutral, oklch).

| Role              | Token CSS                        | Valor aproximado (light) | Uso nesta fase                                                             |
|-------------------|----------------------------------|--------------------------|---------------------------------------------------------------------------|
| Dominant (60%)    | `--background` / `--card`        | oklch(1 0 0) ≈ #ffffff   | Fundo de página, surface do Card, fundo do Dialog                         |
| Secondary (30%)   | `--muted` / `--secondary`        | oklch(0.97 0 0) ≈ #f7f7f7 | Cabeçalho de tabela (`bg-muted/50`), hover de linha (`hover:bg-muted/30`), sidebar |
| Accent (10%)      | `--primary`                      | oklch(0.205 0 0) ≈ #1a1a1a | Reservado para: botão primário "Salvar" no EditDialog, ícone Shield na toolbar, página ativa na paginação |
| Destructive       | `--destructive`                  | oklch(0.577 0.245 27.325) ≈ #dc2626 | Exclusivo para: botão "Desativar" no DropdownMenu, botão de confirmação no DeactivateDialog, Badge `variant="destructive"` para admin inativo |

**Accent reservado para:**
1. Botão primário de submit no EditAdminDialog ("Salvar alterações")
2. Ícone `Shield` na toolbar da página
3. Item de paginação da página atual (`isActive`)

**Semantic extras:**
- `text-amber-600 border-amber-300` — Badge "Pendente" (senha temporária ativa) — padrão já existente no `AdminAdministratorsPage`
- `text-green-600 border-green-300` — Badge "Definida" (senha permanente) — padrão já existente
- Badge `variant="default"` (fundo primary) — admin Ativo
- Badge `variant="destructive"` (fundo destructive) — admin Inativo (substituir "Bloqueado" por "Inativo")

---

## Inventário de Componentes

### Componentes reutilizados SEM alteração

| Componente                   | Localização                                           | Props relevantes para esta fase                         |
|------------------------------|-------------------------------------------------------|--------------------------------------------------------|
| `AdminSearchBar`             | `molecules/AdminSearchBar.tsx`                        | `value`, `onChange`, `placeholder`, `disabled` — usar 2x |
| `AdminPagination`            | `molecules/AdminPagination.tsx`                       | `page`, `pageSize`, `totalCount`, `onPageChange`       |
| `DropdownMenu` + primitivos  | `ui/dropdown-menu.tsx`                                | `DropdownMenuTrigger`, `DropdownMenuContent`, `DropdownMenuItem`, `DropdownMenuSeparator` |
| `Dialog` + primitivos        | `ui/dialog.tsx`                                       | `Dialog`, `DialogContent`, `DialogHeader`, `DialogTitle`, `DialogDescription`, `DialogFooter` |
| `Skeleton`                   | `ui/skeleton.tsx`                                     | Sem props — usar inline `<Skeleton className="h-4 w-full" />` |
| `Button`                     | `ui/button.tsx`                                       | `variant`, `size`, `disabled`                          |
| `Input`                      | `ui/input.tsx`                                        | Usado em EditAdminDialog e one-time password display   |
| `Badge`                      | `ui/badge.tsx`                                        | `variant="default"` (ativo), `variant="destructive"` (inativo), `variant="outline"` (senha temp) |
| `Alert` + `AlertDescription` | `ui/alert.tsx`                                        | Usado no DeactivateDialog (`variant="destructive"`)    |
| `Label`                      | `ui/label.tsx`                                        | Formulário de edição                                   |
| `Tooltip` + primitivos       | `ui/tooltip.tsx`                                      | Tooltip no botão ⋯ desabilitado (D-07)                 |
| `Card` + primitivos          | `ui/card.tsx`                                         | Wrapper da página                                      |

### Componentes adaptados

| Componente              | Adaptação necessária                                                                                    |
|-------------------------|---------------------------------------------------------------------------------------------------------|
| `AdminStatusFilter`     | Substituir `STATUS_OPTIONS` por `["Todos"/"all", "Ativo"/"active", "Inativo"/"inactive"]` via prop `options` ou criar variante `AdminAdminStatusFilter` |

### Novos componentes a criar (nesta fase)

| Componente                   | Descrição                                                                                   |
|------------------------------|---------------------------------------------------------------------------------------------|
| `AdminAdministratorsTable`   | Tabela com colunas Nome / Email / Status / Senha Temp / Ações — esqueleto de loading incluso |
| `AdminActionsDropdown`       | Botão `⋯` + DropdownMenu contextual por status; desabilitado na própria linha com Tooltip  |
| `EditAdminDialog`            | Dialog com form React Hook Form + Zod: campos Nome (obrigatório) e Email (obrigatório, formato válido) |
| `ResetPasswordDialog`        | Dialog one-time: exibe senha em campo monospace, botão Copiar com feedback 2s, aviso de não-recuperação, botão Fechar |
| `DeactivateAdminDialog`      | Dialog de confirmação com Alert destructive; confirma desativação do admin selecionado       |
| `ReactivateAdminDialog`      | Dialog de confirmação simples; confirma reativação do admin selecionado                     |

---

## Especificação de Interações

### Tabela de Administradores

**Colunas (da esquerda para a direita):**

| # | Cabeçalho       | Conteúdo da célula                                               | Largura      |
|---|-----------------|------------------------------------------------------------------|--------------|
| 1 | Nome            | `admin.fullName` — `font-medium`                                 | auto         |
| 2 | Email           | `admin.email` — `text-muted-foreground`                          | auto         |
| 3 | Status          | Badge: Ativo (`variant="default"`) / Inativo (`variant="destructive"`) | 100px     |
| 4 | Senha Temp      | Badge: Pendente (`text-amber-600 border-amber-300`) / Definida (`text-green-600 border-green-300`) | 120px |
| 5 | Ações           | `AdminActionsDropdown` — botão `⋯` `size="icon" variant="ghost"` | 64px         |

**Linha hover:** `hover:bg-muted/30 transition-colors` — padrão existente.

**Skeleton loading (D-13):**
- Renderizar 5 linhas de skeleton enquanto `isLoading && !result`
- Cada linha: `<Skeleton className="h-4 w-full" />` por célula
- Células de ação: `<Skeleton className="h-8 w-8 rounded" />`

### Botão ⋯ (AdminActionsDropdown)

**Estado normal (outra linha):**
```
variant="ghost" size="icon"   →   <MoreHorizontal className="h-4 w-4" />
```

**Estado desabilitado (própria linha — D-06):**
```
disabled={true}
className="opacity-50 cursor-not-allowed"
```
Envolver com `<Tooltip>` do shadcn:
- `<TooltipTrigger asChild>` envolve o Button desabilitado
- `<TooltipContent>`: "Você não pode modificar a própria conta"

**Detecção de própria linha (D-08):**
```typescript
const { adminId } = useAdminAuth()  // sub do JWT
const isSelf = admin.id === adminId
```

### Itens do DropdownMenu (D-01, D-02, D-03)

| Item           | Condição de exibição          | Ação ao clicar                     |
|----------------|-------------------------------|-------------------------------------|
| "Editar"       | Sempre (exceto própria linha) | Abre `EditAdminDialog`              |
| "Resetar senha"| Sempre (exceto própria linha) | Chama API → abre `ResetPasswordDialog` com senha retornada |
| `DropdownMenuSeparator` | Sempre             | Separador visual                   |
| "Desativar"    | Somente se `isEnabled === true` | Abre `DeactivateAdminDialog`      |
| "Reativar"     | Somente se `isEnabled === false` | Abre `ReactivateAdminDialog`     |

### EditAdminDialog

**Trigger:** Item "Editar" do DropdownMenu.

**Conteúdo:**
- `DialogTitle`: "Editar Administrador"
- `DialogDescription`: "Atualize o nome e o email do administrador."
- Form fields:
  - Nome: `<Input>` — obrigatório, mínimo 2 chars, máximo 100 chars
  - Email: `<Input type="email">` — obrigatório, formato RFC válido
- Validação inline: erro abaixo do campo em `text-sm text-destructive`
- `DialogFooter`:
  - "Cancelar" — `variant="outline"` — fecha dialog sem salvar
  - "Salvar alterações" — `variant="default"` (accent/primary) — submits form

**Estados do botão submit:**
- Normal: "Salvar alterações"
- Submitting: `<Loader2 className="h-4 w-4 mr-1 animate-spin" /> Salvando...` — disabled

**Feedback:** `toast.success("Administrador atualizado com sucesso.")` após fechamento do dialog.

**Erro 409 (email já em uso):** `toast.error("Email já está em uso.", { description: "Escolha outro email e tente novamente." })`

### ResetPasswordDialog (D-04, D-05)

**Trigger:** Item "Resetar senha" do DropdownMenu.

**Fluxo:** Chamada à API ocorre AO CLICAR no item, antes de abrir o dialog. Dialog só abre com a senha já retornada.

**Conteúdo:**
- `DialogTitle`: "Senha Temporária Gerada"
- `DialogDescription`: "Compartilhe esta senha com o administrador agora. Ela não poderá ser recuperada depois."
- Campo de exibição da senha:
  ```
  <Input
    readOnly
    value={generatedPassword}
    className="font-mono text-sm select-all"
  />
  ```
- Alert com ícone AlertTriangle:
  ```
  variant="destructive" (ou variant="default" com borda amber)
  Texto: "Esta senha não pode ser recuperada. Feche somente após copiar."
  ```
- Botão "Copiar":
  - Estado normal: `<Copy className="h-4 w-4 mr-2" /> Copiar senha`
  - Estado após cópia (2s): `<Check className="h-4 w-4 mr-2" /> Copiado!`
  - `variant="outline"`
- `DialogFooter`:
  - "Fechar" — `variant="default"` — fecha dialog

**Comportamento de fechamento (D-05):**
- Ao fechar (onOpenChange → false): limpar senha do state → `setGeneratedPassword(null)`
- Dialog não pode ser reaberta; a senha é descartada permanentemente do state

**Feedback de contexto:** Nenhum toast necessário (a própria dialog é o feedback). Opcional: `toast.success("Senha temporária gerada.")` no momento da chamada API para indicar sucesso da operação de reset antes do dialog abrir.

### DeactivateAdminDialog

**Trigger:** Item "Desativar" do DropdownMenu (apenas para admins com `isEnabled === true`).

**Conteúdo:**
- `DialogTitle`: "Desativar Administrador"
- `DialogDescription`: "O administrador {admin.fullName} perderá acesso ao backoffice. A conta é preservada para auditoria."
- Alert `variant="destructive"`: "Esta ação pode ser revertida reativando o administrador."
- `DialogFooter`:
  - "Cancelar" — `variant="outline"`
  - "Desativar" — `variant="destructive"`

**Estados do botão "Desativar":**
- Normal: "Desativar"
- Submitting: `<Loader2 className="h-4 w-4 mr-1 animate-spin" /> Desativando...` — disabled

**Feedback:** `toast.success("Administrador desativado.")` após sucesso.

**Erro — último admin ativo (SEC-05 — HTTP 409):** `toast.error("Não é possível desativar.", { description: "Deve existir ao menos um administrador ativo." })`

### ReactivateAdminDialog

**Trigger:** Item "Reativar" do DropdownMenu (apenas para admins com `isEnabled === false`).

**Conteúdo:**
- `DialogTitle`: "Reativar Administrador"
- `DialogDescription`: "O administrador {admin.fullName} recuperará acesso ao backoffice."
- `DialogFooter`:
  - "Cancelar" — `variant="outline"`
  - "Reativar" — `variant="default"` (sem destructive — ação positiva)

**Feedback:** `toast.success("Administrador reativado.")` após sucesso.

### Toolbar de Filtros

Layout: `flex flex-col sm:flex-row items-start sm:items-center gap-4`

Ordem dos elementos (esquerda → direita em telas md+):

1. `AdminSearchBar` — placeholder "Buscar por nome..." → state `nameSearch`
2. `AdminSearchBar` — placeholder "Buscar por email..." → state `emailSearch`
3. `AdminStatusFilter` (variante admin) — opções: Todos / Ativo / Inativo → state `status`

Ambos os `AdminSearchBar` usam debounce 300ms embutido no componente (D-09).
Qualquer mudança em qualquer filtro reseta `page` para 1 (D-12).

### Paginação

- `pageSize`: 20 por página (padrão MGMT-01)
- Componente: `AdminPagination` sem alteração
- Posicionamento: `<div className="mt-4">` abaixo do `<CardContent>`
- Exibido somente quando `result.totalCount > 0`

---

## Copywriting Contract

| Elemento                          | Texto                                                                                              |
|-----------------------------------|----------------------------------------------------------------------------------------------------|
| Título da página                  | "Administradores"                                                                                  |
| Placeholder busca por nome        | "Buscar por nome..."                                                                               |
| Placeholder busca por email       | "Buscar por email..."                                                                              |
| Status filter — opção todos       | "Todos"                                                                                            |
| Status filter — opção ativo       | "Ativo"                                                                                            |
| Status filter — opção inativo     | "Inativo"                                                                                          |
| Badge status ativo                | "Ativo"                                                                                            |
| Badge status inativo              | "Inativo"                                                                                          |
| Badge senha temporária pendente   | "Pendente"                                                                                         |
| Badge senha permanente definida   | "Definida"                                                                                         |
| Dropdown — item editar            | "Editar"                                                                                           |
| Dropdown — item resetar senha     | "Resetar senha"                                                                                    |
| Dropdown — item desativar         | "Desativar"                                                                                        |
| Dropdown — item reativar          | "Reativar"                                                                                         |
| Tooltip botão ⋯ desabilitado      | "Você não pode modificar a própria conta"                                                          |
| Empty state heading               | "Nenhum administrador encontrado."                                                                 |
| Empty state body                  | "Ajuste os filtros ou crie um novo administrador."                                                 |
| Error state                       | "Falha ao carregar administradores. Tente novamente." + botão inline "Tentar novamente"            |
| CTA primário EditAdminDialog      | "Salvar alterações"                                                                                |
| CTA cancelar (todos os dialogs)   | "Cancelar"                                                                                         |
| CTA fechar (ResetPasswordDialog)  | "Fechar"                                                                                           |
| Aviso one-time password           | "Esta senha não pode ser recuperada. Feche somente após copiar."                                   |
| Botão copiar senha — normal       | "Copiar senha"                                                                                     |
| Botão copiar senha — após cópia   | "Copiado!"                                                                                         |
| CTA confirmação desativar         | "Desativar"                                                                                        |
| CTA confirmação reativar          | "Reativar"                                                                                         |
| Toast — edição bem-sucedida       | "Administrador atualizado com sucesso."                                                            |
| Toast — reset senha               | "Senha temporária gerada." (opcional — a dialog já fornece feedback)                               |
| Toast — desativação               | "Administrador desativado."                                                                        |
| Toast — reativação                | "Administrador reativado."                                                                         |
| Toast error — falha ao editar     | title: "Falha ao atualizar administrador" / description: "Tente novamente."                        |
| Toast error — email duplicado     | title: "Email já está em uso." / description: "Escolha outro email e tente novamente."             |
| Toast error — falha ao desativar  | title: "Falha ao desativar administrador" / description: "Tente novamente."                        |
| Toast error — último admin ativo  | title: "Não é possível desativar." / description: "Deve existir ao menos um administrador ativo." |
| Toast error — falha ao reativar   | title: "Falha ao reativar administrador" / description: "Tente novamente."                         |
| Toast error — falha ao resetar    | title: "Falha ao resetar senha" / description: "Tente novamente."                                  |
| Botão refresh                     | "Atualizar"                                                                                        |
| Botão criar admin                 | "Criar Admin"                                                                                      |
| Loading skeleton (acessível)      | aria-label="Carregando administradores..." no container da tabela                                  |

---

## Validação de Formulário (EditAdminDialog)

Schema Zod — criar em `src/lib/validation-schemas.ts` como `adminEditAdministratorSchema`:

| Campo | Tipo   | Regras                                              | Mensagem de erro                        |
|-------|--------|-----------------------------------------------------|-----------------------------------------|
| nome  | string | obrigatório, min 2 chars, max 100 chars, trim        | "Nome é obrigatório" / "Nome muito curto" |
| email | string | obrigatório, formato email válido (z.string().email) | "Email inválido" / "Email é obrigatório" |

---

## Estados da Página

| Estado                        | O que renderizar                                                                          |
|-------------------------------|-------------------------------------------------------------------------------------------|
| `isLoading && !result`        | Tabela com 5 linhas de Skeleton                                                           |
| `isLoading && result` (refetch) | Tabela com dados anteriores + opacidade reduzida (opacity-60) — evitar flash de skeleton |
| `!isLoading && isError`       | Mensagem de erro com botão "Tentar novamente"                                             |
| `!isLoading && totalCount === 0` | Empty state: "Nenhum administrador encontrado." + dica de ajuste de filtros           |
| `!isLoading && totalCount > 0` | Tabela + paginação                                                                      |

---

## Registry Safety

| Registry         | Blocos usados nesta fase                                                    | Safety Gate                   |
|------------------|-----------------------------------------------------------------------------|-------------------------------|
| shadcn oficial   | dropdown-menu, dialog, skeleton, button, input, badge, alert, label, tooltip, card, pagination, select | not required — oficial |
| Terceiros        | nenhum                                                                      | N/A                           |

Nenhum registry terceiro declarado. Gate de segurança não aplicável.

---

## Acessibilidade

| Elemento                          | Requisito                                                               |
|-----------------------------------|-------------------------------------------------------------------------|
| Botão ⋯ desabilitado              | `aria-disabled="true"` + Tooltip com role="tooltip"                     |
| Dropdown items                    | `role="menuitem"` (herdado do shadcn DropdownMenuItem)                  |
| Dialog                            | `role="dialog"` + `aria-labelledby` + `aria-describedby` (herdado shadcn) |
| Campo de senha (one-time)         | `aria-label="Senha temporária gerada"` + `readOnly`                     |
| Botão copiar                      | `aria-label="Copiar senha para clipboard"` → muda para `aria-label="Senha copiada"` após cópia |
| Tabela                            | `<table>` semântico com `<thead>/<tbody>/<th scope="col">/<td>`         |
| Skeleton loading                  | Container com `aria-busy="true"` durante loading                        |
| AdminSearchBar                    | `aria-label` igual ao placeholder (já implementado no componente)       |
| Status filter (Select)            | `aria-label="Filtrar por status"`                                        |

---

## Checker Sign-Off

- [ ] Dimensão 1 Copywriting: PASS
- [ ] Dimensão 2 Visuais: PASS
- [ ] Dimensão 3 Color: PASS
- [ ] Dimensão 4 Typography: PASS
- [ ] Dimensão 5 Spacing: PASS
- [ ] Dimensão 6 Registry Safety: PASS

**Aprovação:** pendente

---

*Phase: 36-admin-management-ui*
*UI-SPEC gerado: 2026-04-24*
*Fontes: CONTEXT.md (15 decisões), globals.css (tokens), AdminUsersPage.tsx (padrão), AdminAdministratorsPage.tsx (componente atual)*
