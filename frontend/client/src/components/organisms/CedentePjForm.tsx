// ---------------------------------------------------------------------------
// CedentePjForm: create Cedente PJ (T-4) — CNPJ + RazaoSocial + optional fields
// ---------------------------------------------------------------------------

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createCedentePjSchema,
  type CreateCedentePjData,
} from "@/lib/fundos-schemas";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2 } from "lucide-react";

interface CedentePjFormProps {
  onSubmit: (data: CreateCedentePjData) => Promise<void>;
  onCancel: () => void;
  isSubmitting?: boolean;
}

export function CedentePjForm({
  onSubmit,
  onCancel,
  isSubmitting,
}: CedentePjFormProps) {
  const form = useForm<CreateCedentePjData>({
    resolver: zodResolver(createCedentePjSchema),
    defaultValues: {
      cnpj: "",
      razaoSocial: "",
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
      aria-label="Criar cedente pessoa jurídica"
    >
      {serverError && (
        <div role="alert" className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {serverError}
        </div>
      )}

      <div className="space-y-1">
        <Label htmlFor="cnpj">CNPJ</Label>
        <Input
          id="cnpj"
          {...register("cnpj")}
          aria-invalid={!!errors.cnpj}
          placeholder="Apenas números (14 dígitos)"
          maxLength={14}
        />
        {errors.cnpj && (
          <p className="text-sm text-destructive" role="alert">{errors.cnpj.message}</p>
        )}
      </div>

      <div className="space-y-1">
        <Label htmlFor="razaoSocial">Razão Social</Label>
        <Input id="razaoSocial" {...register("razaoSocial")} aria-invalid={!!errors.razaoSocial} />
        {errors.razaoSocial && (
          <p className="text-sm text-destructive" role="alert">{errors.razaoSocial.message}</p>
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
          Criar Cedente PJ
        </Button>
      </div>
    </form>
  );
}
