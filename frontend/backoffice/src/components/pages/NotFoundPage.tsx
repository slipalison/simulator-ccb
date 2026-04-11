import { Button } from "@/components/ui/button";
import { Link } from "@tanstack/react-router";
import { Shield } from "lucide-react";

/**
 * Page: 404 — renderizada pelo notFoundComponent do rootRoute.
 * Exibe mensagem clara e link de volta para home.
 */
export function NotFoundPage() {
  return (
    <div className="min-h-screen bg-background">
      <header className="border-b px-6 py-4 flex items-center gap-3 bg-card">
        <Shield className="h-6 w-6 text-primary" />
        <h1 className="text-lg font-semibold">Backoffice Admin</h1>
      </header>
      <div className="flex flex-col items-center justify-center gap-6 py-16 text-center">
        <h1 className="text-6xl font-bold text-foreground">404</h1>
        <p className="text-xl text-muted-foreground">Página não encontrada</p>
        <p className="text-sm text-muted-foreground max-w-sm">
          A rota que você tentou acessar não existe neste aplicativo.
        </p>
        <Link to="/admin/login">
          <Button variant="outline">Voltar para o login</Button>
        </Link>
      </div>
    </div>
  );
}
