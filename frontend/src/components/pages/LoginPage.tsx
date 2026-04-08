import { useEffect, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { LoginForm } from "@/components/molecules/LoginForm";
import { useAuth } from "@/lib/auth-context";
import { LoginError } from "@/lib/api";
import type { LoginData } from "@/lib/validation-schemas";
import { AuthLayout } from "@/components/templates/AuthLayout";

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
    <AuthLayout
      title="Login"
      subtitle="Entre com seu email e senha para acessar sua conta"
      footer={
        <div className="space-y-2 text-center text-sm">
          <p className="text-slate-600">
            Nao tem uma conta?{" "}
            <a href="/register" className="font-medium text-primary hover:underline">
              Criar conta
            </a>
          </p>
          <p className="text-slate-600">
            <a href="/forgot-password" className="font-medium text-primary hover:underline">
              Esqueci minha senha
            </a>
          </p>
        </div>
      }
    >
      <LoginForm onSubmit={handleLogin} serverError={serverError} />
    </AuthLayout>
  );
}
