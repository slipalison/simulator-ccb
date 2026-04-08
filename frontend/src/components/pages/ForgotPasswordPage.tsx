import { useState, FormEvent } from "react";
import { useNavigate } from "@tanstack/react-router";
import { AuthLayout } from "@/components/templates/AuthLayout";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { forgotPasswordClient, ForgotPasswordError } from "@/lib/api";

/**
 * ForgotPasswordPage: user enters email to receive password reset link.
 * Always shows generic success message (no info disclosure).
 */
export function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      await forgotPasswordClient(email);
      setSubmitted(true);
    } catch (err: unknown) {
      if (err instanceof ForgotPasswordError) {
        setError(err.message);
      } else {
        setError("Ocorreu um erro inesperado. Tente novamente.");
      }
    } finally {
      setLoading(false);
    }
  };

  if (submitted) {
    return (
      <AuthLayout
        title="Email Enviado"
        subtitle="Verifique sua caixa de entrada"
        footer={
          <div className="text-center text-sm">
            <a href="/login" className="font-medium text-primary hover:underline">
              Voltar para login &rarr;
            </a>
          </div>
        }
      >
        <div className="space-y-4">
          <div className="rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-800">
            Se o email existir, voce recebera um link de recuperacao.
          </div>
          <Button
            type="button"
            variant="outline"
            className="w-full"
            onClick={() => navigate({ to: "/login" as any })}
          >
            Voltar para login
          </Button>
        </div>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout
      title="Recuperar Senha"
      subtitle="Informe seu email para receber um link de recuperacao"
      footer={
        <div className="text-center text-sm">
          <a href="/login" className="font-medium text-primary hover:underline">
            Voltar para login &rarr;
          </a>
        </div>
      }
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-1">
          <Label htmlFor="email">Email</Label>
          <Input
            id="email"
            type="email"
            placeholder="seu@email.com"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            aria-label="Email"
          />
        </div>

        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800" role="alert">
            {error}
          </div>
        )}

        <Button type="submit" className="w-full" disabled={loading}>
          {loading ? "Enviando..." : "Enviar link"}
        </Button>
      </form>
    </AuthLayout>
  );
}
