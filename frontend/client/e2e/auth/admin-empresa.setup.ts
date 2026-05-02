import { test as setup, expect } from '@playwright/test';
import { fileURLToPath } from 'url';
import path from 'path';
import fs from 'fs';
import { generateValidCpf } from '../fixtures/test-data';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const authFile = path.join(__dirname, '../../playwright/.auth/admin-empresa.json');
const viewerFile = path.join(__dirname, '../../playwright/.auth/viewer-creds.json');

const VIEWER_EMAIL = 'e2e-viewer@test.com';

setup('authenticate as admin-empresa', async ({ page }) => {
  await page.goto('http://localhost:5173/');

  await expect(page.locator('#username')).toBeVisible({ timeout: 30000 });

  await page.locator('#username').fill(process.env.E2E_PJ_EMAIL!);
  await page.locator('#password').fill(process.env.E2E_PJ_PASSWORD!);
  await page.locator('#kc-login').click();

  await page.waitForURL('http://localhost:5173/**', { timeout: 60000 });

  await expect(page).toHaveURL(/localhost:5173\/(employees|dashboard|profile)/, { timeout: 15000 });
  await expect(page.locator('nav')).toBeVisible({ timeout: 15000 });

  // Create viewer employee via API using authenticated context
  const meResp = await page.request.get('http://localhost:5173/auth/me');
  const meData = await meResp.json();

  // Use companyId from /auth/me; fall back to extracting from page URL or cookies
  const companyId = meData.companyId;
  if (companyId) {
    try {
      const empResp = await page.request.post(
        `http://localhost:8080/api/companies/${companyId}/employees/registration`,
        {
          data: {
            nome: 'Viewer E2E',
            cpf: generateValidCpf(),
            email: VIEWER_EMAIL,
            phone: '11988880000',
          },
          headers: { 'Content-Type': 'application/json' },
        },
      );
      if (empResp.ok()) {
        const empData = await empResp.json();
        // Save viewer credentials for viewer setup
        fs.writeFileSync(viewerFile, JSON.stringify({
          email: VIEWER_EMAIL,
          temporaryPassword: empData.temporaryPassword,
        }));
      }
    } catch {
      // Employee may already exist — that's OK, viewer can use env vars
    }
  }

  await page.context().storageState({ path: authFile });
});