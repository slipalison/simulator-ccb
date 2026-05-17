// ---------------------------------------------------------------------------
// FundoTiposAtivosTabPage: sub-route /fundos/$fundoId/tipos-ativos (T-7)
// ---------------------------------------------------------------------------

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Plus } from "lucide-react";
import { AssociationTable, type AssociationRow } from "@/components/organisms/AssociationTable";
import { AssociationForm } from "@/components/organisms/AssociationForm";
import { StatusTransitionDropdown } from "@/components/organisms/StatusTransitionDropdown";
import { Paginator } from "@/components/molecules/Paginator";
import { useAuth } from "@/lib/auth-context";
import {
  listFundoTiposAtivo,
  createFundoTipoAtivo,
  transitionFundoTipoAtivoStatus,
  listTiposAtivo,
} from "@/lib/fundos-api";
import { useFundoTipoAtivoAllowedTransitions } from "@/lib/use-allowed-transitions";
import { parseApiError, mapApiErrorToForm, showSuccessToast } from "@/lib/api-errors";
import {
  RELATIONSHIP_STATUS_LABELS,
  type CreateAssociationData,
  type RelationshipStatus,
} from "@/lib/fundos-schemas";
import { useForm } from "react-hook-form";

interface FundoTiposAtivosTabPageProps {
  fundoId: string;
}

export function FundoTiposAtivosTabPage({ fundoId }: FundoTiposAtivosTabPageProps) {
  const { auth } = useAuth();
  const permissions = auth.permissions;
  const canWrite = permissions.includes("funds:write");

  const [page, setPage] = useState(1);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);

  const qc = useQueryClient();
  const dummyForm = useForm();

  const { data, isLoading } = useQuery({
    queryKey: ["fundo-tipos-ativos", fundoId, page],
    queryFn: () => listFundoTiposAtivo(fundoId, { page, pageSize: 20, search: "" }),
    enabled: !!fundoId,
  });

  const { data: tiposAtivoData } = useQuery({
    queryKey: ["tipos-ativos-select"],
    queryFn: () => listTiposAtivo({ page: 1, pageSize: 100, search: "" }),
    enabled: createDialogOpen,
  });

  const createMutation = useMutation({
    mutationFn: (d: CreateAssociationData) =>
      createFundoTipoAtivo(fundoId, {
        tipoAtivoId: d.targetId,
        limitePercentual: d.limitePercentual,
        limiteValor: d.limiteValor,
        dataInicio: d.dataInicio,
        dataFim: d.dataFim,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["fundo-tipos-ativos", fundoId] });
      showSuccessToast("Associação Fundo-Tipo de Ativo criada com sucesso.");
      setCreateDialogOpen(false);
    },
  });

  async function handleCreate(formData: CreateAssociationData) {
    try {
      await createMutation.mutateAsync(formData);
    } catch (err) {
      if (err instanceof Response) {
        const problem = await parseApiError(err);
        mapApiErrorToForm(problem, dummyForm.setError);
      }
    }
  }

  const rows: AssociationRow[] =
    data?.items.map((item) => ({
      id: item.id,
      targetLabel:
        tiposAtivoData?.items.find((t) => t.id === item.tipoAtivoId)?.descricao ??
        item.tipoAtivoId,
      limitePercentual: item.limitePercentual,
      limiteValor: item.limiteValor,
      dataInicio: item.dataInicio,
      dataFim: item.dataFim,
      status: item.status,
    })) ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">
          Tipos de Ativo Associados
        </h3>
        {canWrite && (
          <Button size="sm" onClick={() => setCreateDialogOpen(true)}>
            <Plus className="mr-2 h-4 w-4" aria-hidden="true" />
            Associar Tipo de Ativo
          </Button>
        )}
      </div>

      <AssociationTable
        rows={rows}
        isLoading={isLoading}
        canWrite={canWrite}
        onEditLimits={() => {}}
        renderStatusControl={(row) => (
          <FundoTipoAtivoStatusControl
            fundoId={fundoId}
            associationId={row.id}
            currentStatus={row.status}
          />
        )}
      />

      {data && data.totalPages > 1 && (
        <Paginator page={page} totalPages={data.totalPages} onPageChange={setPage} />
      )}

      <Dialog open={createDialogOpen} onOpenChange={setCreateDialogOpen}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Associar Tipo de Ativo ao Fundo</DialogTitle>
          </DialogHeader>
          <AssociationForm
            targetOptions={
              tiposAtivoData?.items.map((t) => ({ id: t.id, label: `${t.codigo} - ${t.descricao}` })) ?? []
            }
            targetLabel="Tipo de Ativo"
            onSubmit={handleCreate}
            onCancel={() => setCreateDialogOpen(false)}
            isSubmitting={createMutation.isPending}
          />
        </DialogContent>
      </Dialog>
    </div>
  );
}

function FundoTipoAtivoStatusControl({
  fundoId,
  associationId,
  currentStatus,
}: {
  fundoId: string;
  associationId: string;
  currentStatus: RelationshipStatus;
}) {
  const qc = useQueryClient();
  const { data: allowedTransitions, isLoading } =
    useFundoTipoAtivoAllowedTransitions(fundoId, associationId);

  const transitionMutation = useMutation({
    mutationFn: (newStatus: RelationshipStatus) =>
      transitionFundoTipoAtivoStatus(fundoId, associationId, newStatus),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["fundo-tipos-ativos", fundoId] });
      qc.invalidateQueries({ queryKey: ["allowed-transitions", "fundo-tipo-ativo"] });
      showSuccessToast("Status da associação atualizado.");
    },
  });

  return (
    <StatusTransitionDropdown
      currentStatus={currentStatus}
      allowedTransitions={allowedTransitions}
      isLoadingTransitions={isLoading}
      onTransition={(s) => transitionMutation.mutateAsync(s).then(() => undefined)}
      statusLabels={RELATIONSHIP_STATUS_LABELS}
    />
  );
}
