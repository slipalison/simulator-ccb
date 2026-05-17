/**
 * Root-level vitest config for scripts/ tests.
 * SPA-level tests use their own configs under frontend/{client,backoffice}/.
 * This config targets only the scripts/ directory and runs in Node environment
 * (no jsdom needed for pure-Node guard scripts).
 */
import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    environment: "node",
    include: ["scripts/**/*.test.{mjs,ts,js}"],
  },
});
