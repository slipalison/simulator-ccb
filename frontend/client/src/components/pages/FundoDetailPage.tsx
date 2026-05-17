// ---------------------------------------------------------------------------
// FundoDetailPage: detail + status transition + tabs (T-6, T-7)
// Route: /fundos/$fundoId
// ---------------------------------------------------------------------------

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useParams, useNavigate } from "@tanstack/react-router";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { ArrowLeft, Pencil } from "lucide-react";
import { getFundo, updateFundo, transitionFundoStatus } from "@/lib/fundos-api";
import { useFundoAllowedTransitions } from "@/lib/use-allowed-transitions";
import { parseApiError, mapApiErrorToForm, showSuccessToast } from "@/lib/api-errors";
import { FundoStatusBadge } from "@/components/organisms/FundoStatusBadge";
import { StatusTransitionDropdown } from "@/components/organisms/StatusTransitionDropdown";
import { FundoForm } from "@/components/organisms/FundoForm";
import { Skeleton } from "@/components/ui/skeleton";
import {
  FUNDO_STATUS_LABELS,
  TIPO_FUNDO_LABELS,
  type FundoStatus,
  type UpdateFundoData,
} from "@/lib/fundos-schemas";
import { FundoCedentesTabPage } from "@/components/pages/FundoCedentesTabPage";
import { FundoTiposAtivosTabPage } from "@/components/pages/FundoTiposAtivosTabPage";
import { useAuth } from "@/lib/auth-context";
import { useForm } from "react-hook-form";

type Tab = "dados" | "cedentes" | "tipos-ativos";

export function FundoDetailPage() {
  const params = useParams({ from: "/fundos/$fundoId" as any });
  const fundoId = (params as any).fundoId as string;
  const navigate = useNavigate();
  const { auth } = useAuth();
  const permissions = auth.permissions;
  const canWrite = permissions.includes("funds:write");
  const canManage = permissions.includes("funds:manage");

  const [activeTab, setActiveTab] = useState<Tab>("dados");
  const [editDialogOpen, setEditDialogOpen] = useState(false);

  const qc = useQueryClient();
  const dummyForm = useForm();

  const { data: fundo, isLoading } = useQuery({
    queryKey: ["fundo", fundoId],
    queryFn: () => getFundo(fundoId),
    enabled: !!fundoId,
  });

  const { data: allowedTransitions, isLoading: isLoadingTransitions } =
    useFundoAllowedTransitions(fundoId);

  const updateMutation = useMutation({
    mutationFn: (data: UpdateFundoData) => updateFundo(fundoId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["fundo", fundoId] });
      qc.invalidateQueries({ queryKey: ["fundos"] });
      showSuccessToast("Fundo atualizado com sucesso.");
      setEditDialogOpen(false);
    },
  });

  const transitionMutation = useMutation({
    mutationFn: (newStatus: FundoStatus) => transitionFundoStatus(fundoId, newStatus),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["fundo", fundoId] });
      qc.invalidateQueries({ queryKey: ["fundos"] });
      qc.invalidateQueries({ queryKey: ["allowed-transitions", "fundo", fundoId] });
      showSuccessToast("Status do fundo atualizado com sucesso.");
    },
  });

  async function handleUpdateSubmit(formData: UpdateFundoData) {
    try {
      await updateMutation.mutateAsync(formData);
    } catch (err) {
      if (err instanceof Response) {
        const problem = await parseApiError(err);
        mapApiErrorToForm(problem, dummyForm.setError);
      }
    }
  }

  async function handleTransition(newStatus: FundoStatus) {
    try {
      await transitionMutation.mutateAsync(newStatus);
    } catch (err) {
      if (err instanceof Response) {
        const problem = await parseApiError(err);
        mapApiErrorToForm(problem, dummyForm.setError);
      }
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-4" aria-busy="true" aria-label="Carregando fundo">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-32 w-full" />
      </div>
    );
  }

  if (!fundo) {
    return (
      <div className="py-12 text-center text-muted-foreground" role="status">
        Fundo não encontrado.
      </div>
    );
  }

  const tabs: { id: Tab; label: string }[] = [
    { id: "dados", label: "Dados" },
    { id: "cedentes", label: "Cedentes" },
    { id: "tipos-ativos", label: "Tipos de Ativo" },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start gap-4">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => navigate({ to: "/fundos" as any })}
          aria-label="Voltar para lista de fundos"
        >
          <ArrowLeft className="h-4 w-4" aria-hidden="true" />
        </Button>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight">{fundo.nome}</h1>
            <FundoStatusBadge status={fundo.status} />
          </div>
          <p className="text-muted-foreground text-sm mt-0.5">
            {TIPO_FUNDO_LABELS[fundo.tipoFundo]} · CNPJ: {fundo.cnpj}
          </p>
        </div>
        <div className="flex items-center gap-2">
          {(canWrite || canManage) && (
            <StatusTransitionDropdown
              currentStatus={fundo.status}
              allowedTransitions={allowedTransitions}
              isLoadingTransitions={isLoadingTransitions}
              onTransition={handleTransition}
              statusLabels={FUNDO_STATUS_LABELS}
              label="Alterar status do fundo"
            />
          )}
          {canWrite && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setEditDialogOpen(true)}
              aria-label="Editar fundo"
            >
              <Pencil className="h-4 w-4 mr-2" aria-hidden="true" />
              Editar
            </Button>
          )}
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b">
        <nav className="flex gap-1" role="tablist" aria-label="Seções do fundo">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              role="tab"
              aria-selected={activeTab === tab.id}
              aria-controls={`tabpanel-${tab.id}`}
              className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
                activeTab === tab.id
                  ? "border-primary text-primary"
                  : "border-transparent text-muted-foreground hover:text-foreground"
              }`}
              onClick={() => setActiveTab(tab.id)}
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      {/* Tab panels */}
      {activeTab === "dados" && (
        <section
          id="tabpanel-dados"
          role="tabpanel"
          aria-label="Dados do fundo"
          className="rounded-lg border p-4 space-y-3"
        >
          <dl className="grid grid-cols-2 gap-3 text-sm">
            <div>
              <dt className="text-muted-foreground">Tipo de Fundo</dt>
              <dd>{TIPO_FUNDO_LABELS[fundo.tipoFundo]}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Classe ANBIMA</dt>
              <dd>{fundo.classeAnbima ?? "—"}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Segmento</dt>
              <dd>{fundo.segmento ?? "—"}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Data de Constituição</dt>
              <dd>
                {fundo.dataConstituicao
                  ? new Date(fundo.dataConstituicao).toLocaleDateString("pt-BR")
                  : "—"}
              </dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Criado em</dt>
              <dd>{new Date(fundo.createdAt).toLocaleDateString("pt-BR")}</dd>
            </div>
          </dl>
        </section>
      )}

      {activeTab === "cedentes" && (
        <section id="tabpanel-cedentes" role="tabpanel" aria-label="Cedentes do fundo">
          <FundoCedentesTabPage fundoId={fundoId} />
        </section>
      )}

      {activeTab === "tipos-ativos" && (
        <section id="tabpanel-tipos-ativos" role="tabpanel" aria-label="Tipos de ativo do fundo">
          <FundoTiposAtivosTabPage fundoId={fundoId} />
        </section>
      )}

      {/* Edit dialog */}
      <Dialog open={editDialogOpen} onOpenChange={setEditDialogOpen}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Editar Fundo</DialogTitle>
          </DialogHeader>
          <FundoForm
            mode="edit"
            initial={fundo}
            onSubmit={handleUpdateSubmit as any}
            onCancel={() => setEditDialogOpen(false)}
            isSubmitting={updateMutation.isPending}
          />
        </DialogContent>
      </Dialog>
    </div>
  );
}
