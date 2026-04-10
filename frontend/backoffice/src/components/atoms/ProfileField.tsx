// ---------------------------------------------------------------------------
// Atom: ProfileField
// ---------------------------------------------------------------------------
// Read-only label + value display for profile data.
// No input — purely presentational.
// ---------------------------------------------------------------------------

export interface ProfileFieldProps {
  label: string;
  value: string;
}

/**
 * Atom: exibe um campo de perfil em modo somente leitura.
 * Label em cinza acima, valor em destaque abaixo.
 */
export function ProfileField({ label, value }: ProfileFieldProps) {
  return (
    <div className="space-y-1">
      <p className="text-sm font-medium text-muted-foreground">{label}</p>
      <p className="text-sm text-foreground">{value}</p>
    </div>
  );
}
