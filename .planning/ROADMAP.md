# Roadmap: Onboarding de Clientes

## Overview

This roadmap builds a secure PF/PJ client onboarding system from infrastructure up. The delivery sequence mirrors the dependency chain: Docker infrastructure first, then a hardened Keycloak, then the DDD backend domain, then observability wiring, then registration and authentication endpoints, then a frontend scaffold, and finally the three user-facing screens (registration, login, profile). Every phase delivers a coherent, independently verifiable capability before the next begins.

## Phases

- [x] **Phase 1: Infrastructure** - Docker Compose with dual PostgreSQL, Keycloak realm configured and running (completed 2026-04-01)
- [x] **Phase 2: Keycloak Security Hardening** - Keycloak hardened against all documented attack surfaces (completed 2026-04-02)
- [x] **Phase 3: Backend Domain Layer** - DDD domain model with value objects, aggregate, and full test coverage
- [x] **Phase 4: Observability** - Serilog + OpenTelemetry wired across all services with correlation ID propagation
- [x] **Phase 5: Registration API** - Backend endpoints for PF/PJ registration with full validation and Keycloak user creation
- [x] **Phase 6: Authentication API** - JWT issuance, token refresh, and protected route enforcement in the backend
- [x] **Phase 7: Frontend Foundation** - Vinxi SPA scaffold with Atomic Design structure, routing, and form primitives (completed 2026-04-07)
- [x] **Phase 8: Registration UI** - PF/PJ registration forms integrated with the API, including client-side validation and post-registration redirect (completed 2026-04-07)
- [x] **Phase 9: Login UI** - Custom login screen with ROPC token exchange and in-memory JWT storage (completed 2026-04-07)
- [x] **Phase 10: Profile UI** - Read-only profile screen displaying PF/PJ data via authenticated API call (completed 2026-04-08)
- [x] **Phase 11: UX Redesign** - Unified registration form with password UX, login-first navigation, auto-login post-registration, and forgot password flow (completed 2026-04-08)
- [ ] **Phase 12: UI Redesign** - shadcn/ui adoption, dark/light theme, complete visual redesign of all screens (Login, Registration, Profile, Forgot/Reset Password)

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
- [ ] 01-PLAN-01.md — Repo skeleton, compose.yaml with dual PostgreSQL, secret management
- [ ] 01-PLAN-02.md — Keycloak realm JSON with clients, brute force, password policy
- [ ] 01-PLAN-03.md — .NET solution scaffold, Vinxi frontend, full stack smoke test

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
- [ ] 08-01-PLAN.md — Registration entry point: /registration route, PF/PJ type selector, placeholders
- [ ] 08-02-PLAN.md — PF and PJ registration forms: Zod schemas, RHF + inline validation, check-digit
- [ ] 08-03-PLAN.md — API integration: registerClient, error handling, success redirect to /login

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
**Plans**: 3/3 plans
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
**Plans**: 2/2 plans
Plans:
- [ ] 11-01-PLAN.md — Unified registration form, password strength meter, show/hide, confirm password, login-first navigation, auto-login
- [ ] 11-02-PLAN.md — Forgot/reset password flow with Resend.com integration

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10

Note: Phase 7 (Frontend Foundation) depends only on Phase 1 and can begin in parallel with Phases 2–6 if desired, but the default execution order is sequential.

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Infrastructure | 3/3 | Complete   | 2026-04-01 |
| 2. Keycloak Security Hardening | 1/1 | Complete   | 2026-04-02 |
| 3. Backend Domain Layer | 2/2 | Complete   | 2026-04-02 |
| 4. Observability | 4/4 | Complete   | 2026-04-03 |
| 5. Registration API | 4/4 | Complete   | 2026-04-05 |
| 6. Authentication API | 3/3 | Complete   | 2026-04-06 |
| 7. Frontend Foundation | 4/4 | Complete   | 2026-04-07 |
| 8. Registration UI | 3/3 | Complete   | 2026-04-07 |
| 9. Login UI | 3/3 | Complete   | 2026-04-07 |
| 10. Profile UI | 3/3 | Complete   | 2026-04-08 |
| 11. UX Redesign | 2/2 | Complete   | 2026-04-08 |
