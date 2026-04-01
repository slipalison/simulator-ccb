# Research Summary

**Domain:** Client Onboarding System (PF/PJ + Keycloak)
**Synthesized:** 2026-04-01
**Research files:** STACK.md, FEATURES.md, ARCHITECTURE.md, PITFALLS.md

---

## Stack Consensus

**.NET 10** backend with ASP.NET Core Controllers (no Minimal API), Entity Framework Core + Npgsql for PostgreSQL, MediatR for CQRS, FluentValidation for input validation, Serilog 4.x + OpenTelemetry SDK for observability.

**React 19 + Vinxi** frontend in SPA mode, TanStack Router, React Hook Form + Zod for forms, Tailwind CSS for styling, Atomic Design component structure.

**Keycloak 26.1** (quay.io) as identity provider with two clients: public (`onboarding-app` for ROPC login) and confidential (`onboarding-api-admin` for Admin API user creation via service account).

**Two separate PostgreSQL 16** instances: `app_db` for application data, `keycloak_db` for Keycloak internal state. Strict isolation.

**Testing:** xUnit + FluentAssertions + NSubstitute for unit tests, Testcontainers for integration tests with real PostgreSQL and Keycloak.

---

## Table Stakes (Must Have)

- PF registration: nome, CPF (mod-11 validation), email, telefone, senha
- PJ registration: razão social, CNPJ (check-digit validation), email, telefone, senha
- Duplicate detection (CPF/CNPJ/email uniqueness) before Keycloak user creation
- Server-side validation on all fields (client-side is UX only)
- Custom login form with ROPC token exchange
- JWT storage in memory (NOT localStorage)
- Protected profile route with read-only data display
- Brute force protection enabled in Keycloak
- Password policy enforcement in Keycloak
- Generic error messages (no information leakage)
- Health check endpoints for Docker Compose
- Structured logging from day one

---

## Key Differentiators

- OpenTelemetry distributed tracing across API + Keycloak calls
- DDD with rich domain model (CPF/CNPJ as value objects, factory methods)
- TDD from first test — domain logic tested in isolation
- Atomic Design component library for easy UI evolution
- Keycloak Admin API via least-privilege service account
- Correlation ID propagation across services

---

## Critical Watch-Outs

1. **Keycloak SSRF** (CVE-2020-10770, CVE-2026-1518): Disable `request_uri` support
2. **Open redirect**: Register exact redirect URIs, no wildcards
3. **Admin console exposure**: Bind to 127.0.0.1 only, block `/admin` in production
4. **Brute force disabled by default**: Must enable explicitly in realm settings
5. **ROPC grant deprecated**: Conscious tradeoff — document and plan migration path
6. **DB order matters**: Persist to app_db FIRST, then create Keycloak user
7. **Token storage**: Memory only — localStorage is XSS attack vector
8. **Docker startup race**: Healthchecks + depends_on conditions required

---

## Architecture Highlights

- **Two PostgreSQL containers** — strict isolation between app and Keycloak data
- **DDD project structure**: Domain → Application → Infrastructure → API (dependency inversion)
- **Client aggregate** with PersonType (PF|PJ), value objects, factory methods
- **Registration flow**: React → API → app_db (persist) → Keycloak Admin API (create user) → 201
- **Login flow**: React → Keycloak token endpoint (ROPC) → JWT in memory → protected routes
- **Profile flow**: React → API (Bearer JWT) → app_db query → read-only profile

---

## Recommended Build Order

1. **Infrastructure**: Docker Compose, PostgreSQL x2, Keycloak realm + clients + hardening
2. **Domain Layer**: Value objects (CPF, CNPJ, Email, Phone), Client aggregate, tests
3. **Persistence**: EF Core, migrations, repository implementation
4. **Keycloak Integration**: Service account, KeycloakUserService, registration handler
5. **API Endpoints**: Controllers, JWT validation, health checks
6. **Observability**: Serilog + OpenTelemetry wiring (can start as early as step 3)
7. **Frontend Setup**: Vinxi + Atomic Design scaffold, routing
8. **Registration UI**: PF/PJ forms, API integration, post-registration redirect
9. **Login UI**: Custom login form, ROPC token exchange, token storage
10. **Profile UI**: Authenticated fetch, read-only display
11. **Security Hardening**: Final review, penetration testing checklist

---

## Risk Register

| Risk | Severity | Mitigation |
|------|----------|------------|
| ROPC grant prevents MFA | MEDIUM | Plan migration to Auth Code + PKCE |
| Keycloak SSRF vulnerabilities | HIGH | Disable request_uri, network segmentation |
| JWT in localStorage | HIGH | Enforce memory-only storage |
| Docker startup race conditions | MEDIUM | Healthchecks + depends_on |
| CNPJ alphanumeric format (July 2026) | LOW | Support both formats from start |
| Keycloak Admin API credentials leak | HIGH | Environment variables only, log masking |
