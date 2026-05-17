// ---------------------------------------------------------------------------
// CedenteDetailPage: detail view for a Cedente (T-4)
// Route: /cedentes/$cedenteId
// Shows data + tabs (dados, tipos-ativos — T-7 populates the tab)
// ---------------------------------------------------------------------------

import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { cedenteDetailRouteApi } from "@/router";
import { getCedente } from "@/lib/fundos-api";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";
import { SIMPLE_STATUS_LABELS } from "@/lib/fundos-schemas";
import { CedenteTiposAtivosTabPage } from "@/components/pages/CedenteTiposAtivosTabPage";

export function CedenteDetailPage() {
  const { cedenteId } = cedenteDetailRouteApi.useParams();
  const navigate = useNavigate();

  const { data: cedente, isLoading } = useQuery({
    queryKey: ["cedente", cedenteId],
    queryFn: () => getCedente(cedenteId),
    enabled: !!cedenteId,
  });

  if (isLoading) {
    return (
      <div className="space-y-4" aria-busy="true" aria-label="Carregando cedente">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-32 w-full" />
      </div>
    );
  }

  if (!cedente) {
    return (
      <div className="py-12 text-center text-muted-foreground" role="status">
        Cedente não encontrado.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => navigate({ to: "/cedentes" })}
          aria-label="Voltar para lista de cedentes"
        >
          <ArrowLeft className="h-4 w-4" aria-hidden="true" />
        </Button>
        <div>
          <h1 className="text-2xl font-bold tracking-tight">{cedente.nome}</h1>
          <p className="text-muted-foreground">
            {cedente.cedenteTipo} · {cedente.documento}
          </p>
        </div>
        <span
          className={`ml-auto inline-flex rounded-full px-2.5 py-0.5 text-xs font-semibold ${
            cedente.status === "ATIVO"
              ? "bg-green-100 text-green-800"
              : "bg-gray-100 text-gray-600"
          }`}
        >
          {SIMPLE_STATUS_LABELS[cedente.status] ?? cedente.status}
        </span>
      </div>

      {/* Dados básicos */}
      <section className="rounded-lg border p-4 space-y-3" aria-label="Dados do cedente">
        <h2 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
          Dados
        </h2>
        <dl className="grid grid-cols-2 gap-3 text-sm">
          <div>
            <dt className="text-muted-foreground">Email</dt>
            <dd>{cedente.email ?? "—"}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">Telefone</dt>
            <dd>{cedente.telefone ?? "—"}</dd>
          </div>
          <div className="col-span-2">
            <dt className="text-muted-foreground">Endereço</dt>
            <dd>{cedente.endereco ?? "—"}</dd>
          </div>
        </dl>
      </section>

      {/* Tipos de Ativo associados (T-7) */}
      <section aria-label="Tipos de ativo associados">
        <h2 className="text-lg font-semibold mb-3">Tipos de Ativo</h2>
        <CedenteTiposAtivosTabPage cedenteId={cedenteId} />
      </section>
    </div>
  );
}
