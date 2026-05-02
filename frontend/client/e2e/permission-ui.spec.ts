import { test, expect } from '@playwright/test';
import { EmployeesPage } from './pages/employees.page';
import { getAccessTokenFromCookies, decodeAccessToken } from './fixtures/jwt-utils';

test.describe('E2E-05: JWT claims + permission UI por access group', () => {
  test('viewer: JWT contém group "viewer" e UI é read-only', async ({ page }) => {
    const employeesPage = new EmployeesPage(page);

    // Navigate to employees (viewer storageState already applied)
    await employeesPage.goto();
    await expect(page.getByTestId('employees-page')).toBeVisible();

    // 1. Decode JWT from cookie and verify groups claim
    const accessToken = await getAccessTokenFromCookies(page);
    expect(accessToken).toBeTruthy();
    const claims = decodeAccessToken(accessToken!);

    // Verify viewer group in JWT
    const hasViewerGroup = claims.groups?.includes('viewer') ||
      claims.realm_access?.roles?.includes('viewer');
    expect(hasViewerGroup).toBeTruthy();

    // 2. Verify /auth/me returns viewer access group
    const meResponse = await page.request.get('http://localhost:5173/auth/me');
    const meData = await meResponse.json();
    expect(meData.accessGroup).toBe('viewer');

    // 3. Verify viewer UI: NO Ações column
    const hasActions = await employeesPage.hasActionsColumn();
    expect(hasActions).toBe(false);

    // 4. Verify NO Dashboard sidebar link for viewer
    const dashboardLink = page.locator('a[href="/dashboard"]');
    await expect(dashboardLink).not.toBeVisible();

    // 5. Verify Employees sidebar IS visible
    const employeesLink = page.locator('a[href="/employees"]');
    await expect(employeesLink).toBeVisible();

    // 6. Verify no action buttons visible for any employee in table
    const actionDropdowns = page.locator('[data-testid^="actions-dropdown-trigger-"]');
    const actionCount = await actionDropdowns.count();
    expect(actionCount).toBe(0);
  });

  test('admin-empresa: JWT contém group "admin-empresa" e UI tem ações completas', async ({ page }) => {
    // Determine which project we're in by checking /auth/me
    const meResponse = await page.request.get('http://localhost:5173/auth/me');
    const meData = await meResponse.json();

    test.skip(meData.accessGroup !== 'admin-empresa', 'This test only runs in admin-empresa project');

    const employeesPage = new EmployeesPage(page);

    await employeesPage.goto();
    await expect(page.getByTestId('employees-page')).toBeVisible();

    // 1. Decode JWT from cookie
    const accessToken = await getAccessTokenFromCookies(page);
    expect(accessToken).toBeTruthy();
    const claims = decodeAccessToken(accessToken!);

    const hasAdminGroup = claims.groups?.includes('admin-empresa') ||
      claims.realm_access?.roles?.includes('admin-empresa');
    expect(hasAdminGroup).toBeTruthy();

    // 2. Verify /auth/me returns admin-empresa access group
    expect(meData.accessGroup).toBe('admin-empresa');

    // 3. Verify admin-empresa UI: Ações column IS visible
    const hasActions = await employeesPage.hasActionsColumn();
    expect(hasActions).toBe(true);

    // 4. Verify Dashboard sidebar link IS visible
    const dashboardLink = page.locator('a[href="/dashboard"]');
    await expect(dashboardLink).toBeVisible();

    // 5. If there are employees, verify action buttons visible
    const actionDropdowns = page.locator('[data-testid^="actions-dropdown-trigger-"]');
    const actionCount = await actionDropdowns.count();
    if (actionCount > 0) {
      // Click first dropdown to verify actions
      await actionDropdowns.first().click();
      await expect(page.locator('[data-testid^="action-edit-"]').first()).toBeVisible();
      await expect(page.locator('[data-testid^="action-change-group-"]').first()).toBeVisible();
    }
  });
});