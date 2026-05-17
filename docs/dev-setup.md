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
