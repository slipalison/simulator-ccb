// ---------------------------------------------------------------------------
// CedentesListPage: paginated Cedente list with PF/PJ create (T-4)
// Route: /cedentes
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
import { CedenteTable } from "@/components/organisms/CedenteTable";
import { CedentePfForm } from "@/components/organisms/CedentePfForm";
import { CedentePjForm } from "@/components/organisms/CedentePjForm";
import { CedenteTipoToggle } from "@/components/atoms/CedenteTipoToggle";
import { SearchInput } from "@/components/molecules/SearchInput";
import { Paginator } from "@/components/molecules/Paginator";
import { useAuth } from "@/lib/auth-context";
import { listCedentes, createCedentePf, createCedentePj } from "@/lib/fundos-api";
import { parseApiError, mapApiErrorToForm, showSuccessToast } from "@/lib/api-errors";
import type {
  CedenteDto,
  CedenteTipo,
  CreateCedentePfData,
  CreateCedentePjData,
} from "@/lib/fundos-schemas";
import { useForm } from "react-hook-form";

export function CedentesListPage() {
  const { auth } = useAuth();
  const permissions = auth.permissions;
  const canWrite = permissions.includes("funds:write");

  const navigate = useNavigate({ from: "/cedentes" });
  const search = useSearch({ from: "/cedentes" as any });
  const page = (search as any).page ?? 1;
  const searchQuery = (search as any).search ?? "";

  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [newCedenteTipo, setNewCedenteTipo] = useState<CedenteTipo>("PF");
  const [_editItem, _setEditItem] = useState<CedenteDto | null>(null);

  const qc = useQueryClient();
  const dummyForm = useForm();

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ["cedentes", { page, search: searchQuery }],
    queryFn: () => listCedentes({ page, search: searchQuery, pageSize: 20 }),
  });

  const createPfMutation = useMutation({
    mutationFn: createCedentePf,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["cedentes"] });
      showSuccessToast("Cedente PF criado com sucesso.");
      setCreateDialogOpen(false);
    },
  });

  const createPjMutation = useMutation({
    mutationFn: createCedentePj,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["cedentes"] });
      showSuccessToast("Cedente PJ criado com sucesso.");
      setCreateDialogOpen(false);
    },
  });

  function handleSearch(value: string) {
    navigate({ search: { page: 1, search: value } as any, replace: true });
  }

  function handlePageChange(newPage: number) {
    navigate({ search: { page: newPage, search: searchQuery } as any });
  }

  function handleDetail(item: CedenteDto) {
    navigate({ to: `/cedentes/${item.id}` as any });
  }

  async function handleSubmitPf(formData: CreateCedentePfData) {
    try {
      await createPfMutation.mutateAsync(formData);
    } catch (err) {
      if (err instanceof Response) {
        const problem = await parseApiError(err);
        mapApiErrorToForm(problem, dummyForm.setError);
      }
    }
  }

  async function handleSubmitPj(formData: CreateCedentePjData) {
    try {
      await createPjMutation.mutateAsync(formData);
    } catch (err) {
      if (err instanceof Response) {
        const problem = await parseApiError(err);
        mapApiErrorToForm(problem, dummyForm.setError);
      }
    }
  }

  const isSubmitting = createPfMutation.isPending || createPjMutation.isPending;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Cedentes</h1>
          <p className="text-muted-foreground">Gerencie os cedentes da empresa</p>
        </div>
        {canWrite && (
          <Button onClick={() => setCreateDialogOpen(true)}>
            <Plus className="mr-2 h-4 w-4" aria-hidden="true" />
            Novo Cedente
          </Button>
        )}
      </div>

      <div className="flex items-center gap-4">
        <SearchInput
          value={searchQuery}
          onChange={handleSearch}
          placeholder="Buscar por nome ou documento..."
          className="max-w-sm"
        />
        {isFetching && !isLoading && (
          <span className="text-sm text-muted-foreground" role="status" aria-live="polite">
            Atualizando...
          </span>
        )}
      </div>

      <CedenteTable
        items={data?.items ?? []}
        isLoading={isLoading}
        canWrite={canWrite}
        onEdit={(item) => _setEditItem(item)}
        onDetail={handleDetail}
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

      {/* Create dialog */}
      <Dialog open={createDialogOpen} onOpenChange={setCreateDialogOpen}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Novo Cedente</DialogTitle>
          </DialogHeader>
          <div className="mb-4">
            <CedenteTipoToggle
              value={newCedenteTipo}
              onChange={setNewCedenteTipo}
              disabled={isSubmitting}
            />
          </div>
          {newCedenteTipo === "PF" ? (
            <CedentePfForm
              onSubmit={handleSubmitPf}
              onCancel={() => setCreateDialogOpen(false)}
              isSubmitting={isSubmitting}
            />
          ) : (
            <CedentePjForm
              onSubmit={handleSubmitPj}
              onCancel={() => setCreateDialogOpen(false)}
              isSubmitting={isSubmitting}
            />
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
