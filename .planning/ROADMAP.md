# Roadmap: Onboarding de Clientes

## Overview

This roadmap builds a secure PF/PJ client onboarding system from infrastructure up, then extends it with an admin backoffice panel for user management. The delivery sequence mirrors the dependency chain: Docker infrastructure first, then a hardened Keycloak, then the DDD backend domain, then observability wiring, then registration and authentication endpoints, then a frontend scaffold, and finally the three user-facing screens (registration, login, profile). After v1.0/v2.0 completion, milestone v3.0 adds admin CRUD endpoints, role-based access control, and a backoffice UI with pagination, filtering, and LGPD-compliant user deletion.

Every phase delivers a coherent, independently verifiable capability before the next begins.

---

## Milestones

| Milestone | Name | Phases | Status |
|-----------|------|--------|--------|
| **v1.0** | Foundation — Cadastro e Login com Perfil Read-Only | 1-10 | ✅ Complete |
| **v2.0** | UX/UI Redesign + Production Readiness | 11-15 | ✅ Complete |
| **v3.0** | Admin Backoffice Panel | 16-20 | 📋 Defining |

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

**Goal:** Painel administrativo para gerenciar cadastros de usuários — listar, visualizar, editar, bloquear/desbloquear e excluir (LGPD) com autenticação baseada em cookies httpOnly e autorização por role "admin".

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
- [ ] 18-02-PLAN.md — User detail page with PF/PJ data display, Keycloak status badge, action buttons

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
- [ ] 18-02-PLAN.md — User detail page with PF/PJ data display, Keycloak status badge, action buttons

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

## Next Steps

**Current Milestone:** v3.0 — Admin Backoffice Panel (Defining Requirements)

**▶ Recommended Next Actions:**

1. **`/gsd:define-requirements`** — Formalize v3.0 requirements (ADMIN-01 to ADMIN-16, E2E-06/07) in REQUIREMENTS.md
2. **`/gsd:plan-phase 16`** — Start planning Phase 16 (Admin API Endpoints) — the first v3.0 phase
3. **`/gsd:discuss-phase 16`** — Gather context before planning (recommended)

**Deferred:**
- Phase 14 (E2E Testing from v2.0) — can be done in parallel with v3.0 or deferred until after v3.0

---

*Last updated: 2026-04-09 — Milestone v3.0 roadmap created with 5 new phases (16-20)*
