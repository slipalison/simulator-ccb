import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { LabeledField } from "@/components/molecules/LabeledField";
import { AppButton } from "@/components/atoms/AppButton";
import {
  pjRegistrationSchema,
  type PjRegistrationData,
} from "@/lib/validation-schemas";

export interface PjRegistrationFormProps {
  onSubmit?: (data: PjRegistrationData) => void | Promise<void>;
  isSubmitting?: boolean;
  fieldErrors?: Record<string, string[]>;
}

/**
 * Molecule: formul&#225;rio de cadastro PJ com RHF + Zod validation.
 * Campos: razão social, CNPJ, email, telefone, senha.
 * CNPJ e telefone: non-digit stripping on blur.
 */
export function PjRegistrationForm({
  onSubmit,
  isSubmitting: externalIsSubmitting,
  fieldErrors,
}: PjRegistrationFormProps) {
  const {
    register,
    handleSubmit,
    setValue,
    setError,
    formState: { errors, isSubmitting: internalIsSubmitting },
  } = useForm<PjRegistrationData>({
    resolver: zodResolver(pjRegistrationSchema),
    defaultValues: {
      razaoSocial: "",
      cnpj: "",
      email: "",
      phone: "",
      password: "",
    },
  });

  const isSubmitting = externalIsSubmitting ?? internalIsSubmitting;

  // Map server-side field errors to RHF setError
  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, messages]) => {
        setError(field as keyof PjRegistrationData, {
          type: "server",
          message: messages[0],
        });
      });
    }
  }, [fieldErrors, setError]);

  const handleBlurStripDigits = (
    field: "cnpj" | "phone"
  ) => {
    return (e: React.FocusEvent<HTMLInputElement>) => {
      const stripped = e.target.value.replace(/\D/g, "");
      setValue(field, stripped, { shouldValidate: true });
    };
  };

  const submitHandler = async (data: PjRegistrationData) => {
    if (onSubmit) {
      await onSubmit(data);
    } else {
      console.log("Dados PJ válidos:", data);
    }
  };

  return (
    <form onSubmit={handleSubmit(submitHandler)} className="space-y-4" noValidate>
      <LabeledField
        id="pj-razao-social"
        label="Razão Social"
        error={errors.razaoSocial?.message}
        inputProps={{
          placeholder: "Razão Social da empresa",
          ...register("razaoSocial"),
        }}
      />
      <LabeledField
        id="pj-cnpj"
        label="CNPJ"
        error={errors.cnpj?.message}
        inputProps={{
          placeholder: "00000000000000",
          inputMode: "numeric",
          ...register("cnpj"),
          onBlur: handleBlurStripDigits("cnpj"),
        }}
      />
      <LabeledField
        id="pj-email"
        label="Email"
        error={errors.email?.message}
        inputProps={{
          type: "email",
          placeholder: "seu@email.com",
          ...register("email"),
        }}
      />
      <LabeledField
        id="pj-phone"
        label="Telefone"
        error={errors.phone?.message}
        inputProps={{
          placeholder: "11999999999",
          inputMode: "tel",
          ...register("phone"),
          onBlur: handleBlurStripDigits("phone"),
        }}
      />
      <LabeledField
        id="pj-password"
        label="Senha"
        error={errors.password?.message}
        inputProps={{
          type: "password",
          placeholder: "Senha segura",
          ...register("password"),
        }}
      />
      <AppButton type="submit" disabled={isSubmitting} className="w-full">
        {isSubmitting ? "Criando..." : "Criar conta"}
      </AppButton>
    </form>
  );
}
