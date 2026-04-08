import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate } from "@tanstack/react-router";
import { PersonTypeRadio } from "@/components/molecules/PersonTypeRadio";
import { PasswordField } from "@/components/molecules/PasswordField";
import { PasswordStrengthMeter } from "@/components/molecules/PasswordStrengthMeter";
import { AppButton } from "@/components/atoms/AppButton";
import {
  registrationSchema,
  type RegistrationData,
} from "@/lib/validation-schemas";
import {
  registerClient,
  loginClient,
  RegistrationValidationError,
  DuplicateClientError,
  RegistrationUnavailable,
  ApiError,
  LoginError,
} from "@/lib/api";
import { useAuth } from "@/lib/auth-context";

/**
 * RegistrationForm: unified PF/PJ registration form with dynamic fields
 * Uses Radio button to toggle between PF and PJ fields
 * Auto-login after successful registration
 */
export function RegistrationForm() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]> | null>(null);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    setError,
    formState: { errors },
    trigger,
  } = useForm<RegistrationData>({
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

  const personType = watch("personType");
  const password = watch("password");

  // Reset conditional fields when personType changes
  useEffect(() => {
    if (personType === "PF") {
      setValue("razaoSocial", "");
      setValue("cnpj", "");
    } else {
      setValue("nome", "");
      setValue("cpf", "");
    }
    // Clear field errors for the switched type
    setFieldErrors(null);
  }, [personType, setValue]);

  // Map server-side field errors to RHF setError
  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, messages]) => {
        setError(field as keyof RegistrationData, {
          type: "server",
          message: messages[0],
        });
      });
    }
  }, [fieldErrors, setError]);

  const handleBlurStripDigits = (field: "cpf" | "cnpj" | "phone") => {
    return (e: React.FocusEvent<HTMLInputElement>) => {
      const stripped = e.target.value.replace(/\D/g, "");
      setValue(field, stripped, { shouldValidate: true });
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

      // Auto-login after successful registration
      try {
        await login(data.email, data.password);
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        navigate({ to: "/profile" as any, replace: true });
      } catch {
        // Fallback to login page if auto-login fails
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        navigate({ to: "/login" as any, state: { message: "Cadastro criado. Faça login." } as any });
      }
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

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6" noValidate>
      {/* Person Type Radio */}
      <PersonTypeRadio
        value={personType}
        onChange={(value) => setValue("personType", value, { shouldValidate: true })}
      />

      {/* Server error */}
      {submitError && (
        <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-600" role="alert">
          {submitError}
        </div>
      )}

      {/* Common fields */}
      <div className="space-y-4">
        {isPf ? (
          <>
            <div className="space-y-1">
              <label htmlFor="nome" className="block text-sm font-medium text-foreground">
                Nome
              </label>
              <input
                id="nome"
                placeholder="Nome completo"
                className={`w-full rounded-md border px-3 py-2 text-sm transition-colors focus:outline-none focus:ring-2 focus:ring-primary ${
                  errors.nome ? "border-red-300 bg-red-50" : "border-input bg-background"
                }`}
                {...register("nome")}
                aria-invalid={!!errors.nome}
              />
              {errors.nome && (
                <p className="text-xs text-red-600" role="alert">{errors.nome.message}</p>
              )}
            </div>
            <div className="space-y-1">
              <label htmlFor="cpf" className="block text-sm font-medium text-foreground">
                CPF
              </label>
              <input
                id="cpf"
                placeholder="00000000000"
                inputMode="numeric"
                className={`w-full rounded-md border px-3 py-2 text-sm transition-colors focus:outline-none focus:ring-2 focus:ring-primary ${
                  errors.cpf ? "border-red-300 bg-red-50" : "border-input bg-background"
                }`}
                {...register("cpf")}
                onBlur={handleBlurStripDigits("cpf")}
                aria-invalid={!!errors.cpf}
              />
              {errors.cpf && (
                <p className="text-xs text-red-600" role="alert">{errors.cpf.message}</p>
              )}
            </div>
          </>
        ) : (
          <>
            <div className="space-y-1">
              <label htmlFor="razaoSocial" className="block text-sm font-medium text-foreground">
                Razão Social
              </label>
              <input
                id="razaoSocial"
                placeholder="Razão Social da empresa"
                className={`w-full rounded-md border px-3 py-2 text-sm transition-colors focus:outline-none focus:ring-2 focus:ring-primary ${
                  errors.razaoSocial ? "border-red-300 bg-red-50" : "border-input bg-background"
                }`}
                {...register("razaoSocial")}
                aria-invalid={!!errors.razaoSocial}
              />
              {errors.razaoSocial && (
                <p className="text-xs text-red-600" role="alert">{errors.razaoSocial.message}</p>
              )}
            </div>
            <div className="space-y-1">
              <label htmlFor="cnpj" className="block text-sm font-medium text-foreground">
                CNPJ
              </label>
              <input
                id="cnpj"
                placeholder="00000000000000"
                inputMode="numeric"
                className={`w-full rounded-md border px-3 py-2 text-sm transition-colors focus:outline-none focus:ring-2 focus:ring-primary ${
                  errors.cnpj ? "border-red-300 bg-red-50" : "border-input bg-background"
                }`}
                {...register("cnpj")}
                onBlur={handleBlurStripDigits("cnpj")}
                aria-invalid={!!errors.cnpj}
              />
              {errors.cnpj && (
                <p className="text-xs text-red-600" role="alert">{errors.cnpj.message}</p>
              )}
            </div>
          </>
        )}

        {/* Email */}
        <div className="space-y-1">
          <label htmlFor="email" className="block text-sm font-medium text-foreground">
            Email
          </label>
          <input
            id="email"
            type="email"
            placeholder="seu@email.com"
            className={`w-full rounded-md border px-3 py-2 text-sm transition-colors focus:outline-none focus:ring-2 focus:ring-primary ${
              errors.email ? "border-red-300 bg-red-50" : "border-input bg-background"
            }`}
            {...register("email")}
            aria-invalid={!!errors.email}
          />
          {errors.email && (
            <p className="text-xs text-red-600" role="alert">{errors.email.message}</p>
          )}
        </div>

        {/* Phone */}
        <div className="space-y-1">
          <label htmlFor="phone" className="block text-sm font-medium text-foreground">
            Telefone
          </label>
          <input
            id="phone"
            placeholder="11999999999"
            inputMode="tel"
            className={`w-full rounded-md border px-3 py-2 text-sm transition-colors focus:outline-none focus:ring-2 focus:ring-primary ${
              errors.phone ? "border-red-300 bg-red-50" : "border-input bg-background"
            }`}
            {...register("phone")}
            onBlur={handleBlurStripDigits("phone")}
            aria-invalid={!!errors.phone}
          />
          {errors.phone && (
            <p className="text-xs text-red-600" role="alert">{errors.phone.message}</p>
          )}
        </div>

        {/* Password */}
        <PasswordField
          id="password"
          label="Senha"
          value={password}
          onChange={(value) => setValue("password", value, { shouldValidate: true })}
          error={errors.password?.message}
        />

        {/* Password Strength Meter */}
        <PasswordStrengthMeter password={password ?? ""} />

        {/* Confirm Password */}
        <PasswordField
          id="confirmPassword"
          label="Confirmar Senha"
          value={watch("confirmPassword") ?? ""}
          onChange={(value) => setValue("confirmPassword", value, { shouldValidate: true })}
          error={errors.confirmPassword?.message}
        />
      </div>

      {/* Submit button */}
      <AppButton type="submit" disabled={isSubmitting} className="w-full">
        {isSubmitting ? "Criando..." : "Criar conta"}
      </AppButton>
    </form>
  );
}
