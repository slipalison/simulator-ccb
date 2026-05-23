// ---------------------------------------------------------------------------
// AdminCustodianteDetailPage — cross-company Custodiante detail (T-5, D-30)
// Iter 2: direct GET /api/admin/fundos/custodiantes/{id} — removes pageSize=200 hack (D-8).
// ---------------------------------------------------------------------------

import { useQuery } from "@tanstack/react-query";
import { useParams, useNavigate } from "@tanstack/react-router";
import { getAdminCustodiante } from "@/lib/admin-fundos-api";
import { AdminApiError } from "@/lib/admin-api";
import { AdminEntityHeader } from "@/components/molecules/AdminEntityHeader";
import { AdminFieldsGrid } from "@/components/molecules/AdminFieldsGrid";
import { AuditHistorySection } from "@/components/molecules/AuditHistorySection";
import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";
import { Loader2 } from "lucide-react";

export function AdminCustodianteDetailPage() {
  const { custodianteId } = useParams({ from: "/admin/custodiantes/$custodianteId" as any });
  const navigate = useNavigate();

  const { data: custodiante, isLoading, isError, error } = useQuery({
    queryKey: ["admin-custodiante", custodianteId],
    queryFn: () => getAdminCustodiante(custodianteId),
    enabled: !!custodianteId,
  });

  const is404 = error instanceof AdminApiError && error.status === 404;

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" aria-hidden="true" />
        <span className="sr-only">{L.loadingData}</span>
      </div>
    );
  }

  if (is404) {
    return (
      <div className="py-12 text-center" data-testid="custodiante-detail-not-found">
        <p className="text-muted-foreground text-sm">{L.notFound}</p>
        <button
          type="button"
          onClick={() => navigate({ to: "/admin/custodiantes" as any })}
          className="mt-4 text-sm text-primary hover:underline"
        >
          {L.backToList}
        </button>
      </div>
    );
  }

  if (isError) {
    return (
      <p className="text-destructive text-sm py-4" data-testid="custodiante-detail-error">
        {L.errorLoadingData}
      </p>
    );
  }

  if (!custodiante) return null;

  const fields = [
    { label: L.detailFieldId, value: custodiante.id, testId: "custodiante-id" },
    { label: L.colCnpj, value: custodiante.cnpj, testId: "custodiante-cnpj" },
    { label: L.detailFieldEmpresa, value: custodiante.empresaNome, testId: "custodiante-empresa" },
    { label: L.detailFieldCodigoInterno, value: custodiante.codigoInterno, testId: "custodiante-codigo-interno" },
    { label: L.detailFieldEmail, value: custodiante.email, testId: "custodiante-email" },
    { label: L.detailFieldTelefone, value: custodiante.telefone, testId: "custodiante-telefone" },
    { label: L.colCreatedAt, value: new Date(custodiante.createdAt).toLocaleDateString("pt-BR"), testId: "custodiante-created-at" },
  ];

  return (
    <div data-testid="custodiante-detail-page">
      <AdminEntityHeader
        title={L.detailCustodianteTitle}
        nome={custodiante.razaoSocial}
        empresaNome={custodiante.empresaNome}
        status={custodiante.status}
        onBack={() => navigate({ to: "/admin/custodiantes" as any })}
      />
      <div className="bg-card rounded-lg border p-6">
        <AdminFieldsGrid fields={fields} />
      </div>
      <AuditHistorySection entityType="Custodiante" entityId={custodianteId} />
    </div>
  );
}
