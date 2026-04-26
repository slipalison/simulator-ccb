import { RegistrationForm } from "@/components/molecules/RegistrationForm";
import { ThemeToggle } from "@/components/atoms/ThemeToggle";
import { Building2 } from "lucide-react";

/**
 * RegisterPage: full-page wrapper for the PJ registration wizard.
 * Centers RegistrationForm with branding and login link.
 * No sidebar — registration is pre-auth.
 */
export function RegisterPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4 relative">
      <div className="absolute top-4 right-4 z-10">
        <ThemeToggle />
      </div>

      <div className="w-full max-w-lg">
        <div className="text-center mb-8">
          <Building2 className="h-12 w-12 mx-auto text-primary mb-3" />
          <h1 className="text-3xl font-bold tracking-tight">Onboarding</h1>
          <p className="text-muted-foreground mt-1">
            Cadastro para Pessoa Jurídica
          </p>
        </div>

        <RegistrationForm />

        <div className="mt-6 text-center text-sm text-muted-foreground">
          Já tem uma conta?{" "}
          <a
            href="/auth/login"
            className="text-primary hover:underline font-medium"
          >
            Fazer login &rarr;
          </a>
        </div>
      </div>
    </div>
  );
}