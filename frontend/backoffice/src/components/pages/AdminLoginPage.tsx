import { useEffect } from "react";
import { useAdminAuth } from "@/lib/admin-auth-context";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

/**
 * AdminLoginPage: entry point for admin backoffice authentication.
 * Redirects to the server-side /auth/login route which handles
 * PKCE code verifier generation and redirect to Keycloak.
 */
export function AdminLoginPage() {
  const { admin } = useAdminAuth();

  // If already authenticated, redirect to admin users
  useEffect(() => {
    if (admin.isAuthenticated) {
      window.location.href = "/admin/users";
    }
  }, [admin.isAuthenticated]);

  const handleLogin = () => {
    window.location.href = "/auth/login";
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="space-y-1">
          <CardTitle className="text-2xl text-center">Admin Backoffice</CardTitle>
          <CardDescription className="text-center">
            Acesse com sua conta de administrador
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button
            onClick={handleLogin}
            className="w-full"
            disabled={admin.isLoading}
            data-testid="admin-login-button"
          >
            {admin.isLoading ? "Verificando sessão..." : "Entrar"}
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
