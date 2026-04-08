import { useState, FormEvent, useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import { AuthLayout } from "@/components/templates/AuthLayout";
import { PasswordField } from "@/components/molecules/PasswordField";
import { PasswordStrengthMeter } from "@/components/molecules/PasswordStrengthMeter";
import { Button } from "@/components/ui/button";
import { resetPasswordClient, ResetPasswordError } from "@/lib/api";

interface ResetPasswordPageProps {
  /** Token passed directly (for testing). In production, read from URL. */
  token?: string;
}

/**
 * ResetPasswordPage: user enters new password after clicking reset link.
 * Validates token, checks password policy, updates Keycloak password.
 */
export function ResetPasswordPage({ token: propToken }: ResetPasswordPageProps) {
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  // Compute token: use prop if provided, otherwise read from URL
  const token = useMemo(() => {
    if (propToken) return propToken;
    if (typeof window !== "undefined") {
      const params = new URLSearchParams(window.location.search);
      return params.get("token");
    }
    return null;
  }, [propToken]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!token) {
      setError("Token nao fornecido. Solicite um novo link de recuperacao.");
      return;
    }

    if (password !== confirmPassword) {
      setError("As senhas nao coincidem.");
      return;
    }

    setLoading(true);

    if (!token) {
      setError("Token nao fornecido. Solicite um novo link de recuperacao.");
      setLoading(false);
      return;
    }

    try {
      await resetPasswordClient(token, password);
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      navigate({ to: "/login" as any });
    } catch (err: unknown) {
      if (err instanceof ResetPasswordError) {
        setError(err.message);
      } else {
        setError("Ocorreu um erro inesperado. Tente novamente.");
      }
    } finally {
      setLoading(false);
    }
  };

  if (!token) {
    return (
      <AuthLayout
        title="Token Invalido"
        subtitle="Token nao fornecido na URL"
        footer={
          <div className="text-center text-sm">
            <a href="/forgot-password" className="font-medium text-primary hover:underline">
              Solicitar novo link &rarr;
            </a>
          </div>
        }
      >
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800" role="alert">
          Token nao fornecido. Solicite um novo link de recuperacao.
        </div>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout
      title="Nova Senha"
      subtitle="Defina sua nova senha"
      footer={
        <div className="text-center text-sm">
          <a href="/login" className="font-medium text-primary hover:underline">
            Voltar para login &rarr;
          </a>
        </div>
      }
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <PasswordField
          id="password"
          value={password}
          onChange={setPassword}
          label="Senha"
        />
        <PasswordStrengthMeter password={password} />

        <PasswordField
          id="confirmPassword"
          value={confirmPassword}
          onChange={setConfirmPassword}
          label="Confirmar Senha"
          error={password !== confirmPassword && confirmPassword ? "As senhas nao coincidem" : undefined}
        />

        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800" role="alert">
            {error}
          </div>
        )}

        <Button type="submit" className="w-full" disabled={loading}>
          {loading ? "Salvando..." : "Alterar senha"}
        </Button>
      </form>
    </AuthLayout>
  );
}
