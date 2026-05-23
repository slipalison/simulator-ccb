// ---------------------------------------------------------------------------
// Vitest mock for all @opentelemetry/* packages.
//
// telemetry.ts uses dynamic imports of OTel SDK — these are intercepted in
// tests via the `alias` config in vitest.config.ts which maps any
// @opentelemetry/* import to this single mock module.
//
// The mock exposes the minimal surface needed to exercise initTelemetry():
//   - WebTracerProvider (spy constructor + register/addSpanProcessor)
//   - BatchSpanProcessor, SimpleSpanProcessor (spy constructors)
//   - OTLPTraceExporter (spy constructor)
//   - Resource (spy constructor)
//   - Semantic conventions constant
//   - W3CTraceContextPropagator (spy constructor)
//   - ZoneContextManager (spy constructor)
//   - registerInstrumentations (spy function)
//   - getWebAutoInstrumentations (returns empty array)
//
// InMemorySpanExporter is also exported so telemetry.test.ts can use it for
// assertion-level tests (span emit shape).
// ---------------------------------------------------------------------------

import { vi } from "vitest";

// ---- Shared in-memory span store (for InMemorySpanExporter assertions) ----
export const _capturedSpans: Array<{ name: string; attributes: Record<string, unknown> }> = [];

export const InMemorySpanExporter = vi.fn().mockImplementation(() => ({
  export: vi.fn(
    (
      spans: Array<{ name: string; attributes: Record<string, unknown> }>,
      cb: (result: { code: number }) => void
    ) => {
      _capturedSpans.push(...spans);
      cb({ code: 0 });
    }
  ),
  shutdown: vi.fn().mockResolvedValue(undefined),
  forceFlush: vi.fn().mockResolvedValue(undefined),
  getFinishedSpans: vi.fn(() => _capturedSpans),
  reset: vi.fn(() => {
    _capturedSpans.length = 0;
  }),
}));

// ---- WebTracerProvider ----
const _mockRegister = vi.fn();
const _mockAddSpanProcessor = vi.fn();

export const WebTracerProvider = vi.fn().mockImplementation(() => ({
  register: _mockRegister,
  addSpanProcessor: _mockAddSpanProcessor,
  forceFlush: vi.fn().mockResolvedValue(undefined),
  shutdown: vi.fn().mockResolvedValue(undefined),
}));

// ---- Span processors / exporters ----
export const BatchSpanProcessor = vi.fn().mockImplementation(() => ({
  onStart: vi.fn(),
  onEnd: vi.fn(),
  shutdown: vi.fn().mockResolvedValue(undefined),
  forceFlush: vi.fn().mockResolvedValue(undefined),
}));

export const SimpleSpanProcessor = vi.fn().mockImplementation(() => ({
  onStart: vi.fn(),
  onEnd: vi.fn(),
  shutdown: vi.fn().mockResolvedValue(undefined),
  forceFlush: vi.fn().mockResolvedValue(undefined),
}));

export const OTLPTraceExporter = vi.fn().mockImplementation(() => ({
  export: vi.fn((_spans: unknown, cb: (r: { code: number }) => void) => cb({ code: 0 })),
  shutdown: vi.fn().mockResolvedValue(undefined),
}));

// ---- Resource ----
export const Resource = vi.fn().mockImplementation((attrs: Record<string, unknown>) => ({
  attributes: attrs,
  merge: vi.fn(),
}));

// ---- Semantic conventions ----
export const ATTR_SERVICE_NAME = "service.name";
export const SemanticResourceAttributes = {
  SERVICE_NAME: "service.name",
  SERVICE_VERSION: "service.version",
  DEPLOYMENT_ENVIRONMENT: "deployment.environment",
};

// ---- Propagators ----
export const W3CTraceContextPropagator = vi.fn().mockImplementation(() => ({
  inject: vi.fn(),
  extract: vi.fn(),
  fields: vi.fn(() => ["traceparent", "tracestate"]),
}));

export const W3CBaggagePropagator = vi.fn().mockImplementation(() => ({
  inject: vi.fn(),
  extract: vi.fn(),
  fields: vi.fn(() => ["baggage"]),
}));

export const CompositePropagator = vi.fn().mockImplementation(() => ({
  inject: vi.fn(),
  extract: vi.fn(),
  fields: vi.fn(() => []),
}));

// ---- Context manager ----
export const ZoneContextManager = vi.fn().mockImplementation(() => ({
  enable: vi.fn(),
  disable: vi.fn(),
  bind: vi.fn(),
  with: vi.fn(),
  active: vi.fn(),
}));

// ---- Instrumentation registration ----
export const registerInstrumentations = vi.fn();

// ---- Auto-instrumentations ----
export const getWebAutoInstrumentations = vi
  .fn()
  .mockReturnValue([]);

// ---- Trace API (used by route change spans) ----
export const trace = {
  getTracer: vi.fn(() => ({
    startSpan: vi.fn(() => ({
      setAttribute: vi.fn(),
      setStatus: vi.fn(),
      end: vi.fn(),
    })),
    startActiveSpan: vi.fn((_name: string, fn: (span: unknown) => unknown) =>
      fn({
        setAttribute: vi.fn(),
        setStatus: vi.fn(),
        end: vi.fn(),
      })
    ),
  })),
};

// ---- Metrics API (for web-vitals adapter future use) ----
export const metrics = {
  getMeter: vi.fn(() => ({
    createHistogram: vi.fn(() => ({ record: vi.fn() })),
    createCounter: vi.fn(() => ({ add: vi.fn() })),
  })),
};

// ---- Logs API ----
export const logs = {
  getLogger: vi.fn(() => ({
    emit: vi.fn(),
  })),
};

// Re-export mocks so tests can assert on them
export { _mockRegister, _mockAddSpanProcessor };
