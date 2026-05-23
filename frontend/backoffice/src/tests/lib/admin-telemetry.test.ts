// ---------------------------------------------------------------------------
// admin-telemetry.test.ts
// ---------------------------------------------------------------------------
// Unit tests for the backoffice OTel telemetry initialiser (OTel SDK v2).
// Uses vi.mock to replace the heavy OTel SDK with lightweight stubs so that
// (a) the test suite does not depend on a live collector, and
// (b) we can assert configuration choices (propagator, allowlist, PII scrub).
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
// Spies exposed for assertion
// ---------------------------------------------------------------------------

const mockRegister = vi.fn();

// Captured config objects from constructor calls
const capturedProviderConfigs: unknown[] = [];
const capturedResourceAttrs: Record<string, unknown>[] = [];
const capturedFetchConfigs: unknown[] = [];
const capturedUIConfigs: unknown[] = [];

let w3cPropagatorCallCount = 0;
let documentLoadCallCount = 0;
let webTracerProviderCallCount = 0;
const mockRegisterInstrumentations = vi.fn();

// ---------------------------------------------------------------------------
// Mock factories — OTel v2 API (spanProcessors in constructor, no addSpanProcessor)
// ---------------------------------------------------------------------------

vi.mock("@opentelemetry/sdk-trace-web", () => {
  class WebTracerProvider {
    constructor(config?: unknown) {
      webTracerProviderCallCount++;
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
      w3cPropagatorCallCount++;
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
      documentLoadCallCount++;
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
  w3cPropagatorCallCount = 0;
  documentLoadCallCount = 0;
  webTracerProviderCallCount = 0;
}

// ---------------------------------------------------------------------------
// Tests: generateAnonymousSessionId
// ---------------------------------------------------------------------------

describe("generateAnonymousSessionId", () => {
  it("returns a valid UUID v4 string", async () => {
    const { generateAnonymousSessionId } = await import(
      "@/lib/admin-telemetry"
    );
    const id = generateAnonymousSessionId();
    expect(id).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
    );
  });

  it("produces a different id on each call (entropy check)", async () => {
    const { generateAnonymousSessionId } = await import(
      "@/lib/admin-telemetry"
    );
    const ids = new Set(
      Array.from({ length: 10 }, () => generateAnonymousSessionId())
    );
    expect(ids.size).toBe(10);
  });

  it("does NOT write to localStorage (D-12 regression)", async () => {
    const { generateAnonymousSessionId } = await import(
      "@/lib/admin-telemetry"
    );
    const setItemSpy = vi.spyOn(Storage.prototype, "setItem");
    generateAnonymousSessionId();
    expect(setItemSpy).not.toHaveBeenCalled();
    setItemSpy.mockRestore();
  });

  it("does NOT write to sessionStorage (D-12 regression)", async () => {
    const { generateAnonymousSessionId } = await import(
      "@/lib/admin-telemetry"
    );
    // sessionStorage.setItem routes through Storage.prototype.setItem in jsdom
    const setItemSpy = vi.spyOn(Storage.prototype, "setItem");
    generateAnonymousSessionId();
    expect(setItemSpy).not.toHaveBeenCalled();
    setItemSpy.mockRestore();
  });
});

// ---------------------------------------------------------------------------
// Tests: initAdminTelemetry
// ---------------------------------------------------------------------------

describe("initAdminTelemetry", () => {
  beforeEach(() => {
    resetCounters();
    // vi.resetModules() clears the _initialised flag in admin-telemetry.ts
    vi.resetModules();
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("exits early when VITE_OTEL_ENABLED is not 'true'", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "false");
    const { initAdminTelemetry } = await import("@/lib/admin-telemetry");
    await initAdminTelemetry("session-disabled");
    expect(webTracerProviderCallCount).toBe(0);
    expect(mockRegisterInstrumentations).not.toHaveBeenCalled();
  });

  it("initialises SDK when VITE_OTEL_ENABLED === 'true'", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    const { initAdminTelemetry } = await import("@/lib/admin-telemetry");
    await initAdminTelemetry("test-session-abc");

    expect(webTracerProviderCallCount).toBe(1);
    expect(mockRegister).toHaveBeenCalledOnce();
    expect(mockRegisterInstrumentations).toHaveBeenCalledOnce();
  });

  it("configures W3C propagator (never B3/Jaeger)", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    const { initAdminTelemetry } = await import("@/lib/admin-telemetry");
    await initAdminTelemetry("session-w3c-test");

    expect(w3cPropagatorCallCount).toBe(1);
    // The propagator passed to provider.register must have _type === 'W3C'
    const registerArg = mockRegister.mock.calls[0][0] as {
      propagator: { _type: string };
    };
    expect(registerArg.propagator._type).toBe("W3C");
  });

  it("passes spanProcessors in provider constructor config (OTel v2 API)", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    const { initAdminTelemetry } = await import("@/lib/admin-telemetry");
    await initAdminTelemetry("session-spanproc-test");

    expect(capturedProviderConfigs.length).toBe(1);
    const config = capturedProviderConfigs[0] as { spanProcessors: unknown[] };
    expect(Array.isArray(config.spanProcessors)).toBe(true);
    expect(config.spanProcessors.length).toBeGreaterThan(0);
  });

  it("FetchInstrumentation allowlist matches admin API and excludes Keycloak", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    const { initAdminTelemetry } = await import("@/lib/admin-telemetry");
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
    const { initAdminTelemetry } = await import("@/lib/admin-telemetry");
    await initAdminTelemetry("session-instruments-test");

    expect(documentLoadCallCount).toBe(1);
    expect(capturedUIConfigs.length).toBe(1);
  });

  it("UserInteractionInstrumentation suppresses INPUT and TEXTAREA events", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    const { initAdminTelemetry } = await import("@/lib/admin-telemetry");
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

  it("does not initialise twice (idempotent)", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    const { initAdminTelemetry } = await import("@/lib/admin-telemetry");
    await initAdminTelemetry("session-idem-1");
    await initAdminTelemetry("session-idem-2");

    // Second call is a no-op
    expect(webTracerProviderCallCount).toBe(1);
  });

  it("Resource attributes do not contain PII keys", async () => {
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    const { initAdminTelemetry } = await import("@/lib/admin-telemetry");
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
});
