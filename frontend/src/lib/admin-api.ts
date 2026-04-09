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
  constructor(message: string) {
    super(message);
    this.name = "AdminApiError";
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
