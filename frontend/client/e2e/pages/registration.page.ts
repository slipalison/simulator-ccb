import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Page object for the 2-step PJ registration wizard (/register).
 * Selectors match RegistrationForm.tsx component fields.
 */
export class RegistrationPage {
  readonly page: Page;

  // Step 1: Company data
  readonly razaoSocialInput: Locator;
  readonly cnpjInput: Locator;
  readonly continueButton: Locator;

  // Step 2: Access data
  readonly emailInput: Locator;
  readonly phoneInput: Locator;
  readonly passwordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly termsCheckbox: Locator;
  readonly submitButton: Locator;

  constructor(page: Page) {
    this.page = page;

    // Step 1 selectors from RegistrationForm.tsx
    this.razaoSocialInput = page.getByPlaceholder('Nome da empresa');
    this.cnpjInput = page.getByPlaceholder('00.000.000/0000-00');
    this.continueButton = page.getByRole('button', { name: /Continuar/ });

    // Step 2 selectors from RegistrationForm.tsx
    this.emailInput = page.getByPlaceholder('seu@email.com');
    this.phoneInput = page.getByPlaceholder('(00) 00000-0000');
    this.passwordInput = page.locator('#password');
    this.confirmPasswordInput = page.locator('#confirmPassword');
    this.termsCheckbox = page.getByRole('checkbox');
    this.submitButton = page.getByRole('button', { name: /Cadastrar/ });
  }

  /** Navigate to the registration page */
  async goto(): Promise<void> {
    await this.page.goto('/register');
    await expect(this.razaoSocialInput).toBeVisible();
  }

  /** Fill step 1 (company data) and click Continue */
  async fillCompanyData(razaoSocial: string, cnpj: string): Promise<void> {
    await this.razaoSocialInput.fill(razaoSocial);
    await this.cnpjInput.fill(cnpj);
    await this.continueButton.click();
  }

  /** Fill step 2 (access data) fields and check terms checkbox */
  async fillAccessData(email: string, phone: string, password: string): Promise<void> {
    await this.emailInput.fill(email);
    await this.phoneInput.fill(phone);
    await this.passwordInput.fill(password);
    await this.confirmPasswordInput.fill(password);
    await this.termsCheckbox.check();
  }

  /** Click the Cadastrar submit button */
  async submit(): Promise<void> {
    await this.submitButton.click();
  }
}