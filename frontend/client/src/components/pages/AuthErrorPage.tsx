import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { AlertTriangle } from "lucide-react";

/**
 * AuthErrorPage: displays authentication errors from Keycloak callback.
 * Reads ?error= query parameter from the URL.
 */
export function AuthErrorPage() {
  const params = new URLSearchParams(window.location.search);
  const error = params.get("error") || "Ocorreu um erro na autenticação.";

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <AlertTriangle className="h-12 w-12 mx-auto text-destructive mb-2" />
          <CardTitle>Erro de Autenticação</CardTitle>
        </CardHeader>
        <CardContent className="text-center space-y-4">
          <p className="text-sm text-muted-foreground">{error}</p>
          <a
            href="/auth/login"
            className="text-sm text-primary underline inline-block"
          >
            Voltar ao login
          </a>
        </CardContent>
      </Card>
    </div>
  );
}
