# Requirements — Multi-Milestone (v1.0, v3.0, v4.0)

**Milestones Covered:**
- v1.0 — Cadastro e Login com Perfil Read-Only
- v3.0 — Painel Administrativo (Backoffice)
- v4.0 — CI/CD Pipeline + Cybersecurity

**Source:** FEATURES.md + SUMMARY.md + PROJECT.md
**Created:** 2026-04-09
**Last Updated:** 2026-04-11
**Status:** DRAFT — awaiting review

---

## Core Value

Cadastro seguro e funcional de clientes PF/PJ com autenticação robusta via Keycloak — se a segurança falhar, nada mais importa.

---

## Scope Summary

### v1.0 — Client Onboarding

| Category | v1 | v2 | Out of Scope |
|----------|----|----|-------------|
| Registration (PF + PJ) | ✅ 8 | — | — |
| Authentication (Login) | ✅ 5 | — | 1 |
| Profile (Read-Only) | ✅ 3 | — | — |
| Security Hardening | ✅ 7 | — | 1 |
| Observability | ✅ 4 | — | — |
| Infrastructure | ✅ 5 | — | — |
| Data Integrity | ✅ 3 | — | — |
| **Total** | **35** | **0** | **2** |

### v3.0 — Admin Backoffice

| Category | v3 | v4 | Out of Scope |
|----------|----|----|-------------|
| Admin API Endpoints | ✅ 5 | — | — |
| Admin Auth & Session | ✅ 3 | — | — |
| Admin Backoffice UI | ✅ 6 | — | — |
| Admin E2E Testing | ✅ 5 | — | — |
| Architecture (Frontend Separation) | ✅ 3 | — | — |
| **Total** | **22** | **0** | **0** |

### v4.0 — CI/CD Pipeline + Cybersecurity

| Category | v4 | v5 | Out of Scope |
|----------|----|----|-------------|
| CI/CD Pipeline (GitHub Actions) | ✅ 4 | — | — |
| SAST (Semgrep + CodeQL) | ✅ 3 | — | — |
| SCA (Dependabot + Trivy) | ✅ 3 | — | — |
| Container Security (Trivy + Dockle) | ✅ 3 | — | — |
| IaC Scanning (Checkov + Kubescape) | ✅ 3 | — | — |
| Secrets Detection (Gitleaks + TruffleHog) | ✅ 3 | — | — |
| GitHub Security Integration | ✅ 3 | — | — |
| Security Documentation | ✅ 3 | — | — |
| **Total** | **25** | **3 deferred** | **0** |

### Grand Total

| Milestone | Requirements | Deferred | Out of Scope |
|-----------|-------------|----------|-------------|
| v1.0 | 35 | 0 | 2 |
| v3.0 | 22 | 0 | 0 |
| v4.0 | 25 | 3 | 0 |
| **All** | **82** | **3** | **2** |

---

## 1. Registration — PF (Pessoa Física)

### R1.1 — PF Registration Form
- [ ] Formulário React com campos: nome completo, CPF, email, telefone, senha, confirmação de senha
- [ ] Validação client-side com Zod para feedback instantâneo
- [ ] Submissão via POST para endpoint da API .NET

### R1.2 — Server-Side CPF Validation
- [ ] Validação de CPF via algoritmo módulo 11 no backend
- [ ] CPF é value object no Domain (não string pura)
- [ ] Rejeitar CPF inválido com 400 + mensagem clara

### R1.3 — Server-Side Field Validation
- [ ] Validação de email (formato RFC 5322)
- [ ] Validação de telefone (formato brasileiro, 10-11 dígitos)
- [ ] Validação de senha: min 8 chars, 1 maiúscula, 1 minúscula, 1 dígito, 1 caractere especial
- [ ] Nome: obrigatório, 2-200 caracteres
- [ ] Todas as validações rodam no servidor — client-side é apenas UX

### R1.4 — Duplicate Detection (CPF)
- [ ] Verificar unicidade de CPF no PostgreSQL antes de criar usuário
- [ ] Retornar 409 Conflict se CPF já existe

### R1.5 — Duplicate Detection (Email)
- [ ] Verificar unicidade de email no PostgreSQL e no Keycloak antes de criar usuário
- [ ] Retornar 409 Conflict se email já existe

### R1.6 — Persist PostgreSQL First
- [ ] Criar registro do Client no banco app_db ANTES de criar usuário no Keycloak
- [ ] Transação: se falhar no Keycloak, rollback no PostgreSQL

### R1.7 — Keycloak User Creation via Admin API
- [ ] Criar usuário no Keycloak via Admin API usando service account dedicado
- [ ] Service account tem role `manage-users` (least privilege)
- [ ] Senha definida no payload de criação

### R1.8 — Post-Registration Redirect
- [ ] Após sucesso (201), redirecionar usuário para tela de login
- [ ] Mensagem de sucesso exibida na tela de login

---

## 2. Registration — PJ (Pessoa Jurídica)

### R2.1 — PJ Registration Form
- [ ] Formulário React com campos: razão social, CNPJ, email, telefone, senha, confirmação de senha
- [ ] Validação client-side com Zod para feedback instantâneo
- [ ] Submissão via POST para endpoint da API .NET

### R2.2 — Server-Side CNPJ Validation
- [ ] Validação de CNPJ via algoritmo check-digit no backend
- [ ] CNPJ é value object no Domain (não string pura)
- [ ] Suportar formato numérico atual E formato alfanumérico (vigente Julho 2026)
- [ ] Rejeitar CNPJ inválido com 400 + mensagem clara

### R2.3 — Duplicate Detection (CNPJ)
- [ ] Verificar unicidade de CNPJ no PostgreSQL antes de criar usuário
- [ ] Retornar 409 Conflict se CNPJ já existe

### R2.4 — Duplicate Detection (Email) — PJ
- [ ] Mesma verificação de email do PF (compartilhada)
- [ ] Email é único globalmente, independente de PF ou PJ

*(R2.5–R2.8 são compartilhados com PF: server-side validation, persist, Keycloak creation, redirect)*

---

## 3. Authentication (Login)

### R3.1 — Custom Login Form (ROPC)
- [ ] Formulário React com campos: email, senha
- [ ] Submissão via POST direto para endpoint `/token` do Keycloak (ROPC Grant)
- [ ] Tela customizada — não usar tema padrão do Keycloak

### R3.2 — JWT Token Storage
- [ ] Access token e refresh token armazenados em memória (NÃO localStorage)
- [ ] Tokens perdidos em refresh de página — usuário faz login novamente
- [ ] Refresh token usado para renovar access token expirado

### R3.3 — Protected Route Enforcement
- [ ] Rotas protegidas verificam presença e validade do JWT
- [ ] Redirecionar para login se token ausente ou expirado
- [ ] Backend valida JWT em cada request (Authorization: Bearer)

### R3.4 — Generic Auth Error Messages
- [ ] Login falho retorna sempre "Credenciais inválidas" (genérico)
- [ ] Não diferenciar "usuário não encontrado" de "senha incorreta"
- [ ] Evitar enumeration de contas

### R3.5 — Session Timeout
- [ ] Access Token lifespan: 5 minutos
- [ ] SSO Session Max: 8 horas
- [ ] Após 8h, exigir re-autenticação

---

## 4. Profile (Read-Only)

### R4.1 — Profile API Endpoint
- [ ] GET `/api/clients/me` retorna dados do cliente autenticado
- [ ] Autenticação via Bearer JWT
- [ ] Retorna dados específicos de PF ou PJ conforme PersonType

### R4.2 — Read-Only Profile View
- [ ] React renderiza dados do cliente em modo leitura (sem edição)
- [ ] Campos exibidos: nome/razão social, CPF/CNPJ, email, telefone
- [ ] Loading state enquanto busca dados da API

### R4.3 — Protected Profile Route
- [ ] Rota `/profile` exige autenticação
- [ ] Usuário não autenticado é redirecionado para login
- [ ] Após login, redirect automático para profile

---

## 5. Security Hardening

### R5.1 — Keycloak Brute Force Protection
- [ ] Brute Force Detection habilitado no Realm Settings
- [ ] Max login failures: 5
- [ ] Wait increment: 30 segundos (com escalada)

### R5.2 — Keycloak Password Policy
- [ ] Minimum length: 8 caracteres
- [ ] Require uppercase, lowercase, digits, special characters
- [ ] Aplicada tanto no cadastro quanto na troca de senha

### R5.3 — HTTPS Enforcement
- [ ] Keycloak SSL mode: `all requests`
- [ ] Local dev: HTTP permitido com self-signed cert
- [ ] Produção: HTTPS obrigatório

### R5.4 — Keycloak SSRF Prevention
- [ ] Desabilitar `request_uri` no Keycloak (CVE-2020-10770, CVE-2026-1518)
- [ ] Redirect URIs registrados exatamente — sem wildcards
- [ ] Admin console bound a 127.0.0.1 apenas

### R5.5 — Exact Redirect URIs
- [ ] URIs de redirect registradas exatamente no Keycloak
- [ ] Sem wildcards (`*`) — previne open redirect attacks

### R5.6 — Correlation ID Propagation
- [ ] Header `X-Correlation-ID` injetado em todas as chamadas ao Keycloak Admin API
- [ ] Correlacionar logs da API com logs do Keycloak

### R5.7 — Log Masking
- [ ] Senhas, tokens e CPF/CNPJ mascarados nos logs
- [ ] Nunca logar credenciais em texto puro

### R5.8 — Realm Configuration
- [ ] Realm: `onboarding`
- [ ] Public client: `onboarding-app` (Direct Access Grants Enabled, no secret)
- [ ] Confidential client: `onboarding-api-admin` (Service Account, `manage-users` role)

---

## 6. Observability

### R6.1 — Structured Logging (Serilog)
- [ ] Serilog configurado desde o primeiro endpoint
- [ ] Logs em JSON estruturado no stdout
- [ ] Correlation IDs incluídos em cada entrada de log
- [ ] Request logging automático (Serilog.AspNetCore)

### R6.2 — OpenTelemetry Traces
- [ ] SDK configurado com instrumentação de: ASP.NET Core, HttpClient, EF Core
- [ ] TraceId propagado entre API → Keycloak → PostgreSQL
- [ ] Export via OTLP (stdout JSON para dev, collector para prod)

### R6.3 — Health Check Endpoints
- [ ] `/healthz` na API .NET (ASP.NET Core health checks)
- [ ] Keycloak `/health/ready` acessível para Docker Compose
- [ ] Docker Compose usa healthchecks para condições de dependência

### R6.4 — Metrics
- [ ] Métricas de latência de registration e login
- [ ] Métricas de falhas de autenticação (brute force detection)
- [ ] Export via OTLP

---

## 7. Infrastructure

### R7.1 — Docker Compose
- [ ] `compose.yaml` com todos os serviços: API, Frontend, Keycloak, PostgreSQL x2
- [ ] Healthchecks + `depends_on` conditions para evitar race conditions
- [ ] Rede interna isolada entre serviços

### R7.2 — Two PostgreSQL Containers
- [ ] `app_db` para dados da aplicação
- [ ] `keycloak_db` para dados internos do Keycloak
- [ ] Isolamento estrito — nenhum serviço acessa o banco do outro

### R7.3 — Keycloak Container
- [ ] Imagem: `quay.io/keycloak/keycloak:26.1`
- [ ] Modo produção (`start --optimized`)
- [ ] Realm `onboarding` provisionado via configuração

### R7.4 — Environment Variables
- [ ] Todas as credenciais via `.env` (não hardcoded)
- [ ] `.env.example` documentado sem valores sensíveis
- [ ] `.env` no `.gitignore`

### R7.5 — Startup Order
- [ ] PostgreSQL → Keycloak → API → Frontend
- [ ] Healthchecks garantem que cada serviço está pronto antes do próximo

---

## 8. Data Integrity

### R8.1 — DDD Domain Model
- [ ] Value objects: `Cpf`, `Cnpj`, `Email`, `PhoneNumber`
- [ ] Aggregate: `Client` com PersonType (PF | PJ)
- [ ] Factory methods: `Client.CreatePf(...)`, `Client.CreatePj(...)`
- [ ] Invariantes de domínio garantidos no construtor/factory

### R8.2 — CQRS Application Layer
- [ ] `ICommandHandler` e `IQueryHandler` via DI manual (sem MediatR)
- [ ] `RegisterClientCommand` com handler dedicado
- [ ] Separação clara entre comandos (write) e queries (read)

### R8.3 — EF Core Persistence
- [ ] Code-first migrations
- [ ] Repositório implementado no Infrastructure layer
- [ ] Transações para operações que envolvem PostgreSQL + Keycloak

---

## v2 — Deferred

Nenhum requisito foi explicitamente delegado para v2. Os itens abaixo foram considerados durante a pesquisa mas são **parte do v1** para garantir integridade:

- CNPJ alphanumeric format (Julho 2026 deadline — implementar antes)
- Serilog + OpenTelemetry (retrofit é caro — fazer desde o início)
- Idempotency keys (adicionar se QA encontrar double-submit issues)

---

## Out of Scope — v1

### O1 — Email Verification
- **Razão:** Adiciona step assíncrono que bloqueia login; requer infraestrutura de email
- **Quando adicionar:** Quando infra de email existir, como fase isolada

### O2 — Profile Editing
- **Razão:** Introduz write-back para PostgreSQL e Keycloak simultaneamente; riscos de concorrência
- **Quando adicionar:** Milestone separado, testável de forma isolada

### O3 — Social Login (Google, GitHub, OAuth)
- **Razão:** Complexidade sem valor para v1; Keycloak torna retrofittável
- **Quando adicionar:** Se requisito emergir futuramente

### O4 — Admin Dashboard / Backoffice
- **Razão:** Escopo do milestone v3.0 — separado do v1
- **Quando adicionar:** Já em progresso (v3.0)

### O5 — Password Reset / Forgot Password
- **Razão:** Keycloak provê nativamente; duplicar trabalho é risco
- **Quando adicionar:** Quando infra de email estiver pronta

### O6 — Migration ROPC → Auth Code + PKCE
- **Razão:** Decisão consciente — documentado, não para v1
- **Quando adicionar:** v4.0 (security milestone)

### O7 — Mobile App / PWA
- **Razão:** Web-first é o default correto; React + Vinxi já é responsivo
- **Quando adicionar:** Se requisito mobile-native emergir

### O8 — Push/Email Notifications
- **Razão:** Sem evento que requer notificação no v1
- **Quando adicionar:** Quando workflow com steps assíncronos for introduzido

---

## Sources

- `.planning/research/FEATURES.md` — Feature landscape com table stakes, differentiators, anti-features
- `.planning/research/SUMMARY.md` — Research synthesis com stack consensus e risk register
- `.planning/PROJECT.md` — Project definition, core value, current milestone
- `QWEN.md` — Stack technology decisions e architecture patterns

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-09 | Initial requirements from research | GSD |
| 2026-04-09 | Added v3.0 Admin Backoffice requirements (ADMIN-01 to ADMIN-16) | GSD |

---

# Requirements — v3.0: Admin Backoffice Panel

**Milestone:** v3.0 — Painel Administrativo para Gerenciamento de Usuários
**Source:** PROJECT.md Current Milestone v3.0 section
**Created:** 2026-04-09
**Status:** DRAFT — awaiting review

---

## 9. Admin API Endpoints

### R9.1 — Paginated User Listing
- [ ] GET `/api/admin/users` retorna lista paginada (20 por página)
- [ ] Query params: `page`, `pageSize`, `search` (nome/email/documento), `status` (active/blocked/deleted)
- [ ] Responde com `{ items, totalCount, page, pageSize }`
- [ ] Protegido por `[Authorize(Roles = "admin")]`

### R9.2 — User Details
- [ ] GET `/api/admin/users/{id}` retorna dados completos do usuário (PF ou PJ)
- [ ] Inclui status no Keycloak (enabled, locked, roles)
- [ ] Retorna 404 se usuário não existe
- [ ] Protegido por `[Authorize(Roles = "admin")]`

### R9.3 — User Update
- [ ] PUT `/api/admin/users/{id}` atualiza dados do usuário
- [ ] Validação server-side completa (FluentValidation)
- [ ] Atualiza PostgreSQL e Keycloak (transação/compensação)
- [ ] Retorna 400 se validação falhar, 404 se não existe
- [ ] Protegido por `[Authorize(Roles = "admin")]`

### R9.4 — User Block/Unblock
- [ ] POST `/api/admin/users/{id}/block` desativa usuário no Keycloak
- [ ] POST `/api/admin/users/{id}/unblock` reativa usuário no Keycloak
- [ ] Requer campo `reason` no payload para audit trail
- [ ] Retorna 409 se usuário já está no estado solicitado
- [ ] Protegido por `[Authorize(Roles = "admin")]`

### R9.5 — LGPD-Compliant User Deletion
- [ ] DELETE `/api/admin/users/{id}` anonimiza dados no PostgreSQL + deleta usuário no Keycloak
- [ ] Anonimização: nome → "Deleted User", CPF/CNPJ → null, email → anonymized-{id}@deleted.local
- [ ] Retorna 204 se sucesso, 404 se não existe
- [ ] Audit log registra deleção com timestamp e admin responsável
- [ ] Protegido por `[Authorize(Roles = "admin")]`

## 10. Admin Auth & Session Management

### R10.1 — HttpOnly Cookie Authentication
- [ ] Admin login usa cookies httpOnly, Secure, SameSite=Strict
- [ ] Nenhum JWT exposto no localStorage ou sessionStorage do frontend
- [ ] Cookie configurado com path `/api` e expiry adequado

### R10.2 — Transparent Token Refresh
- [ ] Middleware intercepta 401, usa refresh token para obter novo access token
- [ ] Retry automático da requisição original após refresh
- [ ] Se refresh falhar, session expirada → redirect para login

### R10.3 — Session Restoration & Error Handling
- [ ] Ao carregar página, frontend chama `/api/auth/me` para verificar sessão
- [ ] 401 → redirect para `/admin/login` com toast "Sessão expirada"
- [ ] 403 → página de acesso negado "Você não tem permissão para acessar esta área"
- [ ] 5xx → toast genérico "Erro interno do servidor"

## 11. Admin Backoffice UI

### R11.1 — User Listing Page
- [ ] `/admin/users` exibe tabela paginada com nome, documento, email, status, ações
- [ ] Search bar com debounce 300ms (busca por nome, CPF/CNPJ, email)
- [ ] Dropdown de filtro por status: Todos, Ativo, Bloqueado, Deletado
- [ ] Skeleton loading states durante chamadas API
- [ ] Estado vazio exibe "Nenhum usuário encontrado"

### R11.2 — User Detail Page
- [ ] `/admin/users/{id}` exibe dados PF/PJ em modo leitura
- [ ] Badge de status do Keycloak (enabled/disabled/locked)
- [ ] Botões de ação: Editar, Bloquear/Desbloquear, Excluir
- [ ] Breadcrumb navigation: Users → Detail

### R11.3 — Edit User Form
- [ ] Formulário de edição com validação client-side (Zod) e server-side (FluentValidation)
- [ ] Campos: nome/razão social, email, telefone, documento (read-only)
- [ ] Toast de sucesso após atualização
- [ ] Reverter otimista em caso de erro API

### R11.4 — Block/Unblock Dialog
- [ ] Dialog de confirmação com campo obrigatório `reason`
- [ ] Texto explicativo: "Bloqueio impede login do usuário imediatamente"
- [ ] Ação registrada em audit log
- [ ] Tabela atualiza status automaticamente após ação

### R11.5 — LGPD Deletion Flow
- [ ] Dialog exige digitar email do usuário para confirmar
- [ ] Texto de aviso: "Esta ação é irreversível. Dados serão anonimizados."
- [ ] Após confirmação: DELETE `/api/admin/users/{id}` → toast "Usuário excluído conforme LGPD"
- [ ] Tabela remove usuário automaticamente

### R11.6 — Admin Layout & Navigation
- [ ] Layout fixo com header: logo "Backoffice Admin", nome do admin logado, botão logout
- [ ] Sidebar com navegação: Usuários, Audit Log (future), Configurações (future)
- [ ] Responsivo para mobile (sidebar colapsa)

## 12. Admin E2E Testing

### R12.1 — Admin Flow E2E Tests
- [ ] E2E: Admin login → lista usuários → busca → filtra por status → vê detalhes
- [ ] E2E: Admin edita usuário → erros de validação → atualização com sucesso → toast
- [ ] E2E: Admin bloqueia usuário → dialog de confirmação → usuário bloqueado → tabela atualiza
- [ ] E2E: Admin exclui usuário (LGPD) → digita email para confirmar → usuário anonimizado
- [ ] E2E: Usuário não-admin acessando `/admin` → página 403 acesso negado

---

## Requirement Traceability — v3.0

| Requirement | Phase | Status |
|-------------|-------|--------|
| ADMIN-01 — Paginated User Listing | Phase 16 | 📋 Planned |
| ADMIN-02 — User Details | Phase 16 | 📋 Planned |
| ADMIN-03 — User Update | Phase 16 | 📋 Planned |
| ADMIN-04 — User Block/Unblock | Phase 16 | 📋 Planned |
| ADMIN-05 — LGPD Deletion | Phase 16 | 📋 Planned |
| ADMIN-06 — HttpOnly Cookie Auth | Phase 17 | 📋 Planned |
| ADMIN-07 — Transparent Token Refresh | Phase 17 | 📋 Planned |
| ADMIN-08 — Session Restoration & Error Handling | Phase 17 | 📋 Planned |
| ADMIN-09 — User Listing Page | Phase 18 | 📋 Planned |
| ADMIN-10 — User Detail Page | Phase 18 | 📋 Planned |
| ADMIN-11 — Edit User Form | Phase 19 | 📋 Planned |
| ADMIN-12 — Block/Unblock Dialog | Phase 19 | 📋 Planned |
| ADMIN-13 — LGPD Deletion Flow | Phase 20 | 📋 Planned |
| ADMIN-14 — Admin Layout & Navigation | Phase 18 | ✅ Complete |
| ADMIN-15 — E2E Admin Flows | Phase 21 | 📋 Planned |
| ADMIN-16 — Production Documentation | Phase 21 | 📋 Planned |

---

## Architecture — Frontend Separation

### ARCH-01 — Independent Client Project
- [ ] `frontend/client` tem seu próprio `package.json`, `app.config.ts`, `Dockerfile`, `tsconfig.json`
- [ ] Contém apenas telas do usuário final: login, registro, perfil, forgot/reset password
- [ ] Roda na porta 5173 (preservada do original)
- [ ] Build e deploy independentes do backoffice

### ARCH-02 — Independent Backoffice Project
- [ ] `frontend/backoffice` tem seu próprio `package.json`, `app.config.ts`, `Dockerfile`, `tsconfig.json`
- [ ] Contém apenas telas administrativas: admin login, users list, user detail
- [ ] Roda na porta 5174 (nova)
- [ ] Build e deploy independentes do client

### ARCH-03 — Zero Cross-Import Rule
- [ ] Nenhum arquivo de código é compartilhado entre os dois projetos
- [ ] Componentes shadcn/ui, utils, e configs são duplicados (não importados)
- [ ] Cada projeto tem seus próprios contextos de auth, routers, e API clients
- [ ] Código duplicado é aceitável; import cruzado é proibido

---

# Requirements — v4.0: CI/CD Pipeline + Cybersecurity

**Milestone:** v4.0 — Pipeline de Integração Contínua + Esteira de Segurança
**Source:** PROJECT.md v4.0 section + FEATURES.md + PITFALLS.md
**Created:** 2026-04-11
**Status:** DRAFT — awaiting review

---

## 13. GitHub Actions CI/CD Pipeline

### R13.1 — Parallel Build Jobs
- [ ] Workflow `.github/workflows/ci.yml` com 3 jobs paralelos independentes
- [ ] Job `backend`: .NET 10 SDK build + testes unitários + cobertura
- [ ] Job `frontend-client`: Vinxi build + ESLint + TypeScript type check
- [ ] Job `frontend-backoffice`: Vinxi build + ESLint + TypeScript type check
- [ ] Jobs rodam em `ubuntu-latest` com cache de dependências (NuGet, npm)
- [ ] Cada job falha independentemente — falha em um não bloqueia outros

### R13.2 — Backend Build + Test Validation
- [ ] `dotnet build --configuration Release` sem warnings críticos
- [ ] `dotnet test` executa todos os testes unitários (xUnit)
- [ ] Cobertura mínima de testes: 80% de line coverage
- [ ] `dotnet test /p:CollectCoverage=true` com report em cobertura XML
- [ ] Falha se cobertura < threshold definido

### R13.3 — Frontend Build Validation
- [ ] `npm ci` instala dependências com lockfile integrity
- [ ] `tsc --noEmit` valida tipos sem gerar output
- [ ] `eslint . --max-warnings 0` bloqueia warnings como erros
- [ ] `vinxi build` gera artefato de produção sem erros
- [ ] Aplicado a ambos os projetos (`frontend/client` e `frontend/backoffice`)

### R13.4 — Pipeline Caching Strategy
- [ ] Cache de `~/.nuget/packages` para builds .NET subsequentes
- [ ] Cache de `node_modules/.cache` para builds Vite/Vinxi
- [ ] Chave de cache baseada em lockfiles (`*.csproj`, `package-lock.json`)
- [ ] Cache com restore-keys para partial hits

---

## 14. Static Application Security Testing (SAST)

### R14.1 — Semgrep Configuration
- [ ] `.semgrep/` directory com rules customizadas para C# e React
- [ ] Regras específicas para:
  - Detecção de `localStorage` para tokens (anti-pattern)
  - Uso de `HttpContext.Request` sem validação CSRF
  - Hardcoded credentials/API keys em código
  - Falta de validação em inputs de CPF/CNPJ
- [ ] CI step roda `semgrep ci --config auto` em PRs
- [ ] Falha crítica em regras de severidade ERROR

### R14.2 — CodeQL Integration
- [ ] GitHub Advanced Security habilitado com CodeQL analysis
- [ ] `codeql database init` para C# (`dotnet`) e JavaScript/React
- [ ] Queries de segurança para:
  - SQL Injection (EF Core LINQ injection patterns)
  - XSS (React `dangerouslySetInnerHTML` sem sanitização)
  - Insecure deserialization (JSON sem validação de tipo)
  - Path traversal (leitura de arquivos com input do usuário)
- [ ] Resultados visíveis em GitHub Security Tab → Code scanning alerts
- [ ] Bloqueio de merge em alertas CRITICAL ou HIGH

### R14.3 — SAST Policy Enforcement
- [ ] Branch protection rule exige CodeQL scan passing antes de merge
- [ ] PRs com novos alertas SAST exigem justification ou fix
- [ ] Dashboard de tendências de alertas (monitorar redução ao longo do tempo)

---

## 15. Software Composition Analysis (SCA)

### R15.1 — Dependabot Configuration
- [ ] `.github/dependabot.yml` configurado para:
  - `nuget` (backend .NET packages)
  - `npm` (frontend packages — ambos os projetos)
  - `docker` (base images em Dockerfiles)
  - `github-actions` (actions de terceiros)
- [ ] Frequency: `weekly` com open pull requests automáticos
- [ ] Labels: `dependencies`, `security` para CVE updates
- [ ] Auto-merge para patches e minors com CI passing

### R15.2 — Trivy Dependency Scanning
- [ ] CI step roda `trivy fs --scanners vuln .` no repo root
- [ ] Detecta vulnerabilidades em:
  - `packages-lock.json` (npm dependencies)
  - `*.csproj` (NuGet dependencies)
  - Imagens base em Dockerfiles
- [ ] Falha se encontrar vulnerabilidades CRITICAL ou HIGH sem fix disponível
- [ ] Report em SARIF format → upload para GitHub Security Tab

### R15.3 — SCA Reporting
- [ ] Dependabot alerts visíveis em GitHub Security Tab → Dependabot alerts
- [ ] Trivy results exportados para SARIF → Code scanning alerts
- [ ] Dashboard semanal de vulnerabilities open/fix rate

---

## 16. Container Security Scanning

### R16.1 — Trivy Image Scanning
- [ ] CI step executa após build de cada Dockerfile:
  - `docker build -t onboarding-api:ci src/Api/`
  - `trivy image --severity HIGH,CRITICAL onboarding-api:ci`
- [ ] Scanning aplicado a todas as imagens:
  - Backend API (.NET 10 image)
  - Frontend client (Node/Vinxi image)
  - Frontend backoffice (Node/Vinxi image)
  - Keycloak (quay.io/keycloak/keycloak:26.1)
- [ ] Falha se encontrar CVEs CRITICAL ou HIGH na imagem
- [ ] Report em SARIF → GitHub Security Tab

### R16.2 — Dockle Best Practices Check
- [ ] CI step roda `dockle onboarding-api:ci` em cada imagem
- [ ] Verifica boas práticas de Dockerfile:
  - Não rodar como root (`USER` directive)
  - Não usar `latest` tag em base images
  - `.dockerignore` presente e configurado
  - Healthcheck configurado no Dockerfile
  - Secretos não hardcoded em ENV vars
- [ ] Falha em checks de severidade FATAL ou WARN
- [ ] Dockle report em formato JSON → artifacts do workflow

### R16.3 — Container Image Tagging Policy
- [ ] Tags de imagem seguem semver: `onboarding-api:1.2.3`
- [ ] Tags `latest` apenas em builds de main branch
- [ ] Tags `sha-{commit}` para rastreabilidade de builds
- [ ] Image push para registry apenas se scanning passou

---

## 17. Infrastructure as Code (IaC) Scanning

### R17.1 — Checkov for Docker Compose
- [ ] CI step roda `checkov --framework dockerfile --file compose.yaml`
- [ ] Verifica configurações de segurança no Docker Compose:
  - Containers expostos à rede host sem necessidade
  - Volumes montados com permissões excessivas
  - Secrets em variáveis de ambiente não sensíveis
  - Capabilities do Linux não restritas (`privileged: true`)
  - Usuário de container não especificado
- [ ] Falha em checks CRITICAL ou HIGH
- [ ] Checkov report em SARIF → GitHub Security Tab

### R17.2 — Kubescape for Future Kubernetes
- [ ] Kubescape instalado no CI (preparação para futuro deploy K8s)
- [ ] Scan de manifests Kubernetes quando existirem (ainda não no v4.0)
- [ ] Framework: NSA-CISA, MITRE ATT&CK
- [ ] **Nota**: Kubescape é setup-only no v4.0 — scanning real quando K8s manifests forem criados

### R17.3 — IaC Policy Documentation
- [ ] `docs/iac-policies.md` documenta:
  - Regras de segurança para Docker Compose
  - Regras futuras para Kubernetes manifests
  - Processo de exception/approval para waivers
  - Responsáveis por review de IaC changes

---

## 18. Secrets Detection

### R18.1 — Gitleaks Pre-Commit + CI
- [ ] `.gitleaks.toml` configurado com regras para:
  - AWS keys, Azure keys, GCP credentials
  - JWT signing keys
  - Database connection strings com senhas
  - Keycloak client secrets
  - API keys genéricas (padrão `sk-*`, `pk-*`, etc.)
- [ ] Pre-commit hook local (via `pre-commit` framework ou Husky)
- [ ] CI step roda `gitleaks detect --source . --verbose` em PRs
- [ ] Falha se detectar qualquer secret committed
- [ ] Allowlist para falsos positivos (test fixtures, exemplos documentados)

### R18.2 — TruffleHog Active Verification
- [ ] CI step roda `trufflehog filesystem --directory . --only-verified`
- [ ] Diferente de Gitleaks (pattern matching), TruffleHog **verifica ativamente** se a credencial é válida
- [ ] Verifica em:
  - Git history completo (não apenas diff do PR)
  - Arquivos atuais do repo
  - Branches de feature abertas
- [ ] Falha se encontrar credencial verificada como ativa
- [ ] TruffleHog report em SARIF → GitHub Security Tab

### R18.3 — Secrets Incident Response
- [ ] `docs/secrets-incident-response.md` documenta:
  - Processo de revogação imediata quando secret é detectada
  - Rotação de chaves afetadas (Keycloak client secrets, DB passwords)
  - Comunicação de incidente ao time
  - Post-mortem template para análise de causa raiz
- [ ] Runbook automatizado: detecção → revogação → rotação → verificação

---

## 19. GitHub Security Integration

### R19.1 — Security Tab Dashboard
- [ ] GitHub Security Tab exibe dashboard consolidado com:
  - Dependabot alerts (SCA)
  - Code scanning alerts (SAST: Semgrep + CodeQL)
  - Secret scanning alerts (Gitleaks + TruffleHog)
  - Container vulnerabilities (Trivy image scans)
- [ ] Todos os reports exportados em SARIF format
- [ ] Dashboard de tendência: alertas open/closed over time

### R19.2 — Branch Protection Rules
- [ ] Branch `main` protegida com:
  - Require pull request reviews (min 1 reviewer)
  - Require status checks passing:
    - `backend (build + test)`
    - `frontend-client (build + lint)`
    - `frontend-backoffice (build + lint)`
    - `Semgrep SAST`
    - `CodeQL Analysis`
    - `Trivy Dependency Scan`
    - `Trivy Container Scan`
    - `Checkov IaC Scan`
    - `Gitleaks Secrets`
  - Require branches up-to-date before merge (rebase ou squash)
  - Block force pushes para main
  - Require signed commits (opcional — configurar se time adotar GPG)

### R19.3 — PR Security Checks
- [ ] PR template (`.github/pull_request_template.md`) inclui:
  - Checklist de segurança: "Rodei SAST localmente?"
  - "Verifiquei que não committei secrets?"
  - "Adicionei testes para mudanças de segurança?"
- [ ] GitHub Actions bot comenta no PR com resumo de security scans
- [ ] Alertas de segurança bloqueiam merge até resolved ou waived

---

## 20. Security Documentation

### R20.1 — Security Runbook
- [ ] `docs/security-runbook.md` documenta:
  - Como rodar SAST localmente (`semgrep`, `codeql`)
  - Como interpretar alerts no GitHub Security Tab
  - Processo de waiver para falsos positivos
  - Quem aprovar exceptions (security owner do time)
  - Frequência de review de security dashboard (semanal)

### R20.2 — Contributing Guidelines
- [ ] `CONTRIBUTING.md` inclui seção de segurança:
  - "Antes de submeter um PR, rode: `gitleaks`, `semgrep`, `dotnet test`"
  - "Não commite `.env` files ou credenciais"
  - "Reporte vulnerabilidades via Security Tab → New security advisory"
  - "Policy de versão: seguimos semver, CVE tracking via Dependabot"

### R20.3 — Threat Model Document
- [ ] `docs/threat-model.md` documenta:
  - Assets críticos: Keycloak realm, PostgreSQL data, JWT signing keys
  - Attack vectors: SSRF, XSS, SQLi, credential stuffing, supply chain
  - Mitigações implementadas por vetor
  - Riscos residuais aceitos (ex: ROPC grant)
  - Revisão do threat model: a cada 6 meses ou mudanças de auth

---

## Requirement Traceability — v4.0

| Requirement | Phase | Status |
|-------------|-------|--------|
| CI-01 — Parallel Build Jobs | Phase 22 | 📋 Planned |
| CI-02 — Backend Build + Test | Phase 22 | 📋 Planned |
| CI-03 — Frontend Build Validation | Phase 22 | 📋 Planned |
| CI-04 — Pipeline Caching | Phase 22 | 📋 Planned |
| SEC-01 — Semgrep SAST | Phase 23 | 📋 Planned |
| SEC-02 — CodeQL SAST | Phase 23 | 📋 Planned |
| SEC-03 — SAST Policy Enforcement | Phase 23 | 📋 Planned |
| SEC-04 — Dependabot SCA | Phase 24 | 📋 Planned |
| SEC-05 — Trivy Dependency Scan | Phase 24 | 📋 Planned |
| SEC-06 — Trivy Container Scan | Phase 25 | 📋 Planned |
| SEC-07 — Dockle Best Practices | Phase 25 | 📋 Planned |
| SEC-08 — Checkov IaC Scan | Phase 26 | 📋 Planned |
| SEC-09 — Kubescape Setup | Phase 26 | 📋 Planned |
| SEC-10 — Gitleaks Secrets | Phase 27 | 📋 Planned |
| SEC-11 — TruffleHog Verification | Phase 27 | 📋 Planned |
| SEC-12 — Secrets Incident Response | Phase 27 | 📋 Planned |
| SEC-13 — GitHub Security Tab | Phase 28 | 📋 Planned |
| SEC-14 — Branch Protection Rules | Phase 28 | 📋 Planned |
| SEC-15 — Security Documentation | Phase 28 | 📋 Planned |

---

## v4.0 — Deferred

### D4.1 — Kubernetes Manifests + Scanning
- **Razão**: Projeto ainda roda em Docker Compose local; K8s é futuro (v5.0+)
- **Quando adicionar**: Quando migrar para orquestração (Kubernetes, ECS, Cloud Run)

### D4.2 — DAST (Dynamic Application Security Testing)
- **Razão**: OWASP ZAP ou Burp requer aplicação rodando; adiciona complexidade ao CI
- **Quando adicionar**: Quando aplicação estiver em staging environment acessível

### D4.3 — Signed Commits (GPG/SSH)
- **Razão**: Overhead de setup para contribuidores; não bloqueia delivery imediato
- **Quando adicionar**: Quando política de segurança do time exigir provenance de código

---

*Requirements document is living — update as requirements are clarified, validated, or delegated to future milestones.*
