# V3.0 Admin Backoffice — Pitfalls & Common Mistakes

**Domain:** Admin panel for managing client registrations
**Project:** Onboarding de Clientes v3.0
**Researched:** 2026-04-09
**Scope:** Admin backoffice only. Does NOT re-cover v1/v2 pitfalls (documented in `/planning/research/PITFALLS.md`).

---

## How to Read This Document

Each pitfall includes:
- **Risk:** HIGH / MEDIUM / LOW — severity if triggered
- **What:** The trap and why teams fall into it
- **Warning Signs:** Observable symptoms that the pitfall is manifesting
- **Prevention Strategy:** Concrete actions to avoid or mitigate
- **Phase:** Which v3.0 build phase should address it (see `ARCHITECTURE.md` section 6)

---

# 1. SECURITY PITFALLS

---

### SEC-01: Cookie Without Proper Security Flags

**Risk:** HIGH

**What:** Issuing the admin session cookie without `HttpOnly`, `Secure`, `SameSite`, or `Path` flags. `HttpOnly` prevents JavaScript access (XSS theft vector). `Secure` ensures transmission only over HTTPS. `SameSite=Lax` blocks cross-site POST forgery. Without these, the admin session is trivially stealable.

**Warning Signs:**
- `Set-Cookie: admin_session=abc123` with no flags in response headers
- Cookie readable via `document.cookie` in browser dev tools
- Cookie sent on cross-origin requests visible in Network tab
- `Secure: false` deployed to any environment with HTTPS

**Prevention Strategy:**
```
Cookie configuration in .NET:
  - HttpOnly = true          (always — no exceptions)
  - Secure = true            (prod/staging); false (dev only)
  - SameSite = SameSiteMode.Lax  (blocks cross-site POST, allows navigation)
  - Path = "/"
  - MaxAge = 8h (aligned with Keycloak SSO Session Max)
  - Consider __Host- prefix for domain-scoped protection:
    cookie name = "__Host-admin_session"
```
- Use `CookieBuilder` in ASP.NET Core to enforce flags per environment
- Integration test: assert cookie flags on login response
- Never relax `HttpOnly` for debugging — use browser dev tools to inspect server-set cookies instead

**Phase:** Phase 2 (Cookie Auth Middleware)

---

### SEC-02: CSRF Attack on Admin Endpoints

**Risk:** HIGH

**What:** Cookie-based auth is inherently vulnerable to Cross-Site Request Forgery. If an admin visits a malicious site while logged into the admin panel, that site can submit forged requests to the admin API (e.g., disable a user, reset a password). `SameSite=Lax` blocks most CSRF but does NOT protect against same-origin forged requests.

**Warning Signs:**
- Admin endpoints accept POST/PUT/DELETE with only cookie auth and no additional CSRF token
- No `X-CSRF-Token` header validation on state-changing requests
- SameSite cookie is the only CSRF defense
- CORS configured with `AllowCredentials()` but no origin validation on write endpoints

**Prevention Strategy:**
Implement **defense in depth** with multiple layers:

1. **SameSite=Lax** — baseline defense (already recommended in SEC-01)
2. **Double Submit Cookie Pattern (Signed):**
   - API generates CSRF token on login: `hmac(sessionId + random) + "." + random`
   - Store in a **separate non-httpOnly cookie** (e.g., `XSRF-TOKEN`)
   - Frontend reads `XSRF-TOKEN` via JS, sends as `X-CSRF-Token` header on every POST/PUT/PATCH/DELETE
   - Backend validates: recompute HMAC, compare with constant-time comparison
3. **Custom Header Requirement:**
   - All state-changing admin API endpoints require `X-Requested-With: XMLHttpRequest` or `X-CSRF-Token` header
   - Browsers block cross-origin preflight for custom headers — malicious sites cannot forge them
4. **Never use GET for state changes** — audit all admin routes

No third-party library needed. Implement via custom middleware in .NET that validates CSRF on `[HttpPost]`, `[HttpPut]`, `[HttpPatch]`, `[HttpDelete]`.

**Phase:** Phase 2 (Cookie Auth Middleware) + Phase 3 (Authorization Policy)

---

### SEC-03: Role Escalation via Missing or Weak Authorization Policy

**Risk:** HIGH

**What:** An authenticated user without the `admin` role accesses admin endpoints because the controller has `[Authorize]` but no role check, or the role claim is checked incorrectly. In Keycloak, realm roles appear in the `realm_access.roles` array claim — not as a flat `role` claim. Checking `User.IsInRole("admin")` may fail silently depending on how claims are mapped from the Keycloak token.

**Warning Signs:**
- Admin controllers have `[Authorize]` but no `[Authorize(Roles = "admin")]` or policy attribute
- Role check uses `User.HasClaim("role", "admin")` instead of checking `realm_access.roles`
- No 403 test for non-admin user accessing admin endpoints
- Claims mapping from Keycloak token not inspected (what does the JWT actually contain?)

**Prevention Strategy:**
```csharp
// In Program.cs — explicit admin policy:
options.AddPolicy("AdminOnly", policy =>
    policy.RequireAssertion(ctx =>
    {
        // Keycloak puts roles in realm_access.roles array
        var rolesClaim = ctx.User.FindFirst("realm_access.roles");
        if (rolesClaim != null)
        {
            var roles = JsonSerializer.Deserialize<string[]>(rolesClaim.Value);
            return roles?.Contains("admin") == true;
        }
        // Fallback: check flat role claims if Keycloak role mapper flattens them
        return ctx.User.IsInRole("admin");
    }));

// On every admin controller:
[Authorize(Policy = "AdminOnly")]
[Authorize(AuthenticationSchemes = "AdminCookie")]
public class AdminClientsController : ControllerBase { ... }
```
- **Integration test is mandatory:** create test user WITHOUT admin role, authenticate with cookie, attempt GET /api/admin/clients — must return 403
- Log every 403 on admin endpoints with user identity for audit trail
- Never assume role claims map correctly — decode the actual JWT from Keycloak and inspect `realm_access`

**Phase:** Phase 3 (Authorization Policy)

---

### SEC-04: Admin Access via Client-Facing Token Replay

**Risk:** HIGH

**What:** The admin panel and client-facing SPA share the same Keycloak realm. A malicious client could intercept their own JWT, extract claims, and attempt to forge admin cookies or replay their token against admin endpoints if authentication schemes are not properly isolated.

**Warning Signs:**
- Admin endpoints accept JwtBearer tokens (same scheme as client endpoints)
- No separate authentication scheme for admin cookie vs client JWT
- Admin login endpoint does not validate against Keycloak (just checks a local password list)
- Same CORS policy allows both client and admin origins

**Prevention Strategy:**
- Admin endpoints MUST use `[Authorize(AuthenticationSchemes = "AdminCookie")]` — explicit scheme override
- Client endpoints use `[Authorize]` with default JwtBearer scheme
- Admin login validates credentials against Keycloak AND checks `admin` role
- Admin and client CORS origins should be separate if possible
- Admin session IDs must be unpredictable — use cryptographically secure random generator

**Phase:** Phase 2 (Cookie Auth Middleware)

---

### SEC-05: Admin Session Hijacking via Predictable Session IDs

**Risk:** HIGH

**What:** If admin session identifiers stored in `IDistributedCache` are predictable (e.g., sequential integers, timestamps, or short random strings), an attacker can enumerate valid sessions and hijack admin access.

**Warning Signs:**
- Session ID = `admin_{userId}` or `{email}` or sequential numbers
- Session ID shorter than 32 characters
- No rate limiting on session lookup
- `IDistributedCache` entries readable by non-admin processes

**Prevention Strategy:**
- Session ID = `CryptoRandom.CreateHexString(32)` (256-bit entropy minimum)
- Session data in cache: `{ userId, email, roles[], issuedAt, expiresAt, lastActivityAt, ipAddress }`
- Cache key = `admin_session:{sessionId}`
- Session store should not be queryable by partial key (prevents enumeration)
- Log session creation with IP address for audit trail
- Invalidate all existing sessions when admin password changes in Keycloak

**Phase:** Phase 2 (Cookie Auth Middleware)

---

### SEC-06: Keycloak Admin API Token Leakage

**Risk:** HIGH

**What:** The .NET API uses a confidential Keycloak client (`onboarding-api-admin`) with `manage-users` role to perform admin actions. The service account token or client secret leaks to logs, exceptions, or frontend responses.

**Warning Signs:**
- `Keycloak.AuthServices.Sdk` logs full request/response bodies
- Exception messages include Bearer token value
- Service account credentials logged during startup
- Frontend can call Keycloak Admin API directly (no backend proxy)

**Prevention Strategy:**
- `Duende.AccessTokenManagement` handles token lifecycle — do not manually store or log tokens
- Serilog destructuring policy: mask `Authorization` headers, `access_token`, `client_secret`
- Keycloak Admin API calls are backend-only — never expose client secret to frontend
- Audit log every admin action: who did what to whom, when, from which IP
- Use Serilog `LogContext.PushProperty("AdminEmail", adminEmail)` on admin request pipeline

**Phase:** Phase 4 (Admin API Endpoints) + Phase 10 (Observability + Hardening)

---

### SEC-07: Missing Rate Limiting on Admin Login

**Risk:** HIGH

**What:** Keycloak has brute force protection, but the admin login endpoint (`POST /admin/login`) proxies credentials to Keycloak. An attacker can hammer this endpoint, triggering Keycloak lockout for legitimate admins (denial of service) or using it as an oracle to enumerate admin emails.

**Warning Signs:**
- No rate limit on `/admin/login`
- Different error messages for "user not found" vs "wrong password"
- No throttling between failed attempts
- No account lockout notification after failures

**Prevention Strategy:**
- Apply `RateLimiter` middleware (built into .NET 10) on `/admin/login`
- Policy: 5 attempts per minute per IP, then 60-second ban
- Return identical error for all failures: "Invalid credentials" (no enumeration)
- Log failed attempts with IP and email for anomaly detection
- After 3 consecutive failures for same email: add increasing delay (exponential backoff)
- This is complementary to Keycloak brute force — the API layer rate limit protects Keycloak from abuse

**Phase:** Phase 10 (Observability + Hardening)

---

### SEC-08: IDOR — Admin Can Access/Modify Any User by Guessing ID

**Risk:** HIGH

**What:** Admin endpoint `PUT /api/admin/users/{id}` does not verify that the authenticated admin has permission to modify that specific user. While all admins should manage all users in this system, the pattern of not checking ownership/authorization scope is dangerous and should be explicitly documented. If future requirements introduce scoped admin (e.g., regional admins), this becomes a critical vulnerability.

**Warning Signs:**
- Admin endpoint reads `id` from URL and acts without any authorization check beyond "is logged in as admin"
- No centralized authorization handler — each action method does its own ad-hoc check
- GUIDs used as user IDs (harder to guess than sequential integers, but still enumerable)

**Prevention Strategy:**
- Centralize admin authorization in a policy handler (see SEC-03)
- Document explicitly: "All admins can manage all users in v3.0. If scoped admin is needed later, add resource-level authorization here."
- Use GUIDs (not sequential integers) for user IDs to prevent enumeration
- Log all admin actions with target user ID for audit trail (detects abuse patterns)

**Phase:** Phase 3 (Authorization Policy)

---

### SEC-09: Missing Security Headers on Admin Panel

**Risk:** MEDIUM

**What:** Admin panel served without security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`, `Referrer-Policy`). Allows clickjacking, MIME sniffing attacks, and referrer leakage.

**Warning Signs:**
- HTTP response headers on admin pages lack security headers
- Admin panel can be embedded in an `<iframe>` on external sites
- Browser dev tools Security tab shows warnings
- CSP not set — inline scripts and eval allowed by default

**Prevention Strategy:**
```csharp
// In Program.cs — add security headers middleware:
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/admin"))
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["X-XSS-Protection"] = "0"; // Modern browsers use CSP instead
    }
    await next();
});
```
- For dev: relax CSP to allow Vite HMR (`localhost:5173`)
- For prod: strict CSP with nonce for inline scripts

**Phase:** Phase 10 (Observability + Hardening)

---

# 2. UX PITFALLS

---

### UX-01: Unpaginated User Listing Crashes with Scale

**Risk:** HIGH

**What:** Admin "list all clients" query loads all users into memory without pagination. Works fine with 50 test users. Crashes or times out with 5,000+ production users. The frontend tries to render a massive table, freezing the browser.

**Warning Signs:**
- `SELECT * FROM Clients` without `Skip/Take`
- API returns unbounded array with no `totalCount` or `pageSize` metadata
- Frontend table component receives full list and renders all rows
- Response payload > 1MB for client list endpoint
- No `LIMIT` in EF Core query

**Prevention Strategy:**
```csharp
// Backend — enforced pagination with max page size:
public class PagedRequest {
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    private const int MaxPageSize = 100;
    public int EffectivePageSize => Math.Min(Math.Max(PageSize, 1), MaxPageSize);
}

// Query:
var query = _context.Clients.AsNoTracking()
    .OrderByDescending(c => c.RegisteredAt);

var totalCount = await query.CountAsync();
var items = await query
    .Skip((request.Page - 1) * request.EffectivePageSize)
    .Take(request.EffectivePageSize)
    .Select(c => new AdminClientListDto { ... })
    .ToListAsync();

// Return:
return Ok(new PagedResult<AdminClientListDto> {
    Items = items,
    TotalCount = totalCount,
    Page = request.Page,
    PageSize = request.EffectivePageSize,
    TotalPages = (int)Math.Ceiling((double)totalCount / request.EffectivePageSize)
});
```
- Frontend: `@tanstack/react-table` with server-side pagination mode
- Never trust client-supplied `PageSize` — always enforce server-side cap

**Phase:** Phase 4 (Admin API Endpoints) + Phase 6 (Admin Client List Page)

---

### UX-02: Search Without Indexing Causes Full Table Scans

**Risk:** MEDIUM

**What:** Admin search by name, email, CPF, or CNPJ uses `EF.Functions.ILike()` or `.Contains()` without database indexes. Each search triggers a full table scan on the `Clients` table.

**Warning Signs:**
- `WHERE Name LIKE '%search%'` pattern (leading wildcard prevents index use)
- Search response time > 500ms with 1,000+ records
- PostgreSQL query plan shows "Seq Scan" instead of "Index Scan"
- No database migration adding search indexes

**Prevention Strategy:**
```sql
-- Migration: create indexes for common search patterns
CREATE INDEX idx_clients_email ON clients(email);
CREATE INDEX idx_clients_cpf ON clients(cpf) WHERE cpf IS NOT NULL;
CREATE INDEX idx_clients_cnpj ON clients(cnpj) WHERE cnpj IS NOT NULL;
-- For full-text name search (PostgreSQL):
CREATE INDEX idx_clients_name_trgm ON clients USING gin (name gin_trgm_ops);
-- Requires pg_trgm extension:
CREATE EXTENSION IF NOT EXISTS pg_trgm;
```
- Use `.StartsWith()` instead of `.Contains()` when possible (uses B-tree index)
- For fuzzy name search, use PostgreSQL `pg_trgm` extension with `ILIKE '%term%'`
- Monitor query plans with `EXPLAIN ANALYZE` on production-like data volumes

**Phase:** Phase 4 (Admin API Endpoints)

---

### UX-03: Form Validation Bypassed on Edit

**Risk:** MEDIUM

**What:** Admin edit form accepts invalid data because server-side validation was only built for the registration flow (create), not the update flow. CPF/CNPJ validation, email format, and required field checks are missing on PUT.

**Warning Signs:**
- `UpdateClientCommand` has no FluentValidation validator
- PUT endpoint accepts `null` or empty strings for required fields
- Frontend reuses registration Zod schema for edit form (which has different rules — some fields read-only)
- No 422 response on invalid edit submission

**Prevention Strategy:**
- Dedicated `UpdateClientCommandValidator` — do NOT reuse `RegisterClientCommandValidator`
- Edit form: CPF/CNPJ and email should be **read-only** (identity fields cannot change under LGPD without formal process)
- Editable fields: name, phone, address — validate format but not uniqueness
- Frontend Zod schema for edit: mark identity fields as `.readonly()`, not `.optional()`
- Integration test: submit edit with invalid data → must return 422 with field-level errors

**Phase:** Phase 4 (Admin API Endpoints) + Phase 7 (Admin Client Detail Page)

---

### UX-04: Destructive Actions Without Confirmation or Audit

**Risk:** HIGH

**What:** Admin can block/unblock/delete users with a single click — no confirmation dialog, no audit log, no undo. A misclick disables a client's account, triggering support tickets. Under LGPD, accidental deletion of personal data without proper logging is a compliance violation.

**Warning Signs:**
- Delete/block button triggers API call directly (no modal/dialog)
- No confirmation text ("Are you sure you want to block {user}?")
- No audit log entry for state changes
- No "this action cannot be undone" warning for destructive actions
- Delete action permanently removes data without soft-delete or tombstone

**Prevention Strategy:**
- **Block/Unblock:** Use `@radix-ui/react-alert-dialog` with:
  - User's name and email in confirmation text
  - Clear label: "Bloquear" (red) or "Desbloquear" (blue)
  - Consequence description: "O usuario nao podera fazer login ate ser desbloqueado"
- **LGPD Delete:** Multi-step process:
  1. Admin clicks "Excluir dados" → dialog shows what will be deleted
  2. Admin must type the user's email to confirm (prevents misclick)
  3. Final confirmation triggers deletion
  4. Audit log: "Admin {email} deleted client {clientId} — LGPD request"
- **Audit trail:** Every state change (block, unblock, delete, edit) writes to Serilog with `LogEventLevel.Warning` or higher, including admin identity, target user ID, action, timestamp, IP

**Phase:** Phase 7 (Admin Client Detail Page) + Phase 8 (Admin Actions)

---

### UX-05: Admin Cannot Distinguish PF vs PJ at a Glance

**Risk:** LOW (but high UX impact)

**What:** Admin client list shows all users in identical format. Admin cannot quickly identify whether a client is Pessoa Física or Pessoa Jurídica without clicking into details. This slows daily operations.

**Warning Signs:**
- Client list table has no "Tipo" column
- CPF and CNPJ displayed in same column with same formatting
- No visual badge distinguishing PF from PJ

**Prevention Strategy:**
- Table column: "Tipo" with badge — "PF" (blue) or "PJ" (green)
- Document column: format CPF as `XXX.XXX.XXX-XX` and CNPJ as `XX.XXX.XXX/XXXX-XX`
- Filter dropdown: "Todos", "Pessoa Física", "Pessoa Jurídica"
- Frontend: `@radix-ui/react-select` for filter, `Badge` component for type indicator

**Phase:** Phase 6 (Admin Client List Page)

---

### UX-06: Token Refresh Silently Fails, Admin Gets Logged Out Mid-Task

**Risk:** MEDIUM

**What:** Admin session cookie expires while the admin is filling a long edit form. On submit, the API returns 401. The frontend redirects to login, losing all unsaved form data. No warning was given before expiration.

**Warning Signs:**
- No sliding expiration on admin session
- No session expiry warning shown to admin
- Form submission fails with 401 and no recovery mechanism
- No periodic keep-alive ping to extend session

**Prevention Strategy:**
- **Sliding expiration:** On each API request, update session expiry in `IDistributedCache`. If user is active, session stays alive up to `MaxAge` (8h).
- **Frontend detection:** On 401 response, show toast: "Sessao expirada. Faca login para continuar." Save form data to `sessionStorage` before redirect. On return after re-login, restore form data.
- **Keep-alive ping:** Every 5 minutes, send `GET /api/admin/session/refresh` (lightweight endpoint) to extend session while admin is actively viewing the page. Only ping when `document.visibilityState === 'visible'`.
- **Expiry warning:** If session will expire in < 10 minutes, show banner: "Sessao expira em X minutos. Continue trabalhando para estender."

**Phase:** Phase 9 (Cookie Session Management)

---

### UX-07: Filter State Lost on Navigation or Browser Back

**Risk:** MEDIUM

**What:** Admin applies filters (search term, PF/PJ type, date range, sort order), then navigates away and back. All filters reset to defaults. Browser back/forward buttons do not restore filter state. Admin must rebuild the filtered view manually.

**Warning Signs:**
- Filter state stored only in React component state (not in URL)
- URL does not change when filters are applied
- Browser back button navigates to previous page but loses filter context
- Admin cannot bookmark or share a filtered view

**Prevention Strategy:**
- Encode all filter/sort/pagination state in URL query parameters: `?page=2&search=John&type=PF&status=active&sortBy=name&sortOrder=asc`
- Use `@tanstack/react-router` search params or `useSearchParams` hook
- On component mount, read from URL to initialize filter state
- "Clear all filters" button resets URL to base path
- Browser back/forward naturally works because state is in URL

**Phase:** Phase 6 (Admin Client List Page)

---

### UX-08: No Feedback During Long-Running Admin Actions

**Risk:** MEDIUM

**What:** Admin clicks "Delete User" and the button appears to do nothing for 3-5 seconds (Keycloak API call + DB deletion). Admin clicks again, triggering duplicate requests. Or the admin assumes it failed and navigates away, interrupting the operation.

**Warning Signs:**
- Submit button not disabled during API call
- No loading spinner or progress indicator
- No optimistic UI update
- No toast or inline feedback during the operation

**Prevention Strategy:**
- Disable submit button immediately on click
- Show `Loader2` spinner inside button text: `[Spinner] Excluindo...`
- For block/unblock: optimistic update (change badge color immediately), rollback on error
- For delete: optimistic removal from table (row fades out), rollback on error
- Always follow with Sonner toast: success (green, 3s auto-dismiss) or error (red, manual dismiss)
- For LGPD delete (multi-step): show progress steps in dialog

**Phase:** Phase 8 (Admin Actions)

---

# 3. COMPLIANCE PITFALLS (LGPD)

---

### LGPD-01: Incomplete Deletion — Keycloak User Left Behind

**Risk:** HIGH

**What:** Admin deletes client data from app_db but forgets to delete the user from Keycloak. The user still exists in the identity provider with their email, login history, and session data. Under LGPD Article 16 (right to erasure), personal data must be deleted from ALL systems — leaving it in Keycloak is a compliance violation.

**Warning Signs:**
- Delete endpoint only calls `_context.Clients.Remove(client)`
- No call to Keycloak Admin API `DELETE /admin/realms/{realm}/users/{id}`
- No error handling if Keycloak deletion fails after app_db deletion succeeds
- Deleted user can still log in via Keycloak

**Warning Signs in Keycloak:**
- User still appears in Keycloak Admin Console user list after app deletion
- Keycloak audit logs show post-deletion login attempts for "deleted" user
- Keycloak user count does not decrease after app deletion

**Prevention Strategy:**
```
Delete flow (ordered for safety):
  1. Admin confirms deletion (email typed to confirm)
  2. Audit log: "LGPD deletion initiated by {adminEmail} for client {clientId}"
  3. Delete from Keycloak FIRST:
     - KeycloakAdminClient.DeleteUserAsync(keycloakUserId)
     - If fails: abort, return error to admin, no data lost
  4. Delete from app_db:
     - _context.Clients.Remove(client)
     - _context.SaveChanges()
  5. Audit log: "LGPD deletion completed for client {clientId}"
  6. Return 200 to admin
```
- **Why Keycloak first?** If Keycloak deletion succeeds but app_db fails, the user can no longer log in (harmless state). If app_db deletes first and Keycloak fails, the orphan Keycloak user still holds personal data — a compliance violation.
- **Compensation:** If Keycloak deletion fails, log the error, alert the admin, and do NOT delete app_db data. The admin can retry.
- **Soft delete option:** Consider setting a `DeletedAt` timestamp instead of hard delete. A background job performs hard deletion after a 30-day grace period. This provides an undo window and audit trail.

**Phase:** Phase 4 (Admin API Endpoints) + Phase 8 (Admin Actions)

---

### LGPD-02: No Audit Trail for Data Deletion

**Risk:** HIGH

**What:** Data is deleted from both Keycloak and app_db, but there is no immutable record proving WHO deleted WHAT, WHEN, and WHY. Under LGPD, the controller must demonstrate compliance. Without audit logs, the organization cannot prove it honored a deletion request.

**Warning Signs:**
- Delete operation writes no log entry
- Logs do not include admin identity, target user, or timestamp
- No dedicated audit log table or external log sink
- Logs can be modified or deleted by admins

**Prevention Strategy:**
- Every deletion writes a structured log entry:
```json
{
  "Event": "LGPD_USER_DELETED",
  "AdminEmail": "admin@company.com",
  "AdminId": "keycloak-uuid",
  "ClientId": "app-uuid",
  "ClientEmail": "deleted@example.com",
  "KeycloakUserId": "kc-uuid",
  "Timestamp": "2026-04-09T14:30:00Z",
  "IPAddress": "192.168.1.100",
  "UserAgent": "Mozilla/5.0...",
  "Reason": "LGPD deletion request",
  "KeycloakDeleted": true,
  "AppDbDeleted": true
}
```
- Log level: `Warning` or higher (ensures retention in most log pipelines)
- Store audit events in a separate table (`AuditEvents`) that is append-only (no UPDATE/DELETE)
- Export to external log sink (Loki, Tempo, or similar) where admins cannot modify historical entries
- Retention: minimum 5 years (LGPD does not specify, but Brazilian labor law uses 5 years as precedent)

**Phase:** Phase 10 (Observability + Hardening)

---

### LGPD-03: Deletion Without Email Confirmation to Data Subject

**Risk:** MEDIUM

**What:** Admin deletes a user without confirming the user's email address. Under LGPD, the data subject has the right to confirmation that their data was deleted. Without capturing and logging the email at deletion time, the organization cannot send the confirmation response.

**Warning Signs:**
- Delete endpoint does not return or log the deleted user's email
- No email notification sent to the data subject post-deletion
- Admin UI does not require email confirmation before deletion

**Prevention Strategy:**
- **Before deletion:** Admin must type the exact email address of the user in the confirmation dialog. If the typed email does not match, the delete button remains disabled.
- **After deletion:** System logs the email for audit purposes. Optionally, send a deletion confirmation email to the data subject's registered email (if they haven't opted out of communications).
- **Email template:** "Seus dados pessoais foram excluidos de nosso sistema conforme solicitado (LGPD Art. 18). Se tiver duvidas, entre em contato com nosso DPO em dpo@company.com."

**Phase:** Phase 8 (Admin Actions)

---

### LGPD-04: Personal Data in Logs and Telemetry

**Risk:** MEDIUM

**What:** Serilog and OpenTelemetry capture request bodies, query parameters, and headers that include personal data (CPF, CNPJ, email, name). These logs are retained longer than the source data, creating a compliance violation — the user was "deleted" but their data lives on in log files.

**Warning Signs:**
- Serilog logs contain full request bodies including CPF/CNPJ
- OpenTelemetry span attributes include `http.request.body` with personal data
- Log retention > data retention policy
- Logs accessible to developers without restriction

**Prevention Strategy:**
- Serilog `DestructuringPolicy`: redact `cpf`, `cnpj`, `email`, `phone` fields
- OpenTelemetry: use `.Filter = ...` to exclude sensitive attributes from spans
- Log enrichment: use hashed identifiers instead of raw personal data (e.g., SHA256 of email for correlation)
- Log retention policy: auto-delete logs after 90 days (or per organizational policy)
- Document data types in logs and retention periods in a Data Processing Register (LGPD Article 37)

**Phase:** Phase 10 (Observability + Hardening)

---

### LGPD-05: Soft Delete Does Not Actually Delete (Compliance Illusion)

**Risk:** MEDIUM

**What:** System implements "soft delete" (sets `DeletedAt` timestamp) but never performs hard deletion. The data remains in the database indefinitely, queryable by anyone with DB access. Under LGPD, soft delete alone does not satisfy the right to erasure — it only hides data from the UI.

**Warning Signs:**
- `DeletedAt` column exists but no background job performs hard deletion
- "Deleted" users still count toward database size
- Soft-deleted data is excluded from listing queries but still accessible via direct queries
- No scheduled hard delete job or manual deletion process

**Prevention Strategy:**
- Soft delete is acceptable as a **grace period** (e.g., 30 days) — gives undo window
- Hard delete job must run on a schedule (daily cron, Hangfire job, or hosted service)
- Job logic: `DELETE FROM Clients WHERE DeletedAt IS NOT NULL AND DeletedAt < NOW() - INTERVAL '30 days'`
- After hard delete, also delete from Keycloak (if not already done at soft-delete time)
- Document the retention period in the privacy policy
- **Important:** Audit log entries about the deletion must NOT be hard-deleted — they are a legal compliance record, not personal data of the deleted user

**Phase:** Phase 8 (Admin Actions)

---

### LGPD-06: No Data Portability / Export Before Deletion

**Risk:** LOW (but required by LGPD Art. 18)

**What:** LGPD Article 18 grants data subjects the right to data portability — they can request their data in a structured, commonly used, machine-readable format. Admin panel provides no way to export a user's complete data before deletion.

**Warning Signs:**
- No "Export Data" button in admin user detail view
- No endpoint that aggregates all user data from PostgreSQL + Keycloak
- Admin cannot provide data subject with their stored information

**Prevention Strategy:**
- Export endpoint: `GET /api/admin/users/{id}/export` — aggregates all data
- Response: JSON file containing PF/PJ fields, registration timestamps, audit log entries
- Format: machine-readable (JSON), not PDF
- Include metadata: when data was collected, purpose of processing, retention period
- This endpoint does NOT trigger deletion — it's purely for portability

**Phase:** Phase 8 (Admin Actions)

---

# 4. PERFORMANCE PITFALLS

---

### PERF-01: N+1 Queries Enriching Admin List with Keycloak Data

**Risk:** HIGH

**What:** Admin client list endpoint loads all clients from app_db, then for EACH client, makes an HTTP call to Keycloak Admin API to fetch user status (enabled, lastLogin). With 100 clients = 100 HTTP calls = 5-10 second response time.

**Warning Signs:**
- `foreach (var client in clients) { var kcUser = await keycloakClient.GetUserAsync(client.KeycloakUserId); }`
- Admin list response time grows linearly with user count
- Keycloak Admin API rate-limits the application
- OpenTelemetry traces show 100+ spans for a single list request

**Prevention Strategy:**
- **Option A (Recommended):** Do NOT enrich list view with Keycloak data. Show only app_db data (name, email, document, registration date, status flag). Enrich only the detail view for a single user.
- **Option B (Batch):** If Keycloak data is required in list, fetch all users in a single batch call. Keycloak Admin API does not have a bulk GET endpoint, so implement:
  1. Collect all `KeycloakUserId` values
  2. Use `KeycloakAdminClient.GetUsersAsync()` (no ID filter, returns all users) — but paginate
  3. Build a dictionary: `Dictionary<string, KeycloakUser>`
  4. Join in memory with app_db clients
  5. **Caveat:** This works for small user counts (< 1,000). For larger datasets, cache Keycloak user status in app_db (update on every login, block, unblock event).
- **Option C (Cache):** Cache Keycloak user status in app_db with a `LastKeycloakSyncAt` column. List view reads from cache. Background job syncs every 5 minutes.
- **Never do:** N+1 HTTP calls in a request pipeline.

**Phase:** Phase 4 (Admin API Endpoints)

---

### PERF-02: EF Core Tracking Overhead on Read-Only Admin Queries

**Risk:** MEDIUM

**What:** Admin list and detail queries use EF Core change tracking by default. For read-only queries (listing, viewing details), tracking adds memory and CPU overhead for no benefit. With 100+ users per page, the DbContext change tracker holds 100+ entity instances.

**Warning Signs:**
- No `.AsNoTracking()` on list/detail queries
- High memory usage during admin list requests
- EF Core change tracker contains entities that are never modified
- DbContext lifetime extends beyond a single request

**Prevention Strategy:**
```csharp
// ALWAYS use AsNoTracking for read-only admin queries:
var clients = await _context.Clients
    .AsNoTracking()
    .Where(c => !c.DeletedAt.HasValue)
    .OrderByDescending(c => c.RegisteredAt)
    .Skip(...).Take(...)
    .ToListAsync();

// Even better — project to DTO directly (avoids materializing entities):
var clientDtos = await _context.Clients
    .AsNoTracking()
    .Select(c => new AdminClientListDto {
        Id = c.Id,
        Name = c.Name,
        Email = c.Email,
        Document = c.Cpf ?? c.Cnpj,
        PersonType = c.Cpf != null ? "PF" : "PJ",
        RegisteredAt = c.RegisteredAt
    })
    .OrderByDescending(c => c.RegisteredAt)
    .Skip(...).Take(...)
    .ToListAsync();
```
- Project directly to DTO — EF Core generates a single optimized SQL query
- Use `.AsNoTrackingWithIdentityResolution()` only when you need entity identity but not tracking (rare in admin queries)

**Phase:** Phase 4 (Admin API Endpoints)

---

### PERF-03: Missing Select N+1 in Admin List (EF Core Lazy Loading)

**Risk:** MEDIUM

**What:** Admin list query loads `Client` entities, then for each client, accesses a navigation property (e.g., `client.Address`, `client.Documents`) that triggers a lazy-load query. If lazy loading is enabled, this creates N+1 database queries.

**Warning Signs:**
- `Proxies` package installed (`Microsoft.EntityFrameworkCore.Proxies`)
- `UseLazyLoadingProxies()` called in `OnConfiguring`
- Navigation properties accessed without `.Include()` in list queries
- EF Core MiniProfiler or logging shows N+1 SELECT statements

**Prevention Strategy:**
- **Disable lazy loading** entirely in this project. Use explicit `.Include()` when needed:
```csharp
var clients = await _context.Clients
    .AsNoTracking()
    .Include(c => c.Address)  // Only if address is needed in list
    .Where(...)
    .ToListAsync();
```
- Better: project to DTO (see PERF-02) — EF Core generates a JOIN, not N+1 queries
- Add a code analyzer rule or CI check that rejects `UseLazyLoadingProxies()` in PR reviews
- If using `.Select()` projection, lazy loading is impossible (projection translates to SQL)

**Phase:** Phase 4 (Admin API Endpoints)

---

### PERF-04: Unbounded Count Query on Large Tables

**Risk:** MEDIUM

**What:** Pagination requires `totalCount` for the frontend to calculate total pages. `await query.CountAsync()` on a table with millions of rows and complex `WHERE`/`ILIKE` filters is slow. The count query runs on every page request, even when the admin just changes the page number.

**Warning Signs:**
- `CountAsync()` runs on every paginated request
- Count query takes > 200ms on production data
- Count includes complex `ILIKE` filters that prevent index usage
- Pagination metadata is not cached

**Prevention Strategy:**
- **Count is necessary** for proper pagination (frontend needs it). Optimize the count query:
  1. Use the same indexed filters as the data query
  2. If `ILIKE '%term%'` is slow, consider `.StartsWith(term)` for search prefix matching
  3. For very large tables (> 100K rows), consider approximate counts (PostgreSQL `pg_class.reltuples`)
- **Cache the count** for short periods (e.g., 30 seconds) if the admin list does not need real-time accuracy:
```csharp
// Cache totalCount for 30s — good enough for pagination UI
var cachedCount = await _cache.GetAsync<int>("admin_client_count");
if (cachedCount == null) {
    cachedCount = await _context.Clients.CountAsync(c => !c.DeletedAt.HasValue);
    await _cache.SetAsync("admin_client_count", cachedCount, TimeSpan.FromSeconds(30));
}
```
- **Keyset pagination** (cursor-based) as an alternative to offset pagination: eliminates the need for `CountAsync` and `Skip()`, but breaks arbitrary page jumping. Good for infinite scroll, not for numbered pagination.

**Phase:** Phase 4 (Admin API Endpoints)

---

### PERF-05: Frontend Over-Fetching and Re-Rendering on Filter Changes

**Risk:** MEDIUM

**What:** Every filter change (search, person type, status, date range) triggers a full API refetch AND a full table re-render. With large datasets, this causes visible UI lag, duplicate requests, and wasted network bandwidth.

**Warning Signs:**
- Filter inputs trigger API call on every keystroke (no debounce)
- Multiple filter changes fire multiple parallel API requests (race conditions)
- TanStack Table re-renders all rows when a single row changes
- No `useMemo` for expensive filter/sort computations

**Prevention Strategy:**
- **Debounce search input:** 300ms delay before triggering API call
- **Abort previous requests:** Use `AbortController` with `ky` or `fetch` — when a new filter request starts, abort the in-flight one
- **TanStack Query `staleTime`:** Set `staleTime: 10000` (10s) to cache results and avoid refetching on rapid filter toggles
- **React.memo on table rows:** Prevent unnecessary re-renders when row data hasn't changed
- **URL debounce:** Sync filter state to URL with a small delay — prevents URL thrashing on every keystroke

**Phase:** Phase 6 (Admin Client List Page)

---

# 5. CROSS-CUTTING PITFALLS

---

### CROSS-01: Admin and Client APIs Share Same Base Path

**Risk:** MEDIUM

**What:** Admin endpoints are under `/api/admin/...` and client endpoints are under `/api/v1/...`. If admin endpoints were accidentally placed under `/api/v1/...` without an admin-specific prefix, CORS policies, authentication middleware, and authorization policies could be misapplied.

**Warning Signs:**
- No clear URL convention separating admin from client APIs
- Admin endpoints do not have a dedicated `[Route("api/v1/admin/[controller]")]` prefix
- Same controller handles both admin and client actions
- Middleware pipeline does not branch on path for admin routes

**Prevention Strategy:**
- Admin API prefix: `/api/v1/admin/[controller]`
- Client API prefix: `/api/v1/[controller]`
- Use `MapWhen` or separate middleware pipelines for admin vs client routes
- Admin-specific middleware (CSRF, cookie auth) applied only to `/api/v1/admin/*`
- Integration test: verify admin endpoint returns 401 without cookie, 403 without admin role

**Phase:** Phase 3 (Authorization Policy) + Phase 4 (Admin API Endpoints)

---

### CROSS-02: No Integration Tests for Admin Auth Flow

**Risk:** HIGH

**What:** Admin authentication and authorization are complex: cookie auth + CSRF + Keycloak role check + admin policy. Without integration tests, a regression in any layer could allow unauthorized access to admin endpoints.

**Warning Signs:**
- No test that authenticates with cookie and accesses admin endpoint
- No test for 403 on admin endpoint with non-admin user
- No test for CSRF token validation on POST/PUT/DELETE
- No test for complete LGPD deletion flow (Keycloak + app_db + audit log)

**Prevention Strategy:**
```csharp
// Integration test examples:
[Fact]
public async Task GetAdminClients_WithoutCookie_Returns401() {
    var client = factory.CreateClient();
    var response = await client.GetAsync("/api/v1/admin/clients");
    response.StatusCode.ShouldNotBeNull().Be(HttpStatusCode.Unauthorized);
}

[Fact]
public async Task GetAdminClients_WithNonAdminCookie_Returns403() {
    var client = factory.CreateClient();
    // Set up user WITHOUT admin role
    var cookie = await CreateAdminCookieAsync(nonAdminUser);
    client.DefaultRequestHeaders.Add("Cookie", cookie);
    var response = await client.GetAsync("/api/v1/admin/clients");
    response.StatusCode.ShouldNotBeNull().Be(HttpStatusCode.Forbidden);
}

[Fact]
public async Task PostBlockClient_WithoutCsrfToken_Returns400() {
    var client = factory.CreateClient();
    var cookie = await CreateAdminCookieAsync(adminUser);
    client.DefaultRequestHeaders.Add("Cookie", cookie);
    var response = await client.PostAsJsonAsync("/api/v1/admin/clients/123/block", new { });
    response.StatusCode.ShouldNotBeNull().Be(HttpStatusCode.BadRequest); // CSRF missing
}
```
- Testcontainers for PostgreSQL + Keycloak in integration tests
- `WebApplicationFactory<Program>` for in-process API testing
- Run integration tests in CI pipeline before merge

**Phase:** Phase 5 (Integration Tests)

---

### CROSS-03: Admin Panel Has No Health Check or Readiness Endpoint

**Risk:** LOW

**What:** Admin panel deployment has no way to verify it is healthy and can connect to Keycloak + PostgreSQL. If Keycloak is down, the admin panel may appear "up" but cannot perform any actions.

**Warning Signs:**
- No `/health` or `/ready` endpoint
- Health check does not verify Keycloak connectivity
- Health check does not verify PostgreSQL connectivity
- Load balancer cannot detect admin panel degradation

**Prevention Strategy:**
```csharp
// In Program.cs:
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgres")
    .AddUrlGroup(new Uri(keycloakUrl + "/health"), name: "keycloak");

app.MapHealthChecks("/health");           // Liveness: is the process alive?
app.MapHealthChecks("/ready");           // Readiness: can it serve requests?
app.MapHealthChecks("/health/detailed",  // Detailed: which dependency is down?
    new HealthCheckOptions { ResponseWriter = WriteHealthCheckResponse });
```
- `/health` → load balancer liveness probe (returns 200 if process is running)
- `/ready` → load balancer readiness probe (returns 200 only if DB + Keycloak are reachable)
- `/health/detailed` → human-readable status for ops team

**Phase:** Phase 10 (Observability + Hardening)

---

# 6. SUMMARY — RISK MATRIX

| Code | Pitfall | Risk | Phase |
|------|---------|------|-------|
| SEC-01 | Cookie without security flags | HIGH | Phase 2 |
| SEC-02 | CSRF attack on admin endpoints | HIGH | Phase 2 |
| SEC-03 | Role escalation / weak policy | HIGH | Phase 3 |
| SEC-04 | Client-facing token replay | HIGH | Phase 2 |
| SEC-05 | Predictable session IDs | HIGH | Phase 2 |
| SEC-06 | Keycloak API token leakage | HIGH | Phase 4, 10 |
| SEC-07 | No rate limiting on admin login | HIGH | Phase 10 |
| SEC-08 | IDOR — no resource-level auth | HIGH | Phase 3 |
| SEC-09 | Missing security headers | MEDIUM | Phase 10 |
| UX-01 | Unpaginated user listing | HIGH | Phase 4, 6 |
| UX-02 | Search without indexing | MEDIUM | Phase 4 |
| UX-03 | Form validation bypassed on edit | MEDIUM | Phase 4, 7 |
| UX-04 | Destructive actions without confirmation | HIGH | Phase 7, 8 |
| UX-05 | No PF/PJ distinction | LOW | Phase 6 |
| UX-06 | Token refresh silently fails | MEDIUM | Phase 9 |
| UX-07 | Filter state lost on navigation | MEDIUM | Phase 6 |
| UX-08 | No feedback during long actions | MEDIUM | Phase 8 |
| LGPD-01 | Incomplete deletion (Keycloak left behind) | HIGH | Phase 4, 8 |
| LGPD-02 | No audit trail for deletion | HIGH | Phase 10 |
| LGPD-03 | No email confirmation to data subject | MEDIUM | Phase 8 |
| LGPD-04 | Personal data in logs | MEDIUM | Phase 10 |
| LGPD-05 | Soft delete illusion | MEDIUM | Phase 8 |
| LGPD-06 | No data portability export | LOW | Phase 8 |
| PERF-01 | N+1 Keycloak HTTP calls | HIGH | Phase 4 |
| PERF-02 | EF Core tracking on read queries | MEDIUM | Phase 4 |
| PERF-03 | Lazy loading N+1 queries | MEDIUM | Phase 4 |
| PERF-04 | Unbounded count on large tables | MEDIUM | Phase 4 |
| PERF-05 | Frontend over-fetching/re-rendering | MEDIUM | Phase 6 |
| CROSS-01 | Admin/client API path confusion | MEDIUM | Phase 3, 4 |
| CROSS-02 | No integration tests for admin auth | HIGH | Phase 5 |
| CROSS-03 | No health check endpoint | LOW | Phase 10 |

---

## Sources

- [OWASP Cross-Site Request Forgery (CSRF) Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html)
- [OWASP Session Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html)
- [OWASP Cookie Security Attributes](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html#cookie-attributes)
- [ASP.NET Core Cookie Authentication — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie)
- [Role-based Authorization — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/mvc/security/authorization/roles)
- [LGPD Compliance Guide — SecurePrivacy](https://secureprivacy.ai/blog/lgpd-compliance-requirements)
- [LGPD Article 16 — Right to Erasure](https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm)
- [LGPD Article 18 — Data Portability](https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm)
- [TanStack Table v8 — Server-side Pagination](https://tanstack.com/table/v8/docs/guide/pagination)
- [EF Core Performance — AsNoTracking](https://learn.microsoft.com/en-us/ef/core/querying/tracking#no-tracking-queries)
- [EF Core Performance — Projections](https://learn.microsoft.com/en-us/ef/core/performance/#use-projections)
- [Keycloak Server Administration Guide](https://www.keycloak.org/docs/latest/server_admin/)
- [Keycloak Admin REST API](https://www.keycloak.org/docs-api/latest/rest-api/)
- [Keycloak Admin API — Delete User](https://www.keycloak.org/docs-api/latest/rest-api/#_delete_user)
- [Keycloak Disable User via Admin API — Forum](https://forum.keycloak.org/t/disable-user-using-keycloak-admin-rest-api/12267)
- [Audit Log Design Patterns — dev.to](https://dev.to/akkaraponph/comprehensive-research-audit-log-paradigms-gopostgresqlgorm-design-patterns-1jmm)
- [Optimistic UI Pattern — freeCodeCamp](https://www.freecodecamp.org/news/how-to-use-the-optimistic-ui-pattern-with-the-useoptimistic-hook-in-react/)
- [How to Build a Modern Admin Dashboard — Medium](https://medium.com/@wishula/how-to-build-a-modern-scalable-admin-portal-a-step-by-step-guide-3d1ffc29595e)
- [Build Admin Dashboard with shadcn/ui — freeCodeCamp](https://www.freecodecamp.org/news/build-an-admin-dashboard-with-shadcn-ui-and-tanstack-start/)
- [Token Refresh with Axios Interceptors — Medium](https://medium.com/@velja/token-refresh-with-axios-interceptors-for-a-seamless-authentication-experience-854b06064b0d)
