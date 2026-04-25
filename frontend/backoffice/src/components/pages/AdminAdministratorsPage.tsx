import { useState, useEffect, useCallback } from "react";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Shield, RefreshCw, UserPlus } from "lucide-react";
import { toast } from "sonner";
import {
  getAdministratorsPaginated,
  updateAdministrator,
  resetAdministratorPassword,
  toggleAdministratorStatus,
  AdminApiError,
} from "@/lib/admin-api";
import type { AdminUserDto, PaginatedResult } from "@/lib/admin-api";
import { AdminSearchBar } from "@/components/molecules/AdminSearchBar";
import { AdminStatusFilter, ADMIN_STATUS_OPTIONS } from "@/components/molecules/AdminStatusFilter";
import { AdminPagination } from "@/components/molecules/AdminPagination";
import { AdminAdministratorsTable } from "@/components/molecules/AdminAdministratorsTable";
import { EditAdminDialog } from "@/components/molecules/EditAdminDialog";
import { ResetPasswordDialog } from "@/components/molecules/ResetPasswordDialog";
import { DeactivateAdminDialog } from "@/components/molecules/DeactivateAdminDialog";
import { ReactivateAdminDialog } from "@/components/molecules/ReactivateAdminDialog";
import type { AdminEditAdministratorInput } from "@/lib/validation-schemas";

type DialogState =
  | { type: "none" }
  | { type: "edit"; admin: AdminUserDto }
  | { type: "reset-password"; password: string }
  | { type: "deactivate"; admin: AdminUserDto }
  | { type: "reactivate"; admin: AdminUserDto };

export function AdminAdministratorsPage() {
  const [page, setPage] = useState(1);
  const [nameSearch, setNameSearch] = useState("");
  const [emailSearch, setEmailSearch] = useState("");
  const [status, setStatus] = useState("all");

  const [result, setResult] = useState<PaginatedResult<AdminUserDto> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isError, setIsError] = useState(false);

  const [dialog, setDialog] = useState<DialogState>({ type: "none" });

  useEffect(() => {
    setPage(1);
  }, [nameSearch, emailSearch, status]);

  const fetchAdmins = useCallback(async () => {
    setIsLoading(true);
    setIsError(false);
    try {
      const data = await getAdministratorsPaginated({
        page,
        pageSize: 20,
        name: nameSearch || undefined,
        email: emailSearch || undefined,
        status: status === "all" ? undefined : status,
      });
      setResult(data);
    } catch {
      setIsError(true);
    } finally {
      setIsLoading(false);
    }
  }, [page, nameSearch, emailSearch, status]);

  useEffect(() => {
    fetchAdmins();
  }, [fetchAdmins]);

  const handleOpenEdit = (admin: AdminUserDto) => {
    setDialog({ type: "edit", admin });
  };

  const handleOpenResetPassword = async (admin: AdminUserDto) => {
    try {
      const result = await resetAdministratorPassword(admin.id, admin.fullName);
      toast.success("Senha temporária gerada.");
      setDialog({ type: "reset-password", password: result.temporaryPassword });
    } catch (err) {
      const status = err instanceof AdminApiError ? err.status : undefined;
      toast.error("Falha ao resetar senha", {
        description: status === 400 ? "Operação não permitida." : "Tente novamente.",
      });
    }
  };

  const handleOpenDeactivate = (admin: AdminUserDto) => {
    setDialog({ type: "deactivate", admin });
  };

  const handleOpenReactivate = (admin: AdminUserDto) => {
    setDialog({ type: "reactivate", admin });
  };

  const handleCloseDialog = () => {
    setDialog({ type: "none" });
  };

  const handleSaveEdit = async (adminId: string, data: AdminEditAdministratorInput) => {
    try {
      await updateAdministrator(adminId, { fullName: data.fullName, email: data.email });
      toast.success("Administrador atualizado com sucesso.");
      fetchAdmins();
    } catch (err) {
      const apiErr = err instanceof AdminApiError ? err : null;
      if (apiErr?.status === 409) {
        toast.error("Email já está em uso.", {
          description: "Escolha outro email e tente novamente.",
        });
      } else {
        toast.error("Falha ao atualizar administrador", { description: "Tente novamente." });
      }
      throw err;
    }
  };

  const handleDeactivate = async (adminId: string) => {
    try {
      await toggleAdministratorStatus(
        adminId,
        dialog.type === "deactivate" ? dialog.admin.fullName : "",
        false
      );
      toast.success("Administrador desativado.");
      fetchAdmins();
    } catch (err) {
      const apiErr = err instanceof AdminApiError ? err : null;
      if (apiErr?.status === 400 || apiErr?.status === 409) {
        toast.error("Não é possível desativar.", {
          description: "Deve existir ao menos um administrador ativo.",
        });
      } else {
        toast.error("Falha ao desativar administrador", { description: "Tente novamente." });
      }
      throw err;
    }
  };

  const handleReactivate = async (adminId: string) => {
    try {
      await toggleAdministratorStatus(
        adminId,
        dialog.type === "reactivate" ? dialog.admin.fullName : "",
        true
      );
      toast.success("Administrador reativado.");
      fetchAdmins();
    } catch {
      toast.error("Falha ao reativar administrador", { description: "Tente novamente." });
      throw new Error("reactivate failed");
    }
  };

  return (
    <div className="space-y-6" data-testid="admin-administrators-page">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Shield className="h-5 w-5 text-primary" aria-hidden="true" />
          <h2 className="text-xl font-semibold">Administradores</h2>
        </div>
        <div className="flex gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={fetchAdmins}
            disabled={isLoading}
            data-testid="refresh-button"
          >
            <RefreshCw className={`h-4 w-4 mr-1 ${isLoading ? "animate-spin" : ""}`} aria-hidden="true" />
            Atualizar
          </Button>
          <Button
            size="sm"
            onClick={() => { window.location.href = "/admin/create"; }}
            data-testid="create-admin-button"
          >
            <UserPlus className="h-4 w-4 mr-1" aria-hidden="true" />
            Criar Admin
          </Button>
        </div>
      </div>

      <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4">
        <AdminSearchBar
          value={nameSearch}
          onChange={(val) => setNameSearch(val)}
          placeholder="Buscar por nome..."
          disabled={isLoading}
        />
        <AdminSearchBar
          value={emailSearch}
          onChange={(val) => setEmailSearch(val)}
          placeholder="Buscar por email..."
          disabled={isLoading}
        />
        <AdminStatusFilter
          value={status}
          onChange={(val) => setStatus(val)}
          disabled={isLoading}
          options={ADMIN_STATUS_OPTIONS}
        />
      </div>

      <Card>
        <CardHeader className="p-0" />
        <CardContent className="p-0">
          <AdminAdministratorsTable
            result={result}
            isLoading={isLoading}
            isError={isError}
            onRetry={fetchAdmins}
            onEdit={handleOpenEdit}
            onResetPassword={handleOpenResetPassword}
            onDeactivate={handleOpenDeactivate}
            onReactivate={handleOpenReactivate}
          />
        </CardContent>
      </Card>

      {result && result.totalCount > 0 && (
        <div className="mt-4">
          <AdminPagination
            page={page}
            pageSize={20}
            totalCount={result.totalCount}
            onPageChange={setPage}
          />
        </div>
      )}

      <EditAdminDialog
        open={dialog.type === "edit"}
        admin={dialog.type === "edit" ? dialog.admin : null}
        onClose={handleCloseDialog}
        onSave={handleSaveEdit}
      />

      <ResetPasswordDialog
        open={dialog.type === "reset-password"}
        generatedPassword={dialog.type === "reset-password" ? dialog.password : null}
        onClose={handleCloseDialog}
      />

      <DeactivateAdminDialog
        open={dialog.type === "deactivate"}
        admin={dialog.type === "deactivate" ? dialog.admin : null}
        onClose={handleCloseDialog}
        onDeactivate={handleDeactivate}
      />

      <ReactivateAdminDialog
        open={dialog.type === "reactivate"}
        admin={dialog.type === "reactivate" ? dialog.admin : null}
        onClose={handleCloseDialog}
        onReactivate={handleReactivate}
      />
    </div>
  );
}