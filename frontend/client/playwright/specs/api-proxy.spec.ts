/**
 * T-9b: Client SPA api-proxy regression spec (Phase 49 iter 2 — auth-flow-fix)
 *
 * Covers three scenarios that guard against Bug 3 (api-proxy 503 TypeError: fetch failed)
 * documented in .jdi/phases/auth-flow-fix/INVESTIGATION-api-proxy.md.
 *
 * Root cause: a stale vinxi-host process on the Windows host intercepts `localhost:5173`
 * via IPv6 ([::]:5173), cannot reach the Docker bridge `onboarding-net`, and returns a
 * 503 HTML error page instead of proxying to the .NET backend. These tests use
 * `http://127.0.0.1:5173` (D-17) to ensure IPv4 routing to the Docker port mapping.
 *
 * All three scenarios use Playwright's `request` fixture (APIRequestContext) so they
 * exercise the Vinxi proxy layer without requiring a browser page load.
 *
 * Proxy route in app.config.ts: base="/api", handler="./server.ts"
 * Backend reachable inside Docker as: http://api:8080/api/*
 *
 * Scenario 1 — Single listener guard:
 *   Shells out to detect listeners on port 5173. Asserts exactly ONE process listens.
 *   If more than one is detected, fails with PID info + cleanup instructions.
 *   Skip gracefully on platforms where the probe command is unavailable.
 *
 * Scenario 2 — Proxy reaches backend on POST:
 *   POST /api/companies/registration with empty body → expects 422 JSON
 *   (application/problem+json or application/json). Never 503 HTML.
 *   If the response is HTML, the test fails with a message pointing at Bug 3.
 *
 * Scenario 3 — Proxy reaches healthz on GET:
 *   GET /api/healthz/live → expects 200 with body "Healthy".
 *   Proxies to http://api:8080/healthz/live (AllowAnonymous in Program.cs).
 */

import { test, expect } from '@playwright/test';
import { execSync } from 'child_process';

const SPA_PORT = 5173;
const BASE_URL = `http://127.0.0.1:${SPA_PORT}`;

// ── Helpers ───────────────────────────────────────────────────────────────────

/**
 * Probe TCP listeners on the given port using platform-appropriate CLI tools.
 * Returns an array of `{ pid: number }` for each listening process found.
 * Returns null if the probe command is unavailable on this platform.
 */
function probeListeners(port: number): Array<{ pid: number }> | null {
  if (process.platform === 'win32') {
    // PowerShell: Get-NetTCPConnection — available on Windows 8+ / Server 2012+
    try {
      const raw = execSync(
        `powershell -NoProfile -Command "Get-NetTCPConnection -LocalPort ${port} -State Listen | Select-Object -ExpandProperty OwningProcess"`,
        { encoding: 'utf-8', stdio: ['pipe', 'pipe', 'pipe'], timeout: 10000 }
      );
      const pids = raw
        .trim()
        .split(/\r?\n/)
        .map((l) => l.trim())
        .filter(Boolean)
        .map(Number)
        .filter((n) => !isNaN(n));
      return pids.map((pid) => ({ pid }));
    } catch {
      // PowerShell not available or command failed — skip gracefully
      return null;
    }
  } else {
    // Linux / macOS: try `ss -tlpn` first, fall back to `netstat`
    try {
      const raw = execSync(`ss -tlpn "sport = :${port}"`, {
        encoding: 'utf-8',
        stdio: ['pipe', 'pipe', 'pipe'],
        timeout: 10000,
      });
      // ss output lines: "LISTEN 0 128 0.0.0.0:5173 ... users:(("node",pid=123,fd=4))"
      const pids: number[] = [];
      for (const line of raw.split('\n')) {
        const match = line.match(/pid=(\d+)/);
        if (match) pids.push(Number(match[1]));
      }
      // Deduplicate — ss may list multiple sockets for the same pid
      const unique = [...new Set(pids)];
      return unique.map((pid) => ({ pid }));
    } catch {
      // ss not available — not a hard failure on macOS
      try {
        const raw = execSync(`netstat -tlpn 2>/dev/null | awk '$4 ~ /:${port}$/'`, {
          encoding: 'utf-8',
          shell: '/bin/bash',
          stdio: ['pipe', 'pipe', 'pipe'],
          timeout: 10000,
        });
        const pids: number[] = [];
        for (const line of raw.split('\n')) {
          // PID/Program column: "12345/node"
          const match = line.match(/(\d+)\/\w+/);
          if (match) pids.push(Number(match[1]));
        }
        const unique = [...new Set(pids)];
        return unique.map((pid) => ({ pid }));
      } catch {
        return null;
      }
    }
  }
}

// ── Scenario 1: Single listener guard ────────────────────────────────────────

test.describe('api-proxy — client SPA (port 5173)', () => {
  test('Scenario 1 — single listener guard: exactly one process listens on port 5173', () => {
    const listeners = probeListeners(SPA_PORT);

    if (listeners === null) {
      // Probe unavailable on this platform — skip instead of failing
      test.skip(true, 'Listener probe not available on this platform — skipping single-listener guard');
      return;
    }

    if (listeners.length > 1) {
      const pidList = listeners.map((l) => l.pid).join(', ');
      throw new Error(
        `[Bug 3 guard] More than one process is listening on port ${SPA_PORT}. ` +
          `PIDs detected: ${pidList}. ` +
          `A stale vinxi-host process is likely intercepting requests via IPv6 ([::]:${SPA_PORT}). ` +
          `Stop stale processes on Windows with:\n` +
          `  Stop-Process -Id ${pidList} -Force\n` +
          `On Linux/macOS:\n` +
          `  pkill -f "vinxi dev.*--port ${SPA_PORT}"\n` +
          `See: .jdi/phases/auth-flow-fix/INVESTIGATION-api-proxy.md`
      );
    }

    // Exactly one listener (or zero on CI where no process is bound yet — acceptably skip)
    if (listeners.length === 0) {
      test.skip(true, `No listener detected on port ${SPA_PORT} — Docker compose may not be up yet`);
      return;
    }

    expect(listeners).toHaveLength(1);
  });

  // ── Scenario 2: Proxy reaches backend on POST ───────────────────────────────

  test('Scenario 2 — proxy reaches backend on POST: /api/companies/registration returns 422 JSON (not 503 HTML)', async ({
    request,
  }) => {
    // POST with empty body — backend FluentValidation rejects → 422 Unprocessable Entity
    // The proxy (server.ts) forwards the request to http://api:8080/api/companies/registration
    // If this returns 503 HTML it means Bug 3 is active (vinxi-host stale intercepting IPv6)
    const response = await request.post(`${BASE_URL}/api/companies/registration`, {
      data: {},
      failOnStatusCode: false,
    });

    const status = response.status();
    const contentType = response.headers()['content-type'] ?? '';
    const bodyText = await response.text();

    // Hard-fail with diagnostic if we receive a 503 HTML error page
    if (status === 503 || bodyText.includes('<!DOCTYPE html>') || bodyText.includes('TypeError: fetch failed')) {
      throw new Error(
        `[Bug 3 regression] POST /api/companies/registration returned a 503 HTML error page.\n` +
          `Status: ${status}\n` +
          `Content-Type: ${contentType}\n` +
          `Body (first 512 chars): ${bodyText.slice(0, 512)}\n\n` +
          `Root cause: a stale vinxi-host process is likely intercepting requests on localhost:${SPA_PORT} ` +
          `via IPv6. Run Scenario 1 (single listener guard) to confirm.\n` +
          `See: .jdi/phases/auth-flow-fix/INVESTIGATION-api-proxy.md`
      );
    }

    // Assert the proxy reached the backend and got a validation error (not a proxy error)
    expect(status).toBe(422);

    // Assert the response is JSON (application/problem+json or application/json), not HTML
    expect(contentType).toMatch(/application\/(problem\+)?json/);

    // Assert the body is parseable JSON
    let parsed: unknown;
    try {
      parsed = JSON.parse(bodyText);
    } catch {
      throw new Error(
        `[Bug 3 regression] Response body is not valid JSON.\n` +
          `Content-Type: ${contentType}\n` +
          `Body (first 512 chars): ${bodyText.slice(0, 512)}`
      );
    }

    // Basic shape assertion — backend returns RFC 7807 problem details on validation errors
    expect(parsed).toMatchObject({ title: expect.any(String) });
  });

  // ── Scenario 3: Proxy reaches healthz on GET ────────────────────────────────

  test('Scenario 3 — proxy reaches healthz on GET: /api/healthz/live returns 200 Healthy', async ({
    request,
  }) => {
    // GET /api/healthz/live → proxied to http://api:8080/healthz/live
    // The endpoint is AllowAnonymous in Program.cs and returns "Healthy" as plain text.
    // If this returns 503 HTML it means Bug 3 is active.
    const response = await request.get(`${BASE_URL}/api/healthz/live`, {
      failOnStatusCode: false,
    });

    const status = response.status();
    const bodyText = await response.text();

    if (status === 503 || bodyText.includes('<!DOCTYPE html>') || bodyText.includes('TypeError: fetch failed')) {
      throw new Error(
        `[Bug 3 regression] GET /api/healthz/live returned a 503 HTML error page.\n` +
          `Status: ${status}\n` +
          `Body (first 512 chars): ${bodyText.slice(0, 512)}\n\n` +
          `Root cause: a stale vinxi-host process is likely intercepting requests on localhost:${SPA_PORT} ` +
          `via IPv6. Run Scenario 1 (single listener guard) to confirm.\n` +
          `See: .jdi/phases/auth-flow-fix/INVESTIGATION-api-proxy.md`
      );
    }

    expect(status).toBe(200);
    expect(bodyText.trim()).toBe('Healthy');
  });
});
