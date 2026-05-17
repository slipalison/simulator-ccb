// ---------------------------------------------------------------------------
// LimiteExposicaoInput: percentual + valor exposure limits (T-7)
// Both are optional per D-21 (backend accepts both optional)
// ---------------------------------------------------------------------------

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

interface LimiteExposicaoInputProps {
  limitePercentual: number | null | undefined;
  limiteValor: number | null | undefined;
  onLimitePercentualChange: (value: number | null) => void;
  onLimiteValorChange: (value: number | null) => void;
  limitePercentualError?: string;
  limiteValorError?: string;
  disabled?: boolean;
}

/**
 * Exposure limit inputs: percentage (0-100) and absolute value.
 * Both optional. Shown side by side.
 */
export function LimiteExposicaoInput({
  limitePercentual,
  limiteValor,
  onLimitePercentualChange,
  onLimiteValorChange,
  limitePercentualError,
  limiteValorError,
  disabled,
}: LimiteExposicaoInputProps) {
  return (
    <div className="grid grid-cols-2 gap-4">
      <div className="space-y-1">
        <Label htmlFor="limitePercentual">
          Limite Percentual{" "}
          <span className="text-muted-foreground text-xs">(%)</span>
        </Label>
        <Input
          id="limitePercentual"
          type="number"
          min={0}
          max={100}
          step={0.01}
          value={limitePercentual ?? ""}
          onChange={(e) => {
            const v = e.target.value;
            onLimitePercentualChange(v === "" ? null : Number(v));
          }}
          aria-invalid={!!limitePercentualError}
          placeholder="0 a 100 (opcional)"
          disabled={disabled}
        />
        {limitePercentualError && (
          <p className="text-sm text-destructive" role="alert">
            {limitePercentualError}
          </p>
        )}
      </div>

      <div className="space-y-1">
        <Label htmlFor="limiteValor">
          Limite de Valor{" "}
          <span className="text-muted-foreground text-xs">(R$)</span>
        </Label>
        <Input
          id="limiteValor"
          type="number"
          min={0}
          step={0.01}
          value={limiteValor ?? ""}
          onChange={(e) => {
            const v = e.target.value;
            onLimiteValorChange(v === "" ? null : Number(v));
          }}
          aria-invalid={!!limiteValorError}
          placeholder="Valor em R$ (opcional)"
          disabled={disabled}
        />
        {limiteValorError && (
          <p className="text-sm text-destructive" role="alert">
            {limiteValorError}
          </p>
        )}
      </div>
    </div>
  );
}
