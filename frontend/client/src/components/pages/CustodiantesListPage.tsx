// ---------------------------------------------------------------------------
// CustodiantesListPage: paginated Custodiante list (T-5)
// Route: /custodiantes
// ---------------------------------------------------------------------------

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { custodiantesRouteApi } from "@/router";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Plus } from "lucide-react";
import { CustodianteTable } from "@/components/organisms/CustodianteTable";
import { CustodianteForm } from "@/components/organisms/CustodianteForm";
import { SearchInput } from "@/components/molecules/SearchInput";
import { Paginator } from "@/components/molecules/Paginator";
import { useAuth } from "@/lib/auth-context";
import { listCustodiantes, createCustodiante, updateCustodiante } from "@/lib/fundos-api";
import { parseApiError, mapApiErrorToForm, showSuccessToast } from "@/lib/api-errors";
import type {
  CustodianteDto,
  CreateCustodianteData,
  UpdateCustodianteData,
} from "@/lib/fundos-schemas";
import { useForm } from "react-hook-form";

export function CustodiantesListPage() {
  const { auth } = useAuth();
  const permissions = auth.permissions;
  const canWrite = permissions.includes("funds:write");

  const navigate = custodiantesRouteApi.useNavigate();
  const search = custodiantesRouteApi.useSearch();
  const page = search.page ?? 1;
  const searchQuery = search.search ?? "";

  const [dialogMode, setDialogMode] = useState<"create" | "edit" | null>(null);
  const [editItem, setEditItem] = useState<CustodianteDto | null>(null);

  const qc = useQueryClient();
  const dummyForm = useForm();

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ["custodiantes", { page, search: searchQuery }],
    queryFn: () => listCustodiantes({ page, search: searchQuery, pageSize: 20 }),
  });

  const createMutation = useMutation({
    mutationFn: createCustodiante,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["custodiantes"] });
      showSuccessToast("Custodiante criado com sucesso.");
      setDialogMode(null);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateCustodianteData }) =>
      updateCustodiante(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["custodiantes"] });
      showSuccessToast("Custodiante atualizado com sucesso.");
      setDialogMode(null);
    },
  });

  function handleSearch(value: string) {
    navigate({ search: { page: 1, search: value }, replace: true });
  }

  function handlePageChange(newPage: number) {
    navigate({ search: { page: newPage, search: searchQuery } });
  }

  async function handleSubmit(
    formData: CreateCustodianteData | UpdateCustodianteData
  ) {
    try {
      if (dialogMode === "create") {
        await createMutation.mutateAsync(formData as CreateCustodianteData);
      } else if (editItem) {
        await updateMutation.mutateAsync({
          id: editItem.id,
          data: formData as UpdateCustodianteData,
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
          <h1 className="text-2xl font-bold tracking-tight">Custodiantes</h1>
          <p className="text-muted-foreground">Gerencie os custodiantes da empresa</p>
        </div>
        {canWrite && (
          <Button onClick={() => { setEditItem(null); setDialogMode("create"); }}>
            <Plus className="mr-2 h-4 w-4" aria-hidden="true" />
            Novo Custodiante
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

      <CustodianteTable
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
              {dialogMode === "create" ? "Novo Custodiante" : "Editar Custodiante"}
            </DialogTitle>
          </DialogHeader>
          <CustodianteForm
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
