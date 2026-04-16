// ---------------------------------------------------------------------------
// Admin HTTP Interceptor
// ---------------------------------------------------------------------------
// Wrapper for admin API calls that automatically handles 401 responses
// by attempting session restoration and retrying the original request.
// Always includes credentials: 'include' for httpOnly cookie support.
// ---------------------------------------------------------------------------

/**
 * Performs a fetch request with admin-specific error handling.
 * On 401: attempts to restore session via /api/admin/auth/me, then retries.
 * On persistent 401: redirects to /admin/login.
 *
 * @param url - The URL to fetch
 * @param options - Optional RequestInit (credentials: 'include' is always added)
 * @returns The Response object
 */
export async function adminFetch(
  url: string,
  options: RequestInit = {}
): Promise<Response> {
  const fetchOptions: RequestInit = {
    ...options,
    credentials: "include",
  };

  const response = await fetch(url, fetchOptions);

  if (response.status === 401) {
    // Session expired — redirect to auth login
    window.location.href = "/auth/login";
    throw new Error("Session expired");
  }

  return response;
}
