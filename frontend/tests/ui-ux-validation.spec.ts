// @ts-check
import { test, expect } from '@playwright/test';

test.describe('UI/UX Validation - Login and Registration', () => {
  test('LoginPage should have centered card with left-aligned labels', async ({ page }) => {
    await page.goto('http://localhost:5173/login');
    
    // Wait for form to load
    await expect(page.getByLabel('Email')).toBeVisible();
    
    // Take screenshot
    await page.screenshot({ 
      path: 'D:\\REPO\\keycloak-tests\\frontend\\screenshots\\login-page-after.png',
      fullPage: true 
    });
    
    // Verify card structure
    const card = page.locator('.rounded-xl.border.bg-white');
    await expect(card).toBeVisible();
    
    // Verify labels are left-aligned (not centered)
    const labels = page.locator('label');
    const count = await labels.count();
    for (let i = 0; i < count; i++) {
      const label = labels.nth(i);
      const classes = await label.getAttribute('class');
      expect(classes).toContain('text-left');
    }
    
    // Verify title and subtitle
    await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
    await expect(page.getByText('Entre com seu email')).toBeVisible();
    
    // Verify footer links
    await expect(page.getByRole('link', { name: 'Criar conta' })).toBeVisible();
  });

  test('RegistrationForm should have centered card with header/body/footer', async ({ page }) => {
    await page.goto('http://localhost:5173/register');
    
    // Wait for form to load
    await expect(page.getByLabel('Email')).toBeVisible();
    
    // Take screenshot
    await page.screenshot({ 
      path: 'D:\\REPO\\keycloak-tests\\frontend\\screenshots\\registration-form-after.png',
      fullPage: true 
    });
    
    // Verify card structure
    const card = page.locator('.rounded-xl.border.bg-white');
    await expect(card).toBeVisible();
    
    // Verify title and subtitle
    await expect(page.getByRole('heading', { name: 'Criar Conta' })).toBeVisible();
    await expect(page.getByText('Preencha seus dados')).toBeVisible();
    
    // Verify person type radio
    await expect(page.getByRole('radio', { name: 'Pessoa Física' })).toBeVisible();
    await expect(page.getByRole('radio', { name: 'Pessoa Jurídica' })).toBeVisible();
    
    // Verify submit button and login link in footer
    await expect(page.getByRole('button', { name: 'Criar conta' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Faca login' })).toBeVisible();
  });

  test('Forms should be responsive (mobile viewport)', async ({ page }) => {
    // Mobile viewport
    await page.setViewportSize({ width: 375, height: 812 });
    
    await page.goto('http://localhost:5173/login');
    await expect(page.getByLabel('Email')).toBeVisible();
    
    await page.screenshot({ 
      path: 'D:\\REPO\\keycloak-tests\\frontend\\screenshots\\login-mobile.png',
      fullPage: true 
    });
    
    // Card should still be visible and not overflow
    const card = page.locator('.rounded-xl.border.bg-white');
    const box = await card.boundingBox();
    expect(box).not.toBeNull();
    expect(box!.width).toBeLessThanOrEqual(375);
  });
});
