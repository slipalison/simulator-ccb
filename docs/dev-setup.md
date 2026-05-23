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

## Playwright and IPv4 (D-17)

All Playwright configs in this project use `baseURL: 'http://127.0.0.1:PORT'` — never `localhost`.
This pins requests to the IPv4 docker port mapping and prevents false negatives from a stale host
process.
