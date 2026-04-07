import { createRoot } from "react-dom/client";
import { RouterProvider } from "@tanstack/react-router";
import { router } from "@/router";
import { AuthProvider } from "@/lib/auth-context";
import "@/globals.css";

const rootEl = document.getElementById("root");
if (!rootEl) throw new Error("Elemento #root não encontrado no DOM");

createRoot(rootEl).render(
  <AuthProvider>
    <RouterProvider router={router} />
  </AuthProvider>
);
