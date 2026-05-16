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
    // Auth-flow regression suite (T-7 Phase 49) — fresh browser, no storageState
    {
      name: 'auth-flow',
      testDir: './playwright/specs',
      use: {
        ...devices['Desktop Chrome'],
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
  ],

  // No webServer — Docker Compose must be running before E2E tests
});