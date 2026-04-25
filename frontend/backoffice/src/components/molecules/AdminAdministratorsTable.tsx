import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { AdminActionsDropdown } from "@/components/molecules/AdminActionsDropdown";
import type { AdminUserDto, PaginatedResult } from "@/lib/admin-api";

interface AdminAdministratorsTableProps {
  result: PaginatedResult<AdminUserDto> | null;
  isLoading: boolean;
  isError: boolean;
  onRetry: () => void;
  onEdit: (admin: AdminUserDto) => void;
  onResetPassword: (admin: AdminUserDto) => void;
  onDeactivate: (admin: AdminUserDto) => void;
  onReactivate: (admin: AdminUserDto) => void;
  resettingPasswordId?: string;
}

const SKELETON_ROWS = 5;

export function AdminAdministratorsTable({
  result,
  isLoading,
  isError,
  onRetry,
  onEdit,
  onResetPassword,
  onDeactivate,
  onReactivate,
  resettingPasswordId,
}: AdminAdministratorsTableProps) {
  if (isLoading && !result) {
    return (
      <div
        aria-busy="true"
        aria-label="Carregando administradores..."
        data-testid="table-loading"
      >
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="text-left py-3 px-4 font-semibold text-muted-foreground">Nome</th>
              <th className="text-left py-3 px-4 font-semibold text-muted-foreground">Email</th>
              <th className="text-left py-3 px-4 font-semibold text-muted-foreground w-[100px]">Status</th>
              <th className="text-left py-3 px-4 font-semibold text-muted-foreground w-[120px]">Senha Temp</th>
              <th className="text-left py-3 px-4 font-semibold text-muted-foreground w-[64px]">Ações</th>
            </tr>
          </thead>
          <tbody>
            {Array.from({ length: SKELETON_ROWS }).map((_, i) => (
              <tr key={i} className="border-b last:border-0">
                <td className="py-3 px-4"><Skeleton className="h-4 w-full" /></td>
                <td className="py-3 px-4"><Skeleton className="h-4 w-full" /></td>
                <td className="py-3 px-4"><Skeleton className="h-4 w-16" /></td>
                <td className="py-3 px-4"><Skeleton className="h-4 w-16" /></td>
                <td className="py-3 px-4"><Skeleton className="h-8 w-8 rounded" /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }

  if (isError && !result) {
    return (
      <div
        className="p-6 text-center text-sm text-destructive"
        data-testid="table-error"
      >
        Falha ao carregar administradores. Tente novamente.{" "}
        <button
          type="button"
          className="underline hover:no-underline"
          onClick={onRetry}
          data-testid="retry-button"
        >
          Tentar novamente
        </button>
      </div>
    );
  }

  if (!isLoading && result && result.totalCount === 0) {
    return (
      <div
        className="p-6 text-center space-y-1"
        data-testid="table-empty"
      >
        <p className="text-sm font-semibold">Nenhum administrador encontrado.</p>
        <p className="text-sm text-muted-foreground">Ajuste os filtros ou crie um novo administrador.</p>
      </div>
    );
  }

  const items = result?.items ?? [];

  return (
    <div
      className={isLoading ? "opacity-60 pointer-events-none" : undefined}
      aria-busy={isLoading}
      data-testid="administrators-table-wrapper"
    >
      <table className="w-full text-sm" data-testid="administrators-table">
        <thead>
          <tr className="border-b bg-muted/50">
            <th scope="col" className="text-left py-3 px-4 font-semibold text-muted-foreground">Nome</th>
            <th scope="col" className="text-left py-3 px-4 font-semibold text-muted-foreground">Email</th>
            <th scope="col" className="text-left py-3 px-4 font-semibold text-muted-foreground w-[100px]">Status</th>
            <th scope="col" className="text-left py-3 px-4 font-semibold text-muted-foreground w-[120px]">Senha Temp</th>
            <th scope="col" className="text-left py-3 px-4 font-semibold text-muted-foreground w-[64px]">Ações</th>
          </tr>
        </thead>
        <tbody>
          {items.map((admin) => (
            <tr
              key={admin.id}
              className="border-b last:border-0 hover:bg-muted/30 transition-colors"
              data-testid={`admin-row-${admin.id}`}
            >
              <td className="py-3 px-4 font-semibold">{admin.fullName}</td>
              <td className="py-3 px-4 text-muted-foreground">{admin.email}</td>
              <td className="py-3 px-4">
                {admin.isEnabled ? (
                  <Badge variant="default" data-testid={`badge-status-active-${admin.id}`}>
                    Ativo
                  </Badge>
                ) : (
                  <Badge variant="destructive" data-testid={`badge-status-inactive-${admin.id}`}>
                    Inativo
                  </Badge>
                )}
              </td>
              <td className="py-3 px-4">
                {admin.hasTemporaryPassword ? (
                  <Badge
                    variant="outline"
                    className="text-amber-600 border-amber-300"
                    data-testid={`badge-temp-password-${admin.id}`}
                  >
                    Pendente
                  </Badge>
                ) : (
                  <Badge
                    variant="outline"
                    className="text-green-600 border-green-300"
                    data-testid={`badge-password-set-${admin.id}`}
                  >
                    Definida
                  </Badge>
                )}
              </td>
              <td className="py-3 px-4">
                <AdminActionsDropdown
                  admin={admin}
                  onEdit={onEdit}
                  onResetPassword={onResetPassword}
                  onDeactivate={onDeactivate}
                  onReactivate={onReactivate}
                  isResettingPassword={resettingPasswordId !== undefined}
                />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}