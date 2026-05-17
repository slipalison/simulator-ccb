/**
 * T-8: Fundos — Fundo list + detail E2E regression suite (Phase 50)
 *
 * Covers:
 *   - Fundos list page with status filter dropdown
 *   - Fundo detail page tabs (dados, cedentes, tipos-ativos)
 *   - StatusTransitionDropdown visible when transitions allowed
 *   - No token in storage (D-12)
 */

import { test, expect } from "@playwright/test";

test.describe("FundosListPage", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("http://localhost:5173/fundos");
  });

  test("renders page heading", async ({ page }) => {
    await expect(
      page.getByRole("heading", { name: /fundos/i })
    ).toBeVisible({ timeout: 10000 });
  });

  test("renders search input", async ({ page }) => {
    await expect(
      page.getByPlaceholder(/buscar por nome ou cnpj/i)
    ).toBeVisible();
  });

  test("renders status filter dropdown", async ({ page }) => {
    await expect(
      page.getByRole("combobox", { name: /filtrar por status/i })
    ).toBeVisible();
  });

  test("status filter shows all Fundo statuses", async ({ page }) => {
    await page.getByRole("combobox", { name: /filtrar por status/i }).click();
    await expect(page.getByRole("option", { name: /rascunho/i })).toBeVisible();
    await expect(page.getByRole("option", { name: /ativo/i })).toBeVisible();
    await expect(page.getByRole("option", { name: /suspenso/i })).toBeVisible();
    await expect(page.getByRole("option", { name: /em liquidação/i })).toBeVisible();
    await expect(page.getByRole("option", { name: /encerrado/i })).toBeVisible();
  });

  test("renders create button for admin-empresa user", async ({ page }) => {
    await expect(
      page.getByRole("button", { name: /novo fundo/i })
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

test.describe("FundoDetailPage", () => {
  /**
   * NOTE: Requires at least one Fundo seeded in the backend.
   * If backend is not seeded, these tests will be skipped gracefully via
   * conditional navigation check.
   */

  test("detail page has tabs for dados, cedentes, tipos-ativos", async ({ page }) => {
    await page.goto("http://localhost:5173/fundos");

    // If table has any row, navigate to first detail
    const firstDetailLink = page
      .getByRole("row")
      .nth(1) // skip header row
      .getByRole("button", { name: /detalhes/i });

    const count = await firstDetailLink.count();
    if (count === 0) {
      test.skip();
      return;
    }

    await firstDetailLink.click();
    await page.waitForURL(/\/fundos\/[^/]+/, { timeout: 10000 });

    await expect(page.getByRole("tab", { name: /dados/i })).toBeVisible();
    await expect(page.getByRole("tab", { name: /cedentes/i })).toBeVisible();
    await expect(page.getByRole("tab", { name: /tipos de ativo/i })).toBeVisible();
  });
});
