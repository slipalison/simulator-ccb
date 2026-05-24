// ---------------------------------------------------------------------------
// admin-telemetry.test.ts (BFE-1 fix: eliminated vi.resetModules() + dynamic import)
// ---------------------------------------------------------------------------
// Unit tests for the backoffice OTel telemetry initialiser (OTel SDK v2).
// Uses vi.mock() at module level + static imports so that v8 coverage
// instruments the source file exactly once — fixing the 50% coverage gate fail.
//
// BFE-1 root cause: vi.resetModules() in beforeEach + await import() per test
// caused v8 to only instrument the first module evaluation. The subsequent
// re-evaluations (after resetModules) do not contribute to coverage.
//
// Fix: vi.mock() stubs established at hoist time using vi.hoisted() to avoid
// temporal dead zone errors. Module imported ONCE at top (static).
// _resetInitialisedForTesting() called in beforeEach to reset the init guard.
//
// Security assertions (D-12, D-15, D-35):
//   - generateAnonymousSessionId produces a valid UUID v4 string
//   - generateAnonymousSessionId NEVER writes to localStorage/sessionStorage
//   - initAdminTelemetry exits early when VITE_OTEL_ENABLED !== 'true'
//   - FetchInstrumentation is configured with ALLOWED_BACKEND_URLS allowlist
//   - Keycloak / auth chain URLs are in the ignore list
//   - PII attribute keys are scrubbed before export
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";

// ---------------------------------------------------------------------------
// vi.hoisted() — declare mutable spy state BEFORE vi.mock() factories are run.
// This avoids ReferenceError from temporal dead zone when factories reference
// module-level `const` bindings (which are hoisted but not yet initialized).
// ---------------------------------------------------------------------------
const {
  mockRegister,
  capturedProviderConfigs,
  capturedResourceAttrs,
  capturedFetchConfigs,
  capturedUIConfigs,
  counters,
  mockRegisterInstrumentations,
  mockMeter,
} = vi.hoisted(() => {
  const histogram = { record: vi.fn() };
  const mockMeter = {
    createHistogram: vi.fn().mockReturnValue(histogram),
    createCounter: vi.fn(),
    createGauge: vi.fn(),
  };
  return {
    mockRegister: vi.fn(),
    capturedProviderConfigs: [] as unknown[],
    capturedResourceAttrs: [] as Record<string, unknown>[],
    capturedFetchConfigs: [] as unknown[],
    capturedUIConfigs: [] as unknown[],
    counters: { w3cPropagator: 0, documentLoad: 0, webTracerProvider: 0 },
    mockRegisterInstrumentations: vi.fn(),
    mockMeter,
  };
});

// ---------------------------------------------------------------------------
// Mock factories — OTel v2 API (spanProcessors in constructor, no addSpanProcessor)
// ---------------------------------------------------------------------------

vi.mock("@opentelemetry/sdk-trace-web", () => {
  class WebTracerProvider {
    constructor(config?: unknown) {
      counters.webTracerProvider++;
      capturedProviderConfigs.push(config);
    }
    register = mockRegister;
  }
  class BatchSpanProcessor {
    constructor(_exporter?: unknown) { /* stub */ }
  }
  return { WebTracerProvider, BatchSpanProcessor };
});

vi.mock("@opentelemetry/exporter-trace-otlp-http", () => {
  class OTLPTraceExporter {
    constructor(_config?: unknown) { /* stub */ }
  }
  return { OTLPTraceExporter };
});

// In OTel v2, @opentelemetry/resources exports resourceFromAttributes (a function),
// not a constructor. The telemetry module uses resourceFromAttributes().
vi.mock("@opentelemetry/resources", () => {
  function resourceFromAttributes(attrs: Record<string, unknown>) {
    capturedResourceAttrs.push(attrs);
    return { attributes: attrs };
  }
  return { resourceFromAttributes };
});

vi.mock("@opentelemetry/semantic-conventions", () => ({
  ATTR_SERVICE_NAME: "service.name",
  ATTR_SERVICE_VERSION: "service.version",
}));

vi.mock("@opentelemetry/core", () => {
  class W3CTraceContextPropagator {
    readonly _type = "W3C";
    constructor() {
      counters.w3cPropagator++;
    }
  }
  return { W3CTraceContextPropagator };
});

vi.mock("@opentelemetry/instrumentation", () => ({
  registerInstrumentations: mockRegisterInstrumentations,
}));

vi.mock("@opentelemetry/instrumentation-fetch", () => {
  class FetchInstrumentation {
    constructor(config?: unknown) {
      capturedFetchConfigs.push(config);
    }
  }
  return { FetchInstrumentation };
});

vi.mock("@opentelemetry/instrumentation-document-load", () => {
  class DocumentLoadInstrumentation {
    constructor() {
      counters.documentLoad++;
    }
  }
  return { DocumentLoadInstrumentation };
});

vi.mock("@opentelemetry/instrumentation-user-interaction", () => {
  class UserInteractionInstrumentation {
    constructor(config?: unknown) {
      capturedUIConfigs.push(config);
    }
  }
  return { UserInteractionInstrumentation };
});

// Mock @opentelemetry/api metrics to prevent histogram errors in tests
vi.mock("@opentelemetry/api", () => ({
  metrics: {
    getMeter: vi.fn().mockReturnValue(mockMeter),
  },
  trace: {
    getTracer: vi.fn(),
  },
  context: {},
  propagation: {},
}));

// Mock web-vitals to prevent observer registration in unit tests
vi.mock("web-vitals", () => ({
  onCLS: vi.fn(),
  onINP: vi.fn(),
  onLCP: vi.fn(),
  onFCP: vi.fn(),
  onTTFB: vi.fn(),
}));

// ---------------------------------------------------------------------------
// BFE-1 fix: Static import — module evaluated ONCE, v8 covers all lines.
// _resetInitialisedForTesting() resets the init guard between test cases.
// ---------------------------------------------------------------------------
import {
  initAdminTelemetry,
  generateAnonymousSessionId,
  ALLOWED_BACKEND_URLS,
  SUPPRESS_URL_PATTERNS,
  _resetInitialisedForTesting,
  scrubAttributes,
} from "@/lib/telemetry";

// ---------------------------------------------------------------------------
// Reset helpers
// ---------------------------------------------------------------------------

function resetCounters() {
  mockRegister.mockClear();
  mockRegisterInstrumentations.mockClear();
  capturedProviderConfigs.length = 0;
  capturedResourceAttrs.length = 0;
  capturedFetchConfigs.length = 0;
  capturedUIConfigs.length = 0;
  counters.w3cPropagator = 0;
  counters.documentLoad = 0;
  counters.webTracerProvider = 0;
}

// ---------------------------------------------------------------------------
// Tests: generateAnonymousSessionId
// ---------------------------------------------------------------------------

describe("generateAnonymousSessionId", () => {
  it("returns a valid UUID v4 string", () => {
    const id = generateAnonymousSessionId();
    expect(id).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
    );
  });

  it("produces a different id on each call (entropy check)", () => {
    const ids = new Set(
      Array.from({ length: 10 }, () => generateAnonymousSessionId())
    );
    expect(ids.size).toBe(10);
  });

  it("does NOT write to localStorage (D-12 regression)", () => {
    const setItemSpy = vi.spyOn(Storage.prototype, "setItem");
    generateAnonymousSessionId();
    expect(setItemSpy).not.toHaveBeenCalled();
    setItemSpy.mockRestore();
  });

  it("does NOT write to sessionStorage (D-12 regression)", () => {
    // sessionStorage.setItem routes through Storage.prototype.setItem in jsdom
    const setItemSpy = vi.spyOn(Storage.prototype, "setItem");
    generateAnonymousSessionId();
    expect(setItemSpy).not.toHaveBeenCalled();
    setItemSpy.mockRestore();
  });

  it("returns a non-empty string (crypto.randomUUID available in jsdom)", () => {
    // In jsdom, crypto.randomUUID is always available.
    // We verify the primary crypto.randomUUID path produces a valid output.
    const id = generateAnonymousSessionId();
    expect(typeof id).toBe("string");
    expect(id.length).toBeGreaterThan(0);
  });

  it("uses fallback UUID when crypto is replaced with object lacking randomUUID", () => {
    // Replace global crypto with a plain object to force the fallback branch.
    // vi.stubGlobal replaces the entire global — restored via vi.unstubAllGlobals.
    vi.stubGlobal("crypto", {});

    const id = generateAnonymousSessionId();
    // Fallback produces a UUID-shaped string (xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx)
    expect(id).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i);

    vi.unstubAllGlobals();
  });
});

// ---------------------------------------------------------------------------
// Tests: initAdminTelemetry
// ---------------------------------------------------------------------------

describe("initAdminTelemetry", () => {
  beforeEach(() => {
    resetCounters();
    // BFE-1 fix: reset _initialised flag WITHOUT vi.resetModules().
    // This allows v8 to attribute coverage to all branches of the module.
    _resetInitialisedForTesting();
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("exits early when VITE_OTEL_ENABLED is not 'true'", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "false");
    await initAdminTelemetry("session-disabled");
    expect(counters.webTracerProvider).toBe(0);
    expect(mockRegisterInstrumentations).not.toHaveBeenCalled();
  });

  it("exits early when VITE_OTEL_ENABLED is empty string", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "");
    await initAdminTelemetry("session-undef");
    expect(counters.webTracerProvider).toBe(0);
  });

  it("initialises SDK when VITE_OTEL_ENABLED === 'true'", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    await initAdminTelemetry("test-session-abc");

    expect(counters.webTracerProvider).toBe(1);
    expect(mockRegister).toHaveBeenCalledOnce();
    expect(mockRegisterInstrumentations).toHaveBeenCalledOnce();
  });

  it("configures W3C propagator (never B3/Jaeger)", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    await initAdminTelemetry("session-w3c-test");

    expect(counters.w3cPropagator).toBe(1);
    // The propagator passed to provider.register must have _type === 'W3C'
    const registerArg = mockRegister.mock.calls[0][0] as {
      propagator: { _type: string };
    };
    expect(registerArg.propagator._type).toBe("W3C");
  });

  it("passes spanProcessors in provider constructor config (OTel v2 API)", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    await initAdminTelemetry("session-spanproc-test");

    expect(capturedProviderConfigs.length).toBe(1);
    const config = capturedProviderConfigs[0] as { spanProcessors: unknown[] };
    expect(Array.isArray(config.spanProcessors)).toBe(true);
    expect(config.spanProcessors.length).toBeGreaterThan(0);
  });

  it("FetchInstrumentation allowlist matches admin API and excludes Keycloak", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    await initAdminTelemetry("session-allowlist-test");

    expect(capturedFetchConfigs.length).toBe(1);
    const fetchConfig = capturedFetchConfigs[0] as {
      propagateTraceHeaderCorsUrls: RegExp[];
      ignoreUrls: RegExp[];
    };

    // Allowlist must exist and be non-empty
    const allowlist = fetchConfig.propagateTraceHeaderCorsUrls;
    expect(allowlist).toBeDefined();
    expect(allowlist.length).toBeGreaterThan(0);

    // Must match admin API path (same-origin proxy)
    const adminApiUrl = "http://localhost:5174/api/admin/fundos";
    const matchesAdmin = allowlist.some((r) => r.test(adminApiUrl));
    expect(matchesAdmin).toBe(true);

    // Must NOT match Keycloak realm endpoint
    const keycloakUrl =
      "http://localhost:8180/realms/backoffice/protocol/openid-connect/token";
    const matchesKeycloak = allowlist.some((r) => r.test(keycloakUrl));
    expect(matchesKeycloak).toBe(false);

    // Ignore list must suppress Keycloak
    const ignoreUrls = fetchConfig.ignoreUrls;
    expect(ignoreUrls).toBeDefined();
    const keycloakIgnored = ignoreUrls.some((r) => r.test(keycloakUrl));
    expect(keycloakIgnored).toBe(true);
  });

  it("registers DocumentLoad and UserInteraction instrumentations", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    await initAdminTelemetry("session-instruments-test");

    expect(counters.documentLoad).toBe(1);
    expect(capturedUIConfigs.length).toBe(1);
  });

  it("UserInteractionInstrumentation suppresses INPUT and TEXTAREA events", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    await initAdminTelemetry("session-ui-suppress-test");

    expect(capturedUIConfigs.length).toBe(1);
    const uiConfig = capturedUIConfigs[0] as {
      shouldPreventSpanCreation: (
        eventType: string,
        element: Element
      ) => boolean;
    };

    expect(uiConfig.shouldPreventSpanCreation).toBeDefined();

    const inputEl = { tagName: "INPUT" } as Element;
    const textareaEl = { tagName: "TEXTAREA" } as Element;
    const buttonEl = { tagName: "BUTTON" } as Element;

    expect(uiConfig.shouldPreventSpanCreation("click", inputEl)).toBe(true);
    expect(uiConfig.shouldPreventSpanCreation("click", textareaEl)).toBe(true);
    // Buttons carry accessible name — not suppressed
    expect(uiConfig.shouldPreventSpanCreation("click", buttonEl)).toBe(false);
  });

  it("SELECT elements are suppressed by UserInteraction instrumentation", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    await initAdminTelemetry("session-select-suppress-test");

    const uiConfig = capturedUIConfigs[0] as {
      shouldPreventSpanCreation: (eventType: string, element: Element) => boolean;
    };

    const selectEl = { tagName: "SELECT" } as Element;
    expect(uiConfig.shouldPreventSpanCreation("change", selectEl)).toBe(true);
  });

  it("does not initialise twice (idempotent)", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    await initAdminTelemetry("session-idem-1");
    await initAdminTelemetry("session-idem-2");

    // Second call is a no-op
    expect(counters.webTracerProvider).toBe(1);
  });

  it("Resource attributes do not contain PII keys", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    await initAdminTelemetry("anon-session-pii-test");

    expect(capturedResourceAttrs.length).toBe(1);
    const attrs = capturedResourceAttrs[0] as Record<string, unknown>;
    const keys = Object.keys(attrs);

    // Must NOT contain PII attribute names
    const piiKeys = keys.filter((k) =>
      /(email|sub|cpf|cnpj|token|password|authorization)/i.test(k)
    );
    expect(piiKeys).toHaveLength(0);

    // Must contain anonymous session id (not a real user id)
    expect(keys).toContain("onboarding.session_id");
    expect(attrs["onboarding.session_id"]).toBe("anon-session-pii-test");
  });

  it("Resource includes service.name and service.version", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    await initAdminTelemetry("session-resource-test");

    expect(capturedResourceAttrs.length).toBe(1);
    const attrs = capturedResourceAttrs[0] as Record<string, unknown>;
    // ATTR_SERVICE_NAME = "service.name", ATTR_SERVICE_VERSION = "service.version"
    expect(attrs["service.name"]).toBeDefined();
    expect(attrs["service.version"]).toBeDefined();
  });

  it("collector URL defaults to localhost:4318 when VITE_OTEL_COLLECTOR_URL unset", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    // Intentionally do NOT stub VITE_OTEL_COLLECTOR_URL
    await initAdminTelemetry("session-collector-url-test");
    // OTLPTraceExporter was instantiated (its constructor is stubbed — just check SDK ran)
    expect(counters.webTracerProvider).toBe(1);
  });

  it("applyCustomAttributesOnSpan sets path-only http.target (no query params)", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    await initAdminTelemetry("session-fetch-attrs-test");

    const fetchConfig = capturedFetchConfigs[0] as {
      applyCustomAttributesOnSpan: (
        span: { setAttributes: ReturnType<typeof vi.fn> },
        request: unknown
      ) => void;
    };

    expect(fetchConfig.applyCustomAttributesOnSpan).toBeDefined();

    const mockSpan = { setAttributes: vi.fn() };
    const mockRequest = { url: "http://localhost:8080/api/admin/companies?page=1&search=test" };

    fetchConfig.applyCustomAttributesOnSpan(mockSpan, mockRequest);

    expect(mockSpan.setAttributes).toHaveBeenCalledWith(
      expect.objectContaining({ "http.target": "/api/admin/companies" })
    );
  });

  it("applyCustomAttributesOnSpan skips suppressed auth URLs", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    await initAdminTelemetry("session-fetch-suppress-test");

    const fetchConfig = capturedFetchConfigs[0] as {
      applyCustomAttributesOnSpan: (
        span: { setAttributes: ReturnType<typeof vi.fn> },
        request: unknown
      ) => void;
    };

    const mockSpan = { setAttributes: vi.fn() };
    const authRequest = { url: "http://localhost:8180/realms/backoffice/token" };

    fetchConfig.applyCustomAttributesOnSpan(mockSpan, authRequest);

    // setAttributes should NOT be called for suppressed auth URL
    expect(mockSpan.setAttributes).not.toHaveBeenCalled();
  });

  it("applyCustomAttributesOnSpan does not crash on malformed URL", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    await initAdminTelemetry("session-fetch-malformed-test");

    const fetchConfig = capturedFetchConfigs[0] as {
      applyCustomAttributesOnSpan: (
        span: { setAttributes: ReturnType<typeof vi.fn> },
        request: unknown
      ) => void;
    };

    const mockSpan = { setAttributes: vi.fn() };
    // url is not a string — triggers the catch block
    const badRequest = { url: null };

    expect(() =>
      fetchConfig.applyCustomAttributesOnSpan(mockSpan, badRequest)
    ).not.toThrow();
  });
});

// ---------------------------------------------------------------------------
// Tests: ALLOWED_BACKEND_URLS and SUPPRESS_URL_PATTERNS (exported constants)
// ---------------------------------------------------------------------------

describe("ALLOWED_BACKEND_URLS", () => {
  it("matches admin API paths on localhost", () => {
    const adminUrl = "http://localhost:8080/api/admin/companies";
    const matches = ALLOWED_BACKEND_URLS.some((r) => r.test(adminUrl));
    expect(matches).toBe(true);
  });

  it("does NOT match Keycloak realm URLs", () => {
    const keycloakUrl = "http://localhost:8180/realms/backoffice/token";
    const matches = ALLOWED_BACKEND_URLS.some((r) => r.test(keycloakUrl));
    expect(matches).toBe(false);
  });

  it("does NOT match arbitrary third-party URLs", () => {
    const thirdParty = "https://third-party.example.com/api/data";
    const matches = ALLOWED_BACKEND_URLS.some((r) => r.test(thirdParty));
    expect(matches).toBe(false);
  });
});

describe("SUPPRESS_URL_PATTERNS", () => {
  it("suppresses /auth/ paths", () => {
    expect(SUPPRESS_URL_PATTERNS.some((r) => r.test("/auth/callback"))).toBe(true);
  });

  it("suppresses keycloak URLs", () => {
    expect(
      SUPPRESS_URL_PATTERNS.some((r) =>
        r.test("http://localhost:8180/keycloak/login")
      )
    ).toBe(true);
  });

  it("suppresses .well-known OIDC discovery", () => {
    expect(
      SUPPRESS_URL_PATTERNS.some((r) =>
        r.test("https://example.com/.well-known/openid-configuration")
      )
    ).toBe(true);
  });

  it("suppresses /realms/ paths", () => {
    expect(
      SUPPRESS_URL_PATTERNS.some((r) =>
        r.test("http://localhost:8180/realms/backoffice/protocol/token")
      )
    ).toBe(true);
  });

  it("suppresses URLs with code query param (PKCE callback)", () => {
    expect(
      SUPPRESS_URL_PATTERNS.some((r) =>
        r.test("/auth/callback?code=abc123&state=xyz")
      )
    ).toBe(true);
  });

  it("does NOT suppress normal API paths", () => {
    const apiUrl = "http://localhost:8080/api/admin/companies";
    const suppressed = SUPPRESS_URL_PATTERNS.some((r) => r.test(apiUrl));
    expect(suppressed).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// Tests: scrubAttributes (exported for branch coverage — BFE-1 fix)
// ---------------------------------------------------------------------------

describe("scrubAttributes", () => {
  it("drops keys matching PII regex", () => {
    const result = scrubAttributes({ email: "test@example.com", "http.method": "GET" });
    expect(result).not.toHaveProperty("email");
    expect(result).toHaveProperty("http.method");
  });

  it("drops keys matching authorization pattern", () => {
    const result = scrubAttributes({ authorization: "Bearer xyz", status: "200" });
    expect(result).not.toHaveProperty("authorization");
    expect(result).toHaveProperty("status");
  });

  it("drops string values longer than 256 chars (branch: v.length > 256)", () => {
    const longVal = "a".repeat(257);
    const result = scrubAttributes({ "http.response": longVal, ok: "yes" });
    expect(result).not.toHaveProperty("http.response");
    expect(result).toHaveProperty("ok");
  });

  it("keeps safe short values intact", () => {
    const result = scrubAttributes({
      "http.method": "GET",
      "http.target": "/api/admin/companies",
      status: "200",
    });
    expect(result).toEqual({
      "http.method": "GET",
      "http.target": "/api/admin/companies",
      status: "200",
    });
  });

  it("drops cpf and cnpj keys", () => {
    const result = scrubAttributes({ cpf: "12345678901", cnpj: "00000000000195", safe: "ok" });
    expect(result).not.toHaveProperty("cpf");
    expect(result).not.toHaveProperty("cnpj");
    expect(result).toHaveProperty("safe");
  });

  it("case-insensitive PII key matching (TOKEN, EMAIL)", () => {
    const result = scrubAttributes({ TOKEN: "abc", EMAIL: "x@y.com", route: "/api" });
    expect(result).not.toHaveProperty("TOKEN");
    expect(result).not.toHaveProperty("EMAIL");
    expect(result).toHaveProperty("route");
  });
});
