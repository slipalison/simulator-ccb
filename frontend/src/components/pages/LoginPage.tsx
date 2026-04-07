import { useEffect, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { LoginForm } from "@/components/molecules/LoginForm";
import { useAuth } from "@/lib/auth-context";
import { LoginError } from "@/lib/api";
import type { LoginData } from "@/lib/validation-schemas";
import { PageLayout } from "@/components/templates/PageLayout";

/**
 * LoginPage: custom login screen with email + password.
 * Sends credentials to backend (POST /api/auth/login), stores tokens in memory.
 * Redirects to /profile on success.
 */
export function LoginPage() {
  const { login, auth } = useAuth();
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);

  // If already authenticated, redirect to profile
  useEffect(() => {
    if (auth.isAuthenticated) {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      navigate({ to: "/profile" as any, replace: true });
    }
  }, [auth.isAuthenticated, navigate]);

  const handleLogin = async (data: LoginData) => {
    setServerError(null);
    try {
      await login(data.email, data.password);
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      navigate({ to: "/profile" as any, replace: true });
    } catch (error) {
      if (error instanceof LoginError) {
        setServerError(error.message);
      } else {
        setServerError("An unexpected error occurred.");
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
          <LoginForm onSubmit={handleLogin} serverError={serverError} />
        </div>
      </div>
    </PageLayout>
  );
}
