# Phase 1: Infrastructure - Research

**Researched:** 2026-04-01
**Domain:** Docker Compose orchestration — Keycloak 26.x, PostgreSQL 16 x2, .NET 10 API, Vinxi SPA
**Confidence:** HIGH (official docs + verified patterns for all critical questions)

---

## Summary

Phase 1 establishes the foundation: a single `docker compose up` that boots all five services (API,
frontend, app_db, keycloak_db, Keycloak) in the correct order with health-checked dependencies.
The single most critical risk is the Keycloak realm auto-provisioning — getting the realm import
JSON right on first boot, with service account role mappings, is the hardest part of this phase.
Everything else (healthchecks, network isolation, Dockerfile patterns) follows well-documented paths.

The second-most critical concern is Keycloak 26.x's changed defaults from older versions:
admin credentials use `KC_BOOTSTRAP_ADMIN_USERNAME/PASSWORD` (not `KEYCLOAK_ADMIN`), health
checks are exposed on port **9000** (not 8080), and `curl` is absent from the container image
requiring a TCP socket workaround in healthchecks.

**Primary recommendation:** Use `start-dev` mode for local dev (relaxed hostname strictness,
HTTP enabled by default), mount `onboarding-realm.json` to `/opt/keycloak/data/import/`, and
pass `--import-realm` in the command. Wire all `depends_on` with `condition: service_healthy`.

---

## Project Constraints (from CLAUDE.md)

- **Tech Stack**: .NET 10 + React/Vinxi + PostgreSQL + Keycloak — stack defined by user, not negotiable
- **Infra**: Everything must run in Docker Compose locally
- **Security**: Keycloak must be hardened against documented vulnerabilities
- **API Style**: ASP.NET Core Controllers (no Minimal API)
- **Observability**: Serilog + OpenTelemetry required from the start
- **No MediatR**: Commercial license — use manual DI for CQRS
- **No Moq**: Use NSubstitute
- **GSD Workflow**: Do not make direct repo edits outside a GSD workflow command

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| INFRA-01 | Docker Compose orchestrates all services (API, frontend, PostgreSQL x2, Keycloak) | See Standard Stack — Docker Compose V2 topology with 5 services |
| INFRA-02 | PostgreSQL dedicated for application data (app_db) | See Architecture Patterns — separate postgres:16-alpine container on port 5432 |
| INFRA-03 | PostgreSQL dedicated for Keycloak (keycloak_db) — isolated from app_db | See Architecture Patterns — separate postgres:16-alpine container on port 5433 internal |
| INFRA-04 | Healthchecks on all services with depends_on condition: service_healthy | See Code Examples — exact pg_isready and Keycloak port-9000 healthcheck commands |
| INFRA-05 | Keycloak realm "onboarding" configured with clients, policies, and roles | See Code Examples — realm JSON structure with both clients, brute force, password policy |
</phase_requirements>

---

## Standard Stack

### Core Infrastructure Images

| Image | Version | Purpose | Why This |
|-------|---------|---------|----------|
| quay.io/keycloak/keycloak | 26.1 | Identity provider | Project-specified; official Keycloak image |
| postgres | 16-alpine | Application database (app_db) | Alpine reduces image size; 16 is stable |
| postgres | 16-alpine | Keycloak database (keycloak_db) | Same image, separate container, separate volume |
| mcr.microsoft.com/dotnet/sdk | 10.0 | .NET 10 API dev container | Official MS image; includes SDK for dotnet watch |
| mcr.microsoft.com/dotnet/aspnet | 10.0 | .NET 10 API runtime base | Lean runtime-only image for prod stage |
| node | 22-alpine | Frontend dev container | LTS; alpine for smaller footprint |

### Docker Compose Version

Docker Compose V2 (plugin-based, `docker compose` not `docker-compose`). Confirmed available:
Docker 29.2.1 + Compose v5.0.2 on this machine. Use `compose.yaml` (preferred name) or
`docker-compose.yml` — both work.

### Verified Package Versions (npm, 2026-04-01)

| Package | Verified Version |
|---------|-----------------|
| vinxi | 0.5.11 |
| react | 19.2.4 |
| @tanstack/react-router | 1.168.10 |

**Installation (frontend):**
```bash
npm create vinxi@latest frontend --template react-router
```

---

## Architecture Patterns

### Recommended Directory Structure

```
repo-root/
├── compose.yaml                    # Single Compose file for all services
├── .env.example                    # Template — commit this
├── .env                            # Actual secrets — gitignore this
├── keycloak/
│   └── onboarding-realm.json       # Realm import file (auto-imported on first boot)
├── src/
│   ├── Onboarding.Domain/
│   ├── Onboarding.Application/
│   ├── Onboarding.Infrastructure/
│   └── Onboarding.API/
│       └── Dockerfile
└── frontend/
    ├── app.config.ts
    ├── Dockerfile
    └── src/
```

### Pattern 1: Docker Compose Service Topology

```yaml
# compose.yaml — logical structure (not complete file)
services:

  app_db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: onboarding
      POSTGRES_USER: appuser
      POSTGRES_PASSWORD: ${APP_DB_PASSWORD}
    volumes:
      - app_data:/var/lib/postgresql/data
    ports:
      - "127.0.0.1:5432:5432"   # Bind to loopback only
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U appuser -d onboarding"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s

  keycloak_db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: keycloak
      POSTGRES_USER: kcuser
      POSTGRES_PASSWORD: ${KC_DB_PASSWORD}
    volumes:
      - keycloak_data:/var/lib/postgresql/data
    # No host port exposure — Keycloak accesses via internal network only
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U kcuser -d keycloak"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s

  keycloak:
    image: quay.io/keycloak/keycloak:26.1
    command: ["start-dev", "--import-realm"]
    environment:
      KC_DB: postgres
      KC_DB_URL: jdbc:postgresql://keycloak_db:5432/keycloak
      KC_DB_USERNAME: kcuser
      KC_DB_PASSWORD: ${KC_DB_PASSWORD}
      KC_BOOTSTRAP_ADMIN_USERNAME: admin          # NEW in 26.x (was KEYCLOAK_ADMIN)
      KC_BOOTSTRAP_ADMIN_PASSWORD: ${KC_ADMIN_PASSWORD}
      KC_HEALTH_ENABLED: "true"
      KC_HTTP_ENABLED: "true"                     # Explicit for start-dev clarity
      KC_HOSTNAME_STRICT: "false"                 # Default in start-dev; explicit for docs
      KC_HOSTNAME: http://localhost:8180
    volumes:
      - ./keycloak/onboarding-realm.json:/opt/keycloak/data/import/onboarding-realm.json:ro
    ports:
      - "127.0.0.1:8180:8080"   # Map container 8080 → host 8180; bind loopback
    depends_on:
      keycloak_db:
        condition: service_healthy
    healthcheck:
      # curl not available in Keycloak 26 image — use TCP socket
      test: ["CMD-SHELL", "exec 3<>/dev/tcp/127.0.0.1/9000 && echo -e 'GET /health/ready HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3 && cat <&3 | grep -q '200 OK'"]
      interval: 15s
      timeout: 10s
      retries: 10
      start_period: 60s

  api:
    build:
      context: .
      dockerfile: src/Onboarding.API/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_HTTP_PORTS: "8080"
      ConnectionStrings__AppDb: Host=app_db;Port=5432;Database=onboarding;Username=appuser;Password=${APP_DB_PASSWORD}
      Keycloak__Authority: http://keycloak:8080/realms/onboarding
      Keycloak__AdminClientId: onboarding-api-admin
      Keycloak__AdminClientSecret: ${KC_ADMIN_CLIENT_SECRET}
      Keycloak__RealmUrl: http://keycloak:8080/realms/onboarding
    ports:
      - "127.0.0.1:8080:8080"
    depends_on:
      app_db:
        condition: service_healthy
      keycloak:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/healthz || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    environment:
      VITE_API_URL: http://localhost:8080
      VITE_KEYCLOAK_URL: http://localhost:8180
      VITE_KEYCLOAK_REALM: onboarding
      VITE_KEYCLOAK_CLIENT_ID: onboarding-app
    ports:
      - "127.0.0.1:5173:5173"
    depends_on:
      api:
        condition: service_healthy

volumes:
  app_data:
  keycloak_data:

networks:
  default:
    name: onboarding-net
```

**CRITICAL port mapping note:** The host-side of every port binding uses `127.0.0.1:` prefix
(loopback only). This prevents the services from being accessible on other network interfaces.
In production, a reverse proxy handles public exposure. The internal Docker hostnames use
container names (`app_db`, `keycloak_db`, `keycloak`, `api`).

### Pattern 2: Keycloak Startup — start-dev vs start

For local development, use `start-dev`:

| Behavior | start-dev | start (production) |
|----------|-----------|-------------------|
| KC_HOSTNAME_STRICT | false (default) | Must explicitly set |
| KC_HTTP_ENABLED | true (default) | false (default) — must enable |
| TLS/HTTPS | Not required | Required by default |
| Caching | Disabled | Enabled |
| Database required | Optional (can use embedded H2) | Required |

**Decision for this project:** Use `start-dev` for local Docker Compose. The `start-dev` defaults
match what dev needs (no cert, relaxed hostname). This is never exposed beyond loopback anyway.

### Pattern 3: Keycloak Realm Import on First Boot

```
Volume mount:  ./keycloak/onboarding-realm.json  →  /opt/keycloak/data/import/onboarding-realm.json
Startup flag:  --import-realm  (in command array)
File naming:   MUST be <realm-name>-realm.json  →  onboarding-realm.json
Skip behavior: If realm already exists, import is silently skipped (idempotent on restart)
```

Source: [Keycloak official import/export docs](https://www.keycloak.org/server/importExport) — HIGH confidence.

### Pattern 4: .NET 10 Dockerfile for Development (dotnet watch)

```dockerfile
# src/Onboarding.API/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
WORKDIR /app

# Copy solution and project files for layer caching
COPY *.sln .
COPY src/Onboarding.Domain/*.csproj ./src/Onboarding.Domain/
COPY src/Onboarding.Application/*.csproj ./src/Onboarding.Application/
COPY src/Onboarding.Infrastructure/*.csproj ./src/Onboarding.Infrastructure/
COPY src/Onboarding.API/*.csproj ./src/Onboarding.API/
RUN dotnet restore

# Copy source
COPY src/ ./src/

WORKDIR /app/src/Onboarding.API
EXPOSE 8080

# Hot reload via dotnet watch
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile", "--urls", "http://0.0.0.0:8080"]
```

**For Docker Compose hot reload**, pair with `develop.watch` section in compose.yaml or use a
bind-mount volume. The `dotnet watch` command monitors file changes and hot-reloads (for
supported changes) or restarts (for breaking changes).

Volume mount approach (simpler for initial phase):
```yaml
  api:
    volumes:
      - ./src:/app/src    # Bind-mount source for dotnet watch
      - /app/src/Onboarding.API/obj   # Anonymous volume to prevent host obj folder override
      - /app/src/Onboarding.API/bin   # Anonymous volume to prevent host bin folder override
```

### Pattern 5: Vinxi SPA Dockerfile + Vite HMR in Docker

Vinxi 0.5.x is based on Vite. Vite HMR requires explicit host/port configuration when inside
a container.

```javascript
// app.config.ts — Vinxi SPA configuration with Docker HMR
import { defineConfig } from "vinxi";

export default defineConfig({
  routers: [
    {
      name: "public",
      type: "static",
      dir: "./public",
    },
    {
      name: "client",
      type: "spa",
      handler: "./src/client.tsx",
      vite: {
        server: {
          host: "0.0.0.0",       // Required: listen on all interfaces in container
          port: 5173,
          hmr: {
            host: "localhost",   // Browser connects to this host for HMR WebSocket
            port: 5173,          // Must match exposed Docker port
            clientPort: 5173,    // Port browser uses (if Docker maps differently, change this)
          },
          watch: {
            usePolling: true,    // Required: filesystem events unreliable in Docker on Windows
            interval: 1000,
          },
        },
      },
    },
  ],
});
```

**Windows-specific:** `usePolling: true` is required on Windows because inotify events from bind
mounts don't reliably propagate to the container. Without polling, HMR never triggers.

### Pattern 6: Keycloak Health Check (port 9000, no curl)

```yaml
# In Keycloak service healthcheck:
healthcheck:
  test: ["CMD-SHELL", "exec 3<>/dev/tcp/127.0.0.1/9000 && echo -e 'GET /health/ready HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3 && cat <&3 | grep -q '200 OK'"]
  interval: 15s
  timeout: 10s
  retries: 10
  start_period: 60s
```

Key facts:
- Health endpoint is on management port **9000** (not 8080), enabled with `KC_HEALTH_ENABLED=true`
- `curl` is NOT present in the Keycloak 26.x container image
- The bash TCP socket trick (`/dev/tcp/`) is the recommended workaround
- The `start_period: 60s` gives Keycloak time to start before health checks begin counting failures
- Use `retries: 10` — Keycloak startup with realm import can take 30-60 seconds

### Pattern 7: PostgreSQL Health Check

```yaml
healthcheck:
  test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
  interval: 10s
  timeout: 5s
  retries: 5
  start_period: 30s
```

`pg_isready` is included in all official PostgreSQL images including alpine variants. The `-U`
flag prevents log warnings from PostgreSQL about anonymous connections.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Service startup ordering | Shell scripts that sleep and retry | Docker Compose `depends_on: condition: service_healthy` | Built-in, declarative, restarts correctly |
| Keycloak realm setup | Keycloak Admin API calls in a startup script | Realm import JSON + `--import-realm` | Official mechanism; idempotent; no external script needed |
| Secret management in dev | Plaintext secrets in compose.yaml | `.env` file with compose env interpolation | Standard Compose pattern; `.env` in `.gitignore` |
| .NET container restart on change | Custom file watcher | `dotnet watch` in dev Dockerfile | Official SDK tool; handles both hot reload and full restart |
| Frontend container restart on change | Custom inotifywait scripts | Vite's built-in HMR with `usePolling: true` | First-class feature; works cross-platform |

**Key insight:** Keycloak 26.x provides a complete first-boot realm provisioning mechanism.
The realm JSON export/import format handles everything: realm config, clients, roles, brute force
settings, and password policy. Do not write shell scripts to call the Admin API on startup.

---

## Keycloak Realm JSON Structure

The complete realm JSON for the `onboarding` realm. This is the structure the import file must follow.

```json
{
  "realm": "onboarding",
  "enabled": true,
  "displayName": "Onboarding",
  "registrationAllowed": false,
  "resetPasswordAllowed": false,
  "rememberMe": false,
  "sslRequired": "external",

  "bruteForceProtected": true,
  "permanentLockout": false,
  "failureFactor": 5,
  "waitIncrementSeconds": 30,
  "maxFailureWaitSeconds": 900,
  "minimumQuickLoginWaitSeconds": 60,
  "quickLoginCheckMilliSeconds": 1000,
  "maxDeltaTimeSeconds": 43200,

  "passwordPolicy": "length(8) and upperCase(1) and lowerCase(1) and digits(1) and specialChars(1)",

  "accessTokenLifespan": 300,
  "ssoSessionMaxLifespan": 28800,
  "ssoSessionIdleTimeout": 1800,

  "clients": [
    {
      "clientId": "onboarding-app",
      "enabled": true,
      "publicClient": true,
      "standardFlowEnabled": false,
      "directAccessGrantsEnabled": true,
      "serviceAccountsEnabled": false,
      "redirectUris": ["http://localhost:5173/*"],
      "webOrigins": ["http://localhost:5173"],
      "protocol": "openid-connect"
    },
    {
      "clientId": "onboarding-api-admin",
      "enabled": true,
      "publicClient": false,
      "standardFlowEnabled": false,
      "directAccessGrantsEnabled": false,
      "serviceAccountsEnabled": true,
      "secret": "${KC_ADMIN_CLIENT_SECRET}",
      "protocol": "openid-connect"
    }
  ],

  "clientScopeMappings": {
    "realm-management": [
      {
        "client": "onboarding-api-admin",
        "roles": ["manage-users", "view-users"]
      }
    ]
  }
}
```

**CRITICAL NOTE on `clientScopeMappings`:** This is how the service account for
`onboarding-api-admin` gets the `manage-users` role from `realm-management`. This is the correct
path in the realm import JSON — NOT `scopeMappings` (which is for realm roles). Confidence is
MEDIUM — this field was found across multiple community sources as the correct approach, but
the exact behavior under Keycloak 26 export format may differ slightly. The safest approach is:
configure via Admin Console first, then export the realm JSON, and use the exported JSON as the
import file. This guarantees the structure is exactly what Keycloak 26 expects.

**Secret templating note:** Keycloak realm import JSON does NOT support environment variable
interpolation like `${KC_ADMIN_CLIENT_SECRET}`. The secret must be either:
1. Hardcoded in the JSON (acceptable for dev, bad for prod)
2. Set as a static value and referenced in `.env` documentation
3. Updated after first boot via Admin API (adds complexity)

**Recommendation for dev:** Use a well-known dev secret (e.g., `dev-admin-secret`) directly in
the realm JSON. Document it clearly as dev-only. In production, realm provisioning would use a
different mechanism (Keycloak Operator, Terraform, or Admin API scripts with secrets from vault).

---

## Common Pitfalls

### Pitfall 1: Wrong Admin Credential Variable Names (Keycloak 26.x breaking change)

**What goes wrong:** Using `KEYCLOAK_ADMIN` and `KEYCLOAK_ADMIN_PASSWORD` (old names) with
Keycloak 26.x. The variables are silently ignored and Keycloak starts without an admin account.
**Why it happens:** Documentation from pre-26 versions and most Google search results still
use the old names.
**How to avoid:** Use `KC_BOOTSTRAP_ADMIN_USERNAME` and `KC_BOOTSTRAP_ADMIN_PASSWORD`.
**Warning signs:** Keycloak starts successfully but admin login fails with "Invalid credentials."

### Pitfall 2: Keycloak Health Check on Port 8080 Instead of 9000

**What goes wrong:** `healthcheck` curl/request targets port 8080, gets no response, service
never becomes healthy, dependent services (api) never start.
**Why it happens:** Port 8080 is the application port — the management port (9000) hosts
`/health/ready`.
**How to avoid:** Health endpoint is `http://localhost:9000/health/ready`. Must set
`KC_HEALTH_ENABLED=true`.
**Warning signs:** `docker compose ps` shows `keycloak` as "starting" indefinitely.

### Pitfall 3: curl Not Present in Keycloak Container

**What goes wrong:** `healthcheck: test: ["CMD", "curl", "-f", "http://localhost:9000/health/ready"]`
fails with "executable file not found."
**Why it happens:** Keycloak 26.x removed curl and most utilities from the image for security.
**How to avoid:** Use the `/dev/tcp` bash TCP socket pattern instead of curl.
**Warning signs:** Health check fails immediately, not after timeout.

### Pitfall 4: Realm Import Skipped Silently

**What goes wrong:** Realm JSON updated but changes not applied after `docker compose up`.
**Why it happens:** Realm already exists from previous boot — import is deliberately skipped.
**How to avoid:** To reset realm config: `docker compose down -v` (removes volumes, destroys
Keycloak's DB, forces fresh import on next up). For prod changes, use Admin API or Keycloak
migration tooling.
**Warning signs:** Configuration changes to realm JSON have no effect.

### Pitfall 5: Docker Startup Race — API Connects Before Keycloak Ready

**What goes wrong:** API container starts, tries to fetch OIDC metadata from Keycloak, gets
connection refused, crashes. `docker compose up` reports success but API is dead.
**Why it happens:** Without `condition: service_healthy` on depends_on, Compose only waits for
the container to start, not for the service inside to be ready.
**How to avoid:** Use `depends_on: keycloak: condition: service_healthy` on the API service.
**Warning signs:** API logs show "Connection refused" to Keycloak on startup, then exit.

### Pitfall 6: Single PostgreSQL Instance for Both App and Keycloak

**What goes wrong:** Schema migrations for app_db conflict with Keycloak's internal schema.
Backup strategies become entangled. A Keycloak upgrade that migrates its schema can interfere
with app migrations.
**Why it happens:** Saves one container, seems simpler.
**How to avoid:** Two containers: `app_db` (port 5432 exposed to host) and `keycloak_db` (no
host port — internal only). Separate volumes: `app_data` and `keycloak_data`.
**Warning signs:** One `postgres` service in compose.yaml accessed by both API and Keycloak.

### Pitfall 7: Vite HMR Not Working in Docker on Windows

**What goes wrong:** Saving a file in the frontend doesn't trigger hot reload. Browser stays on
old version until manual page refresh.
**Why it happens:** Windows inotify filesystem events don't propagate through Docker Desktop
bind mounts to the Linux container.
**How to avoid:** Set `server.watch.usePolling: true` and `server.watch.interval: 1000` in
Vinxi's vite config. This uses polling instead of inotify events.
**Warning signs:** File changes never trigger browser update.

### Pitfall 8: KC_HOSTNAME Mismatch Between Internal and External URL

**What goes wrong:** Tokens issued by Keycloak have `iss` claim = `http://keycloak:8080/realms/onboarding`
(internal Docker hostname). The .NET API's JwtBearer middleware validates `iss` against the
configured `Authority`. Mismatch causes 401 for every request.
**Why it happens:** Keycloak sets the `iss` from its hostname configuration. The .NET API
must validate against the exact issuer in the token.
**How to avoid:** Either: (a) set `KC_HOSTNAME=http://localhost:8180` and override ValidIssuer
in JwtBearerOptions to match; or (b) set `KC_HOSTNAME=http://localhost:8180` so tokens carry
the public hostname, and configure Authority to match. Document the chosen approach explicitly.
**Warning signs:** JwtBearer logs "Issuer validation failed" or "IDX10205" error codes.

---

## Code Examples

### Keycloak Healthcheck (verified TCP socket pattern)
```yaml
# Source: https://www.keycloak.org/observability/health (official docs)
healthcheck:
  test: ["CMD-SHELL", "exec 3<>/dev/tcp/127.0.0.1/9000 && echo -e 'GET /health/ready HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3 && cat <&3 | grep -q '200 OK'"]
  interval: 15s
  timeout: 10s
  retries: 10
  start_period: 60s
```

### PostgreSQL Healthcheck
```yaml
# Source: Standard Docker pattern verified against postgres:16-alpine
healthcheck:
  test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
  interval: 10s
  timeout: 5s
  retries: 5
  start_period: 30s
```

### .NET 10 Dev Dockerfile (dotnet watch)
```dockerfile
# Source: mcr.microsoft.com/dotnet/sdk:10.0 official image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
WORKDIR /app
COPY *.sln .
COPY src/**/*.csproj ./
RUN find . -name "*.csproj" -exec sh -c 'mkdir -p $(dirname "$1") && mv "$1" "$1"' _ {} \;
RUN dotnet restore

COPY src/ ./src/
WORKDIR /app/src/Onboarding.API
EXPOSE 8080
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile", "--urls", "http://0.0.0.0:8080"]
```

Note: The `COPY src/**/*.csproj` layer-cache trick requires a Docker BuildKit feature.
Alternatively, explicitly copy each `.csproj` file individually for guaranteed caching behavior.

### Vinxi SPA — Docker-compatible HMR config
```typescript
// Source: Vite docs + Vinxi 0.5.x (verified pattern for Docker on Windows)
// app.config.ts
import { defineConfig } from "vinxi";

export default defineConfig({
  routers: [
    {
      name: "client",
      type: "spa",
      handler: "./src/client.tsx",
      vite: {
        server: {
          host: "0.0.0.0",
          port: 5173,
          hmr: { host: "localhost", port: 5173, clientPort: 5173 },
          watch: { usePolling: true, interval: 1000 },
        },
      },
    },
  ],
});
```

### Keycloak 26.x Key Environment Variables
```yaml
# Source: https://www.keycloak.org/server/containers (official)
environment:
  KC_BOOTSTRAP_ADMIN_USERNAME: admin          # Replaces KEYCLOAK_ADMIN (deprecated)
  KC_BOOTSTRAP_ADMIN_PASSWORD: changeme       # Replaces KEYCLOAK_ADMIN_PASSWORD (deprecated)
  KC_DB: postgres
  KC_DB_URL: jdbc:postgresql://keycloak_db:5432/keycloak
  KC_DB_USERNAME: kcuser
  KC_DB_PASSWORD: ${KC_DB_PASSWORD}
  KC_HEALTH_ENABLED: "true"                   # Exposes /health/ready on port 9000
  KC_HTTP_ENABLED: "true"                     # Allows HTTP (implied by start-dev)
  KC_HOSTNAME_STRICT: "false"                 # Allows any hostname (implied by start-dev)
  KC_HOSTNAME: http://localhost:8180          # Sets iss claim in tokens
```

### Minimal .env.example
```bash
# .env.example — commit this; copy to .env and fill in values
APP_DB_PASSWORD=change_me_app
KC_DB_PASSWORD=change_me_kc
KC_ADMIN_PASSWORD=change_me_admin
KC_ADMIN_CLIENT_SECRET=dev-admin-secret   # Used in realm JSON and API config
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `KEYCLOAK_ADMIN` env var | `KC_BOOTSTRAP_ADMIN_USERNAME` | Keycloak 22+ / stable in 26 | Old vars ignored silently |
| Health check on port 8080 | Health check on port 9000 (management port) | Keycloak 20+ | All healthcheck commands must change |
| `curl` in healthcheck | `/dev/tcp` bash socket | Keycloak 22+ (curl removed) | Requires different healthcheck syntax |
| `KC_PROXY=edge` | `KC_PROXY_HEADERS=xforwarded` + `KC_HTTP_ENABLED=true` | Keycloak 22+ | Old KC_PROXY variable deprecated |
| `docker-compose` (v1) | `docker compose` (V2 plugin) | Docker 20.10+ | V1 is end-of-life |

**Deprecated in Keycloak 26:**
- `KEYCLOAK_ADMIN` / `KEYCLOAK_ADMIN_PASSWORD` — replaced by `KC_BOOTSTRAP_*` variants
- `KC_PROXY=edge|reencrypt|passthrough` — replaced by `KC_PROXY_HEADERS` + explicit `KC_HTTP_ENABLED`

---

## Open Questions

1. **Keycloak `clientScopeMappings` vs `scopeMappings` for service account role in realm import**
   - What we know: `clientScopeMappings` with `realm-management` key assigns client roles to the service account; structure confirmed across community sources
   - What's unclear: Whether Keycloak 26.x export uses a different key name or format; no official doc page confirms the exact JSON field for service account role bindings in realm import
   - Recommendation: Configure in Admin Console first, export realm JSON, use the export as the authoritative import file. This eliminates guessing the JSON schema.

2. **KC_HOSTNAME issuer alignment with .NET JwtBearer Authority**
   - What we know: Keycloak's `KC_HOSTNAME` controls the `iss` claim; JwtBearer validates `iss`
   - What's unclear: Exact `ValidIssuer` vs `Authority` behavior when they differ in .NET 10
   - Recommendation: Set `KC_HOSTNAME=http://localhost:8180`, configure both Authority (for metadata fetch using internal Docker network: `http://keycloak:8080/realms/onboarding`) and ValidIssuer (public hostname: `http://localhost:8180/realms/onboarding`) explicitly in JwtBearerOptions.

3. **dotnet watch COPY layer cache with multi-project solution**
   - What we know: The `COPY *.csproj` pattern works for single projects; multi-project solutions require individual COPY lines
   - What's unclear: Whether glob patterns work reliably for nested project files in BuildKit
   - Recommendation: Use explicit `COPY src/Onboarding.X/Onboarding.X.csproj` lines to guarantee layer caching correctness.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|-------------|-----------|---------|----------|
| Docker | All containers | Yes | 29.2.1 | — |
| Docker Compose V2 | Orchestration | Yes | v5.0.2 | — |
| .NET 10 SDK | API dev | Yes | 10.0.201 | — |
| Node.js | Frontend dev | Yes | 24.14.0 | — |
| npm | Frontend packages | Yes | bundled with Node 24 | — |

**No missing dependencies.** All required tools are present on this machine.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.x (to be installed in Phase 3 — not needed for infra-only phase) |
| Config file | None yet (Wave 0 gap) |
| Quick run command | `docker compose ps` (infra smoke test) |
| Full suite command | `docker compose up --wait && docker compose ps` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INFRA-01 | `docker compose up` starts all 5 services | Smoke (manual) | `docker compose up -d --wait && docker compose ps` | ❌ Wave 0 |
| INFRA-02 | app_db is a separate container with its own volume | Smoke (manual) | `docker inspect keycloak-tests_app_db_1` or `docker compose ps app_db` | ❌ Wave 0 |
| INFRA-03 | keycloak_db is separate, no shared volumes with app_db | Smoke (manual) | `docker volume ls` — verify two separate volumes | ❌ Wave 0 |
| INFRA-04 | All healthchecks pass; dependents wait | Smoke (manual) | `docker compose up --wait` exits 0 and all services show `healthy` | ❌ Wave 0 |
| INFRA-05 | Realm "onboarding" exists with expected clients | Smoke (manual) | `curl http://localhost:8180/realms/onboarding` returns 200 with realm data | ❌ Wave 0 |

**Note:** Phase 1 is pure infrastructure. Formal xUnit tests are not applicable here. All
validation is smoke-tested via Docker Compose commands and Keycloak Admin API calls.

### Sampling Rate
- **Per task commit:** `docker compose config --quiet` (validates compose file syntax)
- **Per wave merge:** `docker compose up -d --wait && docker compose ps`
- **Phase gate:** All 5 services healthy, realm API returns 200, both PostgreSQL instances accept connections

### Wave 0 Gaps
- [ ] `docker compose up -d --wait` smoke test script (optional helper script)
- [ ] No xUnit framework needed for Phase 1 — infrastructure validation is manual/CLI

---

## Sources

### Primary (HIGH confidence)
- [Keycloak Import/Export Guide](https://www.keycloak.org/server/importExport) — realm import mechanism, `--import-realm` flag, skip behavior
- [Keycloak Health Checks](https://www.keycloak.org/observability/health) — port 9000, `KC_HEALTH_ENABLED`, TCP socket healthcheck pattern
- [Keycloak Running in Container](https://www.keycloak.org/server/containers) — `KC_BOOTSTRAP_ADMIN_USERNAME/PASSWORD`, start-dev command
- [.NET 10 Docker Images](https://mcr.microsoft.com/en-us/product/dotnet/sdk/about) — `mcr.microsoft.com/dotnet/sdk:10.0` official image
- [Keycloak Hostname Configuration](https://www.keycloak.org/server/hostname) — KC_HOSTNAME, KC_HOSTNAME_STRICT behavior

### Secondary (MEDIUM confidence)
- [Vite HMR in Docker Discussion](https://github.com/vitejs/vite/discussions/14007) — usePolling, HMR port config patterns verified across multiple sources
- [Keycloak Docker Compose 2025 Guide — Mastertheboss](https://www.mastertheboss.com/keycloak/keycloak-with-docker/) — practical compose examples, KC_BOOTSTRAP vars
- Keycloak realm JSON brute force fields — `bruteForceProtected`, `failureFactor`, `waitIncrementSeconds` — verified via multiple GitHub realm export examples
- Password policy string format — `"length(8) and upperCase(1) and lowerCase(1) and digits(1) and specialChars(1)"` — verified via Keycloak docs and community sources

### Tertiary (LOW confidence — flag for validation)
- `clientScopeMappings` JSON field for service account role assignment — found in community sources but no official doc page confirmed the exact Keycloak 26 JSON schema; recommend export-then-import approach

---

## Metadata

**Confidence breakdown:**
- Docker Compose topology: HIGH — standard patterns, official docs
- Keycloak 26.x startup flags: HIGH — official container docs confirmed
- Keycloak healthcheck (port 9000, TCP socket): HIGH — official health docs confirmed
- Realm import mechanism: HIGH — official import/export docs confirmed
- Realm JSON brute force/password policy structure: MEDIUM — confirmed via multiple exports, not official schema doc
- Service account role assignment in realm JSON: LOW — community-sourced; export-first approach mitigates risk
- Vite HMR in Docker (Windows polling): MEDIUM — confirmed via Vite issue tracker, no official Vinxi-specific doc

**Research date:** 2026-04-01
**Valid until:** 2026-07-01 (Keycloak 26.x stable; .NET 10 LTS; check before Keycloak 27 release)
