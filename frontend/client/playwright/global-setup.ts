import { execSync } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

/**
 * Global setup for client SPA Playwright E2E suite.
 *
 * Runs before any test file. Performs two pre-flight checks:
 * 1. Verifies that `docker compose ps` shows the stack is healthy.
 *    If the required services are not running, fails with a clear message.
 * 2. Invokes `bash scripts/seed-test-users.sh` (idempotent) to ensure
 *    e2e test users exist in Keycloak before tests run.
 *
 * On Windows (developer machine): the script is invoked via bash (Git Bash /
 * WSL). In CI (Linux), bash is available directly.
 */
export default async function globalSetup(): Promise<void> {
  // ── Step 1: verify docker compose stack health ───────────────────────────
  const requiredServices = ['keycloak', 'api', 'frontend-client'];

  let composeOutput = '';
  try {
    composeOutput = execSync('docker compose ps --format json', {
      cwd: path.resolve(__dirname, '../../..'),
      encoding: 'utf-8',
      stdio: ['pipe', 'pipe', 'pipe'],
    });
  } catch {
    throw new Error(
      '[E2E globalSetup] docker compose ps failed. ' +
        'Ensure Docker is running and `docker compose up -d` has been executed from the repo root. ' +
        'Required services: ' + requiredServices.join(', ')
    );
  }

  // Parse JSONL output (one JSON object per line, or array)
  let services: Array<{ Name?: string; Service?: string; State?: string; Status?: string }> = [];
  try {
    const trimmed = composeOutput.trim();
    if (trimmed.startsWith('[')) {
      services = JSON.parse(trimmed);
    } else {
      services = trimmed
        .split('\n')
        .filter(Boolean)
        .map((line) => JSON.parse(line));
    }
  } catch {
    services = [];
  }

  const unhealthy: string[] = [];
  for (const svc of requiredServices) {
    const entry = services.find(
      (s) => (s.Service === svc || s.Name?.includes(svc))
    );
    if (!entry) {
      unhealthy.push(`${svc} (not found in compose ps output)`);
      continue;
    }
    const state = (entry.State ?? entry.Status ?? '').toLowerCase();
    if (!state.includes('running') && !state.includes('healthy')) {
      unhealthy.push(`${svc} (state: ${entry.State ?? entry.Status})`);
    }
  }

  if (unhealthy.length > 0) {
    throw new Error(
      '[E2E globalSetup] The following services are not running:\n  ' +
        unhealthy.join('\n  ') +
        '\n\nRun `docker compose up -d` from the repo root and wait for all services to become healthy, then retry.'
    );
  }

  // ── Step 2: seed test users (idempotent) ─────────────────────────────────
  // Invokes scripts/seed-test-users.sh from the repo root.
  // The script uses client_credentials to upsert:
  //   e2e-client@example.com / E2EClient@123! → client realm, group=admin-empresa
  // On Windows (developer machine): Git Bash provides bash on PATH.
  // On Linux CI: bash is available natively.
  const repoRoot = path.resolve(__dirname, '../../..');
  const seedScript = path.join(repoRoot, 'scripts', 'seed-test-users.sh');

  try {
    const output = execSync(`bash "${seedScript}"`, {
      cwd: repoRoot,
      encoding: 'utf-8',
      stdio: ['pipe', 'pipe', 'pipe'],
      env: {
        ...process.env,
        KC_ADMIN_CLIENT_SECRET: process.env.KC_ADMIN_CLIENT_SECRET ?? 'dev-admin-secret',
        KEYCLOAK_URL: process.env.KEYCLOAK_URL ?? 'http://localhost:8180',
      },
    });
    process.stdout.write('[E2E globalSetup] seed-test-users.sh output:\n' + output + '\n');
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    throw new Error(
      '[E2E globalSetup] seed-test-users.sh failed:\n' + message +
        '\n\nCheck that Keycloak is healthy and KC_ADMIN_CLIENT_SECRET matches the compose.yaml value (default: dev-admin-secret).'
    );
  }
}
