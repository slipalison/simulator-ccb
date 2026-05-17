import { defineConfig, devices } from '@playwright/test';

/**
 * Backoffice Playwright config without globalSetup — for running api-proxy specs
 * that do NOT require the seed-test-users.sh step (no auth needed for proxy probes).
 *
 * Use this config when jq is unavailable on the host (blocks seed-test-users.sh):
 *   pnpm exec playwright test api-proxy.spec.ts --config=pw-no-setup.config.ts
 *
 * The main playwright.config.ts (with globalSetup) should be used for auth-flow specs
 * that require seeded e2e-admin@example.com credentials.
 */
export default defineConfig({
  testDir: './playwright/specs',
  fullyParallel: false,
  forbidOnly: false,
  workers: 1,
  retries: 0,
  reporter: 'list',
  timeout: 60000,
  use: {
    baseURL: 'http://127.0.0.1:5174',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'api-proxy',
      testDir: './playwright/specs',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /api-proxy\.spec\.ts/,
    },
  ],
});
