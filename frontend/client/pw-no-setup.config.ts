import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './playwright/specs',
  fullyParallel: false,
  forbidOnly: false,
  workers: 1,
  retries: 0,
  reporter: 'list',
  timeout: 60000,
  use: {
    baseURL: 'http://127.0.0.1:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    // auth-flow: baseURL override to localhost — Keycloak realm redirectUris only
    // contain localhost variants; using 127.0.0.1 breaks pkce_state cookie delivery.
    // D-17 narrowed to api-proxy probes only (iter 3, 2026-05-16).
    {
      name: 'auth-flow',
      testDir: './playwright/specs',
      use: { ...devices['Desktop Chrome'], baseURL: 'http://localhost:5173' },
      testMatch: /auth-flow\.spec\.ts/,
    },
    {
      name: 'api-proxy',
      testDir: './playwright/specs',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /api-proxy\.spec\.ts/,
    },
  ],
});
