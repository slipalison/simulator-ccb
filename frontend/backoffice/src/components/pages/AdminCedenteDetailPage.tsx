// ---------------------------------------------------------------------------
// AdminCedenteDetailPage — cross-company Cedente detail (T-5, D-30)
// ---------------------------------------------------------------------------

import { useQuery } from "@tanstack/react-query";
import { useParams, useNavigate } from "@tanstack/react-router";
import { listAdminCedentes } from "@/lib/admin-fundos-api";
import { AdminEntityHeader } from "@/components/molecules/AdminEntityHeader";
import { AdminFieldsGrid } from "@/components/molecules/AdminFieldsGrid";
import { AuditHistorySection } from "@/components/molecules/AuditHistorySection";
import { adminFundosLocale as L } from "@/locales/pt-BR/admin-fundos";
import { Loader2 } from "lucide-react";

export function AdminCedenteDetailPage() {
  const { cedenteId } = useParams({ from: "/admin/cedentes/$cedenteId" as any });
  const navigate = useNavigate();

  const { data, isLoading, isError } = useQuery({
    queryKey: ["admin-cedente-detail", cedenteId],
    queryFn: () => listAdminCedentes({ page: 1, pageSize: 200 }),
    enabled: !!cedenteId,
  });

  const cedente = data?.items.find((c) => c.id === cedenteId);

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
      <p className="text-destructive text-sm py-4" data-testid="cedente-detail-error">
        {L.errorLoadingData}
      </p>
    );
  }

  if (!cedente) {
    return (
      <div className="py-12 text-center" data-testid="cedente-detail-not-found">
        <p className="text-muted-foreground text-sm">{L.notFound}</p>
        <button
          type="button"
          onClick={() => navigate({ to: "/admin/cedentes" as any })}
          className="mt-4 text-sm text-primary hover:underline"
        >
          {L.backToList}
        </button>
      </div>
    );
  }

  const fields = [
    { label: L.detailFieldId, value: cedente.id, testId: "cedente-id" },
    { label: L.colDocumento, value: cedente.documento, testId: "cedente-documento" },
    { label: L.detailFieldEmpresa, value: cedente.empresaNome, testId: "cedente-empresa" },
    { label: L.detailFieldTipo, value: cedente.cedenteTipo === 0 || cedente.cedenteTipo === "PF" ? L.tipoCedentePF : L.tipoCedentePJ, testId: "cedente-tipo" },
    { label: L.detailFieldEmail, value: cedente.email, testId: "cedente-email" },
    { label: L.detailFieldTelefone, value: cedente.telefone, testId: "cedente-telefone" },
    { label: L.detailFieldEndereco, value: cedente.endereco, testId: "cedente-endereco" },
    { label: L.colCreatedAt, value: new Date(cedente.createdAt).toLocaleDateString("pt-BR"), testId: "cedente-created-at" },
  ];

  return (
    <div data-testid="cedente-detail-page">
      <AdminEntityHeader
        title={L.detailCedenteTitle}
        nome={cedente.nome}
        empresaNome={cedente.empresaNome}
        status={cedente.status}
        onBack={() => navigate({ to: "/admin/cedentes" as any })}
      />
      <div className="bg-card rounded-lg border p-6">
        <AdminFieldsGrid fields={fields} />
      </div>
      <AuditHistorySection entityType="Cedente" entityId={cedenteId} />
    </div>
  );
}
