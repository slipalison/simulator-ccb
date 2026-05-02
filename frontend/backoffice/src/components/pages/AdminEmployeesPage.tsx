import { useState, useEffect, useCallback } from "react";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { AdminSearchBar } from "@/components/molecules/AdminSearchBar";
import { AdminStatusFilter } from "@/components/molecules/AdminStatusFilter";
import { AdminPagination } from "@/components/molecules/AdminPagination";
import { listEmployees, blockEmployee, unblockEmployee, deleteEmployee } from "@/lib/admin-api";
import { toast } from "sonner";
import type { EmployeeSummaryDto, PaginatedResult } from "@/lib/admin-api";
import { Users, Trash2, Ban, CheckCircle } from "lucide-react";

export function AdminEmployeesPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [status, setStatus] = useState("all");
  const [result, setResult] = useState<PaginatedResult<EmployeeSummaryDto> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isError, setIsError] = useState(false);
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);

  const fetchEmployees = useCallback(async () => {
    setIsLoading(true);
    setIsError(false);
    try {
      const data = await listEmployees({
        page,
        pageSize: 20,
        search: debouncedSearch || undefined,
        status: status === "all" ? undefined : status,
      });
      setResult(data);
    } catch {
      setIsError(true);
      toast.error("Falha ao carregar funcionários", { description: "Tente novamente." });
    } finally {
      setIsLoading(false);
    }
  }, [page, debouncedSearch, status]);

  useEffect(() => { fetchEmployees(); }, [fetchEmployees]);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timer);
  }, [search]);

  useEffect(() => { setPage(1); }, [debouncedSearch, status]);

  const handleBlock = async (id: string) => {
    try {
      await blockEmployee(id);
      toast.success("Funcionário bloqueado.");
      fetchEmployees();
    } catch {
      toast.error("Falha ao bloquear funcionário.");
    }
  };

  const handleUnblock = async (id: string) => {
    try {
      await unblockEmployee(id);
      toast.success("Funcionário desbloqueado.");
      fetchEmployees();
    } catch {
      toast.error("Falha ao desbloquear funcionário.");
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await deleteEmployee(id);
      toast.success("Funcionário excluído (LGPD).");
      setConfirmDeleteId(null);
      fetchEmployees();
    } catch {
      toast.error("Falha ao excluir funcionário.");
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-col gap-4">
          <div className="flex items-center gap-2">
            <Users className="h-5 w-5 text-primary" />
            <h2 className="text-xl font-semibold">Funcionários</h2>
          </div>
          <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4">
            <AdminSearchBar
              value={search}
              onChange={setSearch}
              placeholder="Buscar por nome, CPF ou email"
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
        {isLoading && !result ? (
          <div className="space-y-3">
            {Array.from({ length: 5 }).map((_, i) => (
              <div key={i} className="h-12 bg-muted animate-pulse rounded" />
            ))}
          </div>
        ) : isError ? (
          <div className="text-center py-8 text-muted-foreground">
            <p>Erro ao carregar funcionários.</p>
            <button onClick={fetchEmployees} className="text-primary underline mt-2">Tentar novamente</button>
          </div>
        ) : !result || result.totalCount === 0 ? (
          <div className="text-center py-8 text-muted-foreground" data-testid="empty-state">
            Nenhum funcionário encontrado.
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full" data-testid="employees-table">
                <thead>
                  <tr className="border-b text-sm text-muted-foreground">
                    <th className="text-left py-3 px-2">Nome</th>
                    <th className="text-left py-3 px-2">CPF</th>
                    <th className="text-left py-3 px-2">Email</th>
                    <th className="text-left py-3 px-2">Empresa</th>
                    <th className="text-left py-3 px-2">Grupo</th>
                    <th className="text-left py-3 px-2">Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {result.items.map((emp) => (
                    <tr key={emp.id} className="border-b hover:bg-muted/50 transition-colors" data-testid={`employee-row-${emp.id}`}>
                      <td className="py-3 px-2 font-medium">{emp.nome}</td>
                      <td className="py-3 px-2 font-mono text-sm">{emp.cpf || "—"}</td>
                      <td className="py-3 px-2 text-sm">{emp.email}</td>
                      <td className="py-3 px-2 text-sm">{emp.companyRazaoSocial || "—"}</td>
                      <td className="py-3 px-2 text-sm">
                        <span className={`inline-flex items-center rounded-full px-2 py-1 text-xs font-medium ${
                          emp.accessGroupName === "admin-empresa" ? "bg-green-100 text-green-800" :
                          emp.accessGroupName === "viewer" ? "bg-gray-100 text-gray-800" :
                          emp.accessGroupName === "dashboard" ? "bg-blue-100 text-blue-800" :
                          "bg-gray-100 text-gray-500"
                        }`}>
                          {emp.accessGroupName || "—"}
                        </span>
                      </td>
                      <td className="py-3 px-2">
                        <div className="flex gap-1">
                          <Button variant="ghost" size="sm" onClick={() => handleBlock(emp.id)} title="Bloquear" data-testid={`block-${emp.id}`}>
                            <Ban className="h-4 w-4" />
                          </Button>
                          <Button variant="ghost" size="sm" onClick={() => handleUnblock(emp.id)} title="Desbloquear" data-testid={`unblock-${emp.id}`}>
                            <CheckCircle className="h-4 w-4" />
                          </Button>
                          <Button variant="ghost" size="sm" onClick={() => setConfirmDeleteId(emp.id)} title="Excluir LGPD" data-testid={`delete-${emp.id}`}>
                            <Trash2 className="h-4 w-4 text-destructive" />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {result.totalCount > 0 && (
              <div className="mt-4">
                <AdminPagination
                  page={result.page}
                  pageSize={result.pageSize}
                  totalCount={result.totalCount}
                  onPageChange={setPage}
                />
              </div>
            )}
          </>
        )}

        {confirmDeleteId && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50" data-testid="delete-confirm-dialog">
            <div className="bg-card rounded-lg p-6 max-w-sm mx-4">
              <h3 className="text-lg font-semibold mb-2">Confirmar exclusão LGPD</h3>
              <p className="text-sm text-muted-foreground mb-4">
                Esta ação é irreversível. Os dados do funcionário serão anonimizados e a conta Keycloak será excluída.
              </p>
              <div className="flex gap-2 justify-end">
                <Button variant="outline" size="sm" onClick={() => setConfirmDeleteId(null)}>Cancelar</Button>
                <Button variant="destructive" size="sm" onClick={() => handleDelete(confirmDeleteId)}>Excluir</Button>
              </div>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}