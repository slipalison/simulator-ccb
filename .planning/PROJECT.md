# Onboarding de Clientes

## What This Is

Sistema de onboarding para cadastro exclusivo de Pessoas Jurídicas (PJ). O PJ é o usuário principal que gerencia funcionários (PF) da sua empresa — cadastra, bloqueia, reseta senha e define permissões. Isolamento total entre empresas: PJ não vê/edita funcionários de outra PJ. BackOffice mantém poder de auditoria e suporte global. Segurança é prioridade — Keycloak hardened, permissões via roles/groups nativos.

## Core Value

Cadastro seguro PJ com gestão de funcionários e permissões via Keycloak — isolamento entre empresas é requisito de primeira classe. Se a segurança falhar, nada mais importa.

## Previous Milestone: v3.0 Painel de Backoffice Admin ✅ COMPLETE

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

## Previous Milestone: v4.0 CI/CD Pipeline + Cybersecurity ✅ COMPLETE

**Goal:** Pipeline de integração contínua com builds paralelas (backend + 2 frontends) e esteira completa de segurança (SAST, SCA, containers, IaC, secrets).

**Result:** 100% complete — 8/8 phases, 20/20 plans. CI pipeline com 12 jobs operacional.

## Previous Milestone: v5.0 Auth Code Flow + Admins + Auditoria ✅ COMPLETE

**Goal:** Migrar backoffice para ACF+PKCE, criar admins com senha temporária, auditoria append-only, isolamento de realms Keycloak.

**Result:** 100% complete — 6/6 phases (29–34). Dois realms isolados (backoffice/client), auth PKCE funcionando, audit log operacional.

## Previous Milestone: v6.0 Gestão Completa de Administradores ✅ COMPLETE

**Goal:** Admin pode gerenciar outros admins com operações completas — listar com paginação/filtros, editar, resetar senha e desativar/reativar — com segurança e auditoria obrigatórias em cada operação.

**Result:** 100% complete — 2/2 phases, 5/5 plans.

## Current Milestone: v7.0 PJ-Only Onboarding + Gestão de Funcionários

**Goal:** Transformar cadastro misto PF/PJ em PJ-only, onde PJ é usuário principal que gerencia funcionários PF com grupos de acesso, aceite de termos e auditoria completa.

**Target features:**
- Cadastro exclusivamente PJ (PF removido do fluxo — base zerada)
- PJ cadastra funcionários PF vinculados à sua empresa
- PJ gerencia funcionários: cadastrar, bloquear, resetar senha
- Grupos de acesso via Keycloak nativo: Admin Empresa, Viewer, Dashboard
- Isolamento crítico: PJ não vê/edita funcionários de outra PJ
- Aceite de termos de uso obrigatório (texto mock)
- Auditoria de ações dos funcionários visível ao admin (PJ ou Admin Empresa)
- Dashboard mock com dados estáticos
- CI com 80% cobertura no GitHub Actions
- Reflete em API, frontend Client e frontend BackOffice

**Depends on**: Milestone v6.0 completo (Gestão Completa de Administradores)

## Requirements

### Validated

- [x] Keycloak hardened contra vulnerabilidades conhecidas (SSRF, open redirect, brute force, etc.) — Validated in Phase 02: keycloak-security-hardening
- [x] Domain layer com value objects Cpf, Cnpj, Email, PhoneNumber e aggregate Client com factory methods PF/PJ — Validated in Phase 03: backend-domain-layer
- [x] CQRS application layer — ICommandHandler/IQueryHandler, RegisterClientCommand + handler, DI wiring manual (sem MediatR) — Validated in Phase 03: backend-domain-layer
- [x] Backend com TDD, DDD, SOLID, sem Minimal API (parcial — domain + application layers) — Validated in Phase 03: backend-domain-layer

### Active

- [ ] Cadastro exclusivamente PJ — remoção completa do fluxo PF
- [ ] PJ é usuário principal que gerencia funcionários da sua empresa
- [ ] PJ cadastra funcionários PF vinculados à sua empresa
- [ ] PJ bloqueia/desbloqueia funcionários
- [ ] PJ reseta senha de funcionários
- [ ] Grupos de acesso: Admin Empresa, Viewer, Dashboard (via Keycloak roles/groups)
- [ ] Isolamento entre empresas — PJ não vê/edita dados de outra PJ
- [ ] Aceite de termos de uso obrigatório no cadastro (texto mock)
- [ ] Auditoria de ações dos funcionários visível ao admin
- [ ] Dashboard mock com dados estáticos
- [ ] CI GitHub Actions com 80% cobertura
- [ ] BackOffice mantém poder de auditar/suportar qualquer empresa

### Out of Scope

- Validação de email no cadastro — não necessário no v1
- OAuth social login (Google, GitHub, etc.) — complexidade adicional sem valor para v1
- Mobile app — web-first
- Notificações push/email — sem necessidade no v1
- Bit Flags no JWT — Keycloak nativo (roles/groups) é a abordagem escolhida
- Dashboard real com dados dinâmicos — mock estático por enquanto
- Impersonação de funcionários por PJ — fora do escopo de segurança
- 2FA obrigatório para funcionários — requer configuração de realm separada

## Context

### Stack Definida

- **Backend**: C# .NET 10, Controllers (não Minimal API), DDD com TDD
- **Frontend**: React + Vinxi (meta-framework baseado em Vite)
- **Banco**: PostgreSQL
- **Auth**: Keycloak (self-hosted, hardened)
- **Infra**: Docker Compose (desenvolvimento local)
- **Observabilidade**: Serilog + OpenTelemetry (logs, traces, metrics)

### Fluxo Principal

1. PJ acessa tela de cadastro → preenche dados da empresa + senha + aceita termos de uso
2. API C# valida dados, persiste no PostgreSQL, cria user no Keycloak via Admin API
3. Redirecionamento para tela de login
4. PJ faz login → Token JWT retornado → redirecionamento para Dashboard
5. PJ pode cadastrar funcionários PF para sua empresa
6. PJ pode gerenciar funcionários: bloquear, resetar senha, editar permissões
7. Funcionário PF faz login → vê apenas suas telas conforme permissões
8. Admin da empresa + PJ dono podem auditar ações dos funcionários

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
| Keycloak nativo (roles/groups) para permissões | Bit Flags no JWT rejeitado — Keycloak já suporta roles/groups nativo, sem custom mapper | — Pending |
| Cadastro PJ-only | Remoção completa do fluxo PF — base zerada via docker compose down -v | — Pending |
| Grupos de acesso: Admin Empresa, Viewer, Dashboard | Admin Empresa = mesmos poderes PJ; Viewer = ver sem editar; Dashboard = acesso ao dashboard | — Pending |
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
*Last updated: 2026-04-25 — Milestone v7.0 started: PJ-Only Onboarding + Gestão de Funcionários*
