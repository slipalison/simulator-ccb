// ---------------------------------------------------------------------------
// Admin HTTP Interceptor
// ---------------------------------------------------------------------------
// Wrapper for admin API calls that automatically handles 401 responses
// by redirecting to Auth Code Flow login. Always includes credentials: 'include'.
// ---------------------------------------------------------------------------

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
    window.location.href = "/auth/login";
    throw new Error("Session expired");
  }

  return response;
}
