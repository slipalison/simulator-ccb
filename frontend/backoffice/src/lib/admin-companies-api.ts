// ---------------------------------------------------------------------------
// Admin Companies API — company dropdown for EmpresaFilterDropdown (D-31)
// Reuses existing GET /api/admin/companies from admin-api.ts but wraps
// as a standalone typed function for TanStack Query (D-4 independent layer).
// Separate file so admin-fundos-api.ts stays focused on fundos domain.
// ---------------------------------------------------------------------------

import { adminFetch } from "@/lib/admin-http-interceptor";
import { AdminApiError } from "@/lib/admin-api";

export interface CompanyOption {
  id: string;
  razaoSocial: string;
}

// Matches CompanySummaryDto in admin-api.ts but typed minimally for dropdown use.
interface CompanySummaryItem {
  id: string;
  razaoSocial: string;
  email: string;
  phone: string;
  cnpj?: string;
  isDeleted: boolean;
}

interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/**
 * Returns all active companies for the empresa filter dropdown.
 * Fetches page 1 with large pageSize — companies list is small in practice.
 * Cached with staleTime 5min via TanStack Query (empresas change rarely).
 */
export async function listCompaniesForFilter(): Promise<CompanyOption[]> {
  const response = await adminFetch("/api/admin/companies?pageSize=200&page=1", {
    method: "GET",
  });

  if (!response.ok) {
    throw new AdminApiError("Falha ao carregar empresas.", response.status);
  }

  const data = (await response.json()) as PaginatedResult<CompanySummaryItem>;
  return data.items
    .filter((c) => !c.isDeleted)
    .map((c) => ({ id: c.id, razaoSocial: c.razaoSocial }));
}
