// ---------------------------------------------------------------------------
// Client API functions
// ---------------------------------------------------------------------------
// Uses httpOnly cookies managed by Vinxi auth-server (/auth/* routes).
// All requests MUST include credentials: 'include'.
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// Auto-refresh: when an API call returns 401, try to refresh the access token
// via /auth/refresh. If refresh succeeds, retry the original request once.
// If refresh also fails, redirect to /auth/login.
// ---------------------------------------------------------------------------

let isRefreshing = false;
let refreshPromise: Promise<boolean> | null = null;

async function refreshAccessToken(): Promise<boolean> {
  if (isRefreshing && refreshPromise) {
    return refreshPromise;
  }

  isRefreshing = true;
  refreshPromise = (async () => {
    try {
      const res = await fetch("/auth/refresh", {
        method: "POST",
        credentials: "include",
      });
      return res.ok;
    } catch {
      return false;
    } finally {
      isRefreshing = false;
    }
  })();

  return refreshPromise;
}

async function apiFetch(url: string, init: RequestInit = {}): Promise<Response> {
  const response = await fetch(url, { credentials: "include", ...init });

  if (response.status !== 401) {
    return response;
  }

  const refreshed = await refreshAccessToken();

  if (!refreshed) {
    window.location.href = "/auth/login";
    return response;
  }

  const retry = await fetch(url, { credentials: "include", ...init });

  if (retry.status === 401) {
    window.location.href = "/auth/login";
  }

  return retry;
}

// ---------------------------------------------------------------------------
// Registration API client
// ---------------------------------------------------------------------------

export interface RegisterCompanyRequest {
  razaoSocial: string;
  cnpj: string;
  email: string;
  phone: string;
  password: string;
  termsAccepted: boolean;
  termsVersion: string;
}

export class RegistrationValidationError extends Error {
  constructor(public errors: Record<string, string[]>) {
    super("Registration validation failed");
    this.name = "RegistrationValidationError";
  }
}

export class DuplicateClientError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "DuplicateClientError";
  }
}

export class RegistrationUnavailable extends Error {
  constructor(message: string) {
    super(message);
    this.name = "RegistrationUnavailable";
  }
}

export class ApiError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "ApiError";
  }
}

export async function registerCompany(
  data: RegisterCompanyRequest
): Promise<{ id: string }> {
  const response = await fetch("/api/companies/registration", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    body: JSON.stringify(data),
  });

  if (response.status === 201) {
    const json = await response.json();
    return { id: json.id as string };
  }

  if (response.status === 422) {
    const problemDetails = (await response.json()) as {
      errors?: Record<string, string[]>;
    };
    throw new RegistrationValidationError(problemDetails.errors ?? {});
  }

  if (response.status === 409) {
    throw new DuplicateClientError(
      "A client with the provided information already exists."
    );
  }

  if (response.status === 503) {
    throw new RegistrationUnavailable(
      "Please try again in a few moments."
    );
  }

  throw new ApiError("An unexpected error occurred.");
}

// ---------------------------------------------------------------------------
// Profile API client
// ---------------------------------------------------------------------------

import type { CompanyProfileDto, EmployeeDto, PaginatedEmployeesResult } from "@/lib/types";

export class ProfileError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "ProfileError";
  }
}

/**
 * Fetch current company's profile using httpOnly cookie.
 * No Bearer token needed — auth is via cookie (ACF).
 * Endpoint: GET /api/companies/me
 */
export async function getProfileClient(): Promise<CompanyProfileDto> {
  const response = await apiFetch("/api/companies/me", {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
  });

  if (!response.ok) {
    if (response.status === 401) {
      throw new ProfileError("Authentication required");
    }
    throw new ProfileError("Failed to fetch profile data");
  }

  return response.json() as Promise<CompanyProfileDto>;
}

// ---------------------------------------------------------------------------
// Employee Management API client (Phase 40)
// ---------------------------------------------------------------------------
// Uses companyId from auth context — all requests scoped to user's company.
// credentials: 'include' required for httpOnly cookie auth (ACF).
// ---------------------------------------------------------------------------

export class EmployeeApiError extends Error {
  public status?: number;
  constructor(message: string, status?: number) {
    super(message);
    this.name = "EmployeeApiError";
    this.status = status;
  }
}

export interface RegisterEmployeeResult {
  employeeId: string;
  temporaryPassword: string;
}

export async function registerEmployee(
  companyId: string,
  data: { nome: string; cpf: string; email: string; phone: string; accessGroupId?: string }
): Promise<RegisterEmployeeResult> {
  const response = await apiFetch(
    `/api/companies/${companyId}/employees`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    }
  );

  if (response.status === 422) {
    const problemDetails = (await response.json()) as { errors?: Record<string, string[]> };
    throw new RegistrationValidationError(problemDetails.errors ?? {});
  }

  if (response.status === 409) {
    throw new DuplicateClientError("A user with this email already exists.");
  }

  if (response.status === 400) {
    const body = await response.json().catch(() => ({}));
    throw new EmployeeApiError(body.detail || "Invalid data.", 400);
  }

  if (!response.ok) {
    throw new EmployeeApiError("Failed to register employee.", response.status);
  }

  return response.json() as Promise<RegisterEmployeeResult>;
}

/**
 * List employees for the current company with pagination and optional filters.
 * GET /api/companies/{companyId}/employees
 */
export async function getEmployees(
  companyId: string,
  params: { page?: number; pageSize?: number; search?: string; status?: string } = {}
): Promise<PaginatedEmployeesResult> {
  const searchParams = new URLSearchParams();
  if (params.page) searchParams.set("page", String(params.page));
  if (params.pageSize) searchParams.set("pageSize", String(params.pageSize));
  if (params.search) searchParams.set("search", params.search);
  if (params.status) searchParams.set("status", params.status);

  const queryString = searchParams.toString();
  const url = queryString
    ? `/api/companies/${companyId}/employees?${queryString}`
    : `/api/companies/${companyId}/employees`;

  const response = await apiFetch(url, {
    method: "GET",
    headers: { "Content-Type": "application/json" },
  });

  if (!response.ok) {
    throw new EmployeeApiError("Failed to fetch employees.", response.status);
  }

  return response.json() as Promise<PaginatedEmployeesResult>;
}

/**
 * Toggle employee active/blocked status.
 * POST /api/companies/{companyId}/employees/{employeeId}/toggle-status
 */
export async function toggleEmployeeStatus(
  companyId: string,
  employeeId: string,
  activate: boolean
): Promise<void> {
  const response = await apiFetch(
    `/api/companies/${companyId}/employees/${employeeId}/toggle-status`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ activate }),
    }
  );

  if (response.status === 404) {
    throw new EmployeeApiError("Employee not found.", 404);
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new EmployeeApiError(body.detail || "Failed to toggle employee status.", response.status);
  }
}

/**
 * Reset employee password — returns a temporary password shown once.
 * POST /api/companies/{companyId}/employees/{employeeId}/reset-password
 */
export async function resetEmployeePassword(
  companyId: string,
  employeeId: string
): Promise<{ temporaryPassword: string }> {
  const response = await apiFetch(
    `/api/companies/${companyId}/employees/${employeeId}/reset-password`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
    }
  );

  if (response.status === 404) {
    throw new EmployeeApiError("Employee not found.", 404);
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new EmployeeApiError(body.detail || "Failed to reset employee password.", response.status);
  }

  return response.json() as Promise<{ temporaryPassword: string }>;
}

/**
 * Update employee data (nome, email, phone).
 * PUT /api/companies/{companyId}/employees/{employeeId}
 */
export async function updateEmployee(
  companyId: string,
  employeeId: string,
  data: { nome: string; email: string; phone: string }
): Promise<EmployeeDto> {
  const response = await apiFetch(
    `/api/companies/${companyId}/employees/${employeeId}`,
    {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    }
  );

  if (response.status === 404) {
    throw new EmployeeApiError("Employee not found.", 404);
  }

  if (response.status === 400) {
    const body = await response.json().catch(() => ({}));
    throw new EmployeeApiError(body.detail || "Invalid data.", 400);
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new EmployeeApiError(body.detail || "Failed to update employee.", response.status);
  }

  return response.json() as Promise<EmployeeDto>;
}

/**
 * Delete employee (LGPD — hard delete).
 * DELETE /api/companies/{companyId}/employees/{employeeId}
 */
export async function deleteEmployee(
  companyId: string,
  employeeId: string
): Promise<void> {
  const response = await apiFetch(
    `/api/companies/${companyId}/employees/${employeeId}`,
    {
      method: "DELETE",
    }
  );

  if (response.status === 404) {
    throw new EmployeeApiError("Employee not found.", 404);
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new EmployeeApiError(body.detail || "Failed to delete employee.", response.status);
  }
}

/**
 * Change employee's access group.
 * PUT /api/companies/{companyId}/employees/{employeeId}/access-group
 */
export async function changeEmployeeAccessGroup(
  companyId: string,
  employeeId: string,
  accessGroupId: string
): Promise<void> {
  const response = await apiFetch(
    `/api/companies/${companyId}/employees/${employeeId}/access-group`,
    {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ accessGroupId }),
    }
  );

  if (response.status === 404) {
    throw new EmployeeApiError("Employee not found.", 404);
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new EmployeeApiError(body.detail || "Failed to change access group.", response.status);
  }
}

export interface AccessGroupDto {
  id: string;
  name: string;
  permissions: string[];
  isDefault: boolean;
}

export async function getAccessGroups(
  companyId: string
): Promise<AccessGroupDto[]> {
  const response = await apiFetch(
    `/api/companies/${companyId}/access-groups`,
    {
      method: "GET",
      headers: { "Content-Type": "application/json" },
    }
  );

  if (!response.ok) {
    throw new EmployeeApiError("Failed to fetch access groups.", response.status);
  }

  return response.json() as Promise<AccessGroupDto[]>;
}

export async function createAccessGroup(
  companyId: string,
  data: { name: string; permissions: string[] }
): Promise<AccessGroupDto> {
  const response = await apiFetch(
    `/api/companies/${companyId}/access-groups`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    }
  );

  if (response.status === 400) {
    const body = await response.json().catch(() => ({}));
    throw new EmployeeApiError(body.detail || "Invalid data.", 400);
  }

  if (!response.ok) {
    throw new EmployeeApiError("Failed to create access group.", response.status);
  }

  return response.json() as Promise<AccessGroupDto>;
}

export async function updateAccessGroup(
  companyId: string,
  accessGroupId: string,
  data: { name?: string; permissions?: string[] }
): Promise<AccessGroupDto> {
  const response = await apiFetch(
    `/api/companies/${companyId}/access-groups/${accessGroupId}`,
    {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    }
  );

  if (response.status === 400) {
    const body = await response.json().catch(() => ({}));
    throw new EmployeeApiError(body.detail || "Invalid data.", 400);
  }

  if (!response.ok) {
    throw new EmployeeApiError("Failed to update access group.", response.status);
  }

  return response.json() as Promise<AccessGroupDto>;
}

export async function deleteAccessGroup(
  companyId: string,
  accessGroupId: string
): Promise<void> {
  const response = await apiFetch(
    `/api/companies/${companyId}/access-groups/${accessGroupId}`,
    {
      method: "DELETE",
    }
  );

  if (response.status === 400) {
    const body = await response.json().catch(() => ({}));
    throw new EmployeeApiError(body.detail || "Cannot delete access group.", 400);
  }

  if (!response.ok) {
    throw new EmployeeApiError("Failed to delete access group.", response.status);
  }
}

export const PERMISSION_LABELS: Record<string, string> = {
  "employees:read": "Ver funcionários",
  "employees:write": "Gerenciar funcionários",
  "employees:delete": "Excluir funcionários",
  "audit:read": "Ver auditoria",
  "dashboard:access": "Acesso ao dashboard",
  "access-groups:manage": "Gerenciar grupos",
};

export const PERMISSION_OPTIONS = [
  { value: "employees:read", label: "Ver funcionários" },
  { value: "employees:write", label: "Gerenciar funcionários" },
  { value: "employees:delete", label: "Excluir funcionários" },
  { value: "audit:read", label: "Ver auditoria" },
  { value: "dashboard:access", label: "Acesso ao dashboard" },
  { value: "access-groups:manage", label: "Gerenciar grupos" },
];

// ---------------------------------------------------------------------------
// Forgot/Reset Password API clients
// ---------------------------------------------------------------------------

export class ForgotPasswordError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "ForgotPasswordError";
  }
}

export class ResetPasswordError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "ResetPasswordError";
  }
}

export interface ForgotPasswordResponse {
  message: string;
}

export async function forgotPasswordClient(
  email: string
): Promise<ForgotPasswordResponse> {
  const response = await fetch("/api/auth/forgot-password", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email }),
  });

  if (response.status === 429) {
    const error = await response.json() as { detail?: string };
    throw new ForgotPasswordError(error.detail || "Muitas tentativas. Tente novamente mais tarde.");
  }

  if (response.status === 400) {
    const error = await response.json() as { title?: string };
    throw new ForgotPasswordError(error.title || "Email invalido.");
  }

  if (response.status === 200) {
    return (await response.json()) as ForgotPasswordResponse;
  }

  throw new ApiError("An unexpected error occurred.");
}

export async function resetPasswordClient(
  token: string,
  newPassword: string
): Promise<{ message: string }> {
  const response = await fetch("/api/auth/reset-password", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token, newPassword }),
  });

  if (!response.ok) {
    const error = await response.json() as { detail?: string };
    throw new ResetPasswordError(error.detail || "Erro ao redefinir senha.");
  }

  return (await response.json()) as { message: string };
}
