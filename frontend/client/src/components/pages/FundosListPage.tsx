// ---------------------------------------------------------------------------
// FundosListPage: paginated Fundo list with status filter (T-6)
// Route: /fundos
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { FundoTable } from "@/components/organisms/FundoTable";
import { FundoForm } from "@/components/organisms/FundoForm";
import { SearchInput } from "@/components/molecules/SearchInput";
import { Paginator } from "@/components/molecules/Paginator";
import { useAuth } from "@/lib/auth-context";
import { listFundos, createFundo } from "@/lib/fundos-api";
import { parseApiError, mapApiErrorToForm, showSuccessToast } from "@/lib/api-errors";
import {
  FundoStatusEnum,
  FUNDO_STATUS_LABELS,
  type FundoDto,
  type CreateFundoData,
} from "@/lib/fundos-schemas";
import { useForm } from "react-hook-form";

export function FundosListPage() {
  const { auth } = useAuth();
  const permissions: string[] = (auth as any).permissions ?? [];
  const canWrite = permissions.includes("funds:write");

  const navigate = useNavigate({ from: "/fundos" });
  const search = useSearch({ from: "/fundos" as any });
  const page = (search as any).page ?? 1;
  const searchQuery = (search as any).search ?? "";
  const statusFilter = (search as any).status as string | undefined;

  const [createDialogOpen, setCreateDialogOpen] = useState(false);

  const qc = useQueryClient();
  const dummyForm = useForm();

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ["fundos", { page, search: searchQuery, status: statusFilter }],
    queryFn: () =>
      listFundos({
        page,
        search: searchQuery,
        pageSize: 20,
        status: statusFilter as any,
      }),
  });

  const createMutation = useMutation({
    mutationFn: createFundo,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["fundos"] });
      showSuccessToast("Fundo criado com sucesso.");
      setCreateDialogOpen(false);
    },
  });

  function handleSearch(value: string) {
    navigate({ search: { page: 1, search: value, status: statusFilter } as any, replace: true });
  }

  function handlePageChange(newPage: number) {
    navigate({ search: { page: newPage, search: searchQuery, status: statusFilter } as any });
  }

  function handleStatusFilter(value: string) {
    navigate({
      search: { page: 1, search: searchQuery, status: value === "all" ? undefined : value } as any,
      replace: true,
    });
  }

  function handleDetail(item: FundoDto) {
    navigate({ to: `/fundos/${item.id}` as any });
  }

  function handleEdit(item: FundoDto) {
    navigate({ to: `/fundos/${item.id}` as any });
  }

  async function handleSubmit(formData: CreateFundoData) {
    try {
      await createMutation.mutateAsync(formData);
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
          <h1 className="text-2xl font-bold tracking-tight">Fundos</h1>
          <p className="text-muted-foreground">Gerencie os fundos de investimento da empresa</p>
        </div>
        {canWrite && (
          <Button onClick={() => setCreateDialogOpen(true)}>
            <Plus className="mr-2 h-4 w-4" aria-hidden="true" />
            Novo Fundo
          </Button>
        )}
      </div>

      <div className="flex items-center gap-4">
        <SearchInput
          value={searchQuery}
          onChange={handleSearch}
          placeholder="Buscar por nome ou CNPJ..."
          className="max-w-sm"
        />
        <Select
          value={statusFilter ?? "all"}
          onValueChange={handleStatusFilter}
        >
          <SelectTrigger className="w-44" aria-label="Filtrar por status">
            <SelectValue placeholder="Todos os status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Todos os status</SelectItem>
            {FundoStatusEnum.options.map((s) => (
              <SelectItem key={s} value={s}>
                {FUNDO_STATUS_LABELS[s]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {isFetching && !isLoading && (
          <span className="text-sm text-muted-foreground" role="status" aria-live="polite">
            Atualizando...
          </span>
        )}
      </div>

      <FundoTable
        items={data?.items ?? []}
        isLoading={isLoading}
        canWrite={canWrite}
        onEdit={handleEdit}
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

      <Dialog open={createDialogOpen} onOpenChange={setCreateDialogOpen}>
        <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Novo Fundo</DialogTitle>
          </DialogHeader>
          <FundoForm
            mode="create"
            onSubmit={handleSubmit as any}
            onCancel={() => setCreateDialogOpen(false)}
            isSubmitting={createMutation.isPending}
          />
        </DialogContent>
      </Dialog>
    </div>
  );
}
