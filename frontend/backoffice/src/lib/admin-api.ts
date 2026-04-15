// ---------------------------------------------------------------------------
// Admin Auth API client
// ---------------------------------------------------------------------------
// Typed client for admin login/logout/me endpoints.
// Uses httpOnly cookies — all requests must include credentials: 'include'.
// ---------------------------------------------------------------------------

export interface AdminSessionResponse {
  adminName: string;
  adminEmail: string;
}

export class AdminLoginError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "AdminLoginError";
  }
}

export class AdminApiError extends Error {
  public status?: number;
  constructor(message: string, status?: number) {
    super(message);
    this.name = "AdminApiError";
    this.status = status;
  }
}

// ---------------------------------------------------------------------------
// POST /api/admin/auth/login
// ---------------------------------------------------------------------------

export async function loginAdmin(
  email: string,
  password: string
): Promise<AdminSessionResponse> {
  const response = await fetch("/api/admin/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
    credentials: "include",
  });

  if (response.ok) {
    return response.json() as Promise<AdminSessionResponse>;
  }

  if (response.status === 401) {
    throw new AdminLoginError("Credenciais invalidas.");
  }

  const body = await response.json().catch(() => ({}));
  throw new AdminApiError(body.detail || "Login falhou.");
}

// ---------------------------------------------------------------------------
// POST /api/admin/auth/logout
// ---------------------------------------------------------------------------

export async function logoutAdmin(): Promise<void> {
  const response = await fetch("/api/admin/auth/logout", {
    method: "POST",
    credentials: "include",
  });

  if (!response.ok) {
    throw new AdminApiError("Logout falhou.");
  }
}

// ---------------------------------------------------------------------------
// GET /api/admin/auth/me
// ---------------------------------------------------------------------------

export async function getAdminMe(): Promise<AdminSessionResponse> {
  const response = await fetch("/api/admin/auth/me", {
    method: "GET",
    credentials: "include",
  });

  if (response.ok) {
    return response.json() as Promise<AdminSessionResponse>;
  }

  throw new AdminApiError("Session invalid");
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
// Admin User Listing — GET /api/admin/users
// ---------------------------------------------------------------------------

export interface UserSummaryDto {
  id: string;
  name: string;
  email: string;
  document?: string;
  type: "PF" | "PJ";
  enabled: boolean;
  deletedAt?: string;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ListUsersParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
}

export async function listUsers(
  params: ListUsersParams = {}
): Promise<PaginatedResult<UserSummaryDto>> {
  const searchParams = new URLSearchParams();
  if (params.page) searchParams.set("page", String(params.page));
  if (params.pageSize) searchParams.set("pageSize", String(params.pageSize));
  if (params.search) searchParams.set("search", params.search);
  if (params.status) searchParams.set("status", params.status);

  const queryString = searchParams.toString();
  const url = queryString ? `/api/admin/users?${queryString}` : "/api/admin/users";

  const response = await fetch(url, {
    method: "GET",
    credentials: "include",
  });

  if (!response.ok) {
    throw new AdminApiError("Falha ao listar usuarios.");
  }

  return response.json() as Promise<PaginatedResult<UserSummaryDto>>;
}

// ---------------------------------------------------------------------------
// Admin User Detail — GET /api/admin/users/{id}
// ---------------------------------------------------------------------------

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

export async function getUserDetail(userId: string): Promise<UserDetailDto> {
  const response = await fetch(`/api/admin/users/${userId}`, {
    method: "GET",
    credentials: "include",
  });

  if (response.status === 404) {
    throw new AdminApiError("Usuario nao encontrado.", 404);
  }

  if (!response.ok) {
    throw new AdminApiError("Falha ao carregar dados do usuario.");
  }

  return response.json() as Promise<UserDetailDto>;
}

// ---------------------------------------------------------------------------
// Admin User Update — PUT /api/admin/users/{id}
// ---------------------------------------------------------------------------

export interface UpdateUserDto {
  name?: string;
  email?: string;
  phone?: string;
  address?: string;
}

export async function updateUser(
  userId: string,
  data: UpdateUserDto
): Promise<UserDetailDto> {
  const response = await fetch(`/api/admin/users/${userId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
    credentials: "include",
  });

  if (response.status === 404) {
    throw new AdminApiError("Usuario nao encontrado.", 404);
  }

  if (response.status === 400) {
    const body = await response.json().catch(() => ({}));
    throw new AdminApiError(body.detail || "Dados invalidos.", 400);
  }

  if (!response.ok) {
    throw new AdminApiError("Falha ao atualizar usuario.");
  }

  return response.json() as Promise<UserDetailDto>;
}

// ---------------------------------------------------------------------------
// Admin User Block — POST /api/admin/users/{id}/block
// ---------------------------------------------------------------------------

export async function blockUser(
  userId: string,
  reason: string
): Promise<void> {
  const response = await fetch(`/api/admin/users/${userId}/block`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ reason }),
    credentials: "include",
  });

  if (response.status === 404) {
    throw new AdminApiError("Usuario nao encontrado.", 404);
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new AdminApiError(body.detail || "Falha ao bloquear usuario.");
  }
}

// ---------------------------------------------------------------------------
// Admin User Delete — DELETE /api/admin/users/{id}
// ---------------------------------------------------------------------------

export async function deleteUser(
  userId: string
): Promise<void> {
  const response = await fetch(`/api/admin/users/${userId}`, {
    method: "DELETE",
    credentials: "include",
  });

  if (response.status === 404) {
    throw new AdminApiError("Usuario nao encontrado.", 404);
  }

  if (response.status === 409) {
    throw new AdminApiError("Usuario ja foi deletado.", 409);
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new AdminApiError(body.detail || "Falha ao deletar usuario.");
  }
}

// ---------------------------------------------------------------------------
// Admin User Unblock — POST /api/admin/users/{id}/unblock
// ---------------------------------------------------------------------------

export async function unblockUser(
  userId: string,
  reason?: string
): Promise<void> {
  const response = await fetch(`/api/admin/users/${userId}/unblock`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ reason: reason ?? null }),
    credentials: "include",
  });

  if (response.status === 404) {
    throw new AdminApiError("Usuario nao encontrado.", 404);
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new AdminApiError(body.detail || "Falha ao desbloquear usuario.");
  }
}

// ---------------------------------------------------------------------------
// Admin Management — Phase 29 (Milestone v5.0)
// ---------------------------------------------------------------------------

// POST /api/admin/users — Create new admin
export interface CreateAdminResult {
  adminId: string;
  temporaryPassword: string;
}

export async function createAdmin(
  fullName: string,
  email: string
): Promise<CreateAdminResult> {
  const response = await fetch("/api/admin/users", {
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
  const response = await fetch("/api/admin/me/password", {
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

  const response = await fetch(url, {
    method: "GET",
    credentials: "include",
  });

  if (!response.ok) {
    throw new AdminApiError("Falha ao carregar audit log.");
  }

  return response.json() as Promise<PaginatedResult<AuditLogEntry>>;
}
