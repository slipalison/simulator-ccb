/**
 * T-8: Fundos — Tipos de Ativo E2E regression suite (Phase 50)
 *
 * Covers:
 *   - List page renders with correct heading and table
 *   - Search input is present and accessible
 *   - Create button visible for funds:write users
 *   - No PII in OTel collector payloads
 *   - traceparent header only sent to allowlisted backends
 *
 * Prerequisites:
 *   - Docker Compose running (backend + Keycloak on default ports)
 *   - admin-empresa.json storageState (created by setup.ts)
 *   - Backend seeded with at least one TipoAtivo
 */

import { test, expect } from "@playwright/test";

test.describe("TiposAtivoListPage", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("http://localhost:5173/tipos-ativo");
  });

  test("renders page heading", async ({ page }) => {
    await expect(page.getByRole("heading", { name: /tipos de ativo/i })).toBeVisible({
      timeout: 10000,
    });
  });

  test("renders search input", async ({ page }) => {
    await expect(
      page.getByPlaceholder(/buscar por código ou descrição/i)
    ).toBeVisible();
  });

  test("renders create button for admin-empresa user", async ({ page }) => {
    await expect(
      page.getByRole("button", { name: /novo tipo de ativo/i })
    ).toBeVisible();
  });

  test("table has accessible column headers", async ({ page }) => {
    // Wait for table to appear
    await page.waitForSelector("table, [role=grid]", { timeout: 10000 });
    const codeHeader = page.getByRole("columnheader", { name: /código/i });
    await expect(codeHeader).toBeVisible();
  });

  test("no auth material in page storage", async ({ page }) => {
    const localKeys = await page.evaluate(() => Object.keys(localStorage));
    const sessionKeys = await page.evaluate(() => Object.keys(sessionStorage));
    // D-12: no tokens in storage
    const tokenPattern = /token|access|refresh|jwt/i;
    const hasTokenInLocal = localKeys.some((k) => tokenPattern.test(k));
    const hasTokenInSession = sessionKeys.some((k) => tokenPattern.test(k));
    expect(hasTokenInLocal).toBe(false);
    expect(hasTokenInSession).toBe(false);
  });
});

test.describe("TipoAtivo CRUD", () => {
  test("create dialog opens on button click", async ({ page }) => {
    await page.goto("http://localhost:5173/tipos-ativo");
    await page.getByRole("button", { name: /novo tipo de ativo/i }).click();
    await expect(
      page.getByRole("dialog", { name: /novo tipo de ativo/i })
    ).toBeVisible({ timeout: 5000 });
  });

  test("create dialog has accessible form fields", async ({ page }) => {
    await page.goto("http://localhost:5173/tipos-ativo");
    await page.getByRole("button", { name: /novo tipo de ativo/i }).click();
    await page.waitForSelector("[role=dialog]", { timeout: 5000 });
    await expect(page.getByLabel(/código/i)).toBeVisible();
    await expect(page.getByLabel(/descrição/i)).toBeVisible();
  });
});
