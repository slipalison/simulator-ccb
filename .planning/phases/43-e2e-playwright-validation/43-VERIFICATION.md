---
phase: 43-e2e-playwright-validation
verified: 2026-04-27T12:30:00Z
status: human_needed
score: 11/11 must-haves verified
overrides_applied: 0
gaps: []
human_verification:
  - test: "Rodar npx playwright test no frontend/client com Docker Compose ativo e env vars configuradas (E2E_PJ_EMAIL, E2E_PJ_PASSWORD, E2E_VIEWER_EMAIL, E2E_VIEWER_PASSWORD)"
    expected: "13 E2E tests passam: 2 setup, 1 registration, 1 dashboard, 1 employee-management, 3 employee-login, 4 permission-ui (2 projects x 2 tests), 1 access-group-change"
    why_human: "Testes E2E requerem stack completo (Keycloak, API, PostgreSQL, Vinxi) rodando via Docker Compose e credenciais configuradas — impossível verificar programaticamente sem infraestrutura ativa"
  - test: "Cadastro PJ completo: preencher 2-step wizard → submit → ACF redirect → Keycloak login → redireciona para /employees com sidebar visível"
    expected: "Usuário autenticado na rota padrão admin-empresa, JWT decode mostra groups contendo 'admin-empresa'"
    why_human: "Fluxo ACF completo com redirect para Keycloak e callback — só executável com Keycloak rodando e interação browser real"
  - test: "Dashboard exibe 6 cards mock com títulos corretos (Total Funcionários, Ativos, Bloqueados, Logins Recentes, Ações Recentes, Último Login)"
    expected: "Todos os 6 cards visíveis na página /dashboard após login admin-empresa"
    why_human: "Verificação visual de cards renderizados — requer app rodando"
  - test: "Viewer não vê coluna Ações, não vê Dashboard no sidebar, JWT contém group 'viewer'"
    expected: "UI read-only completa, JWT decode confirma group viewer"
    why_human: "Validação de permissões UI requer renderização real do frontend com estado de autenticação"
  - test: "Mudança de access group: viewer → admin-empresa → re-login → permissões atualizadas (Ações visível, Dashboard no sidebar, JWT atualizado)"
    expected: "Após group change + re-login, UI e JWT refletem novo group admin-empresa"
    why_human: "Fluxo completo de group change com re-login e verificação de consistência Keycloak"
---

# Phase 43: E2E Playwright Validation — Verification Report

**Phase Goal:** Playwright instalado e configurado no frontend/client, E2E tests for all critical PJ flows — registration, login, dashboard, employee management, permission UI, access group change, JWT verification
**Verified:** 2026-04-27T12:30:00Z
**Status:** human_needed
**Re-verification:** Não — verificação inicial

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Playwright está instalado e configurado no frontend/client | ✓ VERIFIED | `playwright.config.ts` existe com `defineConfig`, 6 projects (setup, admin-empresa, viewer, registration, dashboard, employee-login), baseURL `http://localhost:5173`, workers: 1, timeout: 60000. `@playwright/test ^1.59.1` e `playwright ^1.59.1` em package.json devDependencies. `test:e2e` e `test:e2e:ui` scripts presentes. |
| 2 | Auth setup projects salvam storageState para admin-empresa e viewer | ✓ VERIFIED | `admin-empresa.setup.ts` faz ACF login → verifica autenticação → cria viewer employee via API → salva `playwright/.auth/admin-empresa.json`. `viewer.setup.ts` faz ACF login → verifica autenticação → salva `playwright/.auth/viewer.json`. Ambos usam `page.context().storageState({ path: authFile })`. |
| 3 | Cadastro PJ completo navega pela UI e pelo Keycloak ACF redirect | ✓ VERIFIED | `registration.spec.ts` (56 linhas): navega `/register` → preenche step 1 (razaoSocial + CNPJ) → clica Continuar → preenche step 2 (email, phone, password, confirmPassword, terms) → submit → espera Keycloak login (#username, #password, #kc-login) → ACF redirect → verifica URL `/employees|dashboard|profile` + sidebar `nav` visível. |
| 4 | Após cadastro, usuário é redirecionado para a rota padrão do seu group | ✓ VERIFIED | `registration.spec.ts` linha 42: `await expect(page).toHaveURL(/localhost:5173\/(employees|dashboard|profile)/)`. JWT decode no cookie verifica `admin-empresa` group. |
| 5 | Dashboard exibe 6 cards mock com os títulos corretos | ✓ VERIFIED | `dashboard.spec.ts` (33 linhas): verifica h1 "Dashboard", welcome message, e 6 cards: Total Funcionários, Ativos, Bloqueados, Logins Recentes, Ações Recentes, Último Login. Títulos corrigidos para combinar com DashboardCards.tsx real (Resumo 02-01 documentou desvio). |
| 6 | PJ cria funcionário via API e funcionário aparece na lista com status ativo e group viewer | ✓ VERIFIED | `employee-management.spec.ts` (73 linhas): navega `/employees` → GET `/auth/me` para companyId → POST API criação → refresh → verifica `employee-row-{id}`, badge-group "Viewer", badge-status "Ativo", Ações column visível, actions dropdown funcional. |
| 7 | Funcionário viewer faz login e é redirecionado para /employees (read-only) | ✓ VERIFIED | `employee-login.spec.ts` (92 linhas, 3 testes): viewer → ACF login → URL `/employees` → `hasActionsColumn() === false` → Dashboard sidebar não visível. |
| 8 | Funcionário admin-empresa faz login e é redirecionado para /employees (com ações) | ✓ VERIFIED | `employee-login.spec.ts`: admin-empresa → ACF login → URL `/employees` → `hasActionsColumn() === true` → Dashboard sidebar visível. |
| 9 | JWT decode revela groups/roles corretos para cada access group | ✓ VERIFIED | `permission-ui.spec.ts` (88 linhas, 2 testes em 2 projetos = 4 instâncias): viewer JWT contém group "viewer" + UI read-only (no Ações, no Dashboard sidebar, 0 action dropdowns). admin-empresa JWT contém group "admin-empresa" + UI completa (Ações column, Dashboard sidebar, action buttons visíveis). Usa `getAccessTokenFromCookies()` + `decodeAccessToken()` via jose. |
| 10 | Após mudança de group, novo login reflete permissões atualizadas | ✓ VERIFIED | `access-group-change.spec.ts` (158 linhas): cria employee → login como viewer → verifica read-only → logout → login admin-empresa → muda group via ChangeAccessGroupDialog (data-testids: change-access-group-dialog, new-access-group-select, change-group-confirm-button) → espera 3s consistência → logout → re-login como employee → verifica Ações column visível + JWT admin-empresa + Dashboard sidebar visível. Trata UPDATE_PASSWORD do Keycloak (#password-new, #password-confirm, #kc-login). |
| 11 | Todos os E2E tests passam com npx playwright test | ✓ VERIFIED (list) | `npx playwright test --list` lista 13 tests em 8 files: 2 setup + 1 registration + 1 dashboard + 1 employee-management + 3 employee-login + 4 permission-ui (2 projects x 2) + 1 access-group-change. **Execução real requer Docker Compose ativo** → human_needed. |

**Score:** 11/11 truths verified (código substantivo presente e wired; execução real requer infraestrutura)

### Deferred Items

Nenhum item deferido — todos os E2E requirements são escopo desta fase.

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `frontend/client/playwright.config.ts` | Playwright config com 6 projetos, baseURL, single worker | ✓ VERIFIED | 81 linhas. defineConfig com 6 projetos (setup, admin-empresa, viewer, registration, dashboard, employee-login). baseURL http://localhost:5173, workers: 1, timeout: 60000, trace: on-first-retry, screenshot: only-on-failure. |
| `frontend/client/e2e/auth/admin-empresa.setup.ts` | Auth setup salva storageState admin-empresa | ✓ VERIFIED | 60 linhas. ACF login + cria viewer employee via API + storageState save. ESM-compatible com import.meta.url. |
| `frontend/client/e2e/auth/viewer.setup.ts` | Auth setup salva storageState viewer | ✓ VERIFIED | 36 linhas. ACF login + storageState save. ESM-compatible. |
| `frontend/client/e2e/registration.spec.ts` | E2E-01: Cadastro PJ completo | ✓ VERIFIED | 56 linhas. Teste completo: 2-step wizard → submit → ACF redirect → Keycloak login → authenticated state + JWT decode. |
| `frontend/client/e2e/dashboard.spec.ts` | E2E-02: Dashboard cards | ✓ VERIFIED | 33 linhas. 6 card title assertions. |
| `frontend/client/e2e/employee-management.spec.ts` | E2E-03: Criação funcionário | ✓ VERIFIED | 73 linhas. API creation → UI verification → status/group badges → actions column. |
| `frontend/client/e2e/employee-login.spec.ts` | E2E-04: Login redirect por group | ✓ VERIFIED | 92 linhas. 3 testes: viewer, admin-empresa, dashboard (condicional). |
| `frontend/client/e2e/permission-ui.spec.ts` | E2E-05: JWT + permissões UI | ✓ VERIFIED | 88 linhas. 2 testes (viewer + admin-empresa), corre em 2 projetos = 4 instâncias. |
| `frontend/client/e2e/access-group-change.spec.ts` | E2E-06: Mudança group + re-login | ✓ VERIFIED | 158 linhas. Fluxo completo: create → viewer login → change group → re-login → verify admin-empresa. Handles UPDATE_PASSWORD. |
| `frontend/client/e2e/pages/keycloak-login.page.ts` | Keycloak login page object | ✓ VERIFIED | 30 linhas. Locators: #username, #password, #kc-login. Method: login(). |
| `frontend/client/e2e/pages/registration.page.ts` | Registration page object 2-step wizard | ✓ VERIFIED | 66 linhas. Step1: razaoSocial, cnpj, continueButton. Step2: email, phone, password, confirmPassword, terms, submit. Methods: goto, fillCompanyData, fillAccessData, submit. |
| `frontend/client/e2e/pages/dashboard.page.ts` | Dashboard page object com 6 cards | ✓ VERIFIED | 45 linhas. 6 card locators com textos corretos (corrigidos do plan). Method: goto, hasCard. |
| `frontend/client/e2e/pages/employees.page.ts` | Employees page object com data-testids | ✓ VERIFIED | 80 linhas. Locators: employees-page, employees-table-wrapper, employee-row-*, refresh-button. Methods: goto, getRowCount, hasActionsColumn, getEmployeeRow, getGroupBadge, getStatusBadge, openChangeAccessGroupDialog, selectNewGroup, confirmChangeGroup. |
| `frontend/client/e2e/pages/profile.page.ts` | Profile page object | ⚠️ ORPHANED | 22 linhas. Existe, substantivo, mas nenhum teste importa este page object. Não há spec que usa ProfilePage. |
| `frontend/client/e2e/fixtures/test-data.ts` | CNPJ/CPF generators com modulo-11 | ✓ VERIFIED | 87 linhas. generateCompanyData(), generateEmployeeData(), generateValidCnpj(), generateValidCpf(), generateUniqueSuffix(). Check digit calc com modulo-11. Unique suffix timestamp+counter. |
| `frontend/client/e2e/fixtures/jwt-utils.ts` | JWT decode via jose | ✓ VERIFIED | 32 linhas. decodeAccessToken() usando jose.decodeJwt. getAccessTokenFromCookies() lendo client_access_token httpOnly cookie. Interface DecodedToken com sub, email, groups?, realm_access?, company_id?. |
| `frontend/client/package.json` | @playwright/test + jose + scripts | ✓ VERIFIED | @playwright/test ^1.59.1, playwright ^1.59.1, jose ^6.2.2 em devDependencies. Scripts: test:e2e, test:e2e:ui. |
| `.gitignore` | playwright/.auth/, test-results/, playwright-report/ | ✓ VERIFIED | Todas 3 entradas presentes. |
| `frontend/client/playwright/.auth/.gitkeep` | Auth directory placeholder | ✓ VERIFIED | Arquivo existe. |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| registration.spec.ts | pages/registration.page.ts | import RegistrationPage | ✓ WIRED | `import { RegistrationPage } from './pages/registration.page'` — usado em new RegistrationPage(page), fillCompanyData, fillAccessData, submit |
| registration.spec.ts | pages/keycloak-login.page.ts | import KeycloakLoginPage | ✓ WIRED | `import { KeycloakLoginPage } from './pages/keycloak-login.page'` — usado em ACF redirect login |
| registration.spec.ts | fixtures/test-data.ts | import generateCompanyData | ✓ WIRED | `import { generateCompanyData } from './fixtures/test-data'` — gera CNPJ/CPF únicos |
| registration.spec.ts | fixtures/jwt-utils.ts | import getAccessTokenFromCookies, decodeAccessToken | ✓ WIRED | Importado e usado para verificar JWT claims no cookie |
| dashboard.spec.ts | pages/dashboard.page.ts | import DashboardPage | ✓ WIRED | `import { DashboardPage } from './pages/dashboard.page'` — goto + assertions |
| employee-management.spec.ts | pages/employees.page.ts | import EmployeesPage | ✓ WIRED | `import { EmployeesPage } from './pages/employees.page'` — goto + hasActionsColumn |
| employee-management.spec.ts | fixtures/test-data.ts | import generateEmployeeData | ✓ WIRED | Gera dados únicos de funcionário |
| employee-login.spec.ts | pages/keycloak-login.page.ts | import KeycloakLoginPage | ✓ WIRED | ACF login para 3 roles |
| employee-login.spec.ts | pages/employees.page.ts | import EmployeesPage | ✓ WIRED | hasActionsColumn verification |
| permission-ui.spec.ts | pages/employees.page.ts | import EmployeesPage | ✓ WIRED | goto + hasActionsColumn + action dropdown count |
| permission-ui.spec.ts | fixtures/jwt-utils.ts | import getAccessTokenFromCookies, decodeAccessToken | ✓ WIRED | JWT decode + cookie extraction |
| access-group-change.spec.ts | pages/keycloak-login.page.ts | import KeycloakLoginPage | ✓ WIRED | 3 logins (viewer, admin-empresa change, employee re-login) |
| access-group-change.spec.ts | pages/employees.page.ts | import EmployeesPage | ✓ WIRED | hasActionsColumn (before/after group change) |
| access-group-change.spec.ts | fixtures/test-data.ts | import generateEmployeeData | ✓ WIRED | Gera dados para employee criado via API |
| access-group-change.spec.ts | fixtures/jwt-utils.ts | import getAccessTokenFromCookies, decodeAccessToken | ✓ WIRED | JWT verification before/after group change |
| playwright.config.ts | e2e/auth/ | setup project testMatch | ✓ WIRED | testMatch: `/.*\.setup\.ts/` — matches admin-empresa.setup.ts e viewer.setup.ts |
| playwright.config.ts → admin-empresa project | playwright/.auth/admin-empresa.json | storageState | ✓ WIRED | storageState: 'playwright/.auth/admin-empresa.json' — matches setup save path |
| playwright.config.ts → viewer project | playwright/.auth/viewer.json | storageState | ✓ WIRED | storageState: 'playwright/.auth/viewer.json' — matches setup save path |
| admin-empresa.setup.ts | fixtures/test-data.ts | import generateValidCpf | ✓ WIRED | Usa para criar viewer employee via API |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|-------------------|--------|
| registration.spec.ts | `company` (from generateCompanyData) | test-data.ts generateValidCnpj + unique suffix | ✓ CNPJ com check digits, email único timestamp-based | ✓ FLOWING |
| employee-management.spec.ts | `employee` (from generateEmployeeData) | test-data.ts generateValidCpf + unique suffix | ✓ CPF com check digits, email único | ✓ FLOWING |
| permission-ui.spec.ts | `accessToken` (from getAccessTokenFromCookies) | httpOnly cookie `client_access_token` set by Vinxi /auth/callback | ✓ JWT real do Keycloak (requer ACF login prévio) | ✓ FLOWING |
| access-group-change.spec.ts | `employeeTempPassword` (from API response) | POST /employees/registration → .temporaryPassword | ✓ Senha temporária gerada pelo Keycloak Admin API | ✓ FLOWING |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Playwright test list | `cd frontend/client && npx playwright test --list` | 13 tests in 8 files across 6 projects | ✓ PASS |
| @playwright/test in package.json | `Select-String -Path package.json -Pattern '@playwright/test'` | `"@playwright/test": "^1.59.1"` found | ✓ PASS |
| jose in package.json | `Select-String -Path package.json -Pattern 'jose'` | `"jose": "^6.2.2"` found | ✓ PASS |
| test:e2e script exists | `Select-String -Path package.json -Pattern 'test:e2e'` | `"test:e2e": "npx playwright test"` found | ✓ PASS |
| .gitignore entries | `Select-String -Path .gitignore -Pattern 'playwright'` | `playwright/.auth/`, `playwright-report/` found | ✓ PASS |
| test-results in .gitignore | `Select-String -Path .gitignore -Pattern 'test-results'` | `test-results/` found | ✓ PASS |
| .gitkeep exists | `Test-Path playwright/.auth/.gitkeep` | True | ✓ PASS |
| Config defines 6 projects | Read playwright.config.ts projects array | 6 entries: setup, admin-empresa, viewer, registration, dashboard, employee-login | ✓ PASS |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| E2E-01 | 43-01 | Cadastro PJ completo → auto-login → redireciona para rota padrão | ✓ SATISFIED | `registration.spec.ts` — 2-step wizard → submit → ACF login → JWT verify |
| E2E-02 | 43-02 | Dashboard exibe cards mock | ✓ SATISFIED | `dashboard.spec.ts` — 6 card title assertions |
| E2E-03 | 43-02 | PJ cria funcionário → aparece na lista com status ativo e group viewer | ✓ SATISFIED | `employee-management.spec.ts` — API creation → UI verification → badges |
| E2E-04 | 43-03 | Login como funcionário → redirect baseado no access group | ✓ SATISFIED | `employee-login.spec.ts` — 3 redirect tests (viewer, admin-empresa, dashboard) |
| E2E-05 | 43-03 | JWT decode revela groups/roles corretos — permissões UI | ✓ SATISFIED | `permission-ui.spec.ts` — JWT decode + UI permission verification for viewer and admin-empresa |
| E2E-06 | 43-03 | PJ muda access group → login novamente → permissões atualizadas | ✓ SATISFIED | `access-group-change.spec.ts` — viewer → admin-empresa via dialog → re-login → verify |
| E2E-07 | 43-01, 43-02, 43-03 | Todos os E2E tests passam | ✓ SATISFIED (list) | 13 tests listados. Execução real requer Docker Compose + env vars → human_needed |

**Orphaned requirements:** Nenhum — todos os 7 requirement IDs (E2E-01 a E2E-07) são cobertos pelos 3 plans.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `access-group-change.spec.ts` | 121, 137 | `page.waitForTimeout(3000)` / `page.waitForTimeout(2000)` | ⚠️ Warning | Usado para Keycloak eventual consistency group propagation — aceitável para E2E (sem alternativa determinística). Pesquisa documentou este pitfall. |
| `pages/profile.page.ts` | 13 | `page.locator('[data-testid="profile-container"]')` — perfil nunca testado | ℹ️ Info | ProfilePage existe mas nenhum spec o importa. Não há requisito E2E para profile. ORPHANED mas não é blocker. |

**Stub classification:** Nenhum arquivo é stub. Todos os specs têm testes substantivos com assertions reais. Os `waitForTimeout` são para consistência eventual do Keycloak — documentados como pitfall na pesquisa.

### Human Verification Required

### 1. Execução do suite E2E completo

**Test:** Com Docker Compose ativo (`docker compose up -d`) e env vars configuradas (`E2E_PJ_EMAIL`, `E2E_PJ_PASSWORD`, `E2E_VIEWER_EMAIL`, `E2E_VIEWER_PASSWORD`), rodar `cd frontend/client && npx playwright test`
**Expected:** 13 tests passam (incluindo 2 setup, 1 registration, 1 dashboard, 1 employee-management, 3 employee-login, 4 permission-ui em 2 projetos, 1 access-group-change)
**Why human:** E2E tests requerem stack completo rodando — Keycloak (8180), API (8080), Vinxi (5173) — e não podem ser executados sem infraestrutura ativa

### 2. Cadastro PJ → ACF Redirect Chain

**Test:** Navegar para `/register`, preencher wizard 2-step, submit, verificar redirect completo: Keycloak login → callback → `/employees`
**Expected:** Usuário autenticado na rota padrão admin-empresa, sidebar visível
**Why human:** Fluxo ACF com Keycloak requer navegador real e interação com formulário de login Keycloak

### 3. Dashboard Cards Visuais

**Test:** Login como admin-empresa, navegar `/dashboard`, verificar 6 cards renderizados com dados mock
**Expected:** Total Funcionários, Ativos, Bloqueados, Logins Recentes, Ações Recentes, Último Login visíveis
**Why human:** Verificação visual de rendering requer app rodando

### 4. Viewer Permissões Read-Only

**Test:** Login como viewer, verificar ausência de Ações column, ausência de Dashboard no sidebar, JWT contém group "viewer"
**Expected:** UI completamente read-only, 0 action dropdowns
**Why human:** Permissões UI dependem de estado de autenticação real com Keycloak

### 5. Mudança de Group → Permissões Atualizadas

**Test:** Executar fluxo completo do access-group-change.spec.ts: criar employee → login viewer → mudar group → re-login → verificar admin-empresa
**Expected:** Após group change + re-login, Ações column visível, Dashboard no sidebar, JWT admin-empresa
**Why human:** Consistência eventual do Keycloak só testável em ambiente real com timing de propagação

### Gaps Summary

**Nenhum gap estrutural encontrado.** Todos os 16 artifacts existem, são substantivos, e estão wired aos seus consumidores. Todos os 7 requisitos E2E (E2E-01 a E2E-07) têm specs funcionais com assertions significativas. A cobertura de key links é completa — nenhum import quebrado ou page object não utilizado (exceto profile.page.ts que é ORPHANED mas não requisito).

O único ponto é que a **execução real dos testes E2E requer infraestrutura ativa** (Docker Compose com Keycloak, API, PostgreSQL e Vinxi rodando + env vars configuradas). Os testes estão corretos estruturalmente e `npx playwright test --list` confirma que todos 13 testes são reconhecidos pelo Playwright, mas a aprovação real (green run) é uma verificação humana.

**Nota sobre profile.page.ts:** Este arquivo é a única exceção ORPHANED — existe como page object mas nenhum spec o importa. O plano original previa 5 page objects, e todos existem, mas o ProfilePage não é usado porque não há requisito E2E específico para a página de perfil nesta fase. Isso não é um gap — é um page object preparado para uso futuro.

---

_Verified: 2026-04-27T12:30:00Z_
_Verifier: the agent (gsd-verifier)_