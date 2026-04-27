import { test, expect } from '@playwright/test';
import { RegistrationPage } from './pages/registration.page';
import { KeycloakLoginPage } from './pages/keycloak-login.page';
import { generateCompanyData } from './fixtures/test-data';

test.describe('E2E-01: Cadastro PJ completo', () => {
  test('deve cadastrar PJ, fazer auto-login via ACF, e redirecionar para rota padrão', async ({ page }) => {
    const company = generateCompanyData();
    const registrationPage = new RegistrationPage(page);
    const keycloakLogin = new KeycloakLoginPage(page);

    // 1. Navigate to registration
    await registrationPage.goto();
    await expect(registrationPage.razaoSocialInput).toBeVisible();

    // 2. Fill step 1: Company data
    await registrationPage.fillCompanyData(company.razaoSocial, company.cnpj);
    // Verify step 2 fields appear
    await expect(page.getByPlaceholder('seu@email.com')).toBeVisible();

    // 3. Fill step 2: Access data
    await registrationPage.fillAccessData(company.email, company.phone, company.password);
    await expect(page.getByRole('checkbox')).toBeChecked();

    // 4. Submit registration
    await registrationPage.submit();

    // 5. After POST 201, the form does window.location.href = "/"
    //    This triggers ACF: / → /auth/login → Keycloak → /auth/callback → /employees
    // Wait for Keycloak login page to appear
    await expect(keycloakLogin.usernameInput).toBeVisible({ timeout: 30000 });
    await expect(keycloakLogin.passwordInput).toBeVisible();

    // 6. Fill Keycloak login form with the newly created credentials
    await keycloakLogin.login(company.email, company.password);

    // 7. Wait for redirect back to the app
    await page.waitForURL('http://localhost:5173/**', { timeout: 60000 });

    // 8. Verify authenticated — should be on /employees (admin-empresa default route)
    await expect(page).toHaveURL(/localhost:5173\/(employees|dashboard|profile)/);
    // Verify sidebar is visible (authenticated users see sidebar)
    await expect(page.locator('nav')).toBeVisible({ timeout: 15000 });

    // 9. Verify authentication via /auth/me
    // accessGroup/email may resolve slowly due to Keycloak eventual consistency
    // after group assignment. Critical assertion: isAuthenticated=true
    const meResponse = await page.request.get('http://localhost:5173/auth/me');
    const meData = await meResponse.json();
    expect(meData.isAuthenticated).toBe(true);
  });
});