import { useState, useCallback } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { PasswordField } from "@/components/molecules/PasswordField";
import { PasswordStrengthMeter } from "@/components/molecules/PasswordStrengthMeter";
import { TermsDialog } from "@/components/molecules/TermsDialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Loader2, ArrowLeft, ArrowRight } from "lucide-react";
import {
  companyDataSchema,
  companyAccessSchema,
  type CompanyData,
  type CompanyAccessData,
  validateCnpj,
} from "@/lib/validation-schemas";
import {
  registerCompany,
  RegistrationValidationError,
  DuplicateClientError,
  RegistrationUnavailable,
  ApiError,
} from "@/lib/api";

// ---------------------------------------------------------------------------
// CNPJ mask utility: applies XX.XXX.XXX/XXXX-XX format
// ---------------------------------------------------------------------------

function applyCnpjMask(value: string): string {
  const digits = value.replace(/\D/g, "").slice(0, 14);
  if (digits.length <= 2) return digits;
  if (digits.length <= 5) return `${digits.slice(0, 2)}.${digits.slice(2)}`;
  if (digits.length <= 8) return `${digits.slice(0, 2)}.${digits.slice(2, 5)}.${digits.slice(5, 8)}`;
  if (digits.length <= 12) return `${digits.slice(0, 2)}.${digits.slice(2, 5)}.${digits.slice(5, 8)}/${digits.slice(8)}`;
  return `${digits.slice(0, 2)}.${digits.slice(2, 5)}.${digits.slice(5, 8)}/${digits.slice(8, 12)}-${digits.slice(12)}`;
}

function stripCnpjMask(value: string): string {
  return value.replace(/\D/g, "");
}

// ---------------------------------------------------------------------------
// Phone mask utility: applies (XX) XXXXX-XXXX format
// ---------------------------------------------------------------------------

function applyPhoneMask(value: string): string {
  const digits = value.replace(/\D/g, "").slice(0, 11);
  if (digits.length <= 2) return `(${digits}`;
  if (digits.length <= 7) return `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
  return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
}

// ---------------------------------------------------------------------------
// RegistrationForm: PJ-only 2-step wizard
// Step 1: Dados da Empresa (razaoSocial + CNPJ)
// Step 2: Dados de Acesso (email + phone + password + terms)
// ---------------------------------------------------------------------------

const STEP_TITLES = ["Dados da Empresa", "Dados de Acesso"] as const;

export function RegistrationForm() {
  const [step, setStep] = useState<1 | 2>(1);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]> | null>(null);
  const [termsDialogOpen, setTermsDialogOpen] = useState(false);

  // Step 1 form: company data
  const step1Form = useForm<CompanyData>({
    resolver: zodResolver(companyDataSchema),
    defaultValues: {
      razaoSocial: "",
      cnpj: "",
    },
  });

  // Step 2 form: access data + terms
  const step2Form = useForm<CompanyAccessData>({
    resolver: zodResolver(companyAccessSchema),
    defaultValues: {
      email: "",
      phone: "",
      password: "",
      confirmPassword: "",
      termsAccepted: undefined as unknown as true,
    },
  });

  const password = step2Form.watch("password");
  const confirmPassword = step2Form.watch("confirmPassword");
  const passwordsMatch = password && confirmPassword && password === confirmPassword;

  // Map server-side field errors to RHF setError
  if (fieldErrors) {
    Object.entries(fieldErrors).forEach(([field, messages]) => {
      // Try setting on step2 form first (most likely), then step1
      const step2Fields = ["email", "phone", "password", "confirmPassword", "termsAccepted"];
      if (step2Fields.includes(field)) {
        step2Form.setError(field as keyof CompanyAccessData, {
          type: "server",
          message: messages[0],
        });
      } else {
        step1Form.setError(field as keyof CompanyData, {
          type: "server",
          message: messages[0],
        });
      }
    });
    setFieldErrors(null);
  }

  const handleStep1Next = useCallback(async () => {
    const valid = await step1Form.trigger();
    if (!valid) return;

    // Validate CNPJ with modulo-11 on submit
    const rawCnpj = stripCnpjMask(step1Form.getValues("cnpj"));
    if (!validateCnpj(rawCnpj)) {
      step1Form.setError("cnpj", { type: "manual", message: "CNPJ inválido" });
      return;
    }

    setStep(2);
  }, [step1Form]);

  const handleStep2Back = useCallback(() => {
    setStep(1);
  }, []);

  const onSubmit = useCallback(async (data: CompanyAccessData) => {
    setIsSubmitting(true);
    setSubmitError(null);

    const companyData = step1Form.getValues();

    try {
      await registerCompany({
        razaoSocial: companyData.razaoSocial,
        cnpj: stripCnpjMask(companyData.cnpj),
        email: data.email,
        phone: data.phone.replace(/\D/g, ""),
        password: data.password,
        termsAccepted: data.termsAccepted,
        termsVersion: "1.0",
      });

      // After registration, redirect to ACF login (POST → 201 → /)
      window.location.href = "/";
    } catch (err) {
      if (err instanceof RegistrationValidationError) {
        setFieldErrors(err.errors);
      } else if (err instanceof DuplicateClientError) {
        setSubmitError("CNPJ já cadastrado.");
      } else if (err instanceof RegistrationUnavailable) {
        setSubmitError("Serviço temporariamente indisponível. Tente novamente em alguns instantes.");
      } else if (err instanceof ApiError) {
        setSubmitError("Ocorreu um erro inesperado. Tente novamente.");
      } else {
        setSubmitError("Ocorreu um erro inesperado. Tente novamente.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }, [step1Form]);

  return (
    <Card className="w-full">
      <CardHeader className="space-y-1">
        <CardTitle className="text-2xl text-center">Criar sua conta</CardTitle>
        <div className="flex items-center justify-center gap-2 pt-2">
          <div className={`h-2 w-2 rounded-full ${step >= 1 ? "bg-primary" : "bg-muted"}`} />
          <div className={`h-2 w-full rounded-full ${step >= 2 ? "bg-primary" : "bg-muted"}`} />
        </div>
        <p className="text-center text-sm text-muted-foreground">
          Passo {step} de 2 — {STEP_TITLES[step - 1]}
        </p>
      </CardHeader>

      <CardContent>
        {submitError && (
          <Alert variant="destructive" className="mb-4">
            <AlertDescription>{submitError}</AlertDescription>
          </Alert>
        )}

        {/* ---- Step 1: Dados da Empresa ---- */}
        {step === 1 && (
          <Form {...step1Form}>
            <form className="space-y-6" noValidate>
              {/* Razão Social */}
              <FormField
                control={step1Form.control}
                name="razaoSocial"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Razão Social</FormLabel>
                    <FormControl>
                      <Input
                        placeholder="Nome da empresa"
                        disabled={isSubmitting}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* CNPJ */}
              <FormField
                control={step1Form.control}
                name="cnpj"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>CNPJ</FormLabel>
                    <FormControl>
                      <Input
                        placeholder="00.000.000/0000-00"
                        inputMode="numeric"
                        disabled={isSubmitting}
                        value={field.value}
                        onChange={(e) => {
                          // Strip mask on input, re-apply for display
                          const raw = stripCnpjMask(e.target.value);
                          field.onChange(raw);
                        }}
                        onBlur={(e) => {
                          // Apply mask on blur for display
                          const masked = applyCnpjMask(e.target.value);
                          field.onChange(stripCnpjMask(masked));
                          // Trigger validation
                          step1Form.trigger("cnpj");
                        }}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Continue button */}
              <Button
                type="button"
                className="w-full"
                onClick={handleStep1Next}
                disabled={isSubmitting}
              >
                Continuar
                <ArrowRight className="ml-2 h-4 w-4" />
              </Button>
            </form>
          </Form>
        )}

        {/* ---- Step 2: Dados de Acesso ---- */}
        {step === 2 && (
          <Form {...step2Form}>
            <form onSubmit={step2Form.handleSubmit(onSubmit)} className="space-y-6" noValidate>
              {/* Email */}
              <FormField
                control={step2Form.control}
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
                control={step2Form.control}
                name="phone"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Telefone</FormLabel>
                    <FormControl>
                      <Input
                        placeholder="(00) 00000-0000"
                        inputMode="tel"
                        disabled={isSubmitting}
                        value={field.value}
                        onChange={(e) => {
                          const masked = applyPhoneMask(e.target.value);
                          field.onChange(masked);
                        }}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Password */}
              <FormField
                control={step2Form.control}
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
                control={step2Form.control}
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
                control={step2Form.control}
                name="termsAccepted"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
                    <FormControl>
                      <Checkbox
                        checked={field.value}
                        onCheckedChange={field.onChange}
                        disabled={isSubmitting}
                      />
                    </FormControl>
                    <div className="space-y-1 leading-none">
                      <FormLabel className="cursor-pointer">
                        Aceito os{" "}
                        <button
                          type="button"
                          className="text-primary hover:underline font-medium"
                          onClick={(e) => {
                            e.preventDefault();
                            setTermsDialogOpen(true);
                          }}
                        >
                          Termos de Uso
                        </button>
                      </FormLabel>
                      <FormMessage />
                    </div>
                  </FormItem>
                )}
              />

              {/* Back + Submit buttons */}
              <div className="flex gap-3">
                <Button
                  type="button"
                  variant="outline"
                  className="flex-1"
                  onClick={handleStep2Back}
                  disabled={isSubmitting}
                >
                  <ArrowLeft className="mr-2 h-4 w-4" />
                  Voltar
                </Button>
                <Button
                  type="submit"
                  className="flex-1"
                  disabled={isSubmitting}
                >
                  {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  Cadastrar
                </Button>
              </div>
            </form>
          </Form>
        )}
      </CardContent>

      {/* Terms Dialog */}
      <TermsDialog open={termsDialogOpen} onOpenChange={setTermsDialogOpen} />
    </Card>
  );
}