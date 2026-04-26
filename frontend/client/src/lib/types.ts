// ---------------------------------------------------------------------------
// Shared TypeScript types
// ---------------------------------------------------------------------------
// Mirrors backend DTOs — keep in sync with Onboarding.API models.
// PJ-only: ClientProfileDto replaced with CompanyProfileDto (Phase 40).
// ---------------------------------------------------------------------------

/**
 * Mirrors backend CompanyProfileDto record.
 * Returned by GET /api/companies/me.
 */
export interface CompanyProfileDto {
  id: string;
  razaoSocial: string;
  cnpj: string;
  email: string;
  phone: string;
}

/**
 * Mirrors backend EmployeeListItemDto record.
 * Returned by GET /api/companies/{companyId}/employees.
 */
export interface EmployeeDto {
  id: string;
  nome: string;
  cpf: string | null;
  email: string;
  phone: string;
  accessGroupName: string; // "admin-empresa" | "viewer" | "dashboard"
  isDeleted: boolean;
  keycloakEnabled: boolean;
}

/**
 * Paginated result for employee listing.
 * Returned by GET /api/companies/{companyId}/employees.
 */
export interface PaginatedEmployeesResult {
  items: EmployeeDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}