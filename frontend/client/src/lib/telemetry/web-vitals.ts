// ---------------------------------------------------------------------------
// Web Vitals → OTel Metrics adapter — frontend/client SPA (D-35, BFE-3)
// ---------------------------------------------------------------------------
// Subscribes to Web Vitals (CLS, INP, LCP, FCP, TTFB) and forwards each
// metric as a histogram record to the OTel Metrics pipeline.
//
// Design decisions:
//   - Single module, called once from initTelemetry() after SDK is ready.
//   - Uses @opentelemetry/api metrics.getMeter() — lazy singleton, safe to
//     call before the SDK provider registers (records are buffered).
//   - No PII in metric attributes — only metric name + rating (Good/NI/Poor).
//   - INP replaces FID in web-vitals v3+ (FID deprecated).
//
// Security (PRIO 1): no user data in metric attributes.
// Performance (PRIO 2): no blocking — all callbacks are fire-and-forget.
// ---------------------------------------------------------------------------

import { metrics } from "@opentelemetry/api";
import { onCLS, onFCP, onINP, onLCP, onTTFB } from "web-vitals";
import type { Metric } from "web-vitals";

const METER_NAME = "onboarding.client.web-vitals";

/** Single histogram shared by all Web Vitals — distinguished by `vital.name` attribute. */
const vitalsHistogram = metrics
  .getMeter(METER_NAME)
  .createHistogram("frontend.web_vitals", {
    description: "Web Vitals metrics (CLS, INP, LCP, FCP, TTFB) for the client SPA",
    unit: "ms",
  });

function report(metric: Metric): void {
  // Security: only name + rating — no PII, no user identifiers.
  vitalsHistogram.record(metric.value, {
    "vital.name": metric.name,
    "vital.rating": metric.rating,
  });
}

/**
 * Register all Web Vitals observers and forward to OTel Metrics.
 * Call once after OTel SDK provider is registered (from initTelemetry).
 * Safe to call multiple times — web-vitals lib is idempotent internally.
 */
export function registerWebVitals(): void {
  onCLS(report);
  onINP(report);
  onLCP(report);
  onFCP(report);
  onTTFB(report);
}
