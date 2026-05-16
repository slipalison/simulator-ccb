/**
 * T-7c: Backoffice SPA admin-auth-flow regression suite (Phase 49 — auth-flow-fix)
 *
 * Covers scenarios 3, 4, 5, 6 from the 8-scenario matrix distributed
 * across client (auth-flow.spec.ts) and backoffice (this file).
 *
 * Test users created by scripts/seed-test-users.sh (invoked in globalSetup):
 *   e2e-admin@example.com / E2EAdmin@123! → backoffice realm, role=admin
 *
 * Security assertions (D-12, D-15):
 *   - code_challenge_method=S256 verified on the authorize URL.
 *   - localStorage.length === 0 && sessionStorage.length === 0 post-login.
 *
 * Post-login target: /admin/companies (backoffice auth-server.ts:249)
 *
 * Login flow:
 *   1. Visit /admin/login (AdminLoginPage component).
 *   2. Click "Entrar" button (data-testid="admin-login-button").
 *      This triggers window.location.href = "/auth/login" (server-side route).
 *   3. Server /auth/login → 302 → Keycloak authorize URL (PKCE S256).
 *   4. Fill credentials on Keycloak login page.
 *   5. Keycloak → 302 → /auth/callback (server) → token exchange → 302 → /admin/companies.
 */

import { test, expect } from '@playwright/test';

const ADMIN_EMAIL = 'e2e-admin@example.com';
const ADMIN_PASSWORD = 'E2EAdmin@123!';
const BASE_URL = 'http://localhost:5174';

// ── Helpers ───────────────────────────────────────────────────────────────────

/**
 * Perform a full ACF+PKCE login via the backoffice AdminLoginPage.
 * Returns the authorize URL intercepted during the flow so the caller can
 * assert PKCE parameters (code_challenge_method=S256).
 */
async function doAdminLogin(
  page: import('@playwright/test').Page,
  email: string = ADMIN_EMAIL,
  password: string = ADMIN_PASSWORD,
): Promise<string> {
  let capturedAuthorizeUrl = '';

  // Intercept the Keycloak authorize URL to capture PKCE params
  page.on('request', (request) => {
    const url = request.url();
    if (url.includes('/realms/') && url.includes('/protocol/openid-connect/auth')) {
      if (!capturedAuthorizeUrl) {
        capturedAuthorizeUrl = url;
      }
    }
  });

  // Navigate to the admin login page
  await page.goto(`${BASE_URL}/admin/login`, { waitUntil: 'domcontentloaded' });

  // Wait for and click the "Entrar" button (data-testid="admin-login-button")
  // This triggers window.location.href = "/auth/login" on the server-side route
  const loginButton = page.locator('[data-testid="admin-login-button"]');
  await expect(loginButton).toBeVisible({ timeout: 10000 });
  await loginButton.click();

  // Wait for Keycloak login page to load
  // The Keycloak custom theme uses #username, #password, #kc-login
  await expect(page.locator('#username')).toBeVisible({ timeout: 30000 });

  // Fill credentials and submit
  await page.locator('#username').fill(email);
  await page.locator('#password').fill(password);
  await page.locator('#kc-login').click();

  // Wait for post-login redirect to /admin/companies
  await page.waitForURL(`${BASE_URL}/admin/companies`, { timeout: 60000 });

  return capturedAuthorizeUrl;
}

// ── Scenario 3: Backoffice login happy path ───────────────────────────────────

test('Scenario 3 — backoffice login happy path: redirect to /admin/companies, PKCE S256, no storage', async ({
  page,
}) => {
  const authorizeUrl = await doAdminLogin(page);

  // Assert: final URL is /admin/companies
  await expect(page).toHaveURL(/\/admin\/companies/, { timeout: 5000 });

  // Assert PKCE S256 (D-15)
  expect(authorizeUrl).toContain('code_challenge_method=S256');
  expect(authorizeUrl).toMatch(/code_challenge=[A-Za-z0-9_-]{43}/);

  // Assert: no tokens in localStorage / sessionStorage (D-12)
  const storage = await page.evaluate(() => ({
    ls: localStorage.length,
    ss: sessionStorage.length,
  }));
  expect(storage.ls).toBe(0);
  expect(storage.ss).toBe(0);
});

// ── Scenario 4: Backoffice logout ─────────────────────────────────────────────

test('Scenario 4 — backoffice logout: clears session, /auth/me returns 401', async ({ page }) => {
  await doAdminLogin(page);
  await expect(page).toHaveURL(/\/admin\/companies/, { timeout: 5000 });

  // Trigger logout via the server-side route
  await page.goto(`${BASE_URL}/auth/logout`, { waitUntil: 'commit' });

  // After logout Keycloak's end_session_endpoint fires a 302 to post_logout_redirect_uri
  // which is http://localhost:5174/auth/login (backoffice auth-server.ts:269)
  await page.waitForURL(`${BASE_URL}/auth/login`, { timeout: 30000 });
  await expect(page).toHaveURL(/\/auth\/login/);

  // Assert /auth/me returns 401 immediately after logout
  const meResp = await page.request.get(`${BASE_URL}/auth/me`);
  expect(meResp.status()).toBe(401);
});

// ── Scenario 5 (backoffice): Post-login race — no transient /admin/login URL ──

test('Scenario 5 (backoffice) — post-login race: no transient /admin/login URL between callback and /admin/companies', async ({
  page,
}) => {
  // Collect all main-frame navigation URLs during the ACF redirect chain.
  // A regression would show /admin/login appearing AFTER the callback redirect
  // but BEFORE /admin/companies stabilizes — that's the flash the original bug caused.
  const visitedUrls: string[] = [];

  page.on('framenavigated', (frame) => {
    if (frame === page.mainFrame()) {
      visitedUrls.push(frame.url());
    }
  });

  await doAdminLogin(page);
  await expect(page).toHaveURL(/\/admin\/companies/, { timeout: 5000 });

  // Find the callback index in the visited URL list
  const callbackIndex = visitedUrls.findIndex((u) => u.includes('/auth/callback'));

  // After the callback the server redirects → /admin/companies.
  // Assert that no navigation URL between the callback and /admin/companies is a login page.
  const loginAfterCallback = visitedUrls
    .slice(callbackIndex + 1)
    .filter((u) => {
      try {
        const pathname = new URL(u).pathname;
        return /\/admin\/login/.test(pathname) || /\/auth\/login/.test(pathname);
      } catch {
        return false;
      }
    });

  expect(loginAfterCallback).toHaveLength(0);
});

// ── Scenario 6 (backoffice): Refresh resilience — reload stays authenticated ──

test('Scenario 6 (backoffice) — refresh resilience: reload after login stays on /admin/companies', async ({
  page,
}) => {
  await doAdminLogin(page);
  await expect(page).toHaveURL(/\/admin\/companies/, { timeout: 5000 });

  // Reload the page — the SPA must restore auth state from httpOnly cookies
  // via /auth/me (admin-auth-context.tsx tryRestore with 401 retry) without
  // redirecting to login.
  await page.reload({ waitUntil: 'load' });

  // Allow time for tryRestore + single 401-retry (200ms backoff) to complete.
  // AdminLayout renders a loading shell during isLoading=true (T-6a).
  await page.waitForTimeout(2000);

  // Assert still on /admin/companies, not redirected to login
  await expect(page).not.toHaveURL(/\/(admin\/)?login/, { timeout: 10000 });
  await expect(page).toHaveURL(/\/admin\/companies/, { timeout: 10000 });

  // Assert loading shell is NOT visible (auth resolved)
  const loadingShell = page.locator('[data-testid="admin-loading-shell"]');
  await expect(loadingShell).not.toBeVisible({ timeout: 5000 });
});
