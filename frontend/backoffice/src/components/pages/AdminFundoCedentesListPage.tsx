// ---------------------------------------------------------------------------
// AdminFundoCedentesListPage — cross-company FundoCedente associations (T-6)
// ---------------------------------------------------------------------------

import { useQuery } from "@tanstack/react-query";
import { useSearch, useNavigate } from "@tanstack/react-router";
import { listAdminFundoCedentes } from "@/lib/admin-fundos-api";
import { adminListSearchSchema } from "@/lib/admin-fundos-schemas";
import { DEFAULT_PAGE_SIZE } from "@/lib/use-admin-list-search";
import { EmpresaFilterDropdown } from "@/components/molecules/EmpresaFilterDropdown";
import { AdminPaginator } from "@/components/molecules/AdminPaginator";
import { AdminAssociationTable } from "@/components/molecules/AdminAssociationTable";
import type { AssociationRow } from "@/components/molecules/AdminAssociationTable";
import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";
import { Loader2 } from "lucide-react";

export function AdminFundoCedentesListPage() {
  const search = useSearch({ from: "/admin/fundo-cedentes" as any });
  const navigate = useNavigate();
  const { page = 1, empresaId } = adminListSearchSchema.parse(search);

  const { data, isLoading, isFetching, isError } = useQuery({
    queryKey: ["admin-fundo-cedentes", { page, empresaId }],
    queryFn: () =>
      listAdminFundoCedentes({ page, pageSize: DEFAULT_PAGE_SIZE, companyId: empresaId }),
  });

  function nav(next: Partial<{ page: number; empresaId: string | undefined }>) {
    (navigate as any)({
      to: "/admin/fundo-cedentes",
      search: { page, empresaId, ...next },
    });
  }

  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  const rows: AssociationRow[] = items.map((rel) => ({
    id: rel.id,
    empresaNome: rel.empresaNome,
    col1Label: L.colFundo,
    col1Value: rel.fundoNome,
    col2Label: L.colCedente,
    col2Value: rel.cedenteId,
    status: rel.status,
    dataInicio: rel.dataInicio,
    dataFim: rel.dataFim,
    limitePercentual: rel.limitePercentual,
    limiteValor: rel.limiteValor,
  }));

  return (
    <section aria-labelledby="fundo-cedentes-heading">
      <div className="flex items-center justify-between mb-4 flex-wrap gap-2">
        <h1 id="fundo-cedentes-heading" className="text-xl font-semibold">
          {L.pageFundoCedentesTitle}
        </h1>
        <EmpresaFilterDropdown
          value={empresaId}
          onChange={(id) => nav({ empresaId: id, page: 1 })}
          data-testid="fundo-cedentes-empresa-filter"
        />
      </div>

      {isLoading && (
        <div className="flex items-center justify-center py-16">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" aria-hidden="true" />
          <span className="sr-only">{L.loadingData}</span>
        </div>
      )}

      {isError && !isLoading && (
        <p className="text-destructive text-sm py-4" data-testid="fundo-cedentes-error">
          {L.errorLoadingData}
        </p>
      )}

      {!isLoading && !isError && (
        <>
          <div className="relative" data-testid="fundo-cedentes-table-container">
            {isFetching && !isLoading && (
              <div className="absolute inset-0 bg-background/50 flex items-center justify-center z-10">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" aria-hidden="true" />
              </div>
            )}
            <AdminAssociationTable
              rows={rows}
              col1Header={L.colFundo}
              col2Header={L.colCedente}
            />
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
