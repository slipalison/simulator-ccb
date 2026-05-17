// ---------------------------------------------------------------------------
// TanStack Query client configuration (D-24)
// ---------------------------------------------------------------------------
// - retry: 1 (404/401 already handled by apiFetch auto-refresh)
// - staleTime: 30_000 ms default
// - mutations have no retry (pessimistic by default)
// - No cache persistence (memory only — D-12 token safety)
// ---------------------------------------------------------------------------

import { QueryClient } from "@tanstack/react-query";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: 0,
    },
  },
});
