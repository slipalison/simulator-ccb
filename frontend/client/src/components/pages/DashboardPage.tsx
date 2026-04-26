import { useAuth } from "@/lib/auth-context";
import { Navigate } from "@tanstack/react-router";

/**
 * DashboardPage: placeholder page for dashboard.
 * Shows "Dashboard — Em construção" until Plan 04 fills in.
 * Only accessible by admin-empresa and dashboard groups.
 */
export function DashboardPage() {
  const { auth } = useAuth();

  if (!auth.isAuthenticated) {
    return <Navigate to="/auth/login" />;
  }

  if (auth.accessGroup !== "admin-empresa" && auth.accessGroup !== "dashboard") {
    return <Navigate to="/profile" />;
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Dashboard</h1>
      <p className="text-muted-foreground">Em construção</p>
    </div>
  );
}