import { createRoot } from "react-dom/client";
import { RouterProvider } from "@tanstack/react-router";
import { QueryClientProvider } from "@tanstack/react-query";
import { router } from "@/router";
import { ThemeProvider } from "@/lib/theme-provider";
import { AdminAuthProvider } from "@/lib/admin-auth-context";
import { Toaster } from "@/components/ui/sonner";
import { queryClient } from "@/lib/query-client";
import "@/globals.css";

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
