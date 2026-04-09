import { createRoot } from "react-dom/client";
import { RouterProvider } from "@tanstack/react-router";
import { router } from "@/router";
import { ThemeProvider } from "@/lib/theme-provider";
import { AuthProvider } from "@/lib/auth-context";
import { AdminAuthProvider } from "@/lib/admin-auth-context";
import { Toaster } from "@/components/ui/sonner";
import "@/globals.css";

const rootEl = document.getElementById("root");
if (!rootEl) throw new Error("Elemento #root não encontrado no DOM");

createRoot(rootEl).render(
  <ThemeProvider attribute="class" defaultTheme="system" enableSystem>
    <AuthProvider>
      <AdminAuthProvider>
        <RouterProvider router={router} />
        <Toaster />
      </AdminAuthProvider>
    </AuthProvider>
  </ThemeProvider>
);
