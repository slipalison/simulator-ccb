/**
 * admin-fundos-associations.spec.ts — N-N association list E2E regression (T-8, Phase 52)
 *
 * Tests:
 *  1. Navigate to each of 3 association list pages without crash
 *  2. Empresa filter renders on each
 *  3. No invariant errors
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

const ASSOCIATION_PAGES = [
  { path: '/admin/fundo-cedentes', filterTestId: 'fundo-cedentes-empresa-filter', containerTestId: 'fundo-cedentes-table-container' },
  { path: '/admin/fundo-tipos-ativos', filterTestId: 'fundo-tipos-ativos-empresa-filter', containerTestId: 'fundo-tipos-ativos-table-container' },
  { path: '/admin/cedente-tipos-ativos', filterTestId: 'cedente-tipos-ativos-empresa-filter', containerTestId: 'cedente-tipos-ativos-table-container' },
];

test.describe('Admin Fundos Association Lists', () => {
  for (const { path, filterTestId, containerTestId } of ASSOCIATION_PAGES) {
    test(`navigates to ${path} without invariant errors`, async ({ page }) => {
      await loginAsAdmin(page);
      const consoleErrors: string[] = [];
      page.on('console', (msg) => {
        if (msg.type() === 'error') consoleErrors.push(msg.text());
      });

      await page.goto(`http://localhost:5174${path}`);
      await page.waitForSelector(`[data-testid="${containerTestId}"]`, { timeout: 15000 });

      const invariantErrors = consoleErrors.filter((e) => e.includes('Invariant failed'));
      expect(invariantErrors).toHaveLength(0);
    });

    test(`${path} empresa filter is accessible`, async ({ page }) => {
      await loginAsAdmin(page);
      await page.goto(`http://localhost:5174${path}`);
      await page.waitForSelector(`[data-testid="${filterTestId}"]`, { timeout: 15000 });
      const dropdown = page.getByTestId(filterTestId);
      await expect(dropdown).toBeVisible();
      await expect(dropdown).toHaveAttribute('aria-label');
    });
  }
});
