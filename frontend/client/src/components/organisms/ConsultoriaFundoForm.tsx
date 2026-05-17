// ---------------------------------------------------------------------------
// ConsultoriaFundoForm: create/edit form for ConsultoriaFundo (T-5)
// ---------------------------------------------------------------------------

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createConsultoriaFundoSchema,
  updateConsultoriaFundoSchema,
  ConsultoriaFundoStatusEnum,
  SIMPLE_STATUS_LABELS,
  type ConsultoriaFundoDto,
  type CreateConsultoriaFundoData,
  type UpdateConsultoriaFundoData,
} from "@/lib/fundos-schemas";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Loader2 } from "lucide-react";

interface ConsultoriaFundoFormProps {
  mode: "create" | "edit";
  initial?: ConsultoriaFundoDto;
  onSubmit: (data: CreateConsultoriaFundoData | UpdateConsultoriaFundoData) => Promise<void>;
  onCancel: () => void;
  isSubmitting?: boolean;
}

export function ConsultoriaFundoForm({
  mode,
  initial,
  onSubmit,
  onCancel,
  isSubmitting,
}: ConsultoriaFundoFormProps) {
  const isEdit = mode === "edit";
  const schema = isEdit ? updateConsultoriaFundoSchema : createConsultoriaFundoSchema;

  const form = useForm<CreateConsultoriaFundoData | UpdateConsultoriaFundoData>({
    resolver: zodResolver(schema),
    defaultValues: isEdit
      ? {
          razaoSocial: initial?.razaoSocial ?? "",
          nomeFantasia: initial?.nomeFantasia ?? "",
          email: initial?.email ?? "",
          telefone: initial?.telefone ?? "",
          status: initial?.status ?? "ATIVO",
        }
      : {
          razaoSocial: "",
          cnpj: "",
          nomeFantasia: "",
          email: "",
          telefone: "",
        },
  });

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = form;

  const serverError = (errors as any)?.root?.serverError?.message as string | undefined;

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="space-y-4"
      noValidate
      aria-label={isEdit ? "Editar consultoria de fundo" : "Criar consultoria de fundo"}
    >
      {serverError && (
        <div role="alert" className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {serverError}
        </div>
      )}

      <div className="space-y-1">
        <Label htmlFor="razaoSocial">Razão Social</Label>
        <Input id="razaoSocial" {...register("razaoSocial")} aria-invalid={!!errors.razaoSocial} />
        {errors.razaoSocial && (
          <p className="text-sm text-destructive" role="alert">{errors.razaoSocial.message}</p>
        )}
      </div>

      {!isEdit && (
        <div className="space-y-1">
          <Label htmlFor="cnpj">CNPJ</Label>
          <Input
            id="cnpj"
            {...register("cnpj" as any)}
            aria-invalid={!!(errors as any).cnpj}
            placeholder="Apenas números (14 dígitos)"
            maxLength={14}
          />
          {(errors as any).cnpj && (
            <p className="text-sm text-destructive" role="alert">{(errors as any).cnpj.message}</p>
          )}
        </div>
      )}

      <div className="space-y-1">
        <Label htmlFor="nomeFantasia">Nome Fantasia</Label>
        <Input id="nomeFantasia" {...register("nomeFantasia" as any)} placeholder="Opcional" />
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

      {isEdit && (
        <div className="space-y-1">
          <Label htmlFor="status">Status</Label>
          <Select
            defaultValue={initial?.status ?? "ATIVO"}
            onValueChange={(v) => setValue("status" as any, v as any)}
          >
            <SelectTrigger id="status" aria-label="Selecionar status">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {ConsultoriaFundoStatusEnum.options.map((s) => (
                <SelectItem key={s} value={s}>
                  {SIMPLE_STATUS_LABELS[s] ?? s}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
          Cancelar
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />}
          {isEdit ? "Salvar alterações" : "Criar"}
        </Button>
      </div>
    </form>
  );
}
