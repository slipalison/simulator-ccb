// ---------------------------------------------------------------------------
// FundoCedentesTabPage: sub-route /fundos/$fundoId/cedentes (T-7)
// Embedded as tab in FundoDetailPage
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
  listFundoCedentes,
  createFundoCedente,
  transitionFundoCedenteStatus,
  listCedentes,
} from "@/lib/fundos-api";
import { useFundoCedenteAllowedTransitions } from "@/lib/use-allowed-transitions";
import { parseApiError, mapApiErrorToForm, showSuccessToast } from "@/lib/api-errors";
import {
  RELATIONSHIP_STATUS_LABELS,
  type CreateAssociationData,
  type RelationshipStatus,
} from "@/lib/fundos-schemas";
import { useForm } from "react-hook-form";

interface FundoCedentesTabPageProps {
  fundoId: string;
}

export function FundoCedentesTabPage({ fundoId }: FundoCedentesTabPageProps) {
  const { auth } = useAuth();
  const permissions: string[] = (auth as any).permissions ?? [];
  const canWrite = permissions.includes("funds:write");

  const [page, setPage] = useState(1);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);

  const qc = useQueryClient();
  const dummyForm = useForm();

  const { data, isLoading } = useQuery({
    queryKey: ["fundo-cedentes", fundoId, page],
    queryFn: () => listFundoCedentes(fundoId, { page, pageSize: 20, search: "" }),
    enabled: !!fundoId,
  });

  // Cedentes list for the association form dropdown
  const { data: cedentesData } = useQuery({
    queryKey: ["cedentes-select"],
    queryFn: () => listCedentes({ page: 1, pageSize: 100, search: "" }),
    enabled: createDialogOpen,
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateAssociationData) =>
      createFundoCedente(fundoId, {
        cedenteId: data.targetId,
        limitePercentual: data.limitePercentual,
        limiteValor: data.limiteValor,
        dataInicio: data.dataInicio,
        dataFim: data.dataFim,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["fundo-cedentes", fundoId] });
      showSuccessToast("Associação Fundo-Cedente criada com sucesso.");
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
        cedentesData?.items.find((c) => c.id === item.cedenteId)?.nome ?? item.cedenteId,
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
          Cedentes Associados
        </h3>
        {canWrite && (
          <Button size="sm" onClick={() => setCreateDialogOpen(true)}>
            <Plus className="mr-2 h-4 w-4" aria-hidden="true" />
            Associar Cedente
          </Button>
        )}
      </div>

      <AssociationTable
        rows={rows}
        isLoading={isLoading}
        canWrite={canWrite}
        onEditLimits={() => {}} // TODO: limits edit dialog
        renderStatusControl={(row) => (
          <FundoCedenteStatusControl
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
            <DialogTitle>Associar Cedente ao Fundo</DialogTitle>
          </DialogHeader>
          <AssociationForm
            targetOptions={
              cedentesData?.items.map((c) => ({ id: c.id, label: c.nome })) ?? []
            }
            targetLabel="Cedente"
            onSubmit={handleCreate}
            onCancel={() => setCreateDialogOpen(false)}
            isSubmitting={createMutation.isPending}
          />
        </DialogContent>
      </Dialog>
    </div>
  );
}

// ---------------------------------------------------------------------------
// FundoCedenteStatusControl: inline status transition per row
// ---------------------------------------------------------------------------

function FundoCedenteStatusControl({
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
    useFundoCedenteAllowedTransitions(fundoId, associationId);

  const transitionMutation = useMutation({
    mutationFn: (newStatus: RelationshipStatus) =>
      transitionFundoCedenteStatus(fundoId, associationId, newStatus),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["fundo-cedentes", fundoId] });
      qc.invalidateQueries({ queryKey: ["allowed-transitions", "fundo-cedente"] });
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
