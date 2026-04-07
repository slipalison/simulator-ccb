import { Link } from "@tanstack/react-router";
import { PageLayout } from "@/components/templates/PageLayout";

/**
 * LoginPage placeholder — will be implemented in Phase 09.
 * Currently serves as the redirect target after successful registration.
 */
export function LoginPage() {
  return (
    <PageLayout>
      <div className="mx-auto max-w-md text-center">
        <h1 className="mb-4 text-3xl font-bold text-foreground">Login</h1>
        <p className="mb-6 text-muted-foreground">
          Tela de login &#8212; ser&#225; implementada na Phase 09.
        </p>
        <Link
          to="/"
          className="text-sm text-primary underline hover:text-primary/80"
        >
          Voltar para in&#237;cio
        </Link>
      </div>
    </PageLayout>
  );
}
