import { createRoot } from "react-dom/client";
import { RouterProvider } from "@tanstack/react-router";
import { QueryClientProvider } from "@tanstack/react-query";
import { router } from "@/router";
import { ThemeProvider } from "@/lib/theme-provider";
import { AdminAuthProvider } from "@/lib/admin-auth-context";
import { Toaster } from "@/components/ui/sonner";
import { queryClient } from "@/lib/query-client";
import { generateAnonymousSessionId } from "@/lib/admin-telemetry";
import "@/globals.css";

// ---------------------------------------------------------------------------
// Telemetry bootstrap — fires after first paint (dynamic import keeps SDK
// out of the critical render path, per bundle isolation rule).
// Session id is anonymous and lives in memory only (D-12: no storage).
// ---------------------------------------------------------------------------
const _sessionId = generateAnonymousSessionId();

import("@/lib/admin-telemetry").then(({ initAdminTelemetry }) => {
  void initAdminTelemetry(_sessionId);
});

const rootEl = document.getElementById("root");
if (!rootEl) throw new Error("Elemento #root não encontrado no DOM");

createRoot(rootEl).render(
  <ThemeProvider attribute="class" defaultTheme="system" enableSystem>
    <QueryClientProvider client={queryClient}>
      <AdminAuthProvider>
        <RouterProvider router={router} />
        <Toaster />
      </AdminAuthProvider>
    </QueryClientProvider>
  </ThemeProvider>
);
