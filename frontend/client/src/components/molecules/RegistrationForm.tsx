import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate } from "@tanstack/react-router";
import { PersonTypeRadio } from "@/components/molecules/PersonTypeRadio";
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
  registrationSchema,
  type RegistrationData,
} from "@/lib/validation-schemas";
import {
  registerClient,
  RegistrationValidationError,
  DuplicateClientError,
  RegistrationUnavailable,
  ApiError,
} from "@/lib/api";
import { useAuth } from "@/lib/auth-context";

/**
 * RegistrationForm: unified PF/PJ registration form with shadcn/ui
 * Auto-login after successful registration
 */
export function RegistrationForm() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]> | null>(null);

  const form = useForm<RegistrationData>({
    resolver: zodResolver(registrationSchema),
    defaultValues: {
      personType: "PF",
      email: "",
      phone: "",
      password: "",
      confirmPassword: "",
      nome: "",
      cpf: "",
      razaoSocial: "",
      cnpj: "",
    },
  });

  const personType = form.watch("personType");
  const password = form.watch("password");
  const confirmPassword = form.watch("confirmPassword");

  // Reset conditional fields when personType changes
  useEffect(() => {
    if (personType === "PF") {
      form.setValue("razaoSocial", "");
      form.setValue("cnpj", "");
    } else {
      form.setValue("nome", "");
      form.setValue("cpf", "");
    }
    // Clear field errors for the switched type
    setFieldErrors(null);
  }, [personType, form]);

  // Map server-side field errors to RHF setError
  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, messages]) => {
        form.setError(field as keyof RegistrationData, {
          type: "server",
          message: messages[0],
        });
      });
    }
  }, [fieldErrors, form]);

  const handleBlurStripDigits = (field: "cpf" | "cnpj" | "phone") => {
    return (e: React.FocusEvent<HTMLInputElement>) => {
      const stripped = e.target.value.replace(/\D/g, "");
      form.setValue(field, stripped, { shouldValidate: true });
    };
  };

  const onSubmit = async (data: RegistrationData) => {
    setIsSubmitting(true);
    setSubmitError(null);
    setFieldErrors(null);

    try {
      await registerClient({
        nome: data.personType === "PF" ? data.nome : undefined,
        cpf: data.personType === "PF" ? data.cpf : undefined,
        razaoSocial: data.personType === "PJ" ? data.razaoSocial : undefined,
        cnpj: data.personType === "PJ" ? data.cnpj : undefined,
        email: data.email,
        phone: data.phone,
        password: data.password,
      });

      // After registration, redirect to ACF login (Keycloak handles authentication)
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

  const isPf = personType === "PF";
  const passwordsMatch = password && confirmPassword && password === confirmPassword;

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
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6" noValidate>
              {/* Person Type */}
              <FormField
                control={form.control}
                name="personType"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Tipo de pessoa</FormLabel>
                    <FormControl>
                      <PersonTypeRadio
                        value={field.value}
                        onChange={(value) => {
                          field.onChange(value);
                        }}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Server error */}
              {submitError && (
                <Alert variant="destructive">
                  <AlertDescription>{submitError}</AlertDescription>
                </Alert>
              )}

              {/* PF Fields */}
              {isPf && (
                <FormField
                  control={form.control}
                  name="nome"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Nome completo</FormLabel>
                      <FormControl>
                        <Input
                          placeholder="João da Silva"
                          disabled={isSubmitting}
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              )}

              {isPf && (
                <FormField
                  control={form.control}
                  name="cpf"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>CPF</FormLabel>
                      <FormControl>
                        <Input
                          placeholder="000.000.000-00"
                          inputMode="numeric"
                          disabled={isSubmitting}
                          {...field}
                          onBlur={handleBlurStripDigits("cpf")}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              )}

              {/* PJ Fields */}
              {!isPf && (
                <FormField
                  control={form.control}
                  name="razaoSocial"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Razão Social</FormLabel>
                      <FormControl>
                        <Input
                          placeholder="Empresa LTDA"
                          disabled={isSubmitting}
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              )}

              {!isPf && (
                <FormField
                  control={form.control}
                  name="cnpj"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>CNPJ</FormLabel>
                      <FormControl>
                        <Input
                          placeholder="00.000.000/0000-00"
                          inputMode="numeric"
                          disabled={isSubmitting}
                          {...field}
                          onBlur={handleBlurStripDigits("cnpj")}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
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
                        onBlur={handleBlurStripDigits("phone")}
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
                        value={field.value}
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
                        value={field.value}
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

              {/* Submit */}
              <Button type="submit" className="w-full" disabled={isSubmitting}>
                {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Criar conta
              </Button>
            </form>
          </Form>

          {/* Footer link */}
          <div className="mt-6 text-center text-sm text-muted-foreground">
            Ja tem conta?{" "}
            <a href="/auth/login" className="text-primary hover:underline font-medium">
              Fazer login &rarr;
            </a>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
