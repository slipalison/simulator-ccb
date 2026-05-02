import { type Page, type Locator } from '@playwright/test';

/**
 * Page object for Keycloak's custom-themed login form.
 * Selectors match the onboarding-client theme (login.ftl).
 */
export class KeycloakLoginPage {
  readonly page: Page;
  readonly usernameInput: Locator;
  readonly passwordInput: Locator;
  readonly loginButton: Locator;

  constructor(page: Page) {
    this.page = page;
    // Keycloak theme uses these exact IDs (verified from login.ftl)
    this.usernameInput = page.locator('#username');
    this.passwordInput = page.locator('#password');
    this.loginButton = page.locator('#kc-login');
  }

  /**
   * Fill Keycloak login form and submit.
   * Used during ACF redirect after registration or for fresh login.
   */
  async login(email: string, password: string): Promise<void> {
    await this.usernameInput.fill(email);
    await this.passwordInput.fill(password);
    await this.loginButton.click();
  }
}