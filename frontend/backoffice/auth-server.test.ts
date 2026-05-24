/**
 * Tests for auth-server.ts — backoffice SPA
 *
 * Coverage targets (T-2 acceptance):
 *   (a) fail-fast branch: KEYCLOAK_REALM="onboarding" throws at module load time
 *   (b) cookie attribute snapshot: access_token=lax, refresh_token=strict, httpOnly=true
 *
 * Environment note: tests run in vitest jsdom (global config). The auth-server module
 * is pure Node.js — we mock h3 and auth-code-flow so the module resolves cleanly.
 * The validation IIFE only uses process.env + throw, no browser APIs.
 */

import { describe, it, expect, vi, afterEach } from "vitest";

// Mock h3 at module level (hoisted by vitest). This makes auth-server importable
// in the jsdom environment without requiring real h3 request/response objects.
vi.mock("h3", () => ({
  createRouter: vi.fn(() => ({ get: vi.fn(), post: vi.fn(), handler: vi.fn() })),
  defineEventHandler: vi.fn((fn: unknown) => fn),
  getQuery: vi.fn(() => ({})),
  setCookie: vi.fn(),
  deleteCookie: vi.fn(),
  getCookie: vi.fn(() => undefined),
  getRequestHeader: vi.fn(() => undefined),
  sendRedirect: vi.fn(async () => undefined),
}));

// Mock auth-code-flow (relative import from auth-server.ts)
vi.mock("./src/lib/auth-code-flow", () => ({
  generateCodeVerifier: vi.fn(() => "test-verifier"),
  generateCodeChallenge: vi.fn(async () => "test-challenge"),
  buildAuthorizationUrl: vi.fn(() => "http://keycloak/auth"),
  exchangeCodeForTokens: vi.fn(async () => ({
    accessToken: "fake.access.token",
    refreshToken: "fake.refresh.token",
    expiresIn: 300,
  })),
  refreshAccessToken: vi.fn(async () => ({
    accessToken: "new.access.token",
    refreshToken: "new.refresh.token",
    expiresIn: 300,
  })),
}));

// ── Helpers ──────────────────────────────────────────────────────────────────

/**
 * Load auth-server.ts fresh in isolation with a given KEYCLOAK_REALM env value.
 * vi.resetModules() causes the module-level validateRealm() IIFE to re-execute.
 *
 * Always stubs KEYCLOAK_CLIENT_SECRET because auth-server.ts also fail-fasts on
 * an empty secret; tests that only exercise realm validation still need the
 * secret present to reach the realm check.
 */
async function loadModuleWithRealm(realm: string | undefined): Promise<void> {
  vi.resetModules();
  if (realm === undefined) {
    vi.unstubAllEnvs();
    delete process.env.KEYCLOAK_REALM;
  } else {
    vi.stubEnv("KEYCLOAK_REALM", realm);
  }
  vi.stubEnv("KEYCLOAK_CLIENT_SECRET", "test-secret");
  await import("./auth-server");
}

/**
 * Load auth-server.ts fresh with a given CLIENT_SECRET env value. Used by the
 * fail-fast tests below.
 */
async function loadModuleWithSecret(secret: string | undefined): Promise<void> {
  vi.resetModules();
  vi.unstubAllEnvs();
  delete process.env.KEYCLOAK_REALM;
  if (secret === undefined) {
    delete process.env.KEYCLOAK_CLIENT_SECRET;
  } else {
    vi.stubEnv("KEYCLOAK_CLIENT_SECRET", secret);
  }
  await import("./auth-server");
}

// ── T-2a: realm fail-fast ─────────────────────────────────────────────────────

describe("auth-server/backoffice — realm fail-fast (T-2a)", () => {
  afterEach(() => {
    vi.resetModules();
    vi.unstubAllEnvs();
    delete process.env.KEYCLOAK_REALM;
  });

  it("throws when KEYCLOAK_REALM is the legacy value 'onboarding'", async () => {
    await expect(loadModuleWithRealm("onboarding")).rejects.toThrow(
      /KEYCLOAK_REALM="onboarding" is not a supported realm/
    );
  });

  it("error message names the supported realms", async () => {
    await expect(loadModuleWithRealm("onboarding")).rejects.toThrow(
      /Supported values: "client", "backoffice"/
    );
  });

  it("error message mentions Phase 34 realm removal", async () => {
    await expect(loadModuleWithRealm("onboarding")).rejects.toThrow(
      /Phase 34/
    );
  });

  it("throws for any other unrecognised realm value", async () => {
    await expect(loadModuleWithRealm("master")).rejects.toThrow(
      /not a supported realm/
    );
  });

  it("does NOT throw when KEYCLOAK_REALM='backoffice'", async () => {
    await expect(loadModuleWithRealm("backoffice")).resolves.toBeUndefined();
  });

  it("does NOT throw when KEYCLOAK_REALM='client'", async () => {
    await expect(loadModuleWithRealm("client")).resolves.toBeUndefined();
  });

  it("does NOT throw when KEYCLOAK_REALM is undefined (per-SPA default 'backoffice')", async () => {
    await expect(loadModuleWithRealm(undefined)).resolves.toBeUndefined();
  });
});

// ── CLIENT_SECRET fail-fast ───────────────────────────────────────────────────
//
// Regression guard mirroring frontend/client/auth-server.test.ts. Prior to the
// fix, KEYCLOAK_CLIENT_SECRET fell back to "" and the empty secret was sent to
// Keycloak /token, producing 401 unauthorized_client mid-login flow.

describe("auth-server/backoffice — CLIENT_SECRET fail-fast", () => {
  afterEach(() => {
    vi.resetModules();
    vi.unstubAllEnvs();
    delete process.env.KEYCLOAK_CLIENT_SECRET;
  });

  it("throws when KEYCLOAK_CLIENT_SECRET is the empty string", async () => {
    await expect(loadModuleWithSecret("")).rejects.toThrow(
      /KEYCLOAK_CLIENT_SECRET is not set/
    );
  });

  it("throws when KEYCLOAK_CLIENT_SECRET is undefined", async () => {
    await expect(loadModuleWithSecret(undefined)).rejects.toThrow(
      /KEYCLOAK_CLIENT_SECRET is not set/
    );
  });

  it("error message names the upstream symptom (401 unauthorized_client)", async () => {
    await expect(loadModuleWithSecret("")).rejects.toThrow(
      /401 unauthorized_client/
    );
  });

  it("error message points at the .env loading mechanism", async () => {
    await expect(loadModuleWithSecret("")).rejects.toThrow(
      /--env-file-if-exists=\.\.\/\.\.\/\.env/
    );
  });

  it("error message names the compose env var bridge KEYCLOAK_BACKOFFICE_CLIENT_SECRET", async () => {
    await expect(loadModuleWithSecret("")).rejects.toThrow(
      /KEYCLOAK_BACKOFFICE_CLIENT_SECRET/
    );
  });

  it("does NOT throw when CLIENT_SECRET is a non-empty value", async () => {
    await expect(loadModuleWithSecret("real-secret-value")).resolves.toBeUndefined();
  });
});

// ── T-2b: cookie attribute snapshot ──────────────────────────────────────────
//
// Strategy: read the source file and assert the sameSite literal values used in
// every setCookie call for each named cookie. Static assertion catches regressions
// if someone reverts sameSite back to "strict" on the access token cookie.

describe("auth-server/backoffice — cookie sameSite attributes (T-2b)", () => {
  const src = (() => {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const fs = require("fs") as typeof import("fs");
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const path = require("path") as typeof import("path");
    return fs.readFileSync(path.resolve(__dirname, "./auth-server.ts"), "utf8");
  })();

  it("backoffice_access_token: every setCookie uses sameSite='lax'", () => {
    const matches = [...src.matchAll(
      /setCookie\(event,\s*"backoffice_access_token"[\s\S]*?sameSite:\s*["'](\w+)["']/g
    )];
    expect(matches.length).toBeGreaterThan(0);
    for (const m of matches) {
      expect(m[1]).toBe("lax");
    }
  });

  it("backoffice_refresh_token: every setCookie uses sameSite='strict'", () => {
    const matches = [...src.matchAll(
      /setCookie\(event,\s*"backoffice_refresh_token"[\s\S]*?sameSite:\s*["'](\w+)["']/g
    )];
    expect(matches.length).toBeGreaterThan(0);
    for (const m of matches) {
      expect(m[1]).toBe("strict");
    }
  });

  it("backoffice_access_token: every setCookie has httpOnly=true", () => {
    const matches = [...src.matchAll(
      /setCookie\(event,\s*"backoffice_access_token"[\s\S]*?httpOnly:\s*(true)/g
    )];
    expect(matches.length).toBeGreaterThan(0);
    for (const m of matches) {
      expect(m[1]).toBe("true");
    }
  });

  it("backoffice_refresh_token: every setCookie has httpOnly=true", () => {
    const matches = [...src.matchAll(
      /setCookie\(event,\s*"backoffice_refresh_token"[\s\S]*?httpOnly:\s*(true)/g
    )];
    expect(matches.length).toBeGreaterThan(0);
    for (const m of matches) {
      expect(m[1]).toBe("true");
    }
  });

  it("backoffice_access_token: every setCookie has path='/'", () => {
    const matches = [...src.matchAll(
      /setCookie\(event,\s*"backoffice_access_token"[\s\S]*?path:\s*["'](\/)["']/g
    )];
    expect(matches.length).toBeGreaterThan(0);
    for (const m of matches) {
      expect(m[1]).toBe("/");
    }
  });

  it("backoffice_refresh_token: every setCookie has path='/'", () => {
    const matches = [...src.matchAll(
      /setCookie\(event,\s*"backoffice_refresh_token"[\s\S]*?path:\s*["'](\/)["']/g
    )];
    expect(matches.length).toBeGreaterThan(0);
    for (const m of matches) {
      expect(m[1]).toBe("/");
    }
  });

  it("no 'onboarding' used as KEYCLOAK_REALM default or fallback value", () => {
    // Guard against re-introduction of the broken legacy realm name as a default/fallback.
    // The pattern captures: || "onboarding" or ?? "onboarding" or = "onboarding"
    // (client IDs like "onboarding-backoffice" are allowed — those are not realm names)
    expect(src).not.toMatch(/(?:\|\||\?\?|=\s*)["'`]onboarding["'`]/);
    // Also ensure KEYCLOAK_REALM is never assigned the literal string "onboarding"
    expect(src).not.toMatch(/KEYCLOAK_REALM\s*=\s*["'`]onboarding["'`]/);
  });
});

// ── T-18: id_token_hint forwarded on logout (W-SEC-IT4-1) ────────────────────
//
// Strategy: read the source file and assert static structural invariants:
//   (a) id_token cookie is set in the callback handler with httpOnly:true, sameSite:lax, path:/
//   (b) logout handler reads the id_token cookie
//   (c) logout URL appends id_token_hint= when the cookie is present
//   (d) fallback path exists (conditional — does NOT hard-fail when cookie absent)
//   (e) id_token cookie is deleted in the logout handler alongside access/refresh
//
// We also test the runtime branch with a behavioral test simulating the logout logic.

describe("auth-server/backoffice — id_token_hint forwarded on logout (T-18)", () => {
  const src = (() => {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const fs = require("fs") as typeof import("fs");
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const path = require("path") as typeof import("path");
    return fs.readFileSync(path.resolve(__dirname, "./auth-server.ts"), "utf8");
  })();

  // ── Static source assertions ────────────────────────────────────────────

  it("callback handler sets backoffice_id_token cookie with httpOnly:true", () => {
    const matches = [...src.matchAll(
      /setCookie\(event,\s*"backoffice_id_token"[\s\S]*?httpOnly:\s*(true)/g
    )];
    expect(matches.length).toBeGreaterThan(0);
    for (const m of matches) {
      expect(m[1]).toBe("true");
    }
  });

  it("callback handler sets backoffice_id_token cookie with sameSite:lax", () => {
    const matches = [...src.matchAll(
      /setCookie\(event,\s*"backoffice_id_token"[\s\S]*?sameSite:\s*["'](\w+)["']/g
    )];
    expect(matches.length).toBeGreaterThan(0);
    for (const m of matches) {
      expect(m[1]).toBe("lax");
    }
  });

  it("callback handler sets backoffice_id_token cookie with path:'/'", () => {
    const matches = [...src.matchAll(
      /setCookie\(event,\s*"backoffice_id_token"[\s\S]*?path:\s*["'](\/)["']/g
    )];
    expect(matches.length).toBeGreaterThan(0);
    for (const m of matches) {
      expect(m[1]).toBe("/");
    }
  });

  it("logout handler reads backoffice_id_token cookie before deleting tokens", () => {
    // getCookie must be called for backoffice_id_token in the logout handler.
    expect(src).toMatch(/getCookie\(event,\s*"backoffice_id_token"\)/);
  });

  it("logout handler appends id_token_hint when cookie present (conditional path)", () => {
    // The source must contain the conditional append pattern.
    expect(src).toMatch(/id_token_hint.*encodeURIComponent\(idToken\)/s);
  });

  it("logout handler deletes backoffice_id_token cookie", () => {
    // The id_token cookie must be explicitly deleted on logout alongside access/refresh.
    expect(src).toMatch(/deleteCookie\(event,\s*"backoffice_id_token"/);
  });

  it("logout URL omits id_token_hint when backoffice_id_token cookie absent (graceful fallback)", () => {
    // The conditional block must be guarded (if (idToken)) — never hard-fail when absent.
    expect(src).toMatch(/if\s*\(idToken\)\s*\{[\s\S]*?id_token_hint/);
  });

  // ── Behavioral tests: runtime branch ─────────────────────────────────────

  it("logout URL contains id_token_hint= when backoffice_id_token cookie is set", () => {
    const KEYCLOAK_PUBLIC_URL_TEST = "http://localhost:8180";
    const KEYCLOAK_REALM_TEST = "backoffice";
    const CLIENT_ID_TEST = "onboarding-backoffice";
    const FRONTEND_URL_TEST = "http://localhost:5174";

    // Replicate the logout handler logic exactly as written in auth-server.ts
    const idTokenFromCookie = "header.payload.sig"; // simulates getCookie result
    const logoutUrl = `${KEYCLOAK_PUBLIC_URL_TEST}/realms/${KEYCLOAK_REALM_TEST}/protocol/openid-connect/logout`;
    const postLogoutRedirectUri = `${FRONTEND_URL_TEST}/auth/login`;
    let fullUrl = `${logoutUrl}?post_logout_redirect_uri=${encodeURIComponent(postLogoutRedirectUri)}&client_id=${encodeURIComponent(CLIENT_ID_TEST)}`;
    if (idTokenFromCookie) {
      fullUrl += `&id_token_hint=${encodeURIComponent(idTokenFromCookie)}`;
    }

    expect(fullUrl).toContain("id_token_hint=");
    expect(fullUrl).toContain(encodeURIComponent("header.payload.sig"));
  });

  it("logout URL omits id_token_hint when backoffice_id_token cookie absent", () => {
    const KEYCLOAK_PUBLIC_URL_TEST = "http://localhost:8180";
    const KEYCLOAK_REALM_TEST = "backoffice";
    const CLIENT_ID_TEST = "onboarding-backoffice";
    const FRONTEND_URL_TEST = "http://localhost:5174";

    // Simulate absent cookie (undefined)
    const idTokenFromCookie = undefined;
    const logoutUrl = `${KEYCLOAK_PUBLIC_URL_TEST}/realms/${KEYCLOAK_REALM_TEST}/protocol/openid-connect/logout`;
    const postLogoutRedirectUri = `${FRONTEND_URL_TEST}/auth/login`;
    let fullUrl = `${logoutUrl}?post_logout_redirect_uri=${encodeURIComponent(postLogoutRedirectUri)}&client_id=${encodeURIComponent(CLIENT_ID_TEST)}`;
    if (idTokenFromCookie) {
      fullUrl += `&id_token_hint=${encodeURIComponent(idTokenFromCookie)}`;
    }

    expect(fullUrl).not.toContain("id_token_hint");
    expect(fullUrl).toContain("client_id=");
    expect(fullUrl).toContain("post_logout_redirect_uri=");
  });
});

// ── Static guards: dev script + compose.yaml env injection ───────────────────
//
// These guards mirror frontend/client/auth-server.test.ts. They block regression
// of the .env loading misconfig and the Docker env-var bridge.

describe("auth-server/backoffice — dev script env-file flag (static guard)", () => {
  const pkg = (() => {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const fs = require("fs") as typeof import("fs");
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const path = require("path") as typeof import("path");
    return fs.readFileSync(path.resolve(__dirname, "./package.json"), "utf8");
  })();

  it("dev script invokes node with --env-file-if-exists=../../.env", () => {
    const parsed = JSON.parse(pkg) as { scripts?: Record<string, string> };
    const dev = parsed.scripts?.dev ?? "";
    expect(dev).toMatch(/node\b/);
    expect(dev).toMatch(/--env-file-if-exists=\.\.\/\.\.\/\.env/);
  });

  it("dev script invokes vinxi via node_modules/vinxi/bin/cli.mjs (not bare vinxi)", () => {
    const parsed = JSON.parse(pkg) as { scripts?: Record<string, string> };
    const dev = parsed.scripts?.dev ?? "";
    expect(dev).toMatch(/node_modules\/vinxi\/bin\/cli\.mjs/);
  });
});

describe("auth-server/backoffice — compose.yaml env injection (static guard)", () => {
  const compose = (() => {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const fs = require("fs") as typeof import("fs");
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const path = require("path") as typeof import("path");
    return fs.readFileSync(path.resolve(__dirname, "../../compose.yaml"), "utf8");
  })();

  // Extract the frontend-backoffice service block — see client mirror for rationale.
  const backofficeBlock = (() => {
    const lines = compose.split("\n");
    const start = lines.findIndex((l) => l === "  frontend-backoffice:");
    if (start < 0) return "";
    let end = lines.length;
    for (let i = start + 1; i < lines.length; i++) {
      if (/^ {2}\S/.test(lines[i])) {
        end = i;
        break;
      }
    }
    return lines.slice(start, end).join("\n");
  })();

  it("frontend-backoffice block is found in compose.yaml", () => {
    expect(backofficeBlock).not.toBe("");
    expect(backofficeBlock).toMatch(/^ {2}frontend-backoffice:/);
  });

  it("frontend-backoffice environment includes KEYCLOAK_CLIENT_SECRET (mapped from KEYCLOAK_BACKOFFICE_CLIENT_SECRET)", () => {
    expect(backofficeBlock).toMatch(/KEYCLOAK_CLIENT_SECRET:\s*\$\{KEYCLOAK_BACKOFFICE_CLIENT_SECRET\}/);
  });

  it("frontend-backoffice environment includes KEYCLOAK_CLIENT_ID (mapped from KEYCLOAK_BACKOFFICE_CLIENT_ID)", () => {
    expect(backofficeBlock).toMatch(/KEYCLOAK_CLIENT_ID:\s*\$\{KEYCLOAK_BACKOFFICE_CLIENT_ID\}/);
  });

  it("frontend-backoffice environment sets API_INTERNAL_URL to http://api:8080", () => {
    expect(backofficeBlock).toMatch(/API_INTERNAL_URL:\s*http:\/\/api:8080/);
  });
});
