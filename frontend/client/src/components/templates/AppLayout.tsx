import { Sidebar } from "@/components/organisms/Sidebar";
import { Header } from "@/components/organisms/Header";
import { Outlet } from "@tanstack/react-router";
import { useAuth } from "@/lib/auth-context";

// ---------------------------------------------------------------------------
// Template: AppLayout — authenticated content layout with sidebar + header
// ---------------------------------------------------------------------------
// Renders:
//   - Header at top (sticky, full width)
//   - Sidebar on the left (fixed, w-64, only when authenticated)
//   - Main content area with <Outlet /> for nested routes
// ---------------------------------------------------------------------------

export function AppLayout() {
  const { auth } = useAuth();

  return (
    <div className="min-h-screen bg-background">
      <Header />
      {auth.isAuthenticated && <Sidebar />}
      <main
        className={`min-h-[calc(100vh-3.5rem)] p-6 ${
          auth.isAuthenticated ? "ml-64" : ""
        }`}
      >
        <Outlet />
      </main>
    </div>
  );
}