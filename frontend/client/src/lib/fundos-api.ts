// ---------------------------------------------------------------------------
// Fundos module API functions (D-24)
// All functions use apiFetch as transport (auto-refresh 401 handled).
// Returns typed data or throws Response (caller handles via TanStack Query).
// ---------------------------------------------------------------------------

import { apiFetch } from "@/lib/api";
import type {
  TipoAtivoDto,
  ConsultoriaFundoDto,
  CustodianteDto,
  CedenteDto,
  FundoDto,
  RelFundoCedenteDto,
  RelFundoTipoAtivoDto,
  RelCedenteTipoAtivoDto,
  CreateTipoAtivoData,
  UpdateTipoAtivoData,
  CreateConsultoriaFundoData,
  UpdateConsultoriaFundoData,
  CreateCustodianteData,
  UpdateCustodianteData,
  CreateCedentePfData,
  CreateCedentePjData,
  UpdateCedenteData,
  CreateFundoData,
  UpdateFundoData,
  UpdateAssociationLimitsData,
  RelationshipStatus,
  FundoStatus,
  PaginatedSearch,
  FundoListSearch,
} from "@/lib/fundos-schemas";

// ---------------------------------------------------------------------------
// Paginated result shape
// ---------------------------------------------------------------------------

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ---------------------------------------------------------------------------
// Helper: build query string from pagination params
// ---------------------------------------------------------------------------

function buildPaginatedParams(params: PaginatedSearch): string {
  const sp = new URLSearchParams();
  sp.set("page", String(params.page ?? 1));
  sp.set("pageSize", String(params.pageSize ?? 20));
  if (params.search) sp.set("search", params.search);
  return sp.toString();
}

// ---------------------------------------------------------------------------
// TipoAtivo API (global — no company scope, CAD-19..21)
// ---------------------------------------------------------------------------

export async function listTiposAtivo(
  params: PaginatedSearch
): Promise<PaginatedResult<TipoAtivoDto>> {
  const qs = buildPaginatedParams(params);
  const res = await apiFetch(`/api/fundos/tipos-ativo?${qs}`);
  if (!res.ok) throw res;
  return res.json() as Promise<PaginatedResult<TipoAtivoDto>>;
}

export async function getTipoAtivo(id: string): Promise<TipoAtivoDto> {
  const res = await apiFetch(`/api/fundos/tipos-ativo/${id}`);
  if (!res.ok) throw res;
  return res.json() as Promise<TipoAtivoDto>;
}

export async function createTipoAtivo(data: CreateTipoAtivoData): Promise<TipoAtivoDto> {
  const res = await apiFetch("/api/fundos/tipos-ativo", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<TipoAtivoDto>;
}

export async function updateTipoAtivo(
  id: string,
  data: UpdateTipoAtivoData
): Promise<TipoAtivoDto> {
  const res = await apiFetch(`/api/fundos/tipos-ativo/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<TipoAtivoDto>;
}

// ---------------------------------------------------------------------------
// ConsultoriaFundo API (company-scoped, CAD-01..03)
// ---------------------------------------------------------------------------

export async function listConsultoriasFundo(
  params: PaginatedSearch
): Promise<PaginatedResult<ConsultoriaFundoDto>> {
  const qs = buildPaginatedParams(params);
  const res = await apiFetch(`/api/fundos/consultorias?${qs}`);
  if (!res.ok) throw res;
  return res.json() as Promise<PaginatedResult<ConsultoriaFundoDto>>;
}

export async function getConsultoriaFundo(id: string): Promise<ConsultoriaFundoDto> {
  const res = await apiFetch(`/api/fundos/consultorias/${id}`);
  if (!res.ok) throw res;
  return res.json() as Promise<ConsultoriaFundoDto>;
}

export async function createConsultoriaFundo(
  data: CreateConsultoriaFundoData
): Promise<ConsultoriaFundoDto> {
  const res = await apiFetch("/api/fundos/consultorias", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<ConsultoriaFundoDto>;
}

export async function updateConsultoriaFundo(
  id: string,
  data: UpdateConsultoriaFundoData
): Promise<ConsultoriaFundoDto> {
  const res = await apiFetch(`/api/fundos/consultorias/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<ConsultoriaFundoDto>;
}

// ---------------------------------------------------------------------------
// Custodiante API (company-scoped, CAD-05..07)
// ---------------------------------------------------------------------------

export async function listCustodiantes(
  params: PaginatedSearch
): Promise<PaginatedResult<CustodianteDto>> {
  const qs = buildPaginatedParams(params);
  const res = await apiFetch(`/api/fundos/custodiantes?${qs}`);
  if (!res.ok) throw res;
  return res.json() as Promise<PaginatedResult<CustodianteDto>>;
}

export async function getCustodiante(id: string): Promise<CustodianteDto> {
  const res = await apiFetch(`/api/fundos/custodiantes/${id}`);
  if (!res.ok) throw res;
  return res.json() as Promise<CustodianteDto>;
}

export async function createCustodiante(
  data: CreateCustodianteData
): Promise<CustodianteDto> {
  const res = await apiFetch("/api/fundos/custodiantes", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<CustodianteDto>;
}

export async function updateCustodiante(
  id: string,
  data: UpdateCustodianteData
): Promise<CustodianteDto> {
  const res = await apiFetch(`/api/fundos/custodiantes/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<CustodianteDto>;
}

// ---------------------------------------------------------------------------
// Cedente API (company-scoped, D-10 CPF/CNPJ composite unique, CAD-14..17)
// ---------------------------------------------------------------------------

export async function listCedentes(
  params: PaginatedSearch
): Promise<PaginatedResult<CedenteDto>> {
  const qs = buildPaginatedParams(params);
  const res = await apiFetch(`/api/fundos/cedentes?${qs}`);
  if (!res.ok) throw res;
  return res.json() as Promise<PaginatedResult<CedenteDto>>;
}

export async function getCedente(id: string): Promise<CedenteDto> {
  const res = await apiFetch(`/api/fundos/cedentes/${id}`);
  if (!res.ok) throw res;
  return res.json() as Promise<CedenteDto>;
}

export async function createCedentePf(data: CreateCedentePfData): Promise<CedenteDto> {
  const res = await apiFetch("/api/fundos/cedentes/pf", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<CedenteDto>;
}

export async function createCedentePj(data: CreateCedentePjData): Promise<CedenteDto> {
  const res = await apiFetch("/api/fundos/cedentes/pj", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<CedenteDto>;
}

export async function updateCedente(
  id: string,
  data: UpdateCedenteData
): Promise<CedenteDto> {
  const res = await apiFetch(`/api/fundos/cedentes/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<CedenteDto>;
}

// ---------------------------------------------------------------------------
// Fundo API (company-scoped, CAD-09..13)
// ---------------------------------------------------------------------------

export async function listFundos(
  params: FundoListSearch
): Promise<PaginatedResult<FundoDto>> {
  const sp = new URLSearchParams();
  sp.set("page", String(params.page ?? 1));
  sp.set("pageSize", String(params.pageSize ?? 20));
  if (params.search) sp.set("search", params.search);
  if (params.status) sp.set("status", params.status);
  const res = await apiFetch(`/api/fundos?${sp.toString()}`);
  if (!res.ok) throw res;
  return res.json() as Promise<PaginatedResult<FundoDto>>;
}

export async function getFundo(id: string): Promise<FundoDto> {
  const res = await apiFetch(`/api/fundos/${id}`);
  if (!res.ok) throw res;
  return res.json() as Promise<FundoDto>;
}

export async function createFundo(data: CreateFundoData): Promise<FundoDto> {
  const res = await apiFetch("/api/fundos", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<FundoDto>;
}

export async function updateFundo(id: string, data: UpdateFundoData): Promise<FundoDto> {
  const res = await apiFetch(`/api/fundos/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<FundoDto>;
}

export async function transitionFundoStatus(
  id: string,
  newStatus: FundoStatus
): Promise<FundoDto> {
  const res = await apiFetch(`/api/fundos/${id}/status`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ newStatus }),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<FundoDto>;
}

export async function getFundoAllowedTransitions(id: string): Promise<FundoStatus[]> {
  const res = await apiFetch(`/api/fundos/${id}/allowed-transitions`);
  if (!res.ok) throw res;
  return res.json() as Promise<FundoStatus[]>;
}

// ---------------------------------------------------------------------------
// FundoCedente association API (REL-01..04)
// ---------------------------------------------------------------------------

export async function listFundoCedentes(
  fundoId: string,
  params: PaginatedSearch
): Promise<PaginatedResult<RelFundoCedenteDto>> {
  const qs = buildPaginatedParams(params);
  const res = await apiFetch(`/api/fundos/${fundoId}/cedentes?${qs}`);
  if (!res.ok) throw res;
  return res.json() as Promise<PaginatedResult<RelFundoCedenteDto>>;
}

export async function getFundoCedente(
  fundoId: string,
  id: string
): Promise<RelFundoCedenteDto> {
  const res = await apiFetch(`/api/fundos/${fundoId}/cedentes/${id}`);
  if (!res.ok) throw res;
  return res.json() as Promise<RelFundoCedenteDto>;
}

export async function createFundoCedente(
  fundoId: string,
  data: { cedenteId: string; limitePercentual?: number | null; limiteValor?: number | null; dataInicio: string; dataFim?: string | null }
): Promise<RelFundoCedenteDto> {
  const res = await apiFetch(`/api/fundos/${fundoId}/cedentes`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<RelFundoCedenteDto>;
}

export async function updateFundoCedenteLimits(
  fundoId: string,
  id: string,
  data: UpdateAssociationLimitsData
): Promise<RelFundoCedenteDto> {
  const res = await apiFetch(`/api/fundos/${fundoId}/cedentes/${id}/limits`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<RelFundoCedenteDto>;
}

export async function transitionFundoCedenteStatus(
  fundoId: string,
  id: string,
  newStatus: RelationshipStatus
): Promise<RelFundoCedenteDto> {
  const res = await apiFetch(`/api/fundos/${fundoId}/cedentes/${id}/status`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ newStatus }),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<RelFundoCedenteDto>;
}

export async function getFundoCedenteAllowedTransitions(
  fundoId: string,
  id: string
): Promise<RelationshipStatus[]> {
  const res = await apiFetch(
    `/api/fundos/${fundoId}/cedentes/${id}/allowed-transitions`
  );
  if (!res.ok) throw res;
  return res.json() as Promise<RelationshipStatus[]>;
}

// ---------------------------------------------------------------------------
// FundoTipoAtivo association API
// ---------------------------------------------------------------------------

export async function listFundoTiposAtivo(
  fundoId: string,
  params: PaginatedSearch
): Promise<PaginatedResult<RelFundoTipoAtivoDto>> {
  const qs = buildPaginatedParams(params);
  const res = await apiFetch(`/api/fundos/${fundoId}/tipos-ativos?${qs}`);
  if (!res.ok) throw res;
  return res.json() as Promise<PaginatedResult<RelFundoTipoAtivoDto>>;
}

export async function createFundoTipoAtivo(
  fundoId: string,
  data: { tipoAtivoId: string; limitePercentual?: number | null; limiteValor?: number | null; dataInicio: string; dataFim?: string | null }
): Promise<RelFundoTipoAtivoDto> {
  const res = await apiFetch(`/api/fundos/${fundoId}/tipos-ativos`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<RelFundoTipoAtivoDto>;
}

export async function updateFundoTipoAtivoLimits(
  fundoId: string,
  id: string,
  data: UpdateAssociationLimitsData
): Promise<RelFundoTipoAtivoDto> {
  const res = await apiFetch(`/api/fundos/${fundoId}/tipos-ativos/${id}/limits`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<RelFundoTipoAtivoDto>;
}

export async function transitionFundoTipoAtivoStatus(
  fundoId: string,
  id: string,
  newStatus: RelationshipStatus
): Promise<RelFundoTipoAtivoDto> {
  const res = await apiFetch(`/api/fundos/${fundoId}/tipos-ativos/${id}/status`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ newStatus }),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<RelFundoTipoAtivoDto>;
}

export async function getFundoTipoAtivoAllowedTransitions(
  fundoId: string,
  id: string
): Promise<RelationshipStatus[]> {
  const res = await apiFetch(
    `/api/fundos/${fundoId}/tipos-ativos/${id}/allowed-transitions`
  );
  if (!res.ok) throw res;
  return res.json() as Promise<RelationshipStatus[]>;
}

// ---------------------------------------------------------------------------
// CedenteTipoAtivo association API
// ---------------------------------------------------------------------------

export async function listCedenteTiposAtivo(
  cedenteId: string,
  params: PaginatedSearch
): Promise<PaginatedResult<RelCedenteTipoAtivoDto>> {
  const qs = buildPaginatedParams(params);
  const res = await apiFetch(`/api/cedentes/${cedenteId}/tipos-ativos?${qs}`);
  if (!res.ok) throw res;
  return res.json() as Promise<PaginatedResult<RelCedenteTipoAtivoDto>>;
}

export async function createCedenteTipoAtivo(
  cedenteId: string,
  data: { tipoAtivoId: string; limitePercentual?: number | null; limiteValor?: number | null; dataInicio: string; dataFim?: string | null }
): Promise<RelCedenteTipoAtivoDto> {
  const res = await apiFetch(`/api/cedentes/${cedenteId}/tipos-ativos`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<RelCedenteTipoAtivoDto>;
}

export async function updateCedenteTipoAtivoLimits(
  cedenteId: string,
  id: string,
  data: UpdateAssociationLimitsData
): Promise<RelCedenteTipoAtivoDto> {
  const res = await apiFetch(`/api/cedentes/${cedenteId}/tipos-ativos/${id}/limits`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<RelCedenteTipoAtivoDto>;
}

export async function transitionCedenteTipoAtivoStatus(
  cedenteId: string,
  id: string,
  newStatus: RelationshipStatus
): Promise<RelCedenteTipoAtivoDto> {
  const res = await apiFetch(`/api/cedentes/${cedenteId}/tipos-ativos/${id}/status`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ newStatus }),
  });
  if (!res.ok) throw res;
  return res.json() as Promise<RelCedenteTipoAtivoDto>;
}

export async function getCedenteTipoAtivoAllowedTransitions(
  cedenteId: string,
  id: string
): Promise<RelationshipStatus[]> {
  const res = await apiFetch(
    `/api/cedentes/${cedenteId}/tipos-ativos/${id}/allowed-transitions`
  );
  if (!res.ok) throw res;
  return res.json() as Promise<RelationshipStatus[]>;
}
