import { useState, useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { ThemeToggle } from "../atoms/ThemeToggle";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { resetPasswordClient, ResetPasswordError } from "@/lib/api";
import { PasswordField } from "../molecules/PasswordField";
import { PasswordStrengthMeter } from "../molecules/PasswordStrengthMeter";

const resetSchema = z.object({
  password: z.string().min(8, "Senha deve ter no minimo 8 caracteres"),
  confirmPassword: z.string(),
}).superRefine((data, ctx) => {
  if (data.password !== data.confirmPassword) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: "As senhas nao coincidem",
      path: ["confirmPassword"],
    });
  }
});

/**
 * ResetPasswordPage: user enters new password after clicking reset link.
 * Redesigned with shadcn Card + Form, PasswordField + StrengthMeter.
 */
export function ResetPasswordPage({ token: propToken }: { token?: string } = {}) {
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

  const form = useForm<z.infer<typeof resetSchema>>({
    resolver: zodResolver(resetSchema),
    defaultValues: { password: "", confirmPassword: "" },
  });

  async function onSubmit(values: z.infer<typeof resetSchema>) {
    if (!token) {
      setError("Token nao fornecido. Solicite um novo link de recuperacao.");
      return;
    }

    setError(null);
    setLoading(true);

    try {
      await resetPasswordClient(token, values.password);
      navigate({ to: "/auth/login" as any });
    } catch (err: unknown) {
      if (err instanceof ResetPasswordError) {
        setError(err.message);
      } else {
        setError("Ocorreu um erro inesperado. Tente novamente.");
      }
    } finally {
      setLoading(false);
    }
  }

  const password = form.watch("password");
  const confirmPassword = form.watch("confirmPassword");
  const passwordsMatch = password === confirmPassword && confirmPassword.length > 0;

  if (!token) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background p-4">
        <Card className="w-full max-w-md">
          <CardContent className="pt-6">
            <p className="text-center text-destructive">Token nao fornecido. Solicite um novo link de recuperacao.</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4">
      <div className="absolute top-4 right-4">
        <ThemeToggle />
      </div>
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle className="text-2xl text-center">Nova Senha</CardTitle>
          <CardDescription className="text-center">
            Defina sua nova senha
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
              <FormField
                control={form.control}
                name="password"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Senha</FormLabel>
                    <FormControl>
                      <PasswordField
                        value={field.value}
                        onChange={field.onChange}
                        label="Senha"
                        id="password"
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <PasswordStrengthMeter password={password} />

              <FormField
                control={form.control}
                name="confirmPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Confirmar Senha</FormLabel>
                    <FormControl>
                      <PasswordField
                        value={field.value}
                        onChange={field.onChange}
                        label="Confirmar Senha"
                        id="confirmPassword"
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {passwordsMatch && (
                <p className="text-sm text-green-600 dark:text-green-400" data-testid="passwords-match">
                  As senhas coincidem
                </p>
              )}

              {error && (
                <Alert variant="destructive">
                  <AlertDescription>{error}</AlertDescription>
                </Alert>
              )}

              <Button type="submit" className="w-full" disabled={loading}>
                {loading ? "Salvando..." : "Alterar senha"}
              </Button>
            </form>
          </Form>

          <div className="mt-4 text-center text-sm">
            <a href="/auth/login" className="text-primary hover:underline">
              Voltar para login &rarr;
            </a>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
