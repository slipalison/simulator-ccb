import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate } from "@tanstack/react-router";
import { PersonTypeRadio } from "@/components/molecules/PersonTypeRadio";
import { PasswordField } from "@/components/molecules/PasswordField";
import { PasswordStrengthMeter } from "@/components/molecules/PasswordStrengthMeter";
import { AppButton } from "@/components/atoms/AppButton";
import { LabeledField } from "@/components/molecules/LabeledField";
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
import { AuthLayout } from "@/components/templates/AuthLayout";

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
    <AuthLayout
      title="Criar Conta"
      subtitle="Preencha seus dados para criar sua conta"
      footer={
        <p className="text-center text-sm text-slate-600">
          Já tem conta?{" "}
          <a href="/login" className="font-medium text-primary hover:underline">
            Faça login
          </a>
        </p>
      }
    >
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
              <LabeledField
                id="nome"
                label="Nome"
                error={errors.nome?.message}
                inputProps={{
                  placeholder: "Nome completo",
                  disabled: isSubmitting,
                  ...register("nome"),
                }}
              />
              <LabeledField
                id="cpf"
                label="CPF"
                error={errors.cpf?.message}
                inputProps={{
                  placeholder: "00000000000",
                  inputMode: "numeric",
                  disabled: isSubmitting,
                  ...register("cpf"),
                  onBlur: handleBlurStripDigits("cpf"),
                }}
              />
            </>
          ) : (
            <>
              <LabeledField
                id="razaoSocial"
                label="Razão Social"
                error={errors.razaoSocial?.message}
                inputProps={{
                  placeholder: "Razão Social da empresa",
                  disabled: isSubmitting,
                  ...register("razaoSocial"),
                }}
              />
              <LabeledField
                id="cnpj"
                label="CNPJ"
                error={errors.cnpj?.message}
                inputProps={{
                  placeholder: "00000000000000",
                  inputMode: "numeric",
                  disabled: isSubmitting,
                  ...register("cnpj"),
                  onBlur: handleBlurStripDigits("cnpj"),
                }}
              />
            </>
          )}

          {/* Email */}
          <LabeledField
            id="email"
            label="Email"
            error={errors.email?.message}
            inputProps={{
              type: "email",
              placeholder: "seu@email.com",
              disabled: isSubmitting,
              ...register("email"),
            }}
          />

          {/* Phone */}
          <LabeledField
            id="phone"
            label="Telefone"
            error={errors.phone?.message}
            inputProps={{
              placeholder: "11999999999",
              inputMode: "tel",
              disabled: isSubmitting,
              ...register("phone"),
              onBlur: handleBlurStripDigits("phone"),
            }}
          />

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
        <div className="pt-2">
          <AppButton type="submit" disabled={isSubmitting} className="w-full">
            {isSubmitting ? "Criando..." : "Criar conta"}
          </AppButton>
        </div>
      </form>
    </AuthLayout>
  );
}
