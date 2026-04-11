// ---------------------------------------------------------------------------
// Atom: ProfileBadge
// ---------------------------------------------------------------------------
// Visual indicator showing client type: Pessoa Física (PF) or Pessoa Jurídica (PJ).
// ---------------------------------------------------------------------------

export interface ProfileBadgeProps {
  type: "PessoaFisica" | "PessoaJuridica";
}

/**
 * Atom: exibe um badge colorido indicando o tipo de cliente.
 * Verde para Pessoa Física, azul para Pessoa Jurídica.
 */
export function ProfileBadge({ type }: ProfileBadgeProps) {
  if (type === "PessoaFisica") {
    return (
      <span className="inline-flex items-center rounded-full bg-green-100 px-3 py-1 text-xs font-medium text-green-800">
        Pessoa Física
      </span>
    );
  }

  return (
    <span className="inline-flex items-center rounded-full bg-blue-100 px-3 py-1 text-xs font-medium text-blue-800">
      Pessoa Jurídica
    </span>
  );
}
