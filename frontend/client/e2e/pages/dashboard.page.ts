import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Page object for the Dashboard page (/dashboard).
 * Accessible by admin-empresa and dashboard groups.
 */
export class DashboardPage {
  readonly page: Page;
  readonly container: Locator;
  readonly title: Locator;

  // Card text locators
  readonly totalEmployeesCard: Locator;
  readonly activeEmployeesCard: Locator;
  readonly blockedEmployeesCard: Locator;
  readonly recentLoginsCard: Locator;
  readonly recentActionsCard: Locator;
  readonly lastLoginCard: Locator;

  constructor(page: Page) {
    this.page = page;
    this.container = page.locator('h1', { hasText: 'Dashboard' });
    this.title = page.locator('h1', { hasText: 'Dashboard' });

    // Cards identified by their heading text (from DashboardCards component)
    this.totalEmployeesCard = page.getByText('Total Funcionários');
    this.activeEmployeesCard = page.getByText('Funcionários Ativos');
    this.blockedEmployeesCard = page.getByText('Funcionários Inativos');
    this.recentLoginsCard = page.getByText('Logins Recentes');
    this.recentActionsCard = page.getByText('Ações por Período');
    this.lastLoginCard = page.getByText('Último Login');
  }

  /** Navigate to the dashboard page */
  async goto(): Promise<void> {
    await this.page.goto('/dashboard');
    await expect(this.title).toBeVisible();
  }

  /** Check if a specific card is visible */
  async hasCard(cardName: string): Promise<boolean> {
    return await this.page.getByText(cardName).isVisible();
  }
}