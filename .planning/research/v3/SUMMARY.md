# Research Summary — v3.0 Admin Backoffice

**Domain:** Admin Backoffice Panel for Client Registration Management (PF/PJ)
**Synthesized:** 2026-04-09
**Milestone context:** Subsequent (v3.0) — builds on completed v1.0/v2.0 (client registration, login, profile)
**Research files:** STACK.md, FEATURES.md, ARCHITECTURE.md, PITFALLS.md (all in `.planning/research/v3/`)

---

## Executive Summary

The v3.0 Admin Backoffice is a **separate admin UI** within the existing Vinxi frontend, backed by **new admin API endpoints** in the .NET API. Unlike the client-facing flow (JWT Bearer auth), admin users authenticate via **httpOnly cookie-based session auth** with role-based access control enforced by a Keycloak realm role (`admin`).

**Key architectural decision:** Admin backoffice uses cookie auth (not JWT) because admin sessions are high-value XSS targets. Cookies are httpOnly, SameSite=Lax, with CSRF protection via Double Submit Cookie pattern. Admin endpoints are protected by a custom `AdminOnly` authorization policy that checks `realm_access.roles` from Keycloak.

**What's new:** ~12 new NuGet/npm packages, 9 new shadcn/ui components, 2 new Radix primitives, cookie auth middleware, admin authorization policy, admin CRUD endpoints, paginated data table, search/filter infrastructure, LGPD-compliant deletion flow.

**What stays the same:** Keycloak as IdP, .NET 10 Controllers, EF Core + PostgreSQL, DDD domain layer, CQRS application layer, Serilog + OpenTelemetry.

---

## Stack Consensus

### Backend Additions

| Package | Version | Purpose | Confidence |
|---------|---------|---------|------------|
| `Microsoft.AspNetCore.Authentication.Cookies` | 10.0.x (built-in) | httpOnly cookie auth, sliding expiration | HIGH |
| `jose-jwt` | 5.3.0 | Zero-dependency JWT decoder for middleware | HIGH |
| `Asp.Versioning.Http` | 8.1.x | API versioning (`/api/v1/admin/...`) | MEDIUM |
| Manual pagination | — | EF Core `Skip/Take` with `PagedResult<T>` | HIGH |
| Manual CSRF | — | Double Submit Cookie pattern, no package | HIGH |

### Frontend Additions

| Package | Version | Purpose | Confidence |
|---------|---------|---------|------------|
| `@tanstack/react-table` | 8.21.x | Headless data table (pagination, sorting, filtering) | HIGH |
| `@radix-ui/react-dialog` | 1.1.x | Modals for view/edit/details | HIGH |
| `@radix-ui/react-alert-dialog` | 1.1.x | Destructive action confirmations (block, delete) | HIGH |
| `@radix-ui/react-checkbox` | 1.1.x | Row selection in data table | HIGH |
| `@radix-ui/react-select` | 2.1.x | Filter dropdowns (status, type) | HIGH |
| `@radix-ui/react-popover` | 1.1.x | Date range filters, advanced panels | MEDIUM |
| `@radix-ui/react-tabs` | 1.1.x | Tab navigation for admin sections | MEDIUM |
| `react-day-picker` | 9.x | Date range picker for filters | HIGH |
| `jose` (Node.js) | 6.x | SSR cookie decoding in Vinxi | HIGH |

### shadcn/ui Components to Install

`dialog`, `alert-dialog`, `checkbox`, `select`, `popover`, `tabs`, `table`, `calendar`, `avatar`

### What NOT to Use

| Package | Why Avoid |
|---------|-----------|
| `System.IdentityModel.Tokens.Jwt` | Heavy (~15 assemblies), overkill for simple cookie decoding |
| `MediatR` | Commercial license — use manual DI (existing pattern) |
| `MUI` / `Ant Design` / `AG-Grid Enterprise` | Heavy bundles, conflicts with Tailwind/shadcn |
| `localStorage` for admin session | XSS vulnerability — admin accounts are high-value targets |
| `next-auth` / `Auth.js` | Next.js-only, project uses Vinxi |

---

## Table Stakes (Must Have)

### Authentication & Authorization
- Admin login form (email + password) → httpOnly cookie issuance
- Cookie-based session (httpOnly, SameSite=Lax, Secure in prod, 8h sliding expiration)
- Role enforcement (`admin` role check on every endpoint via `AdminOnly` policy)
- CSRF protection (Double Submit Cookie pattern)
- Admin logout (cookie deletion + session removal)

### User Listing
- Server-side paginated table (20 per page default, max 100)
- Column sorting (nome, email, tipo, data registro, status)
- Column visibility toggle
- Row selection (single + multi)
- Loading skeleton, empty state, error state

### Search & Filters
- Global text search (nome, email, CPF/CNPJ) — debounced 300ms
- Filter by person type (PF/PJ/Todos)
- Filter by status (Ativo/Bloqueado)
- Filter by registration date range
- Filter state encoded in URL (bookmarkable, browser back/forward works)
- Clear all filters button

### CRUD Operations
- View user details modal (with Keycloak metadata)
- Edit user form (React Hook Form + Zod, server-side validation feedback)
- Block/unblock with confirmation dialogs (shadcn Alert Dialog)
- LGPD-compliant deletion (double confirmation, email typing, Keycloak-first deletion)

### Session & Error Handling
- Sliding expiration with keep-alive ping
- 401 interceptor → redirect to login
- Sonner toasts for success/error/warning
- Inline form validation + API error mapping
- Loading states on buttons (prevent double-submits)

---

## Key Differentiators (Nice to Have — Defer to Later Phases)

- Bulk actions (select multiple → bulk block/delete/export)
- CSV export of filtered listing
- Audit log viewer (dedicated tab)
- Admin activity dashboard (metrics cards, sparkline charts)
- Advanced search (fuzzy matching, CPF/CNP normalization with `pg_trgm`)
- Dedicated user detail page (`/admin/users/:id` instead of modal)
- Impersonation (admin acts as user for debugging)
- Keyboard shortcuts (Ctrl+K search, J/K navigation)

---

## Critical Watch-Outs (Top 5 from 32 Pitfalls)

1. **SEC-01: Cookie without security flags** (HIGH) — Missing `HttpOnly`/`Secure`/`SameSite` makes admin session trivially stealable. Prevention: enforce via `CookieBuilder`, integration test cookie flags.

2. **SEC-03: Role escalation via weak auth policy** (HIGH) — Keycloak roles are in `realm_access.roles[]` array, not flat claims. Checking `User.IsInRole()` may fail silently. Prevention: custom policy with JSON deserialization, mandatory 403 integration test.

3. **LGPD-01: Incomplete deletion — Keycloak user left behind** (HIGH) — Deleting from app_db but not Keycloak leaves personal data in IdP (LGPD Art. 16 violation). Prevention: Keycloak-first deletion order, abort on Keycloak failure.

4. **UX-01: Unpaginated listing crashes at scale** (HIGH) — `SELECT *` without `Skip/Take` works with 50 test users, crashes with 5,000+. Prevention: enforced pagination with max page size cap, server-side pagination mode in TanStack Table.

5. **SEC-02: CSRF on admin endpoints** (HIGH) — Cookie auth is inherently CSRF-vulnerable. SameSite=Lax blocks most but not all. Prevention: Double Submit Cookie pattern (signed) + custom header requirement.

---

## Architecture Highlights

### Dual Auth Model
Client-facing SPA uses **JWT Bearer** auth (ROPC → memory storage). Admin backoffice uses **httpOnly cookie** auth (session store in `IDistributedCache`). Both coexist on the same API without conflicts — explicit `AuthenticationSchemes` attribute isolates them.

### Cookie Auth Flow
```
Admin login → POST /admin/login → validate against Keycloak → check "admin" role
  → create session in IDistributedCache → set httpOnly cookie
  → subsequent requests: cookie → session lookup → role check → controller
  → sliding expiration on each request → 8h max inactivity → re-login
```

### Role Authorization Flow
```
Keycloak realm role "admin" → assigned manually to admin users
  → login extracts roles from JWT `realm_access.roles[]`
  → stored in session data
  → backend policy "AdminOnly" checks roles claim
  → frontend AdminRoute guard calls GET /api/admin/session/current
  → 401/403 → redirect to login
```

### Integration with Existing Architecture
- **Program.cs**: Add cookie auth scheme alongside JwtBearer (no conflicts)
- **Application layer**: New queries/commands alongside existing CQRS structure
- **Infrastructure**: Extended KeycloakUserService (disableUser, resetPassword), new DistributedCacheSessionStore
- **Frontend**: New `/admin/**` routes alongside existing client routes, AdminRoute guard component
- **Docker**: No new containers needed — same services, same ports

---

## Recommended Build Order (10 Phases)

Based on ARCHITECTURE.md dependency analysis:

1. **Keycloak Admin Role Setup** (config only) — admin role must exist before any code works
2. **Cookie Auth Middleware** (backend foundation) — login, logout, session endpoints
3. **Admin Authorization Policy** (security gate) — `AdminOnly` policy, 403 enforcement
4. **Admin API Endpoints** (backend CRUD) — list, view, edit, block, delete with Keycloak integration
5. **Frontend Admin Foundation** (routing, guards, login page) — `/admin/**` routes, AdminRoute guard
6. **Admin Client List Page** (first page) — paginated table, search, filters
7. **Admin Client Detail Page** (second page) — read-only PF/PJ data + Keycloak status
8. **Admin Actions** (disable, reset password) — confirmation dialogs, optimistic updates
9. **Cookie Session Management** (sliding expiration, expiry handling) — keep-alive, 401 redirect
10. **Observability + Hardening** (logging, rate limiting, security review) — Serilog enrichment, CSRF, security headers

**Critical path:** 1 → 2 → 3 → 4 → 5 → 6. Phases 7-10 can be parallelized.

---

## Confidence Assessment

| Research Area | Confidence | Rationale |
|---------------|------------|-----------|
| Stack | HIGH | Microsoft docs + established Radix/shadcn ecosystem + verified package licenses |
| Features | HIGH | Standard admin panel patterns + LGPD compliance requirements well understood |
| Architecture | HIGH | ASP.NET Core cookie auth + Keycloak role-based auth are mature, well-documented patterns |
| Pitfalls | HIGH | Based on OWASP guidelines + LGPD legal requirements + common admin panel failure modes |

**Risk areas needing deeper research during implementation:**
- Keycloak role claim mapping (`realm_access.roles[]` format) — verify with actual Keycloak 26.1 JWT structure
- CSRF Double Submit Cookie signed pattern — implementation details need careful review
- LGPD deletion ordering (Keycloak first vs app_db first) — validate with legal/compliance team

---

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase Group 1: Foundation (Phases 1-3)
**Rationale:** Security-first approach. Admin role, cookie auth, and authorization policy must work before any admin endpoints are created. All subsequent phases depend on this foundation.

- **Addresses:** Admin login form, cookie session, role enforcement, CSRF protection
- **Avoids:** SEC-01 (cookie flags), SEC-03 (role escalation), SEC-04 (token replay) — by establishing security before features
- **Uses:** `Microsoft.AspNetCore.Authentication.Cookies` (built-in), Keycloak realm role `admin`, custom `AdminOnly` policy

### Phase Group 2: Backend CRUD (Phase 4)
**Rationale:** Admin API endpoints depend on auth foundation being solid. This phase delivers the data layer that all frontend phases consume.

- **Implements:** List clients (paginated), view details, edit, block/unblock, LGPD delete
- **Avoids:** LGPD-01 (incomplete deletion) — by implementing Keycloak-first deletion from the start
- **Uses:** EF Core `Skip/Take` pagination, `PagedResult<T>` DTO, Keycloak Admin API for user management

### Phase Group 3: Frontend Shell (Phase 5)
**Rationale:** Admin routes, guards, and login page need backend endpoints (Phase 2 + 4) to be functional. This phase delivers the admin UI skeleton.

- **Implements:** `/admin/**` routes, AdminRoute guard, admin login page, admin layout
- **Avoids:** UX-06 (token refresh failure) — by establishing session handling before UI
- **Uses:** TanStack Router, `@tanstack/react-router`, shadcn layout components

### Phase Group 4: Admin Pages (Phases 6-8)
**Rationale:** Can be developed in parallel once frontend shell exists. List page is highest priority (admins need to see users first), then detail, then actions.

- **Phase 6:** Paginated listing with search/filters → addresses UX-01 (unpaginated crash), UX-07 (filter state in URL)
- **Phase 7:** Detail page with edit form → addresses UX-03 (form validation bypass), UX-05 (PF/PJ distinction)
- **Phase 8:** Block/unblock/delete actions → addresses UX-04 (destructive actions without confirmation), UX-08 (no feedback during operations)

### Phase Group 5: Polish (Phases 9-10)
**Rationale:** Session management and hardening require all features to be functional for testing. Do this last to validate against real admin workflows.

- **Phase 9:** Sliding expiration, keep-alive, 401 handling → addresses UX-06 (silent session expiry)
- **Phase 10:** Serilog enrichment, OpenTelemetry spans, rate limiting, CSRF hardening, security headers → addresses SEC-02 (CSRF), SEC-07 (rate limiting), SEC-09 (security headers)

### Phase Ordering Rationale
- **Security before features:** Phases 1-3 establish the security foundation. Building admin endpoints without proper auth/authorization first would create a vulnerable system.
- **Backend before frontend:** Phase 4 (API endpoints) must exist before Phase 5+ (UI) can consume them. TDD approach: write API tests first, then build UI.
- **List before detail before actions:** Natural admin workflow is "find user → view details → take action". Build in this order for incremental testing.
- **Hardening last:** Can't test CSRF, rate limiting, or session expiry against a non-functional system.

### Research Flags for Phases
- **Phase 2 (Cookie Auth):** Likely needs deeper research on CSRF Double Submit Cookie signed pattern implementation details
- **Phase 3 (Authorization):** Keycloak role claim format (`realm_access.roles[]`) needs verification with actual JWT from Keycloak 26.1
- **Phase 4 (Admin API):** LGPD deletion order (Keycloak-first) needs compliance team validation
- **Phase 6 (Client List):** Standard pagination patterns, unlikely to need deeper research
- **Phase 10 (Hardening):** Standard OWASP patterns, but rate limiting + Serilog enrichment for admin context may need custom implementation research

---

## Files

| File | Location | Lines |
|------|----------|-------|
| SUMMARY.md | `.planning/research/v3/SUMMARY.md` | this file |
| STACK.md | `.planning/research/v3/STACK.md` | 172 |
| FEATURES.md | `.planning/research/v3/FEATURES.md` | 330 |
| ARCHITECTURE.md | `.planning/research/v3/ARCHITECTURE.md` | 742 |
| PITFALLS.md | `.planning/research/v3/PITFALLS.md` | 1005 |

---

*Research conducted 2026-04-09. Existing v1/v2 research preserved in `.planning/research/` (original files untouched).*
