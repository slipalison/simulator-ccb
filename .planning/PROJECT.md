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

## Previous Milestone: v7.0 PJ-Only Onboarding + Gestão de Funcionários ✅ COMPLETE

**Goal:** Transformar cadastro misto PF/PJ em PJ-only, onde PJ é usuário principal que gerencia funcionários PF com grupos de acesso, aceite de termos e auditoria completa.

**Result:** 100% complete — 8/8 phases, 19/19 plans. Todos gaps de integração resolvidos. Custom Access Groups CRUD implementado.

## Current Milestone: v8.0 Gestão de Fundos

**Goal:** Adicionar módulo de cadastros de fundos de investimento ao sistema existente — consultorias, custodiantes, fundos, cedentes e tipos de ativo — com isolamento multi-tenant obrigatório e administração no backoffice.

**Target features:**
- CRUD ConsultoriaFundo (gestora/consultoria) com CNPJ validado
- CRUD Custodiante (instituição financeira custodiante) com CNPJ validado
- CRUD Fundo (fundo de investimento) com state machine de status e referências a consultoria/custodiante
- CRUD Cedente (PF/PJ que cede créditos) com CPF/CNPJ validado
- CRUD TipoAtivo (catálogo global CVM)
- Relacionamentos N-N: Fundo↔Cedente (com limites de exposição), Cedente↔TipoAtivo, Fundo↔TipoAtivo
- Permissões de fundos integradas ao sistema de access groups existente
- Frontend client para gestão de fundos
- Frontend backoffice para visualização administrativa com audit trail

**Key decisions:**
- D-01: ConsultoriaFundo/Custodiante/Cedente são company-scoped (têm ClienteId, HasQueryFilter) — cada empresa cadastra os seus
- D-02: FundoStatus = state machine: RASCUNHO→ATIVO↔SUSPENSO→EM_LIQUIDACAO→ENCERRADO
- D-03: TipoAtivo é global (sem ClienteId) — catálogo CVM compartilhado por todas as empresas
- D-04: LimiteExposicao ilimitado = sentinel value (-1) — simples, explícito, evita nullable confusion

**Depends on**: Milestone v7.0 completo

## Requirements

### Validated

- [x] Keycloak hardened contra vulnerabilidades conhecidas (SSRF, open redirect, brute force, etc.) — Validated in Phase 02
- [x] Domain layer com value objects Cpf, Cnpj, Email, PhoneNumber e aggregate Client com factory methods PF/PJ — Validated in Phase 03
- [x] CQRS application layer — ICommandHandler/IQueryHandler, RegisterClientCommand + handler, DI wiring manual (sem MediatR) — Validated in Phase 03
- [x] Backend com TDD, DDD, SOLID, sem Minimal API (parcial — domain + application layers) — Validated in Phase 03
- [x] Cadastro exclusivamente PJ — remoção completa do fluxo PF — Validated in Phase 37
- [x] PJ é usuário principal que gerencia funcionários da sua empresa — Validated in Phase 38
- [x] PJ cadastra funcionários PF vinculados à sua empresa — Validated in Phase 38
- [x] PJ bloqueia/desbloqueia funcionários — Validated in Phase 38
- [x] PJ reseta senha de funcionários — Validated in Phase 38
- [x] Grupos de acesso: Admin Empresa, Viewer, Dashboard (via Keycloak roles/groups) — Validated in Phase 39
- [x] Custom access groups CRUD (PERM-06) — Validated in Phase 44
- [x] Isolamento entre empresas — PJ não vê/edita dados de outra PJ — Validated in Phase 39
- [x] Aceite de termos de uso obrigatório no cadastro (texto mock) — Validated in Phase 40
- [x] Auditoria de ações dos funcionários visível ao admin — Validated in Phase 41
- [x] Dashboard mock com dados estáticos — Validated in Phase 40
- [x] CI GitHub Actions com 80% cobertura — Validated in Phase 42
- [x] BackOffice mantém poder de auditar/suportar qualquer empresa — Validated in Phase 41

### Active

- [ ] **CAD-01**: PJ can register ConsultoriaFundo with razao social, CNPJ validated, optional nome fantasia, email, telefone, status
- [ ] **CAD-02**: PJ can list ConsultoriaFundo with pagination (20/page) and search
- [ ] **CAD-03**: PJ can update ConsultoriaFundo fields
- [ ] **CAD-04**: Duplicate CNPJ for ConsultoriaFundo within same company returns 409
- [ ] **CAD-05**: PJ can register Custodiante with razao social, CNPJ validated, optional codigo interno, email,telefone, status
- [ ] **CAD-06**: PJ can list Custodiante with pagination and search
- [ ] **CAD-07**: PJ can update Custodiante fields
- [ ] **CAD-08**: Duplicate CNPJ for Custodiante within same company returns 409
- [ ] **CAD-09**: PJ can register Fundo with nome, CNPJ, ConsultoriaFundo, Custodiante, TipoFundo, optional classe/segmento/datas
- [ ] **CAD-10**: PJ can list Fundo with pagination and search
- [ ] **CAD-11**: PJ can update Fundo data
- [ ] **CAD-12**: Duplicate CNPJ for Fundo within same company returns 409
- [ ] **CAD-13**: Fundo status follows state machine (RASCUNHO→ATIVO↔SUSPENSO→EM_LIQUIDACAO→ENCERRADO)
- [ ] **CAD-14**: PJ can register Cedente PF with validated CPF
- [ ] **CAD-15**: PJ can register Cedente PJ with validated CNPJ
- [ ] **CAD-16**: PJ can list Cedente with pagination and search
- [ ] **CAD-17**: PJ can update Cedente data
- [ ] **CAD-18**: Duplicate CPF/CNPJ for Cedente within same company returns 409
- [ ] **CAD-19**: Admin can create TipoAtivo with unique codigo, descricao, categoria, status
- [ ] **CAD-20**: Admin can list TipoAtivo (global catalog)
- [ ] **CAD-21**: Admin can update TipoAtivo
- [ ] **CAD-22**: Duplicate codigo for TipoAtivo returns 409
- [ ] **REL-01**: PJ can associate Cedente to Fundo with exposure limits and date range
- [ ] **REL-02**: PJ can list Cedentes associated to a Fundo
- [ ] **REL-03**: PJ can update FundoCedente exposure limits, dates, status
- [ ] **REL-04**: PJ can associate Tipos de Ativo to a Cedente
- [ ] **REL-05**: PJ can list/remove Tipos de Ativo from a Cedente
- [ ] **REL-06**: PJ can associate Tipos de Ativo to a Fundo
- [ ] **REL-07**: PJ can list/remove Tipos de Ativo from a Fundo
- [ ] **REL-08**: LimiteExposicaoPercentual unlimited = sentinel value (-1)
- [ ] **REL-09**: FundoCedente enforces ONE active association per Fundo-Cedente pair
- [ ] **TEN-01**: Fundo/FundoCedente company-scoped with HasQueryFilter
- [ ] **TEN-02**: ConsultoriaFundo/Custodiante/Cedente company-scoped with HasQueryFilter
- [ ] **TEN-03**: TipoAtivo global (no ClienteId, no HasQueryFilter)
- [ ] **PERM-01**: Fund permissions (funds:read/write/delete/manage) added to Permissions.cs
- [ ] **PERM-02**: Fund CRUD endpoints require appropriate permission claims
- [ ] **PERM-03**: Existing access groups extended with fund permissions
- [ ] **ADM-01**: Backoffice admin can list Fundo across all companies
- [ ] **ADM-02**: Backoffice admin can view Fundo details
- [ ] **ADM-03**: Backoffice admin can list ConsultoriaFundo/Custodiante/Cedente across all companies
- [ ] **ADM-04**: All fund management actions logged to audit trail
- [ ] **FRO-01**: Client sidebar includes Fundos section
- [ ] **FRO-02**: FundosPage shows list with search, pagination, status badges
- [ ] **FRO-03**: Forms use Zod validation mirroring backend rules
- [ ] **FRO-04**: Backoffice fund views are read-only for auditing
- [ ] **FRO-05**: Fundo status dropdown restricted by state machine

### Out of Scope

- Validação de email no cadastro — não necessário no v1
- OAuth social login (Google, GitHub, etc.) — complexidade adicional sem valor para v1
- Mobile app — web-first
- Notificações push/email — sem necessidade no v1
- Bit Flags no JWT — Keycloak nativo (roles/groups) é a abordagem escolhida
- Dashboard real com dados dinâmicos — mock estático por enquanto (fundos será dinâmico em v8)
- Processamento financeiro — módulo é cadastral, sem movimentação
- Upload de documentos — complexidade alta, deferido
- Integração com APIs externas (CVM, BACEN) — deferido para v2+
- Soft delete — fundos usam status transitions, entidades auxiliares usam ATIVO/INATIVO
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
5. PJ pode gerenciar funcionários: bloquear, resetar senha, editar permissões
6. Funcionário PF faz login → vê apenas suas telas conforme permissões
7. Admin da empresa + PJ dono podem auditar ações dos funcionários
8. PJ pode cadastrar e gerenciar fundos de investimento, consultorias, custodiantes, cedentes e tipos de ativo
9. Fundos têm ciclo de status: rascunho → ativo ↔ suspenso → em liquidação → encerrado
10. Backoffice audita todas as ações de gestão de fundos跨越 empresas

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
| Keycloak nativo (roles/groups) para permissões | Bit Flags no JWT rejeitado — Keycloak já suporta roles/groups nativo, sem custom mapper | ✓ v7.0 |
| Cadastro PJ-only | Remoção completa do fluxo PF — base zerada via docker compose down -v | ✓ v7.0 |
| Grupos de acesso: Admin Empresa, Viewer, Dashboard | Admin Empresa = mesmos poderes PJ; Viewer = ver sem editar; Dashboard = acesso ao dashboard | ✓ v7.0 |
| Formulário de cadastro custom | Cadastro via Admin API do Keycloak — maior controle do fluxo | ✓ v7.0 |
| Sem validação de email no v1 | Simplificar fluxo inicial — cadastrou, já pode logar | ✓ v7.0 |
| Atomic Design no frontend | Facilitar mudanças futuras de layout com componentes reutilizáveis | ✓ v7.0 |
| Cedente/Custodiante company-scoped (D-01) | Cada empresa cadastra os seus — HasQueryFilter reforça isolamento | — v8.0 Pending |
| FundoStatus = state machine (D-02) | RASCUNHO→ATIVO↔SUSPENSO→EM_LIQUIDACAO→ENCERRADO — transições inválidas rejeitadas | — v8.0 Pending |
| TipoAtivo global (D-03) | Catálogo CVM — sem ClienteId, compartilhado entre empresas | — v8.0 Pending |
| LimiteExposicao sentinel -1 (D-04) | Valor -1 = ilimitado — evita nullable confusion | — v8.0 Pending |
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
*Last updated: 2026-05-02 — Milestone v8.0 Gestão de Fundos started*
