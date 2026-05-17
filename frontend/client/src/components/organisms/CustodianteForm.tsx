// ---------------------------------------------------------------------------
// CustodianteForm: create/edit form for Custodiante (T-5)
// ---------------------------------------------------------------------------

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createCustodianteSchema,
  updateCustodianteSchema,
  CustodianteStatusEnum,
  SIMPLE_STATUS_LABELS,
  type CustodianteDto,
  type CreateCustodianteData,
  type UpdateCustodianteData,
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

interface CustodianteFormProps {
  mode: "create" | "edit";
  initial?: CustodianteDto;
  onSubmit: (data: CreateCustodianteData | UpdateCustodianteData) => Promise<void>;
  onCancel: () => void;
  isSubmitting?: boolean;
}

export function CustodianteForm({
  mode,
  initial,
  onSubmit,
  onCancel,
  isSubmitting,
}: CustodianteFormProps) {
  const isEdit = mode === "edit";
  const schema = isEdit ? updateCustodianteSchema : createCustodianteSchema;

  const form = useForm<CreateCustodianteData | UpdateCustodianteData>({
    resolver: zodResolver(schema),
    defaultValues: isEdit
      ? {
          razaoSocial: initial?.razaoSocial ?? "",
          codigoInterno: initial?.codigoInterno ?? "",
          email: initial?.email ?? "",
          telefone: initial?.telefone ?? "",
          status: initial?.status ?? "ATIVO",
        }
      : {
          razaoSocial: "",
          cnpj: "",
          codigoInterno: "",
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
      aria-label={isEdit ? "Editar custodiante" : "Criar custodiante"}
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
        <Label htmlFor="codigoInterno">Código Interno</Label>
        <Input id="codigoInterno" {...register("codigoInterno" as any)} placeholder="Opcional" />
      </div>

      <div className="space-y-1">
        <Label htmlFor="email">Email</Label>
        <Input id="email" type="email" {...register("email" as any)} placeholder="Opcional" />
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
              {CustodianteStatusEnum.options.map((s) => (
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
