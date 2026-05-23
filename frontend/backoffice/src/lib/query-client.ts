// ---------------------------------------------------------------------------
// TanStack Query client — backoffice independent instance (D-4, D-29)
// No shared config with frontend/client. Independent install per D-4.
// ---------------------------------------------------------------------------

import { QueryClient } from "@tanstack/react-query";

// Separate stale time for empresas (changes rarely)
export const EMPRESA_STALE_TIME = 5 * 60 * 1000; // 5 minutes

// Standard stale time for fundos entities
export const DEFAULT_STALE_TIME = 30_000; // 30 seconds

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: DEFAULT_STALE_TIME,
    },
    mutations: {
      retry: 0,
    },
  },
});
