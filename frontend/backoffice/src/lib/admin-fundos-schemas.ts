// ---------------------------------------------------------------------------
// Admin Fundos Zod schemas (backoffice) — D-4 independent, D-29
// Mirrors backend Admin*Dto records exactly (field names, types).
// PascalCase from C# serialized as camelCase by System.Text.Json default.
// ---------------------------------------------------------------------------

import { z } from "zod";
import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";

// ---------------------------------------------------------------------------
// Shared paginated response wrapper
// ---------------------------------------------------------------------------

export const paginatedResultSchema = <T extends z.ZodTypeAny>(itemSchema: T) =>
  z.object({
    items: z.array(itemSchema),
    totalCount: z.number().int().nonnegative(),
    page: z.number().int().positive(),
    pageSize: z.number().int().positive(),
  });

// ---------------------------------------------------------------------------
// URL search params schemas (D-31, TanStack Router validateSearch)
// ---------------------------------------------------------------------------

export const adminListSearchSchema = z.object({
  page: z.number().int().min(1).catch(1),
  search: z.string().optional().catch(undefined),
  empresaId: z.string().uuid().optional().catch(undefined),
});

export type AdminListSearch = z.infer<typeof adminListSearchSchema>;

// ---------------------------------------------------------------------------
// AdminFundoDto — mirrors AdminFundoDto.cs
// TipoFundo enum: 0=Aberto, 1=Fechado (int from backend by default)
// FundoStatus enum: RASCUNHO | ATIVO | SUSPENSO | EM_LIQUIDACAO | ENCERRADO
// ---------------------------------------------------------------------------

export const adminFundoSchema = z.object({
  id: z.string().uuid(),
  clienteId: z.string().uuid(),
  empresaNome: z.string(),
  nome: z.string(),
  cnpj: z.string(),
  consultoriaFundoId: z.string().uuid(),
  custodianteId: z.string().uuid(),
  tipoFundo: z.union([z.number(), z.string()]),  // int enum or string
  classeAnbima: z.string().nullable(),
  segmento: z.string().nullable(),
  dataConstituicao: z.string().nullable(),
  status: z.string(),
  createdAt: z.string(),
});

export type AdminFundoDto = z.infer<typeof adminFundoSchema>;

export const adminFundoListSchema = paginatedResultSchema(adminFundoSchema);

// ---------------------------------------------------------------------------
// AdminConsultoriaFundoDto — mirrors AdminConsultoriaFundoDto.cs
// ConsultoriaFundoStatus: ATIVO | INATIVO
// ---------------------------------------------------------------------------

export const adminConsultoriaFundoSchema = z.object({
  id: z.string().uuid(),
  clienteId: z.string().uuid(),
  empresaNome: z.string(),
  razaoSocial: z.string(),
  nomeFantasia: z.string().nullable(),
  cnpj: z.string(),
  email: z.string().nullable(),
  telefone: z.string().nullable(),
  status: z.string(),
  createdAt: z.string(),
});

export type AdminConsultoriaFundoDto = z.infer<typeof adminConsultoriaFundoSchema>;

export const adminConsultoriaFundoListSchema = paginatedResultSchema(adminConsultoriaFundoSchema);

// ---------------------------------------------------------------------------
// AdminCustodianteDto — mirrors AdminCustodianteDto.cs
// CustodianteStatus: ATIVO | INATIVO
// ---------------------------------------------------------------------------

export const adminCustodianteSchema = z.object({
  id: z.string().uuid(),
  clienteId: z.string().uuid(),
  empresaNome: z.string(),
  razaoSocial: z.string(),
  codigoInterno: z.string().nullable(),
  cnpj: z.string(),
  email: z.string().nullable(),
  telefone: z.string().nullable(),
  status: z.string(),
  createdAt: z.string(),
});

export type AdminCustodianteDto = z.infer<typeof adminCustodianteSchema>;

export const adminCustodianteListSchema = paginatedResultSchema(adminCustodianteSchema);

// ---------------------------------------------------------------------------
// AdminCedenteDto — mirrors AdminCedenteDto.cs
// CedenteTipo: 0=PF, 1=PJ (int enum)
// CedenteStatus: ATIVO | INATIVO
// ---------------------------------------------------------------------------

export const adminCedenteSchema = z.object({
  id: z.string().uuid(),
  clienteId: z.string().uuid(),
  empresaNome: z.string(),
  documento: z.string(),
  nome: z.string(),
  email: z.string().nullable(),
  telefone: z.string().nullable(),
  endereco: z.string().nullable(),
  cedenteTipo: z.union([z.number(), z.string()]),
  status: z.string(),
  createdAt: z.string(),
});

export type AdminCedenteDto = z.infer<typeof adminCedenteSchema>;

export const adminCedenteListSchema = paginatedResultSchema(adminCedenteSchema);

// ---------------------------------------------------------------------------
// AdminRelFundoCedenteDto — mirrors AdminRelFundoCedenteDto.cs
// RelationshipStatus: ATIVO | INATIVO | HISTORICO
// ---------------------------------------------------------------------------

export const adminRelFundoCedenteSchema = z.object({
  id: z.string().uuid(),
  clienteId: z.string().uuid(),
  empresaNome: z.string(),
  fundoId: z.string().uuid(),
  fundoNome: z.string(),
  cedenteId: z.string().uuid(),
  limitePercentual: z.number().nullable(),
  limiteValor: z.number().nullable(),
  dataInicio: z.string(),
  dataFim: z.string().nullable(),
  status: z.string(),
  createdAt: z.string(),
});

export type AdminRelFundoCedenteDto = z.infer<typeof adminRelFundoCedenteSchema>;

export const adminRelFundoCedenteListSchema = paginatedResultSchema(adminRelFundoCedenteSchema);

// ---------------------------------------------------------------------------
// AdminRelFundoTipoAtivoDto — mirrors AdminRelFundoTipoAtivoDto.cs
// ---------------------------------------------------------------------------

export const adminRelFundoTipoAtivoSchema = z.object({
  id: z.string().uuid(),
  clienteId: z.string().uuid(),
  empresaNome: z.string(),
  fundoId: z.string().uuid(),
  fundoNome: z.string(),
  tipoAtivoId: z.string().uuid(),
  limitePercentual: z.number().nullable(),
  limiteValor: z.number().nullable(),
  dataInicio: z.string(),
  dataFim: z.string().nullable(),
  status: z.string(),
  createdAt: z.string(),
});

export type AdminRelFundoTipoAtivoDto = z.infer<typeof adminRelFundoTipoAtivoSchema>;

export const adminRelFundoTipoAtivoListSchema = paginatedResultSchema(adminRelFundoTipoAtivoSchema);

// ---------------------------------------------------------------------------
// AdminRelCedenteTipoAtivoDto — mirrors AdminRelCedenteTipoAtivoDto.cs
// ---------------------------------------------------------------------------

export const adminRelCedenteTipoAtivoSchema = z.object({
  id: z.string().uuid(),
  clienteId: z.string().uuid(),
  empresaNome: z.string(),
  cedenteId: z.string().uuid(),
  tipoAtivoId: z.string().uuid(),
  limitePercentual: z.number().nullable(),
  limiteValor: z.number().nullable(),
  dataInicio: z.string(),
  dataFim: z.string().nullable(),
  status: z.string(),
  createdAt: z.string(),
});

export type AdminRelCedenteTipoAtivoDto = z.infer<typeof adminRelCedenteTipoAtivoSchema>;

export const adminRelCedenteTipoAtivoListSchema = paginatedResultSchema(adminRelCedenteTipoAtivoSchema);

// ---------------------------------------------------------------------------
// AuditLogEntry (entity-scoped filter) — extends existing AuditLogEntry (D-30)
// Backend endpoint: GET /api/admin/audit-log?entityType=X&entityId=Y
// ---------------------------------------------------------------------------

export const auditLogEntrySchema = z.object({
  id: z.string(),
  timestamp: z.string(),
  adminUserId: z.string(),
  adminUserName: z.string(),
  actionType: z.string(),
  targetUserId: z.string().nullable(),
  targetUserName: z.string().nullable(),
  details: z.string().nullable(),
  ipAddress: z.string().nullable(),
});

export type AuditLogEntry = z.infer<typeof auditLogEntrySchema>;

export const auditLogListSchema = paginatedResultSchema(auditLogEntrySchema);

// Re-export locale for convenience
export { L };
