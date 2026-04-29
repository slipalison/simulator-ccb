import { useState, useEffect, useCallback } from "react";
import { useAuth } from "@/lib/auth-context";
import { Navigate } from "@tanstack/react-router";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Users, RefreshCw, UserPlus } from "lucide-react";
import { toast } from "sonner";
import {
  getEmployees,
  toggleEmployeeStatus,
  resetEmployeePassword,
  updateEmployee,
  deleteEmployee,
  changeEmployeeAccessGroup,
  EmployeeApiError,
} from "@/lib/api";
import type { EmployeeDto, PaginatedEmployeesResult } from "@/lib/types";
import { EmployeeSearchBar } from "@/components/molecules/EmployeeSearchBar";
import { EmployeesTable } from "@/components/molecules/EmployeesTable";
import { EditEmployeeDialog } from "@/components/molecules/EditEmployeeDialog";
import { BlockUnblockDialog } from "@/components/molecules/BlockUnblockDialog";
import { ResetPasswordDialog } from "@/components/molecules/ResetPasswordDialog";
import { DeleteEmployeeDialog } from "@/components/molecules/DeleteEmployeeDialog";
import { ChangeAccessGroupDialog } from "@/components/molecules/ChangeAccessGroupDialog";
import { RegisterEmployeeDialog } from "@/components/molecules/RegisterEmployeeDialog";

// ---------------------------------------------------------------------------
// Dialog state
// ---------------------------------------------------------------------------

type DialogState =
  | { type: "none" }
  | { type: "edit"; employee: EmployeeDto }
  | { type: "block-unblock"; employee: EmployeeDto; action: "block" | "unblock" }
  | { type: "reset-password"; employee: EmployeeDto; temporaryPassword: string | null }
  | { type: "delete"; employee: EmployeeDto }
  | { type: "change-access-group"; employee: EmployeeDto }
  | { type: "register" };

const PAGE_SIZE = 20;

export function EmployeesPage() {
  const { auth } = useAuth();
  const companyId = auth.companyId;
  const accessGroup = auth.accessGroup;

  // --- Data state ---
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("all");
  const [result, setResult] = useState<PaginatedEmployeesResult | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isError, setIsError] = useState(false);

  // --- Dialog state ---
  const [dialog, setDialog] = useState<DialogState>({ type: "none" });

  // Reset page when filters change
  useEffect(() => {
    setPage(1);
  }, [search, status]);

  // --- Fetch employees ---
  const fetchEmployees = useCallback(async () => {
    if (!companyId) return;
    setIsLoading(true);
    setIsError(false);
    try {
      const data = await getEmployees(companyId, {
        page,
        pageSize: PAGE_SIZE,
        search: search || undefined,
        status: status === "all" ? undefined : status,
      });
      setResult(data);
    } catch (err) {
      if (err instanceof EmployeeApiError && err.status === 403) {
        toast.error("Acesso negado", { description: "Você não tem permissão para visualizar funcionários." });
      } else {
        setIsError(true);
      }
    } finally {
      setIsLoading(false);
    }
  }, [companyId, page, search, status]);

  useEffect(() => {
    fetchEmployees();
  }, [fetchEmployees]);

  // --- Dialog handlers ---

  const handleOpenEdit = (employee: EmployeeDto) => {
    setDialog({ type: "edit", employee });
  };

  const handleOpenBlockUnblock = (employee: EmployeeDto) => {
    const action = employee.keycloakEnabled ? "block" : "unblock";
    setDialog({ type: "block-unblock", employee, action });
  };

  const handleOpenResetPassword = async (employee: EmployeeDto) => {
    if (!companyId) return;
    try {
      const res = await resetEmployeePassword(companyId, employee.id);
      toast.success("Senha temporária gerada.");
      setDialog({ type: "reset-password", employee, temporaryPassword: res.temporaryPassword });
    } catch (err) {
      const apiErr = err instanceof EmployeeApiError ? err : null;
      toast.error("Falha ao resetar senha", {
        description: apiErr?.status === 400 ? "Operação não permitida." : "Tente novamente.",
      });
    }
  };

  const handleOpenDelete = (employee: EmployeeDto) => {
    setDialog({ type: "delete", employee });
  };

  const handleChangeAccessGroup = (employee: EmployeeDto) => {
    setDialog({ type: "change-access-group", employee });
  };

  const handleOpenRegister = () => {
    setDialog({ type: "register" });
  };

  const handleEmployeeRegistered = (result: { employeeId: string; temporaryPassword: string }) => {
    toast.success("Funcionário cadastrado!", {
      description: `Senha temporária: ${result.temporaryPassword}`,
      duration: 15000,
    });
    setDialog({ type: "none" });
    fetchEmployees();
  };

  const handleCloseDialog = () => {
    setDialog({ type: "none" });
  };

  // --- Mutation handlers ---

  const handleSaveEdit = async (employeeId: string, data: { nome: string; email: string; phone: string }) => {
    if (!companyId) return;
    try {
      await updateEmployee(companyId, employeeId, data);
      toast.success("Funcionário atualizado com sucesso.");
      fetchEmployees();
    } catch (err) {
      const apiErr = err instanceof EmployeeApiError ? err : null;
      if (apiErr?.status === 409) {
        toast.error("Email já está em uso.", { description: "Escolha outro email e tente novamente." });
      } else {
        toast.error("Falha ao atualizar funcionário", { description: "Tente novamente." });
      }
      throw err;
    }
  };

  const handleToggleStatus = async (employeeId: string, activate: boolean) => {
    if (!companyId) return;
    try {
      await toggleEmployeeStatus(companyId, employeeId, activate);
      toast.success(activate ? "Funcionário desbloqueado." : "Funcionário bloqueado.");
      // Optimistic update: update local state immediately
      setResult((prev) => {
        if (!prev) return prev;
        return {
          ...prev,
          items: prev.items.map((e) =>
            e.id === employeeId ? { ...e, keycloakEnabled: activate } : e
          ),
        };
      });
      fetchEmployees();
    } catch {
      toast.error("Falha ao alterar status do funcionário", { description: "Tente novamente." });
      throw new Error("toggle failed");
    }
  };

  const handleDelete = async (employeeId: string) => {
    if (!companyId) return;
    try {
      await deleteEmployee(companyId, employeeId);
      toast.success("Funcionário excluído (LGPD).");
      handleCloseDialog();
      fetchEmployees();
    } catch {
      toast.error("Falha ao excluir funcionário", { description: "Tente novamente." });
      throw new Error("delete failed");
    }
  };

  const handleChangeGroup = async (employeeId: string, newGroupId: string) => {
    if (!companyId) return;
    try {
      await changeEmployeeAccessGroup(companyId, employeeId, newGroupId);
      toast.success("Grupo de acesso alterado.");
      handleCloseDialog();
      fetchEmployees();
    } catch {
      toast.error("Falha ao alterar grupo de acesso", { description: "Tente novamente." });
      throw new Error("change group failed");
    }
  };

  // --- Permission gates (after hooks) ---
  if (!auth.isAuthenticated) {
    return <Navigate to="/auth/login" />;
  }
  if (accessGroup !== "admin-empresa" && accessGroup !== "viewer" && accessGroup !== "dashboard") {
    return <Navigate to="/profile" />;
  }

  // --- Render ---
  return (
    <div className="space-y-6" data-testid="employees-page">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Users className="h-5 w-5 text-primary" aria-hidden="true" />
          <h2 className="text-xl font-semibold">Funcionários</h2>
        </div>
        <div className="flex gap-2">
          {accessGroup === "admin-empresa" && (
            <Button
              variant="default"
              size="sm"
              onClick={handleOpenRegister}
              data-testid="register-employee-button"
            >
              <UserPlus className="h-4 w-4 mr-1" aria-hidden="true" />
              Novo Funcionário
            </Button>
          )}
          <Button
            variant="outline"
            size="sm"
            onClick={fetchEmployees}
            disabled={isLoading}
            data-testid="refresh-button"
          >
            <RefreshCw className={`h-4 w-4 mr-1 ${isLoading ? "animate-spin" : ""}`} aria-hidden="true" />
            Atualizar
          </Button>
        </div>
      </div>

      <EmployeeSearchBar
        searchValue={search}
        statusValue={status}
        onSearchChange={setSearch}
        onStatusChange={setStatus}
        disabled={isLoading}
      />

      <Card>
        <CardHeader className="p-0" />
        <CardContent className="p-0">
          <EmployeesTable
            result={result}
            isLoading={isLoading}
            isError={isError}
            onRetry={fetchEmployees}
            accessGroup={accessGroup ?? ""}
            onEdit={handleOpenEdit}
            onBlockUnblock={handleOpenBlockUnblock}
            onResetPassword={handleOpenResetPassword}
            onDelete={handleOpenDelete}
            onChangeAccessGroup={handleChangeAccessGroup}
          />
        </CardContent>
      </Card>

      {/* Pagination */}
      {result && result.totalCount > 0 && (
        <div className="flex items-center justify-between" data-testid="employees-pagination">
          <p className="text-sm text-muted-foreground">
            Mostrando {((page - 1) * PAGE_SIZE) + 1}–{Math.min(page * PAGE_SIZE, result.totalCount)} de {result.totalCount}
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page <= 1 || isLoading}
              data-testid="prev-page-button"
            >
              Anterior
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => p + 1)}
              disabled={page * PAGE_SIZE >= result.totalCount || isLoading}
              data-testid="next-page-button"
            >
              Próxima
            </Button>
          </div>
        </div>
      )}

      {/* Edit Employee Dialog */}
      {dialog.type === "edit" && (
        <EditEmployeeDialog
          open={dialog.type === "edit"}
          employee={dialog.employee}
          companyId={companyId ?? ""}
          onSuccess={() => { handleCloseDialog(); fetchEmployees(); }}
          onOpenChange={(open: boolean) => { if (!open) handleCloseDialog(); }}
          onSave={handleSaveEdit}
        />
      )}

      {/* Block/Unblock Dialog */}
      {dialog.type === "block-unblock" && (
        <BlockUnblockDialog
          open={dialog.type === "block-unblock"}
          employee={dialog.employee}
          action={dialog.action}
          onConfirm={handleToggleStatus}
          onClose={handleCloseDialog}
        />
      )}

      {/* Reset Password Dialog */}
      <ResetPasswordDialog
        open={dialog.type === "reset-password"}
        temporaryPassword={dialog.type === "reset-password" ? dialog.temporaryPassword : null}
        onClose={handleCloseDialog}
      />

      {/* Delete Employee Dialog */}
      {dialog.type === "delete" && (
        <DeleteEmployeeDialog
          open={dialog.type === "delete"}
          employee={dialog.employee}
          companyId={companyId ?? ""}
          onDelete={handleDelete}
          onSuccess={() => { handleCloseDialog(); fetchEmployees(); }}
          onClose={handleCloseDialog}
        />
      )}

      {/* Change Access Group Dialog */}
      {dialog.type === "change-access-group" && (
        <ChangeAccessGroupDialog
          open={dialog.type === "change-access-group"}
          employee={dialog.employee}
          companyId={companyId ?? ""}
          onConfirm={handleChangeGroup}
          onClose={handleCloseDialog}
        />
      )}

      {/* Register Employee Dialog */}
      <RegisterEmployeeDialog
        open={dialog.type === "register"}
        companyId={companyId ?? ""}
        onRegistered={handleEmployeeRegistered}
        onClose={handleCloseDialog}
      />
    </div>
  );
}