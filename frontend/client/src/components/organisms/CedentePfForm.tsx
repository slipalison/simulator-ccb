// ---------------------------------------------------------------------------
// CedentePfForm: create Cedente PF (T-4) — CPF + Nome + optional fields
// ---------------------------------------------------------------------------

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createCedentePfSchema,
  type CreateCedentePfData,
} from "@/lib/fundos-schemas";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2 } from "lucide-react";

interface CedentePfFormProps {
  onSubmit: (data: CreateCedentePfData) => Promise<void>;
  onCancel: () => void;
  isSubmitting?: boolean;
  serverErrors?: Record<string, string[]>;
}

export function CedentePfForm({
  onSubmit,
  onCancel,
  isSubmitting,
}: CedentePfFormProps) {
  const form = useForm<CreateCedentePfData>({
    resolver: zodResolver(createCedentePfSchema),
    defaultValues: {
      cpf: "",
      nome: "",
      email: "",
      telefone: "",
      endereco: "",
    },
  });

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = form;

  const serverError = (errors as any)?.root?.serverError?.message as string | undefined;

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="space-y-4"
      noValidate
      aria-label="Criar cedente pessoa física"
    >
      {serverError && (
        <div role="alert" className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {serverError}
        </div>
      )}

      <div className="space-y-1">
        <Label htmlFor="cpf">CPF</Label>
        <Input
          id="cpf"
          {...register("cpf")}
          aria-invalid={!!errors.cpf}
          placeholder="Apenas números (11 dígitos)"
          maxLength={11}
        />
        {errors.cpf && (
          <p className="text-sm text-destructive" role="alert">{errors.cpf.message}</p>
        )}
      </div>

      <div className="space-y-1">
        <Label htmlFor="nome">Nome Completo</Label>
        <Input id="nome" {...register("nome")} aria-invalid={!!errors.nome} />
        {errors.nome && (
          <p className="text-sm text-destructive" role="alert">{errors.nome.message}</p>
        )}
      </div>

      <div className="space-y-1">
        <Label htmlFor="email">Email</Label>
        <Input id="email" type="email" {...register("email" as any)} placeholder="Opcional" />
        {(errors as any).email && (
          <p className="text-sm text-destructive" role="alert">{(errors as any).email.message}</p>
        )}
      </div>

      <div className="space-y-1">
        <Label htmlFor="telefone">Telefone</Label>
        <Input id="telefone" {...register("telefone" as any)} placeholder="Opcional" />
      </div>

      <div className="space-y-1">
        <Label htmlFor="endereco">Endereço</Label>
        <Input id="endereco" {...register("endereco" as any)} placeholder="Opcional" />
      </div>

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
          Cancelar
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />}
          Criar Cedente PF
        </Button>
      </div>
    </form>
  );
}
