// ---------------------------------------------------------------------------
// ConsultoriasFundoListPage: paginated ConsultoriaFundo list (T-5)
// Route: /consultorias-fundo
// ---------------------------------------------------------------------------

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useSearch, useNavigate } from "@tanstack/react-router";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Plus } from "lucide-react";
import { ConsultoriaFundoTable } from "@/components/organisms/ConsultoriaFundoTable";
import { ConsultoriaFundoForm } from "@/components/organisms/ConsultoriaFundoForm";
import { SearchInput } from "@/components/molecules/SearchInput";
import { Paginator } from "@/components/molecules/Paginator";
import { useAuth } from "@/lib/auth-context";
import {
  listConsultoriasFundo,
  createConsultoriaFundo,
  updateConsultoriaFundo,
} from "@/lib/fundos-api";
import { parseApiError, mapApiErrorToForm, showSuccessToast } from "@/lib/api-errors";
import type {
  ConsultoriaFundoDto,
  CreateConsultoriaFundoData,
  UpdateConsultoriaFundoData,
} from "@/lib/fundos-schemas";
import { useForm } from "react-hook-form";

export function ConsultoriasFundoListPage() {
  const { auth } = useAuth();
  const permissions: string[] = (auth as any).permissions ?? [];
  const canWrite = permissions.includes("funds:write");

  const navigate = useNavigate({ from: "/consultorias-fundo" });
  const search = useSearch({ from: "/consultorias-fundo" as any });
  const page = (search as any).page ?? 1;
  const searchQuery = (search as any).search ?? "";

  const [dialogMode, setDialogMode] = useState<"create" | "edit" | null>(null);
  const [editItem, setEditItem] = useState<ConsultoriaFundoDto | null>(null);

  const qc = useQueryClient();
  const dummyForm = useForm();

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ["consultorias-fundo", { page, search: searchQuery }],
    queryFn: () => listConsultoriasFundo({ page, search: searchQuery, pageSize: 20 }),
  });

  const createMutation = useMutation({
    mutationFn: createConsultoriaFundo,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["consultorias-fundo"] });
      showSuccessToast("Consultoria de fundo criada com sucesso.");
      setDialogMode(null);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateConsultoriaFundoData }) =>
      updateConsultoriaFundo(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["consultorias-fundo"] });
      showSuccessToast("Consultoria de fundo atualizada com sucesso.");
      setDialogMode(null);
    },
  });

  function handleSearch(value: string) {
    navigate({ search: { page: 1, search: value } as any, replace: true });
  }

  function handlePageChange(newPage: number) {
    navigate({ search: { page: newPage, search: searchQuery } as any });
  }

  async function handleSubmit(
    formData: CreateConsultoriaFundoData | UpdateConsultoriaFundoData
  ) {
    try {
      if (dialogMode === "create") {
        await createMutation.mutateAsync(formData as CreateConsultoriaFundoData);
      } else if (editItem) {
        await updateMutation.mutateAsync({
          id: editItem.id,
          data: formData as UpdateConsultoriaFundoData,
        });
      }
    } catch (err) {
      if (err instanceof Response) {
        const problem = await parseApiError(err);
        mapApiErrorToForm(problem, dummyForm.setError);
      }
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Consultorias de Fundo</h1>
          <p className="text-muted-foreground">Gerencie as consultorias de fundos da empresa</p>
        </div>
        {canWrite && (
          <Button onClick={() => { setEditItem(null); setDialogMode("create"); }}>
            <Plus className="mr-2 h-4 w-4" aria-hidden="true" />
            Nova Consultoria
          </Button>
        )}
      </div>

      <div className="flex items-center gap-4">
        <SearchInput
          value={searchQuery}
          onChange={handleSearch}
          placeholder="Buscar por razão social..."
          className="max-w-sm"
        />
        {isFetching && !isLoading && (
          <span className="text-sm text-muted-foreground" role="status" aria-live="polite">
            Atualizando...
          </span>
        )}
      </div>

      <ConsultoriaFundoTable
        items={data?.items ?? []}
        isLoading={isLoading}
        canWrite={canWrite}
        onEdit={(item) => { setEditItem(item); setDialogMode("edit"); }}
      />

      {data && data.totalPages > 1 && (
        <div className="flex justify-center mt-4">
          <Paginator
            page={data.page}
            totalPages={data.totalPages}
            onPageChange={handlePageChange}
          />
        </div>
      )}

      <Dialog
        open={dialogMode !== null}
        onOpenChange={(open) => !open && setDialogMode(null)}
      >
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>
              {dialogMode === "create" ? "Nova Consultoria de Fundo" : "Editar Consultoria"}
            </DialogTitle>
          </DialogHeader>
          <ConsultoriaFundoForm
            mode={dialogMode ?? "create"}
            initial={editItem ?? undefined}
            onSubmit={handleSubmit}
            onCancel={() => setDialogMode(null)}
            isSubmitting={createMutation.isPending || updateMutation.isPending}
          />
        </DialogContent>
      </Dialog>
    </div>
  );
}
