import { test as setup, expect } from '@playwright/test';
import { fileURLToPath } from 'url';
import path from 'path';
import { generateValidCpf } from '../fixtures/test-data';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const authFile = path.join(__dirname, '../../playwright/.auth/admin-empresa.json');

/**
 * Setup: authenticate as admin-empresa (PJ owner) and save storageState.
 * Also creates a viewer employee for subsequent viewer tests.
 */
setup('authenticate as admin-empresa', async ({ page }) => {
  // Navigate to app — triggers ACF redirect to Keycloak
  await page.goto('http://localhost:5173/');

  // Wait for Keycloak login page to appear
  await expect(page.locator('#username')).toBeVisible({ timeout: 30000 });

  // Fill credentials from environment variables
  await page.locator('#username').fill(process.env.E2E_PJ_EMAIL!);
  await page.locator('#password').fill(process.env.E2E_PJ_PASSWORD!);
  await page.locator('#kc-login').click();

  // Wait for redirect back to the app after ACF callback
  await page.waitForURL('http://localhost:5173/**', { timeout: 60000 });

  // Verify authenticated — should be on /employees (admin-empresa default route)
  await expect(page).toHaveURL(/localhost:5173\/(employees|dashboard|profile)/, { timeout: 15000 });

  // Verify sidebar is visible (only rendered when authenticated)
  await expect(page.locator('nav')).toBeVisible({ timeout: 15000 });

  // Create viewer employee via API for viewer setup
  const meResp = await page.request.get('http://localhost:5173/auth/me');
  const meData = await meResp.json();

  if (meData.companyId && process.env.E2E_VIEWER_EMAIL) {
    try {
      await page.request.post(
        `http://localhost:8080/api/companies/${meData.companyId}/employees/registration`,
        {
          data: {
            nome: 'Viewer E2E',
            cpf: generateValidCpf(),
            email: process.env.E2E_VIEWER_EMAIL,
            phone: '11988880000',
          },
          headers: { 'Content-Type': 'application/json' },
        },
      );
    } catch {
      // Employee may already exist from a previous run — that's OK
    }
  }

  // Save storage state (includes httpOnly cookies)
  await page.context().storageState({ path: authFile });
});