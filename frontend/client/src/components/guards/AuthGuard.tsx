import { useEffect, type ReactNode } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useAuth } from "@/lib/auth-context";

interface AuthGuardProps {
  children: ReactNode;
}

/**
 * AuthGuard: protects routes from unauthenticated access
 * Redirects to /login if user is not authenticated
 */
export function AuthGuard({ children }: AuthGuardProps) {
  const { auth } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!auth.isAuthenticated) {
      navigate({ to: "/login" as any, replace: true });
    }
  }, [auth.isAuthenticated, navigate]);

  if (!auth.isAuthenticated) {
    return (
      <div className="flex min-h-screen items-center justify-center" data-testid="auth-guard-loading">
        <div className="text-center">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent mx-auto" />
          <p className="mt-4 text-muted-foreground">Verificando autenticacao...</p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
