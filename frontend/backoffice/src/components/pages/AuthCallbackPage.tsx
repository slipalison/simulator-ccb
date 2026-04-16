import { useEffect, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Loader2, Shield, AlertTriangle } from "lucide-react";

/**
 * AuthCallbackPage: handles the Keycloak redirect callback.
 * The Vinxi server (/auth/callback) already exchanged the code for tokens
 * and set httpOnly cookies. This page just polls /auth/me to confirm
 * the session and redirects to /admin/users.
 */
export function AuthCallbackPage() {
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const checkAuth = async () => {
      try {
        const res = await fetch("/auth/me", { credentials: "include" });
        if (res.ok) {
          navigate({ to: "/admin/users" as any, replace: true });
        } else {
          setError("Falha na autenticação. Tente novamente.");
        }
      } catch {
        setError("Erro de conexão. Tente novamente.");
      }
    };
    checkAuth();
  }, [navigate]);

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <Card className="w-full max-w-md">
          <CardHeader className="text-center">
            <AlertTriangle className="h-12 w-12 mx-auto text-destructive mb-2" />
            <CardTitle>Erro de Autenticação</CardTitle>
          </CardHeader>
          <CardContent className="text-center">
            <p className="text-sm text-muted-foreground">{error}</p>
            <a
              href="/auth/login"
              className="text-sm text-primary underline mt-4 inline-block"
            >
              Tentar novamente
            </a>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <Shield className="h-12 w-12 mx-auto text-primary mb-2" />
          <CardTitle>Processando login...</CardTitle>
        </CardHeader>
        <CardContent className="text-center">
          <Loader2 className="h-6 w-6 animate-spin mx-auto" />
        </CardContent>
      </Card>
    </div>
  );
}
