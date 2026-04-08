import { Button } from "@/components/ui/button";
import { Header } from "@/components/organisms/Header";
import { Link } from "@tanstack/react-router";

/**
 * Page: 404 — renderizada pelo notFoundComponent do rootRoute.
 * Exibe mensagem clara e link de volta para home.
 */
export function NotFoundPage() {
  return (
    <div className="min-h-screen bg-background">
      <Header />
      <div className="flex flex-col items-center justify-center gap-6 py-16 text-center">
        <h1 className="text-6xl font-bold text-foreground">404</h1>
        <p className="text-xl text-muted-foreground">Página não encontrada</p>
        <p className="text-sm text-muted-foreground max-w-sm">
          A rota que você tentou acessar não existe neste aplicativo.
        </p>
        <Link to="/">
          <Button variant="outline">Voltar para o início</Button>
        </Link>
      </div>
    </div>
  );
}
