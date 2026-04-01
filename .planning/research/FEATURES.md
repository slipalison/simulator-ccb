# Feature Landscape

**Domain:** Client Onboarding System (PF/PJ with Keycloak auth)
**Researched:** 2026-04-01
**Stack context:** .NET 10 backend, React + Vinxi frontend, PostgreSQL, Keycloak (self-hosted)

---

## Table Stakes

Features users expect. Missing = product feels incomplete or insecure.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| PF registration form (nome, CPF, email, telefone, senha) | Core product requirement; no PF path = product doesn't exist | Medium | CPF validation = check-digit algorithm (modulo 11) — must run server-side, not just client-side |
| PJ registration form (razão social, CNPJ, email, telefone, senha) | Core product requirement; same rationale | Medium | CNPJ validation = 14-digit check-digit algorithm; new alphanumeric format begins July 2026 per Receita Federal — validate both formats |
| Duplicate detection (CPF/CNPJ uniqueness) | Registering the same CPF/CNPJ twice breaks data integrity and creates auth conflicts in Keycloak | Low | Check before Keycloak user creation; return 409 Conflict with clear error message |
| Duplicate detection (email uniqueness) | Keycloak uses email as a login credential by default | Low | Validate at API layer before calling Keycloak Admin API |
| Server-side field validation | Client-side is UX convenience; server-side is security requirement; without it, malformed data reaches Keycloak and PostgreSQL | Low | All validation rules live on the server; client mirrors them for UX only |
| Post-registration redirect to login | Expected onboarding step; without it the user is stranded after registering | Low | Standard redirect pattern |
| Custom login screen (React) | Project decision: custom UI over Keycloak's default theme | Medium | Uses ROPC Grant — see security caveats in PITFALLS.md |
| JWT token receipt and storage | Without token storage the session doesn't persist | Low | Store access token + refresh token; use httpOnly cookies or in-memory + refresh rotation |
| Protected profile route | Unauthenticated access to profile page must be blocked | Low | Validate JWT on frontend; backend validates on every request |
| Read-only profile view (PF/PJ data) | Post-login destination; without it, login has no payoff | Low | Display data fetched from backend (not directly from Keycloak) |
| HTTPS enforcement | Any production-grade auth system requires HTTPS | Low | Keycloak SSL mode = "all requests"; Docker Compose can use self-signed for local dev |
| Brute force protection (Keycloak) | Keycloak ships with this disabled by default; without it, login endpoint is open to credential stuffing | Low | Enable in Realm Settings > Security Defenses > Brute Force Detection; configure max login failures and wait increment |
| Password policy enforcement | Users will use weak passwords if not prevented | Low | Configure in Keycloak: minimum length, uppercase, lowercase, digits, special characters |
| Error messages without information leakage | "User not found" vs "Wrong password" reveals account enumeration | Low | Return generic "Invalid credentials" for all auth failures |
| Structured logging (Serilog) | Required for diagnosing registration and auth failures | Low | Structured JSON logs from day one; include correlation IDs |
| Health check endpoints | Docker Compose and any monitoring needs /health to detect dead containers | Low | ASP.NET Core /healthz and Keycloak /health/ready |

---

## Differentiators

Features not universally expected but that add meaningful value for this system.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| OpenTelemetry traces + metrics | Distributed tracing across API + Keycloak interactions surfaces latency spikes in registration flow; most onboarding systems don't ship this from day one | Medium | Instrument: registration call, Keycloak Admin API call, PostgreSQL queries; export to OTLP collector |
| Atomic Design component library | Consistent UI from atoms up; easier to reskin or extend without rewriting | Medium | Enforced from first component; pay the cost once, save it repeatedly |
| DDD domain model (PF/PJ as distinct aggregates) | Clean separation of individual vs company registration logic; easy to add PJ-specific rules (e.g., responsible person, branch) later | Medium | Prevents the "everything is a User" antipattern that conflates the two |
| TDD from day one | Regression safety on critical registration and auth paths; documents expected behavior as executable tests | Medium | Unit tests on domain, integration tests on API endpoints; mandatory per project requirements |
| CNPJ alphanumeric format readiness | Receita Federal begins alphanumeric CNPJ in July 2026; validating only numeric CNPJ will break by then | Low | Accept and validate the new 14-character alphanumeric format in addition to the current numeric one |
| Keycloak Admin API integration with service account (least privilege) | Direct password grant with a superadmin is common but catastrophically insecure; service account scoped to manage-users only limits blast radius | Low | Create dedicated service account in Keycloak; grant only manage-users realm role |
| Idempotency on registration endpoint | Double-submits (network retry, impatient user) create duplicate Keycloak users or PostgreSQL constraint violations; idempotency key prevents silent duplicates | Medium | Generate idempotency token on frontend form render; check on server before processing |
| Correlation ID propagation (API → Keycloak) | When Keycloak Admin API calls fail, correlating the backend log entry with the Keycloak server log is impossible without a shared ID | Low | Inject X-Correlation-ID header in all outbound Keycloak Admin API calls |

---

## Anti-Features

Features to deliberately NOT build in v1. Each has an explicit reason.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Email verification on registration | Adds a required async step that blocks login; doubles complexity (email sending, token storage, expiry); out of scope per PROJECT.md | Accept registration immediately; add email verification as an isolated phase later when email infra exists |
| Social login (Google, GitHub, OAuth) | Keycloak supports it natively but integrating it requires configuring external providers, testing token exchange, handling account linking conflicts | Add as a separate milestone if ever needed; Keycloak makes it retrofittable |
| Profile data editing | Introduces write-back to PostgreSQL and Keycloak user attributes simultaneously; concurrency and consistency risks are non-trivial | Ship read-only first; editing is a separate, testable milestone |
| Admin dashboard / back-office | Separate access control domain (admin roles, audit views); doubles scope; Keycloak's own admin console already covers user management | Use Keycloak admin console for now; build custom back-office only when Keycloak console is insufficient |
| Mobile app / PWA | Web-first is the right default; React on Vinxi is already responsive enough for mobile browsers | Only pursue if explicit mobile-native requirement emerges |
| Push/email notifications | No event requiring notification exists in v1 (no approval workflow, no async processing) | Add when workflow steps that need notification are introduced |
| Password reset / forgot password flow | Keycloak provides this built-in via its own email flow; building a custom one duplicates work and introduces risk | Enable Keycloak's native "Forgot Password" action when email infra is ready |
| Authorization Code Flow + PKCE login | More secure than ROPC, but the project explicitly chose custom login UI with ROPC grant for v1; migrating mid-project creates rework | Document the decision; plan migration as a security milestone if threat model escalates |
| Offline tokens / remember me | Infinite-lived tokens are a stolen-credential disaster; offline sessions require secure storage guarantees the v1 frontend doesn't have | Use reasonable SSO Session Max (8-24h); let users re-authenticate |
| Wildcard redirect URIs in Keycloak clients | Convenient during dev but allows open redirect attacks; a CVE class unto itself | Register exact redirect URIs per client |

---

## Feature Dependencies

```
CPF/CNPJ field on form
  → Server-side CPF/CNPJ validation (check-digit)
    → Duplicate detection against PostgreSQL
      → User persisted in PostgreSQL
        → Keycloak user created via Admin API (after DB commit, not before)
          → Post-registration redirect to login

Custom login form
  → Keycloak ROPC token request (POST /token with username + password)
    → JWT stored on frontend (access token + refresh token)
      → Protected route check (JWT present + valid)
        → Profile API call (Authorization: Bearer <access_token>)
          → Read-only profile view rendered

Brute force protection (Keycloak config)
  → Password policy (Keycloak config)
    → HTTPS enforcement (Keycloak SSL mode)
      → All of the above work safely

OpenTelemetry traces
  → Correlation ID propagation
    → Structured logging (Serilog enriched with TraceId)
```

---

## MVP Recommendation

Prioritize in this order:

1. PF registration form + server-side validation (CPF, duplicate email check)
2. Keycloak user creation via Admin API (service account, least privilege)
3. PJ registration form + server-side validation (CNPJ, duplicate check)
4. Post-registration redirect to login
5. Custom login form + ROPC token exchange
6. JWT storage + protected route enforcement
7. Read-only profile view
8. Keycloak hardening (brute force, password policy, HTTPS, exact redirect URIs, disable request_uri SSRF vector)
9. Serilog + OpenTelemetry from first endpoint (do not defer — retrofitting structured logging is expensive)

Defer until Phase 2 or later:
- CNPJ alphanumeric format support (July 2026 deadline; implement before then)
- Idempotency keys on registration endpoint (add when QA surfaces double-submit issues)
- Email verification (requires email infra decision)
- Profile editing (separate milestone)

---

## Sources

- [Keycloak Server Administration Guide](https://www.keycloak.org/docs/latest/server_admin/)
- [Keycloak User Self-Registration — Baeldung](https://www.baeldung.com/keycloak-user-registration)
- [Keycloak Security Best Practices — hoop.dev](https://hoop.dev/blog/keycloak-security-best-practices/)
- [Keycloak Production Configuration](https://www.keycloak.org/server/configuration-production)
- [Keycloak Session Management — skycloak.io](https://skycloak.io/blog/session-management-in-keycloak-from-refresh-to-idle-timeouts/)
- [Why Deprecate ROPC Grant Type — Logto](https://blog.logto.io/deprecated-ropc-grant-type)
- [CPF Number — Wikipedia](https://en.wikipedia.org/wiki/CPF_number)
- [Brazil Tax ID (TIN) Guide — TaxDo](https://taxdo.com/resources/global-tax-id-validation-guide/brazil)
- [CNPJ Alphanumeric Format 2026 — Commenda](https://www.commenda.io/blog/brazil-cnpj-verification)
- [Keycloak SSRF CVE-2020-10770 — Acunetix](https://www.acunetix.com/vulnerabilities/web/keycloak-request_uri-ssrf-cve-2020-10770/)
- [Keycloak.AuthServices NuGet — v2.7.0](https://www.nuget.org/packages/Keycloak.AuthServices.Authentication)
- [Integrate Keycloak with ASP.NET Core — Milan Jovanovic](https://www.milanjovanovic.tech/blog/integrate-keycloak-with-aspnetcore-using-oauth-2)
- [Red Hat Keycloak Security Vulnerabilities 2025 — stack.watch](https://stack.watch/product/redhat/keycloak/)
- [CVE-2026-1518 Keycloak Blind SSRF — Red Hat Bugzilla](https://bugzilla.redhat.com/show_bug.cgi?id=2433727)
