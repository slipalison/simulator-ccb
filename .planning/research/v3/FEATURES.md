# v3.0 Admin Backoffice — Feature Landscape

**Domain:** Admin panel for managing client registrations (PF/PJ)
**Project:** Onboarding de Clientes v3.0
**Researched:** 2026-04-09
**Scope:** Admin backoffice only. Does NOT re-cover client-facing features (documented in `/planning/research/FEATURES.md`).

---

## How to Read This Document

- **Table Stakes:** Features every modern admin panel must have. Missing = product feels broken or insecure.
- **Differentiators:** Nice-to-have features that elevate the panel from functional to excellent. Not required for v3.0 launch.
- **Anti-Features:** Features we deliberately will NOT build in v3.0, each with an explicit reason.
- **Complexity:** Low (1-2 days), Medium (3-5 days), High (5+ days)
- **UX Expectations:** What the admin experiences — concrete, testable behavior.

---

# 1. Table Stakes (Must Have)

### 1.1 Authentication & Authorization

| Feature | Complexity | Dependencies | UX Expectations |
|---------|------------|--------------|-----------------|
| **Admin login form** (email + password) | Medium | POST `/api/v1/admin/auth/login`, Keycloak ROPC or direct API validation, `httpOnly` cookie issuance | Clean form with email + password fields. "Lembrar-me" checkbox (extends cookie to 8h). On success → redirect to `/admin/users`. On failure → generic "Credenciais invalidas" (no email enumeration). Loading spinner on submit. |
| **Cookie-based session** (httpOnly, SameSite=Lax, Secure in prod) | Medium | ASP.NET Core Cookie Auth middleware, `IDistributedCache`, `__Host-admin_session` cookie name | Cookie is invisible to JS (XSS-safe). Session persists across page reloads. Expires after 8h of inactivity (sliding expiration). |
| **Role enforcement** (`admin` role check on every admin endpoint) | Low | Keycloak realm role `admin`, ASP.NET Core policy `AdminOnly`, claims mapping from `realm_access.roles` | Non-admin users get 403 on all `/api/v1/admin/**` endpoints. Every admin controller has `[Authorize(Policy = "AdminOnly")]` or inherits from `AdminControllerBase`. |
| **CSRF protection** (Double Submit Cookie pattern) | Medium | Separate non-httpOnly CSRF cookie, `X-CSRF-Token` header validation on POST/PUT/PATCH/DELETE | Admin forms work normally — CSRF token is handled by the API client automatically. No visible UX impact, but prevents cross-site forgery attacks. |
| **Admin logout** | Low | `HttpContext.SignOutAsync()`, cookie deletion, session removal from `IDistributedCache` | "Sair" button in header → cookie cleared → redirect to `/admin/login`. All tabs logged out simultaneously (shared session store). |

### 1.2 User Listing (Paginated Table)

| Feature | Complexity | Dependencies | UX Expectations |
|---------|------------|--------------|-----------------|
| **Paginated user listing** (server-side, 20 per page default) | Medium | GET `/api/v1/admin/users?page=&pageSize=&sortBy=&sortOrder=`, EF Core `Skip/Take`, `PagedResult<T>` DTO | Table shows 20 users per page. Page numbers at bottom. "Showing 1-20 of 1,234" format. Page size selector: 10, 20, 50, 100. Response time < 500ms even with 10k+ users. |
| **Column sorting** (nome, email, tipo PF/PJ, data de registro, status) | Medium | Backend accepts `sortBy` + `sortOrder` query params, EF Core `OrderBy` | Click column header → sort toggles asc/desc/none. Visual indicator (arrow icon) on sorted column. Multi-column sort NOT needed for v3.0. |
| **Column visibility toggle** | Low | `@tanstack/react-table` column visibility API, shadcn DropdownMenu | Admin can show/hide columns via dropdown. Preference NOT persisted in v3.0 (resets on page reload). |
| **Row selection** (single + multi via checkbox) | Low | TanStack Table row selection, shadcn Checkbox | Checkbox on each row + "select all" on header. Selected count badge appears ("3 rows selected"). Enables future bulk actions. |
| **Loading skeleton state** | Low | shadcn Skeleton component | While data fetches, table shows shimmer skeletons (not a spinner). Matches column count and structure of real table. |
| **Empty state** | Low | shadcn empty state component or custom | When no users match filters: friendly message "Nenhum cliente encontrado" + clear filters button. Not a blank white space. |
| **Error state** (API failure, network error) | Low | Sonner toast + inline error banner | If API call fails: red toast "Erro ao carregar clientes" + retry button. Table shows last known data or empty state. |

### 1.3 Search & Filters

| Feature | Complexity | Dependencies | UX Expectations |
|---------|------------|--------------|-----------------|
| **Global text search** (busca por nome, email, CPF/CNPJ) | Medium | Backend accepts `search` query param, SQL `ILIKE` on indexed columns | Single search input with magnifying glass icon. Debounced at 300ms client-side. Shows result count. Press Enter or wait 300ms to trigger. |
| **Filter by person type** (PF / PJ / Todos) | Low | Backend accepts `personType` query param, shadcn Select dropdown | Dropdown with 3 options. Selecting updates table immediately (server-side filter). URL updates to reflect filter state (`?personType=PF`). |
| **Filter by status** (Ativo / Bloqueado) | Low | Backend accepts `status` query param, Keycloak `enabled` flag mapping, shadcn Select | Dropdown: "Todos", "Ativo", "Bloqueado". Status badge color: green = ativo, red = bloqueado. |
| **Filter by registration date range** | Medium | `react-day-picker` + shadcn Calendar/popover, backend accepts `dateFrom` + `dateTo` | Date range picker with calendar popover. "De" and "Ate" fields. Quick presets: "Ultimos 7 dias", "Ultimos 30 dias", "Este mes". |
| **Filter state in URL** (shareable, bookmarkable, browser back/forward works) | Medium | `@tanstack/react-router` search params, `useNavigate` | All filter/sort/pagination state encoded in URL query params. Admin can bookmark a filtered view. Browser back/forward navigates filter history correctly. |
| **Clear all filters** | Low | Reset all search params to defaults | Single button "Limpar filtros" resets search, person type, status, date range, sort, and pagination to page 1. Appears only when at least one filter is active. |

### 1.4 View User Details

| Feature | Complexity | Dependencies | UX Expectations |
|---------|------------|--------------|-----------------|
| **User detail modal** (shadcn Dialog) | Medium | GET `/api/v1/admin/users/{id}`, Keycloak user representation merged with PostgreSQL data | Click row or "View" action → modal opens with all user fields in read-only layout. PF: nome, CPF, email, telefone, data de registro, status. PJ: razao social, CNPJ, email, telefone, responsavel, data de registro, status. No edit capability in this view. |
| **Keycloak metadata display** | Low | Keycloak Admin API `GET /admin/realms/{realm}/users/{id}` | Shows Keycloak user ID, creation date, last login, email verified status, number of failed login attempts (if brute force protection triggered). |
| **Copy user ID / Copy email** | Low | Clipboard API | Small copy icon next to ID and email fields. Click → tooltip "Copiado!" for 2s. |

### 1.5 Edit User Data

| Feature | Complexity | Dependencies | UX Expectations |
|---------|------------|--------------|-----------------|
| **Edit modal/form** (React Hook Form + Zod validation) | High | PUT `/api/v1/admin/users/{id}`, Keycloak Admin API update user, PostgreSQL update | Opens in shadcn Dialog. Pre-fills all editable fields. Zod schema mirrors server-side validation. Inline field errors on blur. Submit button disabled until form is valid. |
| **Editable fields** (nome/razao social, email, telefone) | Medium | Keycloak Admin API (email updates user credential), PostgreSQL (PF/PJ data update) | Email changes must update both Keycloak AND PostgreSQL atomically (or rollback on failure). CPF/CNPJ NOT editable (identifier immutability). |
| **Server-side validation feedback** | Medium | FluentValidation on backend, 400 response with field-level errors | If server rejects: form highlights invalid fields with error messages. Toast: "Corrija os erros no formulario." No data loss — form retains entered values. |
| **Optimistic UI update** (optional, rollback on failure) | High | TanStack Query `onMutate`/`onError`/`onSettled` pattern | Table row updates immediately after submit. If server rejects: rollback to previous state + error toast. Makes admin feel instant. |
| **Edit audit trail entry** | Low | Audit log table in PostgreSQL, triggered on successful edit | Every edit logs: admin ID, timestamp, fields changed (old value → new value), IP address. Visible in future audit log feature. |

### 1.6 Block / Unblock User

| Feature | Complexity | Dependencies | UX Expectations |
|---------|------------|--------------|-----------------|
| **Block dialog** (shadcn Alert Dialog — destructive confirmation) | Medium | Keycloak Admin API `PUT /admin/realms/{realm}/users/{id}` with `enabled: false` | Click "Bloquear" on row → red alert dialog: "Bloquear [nome]? Este cliente nao podera mais acessar o sistema." Confirm → user disabled in Keycloak. Toast: "Cliente bloqueado com sucesso." |
| **Unblock dialog** (shadcn Alert Dialog) | Low | Keycloak Admin API with `enabled: true` | Click "Desbloquear" → confirmation dialog (less severe styling): "Desbloquear [nome]?" Confirm → user re-enabled in Keycloak. Toast: "Cliente desbloqueado com sucesso." |
| **Status badge update** (immediate visual feedback) | Low | Optimistic update or refetch after action | Badge changes from green "Ativo" to red "Bloqueado" (or vice versa) without full page reload. |
| **Block reason** (optional text field, stored in PostgreSQL) | Low | PostgreSQL extension column for block reason | In block dialog, optional text area: "Motivo do bloqueio (opcional)." Stored for audit trail. Not required for v3.0 but recommended for compliance. |

### 1.7 LGPD-Compliant User Deletion

| Feature | Complexity | Dependencies | UX Expectations |
|---------|------------|--------------|-----------------|
| **Delete confirmation dialog** (shadcn Alert Dialog, double confirmation) | Medium | DELETE `/api/v1/admin/users/{id}`, Keycloak user deletion, PostgreSQL soft/hard delete | Click "Excluir (LGPD)" → red alert: "Excluir permanentemente os dados de [nome]? Esta acao nao pode ser desfeita." Requires typing user name or email to confirm (prevents accidental deletion). Second confirmation: "Tem certeza? Digite EXCLUIR para confirmar." |
| **Email notification to user before deletion** | High | Email service (SMTP or SendGrid/etc.), email template, deletion scheduling | Before actual deletion: sends email to user's registered address: "Seus dados serao excluidos em 7 dias conforme LGPD. Se deseja manter sua conta, faca login antes do prazo." Deletion scheduled for 7 days later (LGPD allows immediate execution, but 7-day grace period is industry best practice). |
| **Soft delete with retention period** (7 days, configurable) | High | PostgreSQL `deletedAt` column, background job for hard delete, exclusion from listing queries | User marked as `deletedAt` instead of immediately removed. Excluded from normal listing (unless admin filters "show deleted"). Hard delete job runs daily, permanently removes records older than 7 days. |
| **Data export before deletion** (JSON format, LGPD Art. 18 portability) | Medium | Serialization endpoint that aggregates all user data from PostgreSQL + Keycloak attributes | Before deletion: admin can click "Exportar dados" → downloads JSON file with all stored data for that user (PF/PJ fields, registration timestamps, audit log entries). Satisfies LGPD right to data portability. |
| **Deletion audit log entry** | Low | Audit log table, immutable record of deletion | Logs: admin ID, timestamp, user deleted (name, email, CPF/CNPJ), email sent (yes/no), retention period start, scheduled hard delete date. Cannot be deleted itself — immutable compliance record. |
| **Legal hold exception** (prevent deletion if legal obligation exists) | Medium | PostgreSQL flag `legalHold`, admin override checkbox | Checkbox in delete dialog: "Existe obrigacao legal para manter estes dados?" If checked: soft delete only (no hard delete scheduled), data retained indefinitely with `legalHold: true`. Admin must document reason. |

### 1.8 Auto Token Refresh & Session Resilience

| Feature | Complexity | Dependencies | UX Expectations |
|---------|------------|--------------|-----------------|
| **Cookie sliding expiration** | Low | ASP.NET Core Cookie Auth `SlidingExpiration = true` | Every request to admin API extends cookie lifetime by 8h from that moment. Active admins never get unexpectedly logged out. |
| **401 interceptor → redirect to login** | Low | `ky` fetch interceptor or Vinxi route guard | If cookie expires and API returns 401: admin is redirected to `/admin/login` with query param `?redirect=/admin/users`. After login, returns to the page they were on. No silent refresh — admin must re-authenticate. |
| **Concurrent session handling** (same admin logged in from two tabs) | Low | Cookie auth inherently supports this | Both tabs share the same cookie. Actions in one tab don't invalidate the other. If admin logs out in one tab, the other tab gets 401 on next request → redirects to login. |

### 1.9 Error Handling & Feedback

| Feature | Complexity | Dependencies | UX Expectations |
|---------|------------|--------------|-----------------|
| **Sonner toast notifications** (success, error, warning, info) | Low | `sonner` package (already installed) | Success: green toast, auto-dismiss 3s. Error: red toast, persists until dismissed. Warning: yellow toast, auto-dismiss 5s. Info: blue toast, auto-dismiss 3s. Position: top-right. Max 3 toasts visible simultaneously (older ones dismiss). |
| **Inline form validation errors** | Low | React Hook Form + Zod, server error mapping | Field-level errors appear below the input in red text. Form-level error banner at top if server returns general error (e.g., "Email ja esta em uso por outro cliente"). |
| **API error mapping** (400 → validation, 403 → forbidden, 404 → not found, 500 → server error) | Medium | Global error handler in Vinxi, structured ProblemDetails responses from .NET API | Consistent error UX regardless of which endpoint fails. 500 errors show generic "Erro interno do servidor. Tente novamente mais tarde." — never expose stack traces or internal details. |
| **Loading states on buttons** (submit, delete, block) | Low | shadcn Button `disabled` + `Loader2` icon | While action is in progress: button disabled, spinner appears inside button text area. Prevents double-submits. |

---

# 2. Differentiator Features (Nice to Have)

Features that elevate the admin panel from "functional" to "excellent." Not expected in v3.0 but add significant operational value.

| Feature | Complexity | Dependencies | UX Expectations | Why Defer |
|---------|------------|--------------|-----------------|-----------|
| **Bulk actions** (select multiple users → bulk block, bulk delete, bulk export) | High | Row selection (already in table stakes), bulk API endpoint accepting array of IDs, batch Keycloak operations | Checkbox multiple rows → action bar appears at bottom: "Bloquear selecionados (5)", "Excluir selecionados (5)". Single confirmation dialog for all. Progress indicator for batch operation. Partial success handling (3 of 5 blocked, 2 failed with reasons). | Requires bulk API design, batch error handling, and partial success UI. Complex enough to warrant its own phase. |
| **CSV export of user listing** | Medium | Backend CSV serialization endpoint, `Content-Disposition: attachment` header | "Exportar CSV" button above table → downloads current filtered/sorted view as CSV file. Columns match visible columns in table. Respects current filters and sort. File name: `clientes_YYYY-MM-DD.csv`. | Needs CSV serializer, handles large datasets (streaming, not loading all into memory), and column formatting logic. |
| **Audit log viewer** (dedicated tab showing admin actions) | High | Audit log table in PostgreSQL, paginated listing with filters, shadcn Tabs | New tab "Audit Log" in admin panel. Lists all admin actions (edit, block, delete) with: admin name, action type, target user, timestamp, details. Filterable by admin, action type, date range. | Requires audit log schema design, dedicated API endpoints, and separate UI. Out of scope for v3.0 core user management. |
| **Admin activity dashboard** (metrics: total users, active vs blocked, registrations this week/month) | Medium | Aggregation queries on PostgreSQL, charting library (Recharts or Chart.js) | Top of admin dashboard: 4 metric cards — "Total de Clientes", "Ativos", "Bloqueados", "Novos esta semana". Optional sparkline chart showing registration trend over 30 days. | Requires charting library, aggregation queries, and layout design. Nice to have but not critical for user management. |
| **Advanced search** (fuzzy matching, CPF/CNPJ format normalization) | Medium | Backend search with `pg_trgm` extension, input normalization | Search "12345678901" matches "123.456.789-01". Fuzzy name search: "Jonh" matches "John". Typo-tolerant. | Requires PostgreSQL `pg_trgm` extension, indexing strategy, and normalization logic. |
| **User detail page** (dedicated route `/admin/users/:id` instead of modal) | Medium | `@tanstack/react-router` route definition, separate page component | Full-page user detail view with tabs: "Dados Cadastrais", "Historico de Alteracoes", "Sessao Ativa". More screen real estate for complex data. | Modal is sufficient for v3.0. Dedicated page is a layout upgrade for when user data grows (e.g., activity history, linked accounts). |
| **Dark mode** | Low | Tailwind CSS `dark:` variant, theme toggle, localStorage preference | Admin can switch between light and dark themes. Preference persisted in localStorage. | Purely cosmetic. Not a priority for functional admin panel. |
| **Keyboard shortcuts** (Ctrl+K for search, J/K for row navigation, Delete for block) | Medium | Keyboard event listeners, focus management, accessibility considerations | Power users navigate table with keyboard arrows, press D to open delete dialog, Ctrl+K focuses search input. | Accessibility complexity, conflict with browser shortcuts, low ROI for admin panel with limited daily users. |
| **Webhook/notification on user deletion** | Medium | Webhook configuration, HTTP client retry logic, event publishing | When user is deleted (LGPD): POST to configured webhook URL with deletion event payload. Allows external systems to sync. | Requires webhook infrastructure, retry logic, and failure handling. Out of scope for v3.0. |
| **Impersonation** (admin temporarily acts as a user to debug issues) | High | Keycloak token impersonation or session swap, audit trail, clear visual indicator | Admin clicks "Visualizar como usuario" → opens new tab with user's profile view. Banner at top: "Visualizando como [nome] — voce e um administrador." Exit button returns to admin view. | Significant security implications, Keycloak configuration complexity, and audit requirements. Defer to security-focused phase. |

---

# 3. Anti-Features (Deliberately NOT Build in v3.0)

Features explicitly excluded with rationale.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| **User creation from admin panel** | v3.0 is about managing existing registrations, not creating users. User creation is the client-facing onboarding flow (v1.0). Admin-creating users bypasses the registration flow, validation, and consent collection. | Use the existing client-facing registration form. If admin needs to create users, that's a separate operational workflow (Phase 4+). |
| **Password reset from admin panel** | Password reset is Keycloak's native "Forgot Password" flow. Admin-resetting passwords creates security audit gaps and support liability. | Enable Keycloak's native password reset. Admin can only trigger a password reset email to the user (user completes flow themselves). |
| **Role assignment from admin panel** | The system has only two roles: `user` (default) and `admin` (manually assigned). Role management is an operational task, not a daily admin panel feature. | Assign `admin` role directly in Keycloak Admin Console. If role management becomes complex (e.g., custom roles, permissions), build a dedicated RBAC module. |
| **Real-time updates** (WebSockets, Server-Sent Events) | User management is not a real-time domain. Admins don't need to see new registrations appear instantly. WebSockets add infrastructure complexity for minimal UX gain. | Poll every 30s for new registrations (if dashboard metrics exist). For v3.0, manual refresh is sufficient. |
| **Multi-tenant admin** (admin sees only users from their organization) | The system is single-tenant. All admins see all users. Multi-tenancy introduces row-level security, tenant scoping, and data isolation complexity. | Not applicable to current architecture. If multi-tenant becomes a requirement, it's a fundamental architecture change, not a v3.0 feature. |
| **User password viewing or reset to known value** | Catastrophic security anti-pattern. Admins should never see or set user passwords. Keycloak stores passwords as bcrypt/PBKDF2 hashes — they cannot be retrieved. | Admin triggers password reset email to user (Keycloak flow). User sets their own new password. |
| **User activity tracking** (page views, session duration, click tracking) | Privacy violation under LGPD without explicit consent. Behavioral analytics is not user management. | If analytics are needed, use anonymized aggregate data (e.g., "10 logins today") — not per-user tracking. |
| **Custom user attributes / arbitrary field extension** | Schema-less user data creates consistency nightmares and makes validation impossible. PF/PJ schemas are well-defined. | Define new fields in database schema and API contracts. Migrate through normal development process. No runtime schema extension. |
| **User merge** (duplicate user consolidation) | Merging two Keycloak users requires token invalidation, session migration, data reconciliation, and audit trail complexity. | Prevent duplicates via CPF/CNPJ uniqueness validation (already in v1.0). If duplicates exist, handle manually via Keycloak Admin Console + database migration. |
| **File/avatar upload for admin panel** | The system doesn't store user avatars. Adding file upload infrastructure (storage, validation, CDN) is out of scope. | Not applicable. If user profiles need avatars in the future, build it as part of the profile editing milestone. |

---

# 4. Feature Dependency Map

### Core Dependency Graph

```
Admin login form (email + password)
  → POST /api/v1/admin/auth/login (validate against Keycloak Admin API)
    → Check user has "admin" role in Keycloak
      → Create ClaimsPrincipal with role claim
        → HttpContext.SignInAsync() → httpOnly cookie issued
          → Redirect to /admin/users (dashboard)

Cookie on subsequent requests
  → ASP.NET Core Cookie Auth middleware validates
    → [Authorize(Roles = "admin")] check passes
      → Request reaches admin controller
```

### User Listing Stack

```
GET /api/v1/admin/users?page=1&pageSize=20&search=John&personType=PF&status=active&sortBy=nome&sortOrder=asc
  → AdminAuthMiddleware validates cookie
    → AdminController.GetUsers() parses query params
      → EF Core builds IQueryable with:
          - Where(search ILIKE nome OR email OR CPF/CNPJ)
          - Where(personType filter)
          - Where(status → Keycloak enabled flag mapping)
          - OrderBy(sortBy, sortOrder)
          - Skip((page-1) * pageSize).Take(pageSize)
        → Returns PagedResult<T> { Items, TotalCount, Page, PageSize }
  → Frontend: TanStack Table renders with manualPagination
    → Sonner toast on error, skeleton on loading
```

### Edit User Stack

```
Click "Edit" on row
  → GET /api/v1/admin/users/{id} → opens Dialog with form
  → React Hook Form + Zod pre-fills data
  → Admin modifies fields → form validates client-side (Zod)
  → Submit → PUT /api/v1/admin/users/{id}
    → Backend validates (FluentValidation)
      → Update PostgreSQL (PF/PJ data)
      → Update Keycloak (email, if changed)
      → If either fails → rollback both → return 400 with field errors
      → Log audit entry
    → Frontend: optimistic update → refetch → toast success
    → If error: rollback optimistic → show field errors → toast error
```

### Block/Unblock Stack

```
Click "Bloquear" on row
  → shadcn Alert Dialog opens (destructive confirmation)
  → Admin confirms
    → PUT /api/v1/admin/users/{id}/block { enabled: false, reason?: string }
      → Keycloak Admin API: update user with enabled: false
      → PostgreSQL: update blockReason, blockedAt, blockedBy
      → Log audit entry
    → Frontend: update row status badge → toast success
```

### LGPD Delete Stack

```
Click "Excluir (LGPD)" on row
  → Alert Dialog #1: "Excluir permanentemente [nome]?" + optional reason
  → Admin types user name or email to confirm
  → Alert Dialog #2: "Digite EXCLUIR para confirmar"
  → Admin types "EXCLUIR" → DELETE /api/v1/admin/users/{id}
    → Check data, legalHold flag → block if true
    → Send email to user (if email service configured): "Dados serao excluidos em 7 dias"
    → PostgreSQL: soft delete (set deletedAt = now())
    → Keycloak: delete user from realm
    → Log immutable audit entry
    → Schedule hard delete job (7 days from now)
  → Frontend: remove row from table → toast "Exclusao agendada. Email enviado ao cliente."
```

### Dependency Graph (Visual)

```
                    ┌──────────────────────────┐
                    │   Admin Login + Cookie   │
                    │   Auth + CSRF + Role     │
                    │   Enforcement            │
                    └────────────┬─────────────┘
                                 │
                    ┌────────────▼─────────────┐
                    │   Admin Layout +         │
                    │   Navigation + Tabs      │
                    │   (Users / Audit / ...)  │
                    └────────────┬─────────────┘
                                 │
              ┌──────────────────┼──────────────────┐
              │                  │                  │
   ┌──────────▼──────────┐      │                  │
   │  User Listing Table  │      │                  │
   │  - Pagination        │      │                  │
   │  - Sorting           │      │                  │
   │  - Column visibility │      │                  │
   │  - Row selection     │      │                  │
   │  - Search            │      │                  │
   │  - Filters           │      │                  │
   └──────────┬──────────┘      │                  │
              │                  │                  │
    ┌─────────┼─────────┐       │                  │
    ▼         ▼         ▼       ▼                  ▼
┌───────┐ ┌───────┐ ┌───────┐ ┌───────┐ ┌─────────┐
│ View  │ │ Edit  │ │ Block │ │ LGPD  │ │ (Future:│
│ User  │ │ User  │ │/Unblk │ │ Delete│ │ Bulk/   │
│Detail │ │ Form  │ │ Dialog│ │ Dialog│ │ Export) │
│Modal  │ │ Dialog│ │       │ │       │ │         │
└───────┘ └───────┘ └───────┘ └───────┘ └─────────┘

Shared Dependencies (all features):
┌─────────────────────────────────────────────────────────┐
│  • Sonner Toast (error/success feedback)                │
│  • Loading states (skeletons, button spinners)          │
│  • Error handling (400/403/404/500 mapping)             │
│  • Auto token refresh / 401 → login redirect            │
│  • Filter state in URL (bookmarkable)                   │
└─────────────────────────────────────────────────────────┘
```

### Implementation Order (Recommended)

**Phase 1 — Foundation (Days 1-3):**
1. Admin login + cookie auth + CSRF middleware
2. Role `admin` enforcement on all admin controllers
3. Admin layout + navigation skeleton
4. GET `/api/v1/admin/users` with pagination, sorting, filters

**Phase 2 — Core Table (Days 4-6):**
5. TanStack Table integration with server-side pagination
6. Global search + person type filter + status filter
7. Date range filter
8. Column visibility + row selection
9. Loading skeleton + empty state + error state

**Phase 3 — User Actions (Days 7-10):**
10. View user detail modal
11. Edit user form with validation
12. Block/unblock dialog
13. Toast feedback for all actions

**Phase 4 — LGPD Compliance (Days 11-14):**
14. Delete confirmation dialog (double confirm)
15. Soft delete + email notification
16. Data export (JSON)
17. Audit log entries
18. Legal hold exception

**Phase 5 — Polish (Days 15-17):**
19. Filter state in URL
20. Optimistic UI updates
21. Error boundary + global error handler
22. Accessibility audit (keyboard navigation, screen reader labels)

---

## 5. Sources

- [ASP.NET Core Cookie Authentication — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0)
- [Role-based Authorization — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/mvc/security/authorization/roles?view=aspnetcore-10.0)
- [CSRF Prevention — OWASP Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html)
- [TanStack Table v8 — Documentation](https://tanstack.com/table/v8)
- [shadcn/ui Data Table Example](https://ui.shadcn.com/docs/components/data-table)
- [Build Admin Dashboard with shadcn/ui — freeCodeCamp](https://www.freecodecamp.org/news/build-an-admin-dashboard-with-shadcnui-and-tanstack-start/)
- [LGPD Compliance Guide — SecurePrivacy](https://secureprivacy.ai/blog/lgpd-compliance-requirements)
- [LGPD Explained — Termly](https://termly.io/resources/articles/brazils-general-data-protection-law/)
- [Keycloak Disable User via Admin API — Keycloak Forum](https://forum.keycloak.org/t/disable-user-using-keycloak-admin-rest-api/12267)
- [Keycloak Server Administration Guide](https://www.keycloak.org/docs/latest/server_admin/)
- [Optimistic UI Pattern — freeCodeCamp](https://www.freecodecamp.org/news/how-to-use-the-optimistic-ui-pattern-with-the-useoptimistic-hook-in-react/)
- [Token Refresh with Axios Interceptors — Medium](https://medium.com/@velja/token-refresh-with-axios-interceptors-for-a-seamless-authentication-experience-854b06064bde)
- [How to Build a Modern Admin Portal — Medium](https://medium.com/@wishula/how-to-build-a-modern-scalable-admin-portal-a-step-by-step-guide-3de1ffc2959e)
- [Modern Admin Dashboard Templates — BootstrapMade](https://bootstrapmade.com/modern-admin-bootstrap-html-admin-template/)
- [Audit Log Design Patterns — dev.to](https://dev.to/akkaraponph/comprehensive-research-audit-log-paradigms-gopostgresqlgorm-design-patterns-1jmm)