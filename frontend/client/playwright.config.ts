import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E configuration for client frontend.
 *
 * - Sequential execution (fullyParallel: false, workers: 1) due to shared Keycloak state
 * - No webServer — Docker Compose must be running before E2E tests
 * - 60s timeout per test for ACF redirect delays
 * - Setup projects save storageState for authenticated test reuse
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  workers: 1,
  retries: process.env.CI ? 2 : 0,
  reporter: 'html',
  timeout: 60000,
  globalSetup: './playwright/global-setup.ts',

  use: {
    baseURL: 'http://127.0.0.1:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    // Auth-flow regression suite (T-7 Phase 49) — fresh browser, no storageState.
    // baseURL overrides to localhost: Keycloak realm redirectUris only contain
    // http://localhost:5173/auth/callback — pkce_state cookie set on 127.0.0.1 is
    // NOT sent on the localhost callback, breaking state validation. D-17 narrows to
    // api-proxy probes only (iter 3, 2026-05-16). See .jdi/DECISIONS.md D-17.
    {
      name: 'auth-flow',
      testDir: './playwright/specs',
      use: {
        ...devices['Desktop Chrome'],
        baseURL: 'http://localhost:5173',
      },
      testMatch: /auth-flow\.spec\.ts/,
    },

    // api-proxy regression suite (T-9 Phase 49 iter 2) — IPv4 + proxy smoke tests
    {
      name: 'api-proxy',
      testDir: './playwright/specs',
      use: {
        ...devices['Desktop Chrome'],
      },
      testMatch: /api-proxy\.spec\.ts/,
    },


    // Setup: authenticate and save storageState for each role
    {
      name: 'setup',
      testMatch: /.*\.setup\.ts/,
    },

    // Authenticated tests as admin-empresa (PJ owner)
    {
      name: 'admin-empresa',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'playwright/.auth/admin-empresa.json',
      },
      dependencies: ['setup'],
      testMatch: /.*(employee-management|access-group-change|permission-ui)\.spec\.ts/,
    },

    // Authenticated tests as viewer employee
    {
      name: 'viewer',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'playwright/.auth/viewer.json',
      },
      dependencies: ['setup'],
      testMatch: /.*permission-ui\.spec\.ts/,
    },

    // Fresh browser — no storageState (registration + ACF login)
    {
      name: 'registration',
      testMatch: /.*registration\.spec\.ts/,
    },

    // Authenticated tests for dashboard (uses admin-empresa state — dashboard access)
    {
      name: 'dashboard',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'playwright/.auth/admin-empresa.json',
      },
      dependencies: ['setup'],
      testMatch: /.*dashboard\.spec\.ts/,
    },

    // Employee login tests — uses viewer and admin-empresa in same file
    {
      name: 'employee-login',
      dependencies: ['setup'],
      testMatch: /.*employee-login\.spec\.ts/,
    },

    // ── Phase 50: Fundos section E2E ─────────────────────────────────────────

    // Fundos — TipoAtivo CRUD (admin-empresa, funds:write)
    {
      name: 'fundos-tipo-ativo',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'playwright/.auth/admin-empresa.json',
      },
      dependencies: ['setup'],
      testMatch: /.*fundos-tipo-ativo\.spec\.ts/,
    },

    // Fundos — Cedente CRUD (admin-empresa, funds:write)
    {
      name: 'fundos-cedente',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'playwright/.auth/admin-empresa.json',
      },
      dependencies: ['setup'],
      testMatch: /.*fundos-cedente\.spec\.ts/,
    },

    // Fundos — Consultoria + Custodiante CRUD (admin-empresa, funds:write)
    {
      name: 'fundos-consultoria-custodiante',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'playwright/.auth/admin-empresa.json',
      },
      dependencies: ['setup'],
      testMatch: /.*fundos-consultoria-custodiante\.spec\.ts/,
    },

    // Fundos — Fundo CRUD + status (admin-empresa, funds:write)
    {
      name: 'fundos-fundo',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'playwright/.auth/admin-empresa.json',
      },
      dependencies: ['setup'],
      testMatch: /.*fundos-fundo\.spec\.ts/,
    },

    // Fundos — Associations (FundoCedente, FundoTipoAtivo, CedenteTipoAtivo)
    {
      name: 'fundos-associations',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'playwright/.auth/admin-empresa.json',
      },
      dependencies: ['setup'],
      testMatch: /.*fundos-associations\.spec\.ts/,
    },

    // Fundos — Permission-gated UI + OTel security checks
    // viewer user (funds:read only) for write-button absence tests
    {
      name: 'fundos-permissions',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'playwright/.auth/viewer.json',
      },
      dependencies: ['setup'],
      testMatch: /.*fundos-permissions\.spec\.ts/,
    },

    // ── Phase 53: OTel end-to-end trace verification (T-8) ───────────────────
    // Fresh browser — login exercised within spec; no pre-built storageState.
    // Uses localhost (not 127.0.0.1) per D-17 so Keycloak PKCE cookie matches.
    {
      name: 'otel-trace',
      testDir: './playwright/specs',
      use: {
        ...devices['Desktop Chrome'],
        baseURL: 'http://localhost:5173',
      },
      testMatch: /.*otel-trace\.spec\.ts/,
    },
  ],

  // No webServer — Docker Compose must be running before E2E tests
});