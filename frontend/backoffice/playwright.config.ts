import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E configuration for backoffice frontend.
 *
 * Mirrors frontend/client/playwright.config.ts with these differences:
 * - baseURL: http://localhost:5174  (backoffice port)
 * - testDir: ./playwright/specs    (backoffice spec location)
 * - Output dirs: playwright/results/ and playwright-report/
 * - No webServer block — Docker Compose must be running before E2E tests.
 *   The stack is declared in compose.yaml; run `docker compose up -d` first.
 * - Sequential execution (workers: 1) — shared Keycloak session state.
 * - globalSetup verifies compose stack health before tests run.
 */
export default defineConfig({
  testDir: './playwright/specs',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  workers: 1,
  retries: process.env.CI ? 2 : 0,
  reporter: [['html', { outputFolder: 'playwright-report' }]],
  outputDir: 'playwright/results',
  timeout: 60000,
  globalSetup: './playwright/global-setup.ts',

  use: {
    baseURL: 'http://localhost:5174',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    // Fresh browser — no storageState (ACF login flow)
    {
      name: 'backoffice-auth',
      use: {
        ...devices['Desktop Chrome'],
      },
      testMatch: /.*admin-auth-flow\.spec\.ts/,
    },
  ],

  // No webServer — Docker Compose must be running before E2E tests.
  // Run: docker compose up -d
  // Then: cd frontend/backoffice && pnpm test:e2e
});
