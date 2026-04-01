---
phase: 01-infrastructure
plan: "03"
subsystem: infra
tags: [dotnet, aspnetcore, ddd, vinxi, react, typescript, docker, healthcheck]

requires:
  - phase: 01-infrastructure-01
    provides: compose.yaml with api and frontend service definitions
  - phase: 01-infrastructure-02
    provides: keycloak realm JSON for full stack boot

provides:
  - .NET 10 solution with four DDD projects (Domain, Application, Infrastructure, API)
  - GET /healthz endpoint for Docker Compose healthcheck
  - src/Onboarding.API/Dockerfile using dotnet watch for hot reload
  - Vinxi SPA frontend scaffold with usePolling: true for Windows Docker HMR
  - frontend/Dockerfile using node:22-alpine

affects:
  - 02-domain (will add domain models to Onboarding.Domain)
  - 05-registration-api (will implement controllers in Onboarding.API)
  - 09-frontend-login (will build on frontend scaffold)

tech-stack:
  added:
    - dotnet 10.0 (mcr.microsoft.com/dotnet/sdk:10.0)
    - vinxi 0.5.11
    - react 19.2.4
    - typescript 5.7.3
  patterns:
    - DDD project structure with enforced dependency hierarchy via project references
    - Minimal Program.cs (no OpenAPI/Swagger in dev scaffold)
    - Vinxi SPA router type with usePolling for Windows Docker compatibility

key-files:
  created:
    - Onboarding.sln
    - src/Onboarding.Domain/Onboarding.Domain.csproj
    - src/Onboarding.Application/Onboarding.Application.csproj
    - src/Onboarding.Infrastructure/Onboarding.Infrastructure.csproj
    - src/Onboarding.API/Onboarding.API.csproj
    - src/Onboarding.API/Program.cs
    - src/Onboarding.API/Controllers/HealthController.cs
    - src/Onboarding.API/Dockerfile
    - frontend/package.json
    - frontend/app.config.ts
    - frontend/src/client.tsx
    - frontend/tsconfig.json
    - frontend/Dockerfile
    - frontend/.dockerignore
  modified:
    - src/Onboarding.API/appsettings.json (added Keycloak and ConnectionStrings stubs)

key-decisions:
  - "Used --format sln (classic) instead of default .slnx — Dockerfile COPY Onboarding.sln requires classic format"
  - "package.json type:module — Vinxi is ESM-only; commonjs type prevents config loading"
  - "Removed OpenAPI/MapOpenApi from Program.cs — YAGNI for dev scaffold, add when needed"
  - "WeatherForecast boilerplate removed — keeps solution clean from day one"
  - "createApp (not defineConfig) is the correct Vinxi 0.5.x API — defineConfig is not exported"
  - "vinxi dev --port 5173 --host CLI flags used — port from app.config.ts alone is not respected"
  - "index.html added as SPA entry point to avoid SSR document-not-defined error at startup"

patterns-established:
  - "DDD: Domain has zero deps; Application → Domain; Infrastructure → Domain+Application; API → all three"
  - "Dockerfile: explicit per-project COPY for .csproj files ensures layer cache hits on restore"
  - "Vinxi: app.config.ts with usePolling: true + host:0.0.0.0 is the required pattern for Windows Docker HMR"
  - "Vinxi 0.5.x: use createApp (not defineConfig) and pass --port / --host via CLI, not config"
  - "SPA: index.html must exist as entry point; without it Vinxi falls back to SSR and crashes in Node"

requirements-completed: [INFRA-01, INFRA-04]

duration: ~60min (including 3 post-scaffold deviation fixes and human-verify checkpoint)
completed: "2026-04-01"
---

# Phase 01 Plan 03: Application Scaffold Summary

**.NET 10 DDD solution (four projects) + Vinxi React SPA, both with Dockerfiles — completing the full five-service stack ready for `docker compose up --wait`**

## Performance

- **Duration:** ~60 min (including 3 post-scaffold fixes and human-verify checkpoint)
- **Started:** 2026-04-01T18:22:15Z
- **Completed:** 2026-04-01T18:28:22Z (checkpoint approved)
- **Tasks:** 3 of 3 complete (2 auto + 1 human-verify, checkpoint approved by user)
- **Files modified:** 15 (14 scaffolded + frontend/index.html added as deviation)

## Accomplishments

- .NET 10 solution with four projects enforcing DDD dependency hierarchy — Domain, Application, Infrastructure, API
- GET /healthz endpoint wired to Docker Compose healthcheck (`curl -f http://localhost:8080/healthz`)
- Vinxi SPA frontend configured with `usePolling: true` and `host: 0.0.0.0` for reliable HMR inside Docker on Windows
- Both Dockerfiles ready: `mcr.microsoft.com/dotnet/sdk:10.0` with `dotnet watch`, `node:22-alpine` with `npm run dev`
- `dotnet build Onboarding.sln` passes with 0 errors
- All five services confirmed healthy by user via `docker compose up --wait`; Keycloak realm `onboarding` accessible via Admin API; stack idempotent across restart

## Task Commits

Each task was committed atomically:

1. **Task 1: .NET 10 solution scaffold** - `8abe7d4` (feat)
2. **Task 2: Vinxi SPA frontend scaffold** - `10291e2` (feat)
3. **Deviation fix: defineConfig → createApp** - `4343a3b` (fix)
4. **Deviation fix: vinxi dev --port 5173 --host** - `befdb19` (fix)
5. **Deviation fix: index.html SPA entry point** - `86efa45` (fix)
6. **Task 3: Human checkpoint** - approved by user ("approved")

## Files Created/Modified

- `Onboarding.sln` - Classic .sln format (required by Dockerfile COPY)
- `src/Onboarding.Domain/Onboarding.Domain.csproj` - Class library, no external deps
- `src/Onboarding.Application/Onboarding.Application.csproj` - Depends on Domain only
- `src/Onboarding.Infrastructure/Onboarding.Infrastructure.csproj` - Depends on Domain + Application
- `src/Onboarding.API/Onboarding.API.csproj` - Depends on all three
- `src/Onboarding.API/Program.cs` - Minimal setup: AddControllers + MapControllers
- `src/Onboarding.API/Controllers/HealthController.cs` - GET /healthz returns {status, timestamp}
- `src/Onboarding.API/appsettings.json` - Keycloak + ConnectionStrings stubs (overridden by env vars)
- `src/Onboarding.API/Dockerfile` - dotnet watch hot reload dev container
- `frontend/package.json` - vinxi 0.5.11, react 19.2.4, type:module
- `frontend/app.config.ts` - SPA router + usePolling + 0.0.0.0 host
- `frontend/src/client.tsx` - Minimal placeholder App component
- `frontend/tsconfig.json` - react-jsx, ESNext, bundler resolution
- `frontend/Dockerfile` - node:22-alpine, npm run dev
- `frontend/.dockerignore` - excludes node_modules, dist, .vinxi, .env*
- `frontend/index.html` - SPA shell entry point (added as deviation; required to prevent SSR crash)

## Decisions Made

- **Classic `.sln` over `.slnx`**: .NET 10 defaults to `.slnx` format, but Dockerfile uses `COPY Onboarding.sln .` — forced classic format with `--format sln`.
- **`type: "module"` in package.json**: Vinxi is ESM-only. The default `npm init -y` sets `commonjs` which prevents Vinxi from loading its config. Changed to `"module"`.
- **Removed generated boilerplate**: WeatherForecast.cs, WeatherForecastController.cs, Onboarding.API.http deleted to keep the solution clean from day one.
- **No OpenAPI in Program.cs**: `AddOpenApi` and `MapOpenApi` removed — YAGNI for this scaffold phase.
- **`createApp` not `defineConfig`**: Vinxi 0.5.x exports `createApp`; `defineConfig` does not exist in this version.
- **CLI port flags**: Port passed via `vinxi dev --port 5173 --host` — vite.server config values are not picked up from `app.config.ts` at runtime.
- **`index.html` SPA shell**: Required by Vinxi to serve the client router without triggering SSR mode in Node.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] .NET 10 defaults to .slnx format — forced classic .sln**
- **Found during:** Task 1 (solution scaffold)
- **Issue:** `dotnet new sln` created `Onboarding.slnx` (new XML format). Dockerfile `COPY Onboarding.sln .` would fail at build time since the file didn't exist.
- **Fix:** Deleted `.slnx`, re-ran with `--format sln` to produce the classic `.sln` format.
- **Files modified:** Onboarding.sln
- **Verification:** `dotnet build Onboarding.sln` exits 0
- **Committed in:** 8abe7d4

**2. [Rule 1 - Bug] Changed package.json `type` from `commonjs` to `module`**
- **Found during:** Task 2 (frontend scaffold)
- **Issue:** `npm init -y` defaults to `"type": "commonjs"`. Vinxi requires ESM (it uses ES module imports internally). Leaving `commonjs` would cause runtime errors when running `npm run dev`.
- **Fix:** Changed `"type": "commonjs"` to `"type": "module"` in package.json.
- **Files modified:** frontend/package.json
- **Verification:** `node -e "import('vinxi').then(m => console.log('vinxi resolvable'))"` succeeds
- **Committed in:** 10291e2

---

**3. [Rule 1 - Bug] Vinxi ignores port from app.config.ts in Docker context**
- **Found during:** Task 2 post-scaffold (frontend port not binding to 5173)
- **Issue:** Port 5173 set in `app.config.ts` vite.server config was not respected inside Docker; container bound on a different port.
- **Fix:** Changed Dockerfile CMD to `vinxi dev --port 5173 --host` to pass port explicitly via CLI.
- **Files modified:** `frontend/Dockerfile`
- **Verification:** `curl http://localhost:5173/` returned 200.
- **Committed in:** `befdb19`

**4. [Rule 1 - Bug] SSR crash: "document is not defined" at startup**
- **Found during:** Task 2 post-scaffold (frontend crashing after port fix)
- **Issue:** Without an `index.html` entry point, Vinxi attempted SSR of `client.tsx`, which references browser globals unavailable in Node.
- **Fix:** Created `frontend/index.html` as minimal SPA shell (`<div id="root">`) for Vinxi to serve.
- **Files modified:** `frontend/index.html` (created)
- **Verification:** Frontend loaded at `http://localhost:5173/` returning HTML with "Onboarding" heading.
- **Committed in:** `86efa45`

**5. [Rule 1 - Bug] Vinxi `defineConfig` is not exported in 0.5.x**
- **Found during:** Task 2 post-scaffold (container crash on startup)
- **Issue:** The plan specified `import { defineConfig } from "vinxi"` but Vinxi 0.5.x exports `createApp`. Container crashed with import error.
- **Fix:** Rewrote `app.config.ts` to use `import { createApp } from "vinxi"` with equivalent router config.
- **Files modified:** `frontend/app.config.ts`
- **Verification:** Frontend container started without import error.
- **Committed in:** `4343a3b`

---

**Total deviations:** 4 auto-fixed (1 blocking, 3 bug)
**Impact on plan:** All fixes necessary for Docker build and Vinxi runtime. The three Vinxi fixes were cascading — each revealed only after the previous was resolved. No scope creep.

## Issues Encountered

The Vinxi scaffold in the plan used the `defineConfig` API which does not exist in Vinxi 0.5.x. Three cascading issues required resolution before the container ran cleanly: wrong import name → container crash, port from config not picked up → wrong port bound, missing `index.html` → SSR fallback crash. All were diagnosed and fixed sequentially without human intervention.

## Known Stubs

- `frontend/src/client.tsx` — App component with hardcoded placeholder text ("Infrastructure phase — placeholder"). Intentional for this plan; frontend features are built in phases 07-09.
- `src/Onboarding.API/appsettings.json` — Keycloak and ConnectionStrings fields are empty strings. Runtime values injected via Docker Compose environment variables (`ConnectionStrings__AppDb`, etc.).

## Next Phase Readiness

- Full five-service stack verified healthy by user — Phase 01 infrastructure is complete
- Phase 02 (Keycloak Security Hardening) prerequisites met: realm exists, stack boots cleanly
- Phase 03 (Backend Domain Layer) can add entities to `Onboarding.Domain` project
- Phase 07 (Frontend Foundation) can build on the Vinxi scaffold in `frontend/`
- No blockers for Phase 02 start

---
*Phase: 01-infrastructure*
*Completed: 2026-04-01*
