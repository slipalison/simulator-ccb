# Onboarding de Clientes

## What This Is

Sistema de onboarding para cadastro de clientes Pessoa Física (PF) e Pessoa Jurídica (PJ). O usuário se cadastra com dados básicos e senha, é direcionado ao login, e após autenticação visualiza seus dados cadastrais em modo leitura. A segurança é prioridade — Keycloak hardened, infraestrutura containerizada.

## Core Value

Cadastro seguro e funcional de clientes PF/PJ com autenticação robusta via Keycloak — se a segurança falhar, nada mais importa.

## Current Milestone: v3.0 Painel de Backoffice Admin ✅ COMPLETE

**Goal:** Painel administrativo para gerenciar cadastros de usuários — listar, visualizar, editar, bloquear/desbloquear e excluir (LGPD) com autenticação baseada em cookies httpOnly e autorização por role "admin".

**Target features:**
- Endpoints admin na API .NET (listar, detalhes, editar, bloquear, excluir usuários)
- Proteção por role "admin" nos endpoints admin
- Frontend Vinxi para backoffice com autenticação via cookies httpOnly
- Listagem paginada com busca, filtros por status, colunas configuráveis
- Detalhes do usuário, edição com formulário validado
- Bloqueio/desbloqueio com dialog de confirmação
- Exclusão LGPD com confirmação forte (digitar email)
- Middleware de proteção: exige sessão válida + role "admin"
- Refresh automático de access token transparente ao usuário
- Header com nome do admin logado + logout
- Tratamento global de erros (401, 403, 5xx) com toasts
- Server Components para leitura, Client Components para interatividade

**Result:** 100% complete — 5/5 phases, 13/14 plans (E2E phase removed by user decision)

### Stack Adicional para v3.0

- **Frontend Admin**: Vinxi (mesma stack do projeto atual, não Next.js)
- **UI Library**: shadcn/ui (já utilizada no projeto)
- **Auth**: Cookies httpOnly gerenciados pelo Vinxi via Server Actions
- **Token decoding**: jose (Edge Runtime compatible) para middleware
- **Notifications**: Sonner para toasts

**Depends on**: Milestone v2.0 completo (15 phases delivered)

## Current Milestone: v4.0 CI/CD Pipeline + Cybersecurity

**Goal:** Pipeline de integração contínua com builds paralelas (backend + 2 frontends) e esteira completa de segurança (SAST, SCA, containers, IaC, secrets).

**Target features:**
- GitHub Actions workflow com execução paralela por projeto
  - Backend: build + testes unitários + validação de cobertura
  - Frontend client: build + lint + type check
  - Frontend backoffice: build + lint + type check
- Validação de cobertura de testes (threshold mínimo)
- Pipeline de segurança automatizada no CI:
  - **SAST**: Semgrep (rápido, regras C#, customizável) + CodeQL (profundo, nativo GitHub)
  - **SCA**: Dependabot (zero config) + Trivy (dependências + containers + IaC + secrets)
  - **Container**: Trivy (imagem Docker) + Dockle (boas práticas Dockerfile)
  - **IaC**: Checkov (Terraform, K8s, Docker Compose) + Kubescape (Kubernetes)
  - **Secrets**: Gitleaks (credenciais commitadas) + TruffleHog (verificação ativa)
- Relatórios de segurança visíveis no GitHub Security Tab
- Bloqueio de merge em falha crítica de segurança
- Documentação de segurança para contribuidores

**Depends on**: Milestone v3.0 completo (Admin Backoffice + Frontend Separation)

## Requirements

### Validated

- [x] Keycloak hardened contra vulnerabilidades conhecidas (SSRF, open redirect, brute force, etc.) — Validated in Phase 02: keycloak-security-hardening
- [x] Domain layer com value objects Cpf, Cnpj, Email, PhoneNumber e aggregate Client com factory methods PF/PJ — Validated in Phase 03: backend-domain-layer
- [x] CQRS application layer — ICommandHandler/IQueryHandler, RegisterClientCommand + handler, DI wiring manual (sem MediatR) — Validated in Phase 03: backend-domain-layer
- [x] Backend com TDD, DDD, SOLID, sem Minimal API (parcial — domain + application layers) — Validated in Phase 03: backend-domain-layer

### Active

- [ ] GitHub Actions workflow com jobs paralelos (backend, frontend client, frontend backoffice)
- [ ] Build + testes unitários + cobertura para backend .NET 10
- [ ] Build + lint + type check para frontend client (Vinxi)
- [ ] Build + lint + type check para frontend backoffice (Vinxi)
- [ ] SAST: Semgrep + CodeQL configurados para C# e React
- [ ] SCA: Dependabot + Trivy escaneando dependências
- [ ] Container scanning: Trivy + Dockle nas imagens Docker
- [ ] IaC scanning: Checkov + Kubescape para compose.yaml e futuros K8s manifests
- [ ] Secrets scanning: Gitleaks + TruffleHog bloqueando credenciais commitadas
- [ ] Relatórios de segurança no GitHub Security Tab
- [ ] Policy de branch protection bloqueando merge em falha crítica
- [ ] Documentação de segurança para contribuidores

### Out of Scope

- Validação de email no cadastro — não necessário no v1
- OAuth social login (Google, GitHub, etc.) — complexidade adicional sem valor para v1
- Edição de dados cadastrais pelo usuário final — v1 é somente leitura
- Mobile app — web-first
- Notificações push/email — sem necessidade no v1
- Migração de ROPC para Auth Code + PKCE — documentado para v4

## Context

### Stack Definida

- **Backend**: C# .NET 10, Controllers (não Minimal API), DDD com TDD
- **Frontend**: React + Vinxi (meta-framework baseado em Vite)
- **Banco**: PostgreSQL
- **Auth**: Keycloak (self-hosted, hardened)
- **Infra**: Docker Compose (desenvolvimento local)
- **Observabilidade**: Serilog + OpenTelemetry (logs, traces, metrics)

### Fluxo Principal

1. Usuário acessa tela de cadastro → escolhe PF ou PJ
2. Preenche dados básicos + senha
3. API C# valida dados, persiste no PostgreSQL, cria user no Keycloak via Admin API
4. Redirecionamento para tela de login
5. Usuário faz login (tela custom → autenticação Keycloak)
6. Token JWT retornado → redirecionamento para tela de perfil
7. Tela de perfil exibe dados cadastrais (read-only)

### Segurança — Prioridade

Referência de vulnerabilidades Keycloak: superfícies de ataque incluem SSRF, open redirect, brute force, session fixation, header injection. O hardening do Keycloak é requisito de primeira classe, não uma tarefa secundária.

### Decisão Consciente — Login Custom

O login usa tela custom no React autenticando via Keycloak (Resource Owner Password Credentials Grant). Esta abordagem é deprecated no OAuth2.1 e menos segura que o Authorization Code Flow com PKCE. O usuário optou por esta abordagem sabendo do tradeoff. Deve ser revisitado se requisitos de segurança aumentarem.

### Princípios de Design

- **Backend**: DDD (Domain-Driven Design), TDD (Test-Driven Development), SOLID, DRY, YAGNI, KISS
- **Frontend**: Atomic Design — átomos, moléculas, organismos, templates, páginas
- **Sem Minimal API**: usar Controllers padrão do ASP.NET

## Constraints

- **Tech Stack**: .NET 10 + React/Vinxi + PostgreSQL + Keycloak — stack definida pelo usuário
- **Infra**: Tudo deve rodar em Docker Compose localmente
- **Segurança**: Keycloak deve ser hardened contra vulnerabilidades documentadas
- **API Style**: Controllers ASP.NET (sem Minimal API)
- **Observabilidade**: Serilog + OpenTelemetry obrigatórios desde o início

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Login custom (ROPC Grant) | Usuário quer controle total da UI de login | ⚠️ Revisit — deprecated no OAuth2.1 |
| Formulário de cadastro custom | Cadastro via Admin API do Keycloak — maior controle do fluxo | — Pending |
| Sem validação de email no v1 | Simplificar fluxo inicial — cadastrou, já pode logar | — Pending |
| Atomic Design no frontend | Facilitar mudanças futuras de layout com componentes reutilizáveis | — Pending |
| Sem MediatR — CQRS manual via DI | MediatR não é mais open source (licença comercial). Handlers injetados direto via DI nativo do .NET | ✓ Good |
| Backoffice usa mesma stack Vinxi | Manter consistência de stack, não introduzir Next.js para admin | ✓ v3.0 |
| Endpoints admin precisam ser criados | API atual só tem registro/login/me — CRUD admin não existe | ✓ v3.0 |
| Role "admin" para autorização | Keycloak roles em `realm_access.roles` — frontend confia no backend para segurança real | ✓ v3.0 |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd:transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-04-10 — Milestone v4.0 started: CI/CD Pipeline + Cybersecurity (parallel builds, SAST, SCA, container/IaC/secrets scanning)*
