import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { LabeledField } from "@/components/molecules/LabeledField";
import { AppButton } from "@/components/atoms/AppButton";
import {
  pfRegistrationSchema,
  type PfRegistrationData,
} from "@/lib/validation-schemas";

export interface PfRegistrationFormProps {
  onSubmit?: (data: PfRegistrationData) => void | Promise<void>;
  isSubmitting?: boolean;
  fieldErrors?: Record<string, string[]>;
}

/**
 * Molecule: formul&#225;rio de cadastro PF com RHF + Zod validation.
 * Campos: nome, CPF, email, telefone, senha.
 * CPF e telefone: non-digit stripping on blur.
 */
export function PfRegistrationForm({
  onSubmit,
  isSubmitting: externalIsSubmitting,
  fieldErrors,
}: PfRegistrationFormProps) {
  const {
    register,
    handleSubmit,
    setValue,
    setError,
    formState: { errors, isSubmitting: internalIsSubmitting },
  } = useForm<PfRegistrationData>({
    resolver: zodResolver(pfRegistrationSchema),
    defaultValues: {
      nome: "",
      cpf: "",
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
        setError(field as keyof PfRegistrationData, {
          type: "server",
          message: messages[0],
        });
      });
    }
  }, [fieldErrors, setError]);

  const handleBlurStripDigits = (
    field: "cpf" | "phone"
  ) => {
    return (e: React.FocusEvent<HTMLInputElement>) => {
      const stripped = e.target.value.replace(/\D/g, "");
      setValue(field, stripped, { shouldValidate: true });
    };
  };

  const submitHandler = async (data: PfRegistrationData) => {
    if (onSubmit) {
      await onSubmit(data);
    } else {
      console.log("Dados PF válidos:", data);
    }
  };

  return (
    <form onSubmit={handleSubmit(submitHandler)} className="space-y-4" noValidate>
      <LabeledField
        id="pf-nome"
        label="Nome"
        error={errors.nome?.message}
        inputProps={{
          placeholder: "Nome completo",
          ...register("nome"),
        }}
      />
      <LabeledField
        id="pf-cpf"
        label="CPF"
        error={errors.cpf?.message}
        inputProps={{
          placeholder: "00000000000",
          inputMode: "numeric",
          ...register("cpf"),
          onBlur: handleBlurStripDigits("cpf"),
        }}
      />
      <LabeledField
        id="pf-email"
        label="Email"
        error={errors.email?.message}
        inputProps={{
          type: "email",
          placeholder: "seu@email.com",
          ...register("email"),
        }}
      />
      <LabeledField
        id="pf-phone"
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
        id="pf-password"
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
