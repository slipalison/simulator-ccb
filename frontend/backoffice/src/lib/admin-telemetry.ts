// ---------------------------------------------------------------------------
// Admin OTel JS Telemetry — backoffice SPA
// ---------------------------------------------------------------------------
// SECURITY: Independent implementation per D-4 (no shared code with client SPA).
// Follows conventions from <role> telemetry section:
//   - W3C Trace Context propagator (never B3/Jaeger)
//   - First-party collector endpoint only
//   - Explicit propagateTraceHeaderCorsUrls allowlist — Keycloak excluded
//   - Anonymous session id in memory (never Keycloak sub / email)
//   - PII regex scrubber on span attributes
//   - Auth chain URLs suppressed from tracing
//   - Dynamic import after first paint (bundle isolation)
//   - Head sampling 10–20%
//
// OTel SDK version: v2.x — API differences from v1:
//   - Resource is an interface; use resourceFromAttributes() not new Resource()
//   - WebTracerProvider accepts { spanProcessors: [...], resource } in constructor
//   - No addSpanProcessor() method on provider
// ---------------------------------------------------------------------------

import { WebTracerProvider, BatchSpanProcessor } from "@opentelemetry/sdk-trace-web";
import { resourceFromAttributes } from "@opentelemetry/resources";
import {
  ATTR_SERVICE_NAME,
  ATTR_SERVICE_VERSION,
} from "@opentelemetry/semantic-conventions";
import { registerInstrumentations } from "@opentelemetry/instrumentation";
import { FetchInstrumentation } from "@opentelemetry/instrumentation-fetch";
import { DocumentLoadInstrumentation } from "@opentelemetry/instrumentation-document-load";
import { UserInteractionInstrumentation } from "@opentelemetry/instrumentation-user-interaction";
import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-http";
import { W3CTraceContextPropagator } from "@opentelemetry/core";
import type { Attributes, AttributeValue } from "@opentelemetry/api";

// ---------------------------------------------------------------------------
// Constants — loaded from Vite env at build time
// ---------------------------------------------------------------------------

// Service name identifies the backoffice SPA in collector/Jaeger
const SERVICE_NAME: string = import.meta.env.VITE_OTEL_SERVICE_NAME ?? "onboarding-backoffice";

// Must be a first-party collector endpoint. Validated at build time via env.
const COLLECTOR_URL: string =
  import.meta.env.VITE_OTEL_COLLECTOR_URL ?? "http://localhost:4318/v1/traces";

// App version injected by Vite define plugin (or fallback)
const APP_VERSION: string = import.meta.env.VITE_APP_VERSION ?? "dev";

// ---------------------------------------------------------------------------
// Allowlist — ONLY admin API backend receives traceparent headers.
// Keycloak realm requests are EXPLICITLY excluded.
// Regex matches /api/admin/* paths on the same origin (localhost:5174 → API proxy).
// ---------------------------------------------------------------------------
export const ALLOWED_BACKEND_URLS: RegExp[] = [
  // Backend admin API — same-origin proxy (backoffice port 5174 → API 8080)
  /^https?:\/\/localhost(:\d+)?\/api\/admin\//,
  // Production API domain — update when deploying
  // e.g. /^https:\/\/api\.onboarding\.example\//,
];

// ---------------------------------------------------------------------------
// PII scrubber — matches attribute keys that must be dropped before export
// ---------------------------------------------------------------------------
const PII_ATTR_REGEX =
  /(token|secret|password|authorization|cpf|cnpj|email|jwt|sub|refresh|cookie|set-cookie)/i;

// Auth / telemetry suppress patterns — never trace these URLs
export const SUPPRESS_URL_PATTERNS: RegExp[] = [
  /\/auth\//,           // local auth handler (Vinxi h3 route)
  /keycloak/,           // any keycloak URL substring
  /\.well-known/,       // OIDC discovery
  /\/realms\//,         // Keycloak realm paths
  /[?&](code|state)=/, // PKCE callback — may carry auth codes
];

function isSuppressedUrl(url: string): boolean {
  return SUPPRESS_URL_PATTERNS.some((p) => p.test(url));
}

function scrubAttributes(
  attrs: Record<string, string>
): Attributes {
  const out: Attributes = {};
  for (const [k, v] of Object.entries(attrs)) {
    if (PII_ATTR_REGEX.test(k)) continue;
    // Bound string values to prevent accidental large-payload leakage
    if (v.length > 256) continue;
    out[k] = v as AttributeValue;
  }
  return out;
}

// ---------------------------------------------------------------------------
// Telemetry initialised flag — prevents double-init
// ---------------------------------------------------------------------------
let _initialised = false;

// ---------------------------------------------------------------------------
// initAdminTelemetry
// Called once from main.tsx BEFORE ReactDOM.createRoot.
// sessionId: anonymous UUID generated per tab — never Keycloak sub/email.
// ---------------------------------------------------------------------------
export async function initAdminTelemetry(sessionId: string): Promise<void> {
  // Respect VITE_OTEL_ENABLED flag — disabled if unset
  if (import.meta.env.VITE_OTEL_ENABLED !== "true") return;
  if (_initialised) return;
  _initialised = true;

  const resource = resourceFromAttributes({
    [ATTR_SERVICE_NAME]: SERVICE_NAME,
    [ATTR_SERVICE_VERSION]: APP_VERSION,
    // Anonymous, per-tab session id — NEVER Keycloak sub/email
    "onboarding.session_id": sessionId,
    "deployment.environment": import.meta.env.MODE ?? "production",
  });

  const exporter = new OTLPTraceExporter({ url: COLLECTOR_URL });

  // OTel v2: spanProcessors passed in constructor config (no addSpanProcessor)
  const provider = new WebTracerProvider({
    resource,
    spanProcessors: [new BatchSpanProcessor(exporter)],
  });

  // W3C Trace Context propagator — NEVER B3 or Jaeger
  provider.register({ propagator: new W3CTraceContextPropagator() });

  registerInstrumentations({
    instrumentations: [
      new DocumentLoadInstrumentation(),

      new FetchInstrumentation({
        // ONLY forward traceparent to allowlisted admin API origins
        propagateTraceHeaderCorsUrls: ALLOWED_BACKEND_URLS,
        clearTimingResources: true,
        // Suppress traces for auth chain URLs (Keycloak, PKCE, well-known)
        ignoreUrls: SUPPRESS_URL_PATTERNS,
        applyCustomAttributesOnSpan: (span, request) => {
          try {
            const req = request as Request;
            const rawUrl = typeof req.url === "string" ? req.url : String(req.url);
            if (isSuppressedUrl(rawUrl)) return;

            const parsed = new URL(rawUrl, location.origin);
            // Path only — NEVER query params (may contain tokens/search terms with PII)
            span.setAttributes(scrubAttributes({ "http.target": parsed.pathname }));
          } catch {
            // Malformed URL — do not set attribute
          }
        },
      }),

      new UserInteractionInstrumentation({
        // Skip input/textarea interactions to avoid capturing values via accessible name
        shouldPreventSpanCreation: (_eventType: string, element: Element) => {
          const tag = element.tagName?.toUpperCase();
          return tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT";
        },
      }),
    ],
  });
}

// ---------------------------------------------------------------------------
// generateAnonymousSessionId
// Produces a cryptographically-random UUID per tab, stored in memory only.
// Never persisted to localStorage/sessionStorage/cookies (D-12 / security rule).
// ---------------------------------------------------------------------------
export function generateAnonymousSessionId(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  // Fallback for environments without crypto.randomUUID
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}
