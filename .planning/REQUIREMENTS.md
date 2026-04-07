# Requirements: Onboarding de Clientes

**Defined:** 2026-04-01
**Core Value:** Cadastro seguro e funcional de clientes PF/PJ com autenticação robusta via Keycloak

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Infraestrutura

- [x] **INFRA-01**: Docker Compose orquestra todos os serviços (API, frontend, PostgreSQL x2, Keycloak)
- [x] **INFRA-02**: PostgreSQL dedicado para dados da aplicação (app_db)
- [x] **INFRA-03**: PostgreSQL dedicado para Keycloak (keycloak_db) — isolado do app_db
- [x] **INFRA-04**: Healthchecks em todos os serviços com depends_on condition: service_healthy
- [x] **INFRA-05**: Keycloak realm "onboarding" configurado com clients, policies e roles

### Segurança

- [x] **SEC-01**: Keycloak brute force protection habilitado (max 5 falhas, 30s wait, escalating)
- [x] **SEC-02**: Password policy no Keycloak (min 8 chars, uppercase, lowercase, digit, special)
- [x] **SEC-03**: Redirect URIs exatas registradas nos clients Keycloak (sem wildcards)
- [x] **SEC-04**: SSRF protection — disable request_uri no Keycloak
- [x] **SEC-05**: Admin console Keycloak restrita (bind 127.0.0.1 em dev, bloqueada em prod)
- [x] **SEC-06**: HTTPS enforcement configurado no Keycloak (HTTP apenas em dev local)
- [x] **SEC-07**: Service account com least privilege (manage-users only) para Admin API
- [x] **SEC-08**: Erros genéricos em todas as respostas de autenticação (sem information leakage)
- [x] **SEC-09**: Log masking para dados sensíveis (senhas, tokens, secrets não aparecem nos logs)
- [ ] **SEC-10**: JWT armazenado em memória no frontend (nunca localStorage/sessionStorage)

### Cadastro

- [x] **REG-01**: Formulário de cadastro PF com campos: nome, CPF, email, telefone, senha
- [x] **REG-02**: Formulário de cadastro PJ com campos: razão social, CNPJ, email, telefone, senha
- [ ] **REG-03**: Validação server-side de CPF (algoritmo módulo 11)
- [ ] **REG-04**: Validação server-side de CNPJ (check-digit + formato alfanumérico 2026)
- [ ] **REG-05**: Detecção de duplicatas — CPF/CNPJ/email únicos antes de criar user
- [ ] **REG-06**: Criação de user no Keycloak via Admin API após persistência no app_db
- [x] **REG-07**: Redirecionamento pós-cadastro para tela de login
- [ ] **REG-08**: Idempotência no endpoint de registro (chave de idempotência para evitar double-submit)
- [x] **REG-09**: Validação client-side espelha server-side (UX convenience, não segurança)

### Autenticação

- [ ] **AUTH-01**: Tela de login custom no React com autenticação via Keycloak (ROPC grant)
- [ ] **AUTH-02**: Token JWT (access + refresh) retornado após login bem-sucedido
- [ ] **AUTH-03**: Rota /profile protegida — redireciona para login se não autenticado
- [ ] **AUTH-04**: Token refresh automático quando access_token próximo da expiração

### Perfil

- [ ] **PROF-01**: Tela de perfil exibe dados cadastrais do cliente (read-only)
- [ ] **PROF-02**: Dados carregados via GET /api/clients/me com Bearer JWT
- [ ] **PROF-03**: Diferenciação visual entre perfil PF e PJ

### Backend Design

- [x] **BACK-01**: Arquitetura DDD — Domain, Application, Infrastructure, API layers
- [x] **BACK-02**: Value objects: CPF, CNPJ, Email, Phone com auto-validação
- [x] **BACK-03**: Client aggregate com factory methods (RegisterPessoaFisica, RegisterPessoaJuridica)
- [x] **BACK-04**: TDD — testes unitários no domain, integração nos endpoints
- [ ] **BACK-05**: Controllers ASP.NET Core (sem Minimal API)
- [x] **BACK-06**: CQRS manual via DI (commands/handlers injetados diretamente, sem MediatR)

### Observabilidade

- [x] **OBS-01**: Serilog structured logging (JSON) com TraceId/SpanId automáticos
- [x] **OBS-02**: OpenTelemetry traces instrumentando ASP.NET Core, HttpClient, EF Core
- [x] **OBS-03**: OpenTelemetry metrics (runtime + ASP.NET Core)
- [x] **OBS-04**: Correlation ID propagado em chamadas ao Keycloak Admin API
- [x] **OBS-05**: Health check endpoints (/healthz) para API e Keycloak

### Frontend Design

- [ ] **FRONT-01**: Atomic Design — atoms, molecules, organisms, templates, pages
- [ ] **FRONT-02**: Vinxi configurado em SPA mode
- [ ] **FRONT-03**: TanStack Router para rotas type-safe
- [ ] **FRONT-04**: React Hook Form + Zod para validação de formulários
- [ ] **FRONT-05**: Tailwind CSS para estilização

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Validação de Email

- **VMAIL-01**: Envio de email de confirmação após cadastro
- **VMAIL-02**: Bloqueio de login até email verificado

### Social Login

- **SOCIAL-01**: Login via Google OAuth
- **SOCIAL-02**: Login via GitHub OAuth
- **SOCIAL-03**: Account linking com cadastro existente

### Edição de Perfil

- **EDIT-01**: Edição de dados cadastrais (nome, telefone, email)
- **EDIT-02**: Sincronização de alterações com Keycloak user attributes

### Admin Dashboard

- **ADMIN-01**: Dashboard para visualização de clientes cadastrados
- **ADMIN-02**: Busca e filtro de clientes

### Segurança Avançada

- **SECADV-01**: Migração de ROPC para Authorization Code Flow + PKCE
- **SECADV-02**: MFA (Multi-Factor Authentication)

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Validação de email no cadastro | Requer infraestrutura de email; sem valor no v1 |
| OAuth social login | Complexidade adicional; Keycloak permite retrofit fácil |
| Edição de dados cadastrais | v1 é somente leitura; evita problemas de concorrência |
| Dashboard administrativo | Keycloak admin console cobre gerenciamento de usuários |
| Mobile app / PWA | Web-first; React responsivo é suficiente |
| Notificações push/email | Sem workflow que necessite notificação no v1 |
| Forgot password custom | Keycloak oferece nativamente quando email infra existir |
| Authorization Code Flow + PKCE | Tradeoff consciente — ROPC escolhido para v1 |
| Offline tokens / remember me | Risco de segurança sem storage adequado no frontend |
| Real-time features (WebSocket) | Sem caso de uso no v1 |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01 | Phase 1 | Complete |
| INFRA-02 | Phase 1 | Complete |
| INFRA-03 | Phase 1 | Complete |
| INFRA-04 | Phase 1 | Complete |
| INFRA-05 | Phase 1 | Complete |
| SEC-01 | Phase 2 | Complete |
| SEC-02 | Phase 2 | Complete |
| SEC-03 | Phase 2 | Complete |
| SEC-04 | Phase 2 | Complete |
| SEC-05 | Phase 2 | Complete |
| SEC-06 | Phase 2 | Complete |
| SEC-07 | Phase 2 | Complete |
| SEC-08 | Phase 5 | Complete |
| SEC-09 | Phase 4 | Complete |
| SEC-10 | Phase 9 | Pending |
| REG-01 | Phase 8 | Complete |
| REG-02 | Phase 8 | Complete |
| REG-03 | Phase 5 | Pending |
| REG-04 | Phase 5 | Pending |
| REG-05 | Phase 5 | Pending |
| REG-06 | Phase 5 | Pending |
| REG-07 | Phase 8 | Complete |
| REG-08 | Phase 5 | Pending |
| REG-09 | Phase 8 | Complete |
| AUTH-01 | Phase 9 | Pending |
| AUTH-02 | Phase 6 | Pending |
| AUTH-03 | Phase 6 | Pending |
| AUTH-04 | Phase 6 | Pending |
| PROF-01 | Phase 10 | Pending |
| PROF-02 | Phase 10 | Pending |
| PROF-03 | Phase 10 | Pending |
| BACK-01 | Phase 3 | Complete |
| BACK-02 | Phase 3 | Complete |
| BACK-03 | Phase 3 | Complete |
| BACK-04 | Phase 3 | Complete |
| BACK-05 | Phase 5 | Pending |
| BACK-06 | Phase 3 | Complete |
| OBS-01 | Phase 4 | Complete |
| OBS-02 | Phase 4 | Complete |
| OBS-03 | Phase 4 | Complete |
| OBS-04 | Phase 4 | Complete |
| OBS-05 | Phase 4 | Complete |
| FRONT-01 | Phase 7 | Pending |
| FRONT-02 | Phase 7 | Pending |
| FRONT-03 | Phase 7 | Pending |
| FRONT-04 | Phase 7 | Pending |
| FRONT-05 | Phase 7 | Pending |

**Coverage:**
- v1 requirements: 46 total
- Mapped to phases: 46
- Unmapped: 0

---
*Requirements defined: 2026-04-01*
*Last updated: 2026-04-01 after roadmap creation*
