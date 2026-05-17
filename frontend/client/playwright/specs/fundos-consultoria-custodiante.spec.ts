/**
 * T-8: Fundos — Consultorias + Custodiantes E2E regression suite (Phase 50)
 *
 * Covers:
 *   - List pages render for both entities
 *   - Create dialogs accessible
 *   - No token in storage (D-12)
 */

import { test, expect } from "@playwright/test";

test.describe("ConsultoriasFundoListPage", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("http://localhost:5173/consultorias-fundo");
  });

  test("renders page heading", async ({ page }) => {
    await expect(
      page.getByRole("heading", { name: /consultorias/i })
    ).toBeVisible({ timeout: 10000 });
  });

  test("renders search input", async ({ page }) => {
    await expect(
      page.getByPlaceholder(/buscar por razão social ou cnpj/i)
    ).toBeVisible();
  });

  test("renders create button for admin-empresa user", async ({ page }) => {
    await expect(
      page.getByRole("button", { name: /nova consultoria/i })
    ).toBeVisible();
  });

  test("create dialog has CNPJ and razaoSocial fields", async ({ page }) => {
    await page.getByRole("button", { name: /nova consultoria/i }).click();
    await page.waitForSelector("[role=dialog]", { timeout: 5000 });
    await expect(page.getByLabel(/cnpj/i)).toBeVisible();
    await expect(page.getByLabel(/razão social/i)).toBeVisible();
  });
});

test.describe("CustodiantesListPage", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("http://localhost:5173/custodiantes");
  });

  test("renders page heading", async ({ page }) => {
    await expect(
      page.getByRole("heading", { name: /custodiantes/i })
    ).toBeVisible({ timeout: 10000 });
  });

  test("renders search input", async ({ page }) => {
    await expect(page.getByRole("searchbox")).toBeVisible();
  });

  test("renders create button", async ({ page }) => {
    await expect(
      page.getByRole("button", { name: /novo custodiante/i })
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
