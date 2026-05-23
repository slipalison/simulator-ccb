/**
 * admin-fundos-permissions.spec.ts — Permission gating E2E regression (T-8, Phase 52)
 *
 * Tests:
 *  1. Unauthenticated user accessing /admin/fundos → redirected to /admin/login
 *  2. Authenticated admin can access /admin/fundos (sidebar Fundos group visible)
 *  3. D-12: no tokens in storage after navigation
 */

import { test, expect } from '@playwright/test';

const ADMIN_EMAIL = 'e2e-admin@example.com';
const ADMIN_PASSWORD = 'E2EAdmin@123!';

async function loginAsAdmin(page: any) {
  await page.goto('http://localhost:5174/admin/login');
  await page.getByTestId('admin-login-button').click();
  await page.waitForURL(/keycloak|localhost.*auth.*login/, { timeout: 30000 });
  await page.fill('#username', ADMIN_EMAIL);
  await page.fill('#password', ADMIN_PASSWORD);
  await page.click('#kc-login');
  await page.waitForURL('**/admin/**', { timeout: 30000 });
}

test.describe('Admin Fundos Permissions', () => {
  test('unauthenticated access to /admin/fundos redirects to /admin/login', async ({ page }) => {
    // Fresh browser — no session
    await page.goto('http://localhost:5174/admin/fundos');
    // AdminLayout redirects unauthenticated users to /admin/login
    await page.waitForURL('**/admin/login**', { timeout: 15000 });
    expect(page.url()).toContain('/admin/login');
  });

  test('authenticated admin sees Fundos group in sidebar', async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('http://localhost:5174/admin/fundos');
    await page.waitForSelector('[data-testid="sidebar-fundos-group"]', { timeout: 15000 });
    await expect(page.getByTestId('sidebar-fundos-group')).toBeVisible();
    await expect(page.getByTestId('sidebar-fundos-link')).toBeVisible();
    await expect(page.getByTestId('sidebar-cedentes-link')).toBeVisible();
  });

  test('D-12: no token material in localStorage/sessionStorage after admin login', async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('http://localhost:5174/admin/fundos');

    // Security gate: no token storage in browser (D-12)
    const tokenInStorage = await page.evaluate(() => {
      const lsKeys = Object.keys(localStorage);
      const ssKeys = Object.keys(sessionStorage);
      const tokenPattern = /token|access|refresh|jwt|bearer/i;
      const lsLeak = lsKeys.some((k) => tokenPattern.test(k));
      const ssLeak = ssKeys.some((k) => tokenPattern.test(k));
      return { lsLen: localStorage.length, ssLen: sessionStorage.length, lsLeak, ssLeak };
    });

    expect(tokenInStorage.lsLeak).toBe(false);
    expect(tokenInStorage.ssLeak).toBe(false);
  });

  test('sidebar Fundos nav links navigate correctly without crash', async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('http://localhost:5174/admin/companies');
    await page.waitForSelector('[data-testid="sidebar-fundos-link"]', { timeout: 15000 });

    await page.getByTestId('sidebar-fundos-link').click();
    await page.waitForURL('**/admin/fundos**', { timeout: 10000 });
    // Should render fundos page without crash
    await page.waitForSelector('[data-testid="fundos-table-container"], [data-testid="fundos-error"]', { timeout: 15000 });
  });
});
