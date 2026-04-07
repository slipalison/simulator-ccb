# Phase 7: Frontend Foundation - Context

**Gathered:** 2026-04-06
**Status:** Ready for planning
**Source:** User direct input (inline context, bypassing discuss-phase)

<domain>
## Phase Boundary

Scaffold the frontend with vinext (cloudflare/vinext — SSR-enabled, Next.js-style conventions on Vite), shadcn/ui as the design system, TanStack Router for type-safe routing, React Hook Form + Zod for form validation, and Tailwind CSS v4. The result must be a working application shell with Atomic Design directory structure, at least one example component at each level, a type-safe 404 route, and a working form with inline validation errors.

**Architecture:** SSR (Server-Side Rendering) via vinext — NOT pure SPA. The frontend uses vinext's SSR model (Next.js-style file conventions).

Style philosophy: **simple and minimalist** — leverage shadcn/ui defaults, avoid over-engineering the UI layer.

</domain>

<decisions>
## Implementation Decisions

### Framework
- **Framework:** vinext — https://github.com/cloudflare/vinext (Cloudflare's Vite-based meta-framework)
- **CONFIRMED by user (2026-04-06):** Replace existing Vinxi scaffold with vinext
- **CONFIRMED by user (2026-04-06):** Use vinext with SSR — user explicitly accepted SSR mode when informed cloudflare/vinext is SSR-only
- vinext reimplements Next.js-style conventions on Vite; use its project scaffold and file conventions
- Do NOT fall back to plain Vinxi — the user confirmed vinext+SSR is the desired approach
- The existing Vinxi scaffold from Phase 1 will be replaced/migrated

### Design System
- **Design system:** shadcn/ui — https://github.com/shadcn-ui/ui
- Use shadcn/ui components as the atomic building blocks (Button, Input, Label, Card, etc.)
- Install components via `npx shadcn@latest add` — do not copy/paste manually
- Style philosophy: simple and minimalist — use shadcn defaults, avoid custom theme overrides unless necessary

### Styling
- **Tailwind CSS v4** (required by shadcn/ui v2+ and project stack)
- No custom color tokens unless shadcn defaults are insufficient
- Utility-first; no CSS modules or styled-components

### Routing
- **TanStack Router v1** — type-safe file-based or code-based routing
- Unknown paths must render a typed 404 component (NotFoundRoute)
- Routes needed in this phase: `/` (home/landing placeholder), `*` (404)

### Forms
- **React Hook Form v7 + Zod v3** — schema-driven, inline validation
- At least one example form at the molecule/organism level demonstrating inline error display
- Validation errors shown inline beneath fields (not toast/alert)

### Atomic Design Structure
- Directory: `src/components/` split into `atoms/`, `molecules/`, `organisms/`, `templates/`, `pages/`
- At minimum one example component at each level:
  - atom: a shadcn Button or Input wrapper
  - molecule: a labeled form field (Label + Input + error message)
  - organism: a simple example form (e.g., placeholder contact form using RHF + Zod)
  - template: a page layout shell (header + main + footer slots)
  - page: the home page and the 404 page

### Claude's Discretion
- Exact vinext project init command and config file structure (research vinext docs)
- Whether to use file-based routing (vinext convention) or manual route tree
- shadcn/ui init configuration details (baseColor, cssVariables, etc.)
- TypeScript path aliases setup (`@/components`, `@/lib`, etc.)
- Dockerfile / compose wiring for the frontend service (should mirror existing pattern from Phase 1)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Infrastructure
- `.planning/phases/01-infrastructure/01-01-PLAN.md` — Docker Compose structure, frontend service definition
- `.planning/phases/01-infrastructure/01-03-PLAN.md` — Vinxi frontend scaffold (original), may need adaptation for vinext

### Requirements
- `.planning/REQUIREMENTS.md` — FRONT-01 through FRONT-05 (all Phase 7 requirements)
- `.planning/ROADMAP.md` — Phase 7 success criteria

### Project Stack
- `CLAUDE.md` — Tech stack decisions, forbidden packages, conventions

### External
- https://github.com/cloudflare/vinext — vinext framework (read README for init and config)
- https://github.com/shadcn-ui/ui — shadcn/ui (read docs for init with Tailwind v4)

</canonical_refs>

<specifics>
## Specific Ideas

- Keep it **simple and minimalist** — the user's explicit preference
- shadcn/ui handles the visual component library; don't invent custom components for things shadcn covers
- vinext is the framework; don't fall back to plain Vinxi or Next.js
- The example form in organism/molecule level should be functional (RHF + Zod working inline validation) even if the submit handler is a no-op in this phase

</specifics>

<deferred>
## Deferred Ideas

- Full registration/login forms — those belong to Phase 8 and 9
- API integration — Phase 8+
- Authentication state / token management — Phase 9

</deferred>

---

*Phase: 07-frontend-foundation*
*Context gathered: 2026-04-06 via user inline input*
