// ---------------------------------------------------------------------------
// CedenteTipoToggle: PF/PJ toggle for Cedente form (T-4)
// ---------------------------------------------------------------------------

import { Button } from "@/components/ui/button";
import type { CedenteTipo } from "@/lib/fundos-schemas";

interface CedenteTipoToggleProps {
  value: CedenteTipo;
  onChange: (tipo: CedenteTipo) => void;
  disabled?: boolean;
}

/**
 * Toggle button pair for selecting PF (Pessoa Física) or PJ (Pessoa Jurídica).
 */
export function CedenteTipoToggle({
  value,
  onChange,
  disabled,
}: CedenteTipoToggleProps) {
  return (
    <fieldset className="flex gap-2" aria-label="Tipo de cedente">
      <legend className="sr-only">Tipo de cedente</legend>
      <Button
        type="button"
        variant={value === "PF" ? "default" : "outline"}
        size="sm"
        onClick={() => onChange("PF")}
        disabled={disabled}
        aria-pressed={value === "PF"}
        aria-label="Pessoa Física"
      >
        Pessoa Física
      </Button>
      <Button
        type="button"
        variant={value === "PJ" ? "default" : "outline"}
        size="sm"
        onClick={() => onChange("PJ")}
        disabled={disabled}
        aria-pressed={value === "PJ"}
        aria-label="Pessoa Jurídica"
      >
        Pessoa Jurídica
      </Button>
    </fieldset>
  );
}
