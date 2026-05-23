// ---------------------------------------------------------------------------
// AdminFundoDetailPage — cross-company Fundo detail (T-5, D-30)
// Backend has no detail-by-id (D-8 scope). Fetches list filtered by name/id
// and matches the item. AuditHistorySection added inline (D-30, T-7).
// ---------------------------------------------------------------------------

import { useQuery } from "@tanstack/react-query";
import { useParams, useNavigate } from "@tanstack/react-router";
import { listAdminFundos } from "@/lib/admin-fundos-api";
import { AdminEntityHeader } from "@/components/molecules/AdminEntityHeader";
import { AdminFieldsGrid } from "@/components/molecules/AdminFieldsGrid";
import { AuditHistorySection } from "@/components/molecules/AuditHistorySection";
import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";
import { Loader2 } from "lucide-react";

export function AdminFundoDetailPage() {
  const { fundoId } = useParams({ from: "/admin/fundos/$fundoId" as any });
  const navigate = useNavigate();

  // Fetch all fundos to find by ID — backend has no GET /by-id endpoint (D-8).
  // Using page 1 with large pageSize; in production, a real GET /:id endpoint should be added.
  const { data, isLoading, isError } = useQuery({
    queryKey: ["admin-fundo-detail", fundoId],
    queryFn: () => listAdminFundos({ page: 1, pageSize: 200 }),
    enabled: !!fundoId,
  });

  const fundo = data?.items.find((f) => f.id === fundoId);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" aria-hidden="true" />
        <span className="sr-only">{L.loadingData}</span>
      </div>
    );
  }

  if (isError) {
    return (
      <p className="text-destructive text-sm py-4" data-testid="fundo-detail-error">
        {L.errorLoadingData}
      </p>
    );
  }

  if (!fundo) {
    return (
      <div className="py-12 text-center" data-testid="fundo-detail-not-found">
        <p className="text-muted-foreground text-sm">{L.notFound}</p>
        <button
          type="button"
          onClick={() => navigate({ to: "/admin/fundos" as any })}
          className="mt-4 text-sm text-primary hover:underline"
        >
          {L.backToList}
        </button>
      </div>
    );
  }

  const fields = [
    { label: L.detailFieldId, value: fundo.id, testId: "fundo-id" },
    { label: L.colCnpj, value: fundo.cnpj, testId: "fundo-cnpj" },
    { label: L.detailFieldEmpresa, value: fundo.empresaNome, testId: "fundo-empresa" },
    { label: L.detailFieldTipo, value: String(fundo.tipoFundo), testId: "fundo-tipo" },
    { label: L.detailFieldClasseAnbima, value: fundo.classeAnbima, testId: "fundo-classe-anbima" },
    { label: L.detailFieldSegmento, value: fundo.segmento, testId: "fundo-segmento" },
    { label: L.detailFieldDataConstituicao, value: fundo.dataConstituicao ? new Date(fundo.dataConstituicao).toLocaleDateString("pt-BR") : null, testId: "fundo-data-constituicao" },
    { label: L.colCreatedAt, value: new Date(fundo.createdAt).toLocaleDateString("pt-BR"), testId: "fundo-created-at" },
  ];

  return (
    <div data-testid="fundo-detail-page">
      <AdminEntityHeader
        title={L.detailFundoTitle}
        nome={fundo.nome}
        empresaNome={fundo.empresaNome}
        status={fundo.status}
        onBack={() => navigate({ to: "/admin/fundos" as any })}
      />
      <div className="bg-card rounded-lg border p-6">
        <AdminFieldsGrid fields={fields} />
      </div>
      <AuditHistorySection entityType="Fundo" entityId={fundoId} />
    </div>
  );
}
