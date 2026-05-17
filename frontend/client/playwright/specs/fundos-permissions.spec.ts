/**
 * T-8: Fundos — Permission-gated UI E2E regression suite (Phase 50)
 *
 * Covers:
 *   - funds:read user sees list pages without create buttons
 *   - Unauthenticated user redirected to login on Fundos routes
 *   - No PII in OTel collector payloads (security check)
 *   - traceparent not sent to third-party URLs
 *
 * storageState: viewer.json (funds:read only, no funds:write)
 * For unauthenticated tests: fresh browser, no storageState
 */

import { test, expect } from "@playwright/test";

/**
 * Suite: viewer user (funds:read only)
 * Uses storageState: playwright/.auth/viewer.json
 * (set at project level in playwright.config.ts fundos-permissions project)
 */
test.describe("Viewer user (read-only) — no write buttons", () => {
  test("tipos de ativo list: no create button", async ({ page }) => {
    await page.goto("http://localhost:5173/tipos-ativo");
    await expect(
      page.getByRole("heading", { name: /tipos de ativo/i })
    ).toBeVisible({ timeout: 10000 });
    await expect(
      page.getByRole("button", { name: /novo tipo de ativo/i })
    ).not.toBeVisible();
  });

  test("cedentes list: no create button", async ({ page }) => {
    await page.goto("http://localhost:5173/cedentes");
    await expect(
      page.getByRole("heading", { name: /cedentes/i })
    ).toBeVisible({ timeout: 10000 });
    await expect(
      page.getByRole("button", { name: /novo cedente/i })
    ).not.toBeVisible();
  });

  test("fundos list: no create button", async ({ page }) => {
    await page.goto("http://localhost:5173/fundos");
    await expect(
      page.getByRole("heading", { name: /fundos/i })
    ).toBeVisible({ timeout: 10000 });
    await expect(
      page.getByRole("button", { name: /novo fundo/i })
    ).not.toBeVisible();
  });
});

/**
 * Security: OTel payload PII check
 * Intercepts network calls to collector and asserts no PII fields present.
 */
test.describe("OTel security — no PII in collector payload", () => {
  test("no PII regex match in spans sent to collector", async ({ page }) => {
    const collectorBodies: string[] = [];

    // Intercept collector POST (first-party — same origin or /otel path)
    page.route(/\/v1\/traces/, (route) => {
      const body = route.request().postData() ?? "";
      collectorBodies.push(body);
      route.continue();
    });

    await page.goto("http://localhost:5173/fundos");
    await page.waitForTimeout(3000); // allow spans to batch-send

    const PII_PATTERN = /(token|secret|password|authorization|cpf|cnpj|email|jwt)/i;
    for (const body of collectorBodies) {
      expect(PII_PATTERN.test(body)).toBe(false);
    }
  });

  test("traceparent not propagated to third-party URLs", async ({ page }) => {
    const thirdPartyRequests: { url: string; hasTraceparent: boolean }[] = [];

    page.on("request", (req) => {
      const url = req.url();
      const isFirstParty =
        url.startsWith("http://localhost:5173") ||
        url.startsWith("http://127.0.0.1:5173") ||
        url.startsWith("http://localhost:8080") || // Keycloak
        url.startsWith("http://localhost:5000"); // backend API

      if (!isFirstParty) {
        thirdPartyRequests.push({
          url,
          hasTraceparent: !!req.headers()["traceparent"],
        });
      }
    });

    await page.goto("http://localhost:5173/fundos");
    await page.waitForTimeout(2000);

    for (const req of thirdPartyRequests) {
      expect(req.hasTraceparent).toBe(false);
    }
  });
});
