import { useEffect, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { LoginForm } from "@/components/molecules/LoginForm";
import { useAuth } from "@/lib/auth-context";
import { LoginError } from "@/lib/api";
import type { LoginData } from "@/lib/validation-schemas";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ThemeToggle } from "@/components/atoms/ThemeToggle";

/**
 * LoginPage: redesigned with shadcn Card.
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
    <div className="min-h-screen flex items-center justify-center bg-background p-4 relative">
      <div className="absolute top-4 right-4 z-10">
        <ThemeToggle />
      </div>
      <Card className="w-full max-w-md">
        <CardHeader className="space-y-1">
          <CardTitle className="text-2xl text-center">Bem-vindo de volta!</CardTitle>
          <CardDescription className="text-center">
            Entre com suas credenciais para acessar sua conta
          </CardDescription>
        </CardHeader>
        <CardContent>
          <LoginForm onSubmit={handleLogin} serverError={serverError} />
          <div className="mt-6 space-y-2 text-center text-sm">
            <a
              href="/forgot-password"
              className="block text-muted-foreground hover:text-foreground transition-colors"
            >
              Esqueceu a senha?
            </a>
            <div className="text-muted-foreground">
              Nao tem conta?{" "}
              <a href="/register" className="text-primary hover:underline font-medium">
                Criar conta &rarr;
              </a>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
