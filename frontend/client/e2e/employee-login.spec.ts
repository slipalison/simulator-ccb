import { test, expect } from '@playwright/test';
import { KeycloakLoginPage } from './pages/keycloak-login.page';
import { EmployeesPage } from './pages/employees.page';

test.describe('E2E-04: Login como funcionário → redirect baseado no access group', () => {
  test('viewer deve fazer login e ser redirecionado para /employees (read-only)', async ({ page }) => {
    const keycloakLogin = new KeycloakLoginPage(page);

    // 1. Navigate to root — triggers ACF login
    await page.goto('http://localhost:5173/');

    // 2. Wait for Keycloak login page
    await expect(keycloakLogin.usernameInput).toBeVisible({ timeout: 30000 });

    // 3. Login as viewer employee
    await keycloakLogin.login(
      process.env.E2E_VIEWER_EMAIL!,
      process.env.E2E_VIEWER_PASSWORD!,
    );

    // 4. Wait for redirect back to app
    await page.waitForURL('http://localhost:5173/**', { timeout: 60000 });

    // 5. Verify redirected to /employees (viewer default route)
    await expect(page).toHaveURL(/\/employees/, { timeout: 15000 });

    // 6. Verify viewer permissions: NO Ações column
    const employeesPage = new EmployeesPage(page);
    const hasActions = await employeesPage.hasActionsColumn();
    expect(hasActions).toBe(false);

    // 7. Verify viewer does NOT see Dashboard in sidebar
    const dashboardLink = page.locator('a[href="/dashboard"]');
    await expect(dashboardLink).not.toBeVisible();

    // 8. Verify Employees sidebar link IS visible
    const employeesLink = page.locator('a[href="/employees"]');
    await expect(employeesLink).toBeVisible();
  });

  test('admin-empresa deve fazer login e ser redirecionado para /employees (com ações)', async ({ page }) => {
    const keycloakLogin = new KeycloakLoginPage(page);

    // 1. Navigate to root
    await page.goto('http://localhost:5173/');

    // 2. Wait for Keycloak login page
    await expect(keycloakLogin.usernameInput).toBeVisible({ timeout: 30000 });

    // 3. Login as admin-empresa (PJ owner)
    await keycloakLogin.login(
      process.env.E2E_PJ_EMAIL!,
      process.env.E2E_PJ_PASSWORD!,
    );

    // 4. Wait for redirect back
    await page.waitForURL('http://localhost:5173/**', { timeout: 60000 });

    // 5. Verify redirected to /employees (admin-empresa default route)
    await expect(page).toHaveURL(/\/employees/, { timeout: 15000 });

    // 6. Verify admin-empresa permissions: Ações column IS visible
    const employeesPage = new EmployeesPage(page);
    const hasActions = await employeesPage.hasActionsColumn();
    expect(hasActions).toBe(true);

    // 7. Verify Dashboard sidebar link IS visible
    const dashboardLink = page.locator('a[href="/dashboard"]');
    await expect(dashboardLink).toBeVisible();
  });

  test('dashboard employee deve fazer login e ser redirecionado para /dashboard', async ({ page }) => {
    // Note: A dashboard employee must be created in the setup phase or via API.
    // For this test, we use E2E_DASHBOARD_EMAIL/PASSWORD env vars.
    // If env var not set, skip test.
    test.skip(!process.env.E2E_DASHBOARD_EMAIL, 'E2E_DASHBOARD_EMAIL not set');

    const keycloakLogin = new KeycloakLoginPage(page);

    await page.goto('http://localhost:5173/');
    await expect(keycloakLogin.usernameInput).toBeVisible({ timeout: 30000 });
    await keycloakLogin.login(
      process.env.E2E_DASHBOARD_EMAIL!,
      process.env.E2E_DASHBOARD_PASSWORD!,
    );
    await page.waitForURL('http://localhost:5173/**', { timeout: 60000 });

    // Verify redirected to /dashboard (dashboard default route)
    await expect(page).toHaveURL(/\/dashboard/, { timeout: 15000 });
    await expect(page.locator('h1', { hasText: 'Dashboard' })).toBeVisible();
  });
});