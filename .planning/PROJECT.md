# Onboarding de Clientes

## What This Is

Sistema de onboarding para cadastro de clientes Pessoa Física (PF) e Pessoa Jurídica (PJ). O usuário se cadastra com dados básicos e senha, é direcionado ao login, e após autenticação visualiza seus dados cadastrais em modo leitura. A segurança é prioridade — Keycloak hardened, infraestrutura containerizada.

## Core Value

Cadastro seguro e funcional de clientes PF/PJ com autenticação robusta via Keycloak — se a segurança falhar, nada mais importa.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Cadastro de Pessoa Física (nome, CPF, email, telefone, senha)
- [ ] Cadastro de Pessoa Jurídica (razão social, CNPJ, email, telefone, senha)
- [ ] Criação de usuário no Keycloak via Admin API durante o cadastro
- [ ] Tela de login custom com autenticação via Keycloak
- [ ] Redirecionamento pós-cadastro para tela de login
- [ ] Tela de perfil com dados cadastrais (read-only) após primeiro login
- [ ] Keycloak hardened contra vulnerabilidades conhecidas (SSRF, open redirect, brute force, etc.)
- [ ] Docker Compose com toda a infraestrutura (API, frontend, PostgreSQL, Keycloak)
- [ ] Backend com TDD, DDD, SOLID, sem Minimal API
- [ ] Serilog + OpenTelemetry para logs, traces e metrics
- [ ] Frontend com Atomic Design (componentes atômicos)

### Out of Scope

- Validação de email no cadastro — não necessário no v1
- OAuth social login (Google, GitHub, etc.) — complexidade adicional sem valor para v1
- Edição de dados cadastrais — v1 é somente leitura
- Dashboard/área administrativa — fora do escopo inicial
- Mobile app — web-first
- Notificações push/email — sem necessidade no v1

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
*Last updated: 2026-04-01 after initialization*
