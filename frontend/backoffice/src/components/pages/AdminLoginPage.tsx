import { useEffect } from "react";
import { useAdminAuth } from "@/lib/admin-auth-context";

/**
 * AdminLoginPage: redirects to Auth Code Flow login.
 * Kept for backwards compatibility with any existing route references.
 * The canonical login entry point is /auth/login (Vinxi server route).
 */
export function AdminLoginPage() {
  const { admin, login } = useAdminAuth();

  useEffect(() => {
    if (!admin.isLoading) {
      if (admin.isAuthenticated) {
        window.location.href = "/admin/users";
      } else {
        login();
      }
    }
  }, [admin.isLoading, admin.isAuthenticated, login]);

  return (
    <div className="flex items-center justify-center min-h-screen">
      <p className="text-muted-foreground">Redirecionando para login...</p>
    </div>
  );
}
