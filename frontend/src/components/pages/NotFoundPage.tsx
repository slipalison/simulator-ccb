import { PageLayout } from "@/components/templates/PageLayout";
import { AppButton } from "@/components/atoms/AppButton";
import { Link } from "@tanstack/react-router";

/**
 * Page: 404 — renderizada pelo notFoundComponent do rootRoute.
 * Exibe mensagem clara e link de volta para home.
 */
export function NotFoundPage() {
  return (
    <PageLayout>
      <div className="flex flex-col items-center justify-center gap-6 py-16 text-center">
        <h1 className="text-6xl font-bold text-foreground">404</h1>
        <p className="text-xl text-muted-foreground">Página não encontrada</p>
        <p className="text-sm text-muted-foreground max-w-sm">
          A rota que você tentou acessar não existe neste aplicativo.
        </p>
        <Link to="/">
          <AppButton variant="outline">Voltar para o início</AppButton>
        </Link>
      </div>
    </PageLayout>
  );
}
