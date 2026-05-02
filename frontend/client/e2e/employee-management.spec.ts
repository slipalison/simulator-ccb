import { test, expect } from '@playwright/test';
import { EmployeesPage } from './pages/employees.page';
import { generateEmployeeData } from './fixtures/test-data';

test.describe('E2E-03: PJ cria funcionário via API → aparece na lista', () => {
  test('deve criar funcionário e vê-lo na lista com status ativo e group viewer', async ({ page }) => {
    const employeesPage = new EmployeesPage(page);
    const employee = generateEmployeeData();

    // 1. Navigate to employees page (already authenticated via admin-empresa storageState)
    await employeesPage.goto();

    // Verify employees page is visible
    await expect(page.getByTestId('employees-page')).toBeVisible();

    // 2. Get companyId from /auth/me (already authenticated via storageState)
    const meResponse = await page.request.get('http://localhost:5173/auth/me');
    const meData = await meResponse.json();
    const companyId = meData.companyId;
    expect(companyId).toBeTruthy();

    // 3. Create employee via API (no "Create Employee" UI button in Phase 40)
    const createResponse = await page.request.post(
      `http://localhost:8080/api/companies/${companyId}/employees/registration`,
      {
        data: {
          nome: employee.nome,
          cpf: employee.cpf,
          email: employee.email,
          phone: employee.phone,
        },
        headers: { 'Content-Type': 'application/json' },
      }
    );
    expect(createResponse.status()).toBe(201);
    const createdEmployee = await createResponse.json();
    expect(createdEmployee.id).toBeTruthy();

    // 4. Refresh employees list via the refresh button
    await page.getByTestId('refresh-button').click();
    // Wait for the employee list API response to complete
    await page.waitForResponse(
      (resp) => resp.url().includes('/employees') && resp.status() === 200,
      { timeout: 10000 }
    ).catch(() => {
      // Response may have already fired before we started listening
    });

    // 5. Verify the new employee appears in the table
    const employeeRow = page.getByTestId(`employee-row-${createdEmployee.id}`);
    await expect(employeeRow).toBeVisible({ timeout: 10000 });

    // 6. Verify group badge shows "Viewer" (default group for new employees)
    const groupBadge = page.getByTestId(`badge-group-${createdEmployee.id}`);
    await expect(groupBadge).toBeVisible();
    await expect(groupBadge).toContainText('Viewer');

    // 7. Verify status badge shows "Ativo"
    const statusBadge = page.getByTestId(`badge-status-active-${createdEmployee.id}`);
    await expect(statusBadge).toBeVisible();
    await expect(statusBadge).toContainText('Ativo');

    // 8. Verify admin-empresa can see the Ações column
    const hasActions = await employeesPage.hasActionsColumn();
    expect(hasActions).toBe(true);

    // 9. Verify actions dropdown is accessible for the new employee
    await page.getByTestId(`actions-dropdown-trigger-${createdEmployee.id}`).click();
    await expect(
      page.getByTestId(`actions-dropdown-content-${createdEmployee.id}`)
    ).toBeVisible();
  });
});