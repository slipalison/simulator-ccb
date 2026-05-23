// ---------------------------------------------------------------------------
// EmpresaFilterDropdown — empresa filter for cross-company admin lists (D-31)
// Consumes admin-companies-api via TanStack Query (staleTime 5min).
// ---------------------------------------------------------------------------

import { useQuery } from "@tanstack/react-query";
import { listCompaniesForFilter } from "@/lib/admin-companies-api";
import { EMPRESA_STALE_TIME } from "@/lib/query-client";
import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";

interface EmpresaFilterDropdownProps {
  value: string | undefined;
  onChange: (empresaId: string | undefined) => void;
  "data-testid"?: string;
}

export function EmpresaFilterDropdown({
  value,
  onChange,
  "data-testid": testId = "empresa-filter",
}: EmpresaFilterDropdownProps) {
  const { data: companies = [], isLoading } = useQuery({
    queryKey: ["admin-companies-filter"],
    queryFn: listCompaniesForFilter,
    staleTime: EMPRESA_STALE_TIME,
  });

  return (
    <select
      id="empresa-filter-select"
      value={value ?? ""}
      onChange={(e) => onChange(e.target.value || undefined)}
      disabled={isLoading}
      data-testid={testId}
      aria-label={L.filterEmpresaPlaceholder}
      className="h-9 w-52 rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm focus:outline-none focus:ring-1 focus:ring-ring disabled:opacity-50"
    >
      <option value="">{L.filterEmpresaTodas}</option>
      {companies.map((c) => (
        <option key={c.id} value={c.id}>
          {c.razaoSocial}
        </option>
      ))}
    </select>
  );
}
