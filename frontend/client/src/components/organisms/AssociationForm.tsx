// ---------------------------------------------------------------------------
// AssociationForm: shared form for N-N association creation (T-7)
// Used by FundoCedente, CedenteTipoAtivo, FundoTipoAtivo
// ---------------------------------------------------------------------------

import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createAssociationSchema,
  type CreateAssociationData,
} from "@/lib/fundos-schemas";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { DateRangeInput } from "@/components/molecules/DateRangeInput";
import { LimiteExposicaoInput } from "@/components/molecules/LimiteExposicaoInput";
import { Loader2 } from "lucide-react";

interface TargetOption {
  id: string;
  label: string;
}

interface AssociationFormProps {
  targetOptions: TargetOption[];
  targetLabel: string;
  onSubmit: (data: CreateAssociationData) => Promise<void>;
  onCancel: () => void;
  isSubmitting?: boolean;
}

/**
 * Shared form for creating N-N associations.
 * targetOptions: list of entities to associate with (Cedente, TipoAtivo, etc.)
 * targetLabel: label for the target entity dropdown ("Cedente", "Tipo de Ativo")
 */
export function AssociationForm({
  targetOptions,
  targetLabel,
  onSubmit,
  onCancel,
  isSubmitting,
}: AssociationFormProps) {
  const form = useForm<CreateAssociationData>({
    // zodResolver generic inference fails with cross-field refine schema — cast required
    resolver: zodResolver(createAssociationSchema) as never,
    defaultValues: {
      targetId: "",
      limitePercentual: null,
      limiteValor: null,
      dataInicio: new Date().toISOString().split("T")[0],
      dataFim: null,
    },
  });

  const {
    handleSubmit,
    control,
    setValue,
    watch,
    formState: { errors },
  } = form;

  const serverError = (errors as any)?.root?.serverError?.message as string | undefined;
  const dataInicio = watch("dataInicio");
  const dataFim = watch("dataFim");
  const limitePercentual = watch("limitePercentual");
  const limiteValor = watch("limiteValor");

  return (
    <form
      onSubmit={handleSubmit(onSubmit as any)}
      className="space-y-4"
      noValidate
      aria-label={`Criar associação com ${targetLabel}`}
    >
      {serverError && (
        <div role="alert" className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {serverError}
        </div>
      )}

      <div className="space-y-1">
        <Label htmlFor="targetId">
          {targetLabel}{" "}
          <span className="text-destructive" aria-label="obrigatório">*</span>
        </Label>
        <Controller
          name="targetId"
          control={control}
          render={({ field }) => (
            <Select
              value={field.value}
              onValueChange={field.onChange}
            >
              <SelectTrigger id="targetId" aria-label={`Selecionar ${targetLabel}`}>
                <SelectValue placeholder={`Selecione ${targetLabel}`} />
              </SelectTrigger>
              <SelectContent>
                {targetOptions.map((opt) => (
                  <SelectItem key={opt.id} value={opt.id}>
                    {opt.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        />
        {errors.targetId && (
          <p className="text-sm text-destructive" role="alert">{errors.targetId.message}</p>
        )}
      </div>

      <DateRangeInput
        dataInicio={dataInicio}
        dataFim={dataFim}
        onDataInicioChange={(v) => setValue("dataInicio", v)}
        onDataFimChange={(v) => setValue("dataFim", v || null)}
        dataInicioError={errors.dataInicio?.message}
        dataFimError={errors.dataFim?.message as string | undefined}
        disabled={isSubmitting}
      />

      <LimiteExposicaoInput
        limitePercentual={limitePercentual}
        limiteValor={limiteValor}
        onLimitePercentualChange={(v) => setValue("limitePercentual", v)}
        onLimiteValorChange={(v) => setValue("limiteValor", v)}
        limitePercentualError={errors.limitePercentual?.message as string | undefined}
        limiteValorError={errors.limiteValor?.message as string | undefined}
        disabled={isSubmitting}
      />

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
          Cancelar
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />}
          Criar Associação
        </Button>
      </div>
    </form>
  );
}
