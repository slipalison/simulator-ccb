# Roadmap: Onboarding de Clientes

## Overview

This roadmap builds a secure PF/PJ client onboarding system from infrastructure up, then extends it with an admin backoffice panel for user management. The delivery sequence mirrors the dependency chain: Docker infrastructure first, then a hardened Keycloak, then the DDD backend domain, then observability wiring, then registration and authentication endpoints, then a frontend scaffold, and finally the three user-facing screens (registration, login, profile). After v1.0/v2.0 completion, milestone v3.0 adds admin CRUD endpoints, role-based access control, and a backoffice UI with pagination, filtering, and LGPD-compliant user deletion. Milestone v3.0 concludes with a **frontend separation** into two independent projects (client + backoffice). Milestone v4.0 adds a full CI/CD security pipeline. Milestone v5.0 migrates the backoffice to Auth Code Flow + PKCE, adds administrator management, and introduces an immutable audit log.

Every phase delivers a coherent, independently verifiable capability before the next begins.

**⚠️ ARCHITECTURAL CONSTRAINT (Phase 21+):** Two separate frontend projects (`frontend/client` and `frontend/backoffice`) must remain fully independent — no shared code, no cross-imports, separate builds and deploys.

---

## Milestones

| Milestone | Name | Phases | Status |
|-----------|------|--------|--------|
| **v1.0** | Foundation — Cadastro e Login com Perfil Read-Only | 1-10 | ✅ Complete |
| **v2.0** | UX/UI Redesign + Production Readiness | 11-15 | ✅ Complete |
| **v3.0** | Admin Backoffice + Frontend Separation | 16-20 | ✅ Complete (5/5 phases, 13 plans, E2E removed) |
| **v4.0** | CI/CD Pipeline + Cybersecurity | 21-28 | ✅ Complete (8/8 phases, 20/20 plans) |
| **v5.0** | Auth Code Flow (Backoffice) + Gestão de Admins + Auditoria | 29-34 | ✅ Complete (6/6 phases) |
| **v6.0** | Gestão Completa de Administradores | 35-36 | ✅ Complete |

**Phase order rationale (v3.0):** 16→17→18 (admin backend+UI core) → **19 (separation)** → 20 (edit/delete in separated project). E2E phase removed by user decision.

---

## Milestone v1.0 — Foundation (Phases 1-10)

**Goal:** Sistema de onboarding funcional com cadastro PF/PJ, autenticação Keycloak, perfil read-only, observabilidade completa e stack Dockerizada.

### Phases

- [x] **Phase 1: Infrastructure** - Docker Compose with dual PostgreSQL, Keycloak realm configured and running (completed 2026-04-01)
- [x] **Phase 2: Keycloak Security Hardening** - Keycloak hardened against all documented attack surfaces (completed 2026-04-02)
- [x] **Phase 3: Backend Domain Layer** - DDD domain model with value objects, aggregate, and full test coverage (completed 2026-04-02)
- [x] **Phase 4: Observability** - Serilog + OpenTelemetry wired across all services with correlation ID propagation (completed 2026-04-03)
- [x] **Phase 5: Registration API** - Backend endpoints for PF/PJ registration with full validation and Keycloak user creation (completed 2026-04-05)
- [x] **Phase 6: Authentication API** - JWT issuance, token refresh, and protected route enforcement in the backend (completed 2026-04-06)
- [x] **Phase 7: Frontend Foundation** - Vinxi SPA scaffold with Atomic Design structure, routing, and form primitives (completed 2026-04-07)
- [x] **Phase 8: Registration UI** - PF/PJ registration forms integrated with the API, including client-side validation and post-registration redirect (completed 2026-04-07)
- [x] **Phase 9: Login UI** - Custom login screen with ROPC token exchange and in-memory JWT storage (completed 2026-04-07)
- [x] **Phase 10: Profile UI** - Read-only profile screen displaying PF/PJ data via authenticated API call (completed 2026-04-08)

**Requirements Mapped:** INFRA-01 to INFRA-05, SEC-01 to SEC-09, BACK-01 to BACK-06, OBS-01 to OBS-05, REG-01 to REG-09, AUTH-01 to AUTH-04, PROF-01 to PROF-03, FRONT-01 to FRONT-05

**Success Criteria (Achieved):**
1. ✅ Running `docker compose up` starts all services healthy with no manual intervention
2. ✅ Keycloak hardened: brute force protection, password policy, SSRF prevention, exact redirect URIs
3. ✅ DDD domain model with Cpf, Cnpj, Email, PhoneNumber value objects and Client aggregate
4. ✅ Serilog structured logging + OpenTelemetry traces + Grafana LGTM stack operational
5. ✅ PF/PJ registration persists to PostgreSQL, creates Keycloak user, detects duplicates (409)
6. ✅ JWT auth with ROPC grant, token refresh, protected routes, memory-only storage (SEC-10)
7. ✅ Frontend boots with Atomic Design, TanStack Router, RHF + Zod validation
8. ✅ Registration forms with inline validation, auto-login post-registration, redirect to /login
9. ✅ Login screen with ROPC token exchange, generic error messages (no account enumeration)
10. ✅ Profile displays PF/PJ data read-only, visually distinct (badges), auth guard redirect

---

## Milestone v2.0 — UX/UI Redesign + Production Readiness (Phases 11-15)

**Goal:** Transformar o sistema de "funcional mas cru" em "profissional, seguro e pronto para produção".

### Phases

- [x] **Phase 11: UX Redesign** - Unified registration form with password UX, login-first navigation, auto-login post-registration, and forgot password flow (completed 2026-04-08)
- [x] **Phase 12: UI Redesign** - shadcn/ui adoption, dark/light theme, complete visual redesign of all screens (Login, Registration, Profile, Forgot/Reset Password) (completed 2026-04-08)
- [x] **Phase 13: Reset Password Fix** - Configurable frontend URL in reset email, end-to-end forgot/reset/login flow working (completed 2026-04-08)
- [ ] **Phase 14: E2E Testing** - Playwright installation, E2E tests for registration, login, profile, and reset password flows
- [x] **Phase 15: Production Cleanup** - Cookie Secure flag configuration, dead code removal, test suite fixes (completed 2026-04-09)

**Requirements Mapped:** UX-01 to UX-07, UI-01 to UI-07, E2E-01 to E2E-05, PROD-01 to PROD-05

**Success Criteria (Achieved except Phase 14):**
1. ✅ Single registration form with dynamic PF/PJ fields (radio button), no separate type selection
2. ✅ Password strength meter (5 levels), show/hide toggle, confirm password validation
3. ✅ Root `/` shows LoginPage for unauthenticated, auto-redirects to `/profile` for authenticated
4. ✅ Auto-login after registration — no intermediate login screen
5. ✅ Forgot password sends reset email via Resend.com (15min expiry), reset updates Keycloak password
6. ✅ shadcn/ui adopted across all screens, dark/light theme with localStorage persistence
7. ✅ Reset password link uses configurable `Frontend:BaseUrl` (not hardcoded localhost:3001)
8. ✅ Cookie Secure flag environment-configured, dead code removed, all tests passing
9. ⏳ **Phase 14 pending:** Playwright installed, E2E tests for registration → auto-login → profile, login → profile → F5 → session restored, direct /profile → redirect /login

---

## Milestone v3.0 — Admin Backoffice Panel (Phases 16-20)

**Goal:** Painel administrativo **SEPARADO** para gerenciar cadastros de usuários — listar, visualizar, editar, bloquear/desbloquear e excluir (LGPD) com autenticação baseada em cookies httpOnly e autorização por role "admin".

**⚠️ DECISÃO DE ARQUITETURA — NÃO VIOLAR:**
> O sistema deve ter **DOIS projetos frontend independentes**:
> - `frontend/client` — Frontend do cliente final (cadastro, login, perfil)
> - `frontend/backoffice` — Frontend do backoffice administrativo (gestão de usuários)
>
> **Motivo:** Isolamento total — mudanças em um projeto não podem impactar o outro. Cada frontend tem seu próprio ciclo de deploy, dependências, builds e autenticação.
>
> **Regra:** Nenhum arquivo de código pode ser compartilhado entre os dois frontends. Componentes reutilizáveis devem ser duplicados, não importados cruzadamente.

**Depends on:** Milestone v1.0 + v2.0 complete (Phase 14 E2E Testing can be deferred)

### Phase 16: Admin API Endpoints
**Goal:** Backend CRUD endpoints for user management with role-based authorization
**Depends on:** Phase 5 (Registration API), Phase 6 (Authentication API)
**Requirements:** ADMIN-01, ADMIN-02, ADMIN-03, ADMIN-04, ADMIN-05
**Success Criteria** (what must be TRUE):
  1. GET `/api/admin/users` returns paginated list of users with search and status filters
  2. GET `/api/admin/users/{id}` returns detailed user data (PF or PJ) including Keycloak status
  3. PUT `/api/admin/users/{id}` updates user data with full server-side validation
  4. POST `/api/admin/users/{id}/block` and POST `/api/admin/users/{id}/unblock` toggle user active status in Keycloak
  5. DELETE `/api/admin/users/{id}` performs LGPD-compliant deletion (anonymize data + delete Keycloak user)
  6. All admin endpoints require `[Authorize(Roles = "admin")]` — non-admin users receive 403 Forbidden
**Plans:** 3 plans
Plans:
- [ ] 16-01-PLAN.md — Admin DTOs, paginated query models, FluentValidation for update/block/delete
- [ ] 16-02-PLAN.md — AdminUserController with GET/PUT/POST/DELETE, CQRS handlers, Keycloak Admin API integration
- [ ] 16-03-PLAN.md — Role-based authorization middleware, Keycloak "admin" role mapping, 403 handling

### Phase 17: Admin Auth & Session Management
**Goal:** HttpOnly cookie-based authentication for backoffice with transparent token refresh
**Depends on:** Phase 16, Phase 6
**Requirements:** ADMIN-06, ADMIN-07, ADMIN-08
**Success Criteria** (what must be TRUE):
  1. ✅ Admin login uses httpOnly, Secure, SameSite=Strict cookies — no JWT in localStorage
  2. ✅ Access token refresh is transparent — middleware intercepts 401, refreshes, retries original request
  3. ✅ Session expiration redirects admin to login with toast notification
  4. ✅ Admin header displays logged-in admin name + logout button
  5. ✅ Global error handling: 401 → login redirect, 403 → access denied page, 5xx → toast error
**Plans:** 2 plans
Plans:
- [x] 17-01-PLAN.md — Cookie auth middleware, httpOnly cookie setup, admin session storage (completed 2026-04-09)
- [x] 17-02-PLAN.md — Transparent token refresh interceptor, session restoration on page load, error handling middleware (completed 2026-04-09)

### Phase 18: Admin Backoffice UI — List & Details
**Goal:** Paginated user listing with search, filters, and detail view
**Depends on:** Phase 17
**Requirements:** ADMIN-09, ADMIN-10, ADMIN-11
**Success Criteria** (what must be TRUE):
  1. `/admin/users` shows paginated table (20 per page) with name, document, email, status, actions
  2. Search bar filters by name, CPF/CNPJ, or email in real-time (debounced 300ms)
  3. Status filter dropdown: All, Active, Blocked, Deleted
  4. Clicking a user opens `/admin/users/{id}` with full PF/PJ data in read-only mode
  5. Loading skeleton states shown during API calls, error states with retry button
**Plans:** 2 plans
Plans:
- [x] 18-01-PLAN.md — Admin users listing table with pagination, search, filters (58 tests) — COMPLETE 2026-04-09
- [x] 18-02-PLAN.md — User detail page with PF/PJ data display, Keycloak status badge (32 tests) — COMPLETE 2026-04-09

### Phase 19: Frontend Separation — Client vs Backoffice ✅ COMPLETE
**Goal:** Reestruturação do frontend monolítico em dois projetos independentes com builds, deploys e autenticação isolados
**Depends on:** Phase 15 (Production Cleanup), Phase 18 (Admin UI List & Details)
**Requirements:** ARCH-01, ARCH-02, ARCH-03 ✅ ALL MET
**Status:** COMPLETE — 2026-04-09
**Results:**
  - `frontend/client/` — 119 tests passing, builds successfully, port 5173
  - `frontend/backoffice/` — 103 tests passing, builds successfully, port 5174
  - Zero cross-imports between projects (ARCH-03 verified)
  - compose.yaml updated with frontend-client + frontend-backoffice services
  - Monolith `frontend/` deleted — only client/ and backoffice/ remain
**Success Criteria** (what must be TRUE):
  1. Pasta `frontend/client` contém apenas telas do usuário final (login, registro, perfil, forgot/reset password)
  2. Pasta `frontend/backoffice` contém apenas telas administrativas (admin login, users list, user detail)
  3. Cada projeto tem seu próprio `package.json`, `app.config.ts`, `Dockerfile`, `tsconfig.json`
  4. `docker compose.yaml` tem dois serviços separados: `frontend-client` e `frontend-backoffice` com portas diferentes
  5. Nenhum import cruzado entre os projetos — código duplicado é aceitável, import compartilhado é proibido
  6. Ambos frontends buildam e rodem independentemente com `docker compose up`
  7. Testes unitários de ambos projetos passam independentemente
**Plans:** 2 plans
Plans:
- [x] 19-01-PLAN.md — Estrutura `frontend/client` com migração de componentes não-admin, Dockerfile separado, compose.yaml atualizado
- [x] 19-02-PLAN.md — Estrutura `frontend/backoffice` com migração de componentes admin, remoção de rotas `/admin` do client, testes de ambos

### Phase 20: Admin Backoffice UI — Edit, Block, Delete (in separated backoffice project) ✅ COMPLETE
**Goal:** Edit user form, block/unblock dialog, LGPD-compliant deletion with strong confirmation — built directly in the separated backoffice project
**Depends on:** Phase 18 (Admin UI List & Details), Phase 19 (Frontend Separation)
**Requirements:** ADMIN-12, ADMIN-13, ADMIN-14 ✅ ALL MET
**Status:** COMPLETE — 2026-04-10
**Results:**
  - Plan 01: EditUserForm + BlockDialog/UnblockDialog — 131 tests, 13 tasks
  - Plan 02: DeleteDialog (LGPD) — 156 tests total, 8 tasks
  - Zero cross-imports, both frontends build successfully
**Success Criteria** (what must be TRUE):
  1. ✅ Edit form validates all fields client-side (Zod) and server-side (FluentValidation) before submission
  2. ✅ Block/unblock uses confirmation dialog with reason field — action logs to audit trail
  3. ✅ LGPD deletion requires typing user email to confirm — anonymizes PostgreSQL data + deletes Keycloak user
  4. ✅ Success/error toasts after each action — table refreshes automatically
  5. ✅ Optimistic UI updates for block/unblock — reverts on API error
**Plans:** 2 plans
Plans:
- [x] 20-01-PLAN.md — Edit user form with Zod validation, block/unblock dialog with reason, API integration (COMPLETE 2026-04-10)
- [x] 20-02-PLAN.md — LGPD deletion flow (email confirmation dialog), anonymization handler, audit logging (COMPLETE 2026-04-10)

---

## Milestone v4.0 — CI/CD Pipeline + Cybersecurity (Phases 21-28)

**Goal:** Pipeline de integração contínua com builds paralelas (backend + 2 frontends) e esteira completa de segurança (SAST, SCA, containers, IaC, secrets).

**Depends on:** Milestone v3.0 complete (Admin Backoffice + Frontend Separation)

### Phase 21: CI/CD Pipeline Foundation
**Goal:** GitHub Actions workflow com jobs paralelos para backend, frontend-client e frontend-backoffice
**Depends on:** Phase 19 (Frontend Separation)
**Requirements:** R13.1, R13.2, R13.3, R13.4
**Success Criteria** (what must be TRUE):
  1. `.github/workflows/ci.yml` trigger em push para main e PRs
  2. 3 jobs rodam em paralelo: `backend` (.NET 10 build + tests), `frontend-client` (Vinxi build + lint + type check), `frontend-backoffice` (Vinxi build + lint + type check)
  3. Cache configurado: `~/.nuget/packages` para .NET, `node_modules/.cache` para frontend
  4. Backend job falha se cobertura < 80% (`dotnet test /p:CollectCoverage=true`)
  5. Frontend jobs falham se `eslint --max-warnings 0` ou `tsc --noEmit` falharem
  6. Cada job é independente — falha em um não bloqueia execução dos outros
**Plans:** 3 plans
Plans:
- [ ] 21-01-PLAN.md — GitHub Actions workflow scaffold, .NET 10 build + test job, caching strategy
- [ ] 21-02-PLAN.md — Frontend client + backoffice jobs, Vinxi build, ESLint, TypeScript validation
- [ ] 21-03-PLAN.md — Coverage threshold enforcement, cache optimization, independent job failure handling

### Phase 22: SAST — Static Application Security Testing
**Goal:** Semgrep + CodeQL configurados para C# e React com política de bloqueio em alertas críticos
**Depends on:** Phase 21
**Requirements:** R14.1, R14.2, R14.3
**Success Criteria** (what must be TRUE):
  1. `.semgrep/` directory com rules customizadas para C# e React (localStorage tokens, CSRF, hardcoded credentials, CPF/CNPJ validation)
  2. `semgrep ci --config auto` roda em PRs e falha em regras ERROR
  3. CodeQL database init para C# (`dotnet`) e JavaScript/React
  4. CodeQL detecta: SQL Injection, XSS (`dangerouslySetInnerHTML`), insecure deserialization, path traversal
  5. Resultados visíveis em GitHub Security Tab → Code scanning alerts
  6. Branch protection exige CodeQL passing antes de merge
**Plans:** 3 plans
Plans:
- [ ] 22-01-PLAN.md — Semgrep configuration, custom rules for C#/React, CI integration
- [ ] 22-02-PLAN.md — CodeQL database init, C# + JavaScript queries, SARIF export
- [ ] 22-03-PLAN.md — SAST policy enforcement, branch protection rules, alert dashboard setup

### Phase 23: SCA — Software Composition Analysis
**Goal:** Dependabot + Trivy escaneando dependências e vulnerabilidades em pacotes de terceiros
**Depends on:** Phase 21
**Requirements:** R15.1, R15.2, R15.3
**Success Criteria** (what must be TRUE):
  1. `.github/dependabot.yml` configurado para `nuget`, `npm`, `docker`, `github-actions`
  2. Dependabot abre PRs automáticos weekly para updates de dependências
  3. Auto-merge habilitado para patches e minors com CI passing
  4. `trivy fs --scanners vuln .` detecta vulnerabilidades em package-lock.json e *.csproj
  5. Trivy falha se encontrar CVEs CRITICAL ou HIGH sem fix disponível
  6. Reports exportados em SARIF → GitHub Security Tab
**Plans:** 2 plans
Plans:
- [ ] 23-01-PLAN.md — Dependabot configuration, auto-merge policy, weekly update schedule
- [ ] 23-02-PLAN.md — Trivy filesystem scanning, SARIF export, vulnerability threshold enforcement

### Phase 24: Container Security Scanning
**Goal:** Trivy + Dockle verificando segurança de imagens Docker em cada build
**Depends on:** Phase 21
**Requirements:** R16.1, R16.2, R16.3
**Success Criteria** (what must be TRUE):
  1. CI step roda `trivy image --severity HIGH,CRITICAL onboarding-api:ci` após build da imagem
  2. Scanning aplicado a: backend API, frontend-client, frontend-backoffice, Keycloak
  3. Trivy falha se encontrar CVEs CRITICAL ou HIGH na imagem
  4. `dockle onboarding-api:ci` verifica boas práticas: não root, sem `latest` tag, `.dockerignore`, healthcheck
  5. Dockle falha em checks FATAL ou WARN
  6. Image tags seguem semver: `onboarding-api:1.2.3`, `sha-{commit}` para builds
**Plans:** 2 plans
Plans:
- [ ] 24-01-PLAN.md — Trivy image scanning, multi-image CI pipeline, SARIF report
- [ ] 24-02-PLAN.md — Dockle best practices check, image tagging policy, registry push gating

### Phase 25: IaC Scanning — Infrastructure as Code
**Goal:** Checkov + Kubescape verificando segurança de Docker Compose e preparação para Kubernetes
**Depends on:** Phase 21
**Requirements:** R17.1, R17.2, R17.3
**Success Criteria** (what must be TRUE):
  1. `checkov --framework dockerfile --file compose.yaml` verifica: sem `privileged: true`, volumes restritos, sem secrets em ENV
  2. Checkov falha em checks CRITICAL ou HIGH
  3. Kubescape instalado no CI (setup-only no v4.0, scanning real quando K8s manifests existirem)
  4. `docs/iac-policies.md` documenta regras de segurança para Docker Compose e futuros K8s manifests
  5. Reports exportados em SARIF → GitHub Security Tab
**Plans:** 2 plans
Plans:
- [ ] 25-01-PLAN.md — Checkov Docker Compose scanning, policy configuration, SARIF export
- [ ] 25-02-PLAN.md — Kubescape setup (K8s preparation), IaC policy documentation

### Phase 26: Secrets Detection
**Goal:** Gitleaks + TruffleHog bloqueando credenciais commitadas com processo de resposta a incidentes
**Depends on:** Phase 21
**Requirements:** R18.1, R18.2, R18.3
**Success Criteria** (what must be TRUE):
  1. `.gitleaks.toml` configurado para detectar: AWS/Azure/GCP keys, JWT signing keys, DB connection strings, Keycloak secrets
  2. Pre-commit hook local roda `gitleaks detect` antes de commit
  3. CI step roda `gitleaks` em PRs e falha se detectar qualquer secret
  4. `trufflehog filesystem --directory . --only-verified` verifica credenciais ativas em git history
  5. TruffleHog falha se encontrar credencial verificada como ativa
  6. `docs/secrets-incident-response.md` documenta processo de revogação e rotação de chaves
**Plans:** 2 plans
Plans:
- [ ] 26-01-PLAN.md — Gitleaks pre-commit + CI, custom rules, allowlist configuration
- [ ] 26-02-PLAN.md — TruffleHog active verification, SARIF export, secrets incident response documentation

### Phase 27: GitHub Security Integration
**Goal:** Security Tab dashboard, branch protection rules, PR security checks
**Depends on:** Phase 22, Phase 23, Phase 24, Phase 25, Phase 26
**Requirements:** R19.1, R19.2, R19.3
**Success Criteria** (what must be TRUE):
  1. GitHub Security Tab exibe dashboard com: Dependabot alerts, Code scanning alerts (SAST), secret scanning alerts, container vulnerabilities
  2. Todos os reports em SARIF format (Semgrep, CodeQL, Trivy, Checkov, Gitleaks, TruffleHog)
  3. Branch `main` protegida: PR reviews (min 1), status checks passing (9 checks), block force pushes
  4. `.github/pull_request_template.md` inclui checklist de segurança
  5. GitHub Actions bot comenta resumo de security scans em PRs
  6. Alertas CRITICAL/HIGH bloqueiam merge até resolved ou waived
**Plans:** 2 plans
Plans:
- [ ] 27-01-PLAN.md — Security Tab dashboard setup, SARIF aggregation, trend monitoring
- [ ] 27-02-PLAN.md — Branch protection rules, PR template, merge blocking for critical alerts

### Phase 28: Security Documentation + Hardening
**Goal:** Documentação completa de segurança, threat model, contributing guidelines, runbooks
**Depends on:** Phase 22, Phase 23, Phase 24, Phase 25, Phase 26, Phase 27
**Requirements:** R20.1, R20.2, R20.3
**Success Criteria** (what must be TRUE):
  1. `docs/security-runbook.md` documenta: como rodar SAST localmente, interpretar alerts, processo de waiver, security owner
  2. `CONTRIBUTING.md` inclui seção de segurança: pré-PR checks, no-commit rules, vulnerability reporting
  3. `docs/threat-model.md` documenta: assets críticos, attack vectors, mitigações, riscos residuais (ex: ROPC grant)
  4. Threat model revisado: aprovado por security owner, revisão agendada (6 meses)
  5. Documentação linkada no README principal do projeto
**Plans:** 2 plans
Plans:
- [ ] 28-01-PLAN.md — Security runbook, contributing guidelines, PR security checklist
- [ ] 28-02-PLAN.md — Threat model documentation, risk register, review schedule

---

## Milestone Summary

| Milestone | Phases | Plans | Status | Requirements |
|-----------|--------|-------|--------|--------------|
| **v1.0** Foundation | 1-10 | 30 | ✅ Complete | 35 requirements |
| **v2.0** UX/UI + Production | 11-15 | 7+ | ✅ Complete | 14 requirements |
| **v3.0** Admin Backoffice | 16-20 | 13 | ✅ Complete | 22 requirements |
| **v4.0** CI/CD + Security | 21-28 | 20 | ✅ Complete | 25 requirements |
| **v5.0** Auth Code Flow + Admins + Audit | 29-34 | TBD | ✅ Complete | 11 requirements |
| **v6.0** Gestão Completa de Administradores | 35-36 | 5 | ✅ Complete | 14 requirements |
| **Total** | **36 phases** | **81+ plans** | **6 milestones done** | **135 requirements** |

---

## Phase Details

### Phase 1: Infrastructure
**Goal**: The full stack can boot from a single `docker compose up` with all services healthy and isolated
**Depends on**: Nothing (first phase)
**Requirements**: INFRA-01, INFRA-02, INFRA-03, INFRA-04, INFRA-05
**Success Criteria** (what must be TRUE):
  1. Running `docker compose up` starts all services (API, frontend, PostgreSQL app_db, PostgreSQL keycloak_db, Keycloak) with no manual intervention
  2. Healthchecks pass for every service and dependent services wait for healthy upstream before starting
  3. app_db and keycloak_db are separate containers with no shared volumes or network namespaces
  4. Keycloak realm "onboarding" exists with the required clients, policies, and roles after first boot
**Plans**: 3 plans
Plans:
- [x] 01-PLAN-01.md — Repo skeleton, compose.yaml with dual PostgreSQL, secret management
- [x] 01-PLAN-02.md — Keycloak realm JSON with clients, brute force, password policy
- [x] 01-PLAN-03.md — .NET solution scaffold, Vinxi frontend, full stack smoke test

### Phase 2: Keycloak Security Hardening
**Goal**: Keycloak is hardened against all documented attack surfaces before any user data flows through it
**Depends on**: Phase 1
**Requirements**: SEC-01, SEC-02, SEC-03, SEC-04, SEC-05, SEC-06, SEC-07
**Success Criteria** (what must be TRUE):
  1. Brute force protection is active: after 5 failed login attempts the account is locked for at least 30 seconds with escalating wait times
  2. Password policy rejects passwords shorter than 8 characters or missing uppercase, lowercase, digit, or special character
  3. Keycloak admin console is inaccessible from any IP except 127.0.0.1 in the dev environment
  4. The `request_uri` parameter is disabled and no redirect URI accepts wildcards — only exact URIs registered
  5. The service account used for Admin API access holds only the `manage-users` role and no broader permissions
**Plans**: 1 plan
Plans:
- [x] 02-01-PLAN.md — Acceptance test suite, realm JSON hardening (exact redirectUri + clientPolicies), request_uri SPI flag, clean-boot verification

### Phase 3: Backend Domain Layer
**Goal**: The core business rules live in a rich, fully-tested domain model that has no dependency on infrastructure
**Depends on**: Phase 1
**Requirements**: BACK-01, BACK-02, BACK-03, BACK-04, BACK-06
**Success Criteria** (what must be TRUE):
  1. The domain project compiles and all unit tests pass with no database or network dependencies
  2. CPF and CNPJ value objects reject invalid inputs (wrong check digit, wrong format) and accept valid ones
  3. The Client aggregate can be created via `RegisterPessoaFisica` and `RegisterPessoaJuridica` factory methods and enforces its own invariants
  4. A CQRS command for registration exists with a corresponding handler wired via direct DI (no MediatR)
  5. DDD layer boundaries are enforced: Domain references nothing outside itself; Application references only Domain
**Plans**: 2 plans
Plans:
- [x] 03-01-PLAN.md — Test project scaffold, domain value objects (Cpf, Cnpj, Email, PhoneNumber), Client aggregate (TDD RED→GREEN)
- [x] 03-02-PLAN.md — CQRS interfaces, RegisterClientCommand, handler, DI wiring (TDD RED→GREEN)

### Phase 4: Observability
**Goal**: Every request flowing through the system produces structured logs, distributed traces, and metrics with full correlation
**Depends on**: Phase 3
**Requirements**: OBS-01, OBS-02, OBS-03, OBS-04, OBS-05, SEC-09
**Success Criteria** (what must be TRUE):
  1. Every HTTP request generates a structured JSON log entry with TraceId and SpanId fields
  2. A distributed trace spans from the ASP.NET Core request through EF Core queries and HttpClient calls to Keycloak
  3. Runtime and ASP.NET Core metrics are exported via OpenTelemetry
  4. A Correlation ID is injected into every outbound call to the Keycloak Admin API and appears in the corresponding log entries
  5. Passwords, tokens, and secrets never appear in any log output — masked at the sink level
**Plans**: 4 plans
Plans:
- [x] 04-00-PLAN.md — Test scaffold: Onboarding.API.Tests project with stub tests for observability behaviors
- [x] 04-01-PLAN.md — Serilog + OpenTelemetry SDK wiring in Program.cs, SensitiveDataDestructuringPolicy (SEC-09)
- [x] 04-02-PLAN.md — Health check endpoints /healthz/live and /healthz/ready, compose.yaml healthcheck fix
- [x] 04-03-PLAN.md — Grafana LGTM stack (Alloy, Loki, Tempo, Mimir, Grafana) in compose.yaml

### Phase 5: Registration API
**Goal**: Clients can be registered via the API with full server-side validation, duplicate detection, persistence, and Keycloak user creation
**Depends on**: Phase 4
**Requirements**: REG-03, REG-04, REG-05, REG-06, REG-08, BACK-05, SEC-08
**Success Criteria** (what must be TRUE):
  1. POSTing a valid PF payload to the registration endpoint persists the client in app_db and creates the corresponding user in Keycloak
  2. POSTing a valid PJ payload does the same for a Pessoa Jurídica client (CNPJ validates against both the current and the July-2026 alphanumeric format)
  3. Submitting a duplicate CPF, CNPJ, or email returns an error without creating any record in either database
  4. Submitting an invalid CPF or CNPJ (bad check digit) returns a 422 with a descriptive error — no information about existing users is leaked
  5. Submitting the same request twice with the same idempotency key produces exactly one record — the second call returns the cached 201 response
  6. All authentication-related error responses use generic messages that do not reveal whether a user exists
**Plans**: 4 plans
Plans:
- [x] 05-01-PLAN.md — Wave 0 TDD stubs (20 RED stubs across 4 test files for all Phase 5 requirements)
- [x] 05-02-PLAN.md — Infrastructure layer: AppDbContext + ClientRepository + KeycloakUserService + AddInfrastructure()
- [x] 05-03-PLAN.md — Handler (duplicate check + Keycloak integration + compensation) + RegistrationController + FluentValidation + Program.cs wiring
- [x] 05-04-PLAN.md — IdempotencyFilter + all 20 stubs GREEN + RegistrationIntegrationTests with Testcontainers

### Phase 6: Authentication API
**Goal**: The backend can issue JWT tokens, protect routes, and silently refresh expiring access tokens
**Depends on**: Phase 5
**Requirements**: AUTH-02, AUTH-03, AUTH-04
**Success Criteria** (what must be TRUE):
  1. A valid login credential pair exchanges for an access token and a refresh token returned in the API response
  2. Calling GET /api/clients/me without a Bearer token returns 401 and redirects to login
  3. When the access token is near expiry the backend (or frontend token logic) uses the refresh token to obtain a new access token without re-prompting the user
**Plans**: 3 plans
Plans:
- [x] 06-01-PLAN.md — Wave 0 TDD stubs RED: FakeJwtTokenHelper, AuthTestApiFactory, 12 stubs para AUTH-02/03/04
- [x] 06-02-PLAN.md — Contratos: IKeycloakTokenService, TokenResponse, GetByEmailAsync, AddJwtBearer em Program.cs
- [x] 06-03-PLAN.md — Implementação: KeycloakTokenService, AuthController, ClientsController, handlers CQRS, stubs GREEN

### Phase 7: Frontend Foundation
**Goal**: The frontend application boots in SPA mode with a working Atomic Design component tree, type-safe routing, and form infrastructure
**Depends on**: Phase 1
**Requirements**: FRONT-01, FRONT-02, FRONT-03, FRONT-04, FRONT-05
**Success Criteria** (what must be TRUE):
  1. Running `docker compose up` serves the frontend and navigating to the root URL loads the application without errors
  2. The component directory is structured into atoms, molecules, organisms, templates, and pages with at least one example component at each level
  3. TanStack Router routes are type-safe — navigating to an unknown path shows a typed 404 component
  4. A form built with React Hook Form + Zod shows inline validation errors when a field fails schema validation before submission
**Plans**: 4 plans
Plans:
- [x] 07-00-PLAN.md — TDD stubs RED: vitest config, 4 arquivos de stubs para todos os critérios da phase
- [x] 07-01-PLAN.md — Scaffold Vinxi SPA: @vitejs/plugin-react + Tailwind v4 + shadcn/ui init + alias @/*
- [x] 07-02-PLAN.md — Atomic Design (6 componentes) + TanStack Router com notFoundComponent
- [x] 07-03-PLAN.md — ExampleForm com RHF + Zod + erros inline; todos os stubs GREEN

### Phase 8: Registration UI
**Goal**: Users can complete PF or PJ registration through the frontend, see client-side validation feedback, and land on the login screen after submitting
**Depends on**: Phase 7, Phase 5
**Requirements**: REG-01, REG-02, REG-07, REG-09
**Success Criteria** (what must be TRUE):
  1. Navigating to the registration page shows a choice between Pessoa Física and Pessoa Jurídica, each leading to the correct form
  2. Submitting a PF form with an invalid CPF format shows an inline error before the request is sent
  3. Submitting a PJ form with a missing required field shows an inline error before the request is sent
  4. Completing a valid registration submits the form to the API and, on success, redirects the user to the login screen
**Plans**: 3 plans
Plans:
- [x] 08-01-PLAN.md — Registration entry point: /registration route, PF/PJ type selector, placeholders
- [x] 08-02-PLAN.md — PF and PJ registration forms: Zod schemas, RHF + inline validation, check-digit
- [x] 08-03-PLAN.md — API integration: registerClient, error handling, success redirect to /login

### Phase 9: Login UI
**Goal**: Users can log in through the custom React login screen and the resulting JWT is held in memory, never persisted to browser storage
**Depends on**: Phase 8, Phase 6
**Requirements**: AUTH-01, SEC-10
**Success Criteria** (what must be TRUE):
  1. The login screen accepts email and password, submits credentials to Keycloak via ROPC grant, and on success navigates to the profile screen
  2. After a successful login, inspecting localStorage and sessionStorage shows no JWT tokens — the token lives only in React state
  3. After 5 failed login attempts the login screen displays a generic "invalid credentials" error and the account is locked (brute force protection visible end-to-end)
**Plans**: 3 plans
Plans:
- [x] 09-01-PLAN.md — Login schema, API client (loginClient, refreshToken), AuthContext (memory-only tokens), LoginForm molecule
- [x] 09-02-PLAN.md — LoginPage wired with form + auth + redirect, ProfilePage placeholder with AuthGuard, routes + AuthProvider
- [x] 09-03-PLAN.md — Tests: auth-context (memory storage), login-flow (form → API → redirect), profile guard

### Phase 10: Profile UI
**Goal**: An authenticated user can see their own registration data in read-only mode, with visual distinction between PF and PJ profiles
**Depends on**: Phase 9, Phase 6
**Requirements**: PROF-01, PROF-02, PROF-03
**Success Criteria** (what must be TRUE):
  1. After logging in, the user is taken to a profile screen that displays their cadastral data (name/razão social, document, email, phone) in read-only form
  2. The profile data is loaded by GET /api/clients/me with the Bearer JWT — no data is embedded in the route or hardcoded
  3. A PF profile and a PJ profile are visually distinct (different labels, different document field displayed)
  4. Navigating directly to /profile without a token redirects to the login screen
**Plans**: 3 plans
Plans:
- [x] 10-01-PLAN.md — ProfilePage, ProfileCard, ProfileField atoms, API client
- [x] 10-02-PLAN.md — PF/PJ visual differentiation, loading states, error handling
- [x] 10-03-PLAN.md — Tests: profile-flow, profile-card, auth guard redirect

### Phase 11: UX Redesign
**Goal**: Unified registration experience with password UX, login-first navigation, auto-login post-registration, and forgot password flow
**Depends on**: Phase 10, Phase 6
**Requirements**: UX-01, UX-02, UX-03, UX-04, UX-05, UX-06
**Success Criteria** (what must be TRUE):
  1. Registration is completed in a single form with dynamic PF/PJ fields (radio button) — no separate type selection screen
  2. Password field includes a visual strength meter (5 levels) and show/hide toggle
  3. Confirm password field blocks submission if passwords don't match
  4. The root URL `/` shows LoginPage for unauthenticated users, auto-redirects to `/profile` for authenticated users
  5. After successful registration, user is automatically logged in and redirected to profile (no intermediate login screen)
  6. Forgot password flow sends reset email via Resend.com with time-limited token (15min expiry)
  7. Reset password updates Keycloak user password via Admin API
**Plans**: 2 plans
Plans:
- [x] 11-01-PLAN.md — Unified registration form, password strength meter, show/hide, confirm password, login-first navigation, auto-login
- [x] 11-02-PLAN.md — Forgot/reset password flow with Resend.com integration

### Phase 12: UI Redesign
**Goal**: Professional, polished UI with shadcn/ui components and dark/light theme support
**Depends on**: Phase 11
**Requirements**: UI-01, UI-02, UI-03, UI-04, UI-05, UI-06, UI-07
**Success Criteria** (what must be TRUE):
  1. shadcn/ui is set up with components.json and all required components installed (button, input, card, form, dialog, toast, etc.)
  2. Dark/light theme toggle persists user preference via localStorage with system default detection
  3. LoginPage, RegistrationPage, ProfilePage redesigned with shadcn/ui components
  4. Fixed header with logo, theme toggle, and user menu (logout)
  5. All forms use shadcn Form + Input + Label with inline Zod validation errors
**Plans**: 3 plans
Plans:
- [x] 12-01-PLAN.md — shadcn/ui setup, theme infrastructure, Tailwind CSS variables for light/dark
- [x] 12-02-PLAN.md — LoginPage + RegistrationPage redesign with shadcn components
- [x] 12-03-PLAN.md — ProfilePage + Header redesign, user menu, theme toggle

### Phase 13: Reset Password Fix
**Goal:** Configurable frontend URL in reset email, end-to-end forgot/reset/login flow working
**Depends on**: Phase 11 (UX Redesign)
**Gap Closure:** P0-01 from v2.0 audit — reset link hardcoded to localhost:3001, frontend runs on :5173
**Success Criteria** (what must be TRUE):
  1. `Frontend:BaseUrl` configuration exists (environment variable or appsettings)
  2. Reset email contains configurable URL: `{Frontend:BaseUrl}/reset-password?token=...`
  3. Clicking reset link navigates to working reset password page on port 5173
  4. Full flow tested: forgot → email received → reset → login
**Plans**: 1 plan
Plans:
- [x] 13-01-PLAN.md — Configurable Frontend:BaseUrl, update ForgotPasswordCommand, test

### Phase 14: E2E Testing
**Goal:** Playwright installed, E2E tests for critical user flows
**Depends on**: Phase 12 (UI Redesign)
**Gap Closure:** P0-02 from v2.0 audit — zero E2E test coverage
**Success Criteria** (what must be TRUE):
  1. `@playwright/test` installed and configured in frontend project
  2. E2E test: Registration → Auto-login → Profile (PF and PJ)
  3. E2E test: Login → Profile → F5 → Session restored
  4. E2E test: Direct /profile access → redirect to /login
  5. E2E test: Forgot password → reset email → reset password → login
  6. All E2E tests pass with `npx playwright test`
**Plans**: 1 plan
Plans:
- [ ] 14-01-PLAN.md — Playwright install, config, 5 E2E flow tests

### Phase 15: Production Cleanup
**Goal:** Cookie Secure flag configuration, dead code removal, test suite fixes
**Depends on**: Phase 12 (UI Redesign)
**Gap Closure:** P1-01, P1-02, + tech debt from v2.0 audit
**Success Criteria** (what must be TRUE):
  1. Cookie `Secure` flag is environment-configured (true in production, false in dev)
  2. Orphan file `frontend/src/client.tsx` deleted
  3. Dead code `LabeledField.tsx` deleted
  4. HealthCheckEndpointTests fixed (4 failures → 0)
  5. Stale TDD comments removed from test files
  6. All backend tests passing (no failures)
**Plans**: 1 plan
Plans:
- [x] 15-01-PLAN.md — Cookie config, cleanup, test fixes

### Phase 16: Admin API Endpoints
**Goal:** Backend CRUD endpoints for user management with role-based authorization
**Depends on:** Phase 5 (Registration API), Phase 6 (Authentication API)
**Requirements:** ADMIN-01, ADMIN-02, ADMIN-03, ADMIN-04, ADMIN-05
**Success Criteria** (what must be TRUE):
  1. GET `/api/admin/users` returns paginated list of users with search and status filters
  2. GET `/api/admin/users/{id}` returns detailed user data (PF or PJ) including Keycloak status
  3. PUT `/api/admin/users/{id}` updates user data with full server-side validation
  4. POST `/api/admin/users/{id}/block` and POST `/api/admin/users/{id}/unblock` toggle user active status in Keycloak
  5. DELETE `/api/admin/users/{id}` performs LGPD-compliant deletion (anonymize data + delete Keycloak user)
  6. All admin endpoints require `[Authorize(Roles = "admin")]` — non-admin users receive 403 Forbidden
**Plans:** 3 plans
Plans:
- [ ] 16-01-PLAN.md — Admin DTOs, paginated query models, FluentValidation for update/block/delete
- [ ] 16-02-PLAN.md — AdminUserController with GET/PUT/POST/DELETE, CQRS handlers, Keycloak Admin API integration
- [ ] 16-03-PLAN.md — Role-based authorization middleware, Keycloak "admin" role mapping, 403 handling

### Phase 17: Admin Auth & Session Management
**Goal:** HttpOnly cookie-based authentication for backoffice with transparent token refresh
**Depends on:** Phase 16, Phase 6
**Requirements:** ADMIN-06, ADMIN-07, ADMIN-08
**Success Criteria** (what must be TRUE):
  1. ✅ Admin login uses httpOnly, Secure, SameSite=Strict cookies — no JWT in localStorage
  2. ✅ Access token refresh is transparent — middleware intercepts 401, refreshes, retries original request
  3. ✅ Session expiration redirects admin to login with toast notification
  4. ✅ Admin header displays logged-in admin name + logout button
  5. ✅ Global error handling: 401 → login redirect, 403 → access denied page, 5xx → toast error
**Plans:** 2 plans
Plans:
- [x] 17-01-PLAN.md — Cookie auth middleware, httpOnly cookie setup, admin session storage (completed 2026-04-09)
- [x] 17-02-PLAN.md — Transparent token refresh interceptor, session restoration on page load, error handling middleware (completed 2026-04-09)

### Phase 18: Admin Backoffice UI — List & Details
**Goal:** Paginated user listing with search, filters, and detail view
**Depends on:** Phase 17
**Requirements:** ADMIN-09, ADMIN-10, ADMIN-11
**Success Criteria** (what must be TRUE):
  1. `/admin/users` shows paginated table (20 per page) with name, document, email, status, actions
  2. Search bar filters by name, CPF/CNPJ, or email in real-time (debounced 300ms)
  3. Status filter dropdown: All, Active, Blocked, Deleted
  4. Clicking a user opens `/admin/users/{id}` with full PF/PJ data in read-only mode
  5. Loading skeleton states shown during API calls, error states with retry button
**Plans:** 2 plans
Plans:
- [x] 18-01-PLAN.md — Admin users listing table with pagination, search, filters (58 tests) — COMPLETE 2026-04-09
- [x] 18-02-PLAN.md — User detail page with PF/PJ data display, Keycloak status badge (32 tests) — COMPLETE 2026-04-09

### Phase 19: Admin Backoffice UI — Edit, Block, Delete
**Goal:** Edit user form, block/unblock dialog, LGPD-compliant deletion with strong confirmation
**Depends on:** Phase 18
**Requirements:** ADMIN-12, ADMIN-13, ADMIN-14
**Success Criteria** (what must be TRUE):
  1. Edit form validates all fields client-side (Zod) and server-side (FluentValidation) before submission
  2. Block/unblock uses confirmation dialog with reason field — action logs to audit trail
  3. LGPD deletion requires typing user email to confirm — anonymizes PostgreSQL data + deletes Keycloak user
  4. Success/error toasts after each action — table refreshes automatically
  5. Optimistic UI updates for block/unblock — reverts on API error
**Plans:** 2 plans
Plans:
- [ ] 19-01-PLAN.md — Edit user form with Zod validation, block/unblock dialog with reason, API integration
- [ ] 19-02-PLAN.md — LGPD deletion flow (email confirmation dialog), anonymization handler, audit logging

### Phase 20: Admin E2E Testing & Production Readiness
**Goal:** Playwright E2E tests for admin flows, production config, documentation
**Depends on:** Phase 19, Phase 14 (E2E Testing from v2.0)
**Requirements:** ADMIN-15, ADMIN-16, E2E-06, E2E-07
**Success Criteria** (what must be TRUE):
  1. E2E test: Admin login → list users → search → filter by status → view details
  2. E2E test: Admin edits user → validation errors → successful update → toast confirmation
  3. E2E test: Admin blocks user → confirmation dialog → user blocked → table refreshes
  4. E2E test: Admin deletes user (LGPD) → types email to confirm → user anonymized + Keycloak deleted
  5. E2E test: Non-admin user accessing `/admin` receives 403 access denied page
  6. All E2E tests pass with `npx playwright test`
  7. Production documentation updated: deployment guide, admin role setup, backup procedures
**Plans:** 2 plans
Plans:
- [ ] 20-01-PLAN.md — Playwright E2E tests for admin flows (list, edit, block, delete, 403 handling)
- [ ] 20-02-PLAN.md — Production documentation, deployment guide, admin role provisioning in Keycloak

---

## Progress

**Execution Order:**
Phases execute in numeric order within each milestone. Cross-milestone dependencies must be satisfied first.

```
v1.0:  1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10
v2.0:  11 → 12 → 13 → 14 → 15
v3.0:  16 → 17 → 18 → 19 → 20
```

**Milestone v1.0 — Foundation (Complete)**

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Infrastructure | 3/3 | ✅ Complete | 2026-04-01 |
| 2. Keycloak Security Hardening | 1/1 | ✅ Complete | 2026-04-02 |
| 3. Backend Domain Layer | 2/2 | ✅ Complete | 2026-04-02 |
| 4. Observability | 4/4 | ✅ Complete | 2026-04-03 |
| 5. Registration API | 4/4 | ✅ Complete | 2026-04-05 |
| 6. Authentication API | 3/3 | ✅ Complete | 2026-04-06 |
| 7. Frontend Foundation | 4/4 | ✅ Complete | 2026-04-07 |
| 8. Registration UI | 3/3 | ✅ Complete | 2026-04-07 |
| 9. Login UI | 3/3 | ✅ Complete | 2026-04-07 |
| 10. Profile UI | 3/3 | ✅ Complete | 2026-04-08 |

**Milestone v2.0 — UX/UI Redesign (Complete except E2E)**

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 11. UX Redesign | 2/2 | ✅ Complete | 2026-04-08 |
| 12. UI Redesign | 3/3 | ✅ Complete | 2026-04-08 |
| 13. Reset Password Fix | 1/1 | ✅ Complete | 2026-04-08 |
| 14. E2E Testing | 0/1 | ⏳ Pending | — |
| 15. Production Cleanup | 1/1 | ✅ Complete | 2026-04-09 |

**Milestone v3.0 — Admin Backoffice (Defining Requirements)**

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 16. Admin API Endpoints | 0/3 | 📋 Planned | — |
| 17. Admin Auth & Session | 0/2 | 📋 Planned | — |
| 18. Admin List & Details | 0/2 | 📋 Planned | — |
| 19. Admin Edit, Block, Delete | 0/2 | 📋 Planned | — |
| 20. Admin E2E & Production | 0/2 | 📋 Planned | — |

---

---

## Milestone v5.0 — Auth Code Flow (Backoffice) + Gestão de Admins + Auditoria (Phases 29-32)

**Goal:** Migrar o backoffice de ROPC para Authorization Code Flow + PKCE (confidential client, code exchange server-side via Vinxi), adicionar criação e listagem de administradores, e introduzir um audit log imutável de todas as ações administrativas.

**Key decisions:**
- A NEW Keycloak client `onboarding-backoffice` (confidential, standardFlowEnabled) replaces the ROPC login flow in the backoffice
- The existing `AdminAuthController` ROPC endpoints are retired in favor of Auth Code Flow
- Tokens stored server-side in httpOnly cookies managed by Vinxi — no client-side token exposure
- Audit log is append-only (no UPDATE/DELETE on the AuditLog table) — requires new EF Core migration
- Frontend client keeps ROPC + custom UI (conscious decision — UX prioritized, not in scope for v5.0)

**Depends on:** Milestone v4.0 complete (CI/CD Pipeline + Cybersecurity)

**Phase order rationale:** 29 (Keycloak config + backend ACF infrastructure) → 30 (audit backend + admin management backend) → 31 (backoffice ACF frontend UI) → 32 (backoffice admin management UI + audit log UI).
Backend-first ordering ensures API contracts exist before UI is built.

### Phases

- [ ] **Phase 29: Keycloak Config + Auth Code Flow Backend** - New Keycloak client provisioned, .NET backend handles code exchange and cookie issuance (ACF-01, ACF-02, ACF-03, ACF-04)
- [x] **Phase 30: Audit Log Backend + Admin Management Backend** - AuditLog entity + migration, admin creation endpoint, admin listing endpoint (AUD-01, ADM-01, ADM-02, ADM-03, ADM-04) (completed 2026-04-16)
- [x] **Phase 31: Backoffice Auth Code Flow UI** - Vinxi server-side code exchange, login redirect, logout, forced password change handled natively by Keycloak (ACF-01, ACF-02, ACF-03, ACF-04 frontend side)
- [x] **Phase 32: Backoffice Admin Management UI + Audit Log UI** - Create admin form with one-time password display, admin list page, paginated audit log with filters (ADM-01, ADM-02, ADM-03, ADM-04, AUD-02, AUD-03)

---

## Milestone v5.0 — Phase Details

### Phase 29: Keycloak Config + Auth Code Flow Backend
**Goal:** Backend is ready to receive Auth Code Flow callbacks — Keycloak client provisioned, PKCE exchange wired server-side, tokens written to httpOnly cookies
**Depends on:** Phase 28 (Security Documentation)
**Requirements:** ACF-01, ACF-02, ACF-03, ACF-04
**Success Criteria** (what must be TRUE):
  1. Keycloak realm has a new confidential client `onboarding-backoffice` with standardFlowEnabled, exact redirect URIs registered, and client secret configured
  2. Admin navigating to the backoffice is redirected to Keycloak's authorization endpoint with code_challenge (PKCE) — no credentials handled by the app
  3. After Keycloak redirects back with an authorization code, the Vinxi server exchanges it for tokens and writes them to httpOnly, SameSite=Strict cookies — no token is exposed to JavaScript
  4. Logout clears the httpOnly cookies and redirects the browser to Keycloak's OIDC logout endpoint (`/protocol/openid-connect/logout`) — Keycloak SSO session is terminated
  5. An admin with `UPDATE_PASSWORD` requiredAction is automatically redirected by Keycloak to the password change screen during the Auth Code Flow — no extra backend logic needed
**Plans:** TBD
**UI hint**: yes

### Phase 30: Audit Log Backend + Admin Management Backend
**Goal:** The backend persists every admin action immutably and exposes endpoints to create and list administrators
**Depends on:** Phase 29
**Requirements:** AUD-01, ADM-01, ADM-02, ADM-03, ADM-04
**Success Criteria** (what must be TRUE):
  1. A new `AuditLog` table exists in app_db (EF Core migration) with columns: id, actor_email, action_type, target_email, timestamp, details_json — no UPDATE or DELETE operations are permitted on this table
  2. Every admin action (create admin, block user, unblock user, edit user, delete user) automatically writes an audit record via a shared audit service — the record is visible immediately after the action
  3. POST `/api/admin/administrators` creates a new Keycloak user with role `admin` and `UPDATE_PASSWORD` requiredAction, returns the one-time temporary password in the response body
  4. GET `/api/admin/administrators` returns all users with role `admin` in Keycloak — the list includes the actor admin and newly created admins
  5. Both endpoints require `[Authorize(Roles = "admin")]` — non-admin callers receive 403
**Plans:** 4/4 plans complete
Plans:
- [x] 30-01-PLAN.md — IAuditService + AuditService, migração dos 5 handlers, remoção do legado AuditLog, migration DropAuditLogs
- [x] 30-02-PLAN.md — AdminUserDto, GetAdministratorsQuery, GetUsersByRoleAsync, POST /administrators + GET /administrators, frontend admin-api.ts

### Phase 31: Backoffice Auth Code Flow UI
**Goal:** The backoffice frontend replaces the ROPC login form with an Auth Code Flow redirect — Keycloak handles the login screen and forced password change natively
**Depends on:** Phase 29
**Requirements:** ACF-01, ACF-02, ACF-03, ACF-04
**Success Criteria** (what must be TRUE):
  1. Navigating to any protected backoffice route without a session redirects the browser to Keycloak's authorization endpoint — no custom login form is rendered by the backoffice
  2. After completing login on Keycloak (including forced password change if required), the admin is redirected back to the backoffice and the dashboard is displayed — session is established via httpOnly cookies
  3. An admin whose account has `UPDATE_PASSWORD` requiredAction is taken through the Keycloak-native password change screen before reaching the backoffice — no custom password change UI needed in the app
  4. Clicking logout in the backoffice header clears the session cookies and redirects to Keycloak's OIDC logout endpoint — the admin is returned to the Keycloak login page and cannot access the backoffice without re-authenticating
**Plans:** TBD
**UI hint**: yes

### Phase 32: Backoffice Admin Management UI + Audit Log UI
**Goal:** Admins can create new administrator accounts (seeing the temporary password once) and view the paginated, filterable audit log
**Depends on:** Phase 30, Phase 31
**Requirements:** ADM-01, ADM-02, ADM-03, ADM-04, AUD-02, AUD-03
**Success Criteria** (what must be TRUE):
  1. A "Create Administrator" form accepts name and email — on submit, the backend creates the Keycloak account and returns a one-time temporary password displayed in a modal; the modal cannot be reopened and the password is not stored
  2. The administrators list page shows all accounts with the `admin` role — including name, email, and Keycloak account status
  3. The audit log page shows a paginated table (20 per page) of all recorded admin actions with columns: timestamp, actor email, action type, target, details
  4. The audit log supports simultaneous filters: date range (from/to), action type (multi-select), and actor email (free text) — applying filters updates the table without a full page reload
  5. All backoffice pages require an active authenticated session — unauthenticated access triggers the Auth Code Flow redirect established in Phase 31
**Plans:** TBD
**UI hint**: yes

### Phase 33: PKCE + Custom Keycloak Themes para Backoffice e Client
**Goal:** Ambas as aplicações (backoffice e client) autenticam via Authorization Code Flow com PKCE. Cada uma possui um Custom Keycloak Theme dedicado que replica fielmente a identidade visual da aplicação — o usuário nunca percebe a transição para o Keycloak. 2FA, `UPDATE_PASSWORD` e demais required actions são tratados nativamente pelo Keycloak sem código custom nas apps.
**Depends on:** Phase 28 (Security Documentation)
**Requirements:** PKC-01, PKC-02, PKC-03, PKC-04, PKC-05, PKC-06

**Contexto arquitetural:**

ROPC (Resource Owner Password Credentials) foi descartado por duas razões fundamentais:
1. **Sem suporte a 2FA** — TOTP/WebAuthn exigem interação do usuário no Keycloak; ROPC bypassa esse fluxo completamente.
2. **Credenciais passam pela aplicação** — violação do princípio de separação. OAuth 2.1 depreca ROPC explicitamente.

A solução adotada é **Authorization Code Flow com PKCE** (RFC 7636), onde o usuário é redirecionado para o Keycloak mas percebe uma experiência nativa graças ao Custom Theme. Não há iframes nem `keycloak-js` — o redirect é a garantia de segurança.

**Estratégia de Custom Themes:**

| App | Theme Name | Identidade Visual | Experiência |
|-----|-----------|-------------------|-------------|
| `frontend/client` | `onboarding-client` | Branding do produto (cores primárias, logo do produto) | Formulário de login de cliente final: email + senha, mensagens amigáveis, link para cadastro |
| `frontend/backoffice` | `onboarding-backoffice` | Estilo administrativo (neutro, profissional, dark-friendly) | Formulário de login de admin: sem link de cadastro, mensagem de acesso restrito |

Cada theme é um diretório Keycloak com templates FreeMarker + CSS (sem framework JS externo):
```
keycloak/themes/
  onboarding-client/
    login/
      login.ftl          ← formulário de login customizado
      login-reset-password.ftl
      login-update-password.ftl   ← tela de troca de senha obrigatória
      login-otp.ftl      ← 2FA (TOTP)
      template.ftl       ← layout base
      theme.properties   ← herda de 'keycloak' como fallback
      resources/
        css/styles.css
  onboarding-backoffice/
    login/
      login.ftl
      login-reset-password.ftl
      login-update-password.ftl
      login-otp.ftl
      template.ftl
      theme.properties
      resources/
        css/styles.css
```

**Fluxo PKCE por aplicação:**

```
[Client App]
  1. Usuário acessa rota protegida → redirect para Keycloak (onboarding-client theme)
  2. Keycloak autentica → redirect de volta com ?code=...
  3. Backend BFF troca o code por tokens (PKCE verify)
  4. Tokens armazenados em httpOnly cookies → JS nunca acessa

[Backoffice App]
  1. Admin acessa rota protegida → redirect para Keycloak (onboarding-backoffice theme)
  2. Keycloak autentica → se UPDATE_PASSWORD pendente, exibe tela nativa de troca de senha
  3. Redirect de volta com ?code=...
  4. Backend BFF troca o code por tokens
  5. Session via httpOnly cookies
```

**Clients Keycloak necessários:**

| Client ID | App | Grant | PKCE | Redirect URIs |
|-----------|-----|-------|------|---------------|
| `onboarding-app` | Client (frontend/client) | Authorization Code | obrigatório (S256) | `http://localhost:3000/callback` |
| `onboarding-backoffice` | Backoffice | Authorization Code | obrigatório (S256) | `http://localhost:4000/admin/callback` |

**Success Criteria** (what must be TRUE):
  1. Um usuário cliente que acessa `/profile` sem sessão é redirecionado para o Keycloak com o theme `onboarding-client` — o formulário de login exibe o branding do produto e não o tema padrão do Keycloak
  2. Um admin que acessa `/admin/users` sem sessão é redirecionado para o Keycloak com o theme `onboarding-backoffice` — o formulário exibe identidade administrativa, sem link de cadastro, com mensagem de acesso restrito
  3. Um admin com `UPDATE_PASSWORD` requiredAction é apresentado à tela nativa de troca de senha do Keycloak (template `login-update-password.ftl` customizado) antes de acessar o backoffice — zero código de troca de senha nas aplicações
  4. Após login bem-sucedido, o authorization code é trocado por tokens no backend (BFF); nenhum token aparece em localStorage, sessionStorage ou no corpo de respostas JSON acessíveis ao JavaScript
  5. Logout em qualquer app limpa os cookies httpOnly E invoca o OIDC logout endpoint do Keycloak (`/protocol/openid-connect/logout?id_token_hint=...`) — a sessão SSO do Keycloak é encerrada
  6. O realm JSON (`keycloak/onboarding-realm.json`) referencia os themes `onboarding-client` e `onboarding-backoffice` nos respectivos clients — o ambiente sobe via `docker compose up` sem configuração manual adicional

**Plans:** 1/0 plans complete
**UI hint**: yes (Custom Keycloak Themes são a entrega central desta fase)

---

## Milestone v5.0 — Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 29. Keycloak Config + Auth Code Flow Backend | 0/TBD | 📋 Planned | — |
| 30. Audit Log Backend + Admin Management Backend | 4/4 | Complete    | 2026-04-16 |
| 31. Backoffice Auth Code Flow UI | 0/TBD | 📋 Planned | — |
| 32. Backoffice Admin Management UI + Audit Log UI | 0/TBD | 📋 Planned | — |
| 33. PKCE + Custom Keycloak Themes (Backoffice + Client) | 1/0 | Complete    | 2026-04-17 |
| 34. Isolar Backoffice e Client em Realms Separados | 5/5 | ✅ Complete | 2026-04-21 |


### Phase 34: Isolar Backoffice e Client em Realms Separados

**Goal:** Isolar o banco de usuários do Keycloak em dois Realms (backoffice e client) para separar regras de sessão de forma arquitetural
**Requirements**: ARCH-04
**Depends on:** Phase 33
**Plans:** 5 plans (complete)

Plans:
- [x] Realm `backoffice-realm.json` criado com client `onboarding-backoffice` + `onboarding-api-admin`
- [x] Realm `client-realm.json` criado com client `onboarding-client-acf`
- [x] `compose.yaml` importa pasta inteira; frontends apontam para realms corretos
- [x] Backend com dois `AddJwtBearer` (`BearerBackoffice` + `BearerClient`)
- [x] `KeycloakUserService` com roteamento `targetRealm` por parâmetro

---

## Milestone v6.0 — Gestão Completa de Administradores (Phases 35-36)

**Goal:** Admin pode gerenciar outros admins com operações completas — listar com paginação/filtros, editar, resetar senha e desativar/reativar — com segurança e auditoria obrigatórias em cada operação.

**Key decisions:**
- All SEC-* requirements are backend-enforced blockers — they are not optional UI guards
- Phase 35 delivers the complete backend before any UI exists (Phase 36 can integrate against it)
- Existing infrastructure is leveraged: `IAuditService` (append-only), `KeycloakUserService` (targetRealm routing), `BearerBackoffice` auth scheme, `RandomNumberGenerator` for password generation
- Admin self-operation prevention (SEC-01) enforced server-side by comparing actor claim with target ID
- Last-admin guard (SEC-05) enforced server-side before disable — counts enabled admins in backoffice realm

**Depends on:** Milestone v5.0 complete (Phase 34)

**Phase order rationale:** 35 (backend API + security guards + audit) → 36 (backoffice UI with all operations). Backend-first ensures API contracts exist before UI is built.

### Phases

- [x] **Phase 35: Admin Management Backend** - All .NET API endpoints for admin CRUD + security guards (SEC-01..05) + audit logging (AUD-04..06) (MGMT-01..06, SEC-01..05, AUD-04..06)
 (completed 2026-04-22)
- [x] **Phase 36: Admin Management UI** - Updated backoffice UI — paginated admin list with filters, edit modal, reset password modal, deactivate/reactivate confirmations (MGMT-01..06)
 (completed 2026-04-24)

---

## Milestone v6.0 — Phase Details

### Phase 35: Admin Management Backend
**Goal:** The API enforces every security rule and records every audit entry for admin management operations — the frontend cannot bypass any guard
**Depends on:** Phase 34
**Requirements:** MGMT-01, MGMT-02, MGMT-03, MGMT-04, MGMT-05, MGMT-06, SEC-01, SEC-02, SEC-03, SEC-04, SEC-05, AUD-04, AUD-05, AUD-06
**Success Criteria** (what must be TRUE):
   1. GET `/api/admin/administrators/paginated` returns a paginated list (20 per page) filterable by name, email, and status — non-admin callers receive 403 (SEC-02)
   2. PUT `/api/admin/administrators/{id}` updates name and email in Keycloak; attempting to update own account returns 400 (SEC-01); duplicate email returns 409 (SEC-04); audit record written with old and new field values (AUD-04)
   3. POST `/api/admin/administrators/{id}/reset-password` generates a cryptographically secure 16+ char password via `RandomNumberGenerator`, sets it in Keycloak with `UPDATE_PASSWORD` requiredAction, returns the password once in the response body; attempting to reset own password returns 400 (SEC-01); audit record written with actor and target — password never logged (SEC-03, AUD-05)
   4. POST `/api/admin/administrators/{id}/toggle-status` with `{ activate: false }` disables the Keycloak account; attempting to deactivate own account returns 400 (SEC-01); attempting to deactivate the last active admin returns 400/409 (SEC-05); audit record written with actor, target, and optional reason (AUD-06)
   5. POST `/api/admin/administrators/{id}/toggle-status` with `{ activate: true }` re-enables the Keycloak account; attempting to reactivate own account returns 400 (SEC-01); audit record written (AUD-06)
**Plans:** 1/1 plans complete (completed 2026-04-22)

### Phase 36: Admin Management UI
**Goal:** Backoffice users can perform all admin management operations through a polished, feedback-rich interface that reflects backend state immediately
**Depends on:** Phase 35
**Requirements:** MGMT-01, MGMT-02, MGMT-03, MGMT-04, MGMT-05, MGMT-06
**Success Criteria** (what must be TRUE):
  1. The administrators list page shows a paginated table (20 per page) with name, email, and status; name/email filter inputs and a status dropdown filter the results without full page reload; loading skeletons are shown during API calls
  2. An edit modal for each admin pre-populates name and email; the actor's own row has the edit action disabled (reflecting SEC-01); form shows inline validation errors; successful save updates the table row with a toast confirmation
  3. A reset password modal asks the actor to confirm the action; after confirmation the new temporary password is displayed in a one-time reveal dialog (cannot be reopened); the action is disabled on the actor's own row (reflecting SEC-01)
  4. Deactivate and reactivate use confirmation dialogs; the actor's own row has these actions disabled (reflecting SEC-01); attempting to deactivate the last active admin shows a clear error message from the API (reflecting SEC-05); the table status badge updates after each action with a toast confirmation
  5. All unauthenticated access to admin management pages triggers the Auth Code Flow redirect established in Phase 33
**Plans:** 4/4 plans complete
**UI hint**: yes

---

## Milestone v6.0 — Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 35. Admin Management Backend | 1/1 | ✅ Complete | 2026-04-22 |
| 36. Admin Management UI | 4/4 | ✅ Complete | 2026-04-24 |

### Phase 35: Admin Management Backend (completed 2026-04-22)

Plans:
- [x] 35-01-PLAN.md — Paginated GET endpoint, update endpoint, reset-password endpoint, toggle-status endpoint with SEC-01..05 and AUD-04..06

### Phase 36: Admin Management UI (completed 2026-04-24)

Plans:
- [x] 36-01-PLAN.md — API client functions, adminEditAdministratorSchema, adminId in auth context, AdminStatusFilter options
- [x] 36-02-PLAN.md — AdminActionsDropdown + AdminAdministratorsTable
- [x] 36-03-PLAN.md — EditAdminDialog, ResetPasswordDialog, DeactivateAdminDialog, ReactivateAdminDialog
- [x] 36-04-PLAN.md — AdminAdministratorsPage rewrite with full integration

---

## Milestone v7.0 — PJ-Only Onboarding + Gestão de Funcionários (Phases 37-42)

**Goal:** Transformar cadastro misto PF/PJ em PJ-only, onde PJ é usuário principal que gerencia funcionários PF com grupos de acesso via Keycloak, aceite de termos, auditoria e dashboard mock. Isolamento entre empresas é requisito CRÍTICO.

**⚠️ ARQUITETURA — MUDANÇA DE DOMÍNIO:**
> O aggregate `Client` (PF/PJ) é substituído por `Company` (PJ) + `Employee` (PF).
> Base de dados zerada via `docker compose down -v`. New EF Core migration cria schemas novos.
> Keycloak groups/roles nativos para permissões (admin-empresa, viewer, dashboard).
> Isolamento entre empresas via `CompanyId` FK + EF Core global query filter.

**Key decisions:**
- Permissões via Keycloak groups nativos (NÃO Bit Flags no JWT) — sem custom mapper
- Isolamento entre empresas: EF Core global query filter (`HasQueryFilter(e => e.CompanyId == currentCompanyId)`) + defense-in-depth application-level checks
- Cadastro PJ-only: fluxo PF removido do frontend e API
- Aceite de termos: texto mock com timestamp + versão no registro
- Dashboard: dados estáticos/mock, sem dados reais
- Base zerada: `docker compose down -v` — migration limpa cria tudo do zero

**Depends on:** Milestone v6.0 completo (Phase 36)

**Phase order rationale:** 37 (domain redesign foundation) → 38 (employee management API — needs new domain) → 39 (Keycloak groups & isolation — needs API endpoints) → 40 (client frontend — needs backend + permissions) → 41 (backoffice + audit — needs employee data) → 42 (CI coverage — needs all code complete).

### Phases

- [ ] **Phase 37: Domain Model Redesign** - Company + Employee aggregates, TermsAcceptance value object, remove PF flow. New EF Core migration. Base zerada. (REG-02, REG-04, REG-05)
- [ ] **Phase 38: Employee Registration & Management API** - PJ registration endpoint, employee CRUD API, company isolation via global query filter. (REG-01, REG-03, MGMT-01..05)
- [x] **Phase 39: Keycloak Groups & Permissions** - Configure Keycloak groups (admin-empresa, viewer, dashboard), JWT claims mapping, backend permission enforcement, company isolation checks. (PERM-01..05)
- [x] **Phase 40: Client Frontend — PJ Registration & Employee Management** - PJ registration form with terms acceptance, employee list/block/reset/edit/delete, remove PF flow from frontend, dashboard mock. (DASH-01) ✅ Complete (2026-04-26)
- [ ] **Phase 41: BackOffice Employee Management + Audit** - BackOffice views employees from any company, force reset/block. Extend audit log for employee actions. (ADM-01, ADM-02, AUD-01, AUD-02)
- [ ] **Phase 42: CI Coverage Enforcement** - GitHub Actions with 80% test coverage enforcement for backend (.NET) and frontend (React/Vinxi). (CI-01)
- [ ] **Phase 43: E2E Playwright Validation** - Playwright E2E tests: create PJ, login, dashboard, create employee, login employee, validate permissions UI + JWT claims. (E2E-01, E2E-02, E2E-03, E2E-04, E2E-05)
- [ ] **Phase 44: Custom Access Groups CRUD** - PJ cria/edita/deleta grupos de acesso customizados com permissões granulares. Frontend UI com tabela, dialogs e validação. Default groups (admin-empresa, viewer, dashboard) são imutáveis. (PERM-04 extended, PERM-06)

---

### Phase 37: Domain Model Redesign
**Goal:** Novos aggregates Company (PJ) e Employee (PF) substituem Client. TermsAcceptance value object. Remoção completa do fluxo PF. Migration limpa cria tudo do zero.
**Depends on:** Phase 36
**Requirements:** REG-02, REG-04, REG-05
**Success Criteria** (what must be TRUE):
  1. `Company` aggregate existe com propriedades: CNPJ, RazãoSocial, Email, Telefone, KeycloakUserId, TermsAcceptance — CNPJ é value object com validação de check-digit
  2. `Employee` aggregate existe com propriedades: CPF, Nome, Email, Telefone, CompanyId (FK para Company), KeycloakUserId, AccessGroup — CPF é value object com validação
  3. `TermsAcceptance` value object existe com propriedades: AcceptedAt, TermsVersion, IpAddress — aceitar termos é obrigatório no registro PJ
  4. Fluxo PF completamente removido: `RegisterPessoaFisica` factory method, `PersonType.PF` enum value, rotas `/registration?tipo=pf` — nenhum vestígio no código ou nas rotas
  5. Nova EF Core migration cria tabela `companies` e `employees` e remove tabela `clients` — `docker compose down -v && docker compose up` sobe tudo limpo
  6. Isolamento entre empresas: `CompanyConfiguration` e `EmployeeConfiguration` com `HasQueryFilter` filtrando por `CompanyId` — nenhum employee de outra empresa é acessível
  7. Todos os testes unitários do domain passando (Company, Employee, Cnpj, Cpf, TermsAcceptance, EmployeeAccessGroup)
**Plans:** 4 plans
Plans:
- [x] 37-01-PLAN.md — Domain layer: Company + TermsAcceptance + Employee + AccessGroup + Permissions + repositories + delete Client aggregate
- [x] 37-02-PLAN.md — Domain tests: Company, Employee, TermsAcceptance, AccessGroup, Permissions tests + delete Client/PF tests (TDD)
- [x] 37-03-PLAN.md — Infrastructure: EF Core configs, AppDbContext, HasQueryFilter, repositories, migration (drop clients, create companies/employees/access_groups)
- [ ] 37-04-PLAN.md — Application + API: Migrate admin CQRS/DTOs, migrate controllers, delete Client endpoints, Program.cs DI, full build verification

### Phase 38: Employee Registration & Management API
**Goal:** Backend endpoints para registro PJ, cadastro de funcionários e CRUD completo de funcionários — tudo com isolamento obrigatório por empresa.
**Depends on:** Phase 37
**Requirements:** REG-01, REG-03, MGMT-01, MGMT-02, MGMT-03, MGMT-04, MGMT-05
**Success Criteria** (what must be TRUE):
  1. POST `/api/companies/registration` registra PJ com CNPJ, razão social, email, telefone, senha + aceite de termos — CNPJ duplicado retorna 409 (REG-01, REG-02)
  2. POST `/api/companies/{companyId}/employees` cadastra funcionário PF vinculado à empresa — gera senha temporária, Keycloak user criado no realm `client` com group `viewer` padrão (REG-03)
  3. GET `/api/companies/{companyId}/employees` retorna lista paginada de funcionários (20/página) com filtros por nome e status — escopo obrigatório por companyId (MGMT-01)
  4. POST `/api/companies/{companyId}/employees/{id}/toggle-status` bloqueia/desbloqueia funcionário no Keycloak — preserva dados para auditoria (MGMT-02)
  5. POST `/api/companies/{companyId}/employees/{id}/reset-password` gera senha temporária exibida uma vez — Keycloak força troca via `UPDATE_PASSWORD` requiredAction (MGMT-03)
  6. PUT `/api/companies/{companyId}/employees/{id}` edita dados do funcionário (nome, email, telefone) — persiste no Keycloak (MGMT-04)
  7. DELETE `/api/companies/{companyId}/employees/{id}` realiza exclusão LGPD — anonimiza dados no PostgreSQL + delete no Keycloak (MGMT-05)
  8. Nenhum endpoint retorna dados de funcionários de outra empresa — company isolation enforced em todos os queries (PERM-05 backend preview)
**Plans:** 3 plans
Plans:
- [ ] 38-01-PLAN.md — Company Registration endpoint (POST /api/companies/registration) + admin company query handlers
- [ ] 38-02-PLAN.md — Employee CRUD API (register, list, block/unblock, reset password, edit, LGPD delete, access group change) — company-scoped endpoints
- [ ] 38-03-PLAN.md — Admin-side employee handlers (list across companies, block/unblock/delete any employee) — replacing Phase 37 stubs

### Phase 39: Keycloak Groups & Permissions
**Goal:** Keycloak groups configurados (admin-empresa, viewer, dashboard). Backend lê groups do JWT e aplica permissões. Isolamento entre empresas enforced no backend.
**Depends on:** Phase 38
**Requirements:** PERM-01, PERM-02, PERM-03, PERM-04, PERM-05
**Success Criteria** (what must be TRUE):
  1. Keycloak realm `client` tem groups `admin-empresa`, `viewer`, `dashboard` — funcionários recebem `viewer` por padrão no cadastro (PERM-01, PERM-02, PERM-03)
  2. Backend lê groups do JWT claims e aplica autorização: `admin-empresa` pode gerenciar funcionários e ver audit; `viewer` pode apenas visualizar dados; `dashboard` pode acessar dashboard (PERM-01, PERM-02, PERM-03)
  3. PJ pode atribuir/remover groups de funcionários via PUT `/api/companies/{companyId}/employees/{id}/access-group` — apenas groups permitidos: admin-empresa, viewer, dashboard (PERM-04)
  4. Funcionário com `admin-empresa` tem mesmos poderes de gestão do PJ dono (visualizar, editar, bloquear, resetar, excluir funcionários + ver audit) (PERM-01)
  5. Company isolation: EF Core global query filter garante que employee queries NUNCA retornam dados de outra empresa — defense-in-depth check no service layer também (PERM-05)
  6. JWT do Keycloak inclui claims de grupos em `realm_access.roles` ou `groups` — backend mapeia para permissões via `ClaimsPrincipal`
**Plans:** 3 plans
Plans:
- [x] 39-01-PLAN.md — Keycloak client-realm.json groups + Group Membership mapper + IKeycloakUserService group methods
- [x] 39-02-PLAN.md — ClientClaimsMiddleware, ICurrentCompanyPermissionsService, GroupsClaimsTransformation, 7 authorization policies, Program.cs wiring
- [x] 39-03-PLAN.md — Handler extensions (RegisterCompany groups sync, RegisterEmployee group assignment, ChangeAccessGroup Keycloak sync) + CompaniesController permission policies

### Phase 40: Client Frontend — PJ Registration & Employee Management
**Goal:** Frontend client redesenhado para cadastro PJ-only com gestão de funcionários. Dashboard mock com dados estáticos. Remoção completa do fluxo PF.
**Depends on:** Phase 39
**Requirements:** DASH-01
**Success Criteria** (what must be TRUE):
  1. Tela de cadastro mostra apenas formulário PJ (razão social, CNPJ, email, telefone, senha) com checkbox obrigatório de aceite de termos — nenhum seletor PF/PJ existe (REG-01 frontend, REG-05 frontend)
  2. Após cadastro PJ e login, tela de gestão de funcionários mostra lista paginada com nome, email, status e ações (bloquear, resetar senha, editar, excluir) (MGMT-01..05 frontend)
  3. PJ pode atribuir/remover grupos de acesso de funcionários com dropdown (admin-empresa, viewer, dashboard) — mudança reflete no Keycloak em tempo real (PERM-04 frontend)
  4. Tela de dashboard mostra dados estáticos mock: total funcionários ativos/inativos, logins recentes, ações por período (DASH-01)
  5. Funcionário com role `admin-empresa` vê mesmas telas de gestão que o PJ dono — `viewer` vê dados em modo leitura sem botões de ação
  6. Nenhuma rota de cadastro PF existe no frontend — `/registration?tipo=pf` retorna 404 ou redireciona para cadastro PJ
  7. Login de funcionário redireciona para telas baseadas no group: `admin-empresa` → management, `viewer` → read-only employee list, `dashboard` → dashboard
**Plans:** 4 plans
Plans:
- [x] 40-01-PLAN.md — Auth context extension (group/permissions), employee API client, PJ validation schemas, Sidebar, router restructure
- [x] 40-02-PLAN.md — PJ-only 2-step registration wizard with terms acceptance, PF removal
- [x] 40-03-PLAN.md — Employee management UI (list, search, edit, block, reset password, LGPD delete, change access group)
- [x] 40-04-PLAN.md — Dashboard mock (6 cards), permission-based routing, ProfilePage PJ adaptation, PF removal verification

### Phase 41: BackOffice Employee Management + Audit
**Goal:** BackOffice pode visualizar funcionários de qualquer empresa, auditar ações e dar suporte. Audit log estendido para ações de funcionários.
**Depends on:** Phase 39
**Requirements:** ADM-01, ADM-02, AUD-01, AUD-02
**Success Criteria** (what must be TRUE):
  1. GET `/api/admin/employees` retorna lista paginada de funcionários de TODAS as empresas com filtros (empresa, nome, status) — admin backoffice ignora company isolation (ADM-01)
  2. POST `/api/admin/employees/{id}/reset-password` força reset de senha de qualquer funcionário de qualquer empresa (ADM-02)
  3. POST `/api/admin/employees/{id}/toggle-status` bloqueia/desbloqueia qualquer funcionário de qualquer empresa (ADM-02)
  4. PJ e Admin Empresa podem visualizar audit log dos seus funcionários com filtros (data, tipo de ação, ator) — log é escopo por companyId do PJ logado (AUD-01)
  5. Todas as ações de funcionários (login, edição, bloqueio, reset senha) são registradas automaticamente no audit log existente — append-only, sem UPDATE ou DELETE (AUD-02)
  6. AuditLog estendido com campos: `CompanyId`, `TargetEmployeeId`, `ActionType` (valores novos: EMPLOYEE_LOGIN, EMPLOYEE_EDIT, EMPLOYEE_BLOCK, EMPLOYEE_UNBLOCK, EMPLOYEE_PASSWORD_RESET, EMPLOYEE_DELETE, ACCESS_GROUP_CHANGE)
**Plans:** TBD
**UI hint**: yes

### Phase 42: CI Coverage Enforcement
**Goal:** GitHub Actions pipeline com cobertura de testes >= 80% no backend (.NET) e frontend (React/Vinxi).
**Depends on:** Phase 40, Phase 41
**Requirements:** CI-01
**Success Criteria** (what must be TRUE):
   1. GitHub Actions workflow roda em push para main e em PRs com 3 jobs paralelos: `backend` (.NET build + test + coverage), `frontend-client` (build + lint + type check), `frontend-backoffice` (build + lint + type check)
   2. Backend job falha se cobertura < 80% (`dotnet test /p:CollectCoverage=true /p:ThresholdType=line /p:Threshold=80`)
   3. Frontend jobs falham se `eslint --max-warnings 0` ou `tsc --noEmit` falharem
   4. Coverage report gerado em formato cobertura ou lcov para backend; jest coverage para frontends
   5. Workflow usa cache: `~/.nuget/packages` para .NET, `node_modules/.cache` para frontends
**Plans:** TBD

### Phase 43: E2E Playwright Validation
**Goal:** Fluxo E2E completo com Playwright validando todo o pipeline: cadastro PJ → login → dashboard → criação funcionário → login funcionário → validação permissões UI + JWT claims
**Depends on:** Phase 40
**Requirements:** E2E-01, E2E-02, E2E-03, E2E-04, E2E-05
**Success Criteria** (what must be TRUE):
   1. E2E test: Cadastro PJ completo (razão social, CNPJ, email, senha, aceite de termos) → auto-login → redireciona para dashboard
   2. E2E test: Dashboard exibe cards mock (total funcionários, ativos, inativos, logins recentes, ações por período)
   3. E2E test: PJ cria funcionário via UI → funcionário aparece na lista com status ativo e group `viewer`
   4. E2E test: Login como funcionário → redirect baseado no access group (viewer → read-only, admin-empresa → management, dashboard → dashboard)
   5. E2E test: JWT decode revela groups/roles corretos — viewer não vê botões de ação, admin-empresa vê tudo
   6. E2E test: PJ muda access group do funcionário → login novamente → permissões UI atualizadas
   7. Todos os E2E tests passam com `npx playwright test` no `frontend/client`
**Plans:** 3 plans
Plans:
- [x] 43-01-PLAN.md — Playwright infrastructure (config, auth setups, page objects, fixtures) + Registration E2E test (E2E-01) (completed 2026-04-27)
- [x] 43-02-PLAN.md — Dashboard cards E2E test (E2E-02) + Employee management E2E test (E2E-03) (completed 2026-04-27)
- [x] 43-03-PLAN.md — Employee login redirect E2E test (E2E-04) + Permission UI + JWT verification (E2E-05) + Access group change (E2E-06) (completed 2026-04-27)

---

### Phase 44: Custom Access Groups CRUD
**Goal:** PJ pode criar, editar e deletar grupos de acesso customizados com permissões granulares, em vez de ficar limitado aos 3 grupos fixos (admin-empresa, viewer, dashboard). Sistema extensível para crescimento.
**Depends on:** Phase 40 (Client Frontend)
**Requirements:** PERM-04 (extended), PERM-06
**Success Criteria** (what must be TRUE):
  1. POST `/api/companies/{companyId}/access-groups` cria um novo access group com nome e permissões selecionadas — apenas PJ com `access-groups:manage` pode criar
  2. PUT `/api/companies/{companyId}/access-groups/{id}` edita nome e/ou permissões de um access group — grupos default (admin-empresa, viewer, dashboard) NÃO podem ser editados nem deletados
  3. DELETE `/api/companies/{companyId}/access-groups/{id}` deleta um access group customizado — não permite deletar groups default; não permite deletar group com employees vinculados (retorna 400)
  4. Frontend client mostra página "Grupos de Acesso" com tabela listando todos os groups da empresa, botão "Novo Grupo" (visível apenas com `access-groups:manage`), e ações de editar/deletar (desabilitadas para groups default)
  5. Dialog de criação mostra nome + checkboxes de permissões (employees:read, employees:write, employees:delete, audit:read, dashboard:access, access-groups:manage)
  6. Dialog de edição preenche nome e permissões atuais; validação impede nome vazio e nenhuma permissão selecionada
  7. Ao deletar um group com employees vinculados, o diálogo sugere mover employees para outro group antes ou confirma desvinculação
  8. Ao registrar novo funcionário, o dropdown de access groups lista TODOS os groups (default + custom) — `RegisterEmployeeDialog` já busca da API
**Plans:** TBD

---

## Milestone v7.0 — Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 37. Domain Model Redesign | 4/4 | ✅ Complete | 2026-04-26 |
| 38. Employee Registration & Management API | 3/3 | ✅ Complete | 2026-04-26 |
| 39. Keycloak Groups & Permissions | 3/3 | ✅ Complete | 2026-04-26 |
| 40. Client Frontend — PJ Registration & Employee Management | 4/4 | 🔧 Gaps | 2026-04-26 |
| 41. BackOffice Employee Management + Audit | 0/TBD | 🔧 Gaps | 2026-04-26 |
| 42. CI Coverage Enforcement | 0/TBD | ✅ Complete | 2026-04-26 |
| 43. E2E Playwright Validation | 3/3 | ✅ Complete | 2026-04-27 |
| 44. Custom Access Groups CRUD | 0/TBD | 📋 Planned | — |

---

## Milestone Summary

| Milestone | Phases | Plans | Status | Requirements |
|-----------|--------|-------|--------|--------------|
| **v1.0** Foundation | 1-10 | 30 | ✅ Complete | 35 requirements |
| **v2.0** UX/UI + Production | 11-15 | 7+ | ✅ Complete | 14 requirements |
| **v3.0** Admin Backoffice | 16-20 | 13 | ✅ Complete | 22 requirements |
| **v4.0** CI/CD + Security | 21-28 | 20 | ✅ Complete | 25 requirements |
| **v5.0** Auth Code Flow + Admins + Audit | 29-34 | TBD | ✅ Complete | 11 requirements |
| **v6.0** Gestão Completa de Administradores | 35-36 | 5 | ✅ Complete | 14 requirements |
| **v7.0** PJ-Only Onboarding + Gestão de Funcionários | 37-44 | 19+ plans | 🔧 In Progress (gaps) | 21+ requirements |
| **Total** | **43 phases** | **110+ plans** | **6 milestones done** | **143+ requirements** |

---

*Last updated: 2026-04-28 — Phase 44 added: Custom Access Groups CRUD*
