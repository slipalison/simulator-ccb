// ---------------------------------------------------------------------------
// TiposAtivoListPage: paginated TipoAtivo list with search, create, edit (T-3)
// Route: /tipos-ativos
// D-27: URL search params (page, search), debounce 300ms
// D-23: Permission-aware (funds:read to see, funds:write to create/edit)
// ---------------------------------------------------------------------------

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { tiposAtivoRouteApi } from "@/router";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Plus } from "lucide-react";
import { TipoAtivoTable } from "@/components/organisms/TipoAtivoTable";
import { TipoAtivoForm } from "@/components/organisms/TipoAtivoForm";
import { SearchInput } from "@/components/molecules/SearchInput";
import { Paginator } from "@/components/molecules/Paginator";
import { useAuth } from "@/lib/auth-context";
import {
  listTiposAtivo,
  createTipoAtivo,
  updateTipoAtivo,
} from "@/lib/fundos-api";
import { parseApiError, mapApiErrorToForm, showSuccessToast } from "@/lib/api-errors";
import type { TipoAtivoDto, CreateTipoAtivoData, UpdateTipoAtivoData } from "@/lib/fundos-schemas";
import { useForm } from "react-hook-form";

export function TiposAtivoListPage() {
  const { auth } = useAuth();
  const permissions = auth.permissions;
  const canWrite = permissions.includes("funds:write");

  const navigate = tiposAtivoRouteApi.useNavigate();
  const search = tiposAtivoRouteApi.useSearch();
  const page = search.page ?? 1;
  const searchQuery = search.search ?? "";

  const [dialogMode, setDialogMode] = useState<"create" | "edit" | null>(null);
  const [editItem, setEditItem] = useState<TipoAtivoDto | null>(null);

  const qc = useQueryClient();

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ["tipos-ativos", { page, search: searchQuery }],
    queryFn: () => listTiposAtivo({ page, search: searchQuery, pageSize: 20 }),
  });

  const createMutation = useMutation({
    mutationFn: createTipoAtivo,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["tipos-ativos"] });
      showSuccessToast("Tipo de ativo criado com sucesso.");
      setDialogMode(null);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateTipoAtivoData }) =>
      updateTipoAtivo(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["tipos-ativos"] });
      showSuccessToast("Tipo de ativo atualizado com sucesso.");
      setDialogMode(null);
    },
  });

  // Dummy form ref for setError (passed to mapApiErrorToForm)
  const dummyForm = useForm();

  function handleSearch(value: string) {
    navigate({ search: { page: 1, search: value }, replace: true });
  }

  function handlePageChange(newPage: number) {
    navigate({ search: { page: newPage, search: searchQuery } });
  }

  function handleOpenCreate() {
    setEditItem(null);
    setDialogMode("create");
  }

  function handleOpenEdit(item: TipoAtivoDto) {
    setEditItem(item);
    setDialogMode("edit");
  }

  async function handleSubmit(formData: CreateTipoAtivoData | UpdateTipoAtivoData) {
    try {
      if (dialogMode === "create") {
        await createMutation.mutateAsync(formData as CreateTipoAtivoData);
      } else if (editItem) {
        await updateMutation.mutateAsync({ id: editItem.id, data: formData as UpdateTipoAtivoData });
      }
    } catch (err) {
      if (err instanceof Response) {
        const problem = await parseApiError(err);
        mapApiErrorToForm(problem, dummyForm.setError);
      }
    }
  }

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Tipos de Ativo</h1>
          <p className="text-muted-foreground">
            Catálogo global de tipos de ativo (CVM)
          </p>
        </div>
        {canWrite && (
          <Button onClick={handleOpenCreate} aria-label="Criar novo tipo de ativo">
            <Plus className="mr-2 h-4 w-4" aria-hidden="true" />
            Novo Tipo de Ativo
          </Button>
        )}
      </div>

      <div className="flex items-center gap-4">
        <SearchInput
          value={searchQuery}
          onChange={handleSearch}
          placeholder="Buscar por código ou descrição..."
          className="max-w-sm"
        />
        {isFetching && !isLoading && (
          <span className="text-sm text-muted-foreground" role="status" aria-live="polite">
            Atualizando...
          </span>
        )}
      </div>

      <TipoAtivoTable
        items={data?.items ?? []}
        isLoading={isLoading}
        canWrite={canWrite}
        onEdit={handleOpenEdit}
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
              {dialogMode === "create" ? "Novo Tipo de Ativo" : "Editar Tipo de Ativo"}
            </DialogTitle>
          </DialogHeader>
          <TipoAtivoForm
            mode={dialogMode ?? "create"}
            initial={editItem ?? undefined}
            onSubmit={handleSubmit}
            onCancel={() => setDialogMode(null)}
            isSubmitting={isSubmitting}
          />
        </DialogContent>
      </Dialog>
    </div>
  );
}
