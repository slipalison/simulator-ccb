// ---------------------------------------------------------------------------
// FundoForm: create/edit form for Fundo (T-6)
// Includes ConsultoriaFundo + Custodiante autocomplete dropdowns
// ---------------------------------------------------------------------------

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQuery } from "@tanstack/react-query";
import {
  createFundoSchema,
  updateFundoSchema,
  TipoFundoEnum,
  TIPO_FUNDO_LABELS,
  type FundoDto,
  type CreateFundoData,
  type UpdateFundoData,
} from "@/lib/fundos-schemas";
import { listConsultoriasFundo, listCustodiantes } from "@/lib/fundos-api";
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

interface FundoFormProps {
  mode: "create" | "edit";
  initial?: FundoDto;
  onSubmit: (data: CreateFundoData | UpdateFundoData) => Promise<void>;
  onCancel: () => void;
  isSubmitting?: boolean;
}

export function FundoForm({
  mode,
  initial,
  onSubmit,
  onCancel,
  isSubmitting,
}: FundoFormProps) {
  const isEdit = mode === "edit";
  const schema = isEdit ? updateFundoSchema : createFundoSchema;

  const form = useForm<CreateFundoData | UpdateFundoData>({
    resolver: zodResolver(schema),
    defaultValues: isEdit
      ? {
          nome: initial?.nome ?? "",
          classeAnbima: initial?.classeAnbima ?? "",
          segmento: initial?.segmento ?? "",
          dataConstituicao: initial?.dataConstituicao
            ? initial.dataConstituicao.split("T")[0]
            : "",
        }
      : {
          nome: "",
          cnpj: "",
          consultoriaFundoId: "",
          custodianteId: "",
          tipoFundo: "RendaFixa",
          classeAnbima: "",
          segmento: "",
          dataConstituicao: "",
        },
  });

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = form;

  const { data: consultorias } = useQuery({
    queryKey: ["consultorias-fundo-select"],
    queryFn: () => listConsultoriasFundo({ page: 1, pageSize: 100, search: "" }),
    enabled: !isEdit,
  });

  const { data: custodiantes } = useQuery({
    queryKey: ["custodiantes-select"],
    queryFn: () => listCustodiantes({ page: 1, pageSize: 100, search: "" }),
    enabled: !isEdit,
  });

  const serverError = (errors as any)?.root?.serverError?.message as string | undefined;

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="space-y-4"
      noValidate
      aria-label={isEdit ? "Editar fundo" : "Criar fundo"}
    >
      {serverError && (
        <div role="alert" className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {serverError}
        </div>
      )}

      <div className="space-y-1">
        <Label htmlFor="nome">Nome do Fundo</Label>
        <Input id="nome" {...register("nome")} aria-invalid={!!errors.nome} />
        {errors.nome && (
          <p className="text-sm text-destructive" role="alert">{errors.nome.message}</p>
        )}
      </div>

      {!isEdit && (
        <>
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

          <div className="space-y-1">
            <Label htmlFor="consultoriaFundoId">Consultoria de Fundo</Label>
            <Select
              defaultValue=""
              onValueChange={(v) => setValue("consultoriaFundoId" as any, v)}
            >
              <SelectTrigger id="consultoriaFundoId" aria-label="Selecionar consultoria">
                <SelectValue placeholder="Selecione uma consultoria" />
              </SelectTrigger>
              <SelectContent>
                {(consultorias?.items ?? []).map((c) => (
                  <SelectItem key={c.id} value={c.id}>
                    {c.razaoSocial}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {(errors as any).consultoriaFundoId && (
              <p className="text-sm text-destructive" role="alert">
                {(errors as any).consultoriaFundoId.message}
              </p>
            )}
          </div>

          <div className="space-y-1">
            <Label htmlFor="custodianteId">Custodiante</Label>
            <Select
              defaultValue=""
              onValueChange={(v) => setValue("custodianteId" as any, v)}
            >
              <SelectTrigger id="custodianteId" aria-label="Selecionar custodiante">
                <SelectValue placeholder="Selecione um custodiante" />
              </SelectTrigger>
              <SelectContent>
                {(custodiantes?.items ?? []).map((c) => (
                  <SelectItem key={c.id} value={c.id}>
                    {c.razaoSocial}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {(errors as any).custodianteId && (
              <p className="text-sm text-destructive" role="alert">
                {(errors as any).custodianteId.message}
              </p>
            )}
          </div>

          <div className="space-y-1">
            <Label htmlFor="tipoFundo">Tipo de Fundo</Label>
            <Select
              defaultValue="RendaFixa"
              onValueChange={(v) => setValue("tipoFundo" as any, v as any)}
            >
              <SelectTrigger id="tipoFundo" aria-label="Selecionar tipo de fundo">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {TipoFundoEnum.options.map((t) => (
                  <SelectItem key={t} value={t}>
                    {TIPO_FUNDO_LABELS[t]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </>
      )}

      <div className="space-y-1">
        <Label htmlFor="classeAnbima">Classe ANBIMA</Label>
        <Input id="classeAnbima" {...register("classeAnbima" as any)} placeholder="Opcional" />
      </div>

      <div className="space-y-1">
        <Label htmlFor="segmento">Segmento</Label>
        <Input id="segmento" {...register("segmento" as any)} placeholder="Opcional" />
      </div>

      <div className="space-y-1">
        <Label htmlFor="dataConstituicao">Data de Constituição</Label>
        <Input
          id="dataConstituicao"
          type="date"
          {...register("dataConstituicao" as any)}
          placeholder="Opcional"
        />
      </div>

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
          Cancelar
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />}
          {isEdit ? "Salvar alterações" : "Criar Fundo"}
        </Button>
      </div>
    </form>
  );
}
