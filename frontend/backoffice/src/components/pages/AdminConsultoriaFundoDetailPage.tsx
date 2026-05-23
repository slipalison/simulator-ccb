// ---------------------------------------------------------------------------
// AdminConsultoriaFundoDetailPage — cross-company ConsultoriaFundo detail (T-5, D-30)
// Iter 2: direct GET /api/admin/fundos/consultorias/{id} — removes pageSize=200 hack (D-8).
// ---------------------------------------------------------------------------

import { useQuery } from "@tanstack/react-query";
import { useParams, useNavigate } from "@tanstack/react-router";
import { getAdminConsultoriaFundo } from "@/lib/admin-fundos-api";
import { AdminApiError } from "@/lib/admin-api";
import { AdminEntityHeader } from "@/components/molecules/AdminEntityHeader";
import { AdminFieldsGrid } from "@/components/molecules/AdminFieldsGrid";
import { AuditHistorySection } from "@/components/molecules/AuditHistorySection";
import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";
import { Loader2 } from "lucide-react";

export function AdminConsultoriaFundoDetailPage() {
  const { consultoriaId } = useParams({ from: "/admin/consultorias-fundo/$consultoriaId" as any });
  const navigate = useNavigate();

  const { data: consultoria, isLoading, isError, error } = useQuery({
    queryKey: ["admin-consultoria-fundo", consultoriaId],
    queryFn: () => getAdminConsultoriaFundo(consultoriaId),
    enabled: !!consultoriaId,
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
      <div className="py-12 text-center" data-testid="consultoria-detail-not-found">
        <p className="text-muted-foreground text-sm">{L.notFound}</p>
        <button
          type="button"
          onClick={() => navigate({ to: "/admin/consultorias-fundo" as any })}
          className="mt-4 text-sm text-primary hover:underline"
        >
          {L.backToList}
        </button>
      </div>
    );
  }

  if (isError) {
    return (
      <p className="text-destructive text-sm py-4" data-testid="consultoria-detail-error">
        {L.errorLoadingData}
      </p>
    );
  }

  if (!consultoria) return null;

  const fields = [
    { label: L.detailFieldId, value: consultoria.id, testId: "consultoria-id" },
    { label: L.colCnpj, value: consultoria.cnpj, testId: "consultoria-cnpj" },
    { label: L.detailFieldEmpresa, value: consultoria.empresaNome, testId: "consultoria-empresa" },
    { label: L.detailFieldNomeFantasia, value: consultoria.nomeFantasia, testId: "consultoria-nome-fantasia" },
    { label: L.detailFieldEmail, value: consultoria.email, testId: "consultoria-email" },
    { label: L.detailFieldTelefone, value: consultoria.telefone, testId: "consultoria-telefone" },
    { label: L.colCreatedAt, value: new Date(consultoria.createdAt).toLocaleDateString("pt-BR"), testId: "consultoria-created-at" },
  ];

  return (
    <div data-testid="consultoria-detail-page">
      <AdminEntityHeader
        title={L.detailConsultoriaTitle}
        nome={consultoria.razaoSocial}
        empresaNome={consultoria.empresaNome}
        status={consultoria.status}
        onBack={() => navigate({ to: "/admin/consultorias-fundo" as any })}
      />
      <div className="bg-card rounded-lg border p-6">
        <AdminFieldsGrid fields={fields} />
      </div>
      <AuditHistorySection entityType="ConsultoriaFundo" entityId={consultoriaId} />
    </div>
  );
}
