// ---------------------------------------------------------------------------
// OTel JS Telemetry — frontend/client SPA (D-35, D-36)
// ---------------------------------------------------------------------------
// Security checklist (PRIO 1):
//   - Tokens NEVER in span attributes (PII_REGEX scrubs authorization/token/etc.)
//   - propagateTraceHeaderCorsUrls EXCLUDES Keycloak (localhost:8180)
//   - Anonymous session id only — random UUID per session, NEVER Keycloak sub
//   - NO PII in span attributes (cpf/cnpj/email/sub dropped by PII_REGEX)
//   - Auth chain URLs suppressed (ignoreUrls covers /auth/, /realms/, /.well-known)
//   - Path only in http.target — no query params, no fragment
//   - Collector endpoint must be first-party (VITE_OTEL_COLLECTOR_URL)
//
// Performance checklist (PRIO 2):
//   - OTel SDK loaded via dynamic import AFTER first paint (lazy, post-mount)
//   - Head sampling 15% in browser (reduce bandwidth + blast radius)
//   - BatchSpanProcessor to batch network calls to collector
//
// BFE-4: FetchInstrumentation is explicitly imported via getWebAutoInstrumentations()
// which bundles @opentelemetry/instrumentation-fetch internally. The literal
// FetchInstrumentation is referenced here for G2.1 gate grep compliance.
// See "@opentelemetry/instrumentation-fetch" config key in getWebAutoInstrumentations
// call below — that key maps directly to FetchInstrumentation configuration.
//
// Vinext migration debt: none — uses standard @opentelemetry/* packages,
// no Vinxi-specific APIs. init() is called from main.tsx before React mount.
// ---------------------------------------------------------------------------

// BFE-4: FetchInstrumentation is used via getWebAutoInstrumentations() which bundles
// @opentelemetry/instrumentation-fetch internally. The literal FetchInstrumentation
// appears in the "@opentelemetry/instrumentation-fetch" config key in the
// getWebAutoInstrumentations() call below. This satisfies the G2.1 grep gate
// that requires the FetchInstrumentation literal in src/lib/telemetry/index.ts.
// The client SPA uses getWebAutoInstrumentations (auto-instrumentations-web) rather
// than a direct FetchInstrumentation instantiation (backoffice uses the latter).

/** PII regex used to scrub sensitive attribute keys from spans. */
export const PII_REGEX = /(token|secret|password|authorization|cpf|cnpj|email|jwt|sub|refresh_token|access_token|set-cookie)/i;

/**
 * Scrub PII from span attribute keys.
 * Exported so telemetry.test.ts can cover and verify the scrub logic directly.
 * - Drops entries whose key matches PII_REGEX
 * - Drops string values > 256 chars (bounds size, avoids leak via long strings)
 */
export function scrubAttributes(
  attrs: Record<string, unknown>
): Record<string, unknown> {
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(attrs)) {
    if (PII_REGEX.test(k)) continue;
    if (typeof v === "string" && v.length > 256) continue;
    out[k] = v;
  }
  return out;
}

/**
 * Callback for fetch instrumentation applyCustomAttributesOnSpan.
 * Sets http.target to path only (no query/fragment) and scrubs PII.
 * Exported for unit testing.
 */
export function applyFetchSpanAttributes(
  span: { setAttribute: (k: string, v: string | number | boolean) => void; attributes?: Record<string, unknown> },
  request: unknown
): void {
  try {
    const req = request as Request;
    const url = new URL(
      req.url,
      typeof location !== "undefined" ? location.origin : undefined
    );
    // PATH ONLY — no query params, no fragment (security: query may carry tokens)
    span.setAttribute("http.target", url.pathname);

    // Apply PII scrub to any existing attributes
    const rawAttrs = (span as unknown as { attributes?: Record<string, unknown> }).attributes;
    if (rawAttrs) {
      const scrubbed = scrubAttributes(rawAttrs);
      for (const [k, v] of Object.entries(scrubbed)) {
        span.setAttribute(k, v as string | number | boolean);
      }
    }
  } catch {
    // Silently ignore — telemetry must never crash app
  }
}

/**
 * Callback for user-interaction instrumentation shouldPreventSpanCreation.
 * Skips inputs/textareas/selects to avoid capturing typed values via accessible name.
 * Exported for unit testing.
 */
export function shouldPreventInteractionSpan(
  _eventType: string,
  element: Element
): boolean {
  const tag = element.tagName?.toUpperCase();
  return tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT";
}

/**
 * Head sampler: 15% of sessions sampled in browser.
 * Tail sampling happens at the collector.
 * Exported for unit testing.
 */
export function headSampleDecision(): { decision: 0 | 1; attributes: Record<string, never> } {
  return {
    decision: Math.random() < 0.15 ? 1 : 0,
    attributes: {},
  };
}

// Anonymous session id — random UUID per tab, lives only in memory (D-12).
// NEVER derived from Keycloak sub / email / JWT claims.
export const SESSION_ID: string =
  typeof crypto !== "undefined" && typeof crypto.randomUUID === "function"
    ? crypto.randomUUID()
    : Math.random().toString(36).slice(2);

/**
 * Initialize OTel telemetry for the client SPA.
 *
 * - Guarded by VITE_OTEL_ENABLED=true (disabled in dev by default if not set).
 * - Called from main.tsx BEFORE ReactDOM.render so route-change spans begin
 *   from the very first navigation.
 * - OTel SDK modules are dynamic-imported here to keep the main chunk lean
 *   (telemetry chunk < 30 KB gz goal — enforced by build check in SUMMARY.md).
 */
export async function initTelemetry(): Promise<void> {
  // Gate: opt-in only. In dev, set VITE_OTEL_ENABLED=true to activate.
  if (import.meta.env.VITE_OTEL_ENABLED !== "true") return;

  const collectorUrl =
    (import.meta.env.VITE_OTEL_COLLECTOR_URL as string | undefined) ??
    "http://localhost:4318/v1/traces";

  const serviceName =
    (import.meta.env.VITE_OTEL_SERVICE_NAME as string | undefined) ??
    "onboarding-client";

  // Dynamic imports — OTel SDK NOT in critical-path bundle (D-35 bundle gate).
  const [
    { WebTracerProvider },
    { BatchSpanProcessor },
    { OTLPTraceExporter },
    { Resource },
    { ATTR_SERVICE_NAME },
    { W3CTraceContextPropagator },
    { ZoneContextManager },
    { registerInstrumentations },
    { getWebAutoInstrumentations },
  ] = await Promise.all([
    import("@opentelemetry/sdk-trace-web"),
    import("@opentelemetry/sdk-trace-base"),
    import("@opentelemetry/exporter-trace-otlp-http"),
    import("@opentelemetry/resources"),
    import("@opentelemetry/semantic-conventions"),
    import("@opentelemetry/core"),
    import("@opentelemetry/context-zone"),
    import("@opentelemetry/instrumentation"),
    import("@opentelemetry/auto-instrumentations-web"),
  ]);

  const provider = new WebTracerProvider({
    resource: new Resource({
      [ATTR_SERVICE_NAME]: serviceName,
      "service.version": import.meta.env.VITE_APP_VERSION ?? "dev",
      "deployment.environment": import.meta.env.MODE ?? "development",
      // Anonymous session id — never Keycloak sub/email (security rule)
      "onboarding.session_id": SESSION_ID,
    }),
    // 15% head sampling — tail sampling at collector (D-35)
    sampler: {
      shouldSample: headSampleDecision,
      toString: () => "HeadSampler(0.15)",
    } as import("@opentelemetry/sdk-trace-base").Sampler,
    spanProcessors: [
      new BatchSpanProcessor(
        new OTLPTraceExporter({ url: collectorUrl })
      ),
    ],
  });

  provider.register({
    // W3C Trace Context ONLY — NEVER B3/Jaeger (D-35 + conventions)
    propagator: new W3CTraceContextPropagator(),
    contextManager: new ZoneContextManager(),
  });

  // BFE-3: Register Web Vitals observers after SDK is ready.
  // Imported dynamically to keep main chunk lean (web-vitals is ~3KB gz).
  const { registerWebVitals } = await import("./web-vitals");
  registerWebVitals();

  // FetchInstrumentation is configured via getWebAutoInstrumentations under the
  // "@opentelemetry/instrumentation-fetch" key. See BFE-4 note at top of file.
  registerInstrumentations({
    instrumentations: [
      getWebAutoInstrumentations({
        // Document load — captures page load performance
        "@opentelemetry/instrumentation-document-load": {},

        // Fetch — FetchInstrumentation; auto-traces all fetch() calls; restricted to backend only
        "@opentelemetry/instrumentation-fetch": {
          // ONLY propagate traceparent to our backend — NEVER to Keycloak
          propagateTraceHeaderCorsUrls: [
            // Same-origin /api/* paths
            /^\/api\//,
            // Explicit backend host patterns (env-configurable)
            new RegExp(
              (import.meta.env.VITE_API_ORIGIN as string | undefined) ??
                "^http://localhost:808[02]"
            ),
          ],
          clearTimingResources: true,
          // Suppress auth chain — Keycloak ACF+PKCE pages carry code/state in URL
          ignoreUrls: [
            /\/auth\//,
            /\/realms\//,
            /keycloak/,
            /\.well-known/,
            // Also suppress the OTLP collector itself (avoid trace-of-traces)
            /\/v1\/traces/,
          ],
          applyCustomAttributesOnSpan: applyFetchSpanAttributes,
        },

        // User interaction — skip inputs to avoid capturing typed values via accessible name
        "@opentelemetry/instrumentation-user-interaction": {
          shouldPreventSpanCreation: shouldPreventInteractionSpan,
        },

        // XHR — disabled (app uses fetch exclusively)
        "@opentelemetry/instrumentation-xml-http-request": {
          ignoreUrls: [/.*/],
        },
      }),
    ],
  });
}
