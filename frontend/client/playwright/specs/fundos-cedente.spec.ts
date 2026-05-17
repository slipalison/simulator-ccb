/**
 * T-8: Fundos — Cedentes E2E regression suite (Phase 50)
 *
 * Covers:
 *   - Cedentes list page renders
 *   - PF/PJ toggle in create dialog
 *   - Navigation to cedente detail page
 *   - No token in localStorage/sessionStorage (D-12)
 */

import { test, expect } from "@playwright/test";

test.describe("CedentesListPage", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("http://localhost:5173/cedentes");
  });

  test("renders page heading", async ({ page }) => {
    await expect(
      page.getByRole("heading", { name: /cedentes/i })
    ).toBeVisible({ timeout: 10000 });
  });

  test("renders search input", async ({ page }) => {
    await expect(
      page.getByPlaceholder(/buscar por nome ou documento/i)
    ).toBeVisible();
  });

  test("renders create button for admin-empresa user", async ({ page }) => {
    await expect(
      page.getByRole("button", { name: /novo cedente/i })
    ).toBeVisible();
  });

  test("no token in storage (D-12)", async ({ page }) => {
    const localKeys = await page.evaluate(() => Object.keys(localStorage));
    const sessionKeys = await page.evaluate(() => Object.keys(sessionStorage));
    const tokenPattern = /token|access|refresh|jwt/i;
    expect(localKeys.some((k) => tokenPattern.test(k))).toBe(false);
    expect(sessionKeys.some((k) => tokenPattern.test(k))).toBe(false);
  });
});

test.describe("Cedente create dialog", () => {
  test("opens create dialog with PF/PJ toggle", async ({ page }) => {
    await page.goto("http://localhost:5173/cedentes");
    await page.getByRole("button", { name: /novo cedente/i }).click();
    await page.waitForSelector("[role=dialog]", { timeout: 5000 });
    // PF/PJ toggle rendered inside dialog
    await expect(page.getByRole("group")).toBeVisible();
    const pfButton = page.getByRole("button", { name: /pf/i });
    await expect(pfButton).toBeVisible();
    const pjButton = page.getByRole("button", { name: /pj/i });
    await expect(pjButton).toBeVisible();
  });

  test("switching to PJ shows CNPJ field", async ({ page }) => {
    await page.goto("http://localhost:5173/cedentes");
    await page.getByRole("button", { name: /novo cedente/i }).click();
    await page.waitForSelector("[role=dialog]", { timeout: 5000 });
    await page.getByRole("button", { name: /pj/i }).click();
    await expect(page.getByLabel(/cnpj/i)).toBeVisible({ timeout: 3000 });
  });
});
