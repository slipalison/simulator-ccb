import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Page object for the Profile page (/profile).
 */
export class ProfilePage {
  readonly page: Page;
  readonly container: Locator;

  constructor(page: Page) {
    this.page = page;
    // Profile page may have a data-testid or just identifiable by URL
    this.container = page.locator('[data-testid="profile-container"]');
  }

  /** Navigate to the profile page */
  async goto(): Promise<void> {
    await this.page.goto('/profile');
    // Fallback verification — check URL if no data-testid
    await expect(this.page).toHaveURL(/\/profile/);
  }
}