# Pitfalls & Common Mistakes

**Domain:** Client Onboarding System with Keycloak
**Researched:** 2026-04-01
**Reference:** https://vantico.com.br/guia-para-keycloak-e-vulnerabilidades/

---

## Critical Security Pitfalls

### PITFALL-01: Keycloak SSRF via `request_uri` (CVE-2020-10770, CVE-2026-1518)

**Risk:** HIGH
**What:** Keycloak's OIDC `request_uri` parameter can be exploited to make Keycloak send HTTP requests to internal services (SSRF). Attacker sends crafted `request_uri` pointing to internal Docker network addresses.
**Warning signs:** Unexpected outbound requests from Keycloak container. Internal services receiving requests they didn't initiate.
**Prevention:**
- Disable `request_uri` support: Set `--spi-login-protocol-openid-connect-request-uri-enabled=false` in Keycloak startup
- Network segmentation: Keycloak should not have access to services it doesn't need
- Monitor Keycloak outbound traffic
**Phase:** Infrastructure setup (Phase 1)

### PITFALL-02: Open Redirect via Wildcard Redirect URIs

**Risk:** HIGH
**What:** Configuring `*` or overly broad redirect URIs in Keycloak client settings allows attackers to redirect users to malicious sites after authentication, stealing tokens.
**Warning signs:** Client configuration has `*` or `http://localhost:*` as redirect URI.
**Prevention:**
- Register exact redirect URIs: `http://localhost:5173/callback` (not `http://localhost:*`)
- No wildcards in production
- Review redirect URI list on each deployment
**Phase:** Infrastructure setup (Phase 1)

### PITFALL-03: Keycloak Admin Console Exposed Publicly

**Risk:** HIGH
**What:** Keycloak's `/admin` and `/admin/master/console` paths are accessible from outside Docker network. Attackers can brute-force admin credentials.
**Warning signs:** Port 8180 exposed on 0.0.0.0 with no path filtering.
**Prevention:**
- Docker Compose: Bind Keycloak to `127.0.0.1:8180` only (not `0.0.0.0`)
- Production: Reverse proxy blocks `/admin/*` from public internet
- Use strong admin password (min 20 chars, random)
- Enable admin MFA if available
**Phase:** Infrastructure setup (Phase 1)

### PITFALL-04: Brute Force Not Enabled by Default

**Risk:** HIGH
**What:** Keycloak ships with brute force detection DISABLED. Login endpoint accepts unlimited attempts.
**Warning signs:** No lockout after 100+ failed login attempts.
**Prevention:**
- Enable in Realm Settings > Security Defenses > Brute Force Detection
- Max login failures: 5
- Wait increment: 30 seconds
- Max wait: 15 minutes
- Failure reset time: 12 hours
**Phase:** Infrastructure setup (Phase 1)

### PITFALL-05: ROPC Grant Security Implications

**Risk:** MEDIUM
**What:** Resource Owner Password Credentials grant sends username+password directly to token endpoint. Deprecated in OAuth 2.1. Cannot support MFA. Credentials transit through JavaScript.
**Warning signs:** Using ROPC but planning to add MFA later (incompatible). Storing credentials in React state during submission.
**Prevention:**
- Document this as a conscious tradeoff (already done in PROJECT.md)
- Never log credentials on the client side
- Ensure Keycloak token endpoint is HTTPS in production
- Plan migration path to Authorization Code Flow + PKCE if security requirements increase
- Short access token lifetime (5 min) to limit exposure window
**Phase:** Authentication implementation

### PITFALL-06: Admin API Credentials in Frontend/Logs

**Risk:** HIGH
**What:** Keycloak Admin API client secret leaks to frontend code or appears in logs.
**Warning signs:** `KEYCLOAK_ADMIN_SECRET` in frontend `.env`. Admin secret visible in Serilog output.
**Prevention:**
- Admin credentials ONLY in .NET API environment variables
- Frontend uses public client (no secret)
- Serilog destructuring: exclude sensitive fields with `[LogMasked]` or enricher filters
- Never log request bodies containing passwords
**Phase:** All phases — enforce from day one

---

## Architectural Pitfalls

### PITFALL-07: Single PostgreSQL for App + Keycloak

**Risk:** MEDIUM
**What:** Running both app data and Keycloak data in one PostgreSQL instance. Schema conflicts on upgrade, backup complexity, blast radius.
**Warning signs:** `docker-compose.yml` has one `postgres` service used by both.
**Prevention:**
- Two separate PostgreSQL containers: `app_db` and `keycloak_db`
- Independent backup/restore strategies
- Independent version upgrades
**Phase:** Infrastructure setup (Phase 1)

### PITFALL-08: Keycloak User Created Before App DB Commit

**Risk:** MEDIUM
**What:** Creating the Keycloak user first, then persisting to app_db. If app_db insert fails, orphan user exists in Keycloak with no matching app record.
**Warning signs:** Registration endpoint calls Keycloak Admin API before EF Core SaveChanges.
**Prevention:**
- Persist to app_db FIRST (source of truth for registration intent)
- Then create Keycloak user
- If Keycloak fails: app record has no `KeycloakUserId` — can be retried
- If app_db fails: nothing was created in Keycloak — clean state
**Phase:** Registration API implementation

### PITFALL-09: Anemic Domain Model

**Risk:** LOW (but high long-term cost)
**What:** `Client` class with only getters/setters. All validation in Application service. DDD in name only.
**Warning signs:** `Client.Cpf` is a `string`, not a value object. No factory methods. All logic in handlers.
**Prevention:**
- CPF, CNPJ, Email, Phone as value objects with self-validation
- Factory methods on Client: `RegisterPessoaFisica()`, `RegisterPessoaJuridica()`
- Domain layer has ZERO framework dependencies
- TDD: test domain logic in isolation (no database, no HTTP)
**Phase:** Domain layer implementation

### PITFALL-10: Docker Compose Startup Race Conditions

**Risk:** MEDIUM
**What:** API starts before Keycloak is ready. EF Core migration runs before PostgreSQL accepts connections. Random failures on `docker compose up`.
**Warning signs:** "Connection refused" errors on first startup that go away after restart.
**Prevention:**
- Keycloak healthcheck: `curl -f http://localhost:8080/health/ready || exit 1`
- PostgreSQL healthcheck: `pg_isready -U postgres`
- API `depends_on` with `condition: service_healthy`
- EF Core migration retry policy (Npgsql EnableRetryOnFailure)
**Phase:** Infrastructure setup (Phase 1)

---

## Frontend Pitfalls

### PITFALL-11: JWT Stored in localStorage

**Risk:** HIGH
**What:** Storing access/refresh tokens in `localStorage`. Any XSS vulnerability (even from a third-party dependency) can steal all tokens.
**Warning signs:** `localStorage.setItem('access_token', ...)` anywhere in code.
**Prevention:**
- Store access token in React memory (state/context)
- Accept that page refresh requires re-authentication (or use refresh token flow)
- Never use localStorage or sessionStorage for tokens
**Phase:** Frontend authentication

### PITFALL-12: Client-Side Only Validation

**Risk:** MEDIUM
**What:** CPF/CNPJ validation only in React. Attacker bypasses frontend, sends invalid data directly to API.
**Warning signs:** No FluentValidation rules on backend DTOs. Validation only in React Hook Form.
**Prevention:**
- ALL validation runs server-side (FluentValidation on commands)
- Client-side validation is a UX convenience, not a security measure
- Server returns 422 with field-level errors for the frontend to display
**Phase:** Registration API + Frontend

### PITFALL-13: Information Leakage in Error Responses

**Risk:** MEDIUM
**What:** "User with this CPF already exists" reveals account enumeration. Stack traces in production errors reveal internal architecture.
**Warning signs:** Different error messages for "user not found" vs "wrong password". Exception details in 500 responses.
**Prevention:**
- Generic auth errors: "Invalid credentials" for all login failures
- Registration: "Unable to complete registration" (not "CPF already registered")
- ExceptionHandlingMiddleware strips stack traces in non-Development environments
- Problem Details (RFC 7807) for all error responses
**Phase:** All phases — enforce from day one

---

## Keycloak-Specific Configuration Pitfalls

### PITFALL-14: Default Keycloak Realm Settings

**Risk:** MEDIUM
**What:** Using Keycloak defaults which are optimized for ease of setup, not security.
**Prevention checklist:**
- [ ] Password policy configured (not default empty policy)
- [ ] Brute force detection enabled (default: disabled)
- [ ] Session timeouts configured (default: 30 min SSO, too long)
- [ ] Access token lifespan reduced (default: 5 min — OK, verify)
- [ ] Refresh token rotation enabled (SSO Session Idle: lower than Max)
- [ ] Unused flows disabled (e.g., implicit flow)
- [ ] Client scope minimized (only necessary scopes)
**Phase:** Infrastructure setup (Phase 1)

### PITFALL-15: Keycloak Header Injection

**Risk:** MEDIUM
**What:** Keycloak passes certain headers to backend services. If Keycloak is behind a reverse proxy that doesn't sanitize headers, attackers can inject `X-Forwarded-For`, `X-Forwarded-Proto` to bypass security checks.
**Prevention:**
- Configure Keycloak proxy mode correctly: `KC_PROXY_HEADERS=forwarded` or `xforwarded`
- Reverse proxy must strip/override forwarded headers from client requests
- In Docker Compose dev: set `KC_PROXY_HEADERS=xforwarded` if behind a proxy, otherwise omit
**Phase:** Infrastructure setup

---

## Summary — Phase Mapping

| Phase | Pitfalls to Address |
|-------|-------------------|
| Infrastructure Setup | PITFALL-01, 02, 03, 04, 07, 10, 14, 15 |
| Domain Layer | PITFALL-09 |
| Registration API | PITFALL-06, 08, 12, 13 |
| Authentication | PITFALL-05, 06, 11, 13 |
| All Phases | PITFALL-06, 13 (cross-cutting) |

---

## Sources

- [Guia para Keycloak e Vulnerabilidades — Vantico](https://vantico.com.br/guia-para-keycloak-e-vulnerabilidades/)
- [CVE-2020-10770 — Keycloak SSRF via request_uri](https://www.acunetix.com/vulnerabilities/web/keycloak-request_uri-ssrf-cve-2020-10770/)
- [CVE-2026-1518 — Keycloak Blind SSRF](https://bugzilla.redhat.com/show_bug.cgi?id=2433727)
- [Keycloak Security Best Practices — hoop.dev](https://hoop.dev/blog/keycloak-security-best-practices/)
- [Keycloak Production Configuration](https://www.keycloak.org/server/configuration-production)
- [OWASP Token Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/HTML5_Security_Cheat_Sheet.html)
- [Why Deprecate ROPC — Logto](https://blog.logto.io/deprecated-ropc-grant-type)
- [Red Hat Keycloak Security Vulnerabilities 2025 — stack.watch](https://stack.watch/product/redhat/keycloak/)
