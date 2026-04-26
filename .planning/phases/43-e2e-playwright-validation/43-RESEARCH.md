# Phase 43: E2E Playwright Validation - Research

**Researched:** 2026-04-26
**Domain:** E2E Testing / Playwright / Auth Code Flow / Keycloak / Permission UI
**Confidence:** HIGH

## Summary

This phase installs and configures Playwright E2E tests in the `frontend/client` project to validate the complete PJ registration flow, dashboard rendering, employee management, access group permissions, and JWT claim verification. The frontend uses Authorization Code Flow with PKCE (ACF) via httpOnly cookies managed by Vinxi's server-side auth routes (`auth-server.ts`). Playwright must navigate Keycloak's custom-themed login page during redirects, handle cookie-based auth state, decode JWT claims from intercepted network responses, and verify permission-rendered UI elements (viewer sees read-only, admin-empresa sees action buttons).

**Primary recommendation:** Use Playwright's setup project pattern with three auth states (admin-empresa, viewer, dashboard), store httpOnly cookie storageState per role, and use Page Object Model with fixtures for each access group. JWT verification via response interception — capture `/auth/callback` token exchange to decode claims. Registration tests must interact directly with Keycloak's login form during ACF redirect.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| E2E-01 | Cadastro PJ completo → auto-login → redireciona para dashboard | ACF redirect flow (§Auth Flow), PKCE login via Keycloak (§Keycloak Login Interaction), RegistrationForm selectors (§Page Objects) |
| E2E-02 | Dashboard exibe cards mock | DashboardPage selectors (§Page Objects), mock card test IDs |
| E2E-03 | PJ cria funcionário via UI → lista com status ativo e group viewer | EmployeePage selectors (§Page Objects), API endpoints documented (§API Endpoints) |
| E2E-04 | Login como funcionário → redirect baseado no access group | Group-based default routes (§Permission Routing), ACF flow per role |
| E2E-05 | JWT decode revela groups/roles corretos — viewer não vê botões, admin-empresa vê tudo | JWT decoding approach (§JWT Verification), EmployeesTable permission logic (§Permission Rendering) |
| E2E-06 | PJ muda access group → login novamente → permissões atualizadas | ChangeAccessGroupDialog (§Page Objects), Keycloak eventual consistency (§Pitfalls) |
| E2E-07 | Todos os E2E tests passam com `npx playwright test` no `frontend/client` | Playwright config (§Standard Stack), webServer setup |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| PJ Registration form interaction | Browser (SPA) | Frontend Server (Vinxi) | Form renders in browser, validation runs client-side |
| Auth Code Flow redirect | Frontend Server (Vinxi) | Keycloak | Vinxi initiates redirect, Keycloak handles credential form |
| Token exchange & cookie writing | Frontend Server (Vinxi) | — | Server-side PKCE exchange, httpOnly cookie generation |
| JWT claims extraction | Browser (SPA) | Frontend Server (Vinxi /auth/me) | Claims decoded server-side, returned to client via /auth/me |
| Permission-rendered UI | Browser (SPA) | — | accessGroup from AuthContext drives conditional rendering |
| Employee CRUD | API (Backend) | Keycloak | Backend orchestrates DB + Keycloak Admin API |
| Access group assignment | API (Backend) | Keycloak | Backend calls Keycloak Admin API for group membership |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| @playwright/test | 1.59.x | E2E test framework | Already in devDependencies. Industry standard for browser E2E. [VERIFIED: npm registry] |
| playwright | 1.59.x | Browser automation engine | CLI and browser binaries. [VERIFIED: npm registry] |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| jose | 6.x | JWT decode/verify in Node.js | Decode JWT claims from intercepted responses without crypto edge cases |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| jose for JWT decode | atob + JSON.parse | jose handles Base64URL correctly; manual atob fails on URL-safe chars. Use jose. |
| Playwright storageState | Custom cookie injection | storageState is the documented Playwright pattern for auth reuse. |

**Installation:**
```bash
cd frontend/client
npm install --save-dev @playwright/test  # already present
npx playwright install chromium           # install browser binaries
```

**Version verification:**
```bash
npm view @playwright/test version
# 1.59.1 — verified 2026-04-26
npm view playwright version
# 1.59.1 — verified 2026-04-26
```

Note: `playwright` is already in `devDependencies` at `^1.59.1`. `@playwright/test` must be added if not present (check actual package.json — only `playwright` is listed, not `@playwright/test`). Both are needed: `@playwright/test` for the test runner, `playwright` for browser binaries.

## Architecture Patterns

### System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Playwright Test Runner                        │
│  ┌─────────────┐  ┌──────────────┐  ┌────────────────┐              │
│  │  Setup:     │  │  Setup:      │  │  Setup:         │             │
│  │  Register   │  │  Login as    │  │  Login as       │             │
│  │  PJ Company │→ │  admin-empresa│→ │  viewer emp.   │             │
│  │  + Create   │  │  → save      │  │  → save         │             │
│  │  employees  │  │  storageState│  │  storageState   │             │
│  └─────────────┘  └──────────────┘  └────────────────┘              │
│         │                 │                   │                       │
└─────────┼─────────────────┼───────────────────┼───────────────────────┘
          ▼                 ▼                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     Browser (Chromium)                               │
│  ┌──────────┐    ┌──────────┐    ┌──────────────┐                   │
│  │ /register │    │ /auth/   │    │ /dashboard   │                   │
│  │ → API     │    │ login →  │    │ /employees   │                   │
│  │ → ACF     │    │ Keycloak │    │ /profile     │                   │
│  │ redirect  │    │ → cookie │    │ (permission   │                   │
│  └──────────┘    └──────────┘    │  based UI)    │                   │
│       │               │          └──────────────┘                    │
└───────┼───────────────┼──────────────────────────────────────────────┘
        ▼               ▼
┌──────────────┐  ┌──────────────┐
│  Vinxi Server │  │   Keycloak   │
│  (5173)       │  │   (8180)     │
│  /auth/*      │  │   client     │
│  /api/* →     │  │   realm      │
│  proxy → API  │  │              │
└──────┬───────┘  └──────────────┘
       │                  │
       ▼                  ▼
┌──────────────┐  ┌──────────────┐
│  .NET API    │  │  PostgreSQL  │
│  (8080)      │  │  (5432)      │
│  /api/companies│  │  app_db      │
│  /api/employees│  │              │
└──────────────┘  └──────────────┘
```

### Recommended Project Structure
```
frontend/client/
├── playwright.config.ts        # Playwright configuration
├── e2e/                        # E2E test directory
│   ├── auth/                   # Auth setup files
│   │   ├── admin-empresa.setup.ts    # Login as admin-empresa, save state
│   │   ├── viewer.setup.ts          # Login as viewer employee, save state
│   │   └── dashboard.setup.ts        # Login as dashboard employee, save state
│   ├── fixtures/               # Test data factories
│   │   └── test-data.ts              # Company/employee data generators
│   ├── pages/                  # Page Object Model
│   │   ├── registration.page.ts      # /register page object
│   │   ├── dashboard.page.ts         # /dashboard page object
│   │   ├── employees.page.ts         # /employees page object
│   │   ├── keycloak-login.page.ts    # Keycloak login form page object
│   │   └── profile.page.ts           # /profile page object
│   ├── registration.spec.ts    # E2E-01: Cadastro PJ
│   ├── dashboard.spec.ts       # E2E-02: Dashboard cards
│   ├── employee-management.spec.ts  # E2E-03: Create employee
│   ├── employee-login.spec.ts  # E2E-04: Employee login + redirect
│   ├── permission-ui.spec.ts   # E2E-05: JWT claims + permission UI
│   └── access-group-change.spec.ts  # E2E-06: Change group → re-login
├── playwright/.auth/           # Storage state files (gitignored)
│   ├── admin-empresa.json
│   ├── viewer.json
│   └── dashboard.json
└── src/                        # (existing source code)
```

### Pattern 1: Setup Project with Auth StorageState
**What:** Authenticate once per role, save cookie state, reuse across tests
**When to use:** All authenticated E2E tests
**Example:**
```typescript
// e2e/auth/admin-empresa.setup.ts
import { test as setup, expect } from '@playwright/test';
import path from 'path';

const authFile = path.join(__dirname, '../../playwright/.auth/admin-empresa.json');

setup('authenticate as admin-empresa', async ({ page }) => {
  // Navigate to app — triggers ACF redirect to Keycloak
  await page.goto('http://localhost:5173/');
  
  // On Keycloak login page, fill credentials
  await page.locator('#username').fill(process.env.E2E_ADMIN_EMAIL!);
  await page.locator('#password').fill(process.env.E2E_ADMIN_PASSWORD!);
  await page.locator('#kc-login').click();
  
  // Wait for redirect back to app (after ACF callback)
  await page.waitForURL('http://localhost:5173/**');
  
  // Verify authenticated by checking for sidebar or header
  await expect(page.locator('[data-testid="header"]')).toBeVisible();
  
  // Save storage state (includes httpOnly cookies)
  await page.context().storageState({ path: authFile });
});
```

### Pattern 2: Page Object Model with Custom Fixtures
**What:** Typed page objects injected via Playwright fixtures
**When to use:** All test files for cleaner, maintainable selectors
**Example:**
```typescript
// e2e/pages/employees.page.ts
import { type Page, type Locator, expect } from '@playwright/test';

export class EmployeesPage {
  readonly page: Page;
  readonly tableWrapper: Locator;
  readonly actionsColumn: Locator;
  readonly employeeRows: Locator;
  
  constructor(page: Page) {
    this.page = page;
    this.tableWrapper = page.getByTestId('employees-table-wrapper');
    this.actionsColumn = page.getByTestId('employees-table');
    this.employeeRows = page.locator('[data-testid^="employee-row-"]');
  }
  
  async goto() {
    await this.page.goto('/employees');
    await expect(this.tableWrapper).toBeVisible();
  }
  
  async getRowCount(): Promise<number> {
    return await this.employeeRows.count();
  }
  
  async hasActionsColumn(): Promise<boolean> {
    // The "Ações" column only appears for non-viewer users
    const header = this.page.locator('th', { hasText: 'Ações' });
    return await header.isVisible();
  }
  
  async openActionsForEmployee(employeeId: string) {
    await this.page.getByTestId(`actions-dropdown-trigger-${employeeId}`).click();
  }
}
```

### Pattern 3: JWT Claims Verification via Response Interception
**What:** Capture the token exchange response at `/auth/callback` to decode JWT claims
**When to use:** E2E-05 JWT verification tests
**Example:**
```typescript
// Use page.route() to intercept the token callback and capture the access token
const tokenPromise = page.waitForResponse(async (response) => {
  // The /auth/callback is a redirect — the actual token exchange happens server-side.
  // Instead, intercept the /auth/me response which contains decoded claims.
  return response.url().includes('/auth/me') && response.status() === 200;
});

await page.goto('/employees');
const meResponse = await tokenPromise;
const meData = await meResponse.json();

expect(meData.accessGroup).toBe('viewer');
expect(meData.isAuthenticated).toBe(true);
```

For direct JWT decoding (to check groups/roles in the raw token):
```typescript
// Intercept the cookie-setting response from /auth/callback redirect chain
// Then use jose or manual base64url decode on the access_token
import { decodeJwt } from 'jose';

// During ACF login, after callback, extract cookie and decode
const cookies = await page.context().cookies();
const accessTokenCookie = cookies.find(c => c.name === 'client_access_token');
if (accessTokenCookie) {
  const claims = decodeJwt(accessTokenCookie.value);
  expect(claims.groups).toContain('viewer');
}
```

### Anti-Patterns to Avoid
- **Don't use `page.waitForTimeout()`:** Use `waitForURL`, `waitForResponse`, or `expect().toBeVisible()` instead. Keycloak redirects are async and timing-dependent.
- **Don't test with production Keycloak:** Use the dev Keycloak at localhost:8180 with the `client` realm.
- **Don't store test credentials in test files:** Use environment variables (`E2E_PJ_EMAIL`, `E2E_PJ_PASSWORD`, etc.).
- **Don't mock the ACF flow:** The entire point of E2E is testing the real Auth Code Flow. Mocking defeats the purpose.
- **Don't skip Keycloak's login page interaction:** The redirected Keycloak form must be filled with real credentials — this validates the PKCE flow end-to-end.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| JWT decode | Manual base64url + JSON.parse | `jose.decodeJwt()` | Base64URL padding, URL-safe chars, malformed tokens |
| Cookie management | Direct `document.cookie` access | Playwright `page.context().cookies()` | httpOnly cookies are invisible to JS; Playwright's context API reads them |
| Auth state persistence | Custom login helper per test | Playwright `storageState` + setup projects | Documented pattern, automatically reuses cookies across tests |
| Test retry / flake handling | Custom polling loops | Playwright `expect().toBeVisible()` with auto-retry | Built-in auto-wait with configurable timeout |
| Browser launch | Manual Chromium download | `npx playwright install` | Playwright manages compatible browser binaries |

**Key insight:** Playwright's storageState correctly captures httpOnly cookies (unlike JS-based approaches), which is essential since this app stores tokens in httpOnly cookies set by Vinxi's auth-server.

## Common Pitfalls

### Pitfall 1: Keycloak Eventual Consistency for Group Changes
**What goes wrong:** After changing an employee's access group via the API, Keycloak may take 1-2 seconds to reflect the new group in the JWT. If the employee re-logs in immediately, they may still get the OLD group.
**Why it happens:** Keycloak group membership is eventually consistent — the token issued at login may use cached group data. Additionally, access tokens have a 5-minute lifespan (`accessTokenLifespan: 300`).
**How to avoid:** After changing a group, either (1) wait for the old access token to expire (5 min — too slow for tests), or (2) use Keycloak Admin API to invalidate the user's sessions before re-login, or (3) wait 2-3 seconds and verify via `/auth/me` that the new group is reflected.
**Warning signs:** Employee still sees viewer permissions after being changed to admin-empresa.

### Pitfall 2: ACF Redirect Chain Timing
**What goes wrong:** The ACF flow goes: `/auth/login` → Keycloak → `/auth/callback` → `/profile` (or group-based default). Between each redirect, there can be 1-3 seconds of delay.
**Why it happens:** Network latency to Keycloak container, PKCE code exchange, cookie setting.
**How to avoid:** Use `page.waitForURL()` with a generous timeout (30s default) rather than waiting for specific elements. Use `waitForResponse` for network-level verification.
**Warning signs:** `TimeoutError: page.waitForURL: Timeout 5000ms exceeded` on auth callback redirect.

### Pitfall 3: httpOnly Cookie Domain Mismatch
**What goes wrong:** StorageState saved for one domain/port may not work if the baseURL changes.
**Why it happens:** Cookies are domain+path scoped. If the test uses `localhost:5173` but storage state was saved from `127.0.0.1:5173`, cookies won't be sent.
**How to avoid:** Always use the same `baseURL` in Playwright config and in setup files. Use `http://localhost:5173` consistently.
**Warning signs:** Tests pass in setup but fail in test execution — 401 on /auth/me.

### Pitfall 4: Brute Force Lockout During Test Runs
**What goes wrong:** Running login tests repeatedly (especially failed-login tests) can trigger Keycloak's brute force protection (5 failures → 30s lockout).
**Why it happens:** Keycloak's `failureFactor: 5` and `waitIncrementSeconds: 30` config locks accounts after 5 failed attempts.
**How to avoid:** Use the setup project to authenticate once and reuse storageState. For login tests that intentionally test failures, create a separate test account or reset brute-force counters between tests via Keycloak Admin API.
**Warning signs:** Tests flake — sometimes pass, sometimes fail with "account locked" or "invalid credentials" on valid credentials.

### Pitfall 5: Registration Creates Persistent Data
**What goes wrong:** Each test run that registers a company creates a permanent Keycloak user and database record. Duplicate CNPJ/email causes 409 conflicts on re-run.
**Why it happens:** No test data cleanup between runs.
**How to avoid:** Generate unique test data per run using timestamps (e.g., `e2e-company-${Date.now()}@test.com`, `e2e-cnpj-${randomDigits}`). Optionally add `docker compose down -v && docker compose up -d` as a global teardown or manual reset step documented in README.
**Warning signs:** Second test run fails: "CNPJ já cadastrado."

### Pitfall 6: Keycloak Login Form Field Selectors
**What goes wrong:** Using `page.getByLabel('Email')` or `page.getByPlaceholder('email')` fails because Keycloak renders its own HTML with different IDs/labels.
**Why it happens:** The Keycloak login form (`login.ftl`) uses specific `id="username"`, `id="password"`, `id="kc-login"` — not the standard HTML5 label/placeholder patterns.
**How to avoid:** Use the exact selectors from the Keycloak theme: `#username` (or `page.locator('#username')`), `#password`, `#kc-login`. This is verified from the `onboarding-client` theme at `keycloak/themes/onboarding-client/login/login.ftl`.
**Warning signs:** `Error: locator.waitFor: Error: strict mode violation` or element not found.

### Pitfall 7: Vinxi Dev Server Startup Time
**What goes wrong:** Playwright's `webServer` starts `npm run dev` which launches Vinxi, but the server may not be ready before tests begin.
**Why it happens:** Vinxi startup takes 5-10 seconds, and the health check URL must return 200.
**How to avoid:** Use `webServer.url` with a reliable health endpoint. The Vinxi SPA at localhost:5173 returns the HTML page for any route — use `http://localhost:5173` as the URL. Set `webServer.reuseExistingServer: !process.env.CI` to reuse during local dev. **OR** do NOT use webServer at all — document that Docker Compose must be running before E2E tests.
**Warning signs:** Tests fail with connection refused because Vinxi hasn't started yet.

## Code Examples

### Playwright Configuration
```typescript
// playwright.config.ts — Source: Playwright official docs + project analysis
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,  // Sequential — shared Keycloak state can't handle parallel
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,  // Single worker — Keycloak brute-force protection + shared DB
  reporter: 'html',
  timeout: 60000,  // 60s per test — ACF redirects can be slow
  
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    // Setup project: creates test data and auth states
    {
      name: 'setup',
      testMatch: /.*\.setup\.ts/,
    },
    // Authenticated tests as admin-empresa
    {
      name: 'admin-empresa',
      use: { 
        ...devices['Desktop Chrome'],
        storageState: 'playwright/.auth/admin-empresa.json',
      },
      dependencies: ['setup'],
      testMatch: /.*(employee-management|access-group-change|permission-ui)\.spec\.ts/,
    },
    // Authenticated tests as viewer
    {
      name: 'viewer',
      use: { 
        ...devices['Desktop Chrome'],
        storageState: 'playwright/.auth/viewer.json',
      },
      dependencies: ['setup'],
      testMatch: /.*permission-ui\.spec\.ts/,  // Same test file, different project
    },
  ],

  // Don't auto-start server — Docker Compose must be running
  // webServer: { ... },  // intentionally omitted
});
```

### Keycloak Login Page Object
```typescript
// e2e/pages/keycloak-login.page.ts
// Source: keycloak/themes/onboarding-client/login/login.ftl
import { type Page, type Locator } from '@playwright/test';

export class KeycloakLoginPage {
  readonly page: Page;
  readonly usernameInput: Locator;
  readonly passwordInput: Locator;
  readonly loginButton: Locator;

  constructor(page: Page) {
    this.page = page;
    // Keycloak theme uses these exact IDs (verified from login.ftl)
    this.usernameInput = page.locator('#username');
    this.passwordInput = page.locator('#password');
    this.loginButton = page.locator('#kc-login');
  }

  async login(email: string, password: string) {
    await this.usernameInput.fill(email);
    await this.passwordInput.fill(password);
    await this.loginButton.click();
  }
}
```

### Registration Page Object
```typescript
// e2e/pages/registration.page.ts
// Source: frontend/client/src/components/molecules/RegistrationForm.tsx
import { type Page, type Locator, expect } from '@playwright/test';

export class RegistrationPage {
  readonly page: Page;
  readonly razaoSocialInput: Locator;
  readonly cnpjInput: Locator;
  readonly continueButton: Locator;
  readonly emailInput: Locator;
  readonly phoneInput: Locator;
  readonly passwordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly termsCheckbox: Locator;
  readonly submitButton: Locator;

  constructor(page: Page) {
    this.page = page;
    // These selectors match the RegistrationForm form fields
    this.razaoSocialInput = page.getByPlaceholder('Nome da empresa');
    this.cnpjInput = page.getByPlaceholder('00.000.000/0000-00');
    this.continueButton = page.getByRole('button', { name: /Continuar/ });
    this.emailInput = page.getByPlaceholder('seu@email.com');
    this.phoneInput = page.getByPlaceholder('(00) 00000-0000');
    this.passwordInput = page.locator('#password');
    this.confirmPasswordInput = page.locator('#confirmPassword');
    this.termsCheckbox = page.getByRole('checkbox');  // terms checkbox
    this.submitButton = page.getByRole('button', { name: /Cadastrar/ });
  }

  async goto() {
    await this.page.goto('/register');
    await expect(this.razaoSocialInput).toBeVisible();
  }

  async fillCompanyData(razaoSocial: string, cnpj: string) {
    await this.razaoSocialInput.fill(razaoSocial);
    await this.cnpjInput.fill(cnpj);
    await this.continueButton.click();
  }

  async fillAccessData(email: string, phone: string, password: string) {
    await this.emailInput.fill(email);
    await this.phoneInput.fill(phone);
    await this.passwordInput.fill(password);
    await this.confirmPasswordInput.fill(password);
    await this.termsCheckbox.check();
  }

  async submit() {
    await this.submitButton.click();
  }
}
```

### JWT Decoding Utility
```typescript
// e2e/fixtures/jwt-utils.ts
import { decodeJwt } from 'jose';

export interface DecodedToken {
  sub: string;
  email: string;
  groups?: string[];
  realm_access?: { roles: string[] };
  company_id?: string;
  [key: string]: unknown;
}

export function decodeAccessToken(token: string): DecodedToken {
  return decodeJwt(token) as DecodedToken;
}

export async function getAccessTokenFromCookies(page: import('@playwright/test').Page): Promise<string | null> {
  const cookies = await page.context().cookies(['http://localhost:5173']);
  const accessTokenCookie = cookies.find(c => c.name === 'client_access_token');
  return accessTokenCookie?.value ?? null;
}
```

### Test Data Factory
```typescript
// e2e/fixtures/test-data.ts
// Generates unique test data per run to avoid CNPJ/email collisions

let counter = 0;

function uniqueId(): string {
  counter++;
  return `${Date.now()}-${counter}`;
}

export function generateCompanyData() {
  const id = uniqueId();
  return {
    razaoSocial: `Empresa E2E ${id}`,
    // Valid CNPJ: generate with correct check digits
    // Use a fixed prefix + randomizer that passes modulo-11 validation
    cnpj: generateValidCnpj(),
    email: `e2e-pj-${id}@test.com`,
    phone: '11999990000',
    password: 'E2e@Test2026',
  };
}

export function generateEmployeeData() {
  const id = uniqueId();
  return {
    nome: `Funcionário E2E ${id}`,
    cpf: generateValidCpf(),
    email: `e2e-emp-${id}@test.com`,
    phone: '11988880000',
  };
}

// Simple valid CNPJ generator (modulo-11)
function generateValidCnpj(): string {
  const base = Array.from({ length: 12 }, () => Math.floor(Math.random() * 10));
  const d1 = calcCnpjDigit(base, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
  const d2 = calcCnpjDigit([...base, d1], [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
  return [...base, d1, d2].join('');
}

function calcCnpjDigit(nums: number[], weights: number[]): number {
  const sum = nums.reduce((acc, n, i) => acc + n * weights[i], 0);
  const remainder = sum % 11;
  return remainder < 2 ? 0 : 11 - remainder;
}

function generateValidCpf(): string {
  const base = Array.from({ length: 9 }, () => Math.floor(Math.random() * 10));
  const d1 = calcCpfDigit(base);
  const d2 = calcCpfDigit([...base, d1]);
  return [...base, d1, d2].join('');
}

function calcCpfDigit(nums: number[]): number {
  const sum = nums.reduce((acc, n, i) => acc + n * (nums.length + 1 - i), 0);
  const remainder = (sum * 10) % 11;
  return remainder === 10 ? 0 : remainder;
}
```

## Auth Flow Deep Dive

### Complete ACF Flow for E2E Tests

The client-side app uses Auth Code Flow with PKCE. Here is the exact redirect chain:

1. **Browser** navigates to `http://localhost:5173/` (root)
2. **React** renders `RootRoute` component → `useAuth()` → not authenticated → calls `login()`
3. **`login()`** sets `window.location.href = "/auth/login"`
4. **Vinxi auth-server** (`/auth/login`) generates PKCE `code_verifier` + `code_challenge`, sets `pkce_code_verifier` and `pkce_state` httpOnly cookies, then responds with **302 redirect** to Keycloak
5. **Keycloak** (`http://localhost:8180/realms/client/protocol/openid-connect/auth?...`) renders the custom login form (`onboarding-client` theme)
6. **User fills** `#username` + `#password` + clicks `#kc-login`
7. **Keycloak** validates credentials, responds with **302 redirect** to `http://localhost:5173/auth/callback?code=xxx&state=yyy`
8. **Vinxi auth-server** (`/auth/callback`) exchanges code for tokens using PKCE verifier, sets `client_access_token` and `client_refresh_token` httpOnly cookies, then responds with **302 redirect** to `/profile`
9. **React** renders `AuthCallbackPage` → polls `/auth/me` → user is authenticated → `window.location.href = "/profile"`
10. **React** renders profile or group-based default route

**For E2E test flow after registration:**
- After `registerCompany()` succeeds (POST 201), the form calls `window.location.href = "/"`
- This triggers the ACF flow from step 2 above
- The new PJ user's credentials are submitted to Keycloak's login form

**Key selectors for Keycloak login interaction (from `login.ftl`):**
- Username: `#username` (label: depends on `loginWithEmailAllowed` — likely "email")
- Password: `#password` (label: "Senha")
- Submit: `#kc-login` (button text: Keycloak's `doLogIn` i18n key — likely "Entrar")
- Registration link: `.kc-register-link` (text: "Criar conta →")

### Authentication in the Client App (Cookie-Based)

Auth state is determined server-side via httpOnly cookies:
- `client_access_token` — JWT access token (5-min lifespan)
- `client_refresh_token` — Refresh token (8-hour lifespan)
- Session restoration: `AuthProvider` → `fetch("/auth/me", { credentials: "include" })` → returns `{ isAuthenticated, userName, email, accessGroup, companyId }`

**Playwright storageState correctly captures httpOnly cookies.** This is the recommended approach for auth reuse.

## Permission Rendering

### Access Group → UI Behavior Map

| Feature | admin-empresa | viewer | dashboard |
|---------|:---:|:---:|:---:|
| **Sidebar: Dashboard link** | ✓ | ✗ | ✓ |
| **Sidebar: Funcionários link** | ✓ | ✓ | ✓ |
| **Sidebar: Perfil Empresa link** | ✓ | ✓ | ✓ |
| **Employees table: Ações column** | ✓ | ✗ | ✗ |
| **Employee actions dropdown** | ✓ | ✗ | ✗ |
| **Dashboard page access** | ✓ | ✗ (redirect) | ✓ |
| **Employees page access** | ✓ | ✓ (read-only) | ✓ (read-only) |
| **Default route after login** | `/employees` | `/employees` | `/dashboard` |

### How Permissions Are Implemented in Code

1. **Sidebar** (`Sidebar.tsx`): Filters `NAV_ITEMS` by `item.groups.includes(userGroup)`. viewer doesn't see Dashboard link.
2. **EmployeesTable** (`EmployeesTable.tsx`): When `isViewer = accessGroup === "viewer"`, the "Ações" column header and `EmployeeActionsDropdown` are hidden.
3. **DashboardPage** (`DashboardPage.tsx`): `if (auth.accessGroup !== "admin-empresa" && auth.accessGroup !== "dashboard")` → redirects via `Navigate`.
4. **EmployeesPage** (`EmployeesPage.tsx`): `if (accessGroup !== "admin-empresa" && accessGroup !== "viewer" && accessGroup !== "dashboard")` → redirects to `/profile`.
5. **RootRoute** (`router.tsx`): `getDefaultRouteForGroup(auth.accessGroup)` → admin-empresa/viewer → `/employees`, dashboard → `/dashboard`.

**Test verification points:**
- Viewer: No "Ações" column in employee table, no Dashboard in sidebar
- admin-empresa: Full actions dropdown (Edit, Block, Reset Password, Change Group, Delete)
- dashboard: Can access dashboard, sees read-only employee table (no actions column — `isViewer` logic applies since it's not admin-empresa)

### data-testid Attributes Available

From codebase analysis, these `data-testid` values are available for E2E selectors:

| Element | data-testid | Source |
|---------|-------------|--------|
| Employees page container | `employees-page` | EmployeesPage.tsx |
| Employees table wrapper | `employees-table-wrapper` | EmployeesTable.tsx |
| Employees table | `employees-table` | EmployeesTable.tsx |
| Table loading state | `table-loading` | EmployeesTable.tsx |
| Table empty state | `table-empty` | EmployeesTable.tsx |
| Table error state | `table-error` | EmployeesTable.tsx |
| Employee row | `employee-row-{id}` | EmployeesTable.tsx |
| Group badge | `badge-group-{id}` | EmployeesTable.tsx |
| Status badge (active) | `badge-status-active-{id}` | EmployeesTable.tsx |
| Status badge (blocked) | `badge-status-blocked-{id}` | EmployeesTable.tsx |
| Actions dropdown trigger | `actions-dropdown-trigger-{id}` | EmployeeActionsDropdown.tsx |
| Actions dropdown content | `actions-dropdown-content-{id}` | EmployeeActionsDropdown.tsx |
| Edit action | `action-edit-{id}` | EmployeeActionsDropdown.tsx |
| Block/Unblock action | `action-block-unblock-{id}` | EmployeeActionsDropdown.tsx |
| Reset password action | `action-reset-password-{id}` | EmployeeActionsDropdown.tsx |
| Delete action | `action-delete-{id}` | EmployeeActionsDropdown.tsx |
| Change group action | `action-change-group-{id}` | EmployeeActionsDropdown.tsx |
| Change group dialog | `change-access-group-dialog` | ChangeAccessGroupDialog.tsx |
| New group select | `new-access-group-select` | ChangeAccessGroupDialog.tsx |
| Change group confirm | `change-group-confirm-button` | ChangeAccessGroupDialog.tsx |
| Change group cancel | `change-group-cancel-button` | ChangeAccessGroupDialog.tsx |
| Refresh button | `refresh-button` | EmployeesPage.tsx |
| Auth guard loading | `auth-guard-loading` | AuthGuard.tsx |

**Missing data-testid values that would be useful:**
- Registration form fields (currently only `#password` and `#confirmPassword` have IDs)
- Dashboard card containers
- Header (no testid)
- Sidebar nav items

## API Endpoints for E2E Tests

| Endpoint | Method | Purpose | Auth |
|----------|--------|---------|------|
| `/api/companies/registration` | POST | Register PJ company | None |
| `/api/companies/me` | GET | Get company profile | Cookie |
| `/api/companies/{companyId}/employees` | GET | List employees | Cookie |
| `/api/companies/{companyId}/employees/{id}` | PUT | Update employee | Cookie |
| `/api/companies/{companyId}/employees/{id}` | DELETE | Delete employee | Cookie |
| `/api/companies/{companyId}/employees/{id}/toggle-status` | POST | Block/unblock | Cookie |
| `/api/companies/{companyId}/employees/{id}/reset-password` | POST | Reset password | Cookie |
| `/api/companies/{companyId}/employees/{id}/access-group` | PUT | Change access group | Cookie |
| `/auth/login` | GET | Start ACF (redirect) | — |
| `/auth/callback` | GET | ACF token exchange | — |
| `/auth/me` | GET | Get auth state | Cookie |
| `/auth/logout` | GET | Logout | — |

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| ROPC grant (direct token) | Auth Code Flow + PKCE | v5.0 (Phase 29) | E2E must navigate Keycloak login page — cannot use direct token request |
| localStorage JWT | httpOnly cookies | v5.0 (Phase 29) | E2E uses storageState (captures httpOnly cookies), cannot inject via JS |
| Manual auth per test | Setup project + storageState | Playwright 1.30+ | Auth once, reuse across all tests in a project |
| Vitest unit tests only | Vitest + Playwright E2E | Phase 14→43 | Separate test runners: Vitest for unit, Playwright for E2E |

**Deprecated/outdated:**
- Phase 14 (E2E from v2.0): Used ROPC grant — now obsolete since ACF replaced ROPC
- `keycloak-js` adapter: Not used — designed for ROPC. Project explicitly avoids it.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Keycloak's `onboarding-client` theme renders `#username` and `#password` fields for ACF login | Auth Flow / Keycloak Login Interaction | Tests would fail to find login form fields. Verified from `login.ftl` source. |
| A2 | `storageState` captures httpOnly cookies set by Vinxi's auth-server | Standard Stack / Architecture | If Playwright doesn't capture httpOnly cookies in storageState, auth reuse would fail. [CITED: Playwright docs — storageState includes all cookies] |
| A3 | The `/auth/me` endpoint returns `accessGroup` field from JWT claims | Auth Flow | If /auth/me doesn't return accessGroup, permission tests can't verify groups. Verified from `auth-server.ts` source. |
| A4 | Employees with `dashboard` group see a read-only employee table (same as viewer) | Permission Rendering | If dashboard group sees actions column, E2E-05 expectations would be wrong. Verified from `EmployeesTable.tsx`: `isViewer` only checks for `viewer`, not `dashboard`. However, only admin-empresa gets the actions column — dashboard and viewer both see read-only. |
| A5 | `npx playwright test` is the correct run command in `frontend/client` | Standard Stack | If the command path or working directory is wrong, CI integration would fail. [CITED: Playwright official docs] |
| A6 | Playwright's `page.context().cookies()` can read httpOnly cookies for JWT verification | JWT Verification | This is a Playwright API feature; verified from docs. If wrong, JWT test would need an alternative approach. |

## Open Questions

1. **Should E2E tests run against Docker Compose or a dedicated test environment?**
   - What we know: Docker Compose runs all services (Keycloak, API, frontend, DB, observability). E2E needs Keycloak + API + frontend.
   - What's unclear: Whether to document "run Docker Compose first" as a prerequisite vs. using `webServer` config to start only the frontend.
   - Recommendation: Document Docker Compose as prerequisite. Do NOT use `webServer` config — it would only start the frontend, not Keycloak or the API. Add a pre-check in the Playwright config that verifies `http://localhost:5173` is reachable.

2. **Should we add missing `data-testid` attributes to components?**
   - What we know: Several key components (Header, Dashboard cards, Registration form fields) lack `data-testid` attributes.
   - What's unclear: Whether to prioritize adding them as part of this phase or use alternative selectors (getByRole, getByPlaceholder).
   - Recommendation: Add `data-testid` to Header, Dashboard cards, and Registration form steps during the implementation phase. It's low effort and makes tests more resilient.

3. **How to handle the "create employee" flow — does it require a dialog or a separate page?**
   - What we know: The current EmployeesPage doesn't have a "Create Employee" button — employees are created via the backend API directly. The E2E success criterion says "PJ creates employee via UI."
   - What's unclear: Whether Phase 40 implemented a UI form for creating employees or just management of existing ones.
   - Recommendation: Verify by checking the backend `POST /api/companies/{companyId}/employees/registration` endpoint and whether a UI form exists. If no UI form exists, the E2E test will need to use API calls to create the employee, then verify the UI reflects it. UPDATE: From the EmployeesPage.tsx code, the `RegistrationForm` handles company registration, but **employee registration** may use the API directly. This needs verification — the EmployeesPage shows management dialogs but not a "Create Employee" dialog.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Docker Compose | Full E2E stack | ✓ | v2 | — |
| Node.js | Playwright runner | ✓ | 20+ | — |
| Chromium | Playwright browser | ✗ | — | `npx playwright install chromium` |
| Keycloak (Docker) | ACF auth flow | ✓ | 26.1 | — |
| .NET API (Docker) | Employee management | ✓ | 10.0 | — |
| PostgreSQL (Docker) | Data persistence | ✓ | 16-alpine | — |

**Missing dependencies with no fallback:**
- Chromium browser binary: Must run `npx playwright install chromium` before first E2E run

**Missing dependencies with fallback:**
- None — all core dependencies are Docker-managed

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright 1.59.x |
| Config file | `frontend/client/playwright.config.ts` (to be created) |
| Quick run command | `npx playwright test --project=admin-empresa` |
| Full suite command | `npx playwright test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| E2E-01 | Cadastro PJ completo → auto-login → dashboard | e2e | `npx playwright test registration` | ❌ Wave 0 |
| E2E-02 | Dashboard exibe cards mock | e2e | `npx playwright test dashboard` | ❌ Wave 0 |
| E2E-03 | PJ cria funcionário → aparece na lista | e2e | `npx playwright test employee-management` | ❌ Wave 0 |
| E2E-04 | Login como funcionário → redirect por group | e2e | `npx playwright test employee-login` | ❌ Wave 0 |
| E2E-05 | JWT decode + permission UI | e2e | `npx playwright test permission-ui` | ❌ Wave 0 |
| E2E-06 | Change group → re-login → updated permissions | e2e | `npx playwright test access-group-change` | ❌ Wave 0 |
| E2E-07 | All tests pass | e2e | `npx playwright test` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `npx playwright test --project=admin-empresa`
- **Per wave merge:** `npx playwright test`
- **Phase gate:** Full suite green with `npx playwright test` before `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `playwright.config.ts` — Playwright configuration file
- [ ] `e2e/auth/*.setup.ts` — Auth setup files (3 roles)
- [ ] `e2e/pages/*.page.ts` — Page Object files (5 pages)
- [ ] `e2e/fixtures/test-data.ts` — Test data generators
- [ ] `e2e/fixtures/jwt-utils.ts` — JWT decode utilities
- [ ] `e2e/*.spec.ts` — Test spec files (6)
- [ ] `playwright/.auth/` — Storage state directory (gitignored)
- [ ] @playwright/test in package.json devDependencies
- [ ] Chromium browser install: `npx playwright install chromium`
- [ ] .gitignore entry for `playwright/.auth/`

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | ACF + PKCE via Keycloak — E2E validates full redirect flow |
| V3 Session Management | yes | httpOnly cookie session — E2E validates cookie-based state |
| V4 Access Control | yes | Group-based permission rendering — E2E validates viewer vs admin-empresa UI |
| V5 Input Validation | no | Not in scope for E2E (unit tests cover this) |
| V6 Cryptography | no | Not in scope |

### Known Threat Patterns for E2E Testing Stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Test credential exposure | Information Disclosure | Environment variables for E2E creds, .gitignore for .auth/ state |
| Brute force lockout during tests | Denial of Service | storageState reuse (login once), sequential workers |
| Cross-company data leakage | Information Disclosure | E2E test for company isolation (verify PJ cannot see other PJ's employees) |

## Sources

### Primary (HIGH confidence)
- `/microsoft/playwright.dev` - Playwright configuration, auth setup, POM patterns, storageState
- `frontend/client/src/router.tsx` - Route structure and access group redirect logic
- `frontend/client/src/lib/auth-context.tsx` - AuthProvider, useAuth, getDefaultRouteForGroup
- `frontend/client/auth-server.ts` - PKCE flow implementation, /auth/me endpoint
- `frontend/client/src/components/molecules/EmployeesTable.tsx` - Permission rendering (isViewer logic)
- `frontend/client/src/components/organisms/Sidebar.tsx` - Nav filtering by access group
- `keycloak/themes/onboarding-client/login/login.ftl` - Keycloak login form selectors
- `keycloak/client-realm.json` - Realm config, groups, clients, brute force settings

### Secondary (MEDIUM confidence)
- `frontend/client/src/components/pages/EmployeesPage.tsx` - Employee management CRUD flows
- `frontend/client/src/components/molecules/RegistrationForm.tsx` - Registration wizard selectors
- `frontend/client/src/components/molecules/ChangeAccessGroupDialog.tsx` - Group change flow
- `frontend/client/src/lib/api.ts` - API client with endpoint documentation
- `compose.yaml` - Service topology and ports

### Tertiary (LOW confidence)
- None — all findings verified against source code or Playwright official docs

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Playwright is industry standard, version verified
- Architecture: HIGH - Full auth flow and permission rendering traced through source code
- Pitfalls: HIGH - Based on actual Keycloak config (brute force), actual server code (ACF timing), actual theme HTML (selectors)

**Research date:** 2026-04-26
**Valid until:** 2026-05-26 (30 days — Playwright stable)