// ---------------------------------------------------------------------------
// AdminCustodiantesListPage — cross-company Custodiantes list (T-4)
// ---------------------------------------------------------------------------

import { useQuery } from "@tanstack/react-query";
import { useSearch, useNavigate } from "@tanstack/react-router";
import { listAdminCustodiantes } from "@/lib/admin-fundos-api";
import { adminListSearchSchema } from "@/lib/admin-fundos-schemas";
import { DEFAULT_PAGE_SIZE } from "@/lib/use-admin-list-search";
import { EmpresaFilterDropdown } from "@/components/molecules/EmpresaFilterDropdown";
import { AdminPaginator } from "@/components/molecules/AdminPaginator";
import { AdminSearchInput } from "@/components/molecules/AdminSearchInput";
import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";
import { Loader2 } from "lucide-react";

export function AdminCustodiantesListPage() {
  const search = useSearch({ from: "/admin/custodiantes" as any });
  const navigate = useNavigate();
  const { page = 1, search: q = "", empresaId } = adminListSearchSchema.parse(search);

  const { data, isLoading, isFetching, isError } = useQuery({
    queryKey: ["admin-custodiantes", { page, search: q, empresaId }],
    queryFn: () =>
      listAdminCustodiantes({ page, pageSize: DEFAULT_PAGE_SIZE, search: q || undefined, companyId: empresaId }),
  });

  function nav(next: Partial<{ page: number; search: string | undefined; empresaId: string | undefined }>) {
    (navigate as any)({
      to: "/admin/custodiantes",
      search: { page, search: q || undefined, empresaId, ...next },
    });
  }

  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  return (
    <section aria-labelledby="custodiantes-heading">
      <div className="flex items-center justify-between mb-4 flex-wrap gap-2">
        <h1 id="custodiantes-heading" className="text-xl font-semibold">
          {L.pageCustodiantesTitle}
        </h1>
        <div className="flex items-center gap-2 flex-wrap">
          <AdminSearchInput
            value={q}
            onChange={(s) => nav({ search: s || undefined, page: 1 })}
            data-testid="custodiantes-search"
          />
          <EmpresaFilterDropdown
            value={empresaId}
            onChange={(id) => nav({ empresaId: id, page: 1 })}
            data-testid="custodiantes-empresa-filter"
          />
        </div>
      </div>

      {isLoading && (
        <div className="flex items-center justify-center py-16">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" aria-hidden="true" />
          <span className="sr-only">{L.loadingData}</span>
        </div>
      )}

      {isError && !isLoading && (
        <p className="text-destructive text-sm py-4" data-testid="custodiantes-error">
          {L.errorLoadingData}
        </p>
      )}

      {!isLoading && !isError && (
        <>
          <div className="relative overflow-x-auto" data-testid="custodiantes-table-container">
            {isFetching && !isLoading && (
              <div className="absolute inset-0 bg-background/50 flex items-center justify-center z-10">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" aria-hidden="true" />
              </div>
            )}
            <table className="w-full text-sm" data-testid="custodiantes-table">
              <thead>
                <tr className="border-b text-left">
                  <th className="pb-2 pr-4 font-medium text-muted-foreground">{L.detailFieldRazaoSocial}</th>
                  <th className="pb-2 pr-4 font-medium text-muted-foreground">{L.colEmpresa}</th>
                  <th className="pb-2 pr-4 font-medium text-muted-foreground">{L.colCnpj}</th>
                  <th className="pb-2 pr-4 font-medium text-muted-foreground">{L.detailFieldCodigoInterno}</th>
                  <th className="pb-2 font-medium text-muted-foreground">{L.colStatus}</th>
                </tr>
              </thead>
              <tbody>
                {items.length === 0 && !isFetching && (
                  <tr>
                    <td colSpan={5} className="py-8 text-center text-muted-foreground text-sm" data-testid="custodiantes-empty">
                      {L.emptyState}
                    </td>
                  </tr>
                )}
                {items.map((c) => (
                  <tr
                    key={c.id}
                    className="border-b hover:bg-muted/30 transition-colors cursor-pointer"
                    data-testid={`custodiante-row-${c.id}`}
                    onClick={() =>
                      navigate({ to: `/admin/custodiantes/${c.id}` as any })
                    }
                  >
                    <td className="py-3 pr-4 font-medium">{c.razaoSocial}</td>
                    <td className="py-3 pr-4 text-muted-foreground">{c.empresaNome}</td>
                    <td className="py-3 pr-4 text-muted-foreground font-mono text-xs">{c.cnpj}</td>
                    <td className="py-3 pr-4 text-muted-foreground">{c.codigoInterno ?? "—"}</td>
                    <td className="py-3">
                      <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-muted text-muted-foreground">
                        {c.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {totalCount > DEFAULT_PAGE_SIZE && (
            <div className="mt-4">
              <AdminPaginator
                page={page}
                pageSize={DEFAULT_PAGE_SIZE}
                totalCount={totalCount}
                onPageChange={(p) => nav({ page: p })}
              />
            </div>
          )}
        </>
      )}
    </section>
  );
}
