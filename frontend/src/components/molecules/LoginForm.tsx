import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { loginSchema, type LoginData } from "@/lib/validation-schemas";
import { LabeledField } from "@/components/molecules/LabeledField";
import { AppButton } from "@/components/atoms/AppButton";

export interface LoginFormProps {
  onSubmit: (data: LoginData) => void | Promise<void>;
  serverError?: string | null;
}

/**
 * Molecule: login form with RHF + Zod validation.
 * Calls parent onSubmit with valid data — API call happens in parent.
 */
export function LoginForm({ onSubmit, serverError }: LoginFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginData>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  const isDisabled = isSubmitting;

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="space-y-4 w-full max-w-sm"
      noValidate
    >
      {serverError && (
        <p className="text-sm text-red-600" role="alert">
          {serverError}
        </p>
      )}

      <LabeledField
        id="email"
        label="Email"
        error={errors.email?.message}
        inputProps={{
          type: "email",
          placeholder: "seu@email.com",
          disabled: isDisabled,
          ...register("email"),
        }}
      />

      <LabeledField
        id="password"
        label="Senha"
        error={errors.password?.message}
        inputProps={{
          type: "password",
          placeholder: "Sua senha",
          disabled: isDisabled,
          ...register("password"),
        }}
      />

      <AppButton type="submit" className="w-full" disabled={isDisabled}>
        {isSubmitting ? "Entrando..." : "Entrar"}
      </AppButton>
    </form>
  );
}
