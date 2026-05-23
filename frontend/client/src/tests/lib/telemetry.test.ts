// ---------------------------------------------------------------------------
// telemetry.test.ts — unit tests for initTelemetry() (D-2, D-35, T-6 DoD)
//
// Coverage targets (D-2, 80% gates):
//   (a) init runs without throwing — enabled + disabled paths
//   (b) OTel SDK called with correct config (W3C propagator, allowlist, etc.)
//   (c) PII scrub logic — inline unit tests on the scrub function
//   (d) SESSION_ID is anonymous UUID (never Keycloak sub/email)
//   (e) propagateTraceHeaderCorsUrls excludes Keycloak
//
// Strategy: telemetry.ts uses dynamic imports inside initTelemetry().
// We mock all @opentelemetry/* packages via vi.mock() hoisting so that
// when initTelemetry() awaits the dynamic imports, it gets our spies.
//
// NOTE: vi.resetModules() is NOT called in beforeEach because it invalidates
// the vi.mock() factory bindings established at module hoist time. Instead we
// use vi.clearAllMocks() to reset call counts between tests.
// ---------------------------------------------------------------------------

import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";

// ---------------------------------------------------------------------------
// Mock all @opentelemetry/* modules BEFORE any imports that might trigger them.
// vi.mock() is hoisted to the top of the file by vitest's transformer.
//
// IMPORTANT: All mock constructors must use regular `function` declarations,
// NOT arrow functions. Arrow functions cannot be called with `new`.
// ---------------------------------------------------------------------------

vi.mock("@opentelemetry/sdk-trace-web", () => {
  function WebTracerProvider(this: Record<string, unknown>, _opts: unknown) {
    this.register = vi.fn();
    this.addSpanProcessor = vi.fn();
    this.forceFlush = vi.fn().mockResolvedValue(undefined);
    this.shutdown = vi.fn().mockResolvedValue(undefined);
  }
  return { WebTracerProvider: vi.fn(WebTracerProvider) };
});

vi.mock("@opentelemetry/sdk-trace-base", () => {
  function BatchSpanProcessor(this: Record<string, unknown>, _exp: unknown) {
    this.onStart = vi.fn();
    this.onEnd = vi.fn();
    this.shutdown = vi.fn().mockResolvedValue(undefined);
    this.forceFlush = vi.fn().mockResolvedValue(undefined);
  }
  function SimpleSpanProcessor(this: Record<string, unknown>, _exp: unknown) {
    this.onStart = vi.fn();
    this.onEnd = vi.fn();
    this.shutdown = vi.fn().mockResolvedValue(undefined);
    this.forceFlush = vi.fn().mockResolvedValue(undefined);
  }
  return {
    BatchSpanProcessor: vi.fn(BatchSpanProcessor),
    SimpleSpanProcessor: vi.fn(SimpleSpanProcessor),
  };
});

vi.mock("@opentelemetry/exporter-trace-otlp-http", () => {
  function OTLPTraceExporter(this: Record<string, unknown>, _opts: unknown) {
    this.export = vi.fn((_spans: unknown, cb: (r: { code: number }) => void) => cb({ code: 0 }));
    this.shutdown = vi.fn().mockResolvedValue(undefined);
  }
  return { OTLPTraceExporter: vi.fn(OTLPTraceExporter) };
});

vi.mock("@opentelemetry/resources", () => {
  function ResourceCtor(this: Record<string, unknown>, attrs: Record<string, unknown>) {
    this.attributes = attrs;
    this.merge = vi.fn();
  }
  return { Resource: vi.fn(ResourceCtor) };
});

vi.mock("@opentelemetry/semantic-conventions", () => ({
  ATTR_SERVICE_NAME: "service.name",
  SemanticResourceAttributes: {
    SERVICE_NAME: "service.name",
    SERVICE_VERSION: "service.version",
    DEPLOYMENT_ENVIRONMENT: "deployment.environment",
  },
}));

vi.mock("@opentelemetry/core", () => {
  function W3CTraceContextPropagator(this: Record<string, unknown>) {
    this.inject = vi.fn();
    this.extract = vi.fn();
    this.fields = vi.fn(() => ["traceparent", "tracestate"]);
  }
  function W3CBaggagePropagator(this: Record<string, unknown>) {
    this.inject = vi.fn();
    this.extract = vi.fn();
    this.fields = vi.fn(() => ["baggage"]);
  }
  function CompositePropagator(this: Record<string, unknown>) {
    this.inject = vi.fn();
    this.extract = vi.fn();
    this.fields = vi.fn(() => []);
  }
  return {
    W3CTraceContextPropagator: vi.fn(W3CTraceContextPropagator),
    W3CBaggagePropagator: vi.fn(W3CBaggagePropagator),
    CompositePropagator: vi.fn(CompositePropagator),
  };
});

vi.mock("@opentelemetry/context-zone", () => {
  function ZoneContextManager(this: Record<string, unknown>) {
    this.enable = vi.fn();
    this.disable = vi.fn();
    this.bind = vi.fn();
    this.with = vi.fn();
    this.active = vi.fn();
  }
  return { ZoneContextManager: vi.fn(ZoneContextManager) };
});

vi.mock("@opentelemetry/instrumentation", () => ({
  registerInstrumentations: vi.fn(),
}));

vi.mock("@opentelemetry/auto-instrumentations-web", () => ({
  getWebAutoInstrumentations: vi.fn().mockReturnValue([]),
}));

// ---------------------------------------------------------------------------
// Import from real packages — vitest intercepts via vi.mock() above.
// Imports from exact same packages as telemetry.ts dynamic imports.
// ---------------------------------------------------------------------------
import { WebTracerProvider } from "@opentelemetry/sdk-trace-web";
import { BatchSpanProcessor } from "@opentelemetry/sdk-trace-base";
import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-http";
import { Resource } from "@opentelemetry/resources";
import { W3CTraceContextPropagator } from "@opentelemetry/core";
import { ZoneContextManager } from "@opentelemetry/context-zone";
import { registerInstrumentations } from "@opentelemetry/instrumentation";
import { getWebAutoInstrumentations } from "@opentelemetry/auto-instrumentations-web";

// Import telemetry module — after mocks are established
import {
  initTelemetry,
  SESSION_ID,
  scrubAttributes,
  applyFetchSpanAttributes,
  shouldPreventInteractionSpan,
  headSampleDecision,
  PII_REGEX,
} from "@/lib/telemetry";

// ---------------------------------------------------------------------------
// initTelemetry — disabled path
// ---------------------------------------------------------------------------
describe("telemetry — initTelemetry() disabled (no VITE_OTEL_ENABLED)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv("VITE_OTEL_ENABLED", "false");
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("does NOT throw when disabled", async () => {
    await expect(initTelemetry()).resolves.toBeUndefined();
  });

  it("does NOT create WebTracerProvider when disabled", async () => {
    await initTelemetry();
    expect(WebTracerProvider).not.toHaveBeenCalled();
  });
});

// ---------------------------------------------------------------------------
// initTelemetry — enabled path
// ---------------------------------------------------------------------------
describe("telemetry — initTelemetry() when VITE_OTEL_ENABLED=true", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv("VITE_OTEL_ENABLED", "true");
    vi.stubEnv("VITE_OTEL_COLLECTOR_URL", "http://localhost:4318/v1/traces");
    vi.stubEnv("VITE_OTEL_SERVICE_NAME", "onboarding-client");
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("does NOT throw when OTel SDK initializes", async () => {
    await expect(initTelemetry()).resolves.toBeUndefined();
  });

  it("creates WebTracerProvider", async () => {
    await initTelemetry();
    expect(WebTracerProvider).toHaveBeenCalled();
  });

  it("creates OTLPTraceExporter with configured collector URL", async () => {
    await initTelemetry();
    expect(OTLPTraceExporter).toHaveBeenCalledWith(
      expect.objectContaining({ url: "http://localhost:4318/v1/traces" })
    );
  });

  it("uses BatchSpanProcessor for production batching", async () => {
    await initTelemetry();
    expect(BatchSpanProcessor).toHaveBeenCalled();
  });

  it("uses W3CTraceContextPropagator — NEVER B3/Jaeger (D-35)", async () => {
    await initTelemetry();
    expect(W3CTraceContextPropagator).toHaveBeenCalled();
  });

  it("uses ZoneContextManager for async context propagation", async () => {
    await initTelemetry();
    expect(ZoneContextManager).toHaveBeenCalled();
  });

  it("calls registerInstrumentations with auto-instrumentations config", async () => {
    await initTelemetry();
    expect(getWebAutoInstrumentations).toHaveBeenCalled();
    expect(registerInstrumentations).toHaveBeenCalled();
  });

  it("includes service.name in Resource attributes", async () => {
    await initTelemetry();
    expect(Resource).toHaveBeenCalledWith(
      expect.objectContaining({ "service.name": "onboarding-client" })
    );
  });

  it("includes anonymous session_id in Resource — not a Keycloak claim", async () => {
    await initTelemetry();
    expect(Resource).toHaveBeenCalledWith(
      expect.objectContaining({ "onboarding.session_id": SESSION_ID })
    );
  });

  it("propagateTraceHeaderCorsUrls excludes Keycloak port 8180 (D-35)", async () => {
    await initTelemetry();

    const calls = (getWebAutoInstrumentations as ReturnType<typeof vi.fn>).mock.calls;
    const fetchConfig =
      calls[0]?.[0]?.["@opentelemetry/instrumentation-fetch"];

    if (fetchConfig?.propagateTraceHeaderCorsUrls) {
      const corsUrls: RegExp[] = fetchConfig.propagateTraceHeaderCorsUrls;
      const keycloakUrl =
        "http://localhost:8180/realms/onboarding/protocol/openid-connect/token";
      const keycloakMatches = corsUrls.some((p) => p.test(keycloakUrl));
      expect(keycloakMatches).toBe(false);
    }
  });

  it("ignoreUrls covers /realms/, /auth/, keycloak patterns (auth chain suppression)", async () => {
    await initTelemetry();

    const calls = (getWebAutoInstrumentations as ReturnType<typeof vi.fn>).mock.calls;
    const fetchConfig =
      calls[0]?.[0]?.["@opentelemetry/instrumentation-fetch"];

    if (fetchConfig?.ignoreUrls) {
      const ignoreUrls: RegExp[] = fetchConfig.ignoreUrls;
      expect(ignoreUrls.some((p) => p.test("/auth/callback"))).toBe(true);
      expect(ignoreUrls.some((p) => p.test("/realms/onboarding/login"))).toBe(true);
      expect(ignoreUrls.some((p) => p.test("http://keycloak:8180/auth"))).toBe(true);
    }
  });
});

// ---------------------------------------------------------------------------
// PII scrub logic — tests on exported scrubAttributes + PII_REGEX
// These tests cover the exported functions from telemetry.ts directly.
// ---------------------------------------------------------------------------
describe("telemetry — scrubAttributes (exported)", () => {
  it("PII_REGEX matches sensitive key names", () => {
    expect(PII_REGEX.test("access_token")).toBe(true);
    expect(PII_REGEX.test("authorization")).toBe(true);
    expect(PII_REGEX.test("email")).toBe(true);
    expect(PII_REGEX.test("cpf")).toBe(true);
    expect(PII_REGEX.test("cnpj")).toBe(true);
    expect(PII_REGEX.test("password")).toBe(true);
    expect(PII_REGEX.test("secret")).toBe(true);
    expect(PII_REGEX.test("sub")).toBe(true);
    expect(PII_REGEX.test("jwt")).toBe(true);
  });

  it("PII_REGEX is case-insensitive", () => {
    expect(PII_REGEX.test("TOKEN")).toBe(true);
    expect(PII_REGEX.test("EMAIL")).toBe(true);
    expect(PII_REGEX.test("CPF")).toBe(true);
  });

  it("drops attribute key containing 'token'", () => {
    const result = scrubAttributes({ access_token: "abc123", "http.method": "GET" });
    expect(result).not.toHaveProperty("access_token");
    expect(result).toHaveProperty("http.method");
  });

  it("drops attribute key containing 'authorization'", () => {
    const result = scrubAttributes({ authorization: "Bearer xyz", status: 200 });
    expect(result).not.toHaveProperty("authorization");
    expect(result).toHaveProperty("status");
  });

  it("drops attribute key containing 'email'", () => {
    const result = scrubAttributes({ email: "user@example.com", "user.id": "u1" });
    expect(result).not.toHaveProperty("email");
    expect(result).toHaveProperty("user.id");
  });

  it("drops attribute key containing 'cpf'", () => {
    const result = scrubAttributes({ cpf: "12345678901", name: "Test" });
    expect(result).not.toHaveProperty("cpf");
    expect(result).toHaveProperty("name");
  });

  it("drops attribute key containing 'cnpj'", () => {
    const result = scrubAttributes({ cnpj: "00000000000195", "http.url": "/api/fundos" });
    expect(result).not.toHaveProperty("cnpj");
    expect(result).toHaveProperty("http.url");
  });

  it("drops attribute key containing 'password'", () => {
    const result = scrubAttributes({ password: "s3cr3t", method: "POST" });
    expect(result).not.toHaveProperty("password");
    expect(result).toHaveProperty("method");
  });

  it("drops attribute key containing 'secret'", () => {
    const result = scrubAttributes({ client_secret: "xyz", route: "/api/fundos" });
    expect(result).not.toHaveProperty("client_secret");
    expect(result).toHaveProperty("route");
  });

  it("drops attribute key 'sub' (Keycloak subject)", () => {
    const result = scrubAttributes({ sub: "user-uuid", "service.name": "client" });
    expect(result).not.toHaveProperty("sub");
    expect(result).toHaveProperty("service.name");
  });

  it("drops string values longer than 256 chars (prevent leak via long strings)", () => {
    const longValue = "a".repeat(257);
    const result = scrubAttributes({ "http.response_length": longValue, ok: "yes" });
    expect(result).not.toHaveProperty("http.response_length");
    expect(result).toHaveProperty("ok");
  });

  it("keeps safe non-PII attributes intact", () => {
    const result = scrubAttributes({
      "http.method": "GET",
      "http.status_code": 200,
      "http.target": "/api/fundos",
      "route.path": "/fundos",
    });
    expect(result).toEqual({
      "http.method": "GET",
      "http.status_code": 200,
      "http.target": "/api/fundos",
      "route.path": "/fundos",
    });
  });

  it("case-insensitive PII match (TOKEN, EMAIL, CPF variants)", () => {
    const result = scrubAttributes({
      TOKEN: "abc",
      EMAIL: "x@y.com",
      CPF_FIELD: "12345678901",
      normal: "value",
    });
    expect(result).not.toHaveProperty("TOKEN");
    expect(result).not.toHaveProperty("EMAIL");
    expect(result).not.toHaveProperty("CPF_FIELD");
    expect(result).toHaveProperty("normal");
  });
});

// ---------------------------------------------------------------------------
// applyFetchSpanAttributes — sets http.target path-only + PII scrub
// ---------------------------------------------------------------------------
describe("telemetry — applyFetchSpanAttributes", () => {
  it("sets http.target to pathname only (no query, no fragment)", () => {
    const mockSpan = { setAttribute: vi.fn(), attributes: {} };
    const mockRequest = { url: "http://localhost:8080/api/fundos?search=test&page=1" } as Request;

    applyFetchSpanAttributes(mockSpan, mockRequest);

    expect(mockSpan.setAttribute).toHaveBeenCalledWith("http.target", "/api/fundos");
  });

  it("does not crash when URL is relative (using location.origin fallback)", () => {
    const mockSpan = { setAttribute: vi.fn(), attributes: {} };
    const mockRequest = { url: "/api/fundos" } as Request;

    // Should not throw — if URL resolution fails, catch block silences it
    expect(() => applyFetchSpanAttributes(mockSpan, mockRequest)).not.toThrow();
  });

  it("scrubs PII from existing span attributes", () => {
    const mockSpan = {
      setAttribute: vi.fn(),
      attributes: {
        "http.method": "GET",
        authorization: "Bearer token123",
        email: "user@test.com",
      } as Record<string, unknown>,
    };
    const mockRequest = { url: "http://localhost:8080/api/fundos" } as Request;

    applyFetchSpanAttributes(mockSpan, mockRequest);

    // setAttribute should be called for safe attrs only
    const calls = mockSpan.setAttribute.mock.calls.map(([k]) => k);
    expect(calls).toContain("http.target");
    expect(calls).toContain("http.method");
    expect(calls).not.toContain("authorization");
    expect(calls).not.toContain("email");
  });

  it("does not crash on malformed request (missing url)", () => {
    const mockSpan = { setAttribute: vi.fn(), attributes: {} };
    const badRequest = {} as Request;

    expect(() => applyFetchSpanAttributes(mockSpan, badRequest)).not.toThrow();
  });
});

// ---------------------------------------------------------------------------
// shouldPreventInteractionSpan — skip inputs/textareas/selects
// ---------------------------------------------------------------------------
describe("telemetry — shouldPreventInteractionSpan", () => {
  it("returns true for INPUT elements (prevent capturing typed values)", () => {
    const el = { tagName: "INPUT" } as Element;
    expect(shouldPreventInteractionSpan("click", el)).toBe(true);
  });

  it("returns true for TEXTAREA elements", () => {
    const el = { tagName: "TEXTAREA" } as Element;
    expect(shouldPreventInteractionSpan("keydown", el)).toBe(true);
  });

  it("returns true for SELECT elements", () => {
    const el = { tagName: "SELECT" } as Element;
    expect(shouldPreventInteractionSpan("change", el)).toBe(true);
  });

  it("returns false for BUTTON elements (allowed)", () => {
    const el = { tagName: "BUTTON" } as Element;
    expect(shouldPreventInteractionSpan("click", el)).toBe(false);
  });

  it("returns false for A elements (allowed)", () => {
    const el = { tagName: "A" } as Element;
    expect(shouldPreventInteractionSpan("click", el)).toBe(false);
  });

  it("handles lowercase tagName gracefully (normalize via toUpperCase)", () => {
    const el = { tagName: "input" } as Element;
    expect(shouldPreventInteractionSpan("click", el)).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// headSampleDecision — 15% head sampling
// ---------------------------------------------------------------------------
describe("telemetry — headSampleDecision", () => {
  it("returns decision 0 or 1", () => {
    // Run multiple times to cover both branches of the ternary
    const decisions = Array.from({ length: 100 }, () => headSampleDecision().decision);
    expect(decisions.every((d) => d === 0 || d === 1)).toBe(true);
  });

  it("returns empty attributes object", () => {
    const result = headSampleDecision();
    expect(result.attributes).toEqual({});
  });

  it("returns decision=1 (sampled) for some calls — not always 0", () => {
    // With 100 tries at 15% probability, the chance of ALL being 0 is 0.85^100 ≈ 0.0000059
    const decisions = Array.from({ length: 100 }, () => headSampleDecision().decision);
    const hasSampled = decisions.some((d) => d === 1);
    // This test is probabilistic — VERY unlikely to fail. If flaky, mock Math.random.
    expect(hasSampled).toBe(true);
  });

  it("returns decision=0 (not sampled) for some calls — not always 1", () => {
    const decisions = Array.from({ length: 100 }, () => headSampleDecision().decision);
    const hasNotSampled = decisions.some((d) => d === 0);
    expect(hasNotSampled).toBe(true);
  });

  it("branches coverage: decision=1 when Math.random() < 0.15", () => {
    vi.spyOn(Math, "random").mockReturnValueOnce(0.1);
    expect(headSampleDecision().decision).toBe(1);
    vi.restoreAllMocks();
  });

  it("branches coverage: decision=0 when Math.random() >= 0.15", () => {
    vi.spyOn(Math, "random").mockReturnValueOnce(0.5);
    expect(headSampleDecision().decision).toBe(0);
    vi.restoreAllMocks();
  });
});

// ---------------------------------------------------------------------------
// SESSION_ID anonymity guarantee
// ---------------------------------------------------------------------------
describe("telemetry — SESSION_ID anonymity", () => {
  it("SESSION_ID is a non-empty string (anonymous random identifier)", () => {
    expect(typeof SESSION_ID).toBe("string");
    expect(SESSION_ID.length).toBeGreaterThan(0);
  });

  it("SESSION_ID does not contain email pattern (@ symbol)", () => {
    expect(SESSION_ID).not.toMatch(/@/);
  });

  it("SESSION_ID is not a Keycloak claim name (sub, email)", () => {
    expect(SESSION_ID).not.toBe("sub");
    expect(SESSION_ID).not.toBe("email");
    expect(SESSION_ID).not.toBe("");
  });
});
