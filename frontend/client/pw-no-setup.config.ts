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
    {
      name: 'auth-flow',
      testDir: './playwright/specs',
      use: { ...devices['Desktop Chrome'] },
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
