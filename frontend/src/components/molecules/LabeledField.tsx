import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import type { ComponentProps } from "react";

export interface LabeledFieldProps {
  id: string;
  label: string;
  error?: string;
  inputProps?: ComponentProps<typeof Input>;
}

/**
 * Molecule: Label + Input + mensagem de erro inline.
 * Erro exibido com role="alert" para acessibilidade.
 */
export function LabeledField({ id, label, error, inputProps }: LabeledFieldProps) {
  return (
    <div className="space-y-1">
      <Label htmlFor={id} className="text-left">{label}</Label>
      <Input
        id={id}
        aria-invalid={!!error}
        aria-describedby={error ? `${id}-error` : undefined}
        {...inputProps}
      />
      {error && (
        <p id={`${id}-error`} role="alert" className="text-sm text-destructive">
          {error}
        </p>
      )}
    </div>
  );
}
