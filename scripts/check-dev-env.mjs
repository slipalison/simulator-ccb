/**
 * check-dev-env.mjs
 *
 * Pre-dev guard: aborts `pnpm dev` on the host when the corresponding
 * docker compose service is already running.
 *
 * WHY: Running `vinxi dev` on the Windows host while the same SPA runs
 * inside docker compose creates two listeners on the same port. The host
 * process binds [::]:PORT (IPv6) and intercepts requests resolved via
 * `localhost` (which Windows routes to ::1 first). That host process
 * cannot reach the Docker-internal `api` hostname, so every `/api/*`
 * proxy call fails with TypeError: fetch failed → 503. See D-16 and
 * docs/dev-setup.md for the full explanation.
 *
 * Usage:
 *   node scripts/check-dev-env.mjs <service-name> [<service-name>...]
 *
 * Bypass (advanced debugging only):
 *   ALLOW_HOST_DEV=1 pnpm dev
 *
 * @param {Function} [execSyncImpl] - optional injection for testing
 */

import { execSync } from "node:child_process";
import { existsSync } from "node:fs";
import process from "node:process";

const TRUTHY = new Set(["1", "true", "yes"]);

export function run(serviceNames, execSyncImpl = execSync, existsSyncImpl = existsSync) {
  // Container short-circuit: predev fires inside the container when Dockerfile
  // CMD runs `pnpm dev`. The guard is host-only — D-16 specifies the compose
  // workflow IS the canonical runtime — so skip silently inside containers.
  // `/.dockerenv` is the standard Docker marker.
  if (existsSyncImpl("/.dockerenv")) {
    return 0;
  }

  // Bypass escape hatch (documented in docs/dev-setup.md, D-16)
  if (TRUTHY.has((process.env.ALLOW_HOST_DEV ?? "").toLowerCase())) {
    console.log(
      "[check-dev-env] ALLOW_HOST_DEV is set — skipping compose guard. " +
        "Ensure no host vinxi process conflicts with the container (see docs/dev-setup.md)."
    );
    return 0;
  }

  let runningServices;
  try {
    runningServices = execSyncImpl("docker compose ps --status running --services", {
      encoding: "utf-8",
    });
  } catch {
    // Docker not installed or daemon not running — do not block greenfield envs.
    console.warn(
      "[check-dev-env] WARNING: `docker compose ps` failed (Docker not running?). " +
        "Skipping compose guard."
    );
    return 0;
  }

  const running = new Set(
    runningServices
      .split("\n")
      .map((s) => s.trim())
      .filter(Boolean)
  );

  const conflicts = serviceNames.filter((name) => running.has(name));

  if (conflicts.length > 0) {
    console.error(
      "\n[check-dev-env] ABORT: The following compose service(s) are already running:\n" +
        conflicts.map((n) => `  • ${n}`).join("\n") +
        "\n\n" +
        "WHY THIS IS BLOCKED (D-16):\n" +
        "  Running `pnpm dev` on the host creates a vinxi process that binds [::]:PORT\n" +
        "  (IPv6). When your browser resolves `localhost`, Windows routes it to ::1 first,\n" +
        "  hitting the HOST process — which cannot reach the Docker bridge network.\n" +
        "  Every /api/* call fails with TypeError: fetch failed → 503.\n\n" +
        "HOW TO USE THE RUNNING CONTAINER (hot reload via bind mounts):\n" +
        `  docker compose logs -f ${conflicts[0]}\n\n` +
        "HOW TO CLEAN STALE HOST PROCESSES:\n" +
        "  Windows: Get-NetTCPConnection -LocalPort 5173,5174 -State Listen\n" +
        "           Stop-Process -Id <pid> -Force\n" +
        "  Linux:   ss -tlpn | grep -E ':(5173|5174)'\n" +
        "           pkill -f 'vinxi dev'\n\n" +
        "ESCAPE HATCH (advanced debugging only):\n" +
        "  ALLOW_HOST_DEV=1 pnpm dev\n\n" +
        "See docs/dev-setup.md for full details.\n"
    );
    return 1;
  }

  return 0;
}

// Direct invocation guard — only run when this file is the entry point, not when imported.
// Vitest sets process.argv[1] to its own runner path, so the filename will not match.
const argv1 = process.argv[1] ?? "";
const isMain = argv1.endsWith("check-dev-env.mjs");

if (isMain) {
  const services = process.argv.slice(2);
  if (services.length === 0) {
    console.error("[check-dev-env] Usage: node scripts/check-dev-env.mjs <service-name> [...]");
    process.exit(1);
  }
  process.exit(run(services));
}
