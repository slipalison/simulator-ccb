# Requirements — v1: Client Onboarding (PF/PJ + Keycloak Auth)

**Milestone:** v1.0 — Cadastro e Login com Perfil Read-Only
**Source:** FEATURES.md + SUMMARY.md + PROJECT.md
**Created:** 2026-04-09
**Status:** DRAFT — awaiting review

---

## Core Value

Cadastro seguro e funcional de clientes PF/PJ com autenticação robusta via Keycloak — se a segurança falhar, nada mais importa.

---

## Scope Summary

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
