import { useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { LoginForm } from "@/components/molecules/LoginForm";
import { useAuth } from "@/lib/auth-context";
import type { LoginData } from "@/lib/validation-schemas";
import { PageLayout } from "@/components/templates/PageLayout";

/**
 * LoginPage: custom login screen with email + password.
 * Sends credentials to backend (POST /api/auth/login), stores tokens in memory.
 * Redirects to /profile on success (placeholder for Phase 10).
 */
export function LoginPage() {
  const { login, auth } = useAuth();
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);

  const handleSubmit = async (data: LoginData) => {
    setServerError(null);
    try {
      await login(data.email, data.password);
      // Redirect to profile page (Phase 10 — route not yet registered)
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      navigate({ to: "/profile" as any });
    } catch (err) {
      if (err instanceof Error) {
        setServerError(err.message);
      } else {
        setServerError("Invalid credentials.");
      }
    }
  };

  return (
    <PageLayout>
      <div className="mx-auto max-w-md text-center">
        <h1 className="mb-4 text-3xl font-bold text-foreground">Login</h1>
        <p className="mb-6 text-muted-foreground">
          Entre com seu email e senha para acessar sua conta.
        </p>
        <div className="flex justify-center">
          <LoginForm onSubmit={handleSubmit} serverError={serverError} />
        </div>
      </div>
    </PageLayout>
  );
}
