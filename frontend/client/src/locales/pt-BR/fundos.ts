// ---------------------------------------------------------------------------
// Fundos module locale strings (pt-BR)
// Centralised here so fundos-schemas.ts stays schema-only (no inline strings).
// ---------------------------------------------------------------------------

export const fundosLocale = {
  required: "Campo obrigatório",
  cnpjInvalido: "CNPJ inválido",
  cnpjFormat: "CNPJ deve conter 14 dígitos",
  cpfInvalido: "CPF inválido",
  cpfFormat: "CPF deve conter 11 dígitos",
  emailInvalido: "Email inválido",
  minNome: "Nome deve ter pelo menos 2 caracteres",
  minRazaoSocial: "Razão Social deve ter pelo menos 2 caracteres",
  minDescricao: "Descrição deve ter pelo menos 2 caracteres",
  minCodigo: "Código deve ter pelo menos 1 caractere",
  dataInicioObrigatoria: "Data de início é obrigatória",
  dataFimInvalida: "Data fim deve ser posterior à data início",
  limitePercentualRange: "Percentual deve ser entre 0 e 100",
  pageMin: "Página deve ser maior que 0",
  pageSizeRange: "Tamanho da página deve ser entre 1 e 100",
} as const;
