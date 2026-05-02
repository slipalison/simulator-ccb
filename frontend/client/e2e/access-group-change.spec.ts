import { test, expect } from '@playwright/test';
import { KeycloakLoginPage } from './pages/keycloak-login.page';
import { EmployeesPage } from './pages/employees.page';
import { generateEmployeeData } from './fixtures/test-data';
import { getAccessTokenFromCookies, decodeAccessToken } from './fixtures/jwt-utils';

test.describe('E2E-06: PJ muda access group → re-login → permissões atualizadas', () => {
  test('deve mudar group de viewer para admin-empresa e verificar permissões atualizadas', async ({ page, context }) => {
    const employeesPage = new EmployeesPage(page);
    const keycloakLogin = new KeycloakLoginPage(page);
    const employeeData = generateEmployeeData();

    // 1. As admin-empresa, create a new viewer employee
    const meResponse = await page.request.get('http://localhost:5173/auth/me');
    const meData = await meResponse.json();
    const companyId = meData.companyId;
    expect(companyId).toBeTruthy();

    const createResponse = await page.request.post(
      `http://localhost:8080/api/companies/${companyId}/employees/registration`,
      {
        data: {
          nome: employeeData.nome,
          cpf: employeeData.cpf,
          email: employeeData.email,
          phone: employeeData.phone,
        },
        headers: { 'Content-Type': 'application/json' },
      }
    );
    expect(createResponse.status()).toBe(201);
    const createdEmployee = await createResponse.json();
    const employeeId = createdEmployee.id;
    const employeeTempPassword = createdEmployee.temporaryPassword;

    // 2. Logout current session and log in as the new viewer employee
    await page.goto('http://localhost:5173/auth/logout');
    await page.waitForURL(/localhost:8180|localhost:5173/, { timeout: 30000 });

    // Clear all cookies to force fresh login
    await context.clearCookies();

    // Navigate to trigger ACF login
    await page.goto('http://localhost:5173/');
    await expect(keycloakLogin.usernameInput).toBeVisible({ timeout: 30000 });
    await keycloakLogin.login(employeeData.email, employeeTempPassword);

    // Handle Keycloak UPDATE_PASSWORD required action for new employees
    // Keycloak forces password change on first login
    const employeePassword = 'E2e@Test2026New';
    const currentUrl = page.url();
    if (currentUrl.includes('localhost:8180')) {
      const newPasswordInput = page.locator('#password-new');
      if (await newPasswordInput.isVisible({ timeout: 5000 }).catch(() => false)) {
        await newPasswordInput.fill(employeePassword);
        await page.locator('#password-confirm').fill(employeePassword);
        await page.locator('#kc-login').click();
      }
    }

    // Wait for redirect back to app
    await page.waitForURL('http://localhost:5173/**', { timeout: 60000 });
    await expect(page).toHaveURL(/\/employees/, { timeout: 15000 });

    // 3. Verify viewer has read-only permissions
    const hasActionsBefore = await employeesPage.hasActionsColumn();
    expect(hasActionsBefore).toBe(false);

    // 4. Verify JWT shows viewer group
    const viewerToken = await getAccessTokenFromCookies(page);
    if (viewerToken) {
      const viewerClaims = decodeAccessToken(viewerToken);
      const hasViewerGroup = viewerClaims.groups?.includes('viewer');
      expect(hasViewerGroup).toBeTruthy();
    }

    // 5. Verify viewer cannot see Dashboard sidebar
    const dashboardLinkBefore = page.locator('a[href="/dashboard"]');
    await expect(dashboardLinkBefore).not.toBeVisible();

    // 6. Logout and re-login as admin-empresa to change group
    await page.goto('http://localhost:5173/auth/logout');
    await context.clearCookies();
    await page.goto('http://localhost:5173/');
    await expect(keycloakLogin.usernameInput).toBeVisible({ timeout: 30000 });
    await keycloakLogin.login(
      process.env.E2E_PJ_EMAIL!,
      process.env.E2E_PJ_PASSWORD!,
    );
    await page.waitForURL('http://localhost:5173/**', { timeout: 60000 });

    // 7. Navigate to employees and change group via ChangeAccessGroupDialog
    await employeesPage.goto();
    await page.getByTestId('refresh-button').click();
    await page.waitForResponse(
      (resp) => resp.url().includes('/employees') && resp.status() === 200,
      { timeout: 10000 }
    ).catch(() => {
      // Response may have already fired before we started listening
    });

    // Open change group dialog for the employee
    await page.getByTestId(`actions-dropdown-trigger-${employeeId}`).click();
    await page.getByTestId(`action-change-group-${employeeId}`).click();

    // Wait for dialog
    await expect(page.getByTestId('change-access-group-dialog')).toBeVisible();

    // Select admin-empresa group using Radix Select
    await page.getByTestId('new-access-group-select').click();
    await page.getByRole('option', { name: 'Admin Empresa' }).click();

    // Confirm the group change
    await page.getByTestId('change-group-confirm-button').click();

    // Wait for dialog to close
    await expect(page.getByTestId('change-access-group-dialog')).not.toBeVisible({ timeout: 10000 });

    // 8. Wait for Keycloak eventual consistency (group change propagation)
    // The change-group API call updates Keycloak groups; allow 2-3s for propagation
    await page.waitForTimeout(3000);

    // 9. Logout admin-empresa and re-login as the employee with updated group
    await page.goto('http://localhost:5173/auth/logout');
    await context.clearCookies();

    await page.goto('http://localhost:5173/');
    await expect(keycloakLogin.usernameInput).toBeVisible({ timeout: 30000 });
    await keycloakLogin.login(employeeData.email, employeePassword);
    await page.waitForURL('http://localhost:5173/**', { timeout: 60000 });

    // 10. Verify the employee now has admin-empresa permissions
    // The employee may be redirected to /employees (admin-empresa default route)
    await expect(page).toHaveURL(/\/(employees|dashboard)/, { timeout: 15000 });

    // Verify Ações column IS now visible
    await page.waitForTimeout(2000); // Let the page fully render
    const hasActionsAfter = await employeesPage.hasActionsColumn();
    expect(hasActionsAfter).toBe(true);

    // 11. Verify JWT shows admin-empresa group
    const adminToken = await getAccessTokenFromCookies(page);
    if (adminToken) {
      const adminClaims = decodeAccessToken(adminToken);
      const hasAdminGroup = adminClaims.groups?.includes('admin-empresa');
      expect(hasAdminGroup).toBeTruthy();
    }

    // 12. Verify Dashboard sidebar link is now visible (admin-empresa)
    const dashboardLinkAfter = page.locator('a[href="/dashboard"]');
    await expect(dashboardLinkAfter).toBeVisible();

    // 13. Verify /auth/me returns admin-empresa access group
    const meAfterChange = await page.request.get('http://localhost:5173/auth/me');
    const meAfterData = await meAfterChange.json();
    expect(meAfterData.accessGroup).toBe('admin-empresa');
  });
});