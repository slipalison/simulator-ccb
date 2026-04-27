import { test as setup, expect } from '@playwright/test';
import { fileURLToPath } from 'url';
import path from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const authFile = path.join(__dirname, '../../playwright/.auth/viewer.json');

/**
 * Setup: authenticate as viewer employee and save storageState.
 * Assumes the viewer employee was already created by the admin-empresa setup.
 */
setup('authenticate as viewer', async ({ page }) => {
  // Navigate to app — triggers ACF redirect to Keycloak
  await page.goto('http://localhost:5173/');

  // Wait for Keycloak login page to appear
  await expect(page.locator('#username')).toBeVisible({ timeout: 30000 });

  // Fill credentials from environment variables
  await page.locator('#username').fill(process.env.E2E_VIEWER_EMAIL!);
  await page.locator('#password').fill(process.env.E2E_VIEWER_PASSWORD!);
  await page.locator('#kc-login').click();

  // Wait for redirect back to the app after ACF callback
  await page.waitForURL('http://localhost:5173/**', { timeout: 60000 });

  // Verify authenticated — viewer default route is /employees
  await expect(page).toHaveURL(/localhost:5173\/(employees|profile)/, { timeout: 15000 });

  // Verify sidebar is visible (only rendered when authenticated)
  await expect(page.locator('nav')).toBeVisible({ timeout: 15000 });

  // Save storage state (includes httpOnly cookies)
  await page.context().storageState({ path: authFile });
});