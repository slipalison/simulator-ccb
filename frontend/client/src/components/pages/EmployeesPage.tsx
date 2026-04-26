import { useAuth } from "@/lib/auth-context";
import { Navigate } from "@tanstack/react-router";

/**
 * EmployeesPage: placeholder page for employee management.
 * Shows "Funcionários — Em construção" until Plan 03 fills in.
 * Only accessible by admin-empresa and viewer groups.
 */
export function EmployeesPage() {
  const { auth } = useAuth();

  if (!auth.isAuthenticated) {
    return <Navigate to="/auth/login" />;
  }

  if (auth.accessGroup !== "admin-empresa" && auth.accessGroup !== "viewer") {
    return <Navigate to="/profile" />;
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Funcionários</h1>
      <p className="text-muted-foreground">Em construção</p>
    </div>
  );
}