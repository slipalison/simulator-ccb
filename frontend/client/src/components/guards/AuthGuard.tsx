import { useEffect, type ReactNode } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useAuth } from "@/lib/auth-context";

interface AuthGuardProps {
  children: ReactNode;
}

/**
 * AuthGuard: protects routes from unauthenticated access.
 *
 * State machine:
 *   isLoading === true  → render skeleton; do NOT navigate (tryRestore in progress)
 *   isLoading === false && isAuthenticated === false → navigate to /login
 *   isLoading === false && isAuthenticated === true  → render children
 *
 * Bug fix (T-5a / auth-flow-fix): the previous implementation fired navigate on
 * `!isAuthenticated` alone, which triggered during the initial render when
 * isLoading=true and isAuthenticated=false (AuthProvider default state), causing
 * a transient redirect flash before tryRestore() could complete.
 */
export function AuthGuard({ children }: AuthGuardProps) {
  const { auth } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!auth.isLoading && !auth.isAuthenticated) {
      navigate({ to: "/login" as any, replace: true });
    }
  }, [auth.isLoading, auth.isAuthenticated, navigate]);

  if (auth.isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center" data-testid="auth-guard-loading">
        <div className="text-center">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent mx-auto" />
          <p className="mt-4 text-muted-foreground">Verificando autenticacao...</p>
        </div>
      </div>
    );
  }

  if (!auth.isAuthenticated) {
    return null;
  }

  return <>{children}</>;
}
