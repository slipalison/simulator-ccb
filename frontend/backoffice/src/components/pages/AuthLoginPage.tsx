import { useEffect } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useAdminAuth } from "@/lib/admin-auth-context";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Loader2, Shield } from "lucide-react";

/**
 * AuthLoginPage: redirect-only component for Auth Code Flow.
 * Immediately redirects to /auth/login (Vinxi server → Keycloak ACF).
 * Shows a loading spinner while the redirect is processing.
 */
export function AuthLoginPage() {
  const { admin } = useAdminAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (admin.isAuthenticated) {
      navigate({ to: "/admin/users" as any, replace: true });
      return;
    }
    // Redirect to Keycloak via Vinxi server route
    window.location.href = "/auth/login";
  }, [admin.isAuthenticated, navigate]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <Shield className="h-12 w-12 mx-auto text-primary mb-2" />
          <CardTitle>Admin Backoffice</CardTitle>
        </CardHeader>
        <CardContent className="text-center">
          <Loader2 className="h-6 w-6 animate-spin mx-auto" />
          <p className="mt-2 text-sm text-muted-foreground">
            Redirecionando para login...
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
