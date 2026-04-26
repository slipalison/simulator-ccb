# Phase 40: Client Frontend — PJ Registration & Employee Management - Context

**Gathered:** 2026-04-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Frontend do cliente redesenhado para cadastro PJ-only com gestão de funcionários (visualizar,
bloquear, resetar senha, editar, excluir LGPD), atribuição de grupos de acesso, dashboard mock
com dados estáticos. Remoção completa do fluxo PF. Viewer vê dados read-only. admin-empresa vê
mesmas telas de gestão que PJ dono. Sidebar com navegação baseada em permissões.

**Requisitos:** DASH-01, REG-01 frontend, REG-05 frontend, MGMT-01..05 frontend, PERM-04 frontend

</domain>

<decisions>
## Implementation Decisions

### Navegação e Layout
- **D-01:** Sidebar fixa à esquerda com links: Dashboard, Funcionários, Perfil Empresa. Layout padrão para painéis de gestão.
- **D-02:** Rotas pós-login: `/` (dashboard), `/employees` (gestão de funcionários), `/profile` (perfil da empresa/PJ).
- **D-03:** Rotas ocultadas baseado no group do usuário — Viewer vê Employees (read-only) + Profile. Dashboard vê Dashboard + Profile. Admin-empresa vê tudo. PJ dono = admin-empresa.
- **D-04:** Header mantido (logo + menu perfil/sair) com badge de ruolo/group (ex: "Admin Empresa", "Viewer").

### Formulário de Cadastro PJ
- **D-05:** Formulário em wizard 2 passos: Passo 1 (dados empresa: CNPJ, Razão Social), Passo 2 (dados acesso: email, telefone, senha, confirmação, termos).
- **D-06:** PersonTypeRadio removido completamente — cadastro é exclusivamente PJ. Schema Zod dinâmico PF/PJ removido, substituído por schema PJ-only.
- **D-07:** TermsAcceptance: checkbox obrigatório "Aceito os Termos de Uso" + link que abre modal com texto mock da versão 1.0. Checkbox deve estar marcado para submeter.
- **D-08:** Password UX mantido: strength meter 5 níveis + show/hide toggle + confirm password. Padrão existente reutilizado.

### Gestão de Funcionários UI
- **D-09:** Tabela com 5 colunas: Nome, Email, Group Badge (viewer/admin-empresa/dashboard), Status Badge (active/blocked), Actions dropdown.
- **D-10:** Actions dropdown por funcionário: Edit, Block/Unblock, Reset Password, Delete (LGPD), Change Access Group. Viewer vê sem botões de ação.
- **D-11:** Exclusão LGPD: dialog pedindo digitar email do funcionário para confirmar — consistente com padrão backoffice existente.
- **D-12:** Reset de senha: modal one-time reveal — mostra senha temporária UMA VEZ, não reabre. Padrão já existente no backoffice.
- **D-13:** Editar funcionário: campos nome, email, telefone (mesmo padrão que backoffice EditAdminDialog).
- **D-14:** Change Access Group: dropdown com 3 grupos (admin-empresa, viewer, dashboard). Mudança reflete no Keycloak em tempo real.

### Dashboard Simulação
- **D-15:** Dashboard com 6 cards: Total Funcionários, Ativos, Bloqueados, Logins Recentes (7d), Ações Recentes (7d), Último Login.
- **D-16:** Mini charts com Chart.js em cada card — dados mock estáticos.
- **D-17:** Período: últimos 7 dias para logins e ações recentes.
- **D-18:** Dados mock hardcoded no frontend — sem chamadas a API real. Card "Total Funcionários" pode chamar API real (endpoint GET employees existe).

### Permissões e Rotas
- **D-19:** Auth-context estendido para incluir group do usuário (via /auth/me response). Rotas renderizadas condicionalmente baseado no group.
- **D-20:** /employees para Viewer: tabela sem botões de ação, sem opções de editar/bloquear/excluir. Apenas leitura.
- **D-21:** /employees para admin-empresa: tabela completa com todas as ações. Equivalente ao PJ dono.

### Agent's Discretion
- Estrutura exata do sidebar component (organism ou template)
- Design dos mini charts (sparkline, bar, donut) por card
- Campos exatos no modal de edição de funcionário
- Textos exatos do Terms of Use mock
- Animações e transições entre wizard steps
- Design do badge de group no Header
- Estrutura do mock data para dashboard cards

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Backend API — Endpoints que o client consome
- `src/Onboarding.API/Controllers/CompaniesController.cs` — Endpoints de registro PJ e CRUD de funcionários (company-scoped)
- `src/Onboarding.Application/Companies/` — Handlers para RegisterCompany, RegisterEmployee, GetEmployees, etc.
- `src/Onboarding.Application/Common/ICurrentCompanyService.cs` — CompanyId do usuário logado

### Frontend — Componentes existentes a migrar/remover
- `frontend/client/src/components/molecules/RegistrationForm.tsx` — Formulário atual PF/PJ → tornar PJ-only wizard
- `frontend/client/src/components/molecules/PersonTypeRadio.tsx` — DELETAR (seletor PF/PJ)
- `frontend/client/src/lib/validation-schemas.ts` — Schemas Zod PF/PJ → substituir por PJ-only
- `frontend/client/src/lib/api.ts` — API client → adicionar endpoints de funcionários
- `frontend/client/src/lib/auth-context.tsx` — AuthProvider → estender com group info
- `frontend/client/src/router.tsx` — Rotas → adicionar /employees, remover PF routes
- `frontend/client/src/components/pages/ProfilePage.tsx` — Perfil → adaptar para Company profile
- `frontend/client/src/components/organisms/Header.tsx` — Header → adicionar badge de ruolo

### Frontend — shadcn/ui reutilizáveis
- `frontend/client/src/components/ui/table.tsx` — Para tabela de funcionários
- `frontend/client/src/components/ui/pagination.tsx` — Para paginação da tabela
- `frontend/client/src/components/ui/badge.tsx` — Para group badges e status badges
- `frontend/client/src/components/ui/dropdown-menu.tsx` — Para actions dropdown
- `frontend/client/src/components/ui/dialog.tsx` — Para confirm dialogs (LGPD delete, reset password)
- `frontend/client/src/components/ui/card.tsx` — Para dashboard cards
- `frontend/client/src/components/ui/select.tsx` — Para change access group dropdown

### Context de fases anteriores
- `.planning/phases/39-keycloak-groups-permissions/39-CONTEXT.md` — Groups, permissões, JWT claims, middleware
- `.planning/phases/37-domain-model-redesign/37-CONTEXT.md` — Company/Employee aggregates, AccessGroup, TermsAcceptance
- `.planning/phases/34-isolar-backoffice-e-client-em-realms-separados/34-CONTEXT.md` — Dois realms separados, ACF+PKCE

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `RegistrationForm.tsx`: base para wizard PJ — campos CNPJ/razão social, validação, password UX, api call. Transformar em wizard 2 passos.
- `PasswordStrengthMeter.tsx`: reutilizável — 5 níveis, show/hide toggle
- `PasswordField.tsx`: reutilizável — password input com show/hide
- `auth-context.tsx`: AuthProvider com session restoration via /auth/me — estender com group info
- shadcn/ui: Table, Pagination, Badge, DropdownMenu, Dialog, Card, Select — todos prontos
- `Header.tsx`: componente de header com dropdown menu — adicionar badge de ruolo
- Backoffice patterns: `AdminAdministratorsPage`, `DeleteDialog`, `ResetPasswordDialog`, `EditAdminDialog` — referência de padrões de UI para gestão de funcionários no client

### Established Patterns
- Vinxi ACF: login via redirect para Keycloak, session via httpOnly cookies, /auth/me para restore
- Atomic Design: atoms → molecules → organisms → templates → pages
- shadcn/ui para todos os componentes visuais
- Zod + React Hook Form para validação
- TanStack Router para roteamento type-safe

### Integration Points
- `CompaniesController`: GET/POST endpoints para employees — `/api/companies/{companyId}/employees`
- `/auth/me`: precisa retornar group do usuário (admin-empresa, viewer, dashboard) para rotas condicionais
- `ChangeEmployeeAccessGroupCommandHandler`: PUT `/api/companies/{companyId}/employees/{id}/access-group` — dropdown de grupos
- Keycloak: JWT groups claim → mapeado pelo backend → retornado via /auth/me

</code_context>

<specifics>
## Specific Ideas

- Wizard 2 passos: Passo 1 pergunta "dados da empresa" (razão social, CNPJ), Passo 2 pergunta "dados de acesso" (email, telefone, senha, termos) — experiência mais clean que formulário longo
- Sidebar com ícones lucide: LayoutDashboard para Dashboard, Users para Funcionários, Building2 para Perfil Empresa
- Badge de group no Header: "Admin Empresa" em verde, "Viewer" em cinza, "Dashboard" em azul — cores suaves no badge
- Tabela de funcionários: mesma estrutura visual que AdminAdministratorsTable do backoffice, mas adaptada para campos de funcionário (CPF, group, status)
- Dashboard mock: dados fixos como "Total Funcionários: 24", "Ativos: 22", "Bloqueados: 2", "Logins 7d: 45", "Ações 7d: 128", "Último Login: há 2h"
- Mini charts: barra de progresso simples para ativos vs bloqueados, sparkline para logins nos últimos 7 dias

</specifics>

<deferred>
## Deferred Ideas

- BackOffice employee views e audit — Fase 41
- Dashboard com dados reais e dinâmicos — milestone futuro
- Notificação por email ao funcionário quando senha é resetada — requer SMTP
- Funcionário pode editar seus próprios dados — v7.0 é read-only para funcionário
- Exportação de relatórios CSV/PDF de audit log — deferido

</deferred>

---

*Phase: 40-client-frontend-pj-registration*
*Context gathered: 2026-04-26*