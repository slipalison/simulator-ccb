// ---------------------------------------------------------------------------
// AdminFundosListPage — cross-company Fundos list (T-4, D-28, D-31)
// ---------------------------------------------------------------------------

import { useQuery } from "@tanstack/react-query";
import { useSearch, useNavigate } from "@tanstack/react-router";
import { listAdminFundos } from "@/lib/admin-fundos-api";
import { adminListSearchSchema } from "@/lib/admin-fundos-schemas";
import { DEFAULT_PAGE_SIZE } from "@/lib/use-admin-list-search";
import { EmpresaFilterDropdown } from "@/components/molecules/EmpresaFilterDropdown";
import { AdminPaginator } from "@/components/molecules/AdminPaginator";
import { AdminSearchInput } from "@/components/molecules/AdminSearchInput";
import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";
import { Loader2 } from "lucide-react";

export function AdminFundosListPage() {
  const search = useSearch({ from: "/admin/fundos" as any });
  const navigate = useNavigate();
  const { page = 1, search: q = "", empresaId } = adminListSearchSchema.parse(search);

  const { data, isLoading, isFetching, isError } = useQuery({
    queryKey: ["admin-fundos", { page, search: q, empresaId }],
    queryFn: () =>
      listAdminFundos({ page, pageSize: DEFAULT_PAGE_SIZE, search: q || undefined, companyId: empresaId }),
  });

  function nav(next: Partial<{ page: number; search: string; empresaId: string | undefined }>) {
    (navigate as any)({
      to: "/admin/fundos",
      search: { page, search: q || undefined, empresaId, ...next },
    });
  }

  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  return (
    <section aria-labelledby="fundos-heading">
      <div className="flex items-center justify-between mb-4 flex-wrap gap-2">
        <h1 id="fundos-heading" className="text-xl font-semibold">
          {L.pageFundosTitle}
        </h1>
        <div className="flex items-center gap-2 flex-wrap">
          <AdminSearchInput
            value={q}
            onChange={(s) => nav({ search: s || undefined, page: 1 })}
            data-testid="fundos-search"
          />
          <EmpresaFilterDropdown
            value={empresaId}
            onChange={(id) => nav({ empresaId: id, page: 1 })}
            data-testid="fundos-empresa-filter"
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
        <p className="text-destructive text-sm py-4" data-testid="fundos-error">
          {L.errorLoadingData}
        </p>
      )}

      {!isLoading && !isError && (
        <>
          <div className="relative overflow-x-auto" data-testid="fundos-table-container">
            {isFetching && !isLoading && (
              <div className="absolute inset-0 bg-background/50 flex items-center justify-center z-10">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" aria-hidden="true" />
              </div>
            )}
            <table className="w-full text-sm" data-testid="fundos-table">
              <thead>
                <tr className="border-b text-left">
                  <th className="pb-2 pr-4 font-medium text-muted-foreground">{L.colNome}</th>
                  <th className="pb-2 pr-4 font-medium text-muted-foreground">{L.colEmpresa}</th>
                  <th className="pb-2 pr-4 font-medium text-muted-foreground">{L.colCnpj}</th>
                  <th className="pb-2 pr-4 font-medium text-muted-foreground">{L.colStatus}</th>
                  <th className="pb-2 font-medium text-muted-foreground">{L.colCreatedAt}</th>
                </tr>
              </thead>
              <tbody>
                {items.length === 0 && !isFetching && (
                  <tr>
                    <td colSpan={5} className="py-8 text-center text-muted-foreground text-sm" data-testid="fundos-empty">
                      {L.emptyState}
                    </td>
                  </tr>
                )}
                {items.map((fundo) => (
                  <tr
                    key={fundo.id}
                    className="border-b hover:bg-muted/30 transition-colors cursor-pointer"
                    data-testid={`fundo-row-${fundo.id}`}
                    onClick={() =>
                      navigate({ to: `/admin/fundos/${fundo.id}` as any })
                    }
                  >
                    <td className="py-3 pr-4 font-medium">{fundo.nome}</td>
                    <td className="py-3 pr-4 text-muted-foreground">{fundo.empresaNome}</td>
                    <td className="py-3 pr-4 text-muted-foreground font-mono text-xs">{fundo.cnpj}</td>
                    <td className="py-3 pr-4">
                      <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-muted text-muted-foreground">
                        {fundo.status}
                      </span>
                    </td>
                    <td className="py-3 text-muted-foreground text-xs">
                      {new Date(fundo.createdAt).toLocaleDateString("pt-BR")}
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
