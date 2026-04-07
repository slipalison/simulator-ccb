import { PageLayout } from "@/components/templates/PageLayout";
import { ExampleForm } from "@/components/organisms/ExampleForm";

/**
 * Page: home placeholder.
 * Instancia PageLayout com ExampleForm como conteúdo demonstrativo.
 * Formulário com RHF + Zod real será conectado em 07-03-PLAN.md.
 */
export function HomePage() {
  return (
    <PageLayout>
      <div className="flex flex-col items-center gap-8">
        <div className="text-center space-y-2">
          <h1 className="text-2xl font-bold text-foreground">Bem-vindo</h1>
          <p className="text-muted-foreground">
            Plataforma de cadastro de clientes PF e PJ
          </p>
        </div>
        <ExampleForm />
      </div>
    </PageLayout>
  );
}
