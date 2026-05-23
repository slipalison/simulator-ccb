// ---------------------------------------------------------------------------
// Admin Fundos API client (backoffice) — D-4 independent, D-29
// Typed query functions for cross-company read-only endpoints.
// Uses adminFetch (credentials: include, 401 → restore session).
// Endpoints: /api/admin/fundos/* — BearerBackoffice + CrossCompanyAccess.
// ---------------------------------------------------------------------------

import { adminFetch } from "@/lib/admin-http-interceptor";
import { AdminApiError } from "@/lib/admin-api";
import type {
  AdminFundoDto,
  AdminConsultoriaFundoDto,
  AdminCustodianteDto,
  AdminCedenteDto,
  AdminRelFundoCedenteDto,
  AdminRelFundoTipoAtivoDto,
  AdminRelCedenteTipoAtivoDto,
  AuditLogEntry,
} from "@/lib/admin-fundos-schemas";

// ---------------------------------------------------------------------------
// Shared types
// ---------------------------------------------------------------------------

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminFundosListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  companyId?: string;
}

export interface AdminAssociationListParams {
  page?: number;
  pageSize?: number;
  companyId?: string;
}

export interface AuditHistoryParams {
  entityType: string;
  entityId: string;
  page?: number;
  pageSize?: number;
}

// ---------------------------------------------------------------------------
// Internal: build query string from params
// ---------------------------------------------------------------------------

function buildQuery(params: Record<string, string | number | undefined>): string {
  const sp = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== "" && v !== null) {
      sp.set(k, String(v));
    }
  }
  const qs = sp.toString();
  return qs ? `?${qs}` : "";
}

async function adminGet<T>(url: string): Promise<T> {
  const response = await adminFetch(url, { method: "GET" });
  if (!response.ok) {
    throw new AdminApiError(`Falha ao carregar dados: ${url}`, response.status);
  }
  return response.json() as Promise<T>;
}

// ---------------------------------------------------------------------------
// GET /api/admin/fundos — Paginated cross-company list of all Fundos
// ---------------------------------------------------------------------------

export async function listAdminFundos(
  params: AdminFundosListParams = {}
): Promise<PaginatedResult<AdminFundoDto>> {
  const qs = buildQuery({
    page: params.page,
    pageSize: params.pageSize,
    search: params.search,
    companyId: params.companyId,
  });
  return adminGet<PaginatedResult<AdminFundoDto>>(`/api/admin/fundos${qs}`);
}

// ---------------------------------------------------------------------------
// GET /api/admin/fundos/consultorias — ConsultoriaFundo cross-company list
// ---------------------------------------------------------------------------

export async function listAdminConsultorias(
  params: AdminFundosListParams = {}
): Promise<PaginatedResult<AdminConsultoriaFundoDto>> {
  const qs = buildQuery({
    page: params.page,
    pageSize: params.pageSize,
    search: params.search,
    companyId: params.companyId,
  });
  return adminGet<PaginatedResult<AdminConsultoriaFundoDto>>(`/api/admin/fundos/consultorias${qs}`);
}

// ---------------------------------------------------------------------------
// GET /api/admin/fundos/custodiantes — Custodiante cross-company list
// ---------------------------------------------------------------------------

export async function listAdminCustodiantes(
  params: AdminFundosListParams = {}
): Promise<PaginatedResult<AdminCustodianteDto>> {
  const qs = buildQuery({
    page: params.page,
    pageSize: params.pageSize,
    search: params.search,
    companyId: params.companyId,
  });
  return adminGet<PaginatedResult<AdminCustodianteDto>>(`/api/admin/fundos/custodiantes${qs}`);
}

// ---------------------------------------------------------------------------
// GET /api/admin/fundos/cedentes — Cedente cross-company list
// ---------------------------------------------------------------------------

export async function listAdminCedentes(
  params: AdminFundosListParams = {}
): Promise<PaginatedResult<AdminCedenteDto>> {
  const qs = buildQuery({
    page: params.page,
    pageSize: params.pageSize,
    search: params.search,
    companyId: params.companyId,
  });
  return adminGet<PaginatedResult<AdminCedenteDto>>(`/api/admin/fundos/cedentes${qs}`);
}

// ---------------------------------------------------------------------------
// N-N association lists (no search param — backend endpoints don't expose it)
// ---------------------------------------------------------------------------

export async function listAdminFundoCedentes(
  params: AdminAssociationListParams = {}
): Promise<PaginatedResult<AdminRelFundoCedenteDto>> {
  const qs = buildQuery({
    page: params.page,
    pageSize: params.pageSize,
    companyId: params.companyId,
  });
  return adminGet<PaginatedResult<AdminRelFundoCedenteDto>>(`/api/admin/fundos/fundo-cedentes${qs}`);
}

export async function listAdminFundoTiposAtivos(
  params: AdminAssociationListParams = {}
): Promise<PaginatedResult<AdminRelFundoTipoAtivoDto>> {
  const qs = buildQuery({
    page: params.page,
    pageSize: params.pageSize,
    companyId: params.companyId,
  });
  return adminGet<PaginatedResult<AdminRelFundoTipoAtivoDto>>(`/api/admin/fundos/fundo-tipos-ativos${qs}`);
}

export async function listAdminCedenteTiposAtivos(
  params: AdminAssociationListParams = {}
): Promise<PaginatedResult<AdminRelCedenteTipoAtivoDto>> {
  const qs = buildQuery({
    page: params.page,
    pageSize: params.pageSize,
    companyId: params.companyId,
  });
  return adminGet<PaginatedResult<AdminRelCedenteTipoAtivoDto>>(`/api/admin/fundos/cedente-tipos-ativos${qs}`);
}

// ---------------------------------------------------------------------------
// GET /api/admin/audit-log?entityType=X&entityId=Y — filtered audit history (D-30)
// T-1 backend adds entityType + entityId filter. Until T-1 ships, returns all
// entries (backward-compatible — additive params ignored by old endpoint).
// ---------------------------------------------------------------------------

export async function getAuditHistory(
  params: AuditHistoryParams
): Promise<PaginatedResult<AuditLogEntry>> {
  const qs = buildQuery({
    entityType: params.entityType,
    entityId: params.entityId,
    page: params.page,
    pageSize: params.pageSize ?? 10,
  });
  return adminGet<PaginatedResult<AuditLogEntry>>(`/api/admin/audit-log${qs}`);
}
