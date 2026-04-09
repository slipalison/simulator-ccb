## v3.0 Admin Backoffice — Stack Additions

> This document covers ONLY the new libraries required for the Admin Backoffice panel (v3.0). It assumes the v1.0/v2.0 stack (documented in `/QWEN.md`) is already in place. Do not re-list packages already established in the base stack.

---

### Backend Additions — .NET 10 (Admin API + Cookie Auth)

| Component | Package | Version | Rationale | Confidence |
|-----------|---------|---------|-----------|------------|
| Cookie Auth | `Microsoft.AspNetCore.Authentication.Cookies` | 10.0.x | Built-in to .NET 10 SDK. No extra NuGet needed — part of `Microsoft.AspNetCore.App` shared framework. Handles cookie issuance, validation, sliding expiration, and `SameSite`/`Secure` flags. | HIGH |
| JWT decoding (admin middleware) | `jose-jwt` | 5.3.0 | Zero-dependency JWT decoder. Used in custom middleware to read admin session cookie payload (extract `role`, `sub`) without signature validation — cookie auth already guarantees integrity. Lightweight alternative to `System.IdentityModel.Tokens.Jwt`. | HIGH |
| API versioning | `Asp.Versioning.Http` | 8.1.x | Optional but recommended: version admin endpoints (`/api/v1/admin/...`) separately from public client endpoints. Prevents breaking changes from affecting both surfaces. | MEDIUM |
| Pagination model | Manual (no package) | — | Use `IQueryable.Skip/Take` with a `PagedResult<T>` DTO. No library needed — EF Core pagination is trivial and adding a package introduces unnecessary coupling. | HIGH |

#### Cookie Auth Architecture for v3.0

The admin backoffice frontend runs on Vinxi (separate origin from the .NET API). Cookie-based auth requires:

1. **Login flow**: Admin submits credentials to `/api/v1/admin/auth/login` → API validates against Keycloak Admin API → creates `ClaimsPrincipal` with `role: admin` → `HttpContext.SignInAsync()` issues httpOnly cookie.
2. **Cookie config**: `SameSite=Lax` (or `Strict` in prod), `SecurePolicy=Always` (prod), `HttpOnly=true`, `MaxAge=8h`.
3. **CORS**: API must allow credentials (`AllowCredentials()`) from the Vinxi origin. Cookie cannot be read by JS (XSS-safe).
4. **Role check**: Custom `[Authorize(Roles = "admin")]` on all admin controllers. Built-in ASP.NET Core authorization — no extra package.

#### What the `jose-jwt` library does here

The cookie is httpOnly — JavaScript cannot read it. But the Vinxi server-side (SSR/SSG) may need to check admin auth status during server-side rendering. Options:
- **Option A**: Forward cookie to API via SSR proxy, let API return auth status.
- **Option B**: Decode the cookie value server-side in Vinxi using `jose-jwt` (Node.js `jose` package — already available if needed in frontend SSR context).

If the Vinxi SSR layer needs to decode the cookie, use the **Node.js `jose`** package (v6.x) on the frontend side instead of the .NET `jose-jwt`. The .NET `jose-jwt` is only needed if the .NET backend itself needs to decode tokens from external sources.

---

### Frontend Additions — React + Vinxi (Admin UI)

| Component | Package | Version | Rationale | Confidence |
|-----------|---------|---------|-----------|------------|
| Data table | `@tanstack/react-table` | 8.21.x | Headless table library. Provides sorting, filtering, pagination, row selection, column resizing out of the box. Already designed for React 19. Pairs with shadcn/ui Table component. | HIGH |
| **NEW** Radix Dialog | `@radix-ui/react-dialog` | 1.1.x | Required for "view user details", "edit user", and "LGPD delete confirmation" modals. Already implied by shadcn/ui Dialog. | HIGH |
| **NEW** Radix Alert Dialog | `@radix-ui/react-alert-dialog` | 1.1.x | Destructive action confirmations (block/unblock, delete LGPD). Different from Dialog — focused on confirm/cancel patterns with proper focus trapping. | HIGH |
| **NEW** Radix Checkbox | `@radix-ui/react-checkbox` | 1.1.x | Row selection in admin data table, bulk actions. | HIGH |
| **NEW** Radix Select | `@radix-ui/react-select` | 2.1.x | Filter dropdowns (status: active/blocked/deleted, person type: PF/PJ). Accessible, keyboard-navigable. | HIGH |
| **NEW** Radix Popover | `@radix-ui/react-popover` | 1.1.x | Date range filters, advanced filter panels. | MEDIUM |
| **NEW** Radix Tabs | `@radix-ui/react-tabs` | 1.1.x | Tab navigation for admin sections (Users, Audit Log, Settings). | MEDIUM |
| **NEW** React Datepicker | `react-day-picker` | 9.x | Date range picker for "registered between" filters. Zero-dependency, works with shadcn/ui Calendar. | HIGH |

#### Already Installed (No Action Needed)

| Component | Package | Status |
|-----------|---------|--------|
| Button | `@radix-ui/react-slot` + CVA | Already installed |
| Input | `@/components/ui/input` | Already installed |
| Label | `@radix-ui/react-label` | Already installed |
| Card | `@/components/ui/card` | Already installed |
| Dropdown Menu | `@radix-ui/react-dropdown-menu` | Already installed |
| Badge | `@/components/ui/badge` | Already installed |
| Skeleton | `@/components/ui/skeleton` | Already installed |
| Alert | `@/components/ui/alert` | Already installed |
| Separator | `@radix-ui/react-separator` | Already installed |
| Radio Group | `@radix-ui/react-radio-group` | Already installed |
| Toasts | `sonner` | Already installed |
| Forms | `react-hook-form` + `@hookform/resolvers` + `zod` | Already installed |
| Routing | `@tanstack/react-router` | Already installed |

#### shadcn/ui Components to Add (via `npx shadcn@latest add`)

These are UI wrappers around the Radix primitives above:

- `dialog` (wraps `@radix-ui/react-dialog`)
- `alert-dialog` (wraps `@radix-ui/react-alert-dialog`)
- `checkbox` (wraps `@radix-ui/react-checkbox`)
- `select` (wraps `@radix-ui/react-select`)
- `popover` (wraps `@radix-ui/react-popover`)
- `tabs` (wraps `@radix-ui/react-tabs`)
- `table` (plain HTML + Tailwind, no Radix dependency)
- `calendar` (wraps `react-day-picker`)
- `avatar` (wraps `@radix-ui/react-avatar`)

---

### Security / Cookie-Specific

| Component | Package | Version | Rationale | Confidence |
|-----------|---------|---------|-----------|------------|
| Cookie parsing (Node/SSR) | `jose` | 6.x | Node.js JWT/JWE/JWS library. Used in Vinxi SSR to decode httpOnly cookie and check admin auth status during server-side rendering. Zero-dependency, RFC-compliant. | HIGH |
| CSRF protection | Manual (Double Submit Cookie) | — | No package needed. Pattern: API generates CSRF token, stores in cookie (`SameSite=Strict`), frontend reads it (non-httpOnly) and sends as `X-CSRF-Token` header. Since admin cookie is httpOnly, a separate non-httpOnly CSRF cookie is required. | HIGH |

#### CSRF Strategy

Because the admin panel uses httpOnly cookies (not localStorage), the frontend is protected against XSS token theft but **vulnerable to CSRF**. Mitigation:

1. **SameSite=Lax** — blocks cross-site POST requests (adequate for most CSRF scenarios).
2. **Double Submit Cookie pattern** — additional CSRF token cookie (non-httpOnly) + header validation.
3. **Custom header requirement** — API validates `X-CSRF-Token` header matches CSRF cookie value.

No third-party library needed — this is implemented via custom middleware in .NET.

---

### What NOT to Use and Why

| Package | Why Avoid |
|---------|-----------|
| `System.IdentityModel.Tokens.Jwt` | Heavy, brings in `Microsoft.IdentityModel` dependency tree (~15 assemblies). Overkill for simple cookie decoding. `jose-jwt` is zero-dependency. |
| `AspNetCore.Identity` | Keycloak manages identities. Adding ASP.NET Identity creates dual identity systems — conflict and confusion. |
| `MediatR` | Commercial license (not open source). Use manual DI for CQRS (established pattern in v1/v2). |
| `MudBlazor` / `Radzen` | Blazor component libraries. Project uses React/Vinxi, not Blazor. |
| `DataTables.net` | jQuery-based. Incompatible with React architecture. |
| `AG-Grid` (enterprise) | Proprietary license for enterprise features. TanStack Table + shadcn is sufficient and MIT-licensed. |
| `react-table` (v7) | Deprecated — replaced by `@tanstack/react-table` v8. |
| `Material-UI` / `MUI` | Heavy bundle (~100KB+ gzipped). Project already uses shadcn/ui + Tailwind — adding MUI creates inconsistent design system. |
| `Ant Design` | Same as MUI — heavy, different design language, conflicts with Tailwind. |
| `Axios` | Project uses `ky`/`fetch`. Axios adds 13KB for features not needed (cookie auth means no manual token attachment). |
| `localStorage` for admin session | XSS vulnerability. Admin accounts are high-value targets. httpOnly cookies are mandatory. |
| `next-auth` / `Auth.js` | Designed for Next.js. Project uses Vinxi. Also designed for OAuth/social login — admin panel uses cookie auth with Keycloak backend. |
| `FluentAssertions` | v8+ commercial license (Xceed). Use Shouldly (MIT) — already in test stack. |

---

### Version Summary Table

#### Backend (New)

| Package | Version | License |
|---------|---------|---------|
| `Microsoft.AspNetCore.Authentication.Cookies` | 10.0.x (built-in) | Apache 2.0 |
| `jose-jwt` | 5.3.0 | MIT |
| `Asp.Versioning.Http` | 8.1.x | MIT |

#### Frontend (New)

| Package | Version | License |
|---------|---------|---------|
| `@tanstack/react-table` | 8.21.x | MIT |
| `@radix-ui/react-dialog` | 1.1.x | MIT |
| `@radix-ui/react-alert-dialog` | 1.1.x | MIT |
| `@radix-ui/react-checkbox` | 1.1.x | MIT |
| `@radix-ui/react-select` | 2.1.x | MIT |
| `@radix-ui/react-popover` | 1.1.x | MIT |
| `@radix-ui/react-tabs` | 1.1.x | MIT |
| `@radix-ui/react-avatar` | 1.1.x | MIT |
| `react-day-picker` | 9.x | MIT |
| `jose` (Node.js, SSR) | 6.x | MIT |

#### shadcn/ui to Install

| Component | Command |
|-----------|---------|
| `dialog` | `npx shadcn@latest add dialog` |
| `alert-dialog` | `npx shadcn@latest add alert-dialog` |
| `checkbox` | `npx shadcn@latest add checkbox` |
| `select` | `npx shadcn@latest add select` |
| `popover` | `npx shadcn@latest add popover` |
| `tabs` | `npx shadcn@latest add tabs` |
| `table` | `npx shadcn@latest add table` |
| `calendar` | `npx shadcn@latest add calendar` |
| `avatar` | `npx shadcn@latest add avatar` |

---

### Sources

- [ASP.NET Core Cookie Authentication — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0)
- [.NET 10 Authentication Enhancements — Auth0](https://auth0.com/blog/authentication-authorization-enhancements-dotnet-10/)
- [jose-jwt — GitHub](https://github.com/dvsekhvalnov/jose-jwt)
- [jose — npm](https://www.npmjs.com/package/jose)
- [TanStack Table v8 — Documentation](https://tanstack.com/table/v8)
- [shadcn/ui Components](https://ui.shadcn.com/docs/components)
- [Radix UI Primitives](https://www.radix-ui.com/primitives)
- [CSRF Prevention Cheat Sheet — OWASP](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html)
