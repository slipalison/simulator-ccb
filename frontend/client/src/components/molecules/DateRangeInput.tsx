// ---------------------------------------------------------------------------
// DateRangeInput: half-open [dataInicio, dataFim) date range (D-20)
// dataFim is optional (null = infinite validity)
// ---------------------------------------------------------------------------

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

interface DateRangeInputProps {
  dataInicio: string;
  dataFim: string | null | undefined;
  onDataInicioChange: (value: string) => void;
  onDataFimChange: (value: string) => void;
  dataInicioError?: string;
  dataFimError?: string;
  disabled?: boolean;
}

/**
 * Half-open date range input [dataInicio, dataFim) per D-20.
 * dataFim is optional — null/empty means infinite validity.
 */
export function DateRangeInput({
  dataInicio,
  dataFim,
  onDataInicioChange,
  onDataFimChange,
  dataInicioError,
  dataFimError,
  disabled,
}: DateRangeInputProps) {
  return (
    <div className="grid grid-cols-2 gap-4">
      <div className="space-y-1">
        <Label htmlFor="dataInicio">
          Data de Início{" "}
          <span className="text-destructive" aria-label="obrigatório">*</span>
        </Label>
        <Input
          id="dataInicio"
          type="date"
          value={dataInicio}
          onChange={(e) => onDataInicioChange(e.target.value)}
          aria-invalid={!!dataInicioError}
          aria-required="true"
          disabled={disabled}
        />
        {dataInicioError && (
          <p className="text-sm text-destructive" role="alert">{dataInicioError}</p>
        )}
      </div>

      <div className="space-y-1">
        <Label htmlFor="dataFim">
          Data de Fim{" "}
          <span className="text-muted-foreground text-xs">(opcional — vazio = vigência infinita)</span>
        </Label>
        <Input
          id="dataFim"
          type="date"
          value={dataFim ?? ""}
          onChange={(e) => onDataFimChange(e.target.value)}
          aria-invalid={!!dataFimError}
          min={dataInicio || undefined}
          disabled={disabled}
        />
        {dataFimError && (
          <p className="text-sm text-destructive" role="alert">{dataFimError}</p>
        )}
      </div>
    </div>
  );
}
