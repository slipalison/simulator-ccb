import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { PasswordField } from "@/components/molecules/PasswordField";
import { PasswordStrengthMeter } from "@/components/molecules/PasswordStrengthMeter";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ThemeToggle } from "@/components/atoms/ThemeToggle";
import { Loader2 } from "lucide-react";
import {
  companyAccessSchema,
  type CompanyAccessData,
} from "@/lib/validation-schemas";
import {
  registerCompany,
  RegistrationValidationError,
  DuplicateClientError,
  RegistrationUnavailable,
  ApiError,
} from "@/lib/api";
import { useAuth } from "@/lib/auth-context";

/**
 * RegistrationForm: PJ-only company registration form with shadcn/ui.
 * Auto-login after successful registration.
 * Will be converted to a 2-step wizard in Plan 02.
 */
export function RegistrationForm() {

  const { login } = useAuth();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]> | null>(null);

  const form = useForm<CompanyAccessData>({
    resolver: zodResolver(companyAccessSchema),
    defaultValues: {
      email: "",
      phone: "",
      password: "",
      confirmPassword: "",
      termsAccepted: undefined as unknown as true,
    },
  });

  const password = form.watch("password");
  const confirmPassword = form.watch("confirmPassword");
  const passwordsMatch = password && confirmPassword && password === confirmPassword;

  // Map server-side field errors to RHF setError
  if (fieldErrors) {
    // Show once, then clear
    Object.entries(fieldErrors).forEach(([field, messages]) => {
      form.setError(field as keyof CompanyAccessData, {
        type: "server",
        message: messages[0],
      });
    });
    setFieldErrors(null);
  }

  const onSubmit = async (data: CompanyAccessData) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      await registerCompany({
        razaoSocial: "", // TODO: will come from wizard step 1
        cnpj: "", // TODO: will come from wizard step 1
        email: data.email,
        phone: data.phone,
        password: data.password,
        termsAccepted: data.termsAccepted,
        termsVersion: "1.0",
      });

      // After registration, redirect to ACF login
      login();
    } catch (err) {
      if (err instanceof RegistrationValidationError) {
        setFieldErrors(err.errors);
      } else if (err instanceof DuplicateClientError) {
        setSubmitError(err.message);
      } else if (err instanceof RegistrationUnavailable) {
        setSubmitError(err.message);
      } else if (err instanceof ApiError) {
        setSubmitError(err.message);
      } else {
        setSubmitError("An unexpected error occurred.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4 relative">
      <div className="absolute top-4 right-4 z-10">
        <ThemeToggle />
      </div>
      <Card className="w-full max-w-lg">
        <CardHeader className="space-y-1">
          <CardTitle className="text-2xl text-center">Criar sua conta</CardTitle>
          <CardDescription className="text-center">
            Preencha seus dados para se cadastrar
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Alert className="mb-4">
            <AlertDescription>
              Cadastro exclusivo para Pessoa Jurídica (empresa).
            </AlertDescription>
          </Alert>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6" noValidate>
              {/* Server error */}
              {submitError && (
                <Alert variant="destructive">
                  <AlertDescription>{submitError}</AlertDescription>
                </Alert>
              )}

              {/* Email */}
              <FormField
                control={form.control}
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Email</FormLabel>
                    <FormControl>
                      <Input
                        type="email"
                        placeholder="seu@email.com"
                        disabled={isSubmitting}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Phone */}
              <FormField
                control={form.control}
                name="phone"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Telefone</FormLabel>
                    <FormControl>
                      <Input
                        placeholder="(00) 00000-0000"
                        inputMode="tel"
                        disabled={isSubmitting}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Password */}
              <FormField
                control={form.control}
                name="password"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Senha</FormLabel>
                    <FormControl>
                      <PasswordField
                        id="password"
                        label="Senha"
                        value={field.value ?? ""}
                        onChange={field.onChange}
                        disabled={isSubmitting}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Password Strength Meter */}
              <PasswordStrengthMeter password={password ?? ""} />

              {/* Confirm Password */}
              <FormField
                control={form.control}
                name="confirmPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Confirmar senha</FormLabel>
                    <FormControl>
                      <PasswordField
                        id="confirmPassword"
                        label="Confirmar senha"
                        value={field.value ?? ""}
                        onChange={field.onChange}
                        disabled={isSubmitting}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Password match indicator */}
              {passwordsMatch && (
                <p className="text-sm text-green-600 dark:text-green-400 flex items-center gap-1">
                  <span>&#10003;</span> As senhas coincidem
                </p>
              )}

              {/* Terms Acceptance */}
              <FormField
                control={form.control}
                name="termsAccepted"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
                    <FormControl>
                      <input
                        type="checkbox"
                        checked={field.value}
                        onChange={field.onChange}
                        disabled={isSubmitting}
                        className="mt-1 h-4 w-4 shrink-0 rounded border border-primary ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                      />
                    </FormControl>
                    <div className="space-y-1 leading-none">
                      <FormLabel>
                        Aceito os Termos de Uso
                      </FormLabel>
                      <FormMessage />
                    </div>
                  </FormItem>
                )}
              />

              {/* Submit */}
              <Button type="submit" className="w-full" disabled={isSubmitting}>
                {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Criar conta
              </Button>
            </form>
          </Form>

          {/* Footer link */}
          <div className="mt-6 text-center text-sm text-muted-foreground">
            Já tem conta?{" "}
            <a href="/auth/login" className="text-primary hover:underline font-medium">
              Fazer login &rarr;
            </a>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}