import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Page object for the Employees page (/employees).
 * Selectors match data-testid attributes from EmployeesPage.tsx and EmployeesTable.tsx.
 */
export class EmployeesPage {
  readonly page: Page;
  readonly pageContainer: Locator;
  readonly tableWrapper: Locator;
  readonly employeeRows: Locator;
  readonly refreshButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.pageContainer = page.getByTestId('employees-page');
    this.tableWrapper = page.getByTestId('employees-table-wrapper');
    this.employeeRows = page.locator('[data-testid^="employee-row-"]');
    this.refreshButton = page.getByTestId('refresh-button');
  }

  /** Navigate to the employees page */
  async goto(): Promise<void> {
    await this.page.goto('/employees');
    await expect(this.pageContainer).toBeVisible();
  }

  /** Count employee rows in the table */
  async getRowCount(): Promise<number> {
    return await this.employeeRows.count();
  }

  /** Check if the Ações (Actions) column header is visible — only for non-viewer users */
  async hasActionsColumn(): Promise<boolean> {
    const header = this.page.locator('th', { hasText: 'Ações' });
    return await header.isVisible();
  }

  /** Get a specific employee row by id */
  async getEmployeeRow(id: string): Promise<Locator> {
    return this.page.getByTestId(`employee-row-${id}`);
  }

  /** Get the access group badge text for an employee */
  async getGroupBadge(employeeId: string): Promise<string> {
    const badge = this.page.getByTestId(`badge-group-${employeeId}`);
    return await badge.textContent() ?? '';
  }

  /** Get the status badge text for an employee */
  async getStatusBadge(employeeId: string): Promise<string> {
    const activeBadge = this.page.getByTestId(`badge-status-active-${employeeId}`);
    if (await activeBadge.isVisible()) {
      return await activeBadge.textContent() ?? 'Ativo';
    }
    const blockedBadge = this.page.getByTestId(`badge-status-blocked-${employeeId}`);
    return await blockedBadge.textContent() ?? 'Bloqueado';
  }

  /** Open the change access group dialog for an employee */
  async openChangeAccessGroupDialog(employeeId: string): Promise<void> {
    // Open actions dropdown
    await this.page.getByTestId(`actions-dropdown-trigger-${employeeId}`).click();
    // Click change group action
    await this.page.getByTestId(`action-change-group-${employeeId}`).click();
    // Wait for dialog
    await expect(this.page.getByTestId('change-access-group-dialog')).toBeVisible();
  }

  /** Select a new access group in the change-group dialog */
  async selectNewGroup(groupName: string): Promise<void> {
    await this.page.getByTestId('new-access-group-select').click();
    await this.page.getByRole('option', { name: groupName }).click();
  }

  /** Confirm the access group change */
  async confirmChangeGroup(): Promise<void> {
    await this.page.getByTestId('change-group-confirm-button').click();
  }
}