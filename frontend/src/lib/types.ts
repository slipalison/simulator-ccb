// ---------------------------------------------------------------------------
// Shared TypeScript types
// ---------------------------------------------------------------------------
// Mirrors backend DTOs — keep in sync with Onboarding.API models.
// ---------------------------------------------------------------------------

/**
 * Mirrors backend ClientProfileDto record.
 * Returned by GET /api/clients/me.
 */
export interface ClientProfileDto {
  id: string;
  name: string;
  email: string;
  phone: string;
  type: "PessoaFisica" | "PessoaJuridica";
  cpf?: string | null;
  cnpj?: string | null;
  razaoSocial?: string | null;
}
