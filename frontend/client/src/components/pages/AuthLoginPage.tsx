import { useEffect } from "react";
import { useAuth } from "@/lib/auth-context";

/**
 * AuthLoginPage: redirect-only component.
 * If authenticated, redirects to /profile.
 * Otherwise, redirects to /auth/login (Vinxi server → Keycloak ACF).
 */
export function AuthLoginPage() {
  const { auth, login } = useAuth();

  useEffect(() => {
    if (!auth.isLoading) {
      if (auth.isAuthenticated) {
        window.location.href = "/profile";
      } else {
        login();
      }
    }
  }, [auth.isLoading, auth.isAuthenticated, login]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="text-center space-y-4">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto" />
        <p className="text-muted-foreground text-sm">Redirecionando para login...</p>
      </div>
    </div>
  );
}
