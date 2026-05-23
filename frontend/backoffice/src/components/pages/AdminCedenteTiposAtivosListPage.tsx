// ---------------------------------------------------------------------------
// AdminCedenteTiposAtivosListPage — cross-company CedenteTipoAtivo associations (T-6)
// ---------------------------------------------------------------------------

import { useQuery } from "@tanstack/react-query";
import { useSearch, useNavigate } from "@tanstack/react-router";
import { listAdminCedenteTiposAtivos } from "@/lib/admin-fundos-api";
import { adminListSearchSchema } from "@/lib/admin-fundos-schemas";
import { DEFAULT_PAGE_SIZE } from "@/lib/use-admin-list-search";
import { EmpresaFilterDropdown } from "@/components/molecules/EmpresaFilterDropdown";
import { AdminPaginator } from "@/components/molecules/AdminPaginator";
import { AdminAssociationTable } from "@/components/molecules/AdminAssociationTable";
import type { AssociationRow } from "@/components/molecules/AdminAssociationTable";
import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";
import { Loader2 } from "lucide-react";

export function AdminCedenteTiposAtivosListPage() {
  const search = useSearch({ from: "/admin/cedente-tipos-ativos" as any });
  const navigate = useNavigate();
  const { page = 1, empresaId } = adminListSearchSchema.parse(search);

  const { data, isLoading, isFetching, isError } = useQuery({
    queryKey: ["admin-cedente-tipos-ativos", { page, empresaId }],
    queryFn: () =>
      listAdminCedenteTiposAtivos({ page, pageSize: DEFAULT_PAGE_SIZE, companyId: empresaId }),
  });

  function nav(next: Partial<{ page: number; empresaId: string | undefined }>) {
    (navigate as any)({
      to: "/admin/cedente-tipos-ativos",
      search: { page, empresaId, ...next },
    });
  }

  const items = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;

  const rows: AssociationRow[] = items.map((rel) => ({
    id: rel.id,
    empresaNome: rel.empresaNome,
    col1Label: L.colCedente,
    col1Value: rel.cedenteId,
    col2Label: L.colTipoAtivo,
    col2Value: rel.tipoAtivoId,
    status: rel.status,
    dataInicio: rel.dataInicio,
    dataFim: rel.dataFim,
    limitePercentual: rel.limitePercentual,
    limiteValor: rel.limiteValor,
  }));

  return (
    <section aria-labelledby="cedente-tipos-ativos-heading">
      <div className="flex items-center justify-between mb-4 flex-wrap gap-2">
        <h1 id="cedente-tipos-ativos-heading" className="text-xl font-semibold">
          {L.pageCedenteTiposAtivosTitle}
        </h1>
        <EmpresaFilterDropdown
          value={empresaId}
          onChange={(id) => nav({ empresaId: id, page: 1 })}
          data-testid="cedente-tipos-ativos-empresa-filter"
        />
      </div>

      {isLoading && (
        <div className="flex items-center justify-center py-16">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" aria-hidden="true" />
          <span className="sr-only">{L.loadingData}</span>
        </div>
      )}

      {isError && !isLoading && (
        <p className="text-destructive text-sm py-4" data-testid="cedente-tipos-ativos-error">
          {L.errorLoadingData}
        </p>
      )}

      {!isLoading && !isError && (
        <>
          <div className="relative" data-testid="cedente-tipos-ativos-table-container">
            {isFetching && !isLoading && (
              <div className="absolute inset-0 bg-background/50 flex items-center justify-center z-10">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" aria-hidden="true" />
              </div>
            )}
            <AdminAssociationTable
              rows={rows}
              col1Header={L.colCedente}
              col2Header={L.colTipoAtivo}
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
