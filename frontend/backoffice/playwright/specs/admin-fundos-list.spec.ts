/**
 * admin-fundos-list.spec.ts — Backoffice Fundos list page E2E regression (T-8, Phase 52)
 *
 * Tests:
 *  1. Navigate to /admin/fundos → table renders (or empty state)
 *  2. Empresa filter dropdown change → URL updates with empresaId
 *  3. Search input → URL updates with search param
 *  4. URL bookmarkable with params
 *  5. D-12: no tokens in localStorage/sessionStorage
 *
 * Uses backoffice-auth project (localhost:5174) for Keycloak auth flow.
 * Requires docker compose up -d before running.
 */

import { test, expect } from '@playwright/test';

const ADMIN_EMAIL = 'e2e-admin@example.com';
const ADMIN_PASSWORD = 'E2EAdmin@123!';

async function loginAsAdmin(page: any) {
  await page.goto('http://localhost:5174/admin/login');
  await page.getByTestId('admin-login-button').click();
  // Keycloak login form
  await page.waitForURL(/keycloak|localhost.*auth.*login/, { timeout: 30000 });
  await page.fill('#username', ADMIN_EMAIL);
  await page.fill('#password', ADMIN_PASSWORD);
  await page.click('#kc-login');
  await page.waitForURL('**/admin/**', { timeout: 30000 });
}

test.describe('Admin Fundos List Pages', () => {
  test.skip(process.env.CI !== undefined && process.env.SKIP_E2E !== undefined,
    'Skipping E2E in CI without docker stack');

  test('navigates to /admin/fundos and renders page without invariant errors', async ({ page }) => {
    await loginAsAdmin(page);
    const consoleErrors: string[] = [];
    page.on('console', (msg) => {
      if (msg.type() === 'error') consoleErrors.push(msg.text());
    });

    await page.goto('http://localhost:5174/admin/fundos');
    await page.waitForSelector('[data-testid="fundos-table-container"], [data-testid="fundos-error"]', { timeout: 15000 });

    // Zero invariant errors
    const invariantErrors = consoleErrors.filter((e) => e.includes('Invariant failed'));
    expect(invariantErrors).toHaveLength(0);
  });

  test('empresa filter changes URL with empresaId param', async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('http://localhost:5174/admin/fundos');
    await page.waitForSelector('[data-testid="fundos-empresa-filter"]', { timeout: 15000 });

    const dropdown = page.getByTestId('fundos-empresa-filter');
    const options = await dropdown.locator('option').all();
    if (options.length > 1) {
      const empresaId = await options[1].getAttribute('value');
      if (empresaId) {
        await dropdown.selectOption(empresaId);
        await expect(page).toHaveURL(new RegExp(`empresaId=${empresaId}`));
      }
    }
  });

  test('search input updates URL with search param', async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('http://localhost:5174/admin/fundos');
    await page.waitForSelector('[data-testid="fundos-search"]', { timeout: 15000 });

    await page.getByTestId('fundos-search').fill('test');
    await page.waitForTimeout(400); // debounce
    await expect(page).toHaveURL(/search=test/);
  });

  test('URL bookmarkable — opens with params in correct state', async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('http://localhost:5174/admin/fundos?page=1&search=alpha');
    await page.waitForSelector('[data-testid="fundos-search"]', { timeout: 15000 });
    const searchInput = await page.getByTestId('fundos-search').inputValue();
    expect(searchInput).toBe('alpha');
  });

  test('D-12: no tokens in localStorage or sessionStorage after login', async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('http://localhost:5174/admin/fundos');
    const lsLen = await page.evaluate(() => localStorage.length);
    const ssLen = await page.evaluate(() => sessionStorage.length);
    expect(lsLen).toBe(0);
    expect(ssLen).toBe(0);
  });

  test('navigates to /admin/cedentes successfully', async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('http://localhost:5174/admin/cedentes');
    await page.waitForSelector('[data-testid="cedentes-table-container"], [data-testid="cedentes-error"]', { timeout: 15000 });
  });

  test('navigates to /admin/consultorias-fundo successfully', async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('http://localhost:5174/admin/consultorias-fundo');
    await page.waitForSelector('[data-testid="consultorias-table-container"], [data-testid="consultorias-error"]', { timeout: 15000 });
  });

  test('navigates to /admin/custodiantes successfully', async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto('http://localhost:5174/admin/custodiantes');
    await page.waitForSelector('[data-testid="custodiantes-table-container"], [data-testid="custodiantes-error"]', { timeout: 15000 });
  });
});
