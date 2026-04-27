import { test, expect } from '@playwright/test';
import { DashboardPage } from './pages/dashboard.page';

test.describe('E2E-02: Dashboard exibe cards mock', () => {
  test('deve exibir dashboard com 6 cards após login como admin-empresa', async ({ page }) => {
    const dashboardPage = new DashboardPage(page);

    // 1. Navigate to dashboard (already authenticated via admin-empresa storageState)
    await dashboardPage.goto();

    // 2. Verify dashboard heading
    await expect(page.locator('h1', { hasText: 'Dashboard' })).toBeVisible();

    // 3. Verify welcome message with username
    await expect(page.locator('text=Bem-vindo(a)')).toBeVisible();

    // 4. Verify each of the 6 mock cards is visible
    const expectedCards = [
      'Total Funcionários',
      'Ativos',
      'Bloqueados',
      'Logins Recentes',
      'Ações Recentes',
      'Último Login',
    ];

    for (const cardTitle of expectedCards) {
      await expect(
        page.locator(`text=${cardTitle}`).first()
      ).toBeVisible();
    }
  });
});