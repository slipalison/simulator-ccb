// ---------------------------------------------------------------------------
// TipoAtivoForm: create/edit form for TipoAtivo (T-3)
// ---------------------------------------------------------------------------

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createTipoAtivoSchema,
  updateTipoAtivoSchema,
  TipoAtivoStatusEnum,
  TipoAtivoCategoriaEnum,
  TIPO_ATIVO_CATEGORIA_LABELS,
  SIMPLE_STATUS_LABELS,
  type TipoAtivoDto,
  type CreateTipoAtivoData,
  type UpdateTipoAtivoData,
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

interface TipoAtivoFormProps {
  mode: "create" | "edit";
  initial?: TipoAtivoDto;
  onSubmit: (data: CreateTipoAtivoData | UpdateTipoAtivoData) => Promise<void>;
  onCancel: () => void;
  isSubmitting?: boolean;
}

export function TipoAtivoForm({
  mode,
  initial,
  onSubmit,
  onCancel,
  isSubmitting,
}: TipoAtivoFormProps) {
  const isEdit = mode === "edit";
  const schema = isEdit ? updateTipoAtivoSchema : createTipoAtivoSchema;

  const form = useForm<CreateTipoAtivoData | UpdateTipoAtivoData>({
    // zodResolver generic inference incompatible with union schema — cast required
    resolver: zodResolver(schema) as never,
    defaultValues: isEdit
      ? {
          descricao: initial?.descricao ?? "",
          subcategoria: initial?.subcategoria ?? "",
          status: initial?.status ?? "ATIVO",
          ordemExibicao: initial?.ordemExibicao ?? 0,
        }
      : {
          codigo: "",
          descricao: "",
          categoria: "RendaFixa",
          subcategoria: "",
          ordemExibicao: 0,
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
      onSubmit={handleSubmit(onSubmit as any)}
      className="space-y-4"
      noValidate
      aria-label={isEdit ? "Editar tipo de ativo" : "Criar tipo de ativo"}
    >
      {serverError && (
        <div role="alert" className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {serverError}
        </div>
      )}

      {!isEdit && (
        <div className="space-y-1">
          <Label htmlFor="codigo">Código</Label>
          <Input
            id="codigo"
            {...register("codigo" as any)}
            aria-invalid={!!errors.root}
            placeholder="Ex: RF001"
          />
          {(errors as any).codigo && (
            <p className="text-sm text-destructive" role="alert">{(errors as any).codigo.message}</p>
          )}
        </div>
      )}

      <div className="space-y-1">
        <Label htmlFor="descricao">Descrição</Label>
        <Input
          id="descricao"
          {...register("descricao")}
          aria-invalid={!!errors.descricao}
          placeholder="Descrição do tipo de ativo"
        />
        {errors.descricao && (
          <p className="text-sm text-destructive" role="alert">{errors.descricao.message}</p>
        )}
      </div>

      {!isEdit && (
        <div className="space-y-1">
          <Label htmlFor="categoria">Categoria</Label>
          <Select
            defaultValue="RendaFixa"
            onValueChange={(v) => setValue("categoria" as any, v as any)}
          >
            <SelectTrigger id="categoria" aria-label="Selecionar categoria">
              <SelectValue placeholder="Selecione a categoria" />
            </SelectTrigger>
            <SelectContent>
              {TipoAtivoCategoriaEnum.options.map((cat) => (
                <SelectItem key={cat} value={cat}>
                  {TIPO_ATIVO_CATEGORIA_LABELS[cat]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          {(errors as any).categoria && (
            <p className="text-sm text-destructive" role="alert">{(errors as any).categoria.message}</p>
          )}
        </div>
      )}

      <div className="space-y-1">
        <Label htmlFor="subcategoria">Subcategoria</Label>
        <Input
          id="subcategoria"
          {...register("subcategoria" as any)}
          placeholder="Subcategoria (opcional)"
        />
      </div>

      <div className="space-y-1">
        <Label htmlFor="ordemExibicao">Ordem de Exibição</Label>
        <Input
          id="ordemExibicao"
          type="number"
          min={0}
          {...register("ordemExibicao")}
          placeholder="0"
        />
      </div>

      {isEdit && (
        <div className="space-y-1">
          <Label htmlFor="status">Status</Label>
          <Select
            defaultValue={initial?.status ?? "ATIVO"}
            onValueChange={(v) => setValue("status" as any, v as any)}
          >
            <SelectTrigger id="status" aria-label="Selecionar status">
              <SelectValue placeholder="Selecione o status" />
            </SelectTrigger>
            <SelectContent>
              {TipoAtivoStatusEnum.options.map((s) => (
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
