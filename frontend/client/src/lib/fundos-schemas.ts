// ---------------------------------------------------------------------------
// Zod schemas for all Fundos module DTOs (D-24, D-27)
// Mirrors backend FluentValidation rules + domain enums exactly.
// Strings are in pt-BR per i18n convention (locale dict approach).
// ---------------------------------------------------------------------------

import { z } from "zod";
import { validateCnpj, validateCpf } from "@/lib/validation-schemas";
import { fundosLocale } from "@/locales/pt-BR/fundos";
export { fundosLocale } from "@/locales/pt-BR/fundos";

// ---------------------------------------------------------------------------
// Status enums — mirror domain enums exactly
// ---------------------------------------------------------------------------

export const CedenteStatusEnum = z.enum(["ATIVO", "INATIVO"]);
export type CedenteStatus = z.infer<typeof CedenteStatusEnum>;

export const ConsultoriaFundoStatusEnum = z.enum(["ATIVO", "INATIVO"]);
export type ConsultoriaFundoStatus = z.infer<typeof ConsultoriaFundoStatusEnum>;

export const CustodianteStatusEnum = z.enum(["ATIVO", "INATIVO"]);
export type CustodianteStatus = z.infer<typeof CustodianteStatusEnum>;

export const TipoAtivoStatusEnum = z.enum(["ATIVO", "INATIVO"]);
export type TipoAtivoStatus = z.infer<typeof TipoAtivoStatusEnum>;

export const FundoStatusEnum = z.enum([
  "RASCUNHO",
  "ATIVO",
  "SUSPENSO",
  "EM_LIQUIDACAO",
  "ENCERRADO",
]);
export type FundoStatus = z.infer<typeof FundoStatusEnum>;

export const RelationshipStatusEnum = z.enum(["ATIVO", "INATIVO", "HISTORICO"]);
export type RelationshipStatus = z.infer<typeof RelationshipStatusEnum>;

// ---------------------------------------------------------------------------
// TipoFundo enum — mirrors domain TipoFundo exactly
// ---------------------------------------------------------------------------

export const TipoFundoEnum = z.enum([
  "RendaFixa",
  "RendaVariavel",
  "Cambial",
  "Multimercado",
  "Outros",
]);
export type TipoFundo = z.infer<typeof TipoFundoEnum>;

// ---------------------------------------------------------------------------
// TipoAtivoCategoria enum
// ---------------------------------------------------------------------------

export const TipoAtivoCategoriaEnum = z.enum([
  "RendaFixa",
  "RendaVariavel",
  "Derivativo",
  "Misto",
  "Outros",
]);
export type TipoAtivoCategoria = z.infer<typeof TipoAtivoCategoriaEnum>;

// ---------------------------------------------------------------------------
// CedenteTipo enum
// ---------------------------------------------------------------------------

export const CedenteTipoEnum = z.enum(["PF", "PJ"]);
export type CedenteTipo = z.infer<typeof CedenteTipoEnum>;

// ---------------------------------------------------------------------------
// Paginated search params (D-27) — used with TanStack Router validateSearch
// ---------------------------------------------------------------------------

export const DEFAULT_PAGE_SIZE = 20;

export const paginatedSearchSchema = z.object({
  page: z.coerce
    .number()
    .int()
    .min(1, fundosLocale.pageMin)
    .optional()
    .default(1),
  search: z.string().optional().default(""),
  pageSize: z.coerce
    .number()
    .int()
    .min(1)
    .max(100)
    .optional()
    .default(DEFAULT_PAGE_SIZE),
});

export type PaginatedSearch = z.infer<typeof paginatedSearchSchema>;

export const fundoListSearchSchema = paginatedSearchSchema.extend({
  status: FundoStatusEnum.optional(),
});
export type FundoListSearch = z.infer<typeof fundoListSearchSchema>;

// ---------------------------------------------------------------------------
// Paginated result shape (mirrors backend PaginatedResult<T>)
// ---------------------------------------------------------------------------

export function paginatedResult<T extends z.ZodTypeAny>(itemSchema: T) {
  return z.object({
    items: z.array(itemSchema),
    totalCount: z.number(),
    page: z.number(),
    pageSize: z.number(),
    totalPages: z.number(),
  });
}

// ---------------------------------------------------------------------------
// TipoAtivo schemas
// ---------------------------------------------------------------------------

export const tipoAtivoSchema = z.object({
  id: z.string().uuid(),
  codigo: z.string(),
  descricao: z.string(),
  categoria: TipoAtivoCategoriaEnum,
  subcategoria: z.string().nullable(),
  status: TipoAtivoStatusEnum,
  ordemExibicao: z.number(),
});
export type TipoAtivoDto = z.infer<typeof tipoAtivoSchema>;

export const createTipoAtivoSchema = z.object({
  codigo: z
    .string()
    .min(1, fundosLocale.required)
    .min(1, fundosLocale.minCodigo),
  descricao: z
    .string()
    .min(1, fundosLocale.required)
    .min(2, fundosLocale.minDescricao),
  categoria: TipoAtivoCategoriaEnum,
  subcategoria: z.string().optional().nullable(),
  ordemExibicao: z.coerce.number().int().min(0).optional().default(0),
});
export type CreateTipoAtivoData = z.infer<typeof createTipoAtivoSchema>;

export const updateTipoAtivoSchema = z.object({
  descricao: z
    .string()
    .min(1, fundosLocale.required)
    .min(2, fundosLocale.minDescricao),
  subcategoria: z.string().optional().nullable(),
  status: TipoAtivoStatusEnum,
  ordemExibicao: z.coerce.number().int().min(0).optional().default(0),
});
export type UpdateTipoAtivoData = z.infer<typeof updateTipoAtivoSchema>;

// ---------------------------------------------------------------------------
// ConsultoriaFundo schemas
// ---------------------------------------------------------------------------

export const consultoriaFundoSchema = z.object({
  id: z.string().uuid(),
  razaoSocial: z.string(),
  nomeFantasia: z.string().nullable(),
  cnpj: z.string(),
  email: z.string().nullable(),
  telefone: z.string().nullable(),
  status: ConsultoriaFundoStatusEnum,
  createdAt: z.string(),
});
export type ConsultoriaFundoDto = z.infer<typeof consultoriaFundoSchema>;

export const createConsultoriaFundoSchema = z.object({
  razaoSocial: z
    .string()
    .min(1, fundosLocale.required)
    .min(2, fundosLocale.minRazaoSocial),
  cnpj: z
    .string()
    .min(1, fundosLocale.required)
    .regex(/^\d{14}$/, fundosLocale.cnpjFormat)
    .refine(validateCnpj, { message: fundosLocale.cnpjInvalido }),
  nomeFantasia: z.string().optional().nullable(),
  email: z
    .string()
    .optional()
    .nullable()
    .refine((v) => !v || z.string().email().safeParse(v).success, {
      message: fundosLocale.emailInvalido,
    }),
  telefone: z.string().optional().nullable(),
});
export type CreateConsultoriaFundoData = z.infer<typeof createConsultoriaFundoSchema>;

export const updateConsultoriaFundoSchema = z.object({
  razaoSocial: z
    .string()
    .min(1, fundosLocale.required)
    .min(2, fundosLocale.minRazaoSocial),
  nomeFantasia: z.string().optional().nullable(),
  email: z
    .string()
    .optional()
    .nullable()
    .refine((v) => !v || z.string().email().safeParse(v).success, {
      message: fundosLocale.emailInvalido,
    }),
  telefone: z.string().optional().nullable(),
  status: ConsultoriaFundoStatusEnum,
});
export type UpdateConsultoriaFundoData = z.infer<typeof updateConsultoriaFundoSchema>;

// ---------------------------------------------------------------------------
// Custodiante schemas
// ---------------------------------------------------------------------------

export const custodianteSchema = z.object({
  id: z.string().uuid(),
  razaoSocial: z.string(),
  codigoInterno: z.string().nullable(),
  cnpj: z.string(),
  email: z.string().nullable(),
  telefone: z.string().nullable(),
  status: CustodianteStatusEnum,
  createdAt: z.string(),
});
export type CustodianteDto = z.infer<typeof custodianteSchema>;

export const createCustodianteSchema = z.object({
  razaoSocial: z
    .string()
    .min(1, fundosLocale.required)
    .min(2, fundosLocale.minRazaoSocial),
  cnpj: z
    .string()
    .min(1, fundosLocale.required)
    .regex(/^\d{14}$/, fundosLocale.cnpjFormat)
    .refine(validateCnpj, { message: fundosLocale.cnpjInvalido }),
  codigoInterno: z.string().optional().nullable(),
  email: z
    .string()
    .optional()
    .nullable()
    .refine((v) => !v || z.string().email().safeParse(v).success, {
      message: fundosLocale.emailInvalido,
    }),
  telefone: z.string().optional().nullable(),
});
export type CreateCustodianteData = z.infer<typeof createCustodianteSchema>;

export const updateCustodianteSchema = z.object({
  razaoSocial: z
    .string()
    .min(1, fundosLocale.required)
    .min(2, fundosLocale.minRazaoSocial),
  codigoInterno: z.string().optional().nullable(),
  email: z
    .string()
    .optional()
    .nullable()
    .refine((v) => !v || z.string().email().safeParse(v).success, {
      message: fundosLocale.emailInvalido,
    }),
  telefone: z.string().optional().nullable(),
  status: CustodianteStatusEnum,
});
export type UpdateCustodianteData = z.infer<typeof updateCustodianteSchema>;

// ---------------------------------------------------------------------------
// Cedente schemas
// ---------------------------------------------------------------------------

export const cedenteSchema = z.object({
  id: z.string().uuid(),
  documento: z.string(),
  nome: z.string(),
  email: z.string().nullable(),
  telefone: z.string().nullable(),
  endereco: z.string().nullable(),
  cedenteTipo: CedenteTipoEnum,
  status: CedenteStatusEnum,
  createdAt: z.string(),
});
export type CedenteDto = z.infer<typeof cedenteSchema>;

export const createCedentePfSchema = z.object({
  cpf: z
    .string()
    .min(1, fundosLocale.required)
    .regex(/^\d{11}$/, fundosLocale.cpfFormat)
    .refine(validateCpf, { message: fundosLocale.cpfInvalido }),
  nome: z
    .string()
    .min(1, fundosLocale.required)
    .min(2, fundosLocale.minNome),
  email: z
    .string()
    .optional()
    .nullable()
    .refine((v) => !v || z.string().email().safeParse(v).success, {
      message: fundosLocale.emailInvalido,
    }),
  telefone: z.string().optional().nullable(),
  endereco: z.string().optional().nullable(),
});
export type CreateCedentePfData = z.infer<typeof createCedentePfSchema>;

export const createCedentePjSchema = z.object({
  cnpj: z
    .string()
    .min(1, fundosLocale.required)
    .regex(/^\d{14}$/, fundosLocale.cnpjFormat)
    .refine(validateCnpj, { message: fundosLocale.cnpjInvalido }),
  razaoSocial: z
    .string()
    .min(1, fundosLocale.required)
    .min(2, fundosLocale.minRazaoSocial),
  email: z
    .string()
    .optional()
    .nullable()
    .refine((v) => !v || z.string().email().safeParse(v).success, {
      message: fundosLocale.emailInvalido,
    }),
  telefone: z.string().optional().nullable(),
  endereco: z.string().optional().nullable(),
});
export type CreateCedentePjData = z.infer<typeof createCedentePjSchema>;

export const updateCedenteSchema = z.object({
  nome: z
    .string()
    .min(1, fundosLocale.required)
    .min(2, fundosLocale.minNome),
  email: z
    .string()
    .optional()
    .nullable()
    .refine((v) => !v || z.string().email().safeParse(v).success, {
      message: fundosLocale.emailInvalido,
    }),
  telefone: z.string().optional().nullable(),
  endereco: z.string().optional().nullable(),
  status: CedenteStatusEnum,
});
export type UpdateCedenteData = z.infer<typeof updateCedenteSchema>;

// ---------------------------------------------------------------------------
// Fundo schemas
// ---------------------------------------------------------------------------

export const fundoSchema = z.object({
  id: z.string().uuid(),
  nome: z.string(),
  cnpj: z.string(),
  consultoriaFundoId: z.string().uuid(),
  custodianteId: z.string().uuid(),
  tipoFundo: TipoFundoEnum,
  classeAnbima: z.string().nullable(),
  segmento: z.string().nullable(),
  dataConstituicao: z.string().nullable(),
  status: FundoStatusEnum,
  createdAt: z.string(),
});
export type FundoDto = z.infer<typeof fundoSchema>;

export const createFundoSchema = z.object({
  nome: z
    .string()
    .min(1, fundosLocale.required)
    .min(2, "Nome deve ter pelo menos 2 caracteres"),
  cnpj: z
    .string()
    .min(1, fundosLocale.required)
    .regex(/^\d{14}$/, fundosLocale.cnpjFormat)
    .refine(validateCnpj, { message: fundosLocale.cnpjInvalido }),
  consultoriaFundoId: z
    .string()
    .min(1, fundosLocale.required)
    .uuid("Consultoria de fundo inválida"),
  custodianteId: z
    .string()
    .min(1, fundosLocale.required)
    .uuid("Custodiante inválido"),
  tipoFundo: TipoFundoEnum,
  classeAnbima: z.string().optional().nullable(),
  segmento: z.string().optional().nullable(),
  dataConstituicao: z.string().optional().nullable(),
});
export type CreateFundoData = z.infer<typeof createFundoSchema>;

export const updateFundoSchema = z.object({
  nome: z
    .string()
    .min(1, fundosLocale.required)
    .min(2, "Nome deve ter pelo menos 2 caracteres"),
  classeAnbima: z.string().optional().nullable(),
  segmento: z.string().optional().nullable(),
  dataConstituicao: z.string().optional().nullable(),
});
export type UpdateFundoData = z.infer<typeof updateFundoSchema>;

export const transitionFundoStatusSchema = z.object({
  newStatus: FundoStatusEnum,
});
export type TransitionFundoStatusData = z.infer<typeof transitionFundoStatusSchema>;

// ---------------------------------------------------------------------------
// Association relationship schemas (D-20, D-21)
// All 3 associations: FundoCedente, CedenteTipoAtivo, FundoTipoAtivo
// ---------------------------------------------------------------------------

export const relFundoCedenteSchema = z.object({
  id: z.string().uuid(),
  fundoId: z.string().uuid(),
  cedenteId: z.string().uuid(),
  limitePercentual: z.number().nullable(),
  limiteValor: z.number().nullable(),
  dataInicio: z.string(),
  dataFim: z.string().nullable(),
  status: RelationshipStatusEnum,
  createdAt: z.string(),
});
export type RelFundoCedenteDto = z.infer<typeof relFundoCedenteSchema>;

export const relFundoTipoAtivoSchema = z.object({
  id: z.string().uuid(),
  fundoId: z.string().uuid(),
  tipoAtivoId: z.string().uuid(),
  limitePercentual: z.number().nullable(),
  limiteValor: z.number().nullable(),
  dataInicio: z.string(),
  dataFim: z.string().nullable(),
  status: RelationshipStatusEnum,
  createdAt: z.string(),
});
export type RelFundoTipoAtivoDto = z.infer<typeof relFundoTipoAtivoSchema>;

export const relCedenteTipoAtivoSchema = z.object({
  id: z.string().uuid(),
  cedenteId: z.string().uuid(),
  tipoAtivoId: z.string().uuid(),
  limitePercentual: z.number().nullable(),
  limiteValor: z.number().nullable(),
  dataInicio: z.string(),
  dataFim: z.string().nullable(),
  status: RelationshipStatusEnum,
  createdAt: z.string(),
});
export type RelCedenteTipoAtivoDto = z.infer<typeof relCedenteTipoAtivoSchema>;

// Create/Update schemas for associations
export const createAssociationSchema = z
  .object({
    targetId: z.string().uuid(fundosLocale.required),
    limitePercentual: z.coerce
      .number()
      .min(0, fundosLocale.limitePercentualRange)
      .max(100, fundosLocale.limitePercentualRange)
      .optional()
      .nullable(),
    limiteValor: z.coerce.number().min(0).optional().nullable(),
    dataInicio: z.string().min(1, fundosLocale.dataInicioObrigatoria),
    dataFim: z.string().optional().nullable(),
  })
  .refine(
    (data) => {
      if (!data.dataFim || !data.dataInicio) return true;
      return new Date(data.dataFim) > new Date(data.dataInicio);
    },
    {
      message: fundosLocale.dataFimInvalida,
      path: ["dataFim"],
    }
  );
export type CreateAssociationData = z.infer<typeof createAssociationSchema>;

export const updateAssociationLimitsSchema = z.object({
  limitePercentual: z.coerce
    .number()
    .min(0, fundosLocale.limitePercentualRange)
    .max(100, fundosLocale.limitePercentualRange)
    .optional()
    .nullable(),
  limiteValor: z.coerce.number().min(0).optional().nullable(),
});
export type UpdateAssociationLimitsData = z.infer<typeof updateAssociationLimitsSchema>;

export const transitionRelationshipStatusSchema = z.object({
  newStatus: RelationshipStatusEnum,
});
export type TransitionRelationshipStatusData = z.infer<
  typeof transitionRelationshipStatusSchema
>;

// ---------------------------------------------------------------------------
// Fundo status state machine (D-02, D-25) — frontend transition map
// Backend is source of truth; this is for UI pre-filtering only
// ---------------------------------------------------------------------------

export const FUNDO_STATUS_TRANSITIONS: Record<FundoStatus, FundoStatus[]> = {
  RASCUNHO: ["ATIVO"],
  ATIVO: ["SUSPENSO", "EM_LIQUIDACAO"],
  SUSPENSO: ["ATIVO"],
  EM_LIQUIDACAO: ["ENCERRADO"],
  ENCERRADO: [],
};

export const RELATIONSHIP_STATUS_TRANSITIONS: Record<RelationshipStatus, RelationshipStatus[]> = {
  ATIVO: ["INATIVO", "HISTORICO"],
  INATIVO: ["ATIVO", "HISTORICO"],
  HISTORICO: [],
};

// ---------------------------------------------------------------------------
// Status display labels (for UI — locale dict)
// ---------------------------------------------------------------------------

export const FUNDO_STATUS_LABELS: Record<FundoStatus, string> = {
  RASCUNHO: "Rascunho",
  ATIVO: "Ativo",
  SUSPENSO: "Suspenso",
  EM_LIQUIDACAO: "Em Liquidação",
  ENCERRADO: "Encerrado",
};

export const FUNDO_STATUS_COLORS: Record<FundoStatus, string> = {
  RASCUNHO: "bg-gray-500",
  ATIVO: "bg-green-500",
  SUSPENSO: "bg-yellow-500",
  EM_LIQUIDACAO: "bg-orange-500",
  ENCERRADO: "bg-red-500",
};

export const RELATIONSHIP_STATUS_LABELS: Record<RelationshipStatus, string> = {
  ATIVO: "Ativo",
  INATIVO: "Inativo",
  HISTORICO: "Histórico",
};

export const SIMPLE_STATUS_LABELS: Record<string, string> = {
  ATIVO: "Ativo",
  INATIVO: "Inativo",
};

export const TIPO_FUNDO_LABELS: Record<TipoFundo, string> = {
  RendaFixa: "Renda Fixa",
  RendaVariavel: "Renda Variável",
  Cambial: "Cambial",
  Multimercado: "Multimercado",
  Outros: "Outros",
};

export const TIPO_ATIVO_CATEGORIA_LABELS: Record<TipoAtivoCategoria, string> = {
  RendaFixa: "Renda Fixa",
  RendaVariavel: "Renda Variável",
  Derivativo: "Derivativo",
  Misto: "Misto",
  Outros: "Outros",
};
