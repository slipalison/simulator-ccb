# Local Development Setup

Decisions in scope: **D-16** (compose-only dev), **D-17** (Playwright pins 127.0.0.1).

## Official workflow

```bash
# 1. Copy env template and fill secrets
cp .env.example .env

# 2. Start the full stack (frontend SPAs included, with hot reload via bind mounts)
docker compose up -d

# 3. Verify
curl http://127.0.0.1:5173/api/healthz/live
# Expected: Healthy

# Stream frontend logs
docker compose logs -f frontend-client
```

## Seeding e2e test users (required for Keycloak → DB sync)

After `docker compose up -d`, run the seed script to create the two e2e test users in Keycloak
**and** sync their Keycloak `id` (sub) into the `companies` table. The sync is required because
`ClientClaimsMiddleware` resolves `JWT sub → companies.keycloak_user_id` to determine permissions.
Without it the user hits the no-match path and receives 0 permissions.

```bash
KC_ADMIN_CLIENT_SECRET=dev-admin-secret bash scripts/seed-test-users.sh
```

The script is **idempotent** — safe to re-run after `docker compose down -v && docker compose up -d`.

### What the seed script does

1. Acquires a `client_credentials` token from each Keycloak realm.
2. Upserts `e2e-client@example.com` in the `client` realm (group: `admin-empresa`).
3. Upserts `e2e-admin@example.com` in the `backoffice` realm (role: `admin`).
4. **Syncs the Keycloak `id` (sub) for `e2e-client@example.com` into `companies.keycloak_user_id`**
   via `docker compose exec db psql ...`. The DB container name defaults to `db`; override with
   `DB_CONTAINER=<name>` if your compose service has a different name.

### Manual sync (if DB container is unavailable)

If the DB container is not running when you seed, run the SQL manually:

```bash
# Get the Keycloak sub first:
TOKEN=$(curl -s -X POST http://localhost:8180/realms/client/protocol/openid-connect/token \
  -d grant_type=client_credentials -d client_id=onboarding-api-admin \
  -d client_secret=dev-admin-secret | jq -r .access_token)

SUB=$(curl -s "http://localhost:8180/admin/realms/client/users?email=e2e-client@example.com&exact=true" \
  -H "Authorization: Bearer $TOKEN" | jq -r '.[0].id')

# Then update the DB:
docker compose exec db psql -U postgres -d onboarding \
  -c "UPDATE companies SET keycloak_user_id = '$SUB' WHERE email = 'e2e-client@example.com'"
```

## After pnpm changes — rebuild container

When `package.json` changes (new deps, version bumps, removed packages), the running container
still uses the **stale `node_modules`** baked into the previous image. The change is NOT picked
up automatically by a bind-mount restart.

Rebuild the affected container to apply the new dependency graph:

```bash
# Rebuild only the affected container (faster than full compose rebuild)
docker compose build frontend-client    # or frontend-backoffice or api

# Bring it back up
docker compose up -d
```

Scope guide:
- Changed `frontend/client/package.json`       → `docker compose build frontend-client`
- Changed `frontend/backoffice/package.json`    → `docker compose build frontend-backoffice`
- Changed `Onboarding.Api/Onboarding.Api.csproj` → `docker compose build api`
- Changed root `compose.yaml` base image        → `docker compose build <service>`

`docker compose restart <service>` is **NOT sufficient** — it only restarts the existing container
from the existing image without rebaking `node_modules`.

## Why `pnpm dev` on the host is PROHIBITED (D-16)

Running `pnpm dev` in `frontend/client/` or `frontend/backoffice/` on a Windows host creates a
Vinxi process that binds `[::]:5173` (IPv6) and `0.0.0.0:5173`. Docker compose also maps
`127.0.0.1:5173`. Two listeners share the same port.

Windows resolves `localhost` to `::1` first, so browser requests hit the HOST process, which cannot
resolve the Docker hostname `api` (ENOTFOUND). Vinxi returns 503 `TypeError: fetch failed`.

Full root cause: `.jdi/phases/auth-flow-fix/INVESTIGATION-api-proxy.md`.

## Detect a stale host Vinxi process

**Windows (PowerShell):**

```powershell
Get-NetTCPConnection -LocalPort 5173,5174 -State Listen |
  Select-Object LocalAddress, LocalPort, OwningProcess,
    @{N='Cmd';E={(Get-CimInstance Win32_Process -Filter "ProcessId=$($_.OwningProcess)").CommandLine}}
```

Expected: exactly ONE listener per port (`com.docker.backend` on `127.0.0.1`). A second entry on
`0.0.0.0` or `[::]` with a `node.exe` vinxi commandline is the stale process.

**Linux / macOS:**

```bash
ss -tlpn | grep -E ':(5173|5174)\b'
```

## Clean a stale host process

**Windows:** `Stop-Process -Id <pid> -Force`

**Linux / macOS:** `pkill -f 'vinxi dev'`

Confirm with `curl http://127.0.0.1:5173/api/healthz/live` → `Healthy`.

Note: `docker compose restart frontend-client` does NOT kill a stale host process.

## Automatic guard (predev hook)

Both `frontend/client/package.json` and `frontend/backoffice/package.json` have a `predev` script
that calls `scripts/check-dev-env.mjs`. npm/pnpm auto-run `predev` before `dev`. If the compose
service is running, the guard exits 1 with an actionable message and the dev server is blocked.

## Escape hatch: ALLOW_HOST_DEV (D-16)

Only for advanced debugging (e.g., attaching a local Node debugger):

```bash
# Windows PowerShell
$env:ALLOW_HOST_DEV = "1"; pnpm dev

# Linux / macOS
ALLOW_HOST_DEV=1 pnpm dev
```

Accepted values: `1`, `true`, `yes`. You are responsible for ensuring no port conflict exists.

## OTel telemetry — collector + Jaeger UI (D-36, Phase 53 T-5)

The stack includes a first-party OTel Collector Contrib that applies PII scrub processors
before any telemetry reaches backend storage (Alloy → Loki / Tempo / Mimir / Grafana).

### Architecture

```
Browser SPA (port 4318 HTTP)  ──┐
                                 ├──► otel-collector (PII scrub) ──► Alloy ──► Tempo / Loki / Mimir
API container (port 4318 HTTP) ──┘                              └──► Jaeger UI (port 16686)
```

PII-dropped attribute keys: `email`, `cpf`, `cnpj`, `sub`, `refresh_token`, `access_token`,
`authorization`, `set-cookie`, `http.request.header.authorization`,
`http.response.header.set-cookie`, and any key matching `*token*`, `*secret*`, `*password*`,
`*credential*`.

PII-redacted values (attributes that pass key filter): email addresses, Brazilian CPF/CNPJ
patterns, and Bearer token strings are replaced with `****`.

### Starting the collector

```bash
# Start only the collector (and its dependency chain):
docker compose up -d otel-collector

# Verify it is ready — look for "Everything is ready. Begin running and processing data."
docker compose logs otel-collector

# Health check (HTTP):
curl http://127.0.0.1:13133/
```

### Starting Jaeger dev UI

```bash
docker compose up -d jaeger otel-collector

# Open Jaeger UI in browser:
# http://localhost:16686
```

Jaeger only receives spans that have already been PII-scrubbed by `otel-collector`.

### Testing PII scrub manually

Send a test span with an `email` attribute via OTLP HTTP and verify the collector drops it:

```bash
curl -s -X POST http://127.0.0.1:4318/v1/traces \
  -H "Content-Type: application/json" \
  -d '{
    "resourceSpans": [{
      "resource": {"attributes": [{"key": "service.name", "value": {"stringValue": "pii-test"}}]},
      "scopeSpans": [{
        "spans": [{
          "traceId": "00000000000000000000000000000001",
          "spanId": "0000000000000001",
          "name": "pii-scrub-test",
          "kind": 1,
          "startTimeUnixNano": "1700000000000000000",
          "endTimeUnixNano":   "1700000001000000000",
          "attributes": [
            {"key": "email", "value": {"stringValue": "alison@x.com"}},
            {"key": "http.method", "value": {"stringValue": "GET"}}
          ]
        }]
      }]
    }]
  }'

# Inspect collector logs — "email" attribute must NOT appear:
docker compose logs otel-collector | grep -i "pii-scrub-test"
# Expected: span logged with http.method=GET; no email field present.
```

### OTLP endpoint env var

The API reads `OTEL_EXPORTER_OTLP_ENDPOINT` (defaults to `http://otel-collector:4318` in compose).
Override for CI or production:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://my-collector:4318 \
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf \
  dotnet run
```

## Playwright and IPv4 (D-17)

All Playwright configs in this project use `baseURL: 'http://127.0.0.1:PORT'` — never `localhost`.
This pins requests to the IPv4 docker port mapping and prevents false negatives from a stale host
process.
