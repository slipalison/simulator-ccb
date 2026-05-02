import { test as setup, expect } from '@playwright/test';
import { fileURLToPath } from 'url';
import path from 'path';
import fs from 'fs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const authFile = path.join(__dirname, '../../playwright/.auth/viewer.json');
const viewerFile = path.join(__dirname, '../../playwright/.auth/viewer-creds.json');

setup('authenticate as viewer', async ({ page }) => {
  let viewerEmail: string;
  let viewerPassword: string;

  // Use env vars if available, otherwise read from admin-empresa setup
  if (process.env.E2E_VIEWER_EMAIL && process.env.E2E_VIEWER_PASSWORD) {
    viewerEmail = process.env.E2E_VIEWER_EMAIL;
    viewerPassword = process.env.E2E_VIEWER_PASSWORD;
  } else {
    const creds = JSON.parse(fs.readFileSync(viewerFile, 'utf-8'));
    viewerEmail = creds.email;
    viewerPassword = creds.temporaryPassword;
  }

  await page.goto('http://localhost:5173/');

  await expect(page.locator('#username')).toBeVisible({ timeout: 30000 });

  await page.locator('#username').fill(viewerEmail);
  await page.locator('#password').fill(viewerPassword);
  await page.locator('#kc-login').click();

  // Handle Keycloak UPDATE_PASSWORD required action for new employees
  const passwordNew = page.locator('#password-new');
  if (await passwordNew.isVisible({ timeout: 5000 }).catch(() => false)) {
    const newPassword = process.env.E2E_PJ_PASSWORD || 'E2e@Test2026';
    await passwordNew.fill(newPassword);
    await page.locator('#password-confirm').fill(newPassword);
    await page.locator('#kc-login').click();
  }

  await page.waitForURL('http://localhost:5173/**', { timeout: 60000 });
  await expect(page).toHaveURL(/localhost:5173\/(employees|profile)/, { timeout: 15000 });
  await expect(page.locator('nav')).toBeVisible({ timeout: 15000 });

  await page.context().storageState({ path: authFile });
});