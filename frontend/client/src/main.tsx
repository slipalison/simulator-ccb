import { createRoot } from "react-dom/client";
import { RouterProvider } from "@tanstack/react-router";
import { QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { router } from "@/router";
import { queryClient } from "@/lib/query-client";
import { ThemeProvider } from "@/lib/theme-provider";
import { AuthProvider } from "@/lib/auth-context";
import { Toaster } from "@/components/ui/sonner";
import { initTelemetry } from "@/lib/telemetry";
import "@/globals.css";

const rootEl = document.getElementById("root");
if (!rootEl) throw new Error("Elemento #root não encontrado no DOM");

// Initialize OTel telemetry BEFORE React mount (D-35).
// - initTelemetry() is gated by VITE_OTEL_ENABLED=true — no-op in dev/test by default.
// - OTel SDK modules are dynamically imported inside initTelemetry() to keep
//   the main bundle lean (telemetry does NOT block first render when disabled).
// - When enabled, awaiting here ensures traceparent is wired before any fetch
//   fired during React hydration/router initialization.
// Security: init runs AFTER page load — auth ACF+PKCE redirect chain (carrying
// code/state) completes before this module is loaded, so no pre-auth URL capture.
initTelemetry().catch(() => {
  // Telemetry init failure MUST NOT crash the app (security + reliability).
  // Silent catch: no console.log (dropped in prod), error surface via error boundary if needed.
});

createRoot(rootEl).render(
  <ThemeProvider attribute="class" defaultTheme="system" enableSystem>
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
        <Toaster />
      </AuthProvider>
      {(import.meta as any).env?.DEV && <ReactQueryDevtools initialIsOpen={false} />}
    </QueryClientProvider>
  </ThemeProvider>
);
