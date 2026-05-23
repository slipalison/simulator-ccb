/// <reference types="vite/client" />

// Augment Vite's ImportMetaEnv with project-specific variables
interface ImportMetaEnv {
  /** Set to 'true' to enable OTel telemetry in the backoffice SPA (D-35) */
  readonly VITE_OTEL_ENABLED?: string;
  /** OTLP HTTP collector URL — must be first-party. Default: http://localhost:4318/v1/traces */
  readonly VITE_OTEL_COLLECTOR_URL?: string;
  /** Service name reported in OTel resource attributes */
  readonly VITE_OTEL_SERVICE_NAME?: string;
  /** Application version reported in OTel resource attributes */
  readonly VITE_APP_VERSION?: string;
  /** Vite built-in: current mode (development | production) */
  readonly MODE: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
