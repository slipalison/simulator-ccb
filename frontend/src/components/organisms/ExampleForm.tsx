import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { LabeledField } from "@/components/molecules/LabeledField";
import { AppButton } from "@/components/atoms/AppButton";

// Schema Zod — fonte da verdade para validação client-side
const exampleSchema = z.object({
  name: z
    .string()
    .min(1, "Nome é obrigatório")
    .min(2, "Nome deve ter pelo menos 2 caracteres"),
  email: z.string().min(1, "Email é obrigatório").email("Email inválido"),
});

type ExampleFormData = z.infer<typeof exampleSchema>;

/**
 * Organism: formulário de exemplo com React Hook Form + Zod.
 * Demonstra validação inline — erros aparecem abaixo de cada campo.
 * onSubmit é no-op nesta phase (sem integração de API).
 */
export function ExampleForm() {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ExampleFormData>({
    resolver: zodResolver(exampleSchema),
    defaultValues: { name: "", email: "" },
  });

  const onSubmit = (data: ExampleFormData) => {
    // No-op nesta phase — integração com API em Phase 8
    console.log("Dados válidos:", data);
  };

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="space-y-4 w-full max-w-sm"
      noValidate
    >
      <LabeledField
        id="name"
        label="Nome"
        error={errors.name?.message}
        inputProps={{
          placeholder: "Seu nome completo",
          ...register("name"),
        }}
      />
      <LabeledField
        id="email"
        label="Email"
        error={errors.email?.message}
        inputProps={{
          type: "email",
          placeholder: "seu@email.com",
          ...register("email"),
        }}
      />
      <AppButton type="submit" className="w-full">
        Enviar
      </AppButton>
    </form>
  );
}
