# V3.0 Admin Backoffice — Architecture

**Domain:** Admin panel for managing client registrations
**Project:** Onboarding de Clientes
**Researched:** 2026-04-09
**Overall confidence:** HIGH (Microsoft docs + Keycloak role-based patterns + cookie auth best practices)

---

## 1. System Context

The admin backoffice is a **separate UI surface** within the existing Vinxi frontend, backed by **new admin endpoints** in the .NET API. Unlike the client-facing flow (Keycloak ROPC + Bearer JWT), admin users authenticate via **cookie-based session auth** with role-based access control enforced by Keycloak realm roles.

### What Changes vs v1/v2

| Aspect | v1/v2 (Client-facing) | v3.0 (Admin Backoffice) |
|--------|----------------------|-------------------------|
| Auth mechanism | ROPC grant → JWT in memory | Email + password → cookie (httpOnly, secure) |
| Authorization | Any authenticated user | Requires Keycloak realm role `admin` |
| Frontend route | `/profile` (protected by token) | `/admin/**` (protected by cookie + role check) |
| Backend auth | `JwtBearer` middleware | Cookie authentication + role validation |
| Token source | Keycloak token endpoint (direct from browser) | Backend validates against Keycloak, sets cookie |
| Session storage | Memory (React state) | httpOnly cookie set by backend |

### What Stays the Same

- Keycloak remains the identity provider
- The same `onboarding` realm is used
- DDD domain layer and CQRS application layer remain unchanged
- EF Core + PostgreSQL for application data
- Serilog + OpenTelemetry for observability

---

## 2. Component Boundaries

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Browser                                                                     │
│                                                                              │
│  ┌──────────────────────────┐           ┌──────────────────────────────┐    │
│  │  Client-facing SPA       │           │  Admin Backoffice SPA        │    │
│  │  /login, /register,      │           │  /admin/login                │    │
│  │  /profile                │           │  /admin/dashboard            │    │
│  │  Auth: JWT in memory     │           │  /admin/clients              │    │
│  │  Header: Bearer {jwt}    │           │  /admin/clients/{id}         │    │
│  │                          │           │  Auth: httpOnly cookie       │    │
│  └───────────┬──────────────┘           └──────────────┬───────────────┘    │
│              │                                         │                     │
│              │ GET /api/clients/me                     │ POST /admin/login   │
│              │ Authorization: Bearer {jwt}             │ GET /api/admin/...  │
│              │                                         │ Cookie: session     │
│              ▼                                         ▼                     │
└──────────────┼─────────────────────────────────────────┼─────────────────────┘
               │                                         │
┌──────────────▼─────────────────────────────────────────▼─────────────────────┐
│  .NET 10 API (ASP.NET Controllers)                                           │
│                                                                              │
│  ┌────────────────────────────┐   ┌──────────────────────────────────┐      │
│  │  Client Endpoints          │   │  Admin Endpoints                 │      │
│  │  [Authorize]               │   │  [Authorize]                     │      │
│  │  JwtBearer middleware      │   │  [RequireClaim("realm_access     │      │
│  │  GET /api/clients/me       │   │   .roles[*]", "admin")]          │      │
│  │  POST /api/clients         │   │  GET  /api/admin/clients         │      │
│  │  (public for registration) │   │  GET  /api/admin/clients/{id}    │      │
│  │                            │   │  PUT  /api/admin/clients/{id}    │      │
│  │                            │   │  POST /admin/login               │      │
│  │                            │   │  POST /admin/logout              │      │
│  └───────────────┬────────────┘   └───────────────┬──────────────────┘      │
│                  │                                 │                          │
│                  │ EF Core queries                 │ EF Core + Keycloak       │
│                  │ app_db                          │ Admin API calls          │
│                  ▼                                 ▼                          │
└────────────────┼──────────────────────────────────┼──────────────────────────┘
                 │                                  │
┌────────────────▼──────────┐    ┌──────────────────▼──────────────────────┐   │
│  PostgreSQL (app_db)      │    │  Keycloak 26.1                          │   │
│  - Client aggregates      │    │  - Realm: onboarding                    │   │
│  - PF/PJ registration data│    │  - Realm role: admin (assigned to admins│   │
│                           │    │  - Admin API: list users, get user,     │   │
│                           │    │    disable user, reset password         │   │
│                           │    │  - Brute force protection               │   │
└───────────────────────────┘    └─────────────────────────────────────────┘   │
```

### Boundary Rules

| Boundary | Rule |
|----------|------|
| Admin SPA → Admin API | Cookie with each request, no Bearer token |
| Admin API → Keycloak Admin API | Service account (`onboarding-api-admin`) with `manage-users` + `view-users` |
| Admin API → app_db | Same EF Core DbContext, read access to Client aggregates |
| Client SPA → Admin API | **Blocked** — client users lack `admin` role, 403 enforced |
| Admin API → Client endpoints | Not needed — admin endpoints are separate surface |

---

## 3. Cookie Auth Flow (Text-Based Diagram)

### 3.1 Admin Login Flow

```
Admin types email + password in /admin/login
  │
  ▼ POST /admin/login  (email, password)
  Content-Type: application/json
  (no auth header — public endpoint)
  │
  ▼
.NET API — AuthController (no [Authorize])
  │
  ├──▶ IKeycloakTokenService.ExchangePasswordAsync()
  │      POST /realms/onboarding/protocol/openid-connect/token
  │      grant_type=password
  │      client_id=onboarding-app
  │      username={admin_email}
  │      password={admin_password}
  │
  ├──▶ Decode JWT access_token (jose library or System.IdentityModel.Tokens.Jwt)
  │      Extract realm_access.roles[] claim
  │
  ├──▶ Check if "admin" role present
  │      ├── YES: continue
  │      └── NO: return 403 Forbidden ("Admin access required")
  │
  ├──▶ Create authentication session
  │      Generate session identifier
  │      Store minimal session data in server-side cache (IDistributedCache)
  │      { userId: sub, email, roles: ["admin"], issuedAt: now, expiresAt: now + 8h }
  │
  └──▶ Set httpOnly cookie
        Cookie header:
        Set-Cookie: admin_session={session_id};
          HttpOnly;
          Secure;              (true in prod, false in dev)
          SameSite=Lax;        (allows navigation, blocks cross-site POST)
          Path=/;
          Max-Age=28800;       (8 hours = SSO Session Max from Keycloak config)
          samesite=Lax
  │
  ▼
200 OK → { email, name, roles: ["admin"] }
  │
  ▼
React SPA redirects to /admin/dashboard
```

### 3.2 Subsequent Admin Request Flow

```
Browser navigates to /admin/dashboard
  │
  ▼ GET /api/admin/clients
  Cookie: admin_session={session_id}
  │
  ▼
.NET API — Custom cookie auth middleware
  │
  ├──▶ Read admin_session cookie value
  │
  ├──▶ Look up session in IDistributedCache
  │      ├── Found: continue
  │      └── Not found/expired: 401 Unauthorized → redirect to /admin/login
  │
  ├──▶ Validate session not expired
  │
  ├──▶ Check session.roles contains "admin"
  │      ├── YES: set HttpContext.User with claims from session
  │      └── NO: 403 Forbidden
  │
  └──▶ Proceed to AdminClientsController
        │
        ├──▶ GET /api/admin/clients → query all clients from app_db
        │     (optional: enrich with Keycloak user status via Admin API)
        │
        └──▶ Return JSON list of clients
  │
  ▼
React SPA renders admin client list
```

### 3.3 Admin Logout Flow

```
Admin clicks Logout in /admin/dashboard
  │
  ▼ POST /admin/logout
  Cookie: admin_session={session_id}
  │
  ▼
.NET API — AuthController
  │
  ├──▶ Remove session from IDistributedCache
  │
  └──▶ Clear cookie
        Set-Cookie: admin_session=;
          HttpOnly;
          Secure;
          SameSite=Lax;
          Path=/;
          Expires=Thu, 01 Jan 1970 00:00:00 GMT;
          Max-Age=0
  │
  ▼
200 OK
  │
  ▼
React SPA redirects to /admin/login
```

### 3.4 Session Refresh / Keep-Alive Flow

```
Admin is working in /admin/dashboard
  │
  ▼ Periodic keep-alive (every 5 minutes, or on each API call)
  GET /api/admin/session/refresh
  Cookie: admin_session={session_id}
  │
  ▼
.NET API
  │
  ├──▶ Read cookie, look up session
  │
  ├──▶ Update session expiry in cache (sliding expiration)
  │
  └──▶ Return 200 { expiresAt: "..." }
  │
  ▼
Session remains valid. If no activity for 8h → session expires, admin must re-login.
```

---

## 4. Role-Based Authorization Flow

### 4.1 Keycloak Role Assignment

```
Keycloak Realm: onboarding
  ├── Realm Roles:
  │     ├── admin        ← assigned to admin users
  │     └── client       ← assigned to regular clients (optional, not used in v1/v2)
  │
  └── Users:
        ├── admin@company.com
        │     └── Realm Roles: admin
        │
        └── client-user@email.com
              └── Realm Roles: (none, or client)
```

The `admin` role is assigned manually via Keycloak Admin Console or via API during initial setup. It is **not** auto-assigned during registration.

### 4.2 Backend Role Enforcement Flow

```
Request arrives at admin endpoint (e.g., GET /api/admin/clients)
  │
  ▼ Cookie middleware authenticates session (see 3.2)
  │
  ▼ HttpContext.User populated with claims:
      - sub (Keycloak user ID)
      - email
      - roles: ["admin"]
  │
  ▼ [Authorize] attribute passes (valid session exists)
  │
  ▼ [RequireClaim("roles", "admin")] or custom policy "AdminOnly"
  │      Check if User has role claim "admin"
  │      ├── YES: controller action executes
  │      └── NO: 403 Forbidden
  │
  ▼ Controller action runs with admin context
```

### 4.3 Authorization Policy Setup in .NET

```csharp
// In Program.cs (after AddAuthentication, before building app)
builder.Services.AddAuthorization(options =>
{
    // Existing policy for client-facing endpoints (any authenticated user)
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // Admin-only policy — requires "admin" role from Keycloak realm
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("roles", "admin"));
        // Alternative: policy.RequireRole("admin")
});
```

Controllers use `[Authorize(Policy = "AdminOnly")]` for all admin endpoints.

### 4.4 Frontend Role-Based Route Guard

```
React AdminRoute component wraps /admin/** routes
  │
  ▼ On mount, GET /api/admin/session/current
  │      Cookie: admin_session={session_id}
  │
  ├──▶ 200 OK → { roles: ["admin"] }
  │      └── Render child component (admin page)
  │
  ├──▶ 401 Unauthorized → session missing/expired
  │      └── Redirect to /admin/login
  │
  └──▶ 403 Forbidden → session valid but no admin role
        └── Redirect to /unauthorized (or /profile)
```

---

## 5. Data Flow for Admin Operations

### 5.1 List All Clients

```
Admin opens /admin/clients
  │
  ▼ GET /api/admin/clients
  Cookie: admin_session=...
  │
  ▼ AdminClientsController.GetAllAsync()
  │
  ├──▶ EF Core: SELECT * FROM Clients ORDER BY RegisteredAt DESC
  │      (with pagination: Skip/Take, total count)
  │
  └──▶ Map to AdminClientListDto:
        { id, name, personType, email, registeredAt, keycloakUserId }
  │
  ▼ Return JSON → React renders table
```

### 5.2 View Client Details

```
Admin clicks client → /admin/clients/{id}
  │
  ▼ GET /api/admin/clients/{id}
  Cookie: admin_session=...
  │
  ▼ AdminClientsController.GetByIdAsync(id)
  │
  ├──▶ EF Core: SELECT * FROM Clients WHERE Id = {id}
  │      Not found → 404
  │
  ├──▶ (Optional) Keycloak Admin API: GET /admin/realms/{realm}/users/{keycloakUserId}
  │      Enrich with Keycloak user status (enabled, emailVerified, lastLogin)
  │
  └──▶ Map to AdminClientDetailDto:
        { id, personType, name, email, phone, cpf/cnpj, registeredAt,
          keycloakStatus: { enabled, emailVerified, lastLogin, createdAt } }
  │
  ▼ Return JSON → React renders detail view
```

### 5.3 Disable/Enable Client Account

```
Admin toggles account status → confirm dialog
  │
  ▼ PUT /api/admin/clients/{id}/status
  Cookie: admin_session=...
  Body: { enabled: false }
  │
  ▼ AdminClientsController.UpdateStatusAsync(id, enabled)
  │
  ├──▶ Keycloak Admin API: PUT /admin/realms/{realm}/users/{keycloakUserId}
  │      Body: { enabled: false }
  │      (uses service account token via Duende.AccessTokenManagement)
  │
  └──▶ Log audit event: "Admin {adminEmail} {disabled|enabled} client {clientId}"
  │
  ▼ Return 200 → React updates UI
```

### 5.4 Reset Client Password (Admin-Initiated)

```
Admin clicks "Reset Password" on client detail page
  │
  ▼ POST /api/admin/clients/{id}/reset-password
  Cookie: admin_session=...
  │
  ▼ AdminClientsController.ResetPasswordAsync(id)
  │
  ├──▶ Keycloak Admin API: PUT /admin/realms/{realm}/users/{keycloakUserId}/reset-password
  │      Body: { type: "password", value: null, temporary: true }
  │      (triggers Keycloak email with reset link if email configured)
  │
  └──▶ Log audit event: "Admin {adminEmail} triggered password reset for client {clientId}"
  │
  ▼ Return 200 → React shows confirmation
```

---

## 6. Suggested Build Order with Dependency Reasoning

```
Phase 1: Keycloak Admin Role Setup
  ├── Create "admin" realm role in Keycloak
  ├── Assign role to admin user(s)
  └── Verify role appears in access_token (decode JWT, check realm_access.roles)
  Dependency: None — pure Keycloak configuration
  Why first: Admin role must exist before any backend/frontend admin code can work.

Phase 2: Cookie Auth Middleware in .NET API
  ├── Add cookie authentication scheme (CookieAuthenticationDefaults.AuthenticationScheme)
  ├── Create POST /admin/login endpoint (validates creds against Keycloak, checks admin role, sets cookie)
  ├── Create POST /admin/logout endpoint (clears session + cookie)
  ├── Create GET /api/admin/session/current endpoint (returns session info)
  ├── Configure IDistributedCache for session storage (already present in Program.cs)
  └── Unit tests: login with valid admin creds, login with non-admin (403), login with wrong creds (401)
  Dependency: Phase 1 (admin role must exist)
  Why second: Cookie auth is the foundation — all admin endpoints depend on it.

Phase 3: Authorization Policy for Admin
  ├── Add "AdminOnly" authorization policy in Program.cs
  ├── Create AdminApiController base class or filter (applies [Authorize(Policy = "AdminOnly")])
  └── Unit tests: access admin endpoint with client session (403), access with admin session (200)
  Dependency: Phase 2 (cookie auth must work)
  Why third: Policy enforcement must be in place before any admin endpoints are created.

Phase 4: Admin API Endpoints (Backend)
  ├── AdminClientsController with [Authorize(Policy = "AdminOnly")]
  ├── GET /api/admin/clients (list with pagination)
  ├── GET /api/admin/clients/{id} (detail with Keycloak enrichment)
  ├── PUT /api/admin/clients/{id}/status (enable/disable via Keycloak)
  ├── POST /api/admin/clients/{id}/reset-password (trigger Keycloak reset)
  └── Integration tests with Testcontainers (PostgreSQL + Keycloak)
  Dependency: Phase 3 (authorization policy), Phase 2 (cookie auth), existing Domain + Infrastructure layers
  Why fourth: Controllers need auth + authorization to be functional first.

Phase 5: Frontend Admin Foundation
  ├── Add /admin/* routes to TanStack Router (admin layout, login page, dashboard)
  ├── Create AdminRoute guard component (calls /api/admin/session/current)
  ├── Create admin layout template (sidebar + content area)
  ├── Create admin login page (simple email + password form)
  └── E2E test: navigate to /admin/login, submit valid admin creds, redirect to /admin/dashboard
  Dependency: Phase 2 (cookie login endpoints exist), Phase 4 (session endpoint exists)
  Why fifth: Frontend needs backend endpoints to be functional before building UI.

Phase 6: Admin Client List Page
  ├── AdminClientListPage component (table with search, pagination, filters)
  ├── Admin client API service (GET /api/admin/clients)
  └── E2E test: admin logs in, sees client list, clicks client
  Dependency: Phase 5 (admin layout + routing), Phase 4 (admin list endpoint)
  Why sixth: First admin page — depends on all infrastructure being ready.

Phase 7: Admin Client Detail Page
  ├── AdminClientDetailPage component (read-only PF/PJ data + Keycloak status)
  ├── Admin client detail API service (GET /api/admin/clients/{id})
  └── E2E test: admin views client details, sees Keycloak status
  Dependency: Phase 6 (list page pattern established), Phase 4 (detail endpoint)

Phase 8: Admin Actions (Disable, Reset Password)
  ├── Disable/enable toggle UI with confirmation dialog
  ├── Reset password button with confirmation
  └── E2E test: admin disables client, verifies in Keycloak
  Dependency: Phase 7 (detail page exists), Phase 4 (status + reset endpoints)

Phase 9: Cookie Session Management
  ├── Sliding expiration logic in middleware (refresh expiry on each request)
  ├── Auto-redirect on cookie expiry (detect 401, redirect to /admin/login)
  └── E2E test: session expires → admin redirected to login
  Dependency: Phase 5 (cookie auth works), Phase 6+ (admin pages exist to test session expiry)

Phase 10: Observability + Hardening for Admin
  ├── Serilog enrichment: add adminEmail to all admin request logs
  ├── OpenTelemetry spans: admin login, admin client list, admin actions
  ├── Rate limiting on /admin/login (prevent brute force at API level, in addition to Keycloak)
  └── Security review: verify httpOnly cookie flags, SameSite, Secure in prod
  Dependency: All admin features functional
  Why last: Hardening requires working system to test against.
```

### Build Order Summary

```
1. Keycloak Admin Role           (config only)
2. Cookie Auth Middleware        (backend foundation)
3. Admin Authorization Policy    (security gate)
4. Admin API Endpoints           (backend CRUD)
5. Frontend Admin Foundation     (routing, guards, login)
6. Admin Client List             (first page)
7. Admin Client Detail           (second page)
8. Admin Actions                 (disable, reset password)
9. Cookie Session Management     (sliding expiration, expiry handling)
10. Observability + Hardening    (logging, rate limiting, security review)
```

**Critical path:** 1 → 2 → 3 → 4 → 5 → 6. Phases 7-10 can be parallelized or reordered based on priority, but all depend on 1-5.

---

## 7. Integration Points with Existing v1/v2 Architecture

### 7.1 Program.cs Modifications

The existing `Program.cs` already has:
- `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)` for client-facing JWT auth
- `AddAuthorization()` with default policy
- `IDistributedMemoryCache` for Duende token cache and idempotency
- CORS configured for frontend with `AllowCredentials`

**What to add:**

```
Authentication:
  └── AddScheme<CookieAuthenticationOptions, CookieAuthenticationHandler>(
        "AdminCookie", ...)  // separate scheme from JwtBearer

Authorization:
  └── AddPolicy("AdminOnly", policy => policy.RequireClaim("roles", "admin"))

CORS:
  └── Already configured with AllowCredentials — no change needed
```

**No conflicts:** Cookie auth and JWT Bearer auth coexist. The `[Authorize(AuthenticationSchemes = "AdminCookie")]` attribute on admin controllers ensures the correct scheme is used.

### 7.2 Existing Application Layer Reuse

The existing CQRS structure is reused:

```
Onboarding.Application/
├── Clients/
│   ├── Queries/
│   │   ├── GetAllClientsQuery.cs         ← NEW (admin)
│   │   ├── GetClientByIdQuery.cs         ← NEW (admin)
│   │   └── GetClientProfileQuery.cs      ← EXISTING (client-facing)
│   ├── Commands/
│   │   ├── RegisterClientCommand.cs      ← EXISTING
│   │   └── UpdateClientStatusCommand.cs  ← NEW (admin)
│   └── DTOs/
│       ├── ClientProfileDto.cs           ← EXISTING
│       ├── AdminClientListDto.cs         ← NEW (admin)
│       └── AdminClientDetailDto.cs       ← NEW (admin)
├── Admin/
│   ├── Commands/
│   │   ├── AdminLoginCommand.cs          ← NEW
│   │   └── AdminLogoutCommand.cs         ← NEW
│   ├── Queries/
│   │   └── GetAdminSessionQuery.cs       ← NEW
│   └── DTOs/
│       ├── AdminLoginResult.cs           ← NEW
│       └── AdminSessionInfo.cs           ← NEW
└── Ports/
    └── IKeycloakUserService.cs           ← EXISTING (extended with admin methods)
```

**Domain layer remains untouched:** No new domain models needed. Admin operations work on existing `Client` aggregates. The `Client` entity already has `KeycloakUserId`, which is all that's needed for Keycloak Admin API lookups.

### 7.3 Infrastructure Layer Extensions

```
Onboarding.Infrastructure/
├── Persistence/
│   ├── Repositories/
│   │   ├── ClientRepository.cs           ← EXISTING (extended with GetAll, GetById)
│   │   └── AdminClientRepository.cs      ← NEW (or extend existing)
│   └── AppDbContext.cs                   ← UNCHANGED (same DbSet<Client>)
├── Keycloak/
│   ├── KeycloakUserService.cs            ← EXTENDED (disableUser, resetPassword)
│   └── KeycloakAdminClient.cs            ← EXISTING (used for admin operations)
└── Session/
    └── DistributedCacheSessionStore.cs   ← NEW (session CRUD on IDistributedCache)
```

### 7.4 Frontend Integration with Existing Vinxi Structure

```
frontend/src/
├── router.tsx                            ← EXTENDED (admin routes)
├── components/
│   ├── pages/
│   │   ├── LoginPage.tsx                 ← EXISTING
│   │   ├── ProfilePage.tsx               ← EXISTING
│   │   ├── AdminLoginPage.tsx            ← NEW
│   │   ├── AdminDashboardPage.tsx        ← NEW
│   │   ├── AdminClientListPage.tsx       ← NEW
│   │   └── AdminClientDetailPage.tsx     ← NEW
│   ├── guards/
│   │   ├── ProtectedRoute.tsx            ← EXISTING (JWT-based, for client routes)
│   │   └── AdminRoute.tsx                ← NEW (cookie-based, for admin routes)
│   ├── organisms/
│   │   ├── AdminSidebar.tsx              ← NEW
│   │   ├── ClientTable.tsx               ← NEW
│   │   └── ClientDetailCard.tsx          ← NEW
│   └── templates/
│       ├── AppLayout.tsx                 ← EXISTING (client-facing)
│       └── AdminLayout.tsx               ← NEW (sidebar + content)
├── lib/
│   ├── auth-context.tsx                  ← EXISTING (client JWT auth)
│   ├── admin-auth-context.tsx            ← NEW (admin session)
│   └── api-client.ts                     ← EXTENDED (cookie-aware fetch for admin)
└── routes/
    └── admin/
        ├── login.tsx                     ← NEW (if using file-based routing)
        ├── dashboard.tsx
        ├── clients/
        │   ├── index.tsx
        │   └── $id.tsx
        └── unauthorized.tsx
```

### 7.5 Docker Compose — No Changes Required

The existing Docker Compose setup already exposes:
- API on `:8080` (internal: `api:8080`)
- Frontend on `:5173` (internal: `frontend:5173`)
- Keycloak on `:8180` (internal: `keycloak:8080`)
- PostgreSQL on `:5432` (internal: `app_db:5432`)

Admin functionality uses the **same services**. No new containers needed. The only infrastructure change is ensuring the `admin` realm role exists in Keycloak's realm import or initial configuration.

### 7.6 Keycloak Realm Configuration

The existing realm import (or Keycloak initialization) must be extended:

```json
{
  "realm": "onboarding",
  "roles": {
    "realm": [
      {
        "name": "admin",
        "description": "Admin backoffice access",
        "composite": false,
        "clientRole": false
      }
    ]
  },
  "users": [
    {
      "username": "admin@company.com",
      "email": "admin@company.com",
      "enabled": true,
      "credentials": [{ "type": "password", "value": "${ADMIN_PASSWORD}", "temporary": false }],
      "realmRoles": ["admin"]
    }
  ]
}
```

**Important:** The admin user should be pre-created during environment setup, NOT during client registration. It is a privileged account that exists outside the normal client lifecycle.

---

## 8. Security Considerations

### 8.1 Cookie Security Properties

| Property | Value | Rationale |
|----------|-------|-----------|
| `HttpOnly` | `true` | Prevents JavaScript access — XSS cannot steal the cookie |
| `Secure` | `false` (dev), `true` (prod) | Requires HTTPS in production. Dev uses HTTP for local convenience |
| `SameSite` | `Lax` | Blocks cross-site POST attacks, allows same-site navigation and form POSTs |
| `Path` | `/` | Cookie sent with all paths — admin and non-admin |
| `Max-Age` | `28800` (8 hours) | Matches Keycloak SSO Session Max. Admin must re-login after 8h of inactivity |
| Session ID | Random 32+ byte GUID | Unpredictable, generated server-side |

### 8.2 Threat Model

| Threat | Mitigation |
|--------|-----------|
| XSS steals admin cookie | `HttpOnly` flag prevents JavaScript access |
| CSRF attack from another site | `SameSite=Lax` blocks cross-site requests with cookies. For extra safety, add CSRF token to admin forms |
| Brute force on /admin/login | Keycloak brute force protection (already enabled: 5 failures, 30s lockout). Add API-level rate limiting (Phase 10) |
| Session fixation | Generate new session ID on each login, invalidate old session |
| Session hijacking via network sniffing | `Secure` flag in production (HTTPS required). Dev uses HTTP on localhost (safe) |
| Admin role escalation | Admin role assigned only in Keycloak Admin Console. API validates role from Keycloak token at login time. Role cannot be self-assigned |

### 8.3 Admin API vs Client API — No Cross-Contamination

```
Client endpoints:  /api/clients/**        → JwtBearer auth  → [Authorize] (any authenticated user)
Admin endpoints:   /api/admin/**          → Cookie auth     → [Authorize(Policy = "AdminOnly")]

A client user with a valid JWT CANNOT call /api/admin/** — wrong auth scheme + no admin role.
An admin with only a cookie CANNOT call /api/clients/me — wrong auth scheme (needs Bearer JWT).
```

This separation is enforced at the middleware level. Each controller specifies its authentication scheme explicitly.

---

## 9. Token Refresh Strategy (Admin Session)

Unlike the client-facing flow (JWT access token + refresh token from Keycloak), admin sessions use **server-managed sessions**:

```
Session lifecycle:
  ├── Created: on successful /admin/login (Keycloak ROPC + admin role check)
  ├── Validated: on each admin API request (cookie read + cache lookup)
  ├── Refreshed: sliding expiration (extend expiry on each request)
  └── Destroyed: on /admin/logout or expiry (8h since last activity)
```

**Why not use Keycloak refresh tokens for admin sessions?**
- Refresh tokens are designed for client-side token renewal, not server-side session management
- Server-side sessions give full control over expiry, invalidation, and audit
- Admin sessions need role validation at creation time, not on every request (roles are cached in the session)

**Keycloak token usage in admin flow:**
- Access token from ROPC exchange is used **once** during login to validate credentials and extract roles
- After login, the session is fully server-managed — no further Keycloak interaction for auth
- Keycloak Admin API is used for admin operations (disable, reset password) — this uses the service account, not the admin user's token

---

## 10. Recommended NuGet Packages (Admin-Specific)

| Package | Purpose | License | Confidence |
|---------|---------|---------|------------|
| `Microsoft.AspNetCore.Authentication.Cookies` | Cookie auth middleware | Apache 2.0 (Microsoft) | HIGH |
| `System.IdentityModel.Tokens.Jwt` | JWT decoding (extract roles from Keycloak token at login) | MIT (Microsoft) | HIGH |
| Existing: `Duende.AccessTokenManagement` | Service account token cache (Keycloak Admin API calls) | Apache 2.0 | MEDIUM |
| Existing: `Keycloak.AuthServices.Sdk` | Keycloak Admin API client | Apache 2.0 | MEDIUM |

**No new third-party dependencies needed** beyond what's already in the project. JWT decoding uses built-in .NET libraries.

---

## Sources

- [Cookie Authentication in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie) — HIGH confidence
- [Claim-Based Authorization in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims) — HIGH confidence
- [Policy-Based Authorization — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies) — HIGH confidence
- [Keycloak Realm Roles — Keycloak Documentation](https://www.keycloak.org/docs/latest/server_admin/#realm-roles) — HIGH confidence
- [OWASP Session Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html) — HIGH confidence
- [OWASP Authentication Cheat Sheet — Cookie vs Token](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html) — HIGH confidence
- [Secure Cookie Best Practices — MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Cookies#security) — HIGH confidence
- [Protect against CSRF in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery) — HIGH confidence
- [IDistributedCache in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed) — HIGH confidence
