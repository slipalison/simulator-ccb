# T-1: Vinext Discovery — BLOCKED

**Date:** 2026-05-24
**Specialist:** jdi-doer-onboarding-keycloak-frontend-vinext
**Time spent:** ~20 min (within 1h spike mandate)
**Status:** BLOCKED — fundamental product mismatch

---

## Executive finding

**`cloudflare/vinext` is NOT a drop-in replacement for Vinxi.** It is a completely different product.

- **Vinxi** = meta-bundler framework (Nitro/h3 based). Used to compose multi-router Node.js apps with SPA + API server routers. Our `app.config.ts` uses its `createApp({ routers: [...] })` API.
- **Vinext** = "The Next.js API surface, reimplemented on Vite." It is a Vite-backed clone of the Next.js CLI (`next dev` → `vinext dev`). It consumes `next.config.js`, `app/` or `pages/` directories, and deploys to Cloudflare Workers.

These products share no common API surface. Migration is not a "drop-in swap."

---

## Evidence

### npm package metadata (`vinext@0.0.52` — latest as of 2026-05-24)

```
Description: "Run Next.js apps on Vite. Drop-in replacement for the next CLI."
Peer deps: react ^19.2.6, react-dom ^19.2.6, react-server-dom-webpack ^19.2.6,
           vite ^7.0.0 || ^8.0.0
Binary:    "vinext": "dist/cli.js"
```

No `h3`, no `createApp`, no router composition API.

### GitHub README (https://github.com/cloudflare/vinext)

> The Next.js API surface, reimplemented on Vite.

> 🚧 Experimental — under heavy development. This project is an experiment in AI-driven software development. [...] There may be bugs, rough edges, or things that don't work. **Use at your own risk.**

CLI commands: `vinext dev`, `vinext build`, `vinext start`, `vinext deploy` (to Cloudflare Workers), `vinext init` (migrates a **Next.js** project to Vinext).

It auto-detects `app/` or `pages/` directories and loads `next.config.js`. No `app.config.ts`.

### GitHub release cadence

```
v0.0.52  Latest  2026-05-22
v0.0.51          2026-05-19
v0.0.50          2026-05-14
```

Latest v0.0.52 is 2 days old (2026-05-22). Does NOT meet the "≥7 days without revert" stability bar from PLAN.md T-1 acceptance criteria. Even if it did, the product mismatch supersedes the age check.

---

## Incompatibility analysis

### Our current architecture (Vinxi)

```typescript
// app.config.ts
import { createApp } from "vinxi";  // <-- Vinxi-only API
export default createApp({
  routers: [
    { name: "public",    type: "static", dir: "./public" },
    { name: "auth",      type: "http",   handler: "./auth-server.ts", base: "/auth" },
    { name: "api-proxy", type: "http",   handler: "./server.ts",      base: "/api" },
    { name: "client",    type: "spa",    handler: "./index.html" },
  ],
});
```

The `http`-type routers (`auth-server.ts`, `server.ts`) use h3 primitives:
- `defineEventHandler`, `createRouter`, `getQuery`, `setCookie`, `deleteCookie`,
  `getCookie`, `sendRedirect` (from `"h3"`)

This is the BFF (D-39, preserved permanently). It handles ACF+PKCE auth flow and the API proxy.

### What Vinext expects

A Next.js project structure:
```
pages/ or app/
next.config.js
public/
```

No `app.config.ts`. No `createApp`. No h3 http routers. Its server is opaque (Vite dev server + SSR build). No extension point for custom h3 route handlers.

### h3 compatibility gap

Vinext does not expose h3 at all. Its runtime is either Vite dev server (local) or Cloudflare Workers (deploy). Custom server-side handlers require Next.js `API routes` (`pages/api/*.ts`) or `Route Handlers` (`app/api/route.ts`) — both in the Next.js pattern, incompatible with our h3 handler files.

Migrating our BFF to Vinext would require:
1. Rewriting `auth-server.ts` as a Next.js Route Handler (App Router) or API route (Pages Router)
2. Rewriting `server.ts` similarly
3. Converting `app.config.ts` to `next.config.js`
4. Restructuring `src/` into `app/` or `pages/`
5. Handling that TanStack Router (file-based, our routing layer) is not native to Next.js's router

This is NOT a migration — it is a full rewrite of the frontend architecture. It contradicts:
- **D-39** (BFF preserved permanently — server.ts + auth-server.ts unchanged)
- **D-4** (frontend independence — shared-nothing between SPAs; both would need rewriting)
- Phase scope (pure runtime swap, "not touching code of auth, backend C#, Keycloak realms or API contract")

### Additional disqualifiers

- **Experimental warning:** README explicitly warns "under heavy development [...] There may be bugs, rough edges, or things that don't work. Use at your own risk." — not production-ready for a security-critical auth flow.
- **Vite ^7.0.0 peer dep:** Our project uses Vite 5.x (via Vinxi 0.5.11). Vinext requires Vite 7 or 8, meaning all Vite plugins would need compatibility verification.
- **react-server-dom-webpack:** Vinext requires this for App Router RSC. We are a pure SPA — RSC is out of scope and would add significant complexity.
- **TanStack Router conflict:** We use `@tanstack/react-router` with file-based routing. Next.js-style routing (which Vinext implements) conflicts fundamentally with TanStack Router's client-side routing.
- **Cloudflare Workers target:** Vinext's production deployment model is Cloudflare Workers, not Node.js containers. Our compose.yaml runs Node 22-alpine. The entire deployment model changes.

---

## Recommended alternatives

The PLAN.md states the escalation path is to document incompatibility + alternatives, then STOP. The following options preserve the architectural constraints (D-39 BFF, D-4 independence, ACF+PKCE, Node compose runtime).

### Option A: Stay on Vinxi — close Phase 54 as N/A

**Recommendation: PREFERRED**

Vinxi 0.5.11 is stable, production-serving our auth flow today. The migration debt item in ROADMAP.md was based on the assumption that "Vinext = improved Vinxi." That assumption is false.

- Remove Phase 54 from ROADMAP.md (or mark CANCELLED with explanation).
- Remove Phase 55 (backoffice migration) for the same reason.
- Update `.jdi/DECISIONS.md` with D-43: "Vinext migration cancelled — product mismatch discovered in T-1 spike."

No code changes needed. Zero risk.

### Option B: Upgrade to a newer Vinxi release

If the goal was modernization/maintenance, check whether Vinxi has released a newer stable version:

```bash
npm view vinxi versions --json | tail -5
```

If Vinxi 0.5.x → 0.6.x exists, it may have a migration guide. This keeps the `createApp` + h3 routers architecture intact.

**Caveat:** Check changelog for breaking changes before committing. h3 override pinned at `1.15.11` in `overrides` would need verification.

### Option C: Migrate BFF to Hono (if h3 modernization is the real goal)

If the underlying goal is to modernize the server-side layer:
- Hono is a modern, lightweight HTTP framework compatible with multiple runtimes (Node.js, Cloudflare Workers, Deno, Bun)
- It has a comparable API to h3 (`c.cookie()`, `c.redirect()`, etc.)
- Phase would be: Vinxi still as bundler, but swap h3 in auth-server.ts + server.ts to Hono handlers
- Scope: still Node.js compose runtime, no architecture change needed

This is NOT what Phase 54 described — it would be a new phase.

### Option D: Full Next.js migration (NOT recommended)

Full rewrite: port `src/` to Next.js App Router, rewrite BFF as Route Handlers, drop TanStack Router, then run under Vinext. Months of work, very high regression risk on ACF+PKCE, violates D-39, violates D-4 (both SPAs need rewriting simultaneously). **Reject.**

---

## Decision needed from product owner

1. **Is "Vinext" actually what was intended?** The PROJECT.md entry says "Vinext (Cloudflare fork)" — the Cloudflare fork of **what**? If the intent was a Cloudflare fork of **Vinxi** (not Next.js), such a fork does not appear to exist as an npm package.

2. **Should Phase 54 be cancelled** (Option A — stay on Vinxi) or **redirected** (Option B — upgrade Vinxi version, or Option C — different goal)?

---

## STOP condition

Per PLAN.md T-1 acceptance criteria:

> Spike ≤1h. Se Vinext for incompat profundo com h3, escalate via SUMMARY.md "BLOCKED — recommended alternatives" antes de prosseguir.

This spike took ~20 minutes. Incompatibility is fundamental (product mismatch, not a configuration gap). **Proceeding to T-2 through T-8 would be wrong.**

DISCOVERY.md written. SUMMARY.md will carry the BLOCKED verdict. Reviewer routes to escalation.
