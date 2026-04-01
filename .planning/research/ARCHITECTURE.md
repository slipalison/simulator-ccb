# Architecture Patterns

**Domain:** Client onboarding system (PF/PJ registration + authentication)
**Project:** Onboarding de Clientes
**Researched:** 2026-04-01
**Overall confidence:** HIGH (Microsoft docs + Keycloak official docs + verified patterns)

---

## Recommended Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│  Docker Compose Network (internal)                              │
│                                                                 │
│  ┌──────────────┐   HTTPS    ┌──────────────────────────────┐  │
│  │   React SPA  │ ─────────▶ │  .NET 10 API                 │  │
│  │  (Vinxi/Vite)│            │  (ASP.NET Controllers + DDD) │  │
│  │  :5173       │            │  :8080                        │  │
│  └──────┬───────┘            └──────┬──────────┬─────────────┘  │
│         │                          │          │                 │
│         │ ROPC Token Request        │ EF Core  │ Admin API       │
│         │ (direct to Keycloak)      ▼          ▼                 │
│         │                   ┌──────────┐  ┌───────────────┐     │
│         └──────────────────▶│ Keycloak │  │  PostgreSQL   │     │
│                             │  :8180   │  │  :5432        │     │
│                             └──────────┘  └───────────────┘     │
│                                  │                              │
│                             ┌────▼──────────────────────────┐   │
│                             │  keycloak_db (PostgreSQL)      │   │
│                             │  :5433 (internal port)         │   │
└─────────────────────────────────────────────────────────────────┘
```

**Two separate PostgreSQL databases:**
- `app_db` — application data (client registrations, PF/PJ records)
- `keycloak_db` — Keycloak's internal store (users, realms, sessions)

These must be isolated: Keycloak owns its database, the API owns the application database.

---

## Component Boundaries

| Component | Responsibility | Communicates With | Protocol |
|-----------|---------------|-------------------|----------|
| React SPA (Vinxi) | UI — registration forms, login form, profile view | .NET API (registration), Keycloak (token request) | HTTPS REST, form POST to token endpoint |
| .NET API | Registration orchestration, business logic, data persistence | PostgreSQL (app data), Keycloak Admin API (create user) | TCP/EF Core, HTTPS REST |
| Keycloak | Identity provider — token issuance, user store, session management | keycloak_db (state), .NET API (JWT validation introspection via OIDC metadata) | PostgreSQL JDBC, OIDC/JWT |
| PostgreSQL (app_db) | Application data source of truth — PF/PJ records | .NET API only | TCP/5432 |
| PostgreSQL (keycloak_db) | Keycloak internal state | Keycloak only | TCP/5433 |

**Critical boundary rule:** The .NET API must NEVER write directly to `keycloak_db`. All user management in Keycloak goes through the Keycloak Admin REST API only.

---

## Data Flow

### Flow 1: Client Registration

```
User fills registration form (PF or PJ)
  │
  ▼
React SPA validates locally (field presence, CPF/CNPJ format)
  │
  ▼ POST /api/clients  (registration DTO)
.NET API — Application Layer (RegisterClientCommandHandler)
  │
  ├──▶ Domain Layer validates (CPF/CNPJ uniqueness rule, value objects)
  │
  ├──▶ Infrastructure: EF Core persists Client aggregate to app_db
  │        (client record saved BEFORE Keycloak call — app is source of truth)
  │
  └──▶ Infrastructure: KeycloakAdminClient.CreateUserAsync()
           POST /admin/realms/{realm}/users
           (uses cached service account token via Duende.AccessTokenManagement)
  │
  ▼
201 Created returned to React SPA
  │
  ▼
React SPA redirects to /login
```

**Key decision:** Persist to `app_db` first, then create Keycloak user. If Keycloak call fails, return 500 and the record can be replayed. The application database is the authoritative record of registration intent.

### Flow 2: Login (ROPC Grant)

```
User submits credentials on React custom login form
  │
  ▼ POST /realms/{realm}/protocol/openid-connect/token
    grant_type=password
    client_id={public-client}
    username={email}
    password={password}
    (direct from React to Keycloak — API is NOT in this path)
  │
  ▼
Keycloak validates credentials, returns:
    access_token (JWT, short-lived ~5min)
    refresh_token (longer-lived)
    expires_in
  │
  ▼
React SPA stores tokens in memory (or httpOnly cookie — see Security section)
  │
  ▼
React SPA redirects to /profile
```

**WARNING (from PROJECT.md):** ROPC Grant is deprecated in OAuth 2.1. This is a conscious tradeoff. The client secret is NOT used — configure a public client in Keycloak (Direct Access Grants Enabled = true, no client secret).

### Flow 3: Authenticated Profile Fetch

```
React SPA sends GET /api/clients/me
  Authorization: Bearer {access_token}
  │
  ▼
.NET API — JwtBearer middleware validates token locally
  (fetches OIDC metadata once from Keycloak, caches signing keys — no per-request call to Keycloak)
  │
  ▼
.NET API reads subject claim (sub) from token
  │
  ▼
Infrastructure: EF Core queries app_db by external identity (Keycloak user ID or email)
  │
  ▼
Returns ClientProfileDto (read-only)
  │
  ▼
React SPA renders profile view
```

### Flow 4: Token Refresh

```
React SPA detects access_token near expiry (decode JWT exp claim)
  │
  ▼ POST /realms/{realm}/protocol/openid-connect/token
    grant_type=refresh_token
    refresh_token={stored_refresh_token}
    client_id={public-client}
  │
  ▼
Keycloak returns new access_token + refresh_token
  │
  ▼
React SPA replaces tokens in state
```

---

## .NET API — DDD Project Structure

Based on Microsoft's official DDD guidance (eShopOnContainers reference architecture):

```
src/
├── Onboarding.Domain/              ← Class library, NO framework dependencies
│   ├── Clients/
│   │   ├── Client.cs               ← Aggregate root (PF + PJ unified via type)
│   │   ├── PersonType.cs           ← Value object (PF | PJ enum)
│   │   ├── Cpf.cs                  ← Value object with self-validation
│   │   ├── Cnpj.cs                 ← Value object with self-validation
│   │   ├── Email.cs                ← Value object
│   │   ├── Phone.cs                ← Value object
│   │   ├── IClientRepository.cs    ← Repository interface (port)
│   │   └── ClientErrors.cs         ← Domain error types
│   └── SeedWork/
│       ├── Entity.cs               ← Base with Id + domain events
│       └── ValueObject.cs          ← Base with equality semantics
│
├── Onboarding.Application/         ← Class library, orchestrates domain
│   ├── Clients/
│   │   ├── RegisterClientCommand.cs
│   │   ├── RegisterClientCommandHandler.cs
│   │   ├── GetClientProfileQuery.cs
│   │   ├── GetClientProfileQueryHandler.cs
│   │   └── DTOs/
│   │       ├── RegisterClientDto.cs
│   │       └── ClientProfileDto.cs
│   └── Ports/
│       └── IKeycloakUserService.cs ← Interface for Keycloak (no dependency on Keycloak SDK here)
│
├── Onboarding.Infrastructure/      ← EF Core, Keycloak client, external concerns
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Repositories/
│   │   │   └── ClientRepository.cs
│   │   └── Configurations/
│   │       └── ClientEntityConfiguration.cs
│   ├── Keycloak/
│   │   ├── KeycloakUserService.cs  ← Implements IKeycloakUserService
│   │   └── KeycloakOptions.cs
│   └── Observability/
│       └── TelemetryExtensions.cs
│
└── Onboarding.API/                 ← ASP.NET Core project, entry point
    ├── Controllers/
    │   ├── ClientsController.cs    ← POST /api/clients, GET /api/clients/me
    │   └── HealthController.cs
    ├── Middleware/
    │   └── ExceptionHandlingMiddleware.cs
    ├── Program.cs
    └── appsettings.json
```

**Dependency rule (enforced via project references):**
- `Onboarding.Domain` has zero dependencies on other projects
- `Onboarding.Application` depends on `Onboarding.Domain` only
- `Onboarding.Infrastructure` depends on `Onboarding.Domain` + `Onboarding.Application` (for interface implementations)
- `Onboarding.API` depends on all three (wires DI, starts the host)

---

## Client Aggregate Design

```csharp
// Domain/Clients/Client.cs (sketch — not full implementation)
public class Client : Entity
{
    public PersonType PersonType { get; private set; }  // PF | PJ
    public string Name { get; private set; }            // nome / razão social
    public Email Email { get; private set; }
    public Phone Phone { get; private set; }
    public Cpf? Cpf { get; private set; }               // null when PJ
    public Cnpj? Cnpj { get; private set; }             // null when PF
    public string KeycloakUserId { get; private set; }  // set after Keycloak creation
    public DateTime RegisteredAt { get; private set; }

    // Factory methods enforce invariants
    public static Client RegisterPessoaFisica(string name, Cpf cpf, Email email, Phone phone) { ... }
    public static Client RegisterPessoaJuridica(string razaoSocial, Cnpj cnpj, Email email, Phone phone) { ... }

    public void AssignKeycloakId(string keycloakUserId) { ... }
}
```

`KeycloakUserId` is set after the Admin API call succeeds, using `AssignKeycloakId()`. This allows the repository to track whether Keycloak provisioning completed, enabling retry on failure.

---

## React Frontend Structure (Vinxi + Atomic Design)

```
frontend/
├── app.config.js           ← Vinxi configuration (router definitions)
├── src/
│   ├── client.tsx          ← Client entry point (hydration)
│   ├── server.tsx          ← SSR entry point (if using SSR mode)
│   ├── router.tsx          ← Route definitions
│   ├── components/
│   │   ├── atoms/          ← Input, Button, Label, ErrorMessage
│   │   ├── molecules/      ← FormField (label + input + error), PersonTypeSelector
│   │   ├── organisms/      ← RegistrationForm (PF), RegistrationForm (PJ), LoginForm, ProfileCard
│   │   └── templates/      ← AuthLayout, AppLayout
│   ├── pages/
│   │   ├── Register.tsx    ← /register
│   │   ├── Login.tsx       ← /login
│   │   └── Profile.tsx     ← /profile (protected)
│   ├── hooks/
│   │   ├── useAuth.ts      ← Token state, login(), logout(), refresh()
│   │   └── useClient.ts    ← Fetch profile from API
│   ├── services/
│   │   ├── authService.ts  ← ROPC token request, refresh logic
│   │   └── apiService.ts   ← Authenticated fetch to .NET API
│   └── context/
│       └── AuthContext.tsx ← Global auth state provider
```

**Vinxi routing mode:** Use `spa` router mode for this project. SSR is not required — the profile page is user-specific and not SEO-relevant. A simple SPA avoids SSR complexity.

**Token storage:** Store access token in memory (React state/context). Store refresh token in `httpOnly` cookie if the backend serves it that way, or in memory with re-login on page refresh. Do NOT store JWTs in `localStorage` (XSS vulnerability).

---

## Keycloak Admin API — Service Account Pattern

The .NET API authenticates with Keycloak Admin API using a dedicated service account (client credentials grant), not the master admin credentials.

```
Keycloak Realm: onboarding
  ├── Client: "onboarding-app"  (public, Direct Access Grants Enabled)
  │   └── Used by: React frontend (ROPC token requests)
  │
  └── Client: "onboarding-api-admin"  (confidential, Service Account Enabled)
      ├── Service Account Roles: realm-management → manage-users
      └── Used by: .NET API (Admin API calls to create users)
```

The .NET API uses `Duende.AccessTokenManagement` to manage the admin client token lifecycle:
- Requests token via client credentials grant
- Caches token until near expiry
- Auto-refreshes transparently
- Never exposes admin credentials to the frontend

---

## JWT Validation in .NET API

```
Request arrives with Authorization: Bearer {jwt}
  │
  ▼
JwtBearer middleware (AddJwtBearer)
  Authority = "http://keycloak:8180/realms/onboarding"
  Audience  = "onboarding-app"
  MetadataAddress = ".well-known/openid-configuration"
  (Keys fetched once, cached — no per-request Keycloak call)
  │
  ├── Signature valid?   → YES: continue
  │                      → NO: 401
  ├── Token expired?     → NO: continue
  │                      → YES: 401
  └── Audience matches?  → YES: set ClaimsPrincipal, proceed to controller
                         → NO: 401
```

**Configuration note:** In Docker Compose, the `Authority` must use the internal Docker network hostname (`keycloak`), but the token `iss` claim will contain the public hostname. Set `ValidateIssuer = true` and provide `ValidIssuer` matching the Keycloak public URL to avoid mismatch.

---

## Docker Compose Service Topology

```yaml
# Logical service graph (not full compose file)
services:
  app_db:         # postgres:16, volume: app_data
  keycloak_db:    # postgres:16, volume: keycloak_data
  keycloak:       # quay.io/keycloak/keycloak:26.x
                  # depends_on: keycloak_db
                  # KC_DB=postgres, KC_DB_URL=jdbc:postgresql://keycloak_db:5432/keycloak
                  # KC_HOSTNAME=http://localhost:8180 (dev) or real domain (prod)
                  # KC_HTTP_ENABLED=true (dev only — use HTTPS in prod)
  api:            # .NET 10 image
                  # depends_on: app_db, keycloak (healthcheck)
                  # ConnectionStrings__AppDb → app_db:5432
                  # Keycloak__AdminClientId → onboarding-api-admin
                  # Keycloak__AdminClientSecret → (from .env / Docker secret)
  frontend:       # Node image (Vinxi dev server or static nginx)
                  # depends_on: api
                  # VITE_API_URL=http://localhost:8080
                  # VITE_KEYCLOAK_URL=http://localhost:8180
                  # VITE_KEYCLOAK_REALM=onboarding
                  # VITE_KEYCLOAK_CLIENT_ID=onboarding-app
```

**Network isolation:** All services share one internal Docker network. Only these ports are exposed to the host:
- `8080` → API
- `5173` → Frontend
- `8180` → Keycloak (admin console + token endpoint)
- `5432` → app_db (dev convenience — close in prod)

The Keycloak admin console (`/admin`) should NOT be exposed publicly. In production, add a reverse proxy that blocks `/admin/` paths from the public internet.

---

## Observability Architecture

```
.NET API
  │
  ├── Serilog → structured log output → stdout (Docker captures) + OTLP sink
  │     enriched with: TraceId, SpanId, RequestId, UserId (from JWT sub claim)
  │
  └── OpenTelemetry SDK
        ├── Traces: ASP.NET Core instrumentation + HttpClient + EF Core
        ├── Metrics: ASP.NET Core metrics + Runtime metrics
        └── Logs: exported via OTLP (or stdout JSON for Docker Compose)

Serilog 4.x automatically captures TraceId/SpanId from Activity.Current — no
manual enricher needed. Logs and traces are correlated out of the box.
```

For Docker Compose (dev), stdout JSON is sufficient — no Jaeger/Prometheus needed in v1. OpenTelemetry SDK should be wired from day one so traces exist from the first request. Adding an exporter later is a configuration change, not a code change.

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: Calling Keycloak DB Directly
**What:** Writing to Keycloak's PostgreSQL database via direct SQL.
**Why bad:** Bypasses Keycloak's internal consistency, breaks on upgrades, unsupported.
**Instead:** Always use Keycloak Admin REST API at `/admin/realms/{realm}/users`.

### Anti-Pattern 2: Anemic Domain Model
**What:** `Client` class with only getters/setters, all logic in Application services.
**Why bad:** Business rules scattered across services, invariants unenforced, DDD value lost.
**Instead:** Rich domain model — CPF validation inside `Cpf` value object, registration rules in `Client` factory methods.

### Anti-Pattern 3: Storing JWT in localStorage
**What:** `localStorage.setItem('access_token', token)` in React.
**Why bad:** Any XSS script in the page can steal the token. XSS + localStorage = full account compromise.
**Instead:** Memory storage (React state) for access token. Refresh token in httpOnly cookie if possible, or accept re-login on page refresh.

### Anti-Pattern 4: Routing Admin Credentials Through Frontend
**What:** Frontend receives or stores Keycloak admin credentials.
**Why bad:** Exposes admin power to every browser session.
**Instead:** Admin credentials stay in .NET API environment variables only. Frontend uses public client (no secret). API uses confidential service account client.

### Anti-Pattern 5: No Health Checks in Docker Compose
**What:** `api` starts before `keycloak` is ready, fails to connect, crashes.
**Why bad:** Race condition in Docker Compose startup makes dev environment unreliable.
**Instead:** Add `healthcheck` to `keycloak` service and `depends_on: condition: service_healthy` in `api`.

### Anti-Pattern 6: Single PostgreSQL for Both App and Keycloak
**What:** One PostgreSQL instance with two databases on the same container.
**Why bad:** Keycloak schema upgrades can conflict with app migrations; backup strategies differ; isolation breaks.
**Instead:** Two separate PostgreSQL containers (`app_db`, `keycloak_db`). Slightly more memory, dramatically cleaner separation.

---

## Build Order (Phase Dependencies)

The component dependency graph determines the correct build sequence:

```
1. Infrastructure foundation
   Docker Compose skeleton → PostgreSQL (app_db + keycloak_db) → Keycloak (configured realm)

2. Backend Domain + persistence
   Domain models → EF Core migrations → Repository implementations
   (Testable without Keycloak — use in-memory db for unit tests)

3. Keycloak integration in API
   Service account client config → KeycloakUserService → RegisterClientCommandHandler
   (Integration-test with real Keycloak container)

4. API endpoints + JWT validation
   Controllers → JwtBearer config → Protected profile endpoint

5. Frontend Registration flow
   Vinxi setup → Registration form (PF/PJ) → POST to API → redirect to login

6. Frontend Authentication flow
   Login form → ROPC token request → token storage → protected route guard

7. Frontend Profile view
   Authenticated GET /api/clients/me → profile display

8. Observability
   Serilog + OTEL wiring (can be done as early as step 2 — add to API from day one)

9. Keycloak hardening
   Brute force config → admin endpoint restriction → security headers → final review
```

**Why this order:**
- Domain layer has no external dependencies — build and test first
- Keycloak must be running before any integration test that creates users
- Frontend depends on API contract being stable — build API first, then consume
- Hardening is last because it requires a working system to test against

---

## Security Boundaries Summary

| Boundary | Rule |
|----------|------|
| Frontend → Keycloak | Only token endpoint (`/token`). Never admin endpoint. |
| Frontend → API | Bearer token required for `/api/clients/me`. Registration endpoint is public. |
| API → Keycloak Admin | Service account with minimum scope (`manage-users` only). |
| API → app_db | EF Core with dedicated app user (no superuser). |
| Keycloak → keycloak_db | Keycloak internal — no application access. |
| Host → Keycloak `/admin` | Blocked from public internet in production (reverse proxy rule). |

---

## Sources

- [Designing a DDD-oriented microservice — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice) — HIGH confidence
- [Keycloak Admin REST API Reference](https://www.keycloak.org/docs-api/latest/rest-api/index.html) — HIGH confidence
- [Keycloak.AuthServices — Access Token Management (Duende pattern)](https://nikiforovall.blog/keycloak-authorization-services-dotnet/admin-rest-api/access-token.html) — MEDIUM confidence
- [Configuring Keycloak for production](https://www.keycloak.org/server/configuration-production) — HIGH confidence
- [Configure JWT Bearer authentication in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0) — HIGH confidence
- [Integrate Keycloak with ASP.NET Core — Milan Jovanovic](https://www.milanjovanovic.tech/blog/integrate-keycloak-with-aspnetcore-using-oauth-2) — MEDIUM confidence
- [Vinxi — Full Stack JavaScript SDK](https://github.com/nksaraf/vinxi) — MEDIUM confidence (official repo)
- [ROPC / Direct Grant in Keycloak](https://www.keycloak.org/docs/latest/authorization_services/index.html) — HIGH confidence (official)
- [How to Secure JWT in a SPA](https://dev.to/nilanth/how-to-secure-jwt-in-a-single-page-application-cko) — MEDIUM confidence
- [serilog-sinks-opentelemetry](https://github.com/serilog/serilog-sinks-opentelemetry) — HIGH confidence (official)
