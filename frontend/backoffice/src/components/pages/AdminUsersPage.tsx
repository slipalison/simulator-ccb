import { useState, useEffect, useCallback } from "react";
import { useNavigate } from "@tanstack/react-router";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { AdminSearchBar } from "@/components/molecules/AdminSearchBar";
import { AdminStatusFilter } from "@/components/molecules/AdminStatusFilter";
import { AdminUsersTable } from "@/components/molecules/AdminUsersTable";
import { AdminPagination } from "@/components/molecules/AdminPagination";
import { listUsers } from "@/lib/admin-api";
import { toast } from "sonner";
import type { UserSummaryDto, PaginatedResult } from "@/lib/admin-api";

export function AdminUsersPage() {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [status, setStatus] = useState("all");
  const [result, setResult] = useState<PaginatedResult<UserSummaryDto> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isError, setIsError] = useState(false);

  const fetchUsers = useCallback(async () => {
    setIsLoading(true);
    setIsError(false);
    try {
      const data = await listUsers({
        page,
        pageSize: 20,
        search: debouncedSearch || undefined,
        status: status === "all" ? undefined : status,
      });
      setResult(data);
    } catch (err) {
      setIsError(true);
      toast.error("Falha ao carregar usuarios", {
        description: "Tente novamente.",
      });
    } finally {
      setIsLoading(false);
    }
  }, [page, debouncedSearch, status]);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  // Debounce search: 300ms
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timer);
  }, [search]);

  // Reset to page 1 when search or status changes
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, status]);

  const handleViewDetails = (id: string) => {
    navigate({ to: "/admin/users/$id" as any, params: { id } });
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-col gap-4">
          <h2 className="text-xl font-semibold">Usuarios Cadastrados</h2>
          <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4">
            <AdminSearchBar
              value={search}
              onChange={setSearch}
              placeholder="Buscar por nome, CPF/CNPJ ou email"
              disabled={isLoading}
            />
            <AdminStatusFilter
              value={status}
              onChange={setStatus}
              disabled={isLoading}
            />
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <AdminUsersTable
          users={result?.items ?? []}
          onViewDetails={handleViewDetails}
          isLoading={isLoading && !result}
          isEmpty={!isLoading && !isError && result?.totalCount === 0}
          isError={isError}
          onRetry={fetchUsers}
        />
        {result && result.totalCount > 0 && (
          <div className="mt-4">
            <AdminPagination
              page={result.page}
              pageSize={result.pageSize}
              totalCount={result.totalCount}
              onPageChange={setPage}
            />
          </div>
        )}
      </CardContent>
    </Card>
  );
}
