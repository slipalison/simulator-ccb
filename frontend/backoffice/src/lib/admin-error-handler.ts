// ---------------------------------------------------------------------------
// Admin Error Handler Middleware
// ---------------------------------------------------------------------------
// Centralized error handling for admin API calls.
// Handles 401 (session expired), 403 (access denied), and 5xx (server error).
// ---------------------------------------------------------------------------

import { toast } from "sonner";

// ---------------------------------------------------------------------------
// Custom error classes
// ---------------------------------------------------------------------------

export class SessionExpiredError extends Error {
  constructor() {
    super("Session expired");
    this.name = "SessionExpiredError";
  }
}

export class AccessDeniedError extends Error {
  constructor() {
    super("Access denied");
    this.name = "AccessDeniedError";
  }
}

// ---------------------------------------------------------------------------
// Response status checker for admin API calls
// ---------------------------------------------------------------------------

/**
 * Checks a Response and handles error status codes:
 * - 401: shows toast "Sessao expirada" and redirects to /admin/login
 * - 403: redirects to /admin/access-denied
 * - 5xx: shows toast "Erro interno do servidor"
 *
 * @param response - The fetch Response to check
 * @returns true if the response is OK (should proceed to parse JSON)
 * @throws SessionExpiredError | AccessDeniedError | Error
 */
export function checkAdminResponse(response: Response): boolean {
  if (response.status === 401) {
    toast.error("Sessao expirada", {
      description: "Faca login novamente.",
    });
    window.location.href = "/admin/login?expired=true";
    throw new SessionExpiredError();
  }

  if (response.status === 403) {
    window.location.href = "/admin/access-denied";
    throw new AccessDeniedError();
  }

  if (response.status >= 500) {
    toast.error("Erro interno", {
      description: "Erro interno do servidor, tente novamente mais tarde.",
    });
    throw new Error("Server error");
  }

  return true;
}

/**
 * Wraps an admin API call with automatic error handling.
 *
 * Usage:
 *   const data = await handleAdminApiCall<UsersResponse>(
 *     () => fetch("/api/admin/users", { credentials: "include" })
 *   );
 *
 * @param apiCall - Function that returns a Promise<Response>
 * @returns Parsed JSON response of type T
 */
export async function handleAdminApiCall<T>(
  apiCall: () => Promise<Response>
): Promise<T> {
  const response = await apiCall();
  checkAdminResponse(response);
  return response.json() as Promise<T>;
}
