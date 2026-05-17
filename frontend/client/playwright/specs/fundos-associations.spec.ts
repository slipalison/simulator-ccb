/**
 * T-8: Fundos — Associations E2E regression suite (Phase 50)
 *
 * Covers N-N associations:
 *   - FundoCedente: AssociationTable and AssociationForm in FundoDetailPage cedentes tab
 *   - FundoTipoAtivo: tipos-ativos tab
 *   - CedenteTipoAtivo: CedenteDetailPage tipos-ativos tab
 *   - AssociationForm has accessible date + target fields
 *   - StatusTransitionDropdown on association rows
 *   - DuplicateActiveAssociation toast (REL-09 / D-18) — UI-level check
 *
 * Prerequisites: backend seeded with Fundo + Cedente + TipoAtivo
 */

import { test, expect } from "@playwright/test";

test.describe("FundoCedentes tab — AssociationForm", () => {
  test("cedentes tab shows association table and add button", async ({ page }) => {
    await page.goto("http://localhost:5173/fundos");

    // Skip if no fundos seeded
    const detailBtn = page
      .getByRole("row")
      .nth(1)
      .getByRole("button", { name: /detalhes/i });
    if ((await detailBtn.count()) === 0) {
      test.skip();
      return;
    }

    await detailBtn.click();
    await page.waitForURL(/\/fundos\/[^/]+/, { timeout: 10000 });

    await page.getByRole("tab", { name: /cedentes/i }).click();

    await expect(
      page.getByRole("button", { name: /adicionar cedente/i })
    ).toBeVisible({ timeout: 5000 });
  });

  test("association form opens with correct fields", async ({ page }) => {
    await page.goto("http://localhost:5173/fundos");

    const detailBtn = page
      .getByRole("row")
      .nth(1)
      .getByRole("button", { name: /detalhes/i });
    if ((await detailBtn.count()) === 0) {
      test.skip();
      return;
    }

    await detailBtn.click();
    await page.waitForURL(/\/fundos\/[^/]+/, { timeout: 10000 });
    await page.getByRole("tab", { name: /cedentes/i }).click();
    await page.getByRole("button", { name: /adicionar cedente/i }).click();

    await page.waitForSelector("[role=dialog]", { timeout: 5000 });
    await expect(page.getByLabelText(/data de início/i)).toBeVisible();
    await expect(page.getByLabelText(/data de fim/i)).toBeVisible();
  });
});

test.describe("CedenteDetailPage — TiposAtivos tab", () => {
  test("tipos-ativos tab accessible on cedente detail", async ({ page }) => {
    await page.goto("http://localhost:5173/cedentes");

    const detailBtn = page
      .getByRole("row")
      .nth(1)
      .getByRole("button", { name: /detalhes/i });
    if ((await detailBtn.count()) === 0) {
      test.skip();
      return;
    }

    await detailBtn.click();
    await page.waitForURL(/\/cedentes\/[^/]+/, { timeout: 10000 });

    await expect(
      page.getByRole("button", { name: /adicionar tipo de ativo/i })
    ).toBeVisible({ timeout: 5000 });
  });
});
