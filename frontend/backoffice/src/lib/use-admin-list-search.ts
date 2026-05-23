// ---------------------------------------------------------------------------
// useAdminListSearch — shared hook for URL search param state management (D-31)
// Manages page/search/empresaId in URL via TanStack Router navigate.
// Used by all 4 entity list pages + 3 association pages.
// ---------------------------------------------------------------------------

import { useNavigate } from "@tanstack/react-router";
import type { AdminListSearch } from "@/lib/admin-fundos-schemas";

export const DEFAULT_PAGE_SIZE = 20;

/**
 * Returns updaters that navigate with updated search params.
 * Pages pass the current search object from their validateSearch Zod.
 */
export function useAdminListSearch(currentSearch: AdminListSearch, path: string) {
  const navigate = useNavigate();

  function setPage(page: number) {
    (navigate as any)({
      to: path,
      search: { ...currentSearch, page },
    });
  }

  function setSearch(search: string) {
    (navigate as any)({
      to: path,
      search: { ...currentSearch, search: search || undefined, page: 1 },
    });
  }

  function setEmpresaId(empresaId: string | undefined) {
    (navigate as any)({
      to: path,
      search: { ...currentSearch, empresaId, page: 1 },
    });
  }

  return { setPage, setSearch, setEmpresaId };
}
