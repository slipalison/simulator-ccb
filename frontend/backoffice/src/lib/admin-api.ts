// ---------------------------------------------------------------------------
// Admin Auth API client
// ---------------------------------------------------------------------------
// Typed client for admin login/logout/me endpoints.
// Uses httpOnly cookies — all requests must include credentials: 'include'.
// ---------------------------------------------------------------------------

export interface AdminSessionResponse {
  adminName: string;
  adminEmail: string;
  adminId: string;
}

export class AdminLoginError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "AdminLoginError";
  }
}

export class AdminApiError extends Error {
  public status?: number;
  public code?: string;
  constructor(message: string, status?: number, code?: string) {
    super(message);
    this.name = "AdminApiError";
    this.status = status;
    this.code = code;
  }
}

// ---------------------------------------------------------------------------
// GET /auth/logout — redirect to Keycloak OIDC logout (clears cookies)
// ---------------------------------------------------------------------------

export function logoutAdmin(): void {
  window.location.href = "/auth/logout";
}

// ---------------------------------------------------------------------------
// Internal Helper: fetchWithAuth
// Intercepts 401 Unauthorized to trigger a login redirect when token expires.
// ---------------------------------------------------------------------------
async function fetchWithAuth(url: string | URL, init?: RequestInit): Promise<Response> {
  const response = await fetch(url.toString(), init);
  if (response.status === 401) {
    if (window.location.pathname !== "/admin/login") {
      window.location.href = "/admin/login";
    }
  }
  return response;
}

// ---------------------------------------------------------------------------
// GET /auth/me — returns session info from httpOnly cookie (Vinxi server)
// ---------------------------------------------------------------------------

export async function getAdminMe(): Promise<AdminSessionResponse> {
  const response = await fetch("/auth/me", {
    method: "GET",
    credentials: "include",
  });

  if (response.ok) {
    const data = (await response.json()) as {
      isAuthenticated: boolean;
      adminName: string;
      email: string;
      sub: string;
    };
    if (!data.isAuthenticated) {
      throw new AdminApiError("Session invalid", 401);
    }
    return { adminName: data.adminName, adminEmail: data.email, adminId: data.sub };
  }

  // Expose HTTP status so callers can distinguish 401 (unauthenticated) from 5xx (server error).
  throw new AdminApiError("Session invalid", response.status);
}

// ---------------------------------------------------------------------------
// Internal: shared request options for admin API calls
// All requests MUST include credentials (httpOnly cookies).
// ---------------------------------------------------------------------------

function _adminFetchOptions(
  method: string,
  body?: string
): RequestInit & { duplex?: string } {
  const hasBody = method !== "GET" && method !== "HEAD";
  return {
    method,
    headers: hasBody
      ? { "Content-Type": "application/json" }
      : undefined,
    body: hasBody ? body : undefined,
    credentials: "include" as RequestCredentials,
  };
}

// ---------------------------------------------------------------------------
// Legacy aliases (backward compat for tests) — redirect /users → /companies + /employees
// ---------------------------------------------------------------------------

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface UserSummaryDto {
  id: string;
  name: string;
  email: string;
  document?: string;
  type: "PF" | "PJ";
  enabled: boolean;
  deletedAt?: string;
}

export interface UserDetailDto {
  id: string;
  name: string;
  email: string;
  phone: string;
  document?: string;
  type: "PF" | "PJ";
  razaoSocial?: string;
  createdAt: string;
  deletedAt?: string;
  keycloakEnabled: boolean;
  keycloakEmailVerified: boolean;
  keycloakUserId?: string;
}

export interface ListUsersParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
}

export interface UpdateUserDto {
  name?: string;
  email?: string;
  phone?: string;
  address?: string;
}

export async function listUsers(params: ListUsersParams = {}): Promise<PaginatedResult<UserSummaryDto>> {
  const r = await listCompanies(params);
  return {
    ...r,
    items: r.items.map(c => ({
      id: c.id,
      name: c.razaoSocial,
      email: c.email,
      document: c.cnpj,
      type: "PJ" as const,
      enabled: !c.isDeleted,
      deletedAt: undefined,
    })),
  };
}

export async function getUserDetail(userId: string): Promise<UserDetailDto> {
  const c = await getCompanyDetails(userId);
  return {
    id: c.id,
    name: c.razaoSocial,
    email: c.email,
    phone: c.phone,
    document: c.cnpj,
    type: "PJ",
    razaoSocial: c.razaoSocial,
    createdAt: "",
    deletedAt: undefined,
    keycloakEnabled: !c.isDeleted,
    keycloakEmailVerified: true,
    keycloakUserId: undefined,
  };
}

export async function updateUser(_userId?: string, _data?: unknown): Promise<UserDetailDto> {
  throw new AdminApiError("Use PUT /api/admin/companies/{id} instead.", 410);
}

export async function blockUser(_userId?: string, _reason?: string): Promise<void> {
  throw new AdminApiError("Use POST /api/admin/employees/{id}/block instead.", 410);
}

export async function unblockUser(_userId?: string, _reason?: string): Promise<void> {
  throw new AdminApiError("Use POST /api/admin/employees/{id}/unblock instead.", 410);
}

export async function deleteUser(_userId?: string): Promise<void> {
  throw new AdminApiError("Use DELETE /api/admin/employees/{id} instead.", 410);
}

// ---------------------------------------------------------------------------
// Admin Companies — GET /api/admin/companies
// ---------------------------------------------------------------------------

export interface CompanySummaryDto {
  id: string;
  razaoSocial: string;
  email: string;
  phone: string;
  cnpj?: string;
  isDeleted: boolean;
}

export interface ListCompaniesParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
}

export async function listCompanies(
  params: ListCompaniesParams = {}
): Promise<PaginatedResult<CompanySummaryDto>> {
  const searchParams = new URLSearchParams();
  if (params.page) searchParams.set("page", String(params.page));
  if (params.pageSize) searchParams.set("pageSize", String(params.pageSize));
  if (params.search) searchParams.set("search", params.search);
  if (params.status) searchParams.set("status", params.status);

  const queryString = searchParams.toString();
  const url = queryString ? `/api/admin/companies?${queryString}` : "/api/admin/companies";

  const response = await fetchWithAuth(url, { method: "GET", credentials: "include" });

  if (!response.ok) {
    throw new AdminApiError("Falha ao listar empresas.");
  }

  return response.json() as Promise<PaginatedResult<CompanySummaryDto>>;
}

export async function getCompanyDetails(companyId: string): Promise<CompanySummaryDto> {
  const response = await fetchWithAuth(`/api/admin/companies/${companyId}`, {
    method: "GET",
    credentials: "include",
  });

  if (response.status === 404) {
    throw new AdminApiError("Empresa nao encontrada.", 404);
  }

  if (!response.ok) {
    throw new AdminApiError("Falha ao carregar dados da empresa.");
  }

  return response.json() as Promise<CompanySummaryDto>;
}

// ---------------------------------------------------------------------------
// Admin Employees — GET /api/admin/employees
// ---------------------------------------------------------------------------

export interface EmployeeSummaryDto {
  id: string;
  nome: string;
  cpf: string;
  email: string;
  phone: string;
  companyId: string;
  companyRazaoSocial?: string;
  accessGroupId: string;
  accessGroupName?: string;
  isDeleted: boolean;
  keycloakUserId?: string;
}

export interface ListEmployeesParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  companyId?: string;
}

export async function listEmployees(
  params: ListEmployeesParams = {}
): Promise<PaginatedResult<EmployeeSummaryDto>> {
  const searchParams = new URLSearchParams();
  if (params.page) searchParams.set("page", String(params.page));
  if (params.pageSize) searchParams.set("pageSize", String(params.pageSize));
  if (params.search) searchParams.set("search", params.search);
  if (params.status) searchParams.set("status", params.status);
  if (params.companyId) searchParams.set("companyId", params.companyId);

  const queryString = searchParams.toString();
  const url = queryString ? `/api/admin/employees?${queryString}` : "/api/admin/employees";

  const response = await fetchWithAuth(url, { method: "GET", credentials: "include" });

  if (!response.ok) {
    throw new AdminApiError("Falha ao listar funcionários.");
  }

  return response.json() as Promise<PaginatedResult<EmployeeSummaryDto>>;
}

export async function getEmployeeDetails(employeeId: string): Promise<EmployeeSummaryDto> {
  const response = await fetchWithAuth(`/api/admin/employees/${employeeId}`, {
    method: "GET",
    credentials: "include",
  });

  if (response.status === 404) {
    throw new AdminApiError("Funcionário nao encontrado.", 404);
  }

  if (!response.ok) {
    throw new AdminApiError("Falha ao carregar dados do funcionário.");
  }

  return response.json() as Promise<EmployeeSummaryDto>;
}

export async function blockEmployee(employeeId: string): Promise<void> {
  const response = await fetchWithAuth(`/api/admin/employees/${employeeId}/block`, {
    method: "POST",
    credentials: "include",
  });

  if (response.status === 404) {
    throw new AdminApiError("Funcionário nao encontrado.", 404);
  }

  if (!response.ok) {
    throw new AdminApiError("Falha ao bloquear funcionário.");
  }
}

export async function unblockEmployee(employeeId: string): Promise<void> {
  const response = await fetchWithAuth(`/api/admin/employees/${employeeId}/unblock`, {
    method: "POST",
    credentials: "include",
  });

  if (response.status === 404) {
    throw new AdminApiError("Funcionário nao encontrado.", 404);
  }

  if (!response.ok) {
    throw new AdminApiError("Falha ao desbloquear funcionário.");
  }
}

export async function deleteEmployee(employeeId: string): Promise<void> {
  const response = await fetchWithAuth(`/api/admin/employees/${employeeId}`, {
    method: "DELETE",
    credentials: "include",
  });

  if (response.status === 404) {
    throw new AdminApiError("Funcionário nao encontrado.", 404);
  }

  if (!response.ok) {
    throw new AdminApiError("Falha ao excluir funcionário.");
  }
}

// ---------------------------------------------------------------------------
// Admin Management — Phase 29 (Milestone v5.0)
// ---------------------------------------------------------------------------

// POST /api/admin/administrators — Create new admin
export interface CreateAdminResult {
  adminId: string;
  temporaryPassword: string;
}

export async function createAdmin(
  fullName: string,
  email: string
): Promise<CreateAdminResult> {
  const response = await fetchWithAuth("/api/admin/administrators", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ fullName, email }),
    credentials: "include",
  });

  if (response.status === 409) {
    throw new AdminApiError("Email ja cadastrado.", 409);
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new AdminApiError(body.detail || "Falha ao criar admin.");
  }

  return response.json() as Promise<CreateAdminResult>;
}

// PUT /api/admin/me/password — Force password change
export async function forcePasswordChange(
  newPassword: string
): Promise<void> {
  const response = await fetchWithAuth("/api/admin/me/password", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ newPassword }),
    credentials: "include",
  });

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new AdminApiError(body.detail || "Falha ao alterar senha.");
  }
}

// GET /api/admin/audit-log — Paginated audit log with filters
export interface AuditLogEntry {
  id: string;
  timestamp: string;
  adminUserId: string;
  adminUserName: string;
  actionType: string;
  targetUserId: string | null;
  targetUserName: string | null;
  details: string | null;
  ipAddress: string | null;
}

export interface GetAuditLogParams {
  page?: number;
  pageSize?: number;
  startDate?: string;
  endDate?: string;
  actionType?: string;
  adminUserName?: string;
}

export async function getAuditLog(
  params: GetAuditLogParams = {}
): Promise<PaginatedResult<AuditLogEntry>> {
  const searchParams = new URLSearchParams();
  if (params.page) searchParams.set("page", String(params.page));
  if (params.pageSize) searchParams.set("pageSize", String(params.pageSize));
  if (params.startDate) searchParams.set("startDate", params.startDate);
  if (params.endDate) searchParams.set("endDate", params.endDate);
  if (params.actionType) searchParams.set("actionType", params.actionType);
  if (params.adminUserName) searchParams.set("adminUserName", params.adminUserName);

  const queryString = searchParams.toString();
  const url = queryString ? `/api/admin/audit-log?${queryString}` : "/api/admin/audit-log";

  const response = await fetchWithAuth(url, {
    method: "GET",
    credentials: "include",
  });

  if (!response.ok) {
    throw new AdminApiError("Falha ao carregar audit log.");
  }

  return response.json() as Promise<PaginatedResult<AuditLogEntry>>;
}

// ---------------------------------------------------------------------------
// Admin Administrators — Phase 30 (Milestone v5.0) — ADM-04
// ---------------------------------------------------------------------------

// GET /api/admin/administrators — Lista todos os administradores
export interface AdminUserDto {
  id: string;
  email: string;
  fullName: string;
  isEnabled: boolean;
  hasTemporaryPassword: boolean;
}

export async function getAdministrators(): Promise<AdminUserDto[]> {
  const response = await fetchWithAuth("/api/admin/administrators", {
    method: "GET",
    credentials: "include",
  });
  if (!response.ok) {
    throw new AdminApiError("Falha ao carregar administradores.");
  }
  return response.json() as Promise<AdminUserDto[]>;
}

// ---------------------------------------------------------------------------
// Admin Administrators — Phase 36 (Milestone v6.0) — MGMT-01..06
// ---------------------------------------------------------------------------

export interface GetAdministratorsPaginatedParams {
  page?: number;
  pageSize?: number;
  name?: string;
  email?: string;
  status?: string;
}

export interface ResetAdministratorPasswordResult {
  temporaryPassword: string;
}

export async function getAdministratorsPaginated(
  params: GetAdministratorsPaginatedParams = {}
): Promise<PaginatedResult<AdminUserDto>> {
  const searchParams = new URLSearchParams();
  if (params.page) searchParams.set("page", String(params.page));
  if (params.pageSize) searchParams.set("pageSize", String(params.pageSize));
  if (params.name) searchParams.set("name", params.name);
  if (params.email) searchParams.set("email", params.email);
  if (params.status) searchParams.set("status", params.status);

  const queryString = searchParams.toString();
  const url = queryString
    ? `/api/admin/administrators/paginated?${queryString}`
    : "/api/admin/administrators/paginated";

  const response = await fetchWithAuth(url, {
    method: "GET",
    credentials: "include",
  });

  if (!response.ok) {
    throw new AdminApiError("Falha ao carregar administradores.", response.status);
  }

  return response.json() as Promise<PaginatedResult<AdminUserDto>>;
}

export async function updateAdministrator(
  adminId: string,
  data: { fullName: string; email: string }
): Promise<void> {
  const response = await fetchWithAuth(`/api/admin/administrators/${adminId}`, {
    ..._adminFetchOptions("PUT", JSON.stringify({ fullName: data.fullName, email: data.email })),
  });

  if (response.status === 400) {
    const body = await response.json().catch(() => ({}));
    throw new AdminApiError(body.detail || "Operação não permitida.", 400);
  }

  if (response.status === 409) {
    throw new AdminApiError("Email já está em uso.", 409);
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new AdminApiError(body.detail || "Falha ao atualizar administrador.", response.status);
  }
}

export async function resetAdministratorPassword(
  adminId: string,
  targetUserName: string
): Promise<ResetAdministratorPasswordResult> {
  const response = await fetchWithAuth(`/api/admin/administrators/${adminId}/reset-password`, {
    ..._adminFetchOptions("POST", JSON.stringify({ targetUserName })),
  });

  if (response.status === 400) {
    const body = await response.json().catch(() => ({}));
    throw new AdminApiError(body.detail || "Operação não permitida.", 400);
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new AdminApiError(body.detail || "Falha ao resetar senha.", response.status);
  }

  return response.json() as Promise<ResetAdministratorPasswordResult>;
}

export async function toggleAdministratorStatus(
  adminId: string,
  targetUserName: string,
  activate: boolean,
  reason?: string
): Promise<void> {
  const response = await fetchWithAuth(`/api/admin/administrators/${adminId}/toggle-status`, {
    ..._adminFetchOptions(
      "POST",
      JSON.stringify({ activate, targetUserName, reason: reason ?? null })
    ),
  });

  if (response.status === 400 || response.status === 409) {
    const body = await response.json().catch(() => ({}));
    const detail = body.detail || "";
    const code = body.type || "";
    let message = body.detail || "Operação não permitida.";
    if (code.includes("LAST_ADMIN") || detail.toLowerCase().includes("último administrador") || detail.toLowerCase().includes("last admin")) {
      message = "Não é possível desativar o último administrador ativo.";
    } else if (code.includes("SELF_ACTION") || detail.toLowerCase().includes("própria conta") || detail.toLowerCase().includes("self")) {
      message = "Operação não permitida na própria conta.";
    }
    throw new AdminApiError(message, response.status, code);
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new AdminApiError(body.detail || "Falha ao alterar status.", response.status);
  }
}
